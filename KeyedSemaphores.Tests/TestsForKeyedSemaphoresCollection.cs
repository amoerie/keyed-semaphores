using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KeyedSemaphores.Tests;

public class TestsForKeyedSemaphoresCollection
{
    [Fact]
    public async Task ThreeDifferentLocksShouldWork()
    {
        // Arrange
        var keyedSemaphores = new KeyedSemaphoresCollection<int>();

        // Act
        using var _1 = await keyedSemaphores.LockAsync(1);
        using var _2 = await keyedSemaphores.LockAsync(2);
        using var _3 = await keyedSemaphores.LockAsync(3);

        // Assert
        Assert.NotNull(_1);
        Assert.NotNull(_2);
        Assert.NotNull(_3);
    }

    [Fact]
    public async Task ThreeIdenticalLocksShouldWork()
    {
        // Arrange
        var keyedSemaphores = new KeyedSemaphoresCollection<int>();

        // Act
        var t1 = Task.Run(async () =>
        {
            using var _ = await keyedSemaphores.LockAsync(1);
        });
        var t2 = Task.Run(async () =>
        {
            using var _ = await keyedSemaphores.LockAsync(1);
        });
        var t3 = Task.Run(async () =>
        {
            using var _ = await keyedSemaphores.LockAsync(1);
        });
        await t1;
        await t2;
        await t3;

        // Assert
        Assert.NotNull(t1);
        Assert.NotNull(t2);
        Assert.NotNull(t3);
    }

