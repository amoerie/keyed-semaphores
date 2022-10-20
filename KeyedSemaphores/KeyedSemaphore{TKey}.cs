using System;
using System.Threading;

namespace KeyedSemaphores
{
    /// <summary>
    ///     A wrapper around <see cref="System.Threading.SemaphoreSlim" /> that has a unique key.
    /// </summary>
    internal sealed class KeyedSemaphore<TKey>
    {
        /// <summary>
        ///     The unique key of this semaphore
        /// </summary>
        public readonly TKey Key;
        
        /// <summary>
        ///     The semaphore slim that will be used for locking
        /// </summary>
        public readonly SemaphoreSlim SemaphoreSlim;

        /// <summary>
        ///     The consumer counter
        /// </summary>
        public int Consumers;

        /// <summary>
        ///     The releaser that is responsible for decreasing the consumers of this keyed semaphore and potentially removing it from the index
        /// </summary>
        public KeyedSemaphoreReleaser<TKey> Releaser;

        /// <summary>
        /// Initializes a new instance of a keyed semaphore
        /// </summary>
        /// <param name="key">The unique key of this semaphore</param>
        /// <param name="semaphoreSlim">The semaphore slim that will be used internally for locking purposes</param>
        /// <param name="collection">The collection to which this keyed semaphore belongs</param>
        /// <exception cref="ArgumentNullException">When key is null</exception>
        public KeyedSemaphore(TKey key, SemaphoreSlim semaphoreSlim, KeyedSemaphoresCollection<TKey> collection)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            SemaphoreSlim = semaphoreSlim;
            Consumers = 1;
            Releaser = new KeyedSemaphoreReleaser<TKey>(collection, this);
        }

        
    }
}
