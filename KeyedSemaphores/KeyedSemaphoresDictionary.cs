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
    public sealed class KeyedSemaphoresDictionary<TKey>: IKeyedSemaphoresCollection<TKey> where TKey : notnull
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

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async ValueTask<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default, bool continueOnCapturedContext = true)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();

            var keyedSemaphore = GetKeyedSemaphore(key);
            var semaphore = keyedSemaphore._semaphore;
            
            // Wait synchronously for a little bit to try to avoid a Task allocation if we can, then wait asynchronously
            if (!semaphore.Wait(_synchronousWaitDuration, cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }

            return keyedSemaphore._releaser;
        }
        
        /// <inheritdoc />
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async ValueTask<bool> TryLockAsync(TKey key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default, bool continueOnCapturedContext = true)
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
                    && !await semaphore.WaitAsync(timeout.Subtract(_synchronousWaitDuration), cancellationToken).ConfigureAwait(continueOnCapturedContext))
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
        
        /// <inheritdoc />
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async ValueTask<bool> TryLockAsync(TKey key, TimeSpan timeout, Func<Task> callback, CancellationToken cancellationToken = default, bool continueOnCapturedContext = true)
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
                    && !await semaphore.WaitAsync(timeout.Subtract(_synchronousWaitDuration), cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    return false;
                }
            }

            try
            {
                await callback().ConfigureAwait(continueOnCapturedContext);
            }
            finally
            {
                keyedSemaphore._releaser.Dispose();
            }

            return true;
        }
        
        /// <inheritdoc />
        public IDisposable Lock(TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();

            var keyedSemaphore = GetKeyedSemaphore(key);
            var semaphore = keyedSemaphore._semaphore;
            semaphore.Wait(cancellationToken);
            return keyedSemaphore._releaser;
        }

        /// <inheritdoc />
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
        
        /// <inheritdoc />
        public bool IsInUse(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _keyedSemaphores.TryGetValue(key, out var keyedSemaphore)
                   && keyedSemaphore._semaphore.CurrentCount == 0;
        }
    }
}
