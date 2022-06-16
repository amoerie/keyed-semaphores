using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    /// Static API that allows getting or creating keyed semaphores based on a key
    /// </summary>
    public static class KeyedSemaphore
    {
        private static readonly KeyedSemaphoresCollection<string> Collection = new KeyedSemaphoresCollection<string>();

        /// <summary>
        /// Gets or creates a keyed semaphore with the provided key. One key will always result in the same semaphore.
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <returns>
        /// An instance of <see cref="IKeyedSemaphore{TKey}"/>  that can be used to lock your C# thread, which must be disposed when you are done.
        /// Once all parallel consumers of the keyed semaphore have disposed their keyed semaphore, it will be cleaned up.
        /// </returns>
        [Obsolete("Use " + nameof(KeyedSemaphore) + "." + nameof(Provide) + " instead")]
        public static IKeyedSemaphore<string> GetOrCreate(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Collection.Provide(key);
        }

        /// <summary>
        /// Gets or creates a keyed semaphore with the provided key. One key will always result in the same semaphore.
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <returns>
        /// An instance of <see cref="IKeyedSemaphore{TKey}"/>  that can be used to lock your C# thread, which must be disposed when you are done.
        /// Once all parallel consumers of the keyed semaphore have disposed their keyed semaphore, it will be cleaned up.
        /// </returns>
        public static IKeyedSemaphore<string> Provide(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Collection.Provide(key);
        }

        /// <summary>
        /// Asynchronously gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it.
        /// For more fine grained usage of the inner SemaphoreSlim, use <see cref="GetOrCreate"/>
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        /// An instance of <see cref="IKeyedSemaphore{TKey}"/> that has already acquired a lock on the inner <see cref="SemaphoreSlim"/>
        /// </returns>
        public static Task<LockedKeyedSemaphore<string>> LockAsync(string key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Collection.LockAsync(key, cancellationToken);
        }

        /// <summary>
        /// Synchronously gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it.
        /// For more fine grained usage of the inner SemaphoreSlim, use <see cref="GetOrCreate"/>
        /// </summary>
        /// <remarks>
        /// This method will block the current thread until the keyed semaphore lock is acquired.
        /// If possible, consider using the asynchronous <see cref="LockAsync"/> method which does not block the thread 
        /// </remarks>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        /// An instance of <see cref="IKeyedSemaphore{TKey}"/> that has already acquired a lock on the inner <see cref="SemaphoreSlim"/>
        /// </returns>
        public static LockedKeyedSemaphore<string> Lock(string key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Collection.Lock(key, cancellationToken);
        }
    }
}
