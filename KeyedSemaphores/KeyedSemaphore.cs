using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    ///     Static API that allows getting or creating keyed semaphores based on a key
    /// </summary>
    public static class KeyedSemaphore
    {
        private static readonly KeyedSemaphoresCollection<string> Collection = new KeyedSemaphoresCollection<string>();

        /// <summary>
        ///     Asynchronously gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it.
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="timeout">
        /// Time to wait for lock. By default wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        ///     An instance of <see cref="KeyedSemaphore{TKey}" /> that has already acquired a lock on the inner <see cref="SemaphoreSlim" />
        /// </returns>
        public static Task<IDisposable> LockAsync(string key, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Collection.LockAsync(key, timeout, cancellationToken);
        }


        /// <summary>
        ///     Synchronously gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it.
        /// </summary>
        /// <remarks>
        ///     This method will block the current thread until the keyed semaphore lock is acquired.
        ///     If possible, consider using the asynchronous <see cref="LockAsync" /> method which does not block the thread
        /// </remarks>
        /// <param name="timeout">
        /// Time to wait for lock. By default wait indefinitely.
        /// </param>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        ///     An instance of <see cref="KeyedSemaphore{TKey}" /> that has already acquired a lock on the inner <see cref="SemaphoreSlim" />
        /// </returns>
        public static IDisposable Lock(string key, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Collection.Lock(key, timeout, cancellationToken);
        }
    }
}