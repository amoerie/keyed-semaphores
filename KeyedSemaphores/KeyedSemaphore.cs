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
        private static readonly KeyedSemaphoresCollection Collection = new KeyedSemaphoresCollection();

        /// <summary>
        /// Gets or creates a keyed semaphore with the provided key. One key will always result in the same semaphore.
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <returns>
        /// An instance of <see cref="IKeyedSemaphore"/>  that can be used to lock your C# thread, which must be disposed when you are done.
        /// Once all parallel consumers of the keyed semaphore have disposed their keyed semaphore, it will be cleaned up.
        /// </returns>
        public static IKeyedSemaphore GetOrCreate(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Collection.Provide(key);
        }

        /// <summary>
        /// Gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it. For more fine grained usage of the inner SemaphoreSlim, use <see cref="GetOrCreate"/>
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        /// An instance of <see cref="IKeyedSemaphore"/> that has already acquired a lock on the inner <see cref="SemaphoreSlim"/>
        /// </returns>
        public static async Task<LockedKeyedSemaphore> LockAsync(string key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var keyedSemaphore = GetOrCreate(key);
            
            await keyedSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new LockedKeyedSemaphore(keyedSemaphore);
        }

        /// <summary>
        /// Gets or creates a keyed semaphore with the provided key and immediately acquires a lock on it. For more fine grained usage of the inner SemaphoreSlim, use <see cref="GetOrCreate"/>
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <param name="cancellationToken">A cancellation token that will interrupt trying to acquire the lock</param>
        /// <returns>
        /// An instance of <see cref="IKeyedSemaphore"/> that has already acquired a lock on the inner <see cref="SemaphoreSlim"/>
        /// </returns>
        public static LockedKeyedSemaphore Lock(string key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var keyedSemaphore = GetOrCreate(key);

            keyedSemaphore.Wait(cancellationToken);

            return new LockedKeyedSemaphore(keyedSemaphore);
        }
    }
}