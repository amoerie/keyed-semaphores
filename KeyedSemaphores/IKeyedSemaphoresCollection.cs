namespace KeyedSemaphores
{
    /// <summary>
    ///     A collection of keyed semaphores
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    public interface IKeyedSemaphoresCollection<TKey>
    {
        /// <summary>
        ///     Gets or creates a semaphore with the provided key
        /// </summary>
        /// <param name="key">The key of the semaphore</param>
        /// <returns>A new or existing <see cref="IKeyedSemaphore{TKey}" /></returns>
        IKeyedSemaphore<TKey> Provide(TKey key);

        /// <summary>
        ///     Returns a keyed semaphore to its collection, indicating it is no longer needed.
        ///     If all references to a keyed semaphore are returned, it is cleaned up
        /// </summary>
        /// <param name="keyedSemaphore">The keyed semaphore that is being returned</param>
        void Return(IKeyedSemaphore<TKey> keyedSemaphore);
    }
}