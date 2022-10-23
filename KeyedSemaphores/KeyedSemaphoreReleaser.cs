﻿using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace KeyedSemaphores
{
    /// <summary>
    ///     This class will be responsible for releasing a single consumer of a keyed semaphore
    /// </summary>
    public class KeyedSemaphoreReleaser<TKey> : IDisposable
    {
        private readonly KeyedSemaphoresCollection<TKey> _collection;
        private readonly KeyedSemaphore<TKey> _keyedSemaphore;
        /// <summary>
        /// To be sure to pass any exception from the time we got the lock to it's release
        /// </summary>
        internal Exception? Exception { get; set; }

        internal KeyedSemaphoreReleaser(
            KeyedSemaphoresCollection<TKey> collection,
            KeyedSemaphore<TKey> keyedSemaphore)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _keyedSemaphore = keyedSemaphore ?? throw new ArgumentNullException(nameof(keyedSemaphore));
        }

        /// <summary>
        ///     Releases and disposes of the inner <see cref="KeyedSemaphore{TKey}" />
        /// </summary>
        public void Dispose()
        {
            var key = _keyedSemaphore.Key;
            
            while (true)
            {
                if (!Monitor.TryEnter(_keyedSemaphore))
                {
                    continue;
                }
                
                var remainingConsumers = --_keyedSemaphore.Consumers;

                if (remainingConsumers == 0)
                {
                    _collection.Index.TryRemove(key, out _);
                }

                Monitor.Exit(_keyedSemaphore);

                break;
            }

            _keyedSemaphore.SemaphoreSlim.Release();
            if (Exception != null)
            {
                ExceptionDispatchInfo.Capture(Exception).Throw();
            }
        }
    }
}
