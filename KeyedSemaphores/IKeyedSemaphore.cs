using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    /// <summary>
    /// A wrapper around <see cref="SemaphoreSlim"/> that has a unique key. 
    /// </summary>
    public interface IKeyedSemaphore<out TKey> : IDisposable where TKey: IEquatable<TKey>
    {
        /// <summary>
        /// The unique key of this semaphore 
        /// </summary>
        TKey Key { get; }

        /// <summary>Asynchronously waits to enter the inner <see cref="T:System.Threading.SemaphoreSlim"></see>.</summary>
        /// <returns>A task that will complete when the semaphore has been entered.</returns>
        Task WaitAsync();
        
        /// <summary>Asynchronously waits to enter the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, using a <see cref="T:System.TimeSpan"></see> to measure the time interval.</summary>
        /// <param name="timeout">A <see cref="T:System.TimeSpan"></see> that represents the number of milliseconds to wait, a <see cref="T:System.TimeSpan"></see> that represents -1 milliseconds to wait indefinitely, or a <see cref="T:System.TimeSpan"></see> that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <returns>A task that will complete with a result of true if the current thread successfully entered the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, otherwise with a result of false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout">timeout</paramref> is a negative number other than -1, which represents an infinite timeout -or- timeout is greater than <see cref="F:System.Int32.MaxValue"></see>.</exception>
        Task<bool> WaitAsync(TimeSpan timeout);
        
        /// <summary>Asynchronously waits to enter the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, using a <see cref="T:System.TimeSpan"></see> to measure the time interval, while observing a <see cref="T:System.Threading.CancellationToken"></see>.</summary>
        /// <param name="timeout">A <see cref="T:System.TimeSpan"></see> that represents the number of milliseconds to wait, a <see cref="T:System.TimeSpan"></see> that represents -1 milliseconds to wait indefinitely, or a <see cref="T:System.TimeSpan"></see> that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"></see> token to observe.</param>
        /// <returns>A task that will complete with a result of true if the current thread successfully entered the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, otherwise with a result of false.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout">millisecondsTimeout</paramref> is a negative number other than -1, which represents an infinite timeout -or- timeout is greater than <see cref="F:System.Int32.MaxValue"></see>.</exception>
        /// <exception cref="T:System.OperationCanceledException"><paramref name="cancellationToken">cancellationToken</paramref> was canceled.</exception>
        Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
        
        /// <summary>Asynchronously waits to enter the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, while observing a <see cref="T:System.Threading.CancellationToken"></see>.</summary>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"></see> token to observe.</param>
        /// <returns>A task that will complete when the semaphore has been entered.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="T:System.OperationCanceledException"><paramref name="cancellationToken">cancellationToken</paramref> was canceled.</exception>
        Task WaitAsync(CancellationToken cancellationToken);

        /// <summary>Blocks the current thread to enter the inner <see cref="T:System.Threading.SemaphoreSlim"></see>.</summary>
        void Wait();

        /// <summary>Blocks the current thread to enter the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, using a <see cref="T:System.TimeSpan"></see> to measure the time interval.</summary>
        /// <param name="timeout">A <see cref="T:System.TimeSpan"></see> that represents the number of milliseconds to wait, a <see cref="T:System.TimeSpan"></see> that represents -1 milliseconds to wait indefinitely, or a <see cref="T:System.TimeSpan"></see> that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <returns>True if the current thread successfully entered the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, otherwise with a result of false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout">timeout</paramref> is a negative number other than -1, which represents an infinite timeout -or- timeout is greater than <see cref="F:System.Int32.MaxValue"></see>.</exception>
        bool Wait(TimeSpan timeout);

        /// <summary>Blocks the current thread to enter the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, using a <see cref="T:System.TimeSpan"></see> to measure the time interval, while observing a <see cref="T:System.Threading.CancellationToken"></see>.</summary>
        /// <param name="timeout">A <see cref="T:System.TimeSpan"></see> that represents the number of milliseconds to wait, a <see cref="T:System.TimeSpan"></see> that represents -1 milliseconds to wait indefinitely, or a <see cref="T:System.TimeSpan"></see> that represents 0 milliseconds to test the wait handle and return immediately.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"></see> token to observe.</param>
        /// <returns>True if the current thread successfully entered the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, otherwise with a result of false.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout">millisecondsTimeout</paramref> is a negative number other than -1, which represents an infinite timeout -or- timeout is greater than <see cref="F:System.Int32.MaxValue"></see>.</exception>
        /// <exception cref="T:System.OperationCanceledException"><paramref name="cancellationToken">cancellationToken</paramref> was canceled.</exception>
        bool Wait(TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>Blocks the current thread to enter the inner <see cref="T:System.Threading.SemaphoreSlim"></see>, while observing a <see cref="T:System.Threading.CancellationToken"></see>.</summary>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken"></see> token to observe.</param>
        /// <returns>A task that will complete when the semaphore has been entered.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="T:System.OperationCanceledException"><paramref name="cancellationToken">cancellationToken</paramref> was canceled.</exception>
        void Wait(CancellationToken cancellationToken);

        /// <summary>Releases the inner <see cref="T:System.Threading.SemaphoreSlim"></see> object once.</summary>
        /// <exception cref="T:System.ObjectDisposedException">The current instance has already been disposed.</exception>
        /// <exception cref="T:System.Threading.SemaphoreFullException">The inner <see cref="T:System.Threading.SemaphoreSlim"></see> has already reached its maximum size.</exception>
        void Release();
        
        internal int Consumers { get; }
        
        internal int IncreaseConsumers();
        
        internal int DecreaseConsumers();

        internal void InternalDispose();
    }
}
