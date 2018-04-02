# AsyncQueue

A thread-safe queue to support producer-consumer scenarios where a consumer can **`await`** on items to be processed.

`AsyncQueue` was designed to support an application where there may be multiple producers in background threads sending messages to a single consumer for processing (e.g. sending emails).  It's simple to use, essentially supporting just *Enqueue* and *DequeueAsync* methods.

## Example usage

            var queue = new AsyncQueue<int>();

            // Simulate some other task posting an item for processing.
            Task.Run(() =>
                {
                    Thread.Sleep(10);
                    queue.Enqueue(42);
                });

            await queue.DequeueAsync();


