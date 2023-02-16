using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace KeyedSemaphores.Tests;

public class TestForCancellationTokenAndTimeout
{
    [Fact]
    public void Lock_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledException()
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var cancelledCancellationToken = new CancellationToken(true);

        // Act
        var action = () =>
        {
            using var _ = collection.Lock("test", cancelledCancellationToken);
        };
        action.Should().Throw<OperationCanceledException>();
        
        // Assert
        collection.IsInUse("test").Should().BeFalse();
    }
    
    [Fact]
    public void Lock_WhenNotCancelled_ShouldReturnDisposable()
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var cancellationToken = default(CancellationToken);

        // Act
        var releaser = collection.Lock("test", cancellationToken);
        
        // Assert
        collection.IsInUse("test").Should().BeTrue();
        releaser.Dispose();
        collection.IsInUse("test").Should().BeFalse();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryLock_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledExceptionAndNotInvokeCallback(bool useShortTimeout)
    {
        // Arrange
        var isLockAcquired = false;
        var isCallbackInvoked = false;
        void Callback()
        {
            isCallbackInvoked = true;
        }
        var collection = new KeyedSemaphoresCollection<string>();
        var cancelledCancellationToken = new CancellationToken(true);
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));
        
        // Act
        var action = () => isLockAcquired = collection.TryLock("test", timeout, Callback, cancelledCancellationToken);
        action.Should().Throw<OperationCanceledException>();
        
        // Assert
        collection.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryLock_WhenNotCancelled_ShouldInvokeCallbackAndReturnDisposable(bool useShortTimeout)
    {
        // Arrange
        bool isCallbackInvoked = false;
        void Callback()
        {
            isCallbackInvoked = true;
        }
        var collection = new KeyedSemaphoresCollection<string>();
        var cancellationToken = default(CancellationToken);
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = collection.TryLock("test", timeout, Callback, cancellationToken);

        // Assert
        collection.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
    }
    
    [Fact]
    public async Task LockAsync_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledException()
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var cancelledCancellationToken = new CancellationToken(true);

        // Act
        var action = async () =>
        {
            using var _ = await collection.LockAsync("test", cancelledCancellationToken);
        };
        await action.Should().ThrowAsync<OperationCanceledException>();
        
        // Assert
        collection.IsInUse("test").Should().BeFalse();
    }
    
    [Fact]
    public async Task LockAsync_WhenNotCancelled_ShouldReturnDisposable()
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var cancellationToken = default(CancellationToken);

        // Act
        var releaser = await collection.LockAsync("test", cancellationToken);
        
        // Assert
        collection.IsInUse("test").Should().BeTrue();
        releaser.Dispose();
        collection.IsInUse("test").Should().BeFalse();    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithSynchronousCallback_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledExceptionAndNotInvokeCallback(bool useShortTimeout)
    {
        // Arrange
        bool isLockAcquired = false;
        bool isCallbackInvoked = false;
        void Callback()
        {
            isCallbackInvoked = true;
        }
        var collection = new KeyedSemaphoresCollection<string>();
        var cancelledCancellationToken = new CancellationToken(true);
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var action = async () => isLockAcquired = await collection.TryLockAsync("test", timeout, Callback, cancelledCancellationToken);
        await action.Should().ThrowAsync<OperationCanceledException>();
        
        // Assert
        collection.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithSynchronousCallback_WhenNotCancelled_ShouldInvokeCallbackAndReturnTrue(bool useShortTimeout)
    {
        // Arrange
        var isCallbackInvoked = false;
        void Callback()
        {
            isCallbackInvoked = true;
        }
        var collection = new KeyedSemaphoresCollection<string>();
        var cancellationToken = default(CancellationToken);
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = await collection.TryLockAsync("test", timeout, Callback, cancellationToken);

        // Assert
        collection.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithAsynchronousCallback_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledExceptionAndNotInvokeCallback(bool useShortTimeout)
    {
        // Arrange
        bool isLockAcquired = false;
        bool isCallbackInvoked = false;

        async Task Callback()
        {
            await Task.Delay(1);
            isCallbackInvoked = true;
        }
        var collection = new KeyedSemaphoresCollection<string>();
        var cancelledCancellationToken = new CancellationToken(true);
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var action = async () =>
        {
            isLockAcquired = await collection.TryLockAsync("test", timeout, Callback, cancelledCancellationToken);
        };
        await action.Should().ThrowAsync<OperationCanceledException>();
        
        // Assert
        collection.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithAsynchronousCallback_WhenNotCancelled_ShouldInvokeCallbackAndReturnTrue(bool useShortTimeout)
    {
        // Arrange
        var isCallbackInvoked = false;
        async Task Callback()
        {
            await Task.Delay(1);
            isCallbackInvoked = true;
        }
        var collection = new KeyedSemaphoresCollection<string>();
        var cancellationToken = default(CancellationToken);
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = await collection.TryLockAsync("test", timeout, Callback, cancellationToken);

        // Assert
        collection.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryLock_WhenTimedOut_ShouldNotInvokeCallbackAndReturnFalse(bool useShortTimeout)
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var key = "test";
        using var _  = collection.Lock(key);
        var isCallbackInvoked = false;
        void Callback()
        {
            isCallbackInvoked = true;
        }
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));
        
        // Act
        var isLockAcquired = collection.TryLock(key, timeout, Callback);
        
        // Assert
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
        collection.IsInUse(key).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryLock_WhenNotTimedOut_ShouldInvokeCallbackAndReturnTrue(bool useShortTimeout)
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var key = "test";
        var isCallbackInvoked = false;
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));
        void Callback()
        {
            isCallbackInvoked = true;
        }
        
        // Act
        var isLockAcquired = collection.TryLock(key, timeout, Callback);
        
        // Assert
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
        collection.IsInUse(key).Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WhenTimedOut_ShouldNotInvokeCallbackAndReturnFalse(bool useShortTimeout)
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var key = "test";
        using var _  = await collection.LockAsync(key);
        var isCallbackInvoked = false;
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));
        void Callback()
        {
            isCallbackInvoked = true;
        }
        
        // Act
        var isLockAcquired = await collection.TryLockAsync(key, timeout, Callback);
        
        // Assert
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
        collection.IsInUse(key).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WhenNotTimedOut_ShouldNotInvokeCallbackAndReturnFalse(bool useShortTimeout)
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var key = "test";
        var isCallbackInvoked = false;
        var timeout = useShortTimeout
            ? Constants.SynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.SynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));
        void Callback()
        {
            isCallbackInvoked = true;
        }
        
        // Act
        var isLockAcquired = await collection.TryLockAsync(key, timeout, Callback);
        
        // Assert
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
        collection.IsInUse(key).Should().BeFalse();
    }
}
