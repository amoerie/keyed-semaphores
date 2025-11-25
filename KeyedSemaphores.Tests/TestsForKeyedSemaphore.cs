using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace KeyedSemaphores.Tests;

public class TestsForKeyedSemaphore(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));

    public class Async : TestsForKeyedSemaphore
    {
        public Async(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(100, 100, 10, 100)]
        [InlineData(100, 10, 2, 10)]
        [InlineData(100, 50, 5, 50)]
        [InlineData(100, 1, 1, 1)]
        public async Task ShouldApplyParallelismCorrectly(int numberOfThreads, int numberOfKeys, int minParallelism,
            int maxParallelism)
        {
            // Arrange
            var runningTasksIndex = new ConcurrentDictionary<int, int>();
            var parallelismLock = new object();
            var currentParallelism = 0;
            var peakParallelism = 0;

            var threads = Enumerable.Range(0, numberOfThreads)
                .Select(i =>
                    Task.Run(async () => await OccupyTheLockALittleBit(i % numberOfKeys)))
                .ToList();

            // Act + Assert
            await Task.WhenAll(threads);
            
            Assert.True(peakParallelism <= maxParallelism);
            Assert.True(peakParallelism >= minParallelism);

            _output.WriteLine("Peak parallelism was " + peakParallelism);

            async Task OccupyTheLockALittleBit(int key)
            {
                using (await KeyedSemaphore.LockAsync(key.ToString()))
                {
                    var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);

                    lock (parallelismLock)
                    {
                        peakParallelism = Math.Max(incrementedCurrentParallelism, peakParallelism);
                    }

                    var currentTaskId = Task.CurrentId ?? -1;

                    if (!runningTasksIndex.TryAdd(key, currentTaskId))
                    {
                        throw new InvalidOperationException(
                            $"Task #{currentTaskId} acquired a lock using key ${key} but another thread is also still running using this key!");
                    }

                    const int delay = 10;

                    await Task.Delay(delay);

                    if (!runningTasksIndex.TryRemove(key, out var value))
                    {
                        throw new InvalidOperationException($"Task #{currentTaskId} has just finished " +
                                                            $"but the running tasks index does not contain an entry for key {key}");
                    }

                    if (value != currentTaskId)
                    {
                        var ex = new InvalidOperationException($"Task #{currentTaskId} has just finished " +
                                                               $"but the running threads index has linked task #{value} to key {key}!");

                        throw ex;
                    }

                    Interlocked.Decrement(ref currentParallelism);
                }
            }
        }
    }

    public class Sync : TestsForKeyedSemaphore
    {
        public Sync(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(100, 100, 10, 100)]
        [InlineData(100, 10, 2, 10)]
        [InlineData(100, 50, 5, 50)]
        [InlineData(100, 1, 1, 1)]
        public void ShouldApplyParallelismCorrectly(int numberOfThreads, int numberOfKeys, int minParallelism,
            int maxParallelism)
        {
            // Arrange
            var currentParallelism = 0;
            var peakParallelism = 0;
            var parallelismLock = new object();
            var runningThreadsIndex = new ConcurrentDictionary<int, int>();

            var threads = Enumerable.Range(0, numberOfThreads)
                .Select(i => new Thread(() => OccupyTheLockALittleBit(i % numberOfKeys)))
                .ToList();

            // Act
            foreach (var thread in threads) thread.Start();

            foreach (var thread in threads) thread.Join();

            Assert.True(peakParallelism >= minParallelism);
            Assert.True(peakParallelism <= maxParallelism);

            _output.WriteLine("Peak parallelism was " + peakParallelism);

            void OccupyTheLockALittleBit(int key)
            {
                using (KeyedSemaphore.Lock(key.ToString()))
                {
                    var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);

                    lock (parallelismLock)
                    {
                        peakParallelism = Math.Max(incrementedCurrentParallelism, peakParallelism);
                    }

                    var currentThreadId = Thread.CurrentThread.ManagedThreadId;

                    if (!runningThreadsIndex.TryAdd(key, currentThreadId))
                    {
                        throw new InvalidOperationException(
                            $"Thread #{currentThreadId} acquired a lock using key ${key} but another thread is also still running using this key!");
                    }

                    const int delay = 10;

                    Thread.Sleep(delay);

                    if (!runningThreadsIndex.TryRemove(key, out var value))
                    {
                        throw new InvalidOperationException($"Thread #{currentThreadId} has just finished " +
                                                            $"but the running threads index does not contain an entry for key {key}");
                    }

                    if (value != currentThreadId)
                    {
                        var ex = new InvalidOperationException($"Thread #{currentThreadId} has just finished " +
                                                               $"but the running threads index has linked thread #{value} to key {key}!");

                        throw ex;
                    }

                    Interlocked.Decrement(ref currentParallelism);
                }
            }
        }
    }
}
