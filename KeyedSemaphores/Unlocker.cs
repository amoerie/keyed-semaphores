using System;
using System.Threading;

namespace KeyedSemaphores
{
    internal readonly struct Unlocker : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public Unlocker(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;                
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
