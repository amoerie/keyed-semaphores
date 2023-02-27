using System;
using System.Threading;

namespace KeyedSemaphores
{
    internal sealed class Releaser : IDisposable
    {
        internal readonly SemaphoreSlim Semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            Semaphore = semaphore;                
        }

        public void Dispose()
        {
            Semaphore.Release();
        }
    }
}
