using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace KeyedSemaphores.Tests
{
    public class TestsForKeyedSemaphore
    {
        [Fact]
        public async Task ShouldRunThreadsWithDistinctKeysInParallel()
        {
            // Arrange
            var currentParallelism = 0;
            var maxParallelism = 0;
            var parallelismLock = new object();

            // 100 threads, 100 keys
            var threads = Enumerable.Range(0, 100)
                .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i).ConfigureAwait(false)))
                .ToList();

            // Act
            await Task.WhenAll(threads).ConfigureAwait(false);

            maxParallelism.Should().BeGreaterThan(10);

            async Task OccupyTheLockALittleBit(int key)
            {
                using (await KeyedSemaphore.LockAsync(key.ToString()))
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

            // 100 threads, 10 keys
            var threads = Enumerable.Range(0, 100)
                .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i % 10).ConfigureAwait(false)))
                .ToList();

            // Act + Assert
            await Task.WhenAll(threads).ConfigureAwait(false);

            maxParallelism.Should().BeLessOrEqualTo(10);

            async Task OccupyTheLockALittleBit(int key)
            {
                using (await KeyedSemaphore.LockAsync(key.ToString()))
                {
                    var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);


                    lock (parallelismLock)
                    {
                        maxParallelism = Math.Max(incrementedCurrentParallelism, maxParallelism);
                    }

                    var currentTaskId = Task.CurrentId ?? -1;
                    if (runningTasksIndex.TryGetValue(key, out var otherThread))
                    {
                        throw new Exception($"Thread #{currentTaskId} acquired a lock using key ${key} " +
                                            $"but another thread #{otherThread} is also still running using this key!");
                    }

                    runningTasksIndex[key] = currentTaskId;

                    const int delay = 10;

                    await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);

                    if (!runningTasksIndex.TryRemove(key, out var value))
                    {
                        var ex = new Exception($"Thread #{currentTaskId} has finished " +
                                               $"but when trying to cleanup the running threads index, the value is already gone");

                        throw ex;
                    }

                    if (value != currentTaskId)
                    {
                        var ex = new Exception($"Thread #{currentTaskId} has finished and has removed itself from the running threads index," +
                                               $" but that index contained an incorrect value: #{value}!");

                        throw ex;
                    }

                    Interlocked.Decrement(ref currentParallelism);
                }
            }
        }
    }
}