using System;

namespace KeyedSemaphores
{
    /// <summary>
    /// Represents a locked keyed semaphore that has already acquired its inner semaphore.
    /// It is safe to perform any multi-threaded operations while in possession of this object.
    /// </summary>
    public class LockedKeyedSemaphore<TKey> : IDisposable where TKey : IEquatable<TKey>
    {
        private readonly IKeyedSemaphore<TKey> _keyedSemaphore;

        internal LockedKeyedSemaphore(IKeyedSemaphore<TKey> keyedSemaphore)
        {
            _keyedSemaphore = keyedSemaphore ?? throw new ArgumentNullException(nameof(keyedSemaphore));
        }

        /// <summary>
        /// Releases and disposes of the inner <see cref="IKeyedSemaphore{TKey}"/>
        /// </summary>
        public void Dispose()
        {
            _keyedSemaphore.Release();
            _keyedSemaphore.Dispose();
        }
    }
}
