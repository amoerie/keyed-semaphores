using System;

namespace KeyedSemaphores
{
    public class KeyedSemaphoresException : Exception
    {
        public KeyedSemaphoresException(string message) : base(message) { }
        public KeyedSemaphoresException(string message, Exception innerException) : base(message, innerException) { }
    }
}