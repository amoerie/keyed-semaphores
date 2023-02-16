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
    private KeyedSemaphoresCollection<int> _semaphores = default!;
    private AsyncKeyedLocker<int> _asyncKeyedLocker = default!;
    private AsyncKeyedLocker<int> _asyncKeyedLockerPooled = default!;
    private StripedAsyncLock<int> _stripedAsyncLock = default!;

    [Params( 10000)] public int NumberOfLocks { get; set; }

    [Params( 100)] public int Contention { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random();
        _taskIds = Enumerable.Range(0, Contention * NumberOfLocks).OrderBy(_ => random.Next()).ToArray();
        _semaphores = new KeyedSemaphoresCollection<int>(NumberOfLocks);
        _asyncKeyedLocker = new AsyncKeyedLocker<int>(concurrencyLevel: Environment.ProcessorCount, capacity: NumberOfLocks);
        _asyncKeyedLockerPooled = new AsyncKeyedLocker<int>(new AsyncKeyedLockOptions { PoolSize = NumberOfLocks, PoolInitialFill = Environment.ProcessorCount }, concurrencyLevel: Environment.ProcessorCount, capacity: NumberOfLocks);
        _stripedAsyncLock = new StripedAsyncLock<int>(NumberOfLocks);
    }

    [Benchmark(Baseline = true)]
    public async Task KeyedSemaphores()
    {
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await _semaphores.LockAsync(key);

                await Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task AsyncKeyedLock()
    {
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await _asyncKeyedLocker.LockAsync(key);

                await Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }

    [Benchmark(Description = "AsyncKeyedLock with pooling")]
    public async Task AsyncKeyedLockPooled()
    {
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await _asyncKeyedLockerPooled.LockAsync(key);

                await Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }
    
    [Benchmark]
    public async Task StripedAsyncLock()
    {
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await _stripedAsyncLock.LockAsync(key);

                await Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }
}
