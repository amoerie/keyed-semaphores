using System;

namespace KeyedSemaphores
{
    internal interface IKeyedSemaphoreOwner<in TKey> where TKey: IEquatable<TKey>
    {
        void Return(IKeyedSemaphore<TKey> keyedSemaphore);
    }
}
