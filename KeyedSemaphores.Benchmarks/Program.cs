using AsyncKeyedLock;
using AsyncUtilities;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using KeyedSemaphores;

BenchmarkRunner.Run<KeyedSemaphoreBenchmarks>();


[MemoryDiagnoser]
public class KeyedSemaphoreBenchmarks
{
    [Params( /*10, */1000)] public int NumberOfLocks { get; set; }

    [Params( /*1, */10)] public int Contention { get; set; }

    [Benchmark(Baseline = true)]
    public async Task KeyedSemaphores()
    {
        var semaphores = new KeyedSemaphoresCollection<int>();
        var tasks = Enumerable.Range(0, Contention * NumberOfLocks)
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await semaphores.LockAsync(key);

                await Task.Yield();
            });

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task AsyncKeyedLock()
    {
        var asyncKeyedLocker = new AsyncKeyedLocker();
        var tasks = Enumerable.Range(0, Contention * NumberOfLocks)
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await asyncKeyedLocker.LockAsync(key);

                await Task.Yield();
            });

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task StripedAsyncLock()
    {
        var stripedAsyncLock = new StripedAsyncLock<int>(NumberOfLocks);
        var tasks = Enumerable.Range(0, Contention * NumberOfLocks)
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await stripedAsyncLock.LockAsync(key);

                await Task.Yield();
            });

        await Task.WhenAll(tasks);
    }
}
