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
    public class TestsForKeyedSemaphoresCollection
    {
        private readonly ITestOutputHelper _output;

        public TestsForKeyedSemaphoresCollection(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        private void Log(string message)
        {
            _output.WriteLine($"{DateTime.Now:HH:mm:ss.fff} #{Thread.CurrentThread.ManagedThreadId,3} {message}");
        }

        [Fact]
        public async Task ShouldRunThreadsWithDistinctKeysInParallel()
        {
            // Arrange
            var currentParallelism = 0;
            var maxParallelism = 0;
            var parallelismLock = new object();
            var index = new ConcurrentDictionary<string, IKeyedSemaphore>();
            using var keyedSemaphores = new KeyedSemaphoresCollection(index);

            // 100 threads, 100 keys
            var threads = Enumerable.Range(0, 100)
                .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i).ConfigureAwait(false)))
                .ToList();

            // Act
            await Task.WhenAll(threads).ConfigureAwait(false);

            maxParallelism.Should().BeGreaterThan(10);
            index.Should().BeEmpty();

            async Task OccupyTheLockALittleBit(int key)
            {
                var keyedSemaphore = keyedSemaphores.Provide(key.ToString());
                try
                {
                    await keyedSemaphore.WaitAsync().ConfigureAwait(false);
                    try
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
                    finally
                    {
                        keyedSemaphore.Release();
                    }
                }
                finally
                {
                    keyedSemaphore.Dispose();
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
            var index = new ConcurrentDictionary<string, IKeyedSemaphore>();
            using var keyedSemaphores = new KeyedSemaphoresCollection(index);

            // 100 threads, 10 keys
            var threads = Enumerable.Range(0, 100)
                .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i % 10).ConfigureAwait(false)))
                .ToList();

            // Act + Assert
            await Task.WhenAll(threads).ConfigureAwait(false);

            maxParallelism.Should().BeLessOrEqualTo(10);
            index.Should().BeEmpty();

            async Task OccupyTheLockALittleBit(int key)
            {
                var keyedSemaphore = keyedSemaphores.Provide(key.ToString());
                try
                {
                    await keyedSemaphore.WaitAsync().ConfigureAwait(false);
                    try
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
                    finally
                    {
                        keyedSemaphore.Release();
                    }
                }
                finally
                {
                    keyedSemaphore.Dispose();
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
            var index = new ConcurrentDictionary<string, IKeyedSemaphore>();
            using var keyedSemaphores = new KeyedSemaphoresCollection(index);

            // Many threads, 1 key
            var threads = Enumerable.Range(0, 100)
                .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(1).ConfigureAwait(false)))
                .ToList();

            // Act + Assert
            await Task.WhenAll(threads).ConfigureAwait(false);

            maxParallelism.Should().Be(1);
            index.Should().BeEmpty();


            async Task OccupyTheLockALittleBit(int key)
            {
                var currentTaskId = Task.CurrentId ?? -1;
                var delay = random.Next(500);


                await Task.Delay(delay).ConfigureAwait(false);


                IKeyedSemaphore keyedSemaphore = null;
                try
                {
                    keyedSemaphore = keyedSemaphores.Provide(key.ToString());


                    await keyedSemaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);


                        lock (parallelismLock)
                        {
                            maxParallelism = Math.Max(incrementedCurrentParallelism, maxParallelism);
                        }

                        if (runningTasksIndex.TryGetValue(key, out var otherThread))
                        {
                            throw new Exception($"Task [{currentTaskId,3}] has a lock for key ${key} " +
                                                $"but another task [{otherThread,3}] also has an active lock for this key!");
                        }

                        runningTasksIndex[key] = currentTaskId;

                        if (!runningTasksIndex.TryRemove(key, out var value))
                        {
                            var ex = new Exception($"Task [{currentTaskId,3}] has finished " +
                                                   $"but when trying to cleanup the running tasks index, the value is already gone");

                            throw ex;
                        }

                        if (value != currentTaskId)
                        {
                            var ex = new Exception($"Task [{currentTaskId,3}] has finished and has removed itself from the running tasks index," +
                                                   $" but that index contained a task ID of another task: [{value}]!");

                            throw ex;
                        }

                        Interlocked.Decrement(ref currentParallelism);
                    }
                    finally
                    {
                        keyedSemaphore.Release();
                    }
                }
                finally
                {
                    keyedSemaphore?.Dispose();
                }
            }
        }

        [Fact]
        public async Task DisposingTheKeyedSemaphoresCollectionShouldInterruptAllThreads()
        {
            // Arrange
            var runningTasksIndex = new ConcurrentDictionary<int, int>();
            var parallelismLock = new object();
            var currentParallelism = 0;
            var maxParallelism = 0;
            var random = new Random();
            var index = new ConcurrentDictionary<string, IKeyedSemaphore>();
            using var keyedSemaphores = new KeyedSemaphoresCollection(index);

            // Many threads, 1 key
            var numberOfThreads = 50;
            Log($"Starting {numberOfThreads} threads");
            var threads = Enumerable.Range(0, numberOfThreads)
                .Select(i => Task.Run(async () => await OccupyTheLockALittleBit(i, 1).ConfigureAwait(false)))
                .ToList();

            // Act + Assert
            await Task.Delay(100);
            Log($"[WAITING] keyedSemaphores.Dispose");
            keyedSemaphores.Dispose();
            Log($"[OK]      keyedSemaphores.Dispose");

            await Task.WhenAll(threads).ConfigureAwait(false);

            maxParallelism.Should().Be(1);
            index.Should().BeEmpty();

            async Task OccupyTheLockALittleBit(int thread, int key)
            {
                var currentTaskId = Task.CurrentId ?? -1;
                var delay = random.Next(0, 200);

                await Task.Delay(delay).ConfigureAwait(false);

                IKeyedSemaphore keyedSemaphore = null;
                try
                {
                    try
                    {
                        Log($"[{thread, 2}] [WAITING] KeyedSemaphores.Provide    : {key,3}");
                        keyedSemaphore = keyedSemaphores.Provide(key.ToString());
                        Log($"[{thread, 2}] [OK]      KeyedSemaphores.Provide    : {key,3}");
                    }
                    catch (ObjectDisposedException e)
                    {
                        Log($"[{thread, 2}] [DISPOSED]KeyedSemaphores.Provide    : {key,3}");
                        return;
                    }

                    try
                    {
                        Log($"[{thread, 2}] [WAITING] KeyedSemaphores.WaitAsync  : {key,3}");
                        await keyedSemaphore.WaitAsync();
                        Log($"[{thread, 2}] [OK]      KeyedSemaphores.WaitAsync  : {key,3}");
                    }
                    catch (OperationCanceledException e)
                    {
                        Log($"[{thread, 2}] [CANCELED]KeyedSemaphores.WaitAsync  : {key,3}");
                        return;
                    }

                    try
                    {
                        var incrementedCurrentParallelism = Interlocked.Increment(ref currentParallelism);

                        lock (parallelismLock)
                        {
                            maxParallelism = Math.Max(incrementedCurrentParallelism, maxParallelism);
                        }

                        if (runningTasksIndex.TryGetValue(key, out var otherThread))
                        {
                            throw new Exception($"[{thread, 2}] Task [{currentTaskId,3}] has a lock for key ${key} " +
                                                $"but another task [{otherThread,3}] also has an active lock for this key!");
                        }

                        runningTasksIndex[key] = currentTaskId;

                        if (!runningTasksIndex.TryRemove(key, out var value))
                        {
                            var ex = new Exception($"[{thread, 2}] Task [{currentTaskId,3}] has finished " +
                                                   $"but when trying to cleanup the running tasks index, the value is already gone");

                            throw ex;
                        }

                        if (value != currentTaskId)
                        {
                            var ex = new Exception($"[{thread, 2}] Task [{currentTaskId,3}] has finished and has removed itself from the running tasks index," +
                                                   $" but that index contained a task ID of another task: [{value}]!");

                            throw ex;
                        }

                        Interlocked.Decrement(ref currentParallelism);
                    }
                    finally
                    {
                        try
                        {
                            Log($"[{thread, 2}] [WAITING] KeyedSemaphore.Release     : {key,3}");
                            keyedSemaphore.Release();
                            Log($"[{thread, 2}] [OK]      KeyedSemaphore.Release     : {key,3}");
                        }
                        catch (ObjectDisposedException e)
                        {
                            Log($"[{thread, 2}] [DISPOSED]KeyedSemaphore.Release     : {e}");
                        }
                    }
                }
                finally
                {
                    if (keyedSemaphore != null)
                    {
                        Log($"[{thread, 2}] [WAITING] KeyedSemaphore.Dispose     : {key,3}");
                        keyedSemaphore.Dispose();
                        Log($"[{thread, 2}] [OK]      KeyedSemaphore.Dispose     : {key,3}");
                    }
                }
            }
        }
    }
}