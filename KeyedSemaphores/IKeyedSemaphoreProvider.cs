using System;

namespace KeyedSemaphores
{
    /// <summary>
    /// A provider of keyed semaphores
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public interface IKeyedSemaphoreProvider<TKey> where TKey: IEquatable<TKey>
    {
        /// <summary>
        /// Gets or creates a semaphore with the provided key
        /// </summary>
        /// <param name="key">The key of the semaphore</param>
        /// <returns>A new or existing <see cref="IKeyedSemaphore{TKey}"/></returns>
        IKeyedSemaphore<TKey> Provide(TKey key);
    }
}
