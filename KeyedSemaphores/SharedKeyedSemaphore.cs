using System;
using System.Threading;

namespace KeyedSemaphores
{
    /// <summary>
    /// Wraps an instance of <see cref="SemaphoreSlim"/> and is shared by possibly multiple keys, depending on their hash code and the capacity of the <see cref="KeyedSemaphoresCollection{TKey}"/>
    /// </summary>
    internal sealed class SharedKeyedSemaphore : IDisposable
    {
        internal readonly SemaphoreSlim _semaphore;

        public SharedKeyedSemaphore(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;                
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
