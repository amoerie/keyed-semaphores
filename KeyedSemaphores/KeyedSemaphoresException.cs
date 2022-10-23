using System;

namespace KeyedSemaphores
{
    /// <summary>
    ///     A exception that occured in the internals of KeyedSemaphores
    /// </summary>
    public class KeyedSemaphoresException : Exception
    {
        /// <inheritdoc cref="Exception" />
        public KeyedSemaphoresException(string message) : base(message)
        {
        }

        /// <inheritdoc cref="Exception" />
        public KeyedSemaphoresException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception trigger when the time to wait for the lock expires.
    /// </summary>
    public class TimeoutException : KeyedSemaphoresException
    {
        /// <summary>
        /// Time that was allotted to wait to get the lock
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <inheritdoc cref="Exception" />
        public TimeoutException(string message, TimeSpan timeout) : base(message)
        {
            Timeout = timeout;
        }
    }
}