using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace KeyedSemaphores.Tests;

public class TestsForKeyedSemaphoresDictionary
{
    [Fact]
    public async Task ThreeDifferentLocksShouldWork()
    {
        // Arrange
        var keyedSemaphores = new KeyedSemaphoresDictionary<int>();

        // Act
        using var _1 = await keyedSemaphores.LockAsync(1);
        using var _2 = await keyedSemaphores.LockAsync(2);
        using var _3 = await keyedSemaphores.LockAsync(3);

        // Assert
        _1.Should().NotBeNull();
        _2.Should().NotBeNull();
        _3.Should().NotBeNull();
    }

    [Fact]
    public async Task ThreeIdenticalLocksShouldWork()
    {
        // Arrange
        var keyedSemaphores = new KeyedSemaphoresDictionary<int>();

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
        t1.Should().NotBeNull();
        t2.Should().NotBeNull();
        t3.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldRunThreadsWithDistinctKeysInParallel()
    {
        // Arrange
        var currentParallelism = 0;
        var maxParallelism = 0;
        var parallelismLock = new object();
        var keyedSemaphores = new KeyedSemaphoresDictionary<int>();

        // 100 threads, 100 keys
        var threads = Enumerable.Range(0, 100)
            .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i).ConfigureAwait(false)))
            .ToList();

        // Act
        await Task.WhenAll(threads).ConfigureAwait(false);

        maxParallelism.Should().BeGreaterThan(10);
        foreach (var key in Enumerable.Range(0, 100))
        {
            keyedSemaphores.IsInUse(key).Should().BeFalse();
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


                await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);

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
        var keyedSemaphores = new KeyedSemaphoresDictionary<int>();

        // 100 threads, 10 keys
        var threads = Enumerable.Range(0, 100)
            .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i % 10).ConfigureAwait(false)))
            .ToList();

        // Act + Assert
        await Task.WhenAll(threads).ConfigureAwait(false);

        maxParallelism.Should().BeLessOrEqualTo(10);
        foreach (var key in Enumerable.Range(0, 100))
        {
            keyedSemaphores.IsInUse(key % 10).Should().BeFalse();
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

                await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);

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
        var keyedSemaphores = new KeyedSemaphoresDictionary<int>();

        // Many threads, 1 key
        var threads = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(async () => await OccupyTheLockALittleBit(1).ConfigureAwait(false)))
            .ToList();

        // Act + Assert
        await Task.WhenAll(threads).ConfigureAwait(false);

        maxParallelism.Should().Be(1);
        keyedSemaphores.IsInUse(1).Should().BeFalse();


        async Task OccupyTheLockALittleBit(int key)
        {
            var currentTaskId = Task.CurrentId ?? -1;
            var delay = random.Next(500);

            await Task.Delay(delay).ConfigureAwait(false);

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
        var keyedSemaphores = new KeyedSemaphoresDictionary<int>();

        // 100 threads, 100 keys
        var threads = Enumerable.Range(0, 100)
            .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i).ConfigureAwait(false)))
            .ToList();

        // Act
        await Task.WhenAll(threads).ConfigureAwait(false);

        maxParallelism.Should().BeGreaterThan(10);
        foreach (var key in Enumerable.Range(0, 100))
        {
            keyedSemaphores.IsInUse(key).Should().BeFalse();
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

                await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);

                Interlocked.Decrement(ref currentParallelism);
            }
        }
    }

    [Fact]
    public async Task IsInUseShouldReturnTrueWhenLockedAndFalseWhenNotLocked()
    {
        // Arrange
        var keyedSemaphores = new KeyedSemaphoresDictionary<int>();

        // 10 threads, 10 keys
        var threads = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i).ConfigureAwait(false)))
            .ToList();

        // Act
        await Task.WhenAll(threads).ConfigureAwait(false);
        foreach (var key in Enumerable.Range(0, 10))
        {
            keyedSemaphores.IsInUse(key).Should().BeFalse();
        }

        async Task OccupyTheLockALittleBit(int key)
        {
            keyedSemaphores.IsInUse(key).Should().BeFalse();

            using (await keyedSemaphores.LockAsync(key))
            {
                const int delay = 250;

                await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);

                keyedSemaphores.IsInUse(key).Should().BeTrue();
            }

            keyedSemaphores.IsInUse(key).Should().BeFalse();
        }
    }

    [Fact]
    public void Lock_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledException()
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancelledCancellationToken = new CancellationToken(true);

        // Act
        Action action = () =>
        {
            using var _ = dictionary.Lock("test", cancelledCancellationToken);
        };
        action.Should().Throw<OperationCanceledException>();

        // Assert
        dictionary.IsInUse("test").Should().BeFalse();
    }

    [Fact]
    public void Lock_WhenNotCancelled_ShouldReturnDisposable()
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancellationToken = default(CancellationToken);

        // Act
        var releaser = dictionary.Lock("test", cancellationToken);

        // Assert
        dictionary.IsInUse("test").Should().BeTrue();
        releaser.Dispose();
        dictionary.IsInUse("test").Should().BeFalse();
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

        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancelledCancellationToken = new CancellationToken(true);
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        Action action = () =>
            isLockAcquired = dictionary.TryLock("test", timeout, Callback, cancelledCancellationToken);
        action.Should().Throw<OperationCanceledException>();

        // Assert
        dictionary.IsInUse("test").Should().BeFalse();
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

        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancellationToken = default(CancellationToken);
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = dictionary.TryLock("test", timeout, Callback, cancellationToken);

        // Assert
        dictionary.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task LockAsync_WhenCancelled_ShouldReleaseKeyedSemaphoreAndThrowOperationCanceledException()
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancelledCancellationToken = new CancellationToken(true);

        // Act
        Func<Task> action = async () =>
        {
            using var _ = await dictionary.LockAsync("test", cancelledCancellationToken);
        };
        await action.Should().ThrowAsync<OperationCanceledException>();

        // Assert
        dictionary.IsInUse("test").Should().BeFalse();
    }

    [Fact]
    public async Task LockAsync_WhenNotCancelled_ShouldReturnDisposable()
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancellationToken = default(CancellationToken);

        // Act
        var releaser = await dictionary.LockAsync("test", cancellationToken);

        // Assert
        dictionary.IsInUse("test").Should().BeTrue();
        releaser.Dispose();
        dictionary.IsInUse("test").Should().BeFalse();
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

        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancelledCancellationToken = new CancellationToken(true);
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        Func<Task> action = async () =>
            isLockAcquired = await dictionary.TryLockAsync("test", timeout, Callback, cancelledCancellationToken);
        await action.Should().ThrowAsync<OperationCanceledException>();

        // Assert
        dictionary.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
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

        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancellationToken = default(CancellationToken);
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = await dictionary.TryLockAsync("test", timeout, Callback, cancellationToken);

        // Assert
        dictionary.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
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

        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancelledCancellationToken = new CancellationToken(true);
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        Func<Task> action = async () =>
        {
            isLockAcquired =
                await dictionary.TryLockAsync("test", timeout, Callback, cancelledCancellationToken);
        };
        await action.Should().ThrowAsync<OperationCanceledException>();

        // Assert
        dictionary.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
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

        var dictionary = new KeyedSemaphoresDictionary<string>();
        var cancellationToken = default(CancellationToken);
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = await dictionary.TryLockAsync("test", timeout, Callback, cancellationToken);

        // Assert
        dictionary.IsInUse("test").Should().BeFalse();
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryLock_WhenTimedOut_ShouldNotInvokeCallbackAndReturnFalse(bool useShortTimeout)
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var key = "test";
        using var _ = dictionary.Lock(key);
        var isCallbackInvoked = false;

        void Callback()
        {
            isCallbackInvoked = true;
        }

        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        // Act
        var isLockAcquired = dictionary.TryLock(key, timeout, Callback);

        // Assert
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
        dictionary.IsInUse(key).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryLock_WhenNotTimedOut_ShouldInvokeCallbackAndReturnTrue(bool useShortTimeout)
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
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
        var isLockAcquired = dictionary.TryLock(key, timeout, Callback);

        // Assert
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
        dictionary.IsInUse(key).Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WhenTimedOut_ShouldNotInvokeCallbackAndReturnFalse(bool useShortTimeout)
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
        var key = "test";
        using var _ = await dictionary.LockAsync(key);
        var isCallbackInvoked = false;
        var timeout = useShortTimeout
            ? Constants.DefaultSynchronousWaitDuration.Subtract(TimeSpan.FromMilliseconds(1))
            : Constants.DefaultSynchronousWaitDuration.Add(TimeSpan.FromMilliseconds(1));

        void Callback()
        {
            isCallbackInvoked = true;
        }

        // Act
        var isLockAcquired = await dictionary.TryLockAsync(key, timeout, Callback);

        // Assert
        isLockAcquired.Should().BeFalse();
        isCallbackInvoked.Should().BeFalse();
        dictionary.IsInUse(key).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryLockAsync_WhenNotTimedOut_ShouldNotInvokeCallbackAndReturnFalse(bool useShortTimeout)
    {
        // Arrange
        var dictionary = new KeyedSemaphoresDictionary<string>();
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
        var isLockAcquired = await dictionary.TryLockAsync(key, timeout, Callback);

        // Assert
        isLockAcquired.Should().BeTrue();
        isCallbackInvoked.Should().BeTrue();
        dictionary.IsInUse(key).Should().BeFalse();
    }
}
