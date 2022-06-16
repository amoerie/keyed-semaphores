using System;

namespace KeyedSemaphores
{
    internal interface IKeyedSemaphoreProvider<TKey> where TKey: IEquatable<TKey>
    {
        IKeyedSemaphore<TKey> Provide(TKey key);
    }
}
