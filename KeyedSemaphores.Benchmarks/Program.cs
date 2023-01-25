using AsyncKeyedLock;
using AsyncUtilities;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using KeyedSemaphores;

BenchmarkRunner.Run<KeyedSemaphoreBenchmarks>();


[MemoryDiagnoser]
public class KeyedSemaphoreBenchmarks
{
    private int[] _taskIds = default!;
    
    [Params( 10000)] public int NumberOfLocks { get; set; }

    [Params( 100)] public int Contention { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random();
        _taskIds = Enumerable.Range(0, Contention * NumberOfLocks).OrderBy(_ => random.Next()).ToArray();
    }

    [Benchmark(Baseline = true)]
    public async Task KeyedSemaphores()
    {
        var semaphores = new KeyedSemaphoresCollection<int>(initialCapacity: _taskIds.Length, estimatedConcurrencyLevel: Environment.ProcessorCount);
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await semaphores.LockAsync(key);

                await Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task AsyncKeyedLock()
    {
        var asyncKeyedLocker = new AsyncKeyedLocker<int>(concurrencyLevel: Environment.ProcessorCount, capacity: _taskIds.Length);
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await asyncKeyedLocker.LockAsync(key);

                await Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }
    
    [Benchmark]
    public async Task StripedAsyncLock()
    {
        var stripedAsyncLock = new StripedAsyncLock<int>(Environment.ProcessorCount);
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await stripedAsyncLock.LockAsync(key);

                await Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }
}
