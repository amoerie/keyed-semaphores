using System;
using System.Threading;

namespace KeyedSemaphores
{
    public interface IKeyedSemaphore : IDisposable
    {
        string Key { get; }
        
        SemaphoreSlim Semaphore { get; }

        internal int Consumers { get; }
        
        internal int IncreaseConsumers();
        
        internal int DecreaseConsumers();
    }
}