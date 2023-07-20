using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    ///     A dictionary of keyed semaphores
    ///     Each key will map to a unique <see cref="SemaphoreSlim"/>
    ///
    ///     Instance of <see cref="SemaphoreSlim"/> are created and disposed on the fly
    ///     This implementation uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> in combination with ref counting
    ///
    ///     While <see cref="KeyedSemaphoresDictionary{TKey}"/> is more flexible (it supports nested locking) and more correct (one key maps to one semaphore) than <see cref="KeyedSemaphoresCollection{TKey}"/>,
    ///     it is slower and incurs more allocations.
    ///     Consider using a <see cref="KeyedSemaphoresCollection{TKey}"/> instead of this dictionary is long lived and never needs support for nested locking.
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public sealed class KeyedSemaphoresDictionary<TKey> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, RefCountedKeyedSemaphore<TKey>> _keyedSemaphores;
        private readonly TimeSpan _synchronousWaitDuration;

        /// <summary>
        ///     Initializes a new, empty keyed semaphores dictionary
        /// </summary>
        public KeyedSemaphoresDictionary(): this(Environment.ProcessorCount, 31, EqualityComparer<TKey>.Default, Constants.DefaultSynchronousWaitDuration)
        {
        }

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="KeyedSemaphoresDictionary{TKey}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="IEqualityComparer{TKey}"/> and specified <paramref name="synchronousWaitDuration"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="capacity">The initial number of elements that the <see cref="ConcurrentDictionary{TKey,TValue}"/> can contain.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys.</param>
        /// <param name="synchronousWaitDuration">
        ///     The duration of time that will be used to wait for the semaphore synchronously.
        ///     If each semaphore is typically held only for a very short time, it can be beneficial to wait synchronously before waiting asynchronously.
        ///     This avoids a Task allocation and the construction of an async state machine in the cases where the synchronous wait succeeds. 
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> is less than 1. -or- <paramref name="capacity"/> is less than 0.</exception>
        public KeyedSemaphoresDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer, TimeSpan synchronousWaitDuration)
        {
            _keyedSemaphores = new ConcurrentDictionary<TKey, RefCountedKeyedSemaphore<TKey>>(concurrencyLevel, capacity, comparer);
            _synchronousWaitDuration = synchronousWaitDuration;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private RefCountedKeyedSemaphore<TKey> GetKeyedSemaphore(TKey key)
        {
            while (true)
            {
                if (_keyedSemaphores.TryGetValue(key, out var existingKeyedSemaphore))
                {
                    var keyedSemaphore = existingKeyedSemaphore.IncrementRefs();

                    if (_keyedSemaphores.TryUpdate(key, keyedSemaphore, existingKeyedSemaphore))
                    {
                        return keyedSemaphore;
                    }
                }
                else
                {
                    var keyedSemaphore = new RefCountedKeyedSemaphore<TKey>(key, _keyedSemaphores);

                    if (_keyedSemaphores.TryAdd(key, keyedSemaphore))
                    {
                        return keyedSemaphore;
                    }
                }
            }
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

            var keyedSemaphore = GetKeyedSemaphore(key);
            var semaphore = keyedSemaphore._semaphore;
            
            // Wait synchronously for a little bit to try to avoid a Task allocation if we can, then wait asynchronously
            if (!semaphore.Wait(_synchronousWaitDuration, cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);
            }

            return keyedSemaphore._releaser;
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
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout cannot be negative");
            }
            
            cancellationToken.ThrowIfCancellationRequested();

            var keyedSemaphore = GetKeyedSemaphore(key);
            var semaphore = keyedSemaphore._semaphore;

            if (timeout < _synchronousWaitDuration)
            {
                if (!semaphore.Wait(timeout, cancellationToken))
                {
                    return false;
                }
            }
            else
            {
                // Wait synchronously for a little bit to try to avoid a Task allocation if we can, then wait asynchronously
                if (!semaphore.Wait(_synchronousWaitDuration, cancellationToken)
                    && !await semaphore.WaitAsync(timeout.Subtract(_synchronousWaitDuration), cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }
            }

            try
            {
                callback();
            }
            finally
            {
                keyedSemaphore._releaser.Dispose();
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

            var keyedSemaphore = GetKeyedSemaphore(key);
            var semaphore = keyedSemaphore._semaphore;

            if (timeout < _synchronousWaitDuration)
            {
                if (!semaphore.Wait(timeout, cancellationToken))
                {
                    return false;
                }
            }
            else
            {
                // Wait synchronously for a little bit to try to avoid a Task allocation if we can, then wait asynchronously
                if (!semaphore.Wait(_synchronousWaitDuration, cancellationToken)
                    && !await semaphore.WaitAsync(timeout.Subtract(_synchronousWaitDuration), cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }
            }

            try
            {
                await callback().ConfigureAwait(false);
            }
            finally
            {
                keyedSemaphore._releaser.Dispose();
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

            var keyedSemaphore = GetKeyedSemaphore(key);
            var semaphore = keyedSemaphore._semaphore;
            semaphore.Wait(cancellationToken);
            return keyedSemaphore._releaser;
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

            var keyedSemaphore = GetKeyedSemaphore(key);
            var semaphore = keyedSemaphore._semaphore;
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
                keyedSemaphore._releaser.Dispose();
            }

            return true;
        }
        
        /// <summary>
        ///     Checks whether the provided key is already locked by anyone
        /// </summary>
        /// <param name="key">
        ///     The unique key of this keyed semaphore
        /// </param>
        /// <returns>
        ///     True when the key is already locked by anyone or false otherwise
        /// </returns>
        public bool IsInUse(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _keyedSemaphores.TryGetValue(key, out var keyedSemaphore)
                   && keyedSemaphore._semaphore.CurrentCount == 0;
        }
    }
}
