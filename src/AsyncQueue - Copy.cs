using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncQueue
{
    public class AsyncQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly object _syncRoot = new object();
        private readonly ConcurrentQueue<TaskCompletionSource<T>> _waitingConsumers = new ConcurrentQueue<TaskCompletionSource<T>>();

        public int Count => _queue.Count;

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);

            NotifyAnyWaitingConsumers();
        }

        public Task<T> DequeueAsync()
        {
            return DequeueAsync(CancellationToken.None);
        }

        public async Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<T> itemCompletionEvent;
            CancellationTokenRegistration completionRegistration;

            lock (_syncRoot)
            {
                if (_queue.TryDequeue(out var queueItem)) { return queueItem; }

                itemCompletionEvent = new TaskCompletionSource<T>();
                completionRegistration = cancellationToken.Register(() => itemCompletionEvent.TrySetCanceled(), useSynchronizationContext: true);

                _waitingConsumers.Enqueue(itemCompletionEvent);
            }

            var item = await itemCompletionEvent.Task;

            completionRegistration.Dispose();

            return item;
        }

        private void NotifyAnyWaitingConsumers()
        {
            lock (_syncRoot)
            {
                TaskCompletionSource<T> consumer;

                do
                {
                    if (_waitingConsumers.TryDequeue(out consumer) &&
                        _queue.TryPeek(out var queueItem) &&
                        consumer.TrySetResult(queueItem))
                    {
                        _queue.TryDequeue(out _); // Remove item just passed to consumer.
                    }
                }
                while (Count > 0 && consumer != null);
            }
        }

    }
}
