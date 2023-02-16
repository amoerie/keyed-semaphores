using System;
using System.Threading;

namespace KeyedSemaphores
{
    internal sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;                
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
