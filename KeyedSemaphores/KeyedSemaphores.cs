using System;

namespace KeyedSemaphores
{
    public static class KeyedSemaphores
    {
        private static readonly Lazy<KeyedSemaphoresCollection> Collection = new Lazy<KeyedSemaphoresCollection>(() => new KeyedSemaphoresCollection());

        public static IKeyedSemaphore GetOrCreate(string key)
        {
            return Collection.Value.Provide(key);
        }
    }
}