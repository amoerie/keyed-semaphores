using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    ///     A collection of keyed semaphores
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public sealed class KeyedSemaphoresCollection<TKey> where TKey : notnull
    {
        /// <summary>
        /// Pre-allocated array of locks
        /// int instead of boolean/byte because that is compatible with Interlocked.CompareExchange
        /// </summary>
        private readonly int[] _locks;

        /// <summary>
        /// Pre-allocated array of unlockers that can be disposed over and over again
        /// </summary>
        private readonly Unlocker[] _unlockers;

        /// <summary>
        /// Limit the number of threads that are spinning waiting for a lock using the wait queue
        /// </summary>
        private readonly SemaphoreSlim[] _waitQueue;

        private const int NumberOfLocks = 4096;

        /// <summary>
        ///     Initializes a new, empty keyed semaphores collection
        /// </summary>
        public KeyedSemaphoresCollection()
        {
            _locks = new int[NumberOfLocks];
            _waitQueue = new SemaphoreSlim[NumberOfLocks];
            _unlockers = new Unlocker[NumberOfLocks];
            for (var i = 0; i < NumberOfLocks; i++)
            {
                _unlockers[i] = new Unlocker(_locks, i);
                _waitQueue[i] = new SemaphoreSlim(1, 1);
            }
        }

        private uint ToIndex(TKey key)
        {
            return (uint)key.GetHashCode() % (uint)_locks.Length;
        }

        /// <summary>
        ///     Gets or creates a keyed semaphore with the provided unique key
        ///     and immediately waits to lock on the inner <see cref="SemaphoreSlim"/> using the provided <paramref name="cancellationToken"/>
        /// </summary>
        /// <param name="key">
        ///     The unique key of this keyed semaphore
        /// </param>
        /// <param name="cancellationToken">
        ///     The <see cref="T:System.Threading.CancellationToken"></see> token to observe.
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> that must be disposed to release the keyed semaphore
        /// </returns>
        /// <exception cref="T:System.OperationCanceledException">
        ///     <paramref name="cancellationToken">cancellationToken</paramref> was canceled.
        /// </exception>
        public async Task<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var index = ToIndex(key);

            cancellationToken.ThrowIfCancellationRequested();

            if (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
            {
                var semaphore = Interlocked.CompareExchange(ref _waitQueue[index], new SemaphoreSlim(1, 1), null)!;
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Only one thread per key can be in the spin-wait mode
                    while (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
                    {
                        await Task.Yield();

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }

            return _unlockers[index];
        }

        /// <summary>
        ///     Gets or creates a keyed semaphore with the provided unique key
        ///     and immediately tries to lock on the inner <see cref="SemaphoreSlim"/> using the provided <paramref name="timeout"/> and <paramref name="cancellationToken"/>
        /// </summary>
        /// <param name="key">
        ///     The unique key of this keyed semaphore
        /// </param>
        /// <param name="timeout">
        ///     A <see cref="T:System.TimeSpan" /> that represents the number of milliseconds to wait
        ///     , a <see cref="T:System.TimeSpan" /> that represents -1 milliseconds to wait indefinitely
        ///     , or a <see cref="T:System.TimeSpan" /> that represents 0 milliseconds to test the wait handle and return immediately.
        /// </param>
        /// <param name="callback">
        ///     A synchronous callback that will be invoked when the keyed semaphore has been locked
        ///     The keyed semaphore will be released automatically after the callback has completed
        /// </param>
        /// <param name="cancellationToken">
        ///     The <see cref="T:System.Threading.CancellationToken"></see> token to observe.
        /// </param>
        /// <returns>
        ///     True when locking the inner <see cref="SemaphoreSlim"/> succeeded and the callback was invoked. 
        ///     False when locking the inner <see cref="SemaphoreSlim"/> failed and the callback was not invoked. 
        /// </returns>
        /// <exception cref="T:System.OperationCanceledException">
        ///     <paramref name="cancellationToken">cancellationToken</paramref> was canceled.
        /// </exception>
        public async Task<bool> TryLockAsync(TKey key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var index = ToIndex(key);

            cancellationToken.ThrowIfCancellationRequested();

            if (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
            {
                var semaphore = _waitQueue[index];
                var stop = Stopwatch.GetTimestamp() + timeout.Ticks;
                if (!await semaphore.WaitAsync(timeout, cancellationToken))
                {
                    // timeout
                    return false;
                }
                
                try
                {
                    while (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
                    {
                        await Task.Yield();

                        cancellationToken.ThrowIfCancellationRequested();

                        if (Stopwatch.GetTimestamp() > stop)
                        {
                            // timeout
                            return false;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }

            try
            {
                callback();
            }
            finally
            {
                _locks[index] = 0;
            }

            return true;
        }

        /// <summary>
        ///     Gets or creates a keyed semaphore with the provided unique key
        ///     and immediately tries to lock on the inner <see cref="SemaphoreSlim"/> using the provided <paramref name="timeout"/> and <paramref name="cancellationToken"/>
        /// </summary>
        /// <param name="key">
        ///     The unique key of this keyed semaphore
        /// </param>
        /// <param name="timeout">
        ///     A <see cref="T:System.TimeSpan" /> that represents the number of milliseconds to wait
        ///     , a <see cref="T:System.TimeSpan" /> that represents -1 milliseconds to wait indefinitely
        ///     , or a <see cref="T:System.TimeSpan" /> that represents 0 milliseconds to test the wait handle and return immediately.
        /// </param>
        /// <param name="callback">
        ///     An asynchronous callback that will be invoked when the keyed semaphore has been locked
        ///     The keyed semaphore will be released automatically after the callback has completed
        /// </param>
        /// <param name="cancellationToken">
        ///     The <see cref="T:System.Threading.CancellationToken"></see> token to observe.
        /// </param>
        /// <returns>
        ///     True when locking the inner <see cref="SemaphoreSlim"/> succeeded and the callback was invoked. 
        ///     False when locking the inner <see cref="SemaphoreSlim"/> failed and the callback was not invoked. 
        /// </returns>
        /// <exception cref="T:System.OperationCanceledException">
        ///     <paramref name="cancellationToken">cancellationToken</paramref> was canceled.
        /// </exception>
        public async Task<bool> TryLockAsync(TKey key, TimeSpan timeout, Func<Task> callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);

            if (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
            {
                var stop = Stopwatch.GetTimestamp() + timeout.Ticks;
                var semaphore = _waitQueue[index];
                if (!await semaphore.WaitAsync(timeout, cancellationToken))
                {
                    return false;
                }

                try
                {
                    while (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
                    {
                        await Task.Yield();

                        cancellationToken.ThrowIfCancellationRequested();

                        if (Stopwatch.GetTimestamp() > stop)
                        {
                            // timeout
                            return false;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }

            try
            {
                await callback().ConfigureAwait(false);
            }
            finally
            {
                _locks[index] = 0;
            }

            return true;
        }

        /// <summary>
        ///     Gets or creates a keyed semaphore with the provided unique key
        ///     and immediately waits to lock on the inner <see cref="SemaphoreSlim"/> using the provided <paramref name="cancellationToken"/>
        /// </summary>
        /// <param name="key">
        ///     The unique key of this keyed semaphore
        /// </param>
        /// <param name="cancellationToken">
        ///     The <see cref="T:System.Threading.CancellationToken"></see> token to observe.
        /// </param>
        /// <returns>
        ///     An <see cref="IDisposable"/> that must be disposed to release the keyed semaphore
        /// </returns>
        /// <exception cref="T:System.OperationCanceledException">
        ///     <paramref name="cancellationToken">cancellationToken</paramref> was canceled.
        /// </exception>
        public IDisposable Lock(TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);

            if (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
            {
                var semaphore = _waitQueue[index];
                
                semaphore.Wait(cancellationToken);

                try
                {
                    while (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
                    {
                        Thread.Yield();

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }

            return _unlockers[index];
        }

        /// <summary>
        ///     Gets or creates a keyed semaphore with the provided unique key
        ///     and immediately tries to lock on the inner <see cref="SemaphoreSlim"/> using the provided <paramref name="timeout"/> and <paramref name="cancellationToken"/>
        /// </summary>
        /// <param name="key">
        ///     The unique key of this keyed semaphore
        /// </param>
        /// <param name="timeout">
        ///     A <see cref="T:System.TimeSpan" /> that represents the number of milliseconds to wait
        ///     , a <see cref="T:System.TimeSpan" /> that represents -1 milliseconds to wait indefinitely
        ///     , or a <see cref="T:System.TimeSpan" /> that represents 0 milliseconds to test the wait handle and return immediately.
        /// </param>
        /// <param name="callback">
        ///     A synchronous callback that will be invoked when the keyed semaphore has been locked
        ///     The keyed semaphore will be released automatically after the callback has completed
        /// </param>
        /// <param name="cancellationToken">
        ///     The <see cref="T:System.Threading.CancellationToken"></see> token to observe.
        /// </param>
        /// <returns>
        ///     True when locking the inner <see cref="SemaphoreSlim"/> succeeded and the callback was invoked. 
        ///     False when locking the inner <see cref="SemaphoreSlim"/> failed and the callback was not invoked. 
        /// </returns>
        /// <exception cref="T:System.OperationCanceledException">
        ///     <paramref name="cancellationToken">cancellationToken</paramref> was canceled.
        /// </exception>
        public bool TryLock(TKey key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);

            if (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
            {
                var semaphore = _waitQueue[index];
                var stop = Stopwatch.GetTimestamp() + timeout.Ticks;
                if (!semaphore.Wait(timeout, cancellationToken))
                {
                    return false;
                }

                try
                {
                    while (Interlocked.CompareExchange(ref _locks[index], 1, 0) == 1)
                    {
                        Thread.Yield();

                        cancellationToken.ThrowIfCancellationRequested();

                        if (Stopwatch.GetTimestamp() > stop)
                        {
                            // timeout
                            return false;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }

            try
            {
                callback();
            }
            finally
            {
                _locks[index] = 0;
            }

            return true;
        }

        /// <summary>
        ///     Check if keyed semaphore already has the provided unique key
        /// </summary>
        /// <param name="key">
        ///     The unique key of this keyed semaphore
        /// </param>
        /// <returns>
        ///     True when key are already locked
        ///     False when key are available for lock
        /// </returns>
        public bool IsInUse(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var index = ToIndex(key);

            return Interlocked.CompareExchange(ref _locks[index], 0, 0) == 1;
        }
    }
}
