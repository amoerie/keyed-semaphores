using System;
using System.Threading;

namespace KeyedSemaphores
{
    /// <summary>
    ///     A wrapper around <see cref="System.Threading.SemaphoreSlim" /> that has a unique key.
    /// </summary>
    internal sealed class KeyedSemaphore<TKey>: IDisposable
    {
        private readonly KeyedSemaphoresCollection<TKey> _collection;
        private readonly TKey _key;

        /// <summary>
        ///     The semaphore slim that will be used for locking
        /// </summary>
        public readonly SemaphoreSlim SemaphoreSlim;


        /// <summary>
        ///     The consumer counter
        /// </summary>
        public int Consumers;

        /// <summary>
        /// Initializes a new instance of a keyed semaphore
        /// </summary>
        /// <param name="key">The unique key of this semaphore</param>
        /// <param name="collection">The collection to which this keyed semaphore belongs</param>
        /// <param name="semaphoreSlim">The semaphore slim that will be used internally for locking purposes</param>
        /// <exception cref="ArgumentNullException">When key is null</exception>
        public KeyedSemaphore(TKey key, KeyedSemaphoresCollection<TKey> collection, SemaphoreSlim semaphoreSlim)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            SemaphoreSlim = semaphoreSlim;
            Consumers = 1;
        }

        public void Dispose()
        {
            while (true)
            {
                if (!Monitor.TryEnter(this))
                {
                    continue;
                }

                var remainingConsumers = --Consumers;

                if (remainingConsumers == 0)
                {
                    _collection.Index.TryRemove(_key, out _);
                }

                Monitor.Exit(this);

                break;
            }

            SemaphoreSlim.Release();
        }
    }
}
