using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
        /// Pre-allocated array of semaphores to handle the actual locking
        /// </summary>
        private readonly SemaphoreSlim[] _semaphores;
        
        /// <summary>
        /// Pre-allocated array of releasers to handle the releasing of the lock
        /// </summary>
        private readonly Releaser[] _releasers;

        /// <summary>
        /// Initializes a new, empty keyed semaphores collection
        /// </summary>
        public KeyedSemaphoresCollection(int? numberOfSemaphores = null)
        {
            numberOfSemaphores ??= Constants.DefaultNumberOfSemaphores;
            _semaphores = new SemaphoreSlim[numberOfSemaphores.Value];
            _releasers = new Releaser[numberOfSemaphores.Value];
            for (var i = 0; i < numberOfSemaphores.Value; i++)
            {
                var semaphore = new SemaphoreSlim(1, 1);
                _semaphores[i] = semaphore;
                _releasers[i] = new Releaser(semaphore);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint ToIndex(TKey key)
        {
            return (uint)key.GetHashCode() % (uint)_semaphores.Length;
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
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async ValueTask<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);
            var semaphore = _semaphores[index];
            
            // Wait synchronously for a little bit to try to avoid a Task allocation if we can, then wait asynchronously
            if (!semaphore.Wait(Constants.SynchronousWaitDuration, cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);
            }

            return _releasers[index];
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
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async ValueTask<bool> TryLockAsync(TKey key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();
            
            var index = ToIndex(key);
            var semaphore = _semaphores[index];
            
            // Wait synchronously for a little bit to try to avoid a Task allocation if we can, then wait asynchronously
            if (!semaphore.Wait(Constants.SynchronousWaitDuration, cancellationToken)
                && !await semaphore.WaitAsync(timeout.Subtract(Constants.SynchronousWaitDuration), cancellationToken))
            {
                return false;
            }
            try
            {
                callback();
            }
            finally
            {
                semaphore.Release();
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
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async ValueTask<bool> TryLockAsync(TKey key, TimeSpan timeout, Func<Task> callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);
            var semaphore = _semaphores[index];
            
            // Wait synchronously for a little bit to try to avoid a Task allocation if we can, then wait asynchronously
            if (!semaphore.Wait(Constants.SynchronousWaitDuration, cancellationToken) 
                && !await semaphore.WaitAsync(timeout.Subtract(Constants.SynchronousWaitDuration), cancellationToken))
            {
                return false;
            }
            try
            {
                await callback().ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
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
            var semaphore = _semaphores[index];
            semaphore.Wait(cancellationToken);
            return _releasers[index];
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
            var semaphore = _semaphores[index];
            if (!semaphore.Wait(timeout, cancellationToken))
            {
                return false;
            }
            try
            {
                callback();
            }
            finally
            {
                semaphore.Release();
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
            return _semaphores[index].CurrentCount == 0;
        }
    }
}
