using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    ///     A collection of keyed semaphores.
    ///     Each key will map to an instance of <see cref="SemaphoreSlim"/> using its hash code.
    ///     This implementation uses a simple pre-created array of <see cref="SemaphoreSlim"/> instances, which are never disposed. 
    ///
    ///     While <see cref="KeyedSemaphoresCollection{TKey}"/> is less flexible than <see cref="KeyedSemaphoresDictionary{TKey}"/>, it has very few allocations and is extremely fast.
    ///     Use this when performance is of the utmost performance.
    ///     In the benchmarks, the dictionary-based approach is ~2x slower and allocates ~6x more. 
    /// </summary>
    /// <remarks>
    ///     Note that it is possible that two keys map to the same underlying <see cref="SemaphoreSlim"/>. 
    ///     This means it is possible that two distinct keys cannot be locked in parallel. 
    ///     The odds of this happening depends on the size of the <see cref="KeyedSemaphoresCollection{TKey}"/>. 
    ///
    ///     Because of this, nested locking is not supported. Deadlocks can occur if two different keys map to the same <see cref="SemaphoreSlim"/>. 
    ///     To avoid this problem, use separate instances of <see cref="KeyedSemaphoresCollection{TKey}"/> or use <see cref="KeyedSemaphoresDictionary{TKey}"/> instead. 
    /// </remarks>
    /// <typeparam name="TKey">The type of key</typeparam>
    public sealed class KeyedSemaphoresCollection<TKey> : IKeyedSemaphoresCollection<TKey> where TKey : notnull
    {
        private readonly TimeSpan _synchronousWaitDuration;

        /// <summary>
        ///     Pre-allocated array of keyed semaphores to handle the releasing of the lock
        /// </summary>
        private readonly SharedKeyedSemaphore[] _keyedSemaphores;
                
        /// <summary>
        ///     Initializes a new, empty keyed semaphores collection
        /// </summary>
        public KeyedSemaphoresCollection(): this(Constants.DefaultNumberOfSemaphores, Constants.DefaultSynchronousWaitDuration)
        {
            
        }
                
        /// <summary>
        ///     Initializes a new, empty keyed semaphores collection
        /// </summary>
        /// <param name="numberOfSemaphores">
        ///     The number of semaphores that will be pre-allocated.
        ///     Every key will map to one of the semaphores.
        ///     Choosing a high value will typically increase throughput and parallelism but allocate slightly more initially.
        ///     Choosing a low value will decrease throughput and parallelism, but allocate less.
        ///     Note that the allocations only happen inside the constructor, and not during typical usage.
        ///     The default value is 4096.
        ///     If you anticipate having a lot more unique keys, then it is recommended to choose a higher value.
        /// </param>
        public KeyedSemaphoresCollection(int numberOfSemaphores): this(numberOfSemaphores, Constants.DefaultSynchronousWaitDuration)
        {
            
        }

        /// <summary>
        ///     Initializes a new, empty keyed semaphores collection
        /// </summary>
        /// <param name="numberOfSemaphores">
        ///     The number of semaphores that will be pre-allocated.
        ///     Every key will map to one of the semaphores.
        ///     Choosing a high value will typically increase throughput and parallelism but allocate slightly more initially.
        ///     Choosing a low value will decrease throughput and parallelism, but allocate less.
        ///     Note that the allocations only happen inside the constructor, and not during typical usage.
        ///     The default value is 4096.
        ///     If you anticipate having a lot more unique keys, then it is recommended to choose a higher value.
        /// </param>
        /// <param name="synchronousWaitDuration">
        ///     The duration of time that will be used to wait for the semaphore synchronously.
        ///     If each semaphore is typically held only for a very short time, it can be beneficial to wait synchronously before waiting asynchronously.
        ///     This avoids a Task allocation and the construction of an async state machine in the cases where the synchronous wait succeeds. 
        /// </param>
        public KeyedSemaphoresCollection(int numberOfSemaphores, TimeSpan synchronousWaitDuration)
        {
            if (synchronousWaitDuration < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(synchronousWaitDuration), synchronousWaitDuration, "Synchronous wait duration cannot be negative");
            }
            
            if (numberOfSemaphores <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfSemaphores), numberOfSemaphores, "Number of semaphores must be higher than 0");
            }

            _synchronousWaitDuration = synchronousWaitDuration;
            _keyedSemaphores = new SharedKeyedSemaphore[numberOfSemaphores];
            for (var i = 0; i < numberOfSemaphores; i++)
            {
                var semaphore = new SemaphoreSlim(1, 1);
                _keyedSemaphores[i] = new SharedKeyedSemaphore(semaphore);
            }
        }
        
        /// <summary>
        ///     Initializes a new, empty keyed semaphores collection
        /// </summary>
        /// <param name="initialCapacity">The initial number of elements that the inner index (<see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />) can contain.</param>
        /// <param name="estimatedConcurrencyLevel">The estimated number of threads that will update the inner index (<see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />) concurrently.</param>
        [Obsolete("Use the constructor that takes a single parameter instead")]
        public KeyedSemaphoresCollection(int initialCapacity, int estimatedConcurrencyLevel): this(initialCapacity, Constants.DefaultSynchronousWaitDuration)
        {
            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint ToIndex(TKey key)
        {
            return (uint)key.GetHashCode() % (uint)_keyedSemaphores.Length;
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async ValueTask<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default, bool continueOnCapturedContext = true)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);
            var releaser = _keyedSemaphores[index];
            var semaphore = releaser._semaphore;

            // Wait synchronously for a little bit to try to avoid a Task allocation if we can, then wait asynchronously
            if (!semaphore.Wait(_synchronousWaitDuration, cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
            }

            return releaser;
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

            var index = ToIndex(key);
            var keyedSemaphore = _keyedSemaphores[index];
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
                keyedSemaphore.Dispose();
            }

            return true;
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async ValueTask<bool> TryLockAsync(TKey key, TimeSpan timeout, Func<Task> callback, CancellationToken cancellationToken = default, bool continueOnCapturedContext = true)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);
            var keyedSemaphore = _keyedSemaphores[index];
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
                keyedSemaphore.Dispose();
            }

            return true;
        }

        /// <inheritdoc />
        public IDisposable Lock(TKey key, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);
            var keyedSemaphore = _keyedSemaphores[index];
            var semaphore = keyedSemaphore._semaphore;
            semaphore.Wait(cancellationToken);
            return keyedSemaphore;
        }

        /// <inheritdoc />
        public bool TryLock(TKey key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            cancellationToken.ThrowIfCancellationRequested();

            var index = ToIndex(key);
            var keyedSemaphore = _keyedSemaphores[index];
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
                semaphore.Release();
            }

            return true;
        }

        /// <inheritdoc />
        public bool IsInUse(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var index = ToIndex(key);
            var keyedSemaphore = _keyedSemaphores[index];
            var semaphore = keyedSemaphore._semaphore;
            return semaphore.CurrentCount == 0;
        }
    }
}
