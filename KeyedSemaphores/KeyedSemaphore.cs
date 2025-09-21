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
        private static readonly KeyedSemaphoresDictionary<string> Dictionary = new KeyedSemaphoresDictionary<string>();

        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.LockAsync" />
        public static ValueTask<IDisposable> LockAsync(string key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Dictionary.LockAsync(key, cancellationToken);
        }

        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.TryLockAsync(TKey,System.TimeSpan,System.Action,System.Threading.CancellationToken)" />
        public static ValueTask<bool> TryLockAsync(string key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Dictionary.TryLockAsync(key, timeout, callback, cancellationToken);
        }

        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.TryLockAsync(TKey,System.TimeSpan,System.Func{Task},System.Threading.CancellationToken)" />
        public static ValueTask<bool> TryLockAsync(string key, TimeSpan timeout, Func<Task> callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Dictionary.TryLockAsync(key, timeout, callback, cancellationToken);
        }
        
        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.Lock" />
        public static IDisposable Lock(string key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Dictionary.Lock(key, cancellationToken);
        }

        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.TryLock" />
        public static bool TryLock(string key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Dictionary.TryLock(key, timeout, callback, cancellationToken);
        }

        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.TryLockAsync(TKey,System.TimeSpan,System.Threading.CancellationToken)" />
        public static async ValueTask<IDisposable?> TryLockAsync(string key, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return await Dictionary.TryLockAsync(key, timeout, cancellationToken);
        }

        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.IsInUse" />
        public static bool IsInUse(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Dictionary.IsInUse(key);
        }
    }
}
