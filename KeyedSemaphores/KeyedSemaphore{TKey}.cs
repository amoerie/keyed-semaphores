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
        ///     The current number of consumers
        /// </summary>
        public int Consumers;

        /// <summary>
        /// Initializes a new instance of a keyed semaphore
        /// </summary>
        /// <param name="key">The unique key of this semaphore</param>
        /// <param name="semaphoreSlim">The semaphore slim that will be used internally for locking purposes</param>
        /// <exception cref="ArgumentNullException">When key is null</exception>
        public KeyedSemaphore(TKey key, SemaphoreSlim semaphoreSlim)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            SemaphoreSlim = semaphoreSlim;
            Consumers = 1;
        }

        
    }
}
