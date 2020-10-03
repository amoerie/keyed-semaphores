using System;

namespace KeyedSemaphores
{
    /// <summary>
    /// Static API that provides allows getting or creating keyed semaphores
    /// </summary>
    public static class KeyedSemaphores
    {
        private static readonly Lazy<KeyedSemaphoresCollection> Collection = new Lazy<KeyedSemaphoresCollection>(() => new KeyedSemaphoresCollection());

        /// <summary>
        /// Gets or creates a keyed semaphore with the provided key. One key will always result in the same semaphore.
        /// </summary>
        /// <param name="key">The unique key of this keyed semaphore</param>
        /// <returns>
        /// An instance that can be used to lock your C# thread, which must be disposed when you are done.
        /// Once all parallel consumers of the keyed semaphore have disposed their keyed semaphore, it will be cleaned up.
        /// </returns>
        public static IKeyedSemaphore GetOrCreate(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Collection.Value.Provide(key);
        }
    }
}