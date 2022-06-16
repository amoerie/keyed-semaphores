using System;
using System.Collections.Concurrent;
using System.Linq;

namespace KeyedSemaphores
{
    /// <summary>
    /// A collection of keyed semaphores that can be used to implement key based fine grained synchronous or asynchronous locking
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public sealed class KeyedSemaphoresCollection<TKey> : IKeyedSemaphoresCollection<TKey>, IDisposable
        where TKey : IEquatable<TKey>
    {
        private readonly ConcurrentDictionary<TKey, IKeyedSemaphore<TKey>> _index;
        
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new, empty keyed semaphores collection
        /// </summary>
        public KeyedSemaphoresCollection()
        {
            _isDisposed = false;
            _index = new ConcurrentDictionary<TKey, IKeyedSemaphore<TKey>>();
        }

        internal KeyedSemaphoresCollection(ConcurrentDictionary<TKey, IKeyedSemaphore<TKey>> index)
        {
            _isDisposed = false;
            _index = index;
        }

        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.Provide" />
        public IKeyedSemaphore<TKey> Provide(TKey key)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("This keyed semaphores collection has already been disposed");

            while (true)
            {
                // ReSharper disable once InconsistentlySynchronizedField
                if (_index.TryGetValue(key, out IKeyedSemaphore<TKey>? existingKeyedSemaphore))
                {
                    lock (existingKeyedSemaphore)
                    {
                        if (existingKeyedSemaphore.Consumers > 0 && _index.ContainsKey(key))
                        {
                            existingKeyedSemaphore.IncreaseConsumers();
                            return existingKeyedSemaphore;
                        }
                    }
                }

                var newKeyedSemaphore = new InternalKeyedSemaphore<TKey>(key, 1, this);

                // ReSharper disable once InconsistentlySynchronizedField
                if (_index.TryAdd(key, newKeyedSemaphore))
                {
                    return newKeyedSemaphore;
                }

                newKeyedSemaphore.InternalDispose();
            }
        }

        /// <inheritdoc cref="IKeyedSemaphoresCollection{TKey}.Return" />
        public void Return(IKeyedSemaphore<TKey> keyedSemaphore)
        {
            if (keyedSemaphore == null) throw new ArgumentNullException(nameof(keyedSemaphore));

            // Do not throw ObjectDisposedException here, because this method is only called from InternalKeyedSemaphore.Dispose
            if (_isDisposed)
                return;

            lock (keyedSemaphore)
            {
                var remainingConsumers = keyedSemaphore.DecreaseConsumers();

                if (remainingConsumers == 0)
                {
                    if (!_index.TryRemove(keyedSemaphore.Key, out _))
                        throw new KeyedSemaphoresException($"Failed to remove a keyed semaphore because it has already been deleted by someone else! Key: {keyedSemaphore.Key}");

                    keyedSemaphore.InternalDispose();
                }
            }
        }

        /// <summary>
        /// Cleans up all keyed semaphores that have not been returned yet
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            while (!_index.IsEmpty)
            {
                var keys = _index.Keys.ToList();

                foreach (var key in keys)
                {
                    if (_index.TryRemove(key, out var keyedSemaphore))
                    {
                        keyedSemaphore.InternalDispose();
                    }
                }
            }
        }
    }
}
