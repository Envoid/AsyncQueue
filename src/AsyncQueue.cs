using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncQueue
{
    public class AsyncQueue<T>
    {
        private readonly LinkedList<T> _queue = new LinkedList<T>();
        private readonly object _syncRoot = new object();
        private readonly ConcurrentQueue<TaskCompletionSource<T>> _waitingConsumers = new ConcurrentQueue<TaskCompletionSource<T>>();

        public int Count
        {
            get
            {
                lock (_syncRoot) 
                { 
                    return _queue.Count; 
                }
            }
        }


        public void Enqueue(T item)
        {
            lock (_syncRoot)
            {
                _queue.AddLast(item);
            }

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
                if (_queue.Any())
                {
                    return RemoveAndReturnFirstQueueItem(_queue);
                }

                itemCompletionEvent = new TaskCompletionSource<T>();
                completionRegistration = cancellationToken.Register(() => itemCompletionEvent.TrySetCanceled(), true);

                _waitingConsumers.Enqueue(itemCompletionEvent);
            }

            T item = await itemCompletionEvent.Task.ConfigureAwait(false);

            completionRegistration.Dispose();

            return item;
        }


        private void NotifyAnyWaitingConsumers()
        {
            lock (_syncRoot)
            {
                if (!_queue.Any()) { return; }

                TaskCompletionSource<T> consumer;

                do
                {
                    if (_waitingConsumers.TryDequeue(out consumer))
                    {
                        T item = RemoveAndReturnFirstQueueItem(_queue);

                        if (!consumer.TrySetResult(item))
                        {
                            _queue.AddFirst(item); // Put back in so can try with subsequent consumer.
                        }
                    }
                }
                while (Count > 0 && consumer != null);
            }
        }


        private T RemoveAndReturnFirstQueueItem(LinkedList<T> list)
        {
            LinkedListNode<T> node = list.First;

            list.Remove(node);

            return node.Value;
        }
    }
}
