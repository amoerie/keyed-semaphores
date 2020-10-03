using System;
using System.Threading;

namespace KeyedSemaphores
{
    /// <summary>
    /// A semaphore that has a unique key. 
    /// </summary>
    public interface IKeyedSemaphore : IDisposable
    {
        /// <summary>
        /// The unique key of this semaphore 
        /// </summary>
        string Key { get; }
        
        /// <summary>
        /// The underlying <see cref="SemaphoreSlim"/> object that can be used to actually lock your C# thread
        /// </summary>
        SemaphoreSlim Semaphore { get; }

        internal int Consumers { get; }
        
        internal int IncreaseConsumers();
        
        internal int DecreaseConsumers();
    }
}