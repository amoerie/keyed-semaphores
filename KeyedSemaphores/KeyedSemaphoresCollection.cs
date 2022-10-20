using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    ///     A collection of keyed semaphores
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public sealed class KeyedSemaphoresCollection<TKey>
    {
        internal readonly Dictionary<TKey, KeyedSemaphore<TKey>> Index;

        /// <summary>
        ///     Initializes a new, empty keyed semaphores collection
        /// </summary>
        public KeyedSemaphoresCollection()
        {
            Index = new Dictionary<TKey, KeyedSemaphore<TKey>>();
        }

        internal KeyedSemaphoresCollection(Dictionary<TKey, KeyedSemaphore<TKey>> index)
        {
            Index = index;
        }

        /// <summary>
        ///     Gets or creates a semaphore with the provided key
        /// </summary>
        /// <param name="key">The key of the semaphore</param>
        /// <returns>A new or existing <see cref="KeyedSemaphore{TKey}" /></returns>
        private KeyedSemaphore<TKey> Provide(TKey key)
        {
            KeyedSemaphore<TKey> keyedSemaphore;
            lock (Index)
            {
                if (Index.TryGetValue(key, out keyedSemaphore))
                {
                    keyedSemaphore.Consumers++;
                }
                else
                {
                    keyedSemaphore = new KeyedSemaphore<TKey>(key, new SemaphoreSlim(1));
                    Index[key] = keyedSemaphore;
                }
            }
            return keyedSemaphore;

            /*while (true)
            {
                // ReSharper disable once InconsistentlySynchronizedField
                if (_index.TryGetValue(key, out var existingKeyedSemaphore))
                    lock (existingKeyedSemaphore)
                    {
                        if (existingKeyedSemaphore.Consumers > 0 && _index.ContainsKey(key))
                        {
                            existingKeyedSemaphore.IncreaseConsumers();
                            return existingKeyedSemaphore;
                        }
                    }

                var newKeyedSemaphore = new KeyedSemaphore<TKey>(key, 1, this);

                // ReSharper disable once InconsistentlySynchronizedField
                if (_index.TryAdd(key, newKeyedSemaphore)) return newKeyedSemaphore;

                newKeyedSemaphore.InternalDispose();
            }*/
        }
        
        /// <summary>
        ///     Gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        ///     An instance of <see cref="KeyedSemaphore{TKey}" /> that has already acquired a lock on the inner <see cref="SemaphoreSlim" />
        /// </returns>
        public async Task<LockedKeyedSemaphore<TKey>> LockAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var keyedSemaphore = Provide(key);

            await keyedSemaphore.SemaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new LockedKeyedSemaphore<TKey>(this, keyedSemaphore);
        }

        /// <summary>
        ///     Synchronously gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it.
        /// </summary>
        /// <remarks>
        ///     This method will block the current thread until the keyed semaphore lock is acquired.
        ///     If possible, consider using the asynchronous <see cref="LockAsync" /> method which does not block the thread
        /// </remarks>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        ///     An instance of <see cref="KeyedSemaphore{TKey}" /> that has already acquired a lock on the inner <see cref="SemaphoreSlim" />
        /// </returns>
        public LockedKeyedSemaphore<TKey> Lock(TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var keyedSemaphore = Provide(key);

            keyedSemaphore.SemaphoreSlim.Wait(cancellationToken);

            return new LockedKeyedSemaphore<TKey>(this, keyedSemaphore);
        }
    }
}
