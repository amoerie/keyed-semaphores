using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    ///     Shorthand extension methods for <see cref="KeyedSemaphoresCollection{TKey}" />
    /// </summary>
    public static class KeyedSemaphoresCollectionExtensions
    {
        /// <summary>
        ///     Gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it
        /// </summary>
        /// <param name="collection">The collection of keyed semaphores to use</param>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        ///     An instance of <see cref="IKeyedSemaphore{TKey}" /> that has already acquired a lock on the inner <see cref="SemaphoreSlim" />
        /// </returns>
        public static async Task<LockedKeyedSemaphore<TKey>> LockAsync<TKey>(
            this KeyedSemaphoresCollection<TKey> collection, TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var keyedSemaphore = collection.Provide(key);

            await keyedSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new LockedKeyedSemaphore<TKey>(keyedSemaphore);
        }

        /// <summary>
        ///     Synchronously gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it.
        /// </summary>
        /// <remarks>
        ///     This method will block the current thread until the keyed semaphore lock is acquired.
        ///     If possible, consider using the asynchronous <see cref="LockAsync{TKey}" /> method which does not block the thread
        /// </remarks>
        /// <param name="collection">The collection of keyed semaphores to use</param>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        ///     An instance of <see cref="IKeyedSemaphore{TKey}" /> that has already acquired a lock on the inner <see cref="SemaphoreSlim" />
        /// </returns>
        public static LockedKeyedSemaphore<TKey> Lock<TKey>(
            this KeyedSemaphoresCollection<TKey> collection, TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var keyedSemaphore = collection.Provide(key);

            keyedSemaphore.Wait(cancellationToken);

            return new LockedKeyedSemaphore<TKey>(keyedSemaphore);
        }
    }
}
