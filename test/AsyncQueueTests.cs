using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncQueue;
using NUnit.Framework;

namespace AsyncQueueTests
{
    [TestFixture]
    public class AsyncQueueTests
    {
        [Test]
        public async Task SingleTask_EnqueueSingleItem_QueueContainsSingleItem()
        {
            var queue = new AsyncQueue<int>();

            queue.Enqueue(42);

            Assert.AreEqual(1, queue.Count);

            var result = await queue.DequeueAsync();

            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(42, result);
        }

        [Test]
        public async Task SingleTask_EnqueueMultipleItems_DequeuedInOriginalOrder()
        {
            var queue = new AsyncQueue<int>();

            queue.Enqueue(42);
            queue.Enqueue(43);
            queue.Enqueue(44);

            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual(42, await queue.DequeueAsync());
            Assert.AreEqual(43, await queue.DequeueAsync());
            Assert.AreEqual(44, await queue.DequeueAsync());

            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void SingleTask_DequeueBeforeAnyAdded_DequeuedOK()
        {
            var queue = new AsyncQueue<int>();

            Task<int> consumer = queue.DequeueAsync();

            Assert.False(consumer.IsCompleted);

            queue.Enqueue(42);  // Will signal Task completed

            Assert.True(consumer.IsCompleted);
            Assert.AreEqual(42, consumer.Result);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void SingleTask_EnqueueBeforeDequeue_DequeuedOK()
        {
            var queue = new AsyncQueue<int>();

            queue.Enqueue(42);  // Will signal Task completed

            Task<int> consumer = queue.DequeueAsync();

            Assert.True(consumer.IsCompleted);
            Assert.AreEqual(42, consumer.Result);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void TwoConsumers_FirstCancelled_SecondGetsItem()
        {
            var queue = new AsyncQueue<int>();

            var completionSource1 = new CancellationTokenSource();
            Task<int> consumer1 = queue.DequeueAsync(completionSource1.Token);
            completionSource1.Cancel();

            Task<int> consumer2 = queue.DequeueAsync();

            queue.Enqueue(42);

            Assert.True(consumer1.IsCompleted);
            Assert.True(consumer1.IsCanceled);

            Assert.True(consumer2.IsCompleted);
            Assert.AreEqual(42, consumer2.Result);
            Assert.AreEqual(0, queue.Count);
        }


        [Test]
        public async Task BackgroundEnqueueTask_EnqueueBeforeDequeue_DequeuedOK()
        {
            var queue = new AsyncQueue<int>();

            Task.Run(() => queue.Enqueue(42));

            await Task.Delay(10);

            Assert.AreEqual(1, queue.Count);

            var result = await queue.DequeueAsync();

            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(42, result);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public async Task BackgroundEnqueueTask_SingleTaskCompletesByBackgroundTask_QueueIsEmpty()
        {
            var queue = new AsyncQueue<int>();

            Task.Run(() =>
                {
                    Thread.Sleep(10);
                    queue.Enqueue(42);
                });

            Assert.AreEqual(0, queue.Count);

            await queue.DequeueAsync();

            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public async Task BackgroundEnqueTask_DequeueBeforeEnqueue_DequeuedOK()
        {
            var queue = new AsyncQueue<int>();

            Task.Run(() =>
                {
                    Thread.Sleep(10);
                    queue.Enqueue(42);
                });

            Assert.AreEqual(0, queue.Count);

            var result = await queue.DequeueAsync();

            Assert.AreEqual(42, result);
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void SingleTask_DequeueIsCancelled_OperationCanceledExceptionThrown()
        {
            var queue = new AsyncQueue<int>();

            var completionSource = new CancellationTokenSource();

            Task<int> consumer = queue.DequeueAsync(completionSource.Token);

            completionSource.Cancel();

            Assert.That(async () => await consumer, Throws.Exception.AssignableTo<OperationCanceledException>());
        }

    }

}
