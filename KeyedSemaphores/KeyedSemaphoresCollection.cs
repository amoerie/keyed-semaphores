using System;
using System.Collections.Concurrent;

namespace KeyedSemaphores
{
    internal class KeyedSemaphoresCollection : IKeyedSemaphoreProvider, IKeyedSemaphoreOwner
    {
        private readonly ConcurrentDictionary<string, IKeyedSemaphore> _index;

        internal KeyedSemaphoresCollection()
        {
            _index = new ConcurrentDictionary<string, IKeyedSemaphore>();
        }

        internal KeyedSemaphoresCollection(ConcurrentDictionary<string, IKeyedSemaphore> index)
        {
            _index = index;
        }

        public IKeyedSemaphore Provide(string key)
        {
            while (true)
            {
                // ReSharper disable once InconsistentlySynchronizedField
                if (_index.TryGetValue(key, out IKeyedSemaphore? keyedSemaphore))
                {
                    lock (keyedSemaphore)
                    {
                        if (keyedSemaphore.Consumers > 0 && _index.ContainsKey(key))
                        {
                            keyedSemaphore.IncreaseConsumers();
                            return keyedSemaphore;
                        }
                    }
                }

                keyedSemaphore = new InternalKeyedSemaphore(key, 1, this);

                // ReSharper disable once InconsistentlySynchronizedField
                if (_index.TryAdd(key, keyedSemaphore))
                {
                    return keyedSemaphore;
                }
            }
        }

        public void Return(IKeyedSemaphore keyedSemaphore)
        {
            if (keyedSemaphore == null) throw new ArgumentNullException(nameof(keyedSemaphore));

            lock (keyedSemaphore)
            {
                var remainingConsumers = keyedSemaphore.DecreaseConsumers();

                if (remainingConsumers == 0 && !_index.TryRemove(keyedSemaphore.Key, out _))
                    throw new KeyedSemaphoresException($"Failed to remove a keyed semaphore because it has already been deleted by someone else! Key: {keyedSemaphore.Key}");
            }
        }
    }
}