using System;
using System.Collections.Concurrent;
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
        internal readonly ConcurrentDictionary<TKey, KeyedSemaphore<TKey>> Index;

        /// <summary>
        ///     Initializes a new, empty keyed semaphores collection
        /// </summary>
        public KeyedSemaphoresCollection()
        {
            Index = new ConcurrentDictionary<TKey, KeyedSemaphore<TKey>>();
        }

        internal KeyedSemaphoresCollection(ConcurrentDictionary<TKey, KeyedSemaphore<TKey>> index)
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
            /*KeyedSemaphore<TKey> keyedSemaphore;
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
            return keyedSemaphore;*/

            while (true)
            {
                if (Index.TryGetValue(key, out var keyedSemaphore) && Monitor.TryEnter(keyedSemaphore))
                {
                    keyedSemaphore.Consumers++;

                    Monitor.Exit(keyedSemaphore);

                    return keyedSemaphore;
                }

                keyedSemaphore = new KeyedSemaphore<TKey>(key, new SemaphoreSlim(1), this);

                if (Index.TryAdd(key, keyedSemaphore))
                {
                    return keyedSemaphore;
                }
            }
        }

        /// <summary>
        ///     Gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="timeout">
        /// Time to wait for lock. By default wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        ///     An instance of <see cref="KeyedSemaphore{TKey}" /> that has already acquired a lock on the inner <see cref="SemaphoreSlim" />
        /// </returns>
        public async Task<IDisposable> LockAsync(TKey key, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (timeout == default) timeout = Timeout.InfiniteTimeSpan;

            var keyedSemaphore = Provide(key);
            try
            {
                if (!await keyedSemaphore.SemaphoreSlim.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
                {
                    throw new TimeoutException($"Couldn't get the lock after waiting {timeout}.", timeout);
                }
            }
            catch (Exception)
            {
                keyedSemaphore.Releaser.Dispose();
                throw;
            }

            return keyedSemaphore.Releaser;
        }

        /// <summary>
        ///     Synchronously gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it.
        /// </summary>
        /// <remarks>
        ///     This method will block the current thread until the keyed semaphore lock is acquired.
        ///     If possible, consider using the asynchronous <see cref="LockAsync" /> method which does not block the thread
        /// </remarks>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="timeout">
        /// Time to wait for lock. By default wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        ///     An instance of <see cref="KeyedSemaphore{TKey}" /> that has already acquired a lock on the inner <see cref="SemaphoreSlim" />
        /// </returns>
        public IDisposable Lock(TKey key, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (timeout == default) timeout = Timeout.InfiniteTimeSpan;

            var keyedSemaphore = Provide(key);
            try
            {
                if (!keyedSemaphore.SemaphoreSlim.Wait(timeout, cancellationToken))
                {
                    throw new TimeoutException($"Couldn't get the lock after waiting {timeout}.", timeout);
                }
            }
            catch (Exception)
            {
                keyedSemaphore.Releaser.Dispose();
                throw;
            }

            return keyedSemaphore.Releaser;
        }
    }
}