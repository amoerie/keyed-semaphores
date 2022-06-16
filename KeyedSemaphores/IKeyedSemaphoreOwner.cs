using System;

namespace KeyedSemaphores
{
    /// <summary>
    /// Represents the owner of a keyed semaphore. The owner is responsible for keeping track and disposing of keyed semaphores.
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public interface IKeyedSemaphoreOwner<in TKey> where TKey: IEquatable<TKey>
    {
        /// <summary>
        /// Returns a keyed semaphore to its owner, indicating it is no longer needed.
        /// If all references to a keyed semaphore are returned to their owner, it is cleaned up
        /// </summary>
        /// <param name="keyedSemaphore">The keyed semaphore that is being returned</param>
        void Return(IKeyedSemaphore<TKey> keyedSemaphore);
    }
}
