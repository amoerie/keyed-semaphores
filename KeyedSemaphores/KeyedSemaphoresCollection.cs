using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    ///     A collection of keyed semaphores
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public sealed class KeyedSemaphoresCollection<TKey>
    {
        internal readonly ConcurrentDictionary<TKey, KeyedSemaphore<TKey>> Index;

        /// <summary>
        ///     Initializes a new, empty keyed semaphores collection
        /// </summary>
        /// <param name="initialCapacity">The initial number of elements that the inner index (<see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />) can contain.</param>
        /// <param name="estimatedConcurrencyLevel">The estimated number of threads that will update the inner index (<see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />) concurrently.</param>
        public KeyedSemaphoresCollection(int? initialCapacity = null, int? estimatedConcurrencyLevel = null)
        {
            Index = initialCapacity != null && estimatedConcurrencyLevel != null
                ? new ConcurrentDictionary<TKey, KeyedSemaphore<TKey>>(estimatedConcurrencyLevel.Value, initialCapacity.Value)
                : new ConcurrentDictionary<TKey, KeyedSemaphore<TKey>>();
        }

        internal KeyedSemaphoresCollection(ConcurrentDictionary<TKey, KeyedSemaphore<TKey>> index)
        {
            Index = index;
        }

        /// <summary>
        ///     Gets or creates a semaphore with the provided key
        /// </summary>
        /// <param name="key">The key of the semaphore</param>
        /// <returns>A new or existing <see cref="KeyedSemaphore{TKey}" /></returns>
        private KeyedSemaphore<TKey> Provide(TKey key)
        {
            if (Index.TryGetValue(key, out var keyedSemaphore))
            {
                if (Monitor.TryEnter(keyedSemaphore))
                {
                    keyedSemaphore.Consumers++;

                    Monitor.Exit(keyedSemaphore);

                    return keyedSemaphore;
                }
            }
            else
            {
                keyedSemaphore = new KeyedSemaphore<TKey>(key, this, new SemaphoreSlim(1));

                if (Index.TryAdd(key, keyedSemaphore))
                {
                    return keyedSemaphore;
                }
            }
            
            while (true)
            {
                if (Index.TryGetValue(key, out keyedSemaphore))
                {
                    if (Monitor.TryEnter(keyedSemaphore))
                    {
                        keyedSemaphore.Consumers++;

                        Monitor.Exit(keyedSemaphore);

                        return keyedSemaphore;
                    }
                }
                else
                {
                    keyedSemaphore = new KeyedSemaphore<TKey>(key, this, new SemaphoreSlim(1));

                    if (Index.TryAdd(key, keyedSemaphore))
                    {
                        return keyedSemaphore;
                    }
                }
            }
        }

        internal void Release(KeyedSemaphore<TKey> keyedSemaphore)
        {
            while (!Monitor.TryEnter(keyedSemaphore))
            {
            }
            
            var remainingConsumers = --keyedSemaphore.Consumers;
            if (remainingConsumers == 0)
            {
                Index.TryRemove(keyedSemaphore.Key, out _);
            }
            
            Monitor.Exit(keyedSemaphore);
            keyedSemaphore.SemaphoreSlim.Release();
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

            var keyedSemaphore = Provide(key);
            try
            {
                await keyedSemaphore.SemaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Release(keyedSemaphore);
                throw;
            }

            return keyedSemaphore;
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

            var keyedSemaphore = Provide(key);
            try
            {
                if (!await keyedSemaphore.SemaphoreSlim.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
                {
                    Release(keyedSemaphore);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                Release(keyedSemaphore);
                throw;
            }

            try
            {
                callback();
            }
            finally
            {
                Release(keyedSemaphore);
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

            var keyedSemaphore = Provide(key);
            try
            {
                if (!await keyedSemaphore.SemaphoreSlim.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
                {
                    Release(keyedSemaphore);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                Release(keyedSemaphore);
                throw;
            }

            try
            {
                await callback().ConfigureAwait(false);
            }
            finally
            {
                Release(keyedSemaphore);     
            }

            return true;
        }/// <summary>
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

            var keyedSemaphore = Provide(key);
            try
            {
                keyedSemaphore.SemaphoreSlim.Wait(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Release(keyedSemaphore);
                throw;
            }

            return keyedSemaphore;
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

            var keyedSemaphore = Provide(key);
            try
            {
                if (!keyedSemaphore.SemaphoreSlim.Wait(timeout, cancellationToken))
                {
                    Release(keyedSemaphore);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                Release(keyedSemaphore);
                throw;
            }

            try
            {
                callback();
            }
            finally
            {
                Release(keyedSemaphore);     
            }

            return true;
        }
    }
}
