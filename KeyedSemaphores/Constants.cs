using System;

namespace KeyedSemaphores
{
    internal static class Constants
    {
        public const int DefaultNumberOfSemaphores = 4096;
        
        public static readonly TimeSpan DefaultSynchronousWaitDuration = TimeSpan.FromMilliseconds(10);
    }
}
