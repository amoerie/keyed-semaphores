using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace KeyedSemaphores
{
    /// <summary>
    /// Wraps an instance of <see cref="SemaphoreSlim"/> and is exclusive to each key
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    internal readonly struct RefCountedKeyedSemaphore<TKey>: IEquatable<RefCountedKeyedSemaphore<TKey>>, IDisposable where TKey : notnull
    {
        public readonly TKey _key;
        public readonly SemaphoreSlim _semaphore;
        public readonly SemaphoreReleaser _releaser;
        public readonly int _refs;

        public RefCountedKeyedSemaphore(TKey key, ConcurrentDictionary<TKey, RefCountedKeyedSemaphore<TKey>> dictionary)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _semaphore = new SemaphoreSlim(1, 1);
            _releaser = new SemaphoreReleaser(key, _semaphore, dictionary);
            _refs = 1;
        }

        public RefCountedKeyedSemaphore(TKey key, SemaphoreSlim semaphore, SemaphoreReleaser releaser, int refs)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            _releaser = releaser ?? throw new ArgumentNullException(nameof(releaser));
            _refs = refs;
        }

        public RefCountedKeyedSemaphore<TKey> IncrementRefs()
        {
            return new RefCountedKeyedSemaphore<TKey>(_key, _semaphore, _releaser, _refs + 1);
        }

        public RefCountedKeyedSemaphore<TKey> DecrementRefs()
        {
            return new RefCountedKeyedSemaphore<TKey>(_key, _semaphore, _releaser, _refs - 1);
        }

        public bool Equals(RefCountedKeyedSemaphore<TKey> other)
        {
            return EqualityComparer<TKey>.Default.Equals(_key, other._key)
                   && _semaphore.Equals(other._semaphore)
                   && _releaser.Equals(other._releaser)
                   && _refs == other._refs;
        }

        public override bool Equals(object? obj)
        {
            return obj is RefCountedKeyedSemaphore<TKey> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_key, _semaphore, _releaser, _refs);
        }

        public static bool operator ==(RefCountedKeyedSemaphore<TKey> left, RefCountedKeyedSemaphore<TKey> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RefCountedKeyedSemaphore<TKey> left, RefCountedKeyedSemaphore<TKey> right)
        {
            return !left.Equals(right);
        }

        internal sealed class SemaphoreReleaser : IDisposable
        {
            private readonly TKey _key;
            private readonly SemaphoreSlim _semaphoreSlim;
            private readonly ConcurrentDictionary<TKey, RefCountedKeyedSemaphore<TKey>> _keyedSemaphores;

            public SemaphoreReleaser(TKey key, SemaphoreSlim semaphoreSlim, ConcurrentDictionary<TKey, RefCountedKeyedSemaphore<TKey>> keyedSemaphores)
            {
                _key = key;
                _semaphoreSlim = semaphoreSlim;
                _keyedSemaphores = keyedSemaphores;
            }

            public void Dispose()
            {
                _semaphoreSlim.Release();

                while (true)
                {
                    if (!_keyedSemaphores.TryGetValue(_key, out var existingKeyedSemaphore))
                    {
                        throw new InvalidOperationException($"Did not expect the keyed semaphore with key {_key} to already be removed from the dictionary");
                    }
                    
                    if (existingKeyedSemaphore._refs == 1) 
                    {
                        // We're the last reference, so we can remove it from the dictionary
                        // The item will only be removed if both the key and value are a full match
                        // i.e. if the Refs has been changed by someone else before we get here, then the Remove operation will not do anything
                        var kvp = new KeyValuePair<TKey, RefCountedKeyedSemaphore<TKey>>(_key, existingKeyedSemaphore);

#if NET8_0_OR_GREATER
                        if(_keyedSemaphores.TryRemove(kvp))
                        {
                            existingKeyedSemaphore.Dispose();
                            return;
                        }
#else
                        if(((ICollection<KeyValuePair<TKey, RefCountedKeyedSemaphore<TKey>>>)_keyedSemaphores).Remove(kvp))
                        {
                            existingKeyedSemaphore.Dispose();
                            return;
                        }
#endif

                        // Someone incremented the value again before we could remove it. Jump back to the beginning of the while loop
                        continue;
                    }
                    
                    // More than 1 reference remains. Decrement the references and update the dictionary
                    var keyedSemaphore = existingKeyedSemaphore.DecrementRefs();
                    if (_keyedSemaphores.TryUpdate(_key, keyedSemaphore, existingKeyedSemaphore))
                    {
                        return;
                    }
                }
            }
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
