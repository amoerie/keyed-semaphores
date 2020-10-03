using System;

namespace KeyedSemaphores
{
    /// <summary>
    /// A exception that occured in the internals of KeyedSemaphores
    /// </summary>
    public class KeyedSemaphoresException : Exception
    {
        /// <inheritdoc cref="Exception"/>
        public KeyedSemaphoresException(string message) : base(message) { }
        
        /// <inheritdoc cref="Exception"/>
        public KeyedSemaphoresException(string message, Exception innerException) : base(message, innerException) { }
    }
}