    [Fact]
    public async Task ShouldRunThreadsWithDistinctKeysInParallel()
    {
        // Arrange
        var currentParallelism = 0;
        var maxParallelism = 0;
        var parallelismLock = new object();
        var keyedSemaphores = new KeyedSemaphoresCollection<int>();

        // 100 threads, 100 keys
        var threads = Enumerable.Range(0, 100)
            .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i)))
            .ToList();

        // Act
        await Task.WhenAll(threads);

        Assert.True((maxParallelism) > (10));
        foreach (var key in Enumerable.Range(0, 100))
        {
            Assert.False(keyedSemaphores.IsInUse(key));
        }

        async Task OccupyTheLockALittleBit(int key)
        {
            using (await keyedSemaphores.LockAsync(key))
            {
                var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);

                lock (parallelismLock)
                {
                    maxParallelism = Math.Max(incrementedCurrentParallelism, maxParallelism);
                }

                const int delay = 250;


                await Task.Delay(TimeSpan.FromMilliseconds(delay));

                Interlocked.Decrement(ref currentParallelism);
            }
        }
    }

    [Fact]
    public async Task ShouldRunThreadsWithSameKeysLinearly()
    {
        // Arrange
        var runningTasksIndex = new ConcurrentDictionary<int, int>();
        var parallelismLock = new object();
        var currentParallelism = 0;
        var maxParallelism = 0;
        var keyedSemaphores = new KeyedSemaphoresCollection<int>();

        // 100 threads, 10 keys
        var threads = Enumerable.Range(0, 100)
            .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i % 10)))
            .ToList();

        // Act + Assert
        await Task.WhenAll(threads);

        Assert.True(maxParallelism <= 10);
        
        foreach (var key in Enumerable.Range(0, 100))
        {
            Assert.False(keyedSemaphores.IsInUse(key % 10));
        }

        async Task OccupyTheLockALittleBit(int key)
        {
            using (await keyedSemaphores.LockAsync(key))
            {
                var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);

                lock (parallelismLock)
                {
                    maxParallelism = Math.Max(incrementedCurrentParallelism, maxParallelism);
                }

                var currentTaskId = Task.CurrentId ?? -1;
                if (runningTasksIndex.TryGetValue(key, out var otherThread))
                    throw new Exception($"Thread #{currentTaskId} acquired a lock using key ${key} " +
                                        $"but another thread #{otherThread} is also still running using this key!");

                runningTasksIndex[key] = currentTaskId;

                const int delay = 10;

                await Task.Delay(TimeSpan.FromMilliseconds(delay));

                if (!runningTasksIndex.TryRemove(key, out var value))
                {
                    var ex = new Exception($"Thread #{currentTaskId} has finished " +
                                           "but when trying to cleanup the running threads index, the value is already gone");

                    throw ex;
                }

                if (value != currentTaskId)
                {
                    var ex = new Exception(
                        $"Thread #{currentTaskId} has finished and has removed itself from the running threads index," +
                        $" but that index contained an incorrect value: #{value}!");

                    throw ex;
                }

                Interlocked.Decrement(ref currentParallelism);
            }
        }
    }

    [Fact]
    public async Task ShouldNeverCreateTwoSemaphoresForTheSameKey()
    {
        // Arrange
        var runningTasksIndex = new ConcurrentDictionary<int, int>();
        var parallelismLock = new object();
        var currentParallelism = 0;
        var maxParallelism = 0;
        var random = new Random();
        var keyedSemaphores = new KeyedSemaphoresCollection<int>();

        // Many threads, 1 key
        var threads = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(async () => await OccupyTheLockALittleBit(1)))
            .ToList();

        // Act + Assert
        await Task.WhenAll(threads);

        Assert.Equal(1, maxParallelism);
        Assert.False(keyedSemaphores.IsInUse(1));


        async Task OccupyTheLockALittleBit(int key)
        {
            var currentTaskId = Task.CurrentId ?? -1;
            var delay = random.Next(500);

            await Task.Delay(delay);

            using (await keyedSemaphores.LockAsync(key))
            {
                var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);

                lock (parallelismLock)
                {
                    maxParallelism = Math.Max(incrementedCurrentParallelism, maxParallelism);
                }

                if (runningTasksIndex.TryGetValue(key, out var otherThread))
                    throw new Exception($"Task [{currentTaskId,3}] has a lock for key ${key} " +
                                        $"but another task [{otherThread,3}] also has an active lock for this key!");

                runningTasksIndex[key] = currentTaskId;

                if (!runningTasksIndex.TryRemove(key, out var value))
                {
                    var ex = new Exception($"Task [{currentTaskId,3}] has finished " +
                                           "but when trying to cleanup the running tasks index, the value is already gone");

                    throw ex;
                }

                if (value != currentTaskId)
                {
                    var ex = new Exception(
                        $"Task [{currentTaskId,3}] has finished and has removed itself from the running tasks index," +
                        $" but that index contained a task ID of another task: [{value}]!");

                    throw ex;
                }

                Interlocked.Decrement(ref currentParallelism);
            }
        }
    }

    [Fact]
    public async Task ShouldRunThreadsWithDistinctStringKeysInParallel()
    {
        // Arrange
        var currentParallelism = 0;
        var maxParallelism = 0;
        var parallelismLock = new object();
        var keyedSemaphores = new KeyedSemaphoresCollection<int>();

        // 100 threads, 100 keys
        var threads = Enumerable.Range(0, 100)
            .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i)))
            .ToList();

        // Act
        await Task.WhenAll(threads);

        Assert.True((maxParallelism) > (10));
        foreach (var key in Enumerable.Range(0, 100))
        {
            Assert.False(keyedSemaphores.IsInUse(key));
        }

        async Task OccupyTheLockALittleBit(int key)
        {
            using (await keyedSemaphores.LockAsync(key))
            {
                var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);

                lock (parallelismLock)
                {
                    maxParallelism = Math.Max(incrementedCurrentParallelism, maxParallelism);
                }

                const int delay = 250;

                await Task.Delay(TimeSpan.FromMilliseconds(delay));

                Interlocked.Decrement(ref currentParallelism);
            }
        }
    }

    [Fact]
    public async Task IsInUseShouldReturnTrueWhenLockedAndFalseWhenNotLocked()
    {
        // Arrange
        var keyedSemaphores = new KeyedSemaphoresCollection<int>();

        // 10 threads, 10 keys
        var threads = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i)))
            .ToList();

        // Act
        await Task.WhenAll(threads);
        foreach (var key in Enumerable.Range(0, 10))
        {
            Assert.False(keyedSemaphores.IsInUse(key));
        }

        async Task OccupyTheLockALittleBit(int key)
        {
            Assert.False(keyedSemaphores.IsInUse(key));

            using (await keyedSemaphores.LockAsync(key))
            {
                const int delay = 250;

                await Task.Delay(TimeSpan.FromMilliseconds(delay));

                Assert.True(keyedSemaphores.IsInUse(key));
            }

            Assert.False(keyedSemaphores.IsInUse(key));
        }
    }

    [Fact]
    public void Lock_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledException()
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var cancelledCancellationToken = new CancellationToken(true);

        // Act
        Action action = () =>
        {
            using var _ = collection.Lock("test", cancelledCancellationToken);
        };
        Assert.Throws<OperationCanceledException>(action);

        // Assert
        Assert.False(collection.IsInUse("test"));
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
        Assert.True(collection.IsInUse("test"));
        releaser.Dispose();
        Assert.False(collection.IsInUse("test"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void
        TryLock_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledExceptionAndNotInvokeCallback(
            bool useShortTimeout)
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
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        Action action = () =>
            isLockAcquired = collection.TryLock("test", timeout, Callback, cancelledCancellationToken);
        Assert.Throws<OperationCanceledException>(action);

        // Assert
        Assert.False(collection.IsInUse("test"));
        Assert.False(isLockAcquired);
        Assert.False(isCallbackInvoked);
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
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = collection.TryLock("test", timeout, Callback, cancellationToken);

        // Assert
        Assert.False(collection.IsInUse("test"));
        Assert.True(isLockAcquired);
        Assert.True(isCallbackInvoked);
    }

    [Fact]
    public async Task LockAsync_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledException()
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var cancelledCancellationToken = new CancellationToken(true);

        // Act
        Func<Task> action = async () =>
        {
            using var _ = await collection.LockAsync("test", cancelledCancellationToken);
        };
        await Assert.ThrowsAsync<OperationCanceledException>(action);

        // Assert
        Assert.False(collection.IsInUse("test"));
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
        Assert.True(collection.IsInUse("test"));
        releaser.Dispose();
        Assert.False(collection.IsInUse("test"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task
        TryLockAsync_WithSynchronousCallback_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledExceptionAndNotInvokeCallback(
            bool useShortTimeout)
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
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        Func<Task> action = async () =>
            isLockAcquired = await collection.TryLockAsync("test", timeout, Callback, cancelledCancellationToken);
        await Assert.ThrowsAsync<OperationCanceledException>(action);

        // Assert
        Assert.False(collection.IsInUse("test"));
        Assert.False(isLockAcquired);
        Assert.False(isCallbackInvoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithSynchronousCallback_WhenNotCancelled_ShouldInvokeCallbackAndReturnTrue(
        bool useShortTimeout)
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
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = await collection.TryLockAsync("test", timeout, Callback, cancellationToken);

        // Assert
        Assert.False(collection.IsInUse("test"));
        Assert.True(isLockAcquired);
        Assert.True(isCallbackInvoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task
        TryLockAsync_WithAsynchronousCallback_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledExceptionAndNotInvokeCallback(
            bool useShortTimeout)
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
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        Func<Task> action = async () =>
        {
            isLockAcquired =
                await collection.TryLockAsync("test", timeout, Callback, cancelledCancellationToken);
        };
        await Assert.ThrowsAsync<OperationCanceledException>(action);

        // Assert
        Assert.False(collection.IsInUse("test"));
        Assert.False(isLockAcquired);
        Assert.False(isCallbackInvoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithAsynchronousCallback_WhenNotCancelled_ShouldInvokeCallbackAndReturnTrue(
        bool useShortTimeout)
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
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = await collection.TryLockAsync("test", timeout, Callback, cancellationToken);

        // Assert
        Assert.False(collection.IsInUse("test"));
        Assert.True(isLockAcquired);
        Assert.True(isCallbackInvoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryLock_WhenTimedOut_ShouldNotInvokeCallbackAndReturnFalse(bool useShortTimeout)
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var key = "test";
        using var _ = collection.Lock(key);
        var isCallbackInvoked = false;

        void Callback()
        {
            isCallbackInvoked = true;
        }

        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = collection.TryLock(key, timeout, Callback);

        // Assert
        Assert.False(isLockAcquired);
        Assert.False(isCallbackInvoked);
        Assert.True(collection.IsInUse(key));
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
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        void Callback()
        {
            isCallbackInvoked = true;
        }

        // Act
        var isLockAcquired = collection.TryLock(key, timeout, Callback);

        // Assert
        Assert.True(isLockAcquired);
        Assert.True(isCallbackInvoked);
        Assert.False(collection.IsInUse(key));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WhenTimedOut_ShouldNotInvokeCallbackAndReturnFalse(bool useShortTimeout)
    {
        // Arrange
        var collection = new KeyedSemaphoresCollection<string>();
        var key = "test";
        using var _ = await collection.LockAsync(key);
        var isCallbackInvoked = false;
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        void Callback()
        {
            isCallbackInvoked = true;
        }

        // Act
        var isLockAcquired = await collection.TryLockAsync(key, timeout, Callback);

        // Assert
        Assert.False(isLockAcquired);
        Assert.False(isCallbackInvoked);
        Assert.True(collection.IsInUse(key));
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
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        void Callback()
        {
            isCallbackInvoked = true;
        }

        // Act
        var isLockAcquired = await collection.TryLockAsync(key, timeout, Callback);

        // Assert
        Assert.True(isLockAcquired);
        Assert.True(isCallbackInvoked);
        Assert.False(collection.IsInUse(key));
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithoutCallback_ShouldReturnDisposable(bool useShortTimeout)
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var key = "test";
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        using (var lockScope = await dictionary.TryLockAsync(key, timeout))
        {
            // Assert
            Assert.NotNull(lockScope);
            Assert.True(dictionary.IsInUse(key));
        }

        Assert.False(dictionary.IsInUse(key));
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithoutCallback_ShouldBlockConflictingTryLockAsync(bool useShortTimeout)
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var key = "test";
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        using var lockScopeOne = await dictionary.TryLockAsync(key, timeout);
        using var lockScopeTwo = await dictionary.TryLockAsync(key, timeout);
        
        // Assert
        Assert.NotNull(lockScopeOne);
        Assert.Null(lockScopeTwo);
        Assert.True(dictionary.IsInUse(key));
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WithoutCallback_ShouldBlockConflictingTryLockAsync_UntilDisposed(bool useShortTimeout)
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var key = "test";
        var jobComplete = false;
        var jobEntered = false;
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        async Task<bool> Job()
        {
            jobEntered = true;

            using var _ = await dictionary.TryLockAsync(key, TimeSpan.FromDays(1));
            
            jobComplete = true;

            return true;
        }

        // Act
        var lockScopeOne = await dictionary.TryLockAsync(key, timeout);
        var callbackTask = Job();
        
        // Assert
        Assert.NotNull(lockScopeOne);
        Assert.True(jobEntered);
        Assert.False(jobComplete);
        Assert.True(dictionary.IsInUse(key));
        
        lockScopeOne!.Dispose(); // Release the lock to allow the callback to proceed

        Assert.True((await callbackTask));
        Assert.False(dictionary.IsInUse(key));
        Assert.True(jobComplete);
    }
}
