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
    private KeyedSemaphoresCollection<int> _keyedSemaphoresCollection = default!;
    private AsyncKeyedLocker<int> _asyncKeyedLocker = default!;
    private AsyncKeyedLocker<int> _asyncKeyedLockerPooled = default!;
    private StripedAsyncKeyedLocker<int> _stripedAsyncKeyedLocker = default!;
    private StripedAsyncLock<int> _stripedAsyncLock = default!;
    private KeyedSemaphoresDictionary<int> _keyedSemaphoresDictionary = default!;

    [Params( 10000)] public int NumberOfLocks { get; set; }

    [Params( 100)] public int Contention { get; set; }
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random();
        _taskIds = Enumerable.Range(0, Contention * NumberOfLocks).OrderBy(_ => random.Next()).ToArray();
        _keyedSemaphoresCollection = new KeyedSemaphoresCollection<int>(NumberOfLocks);
        _keyedSemaphoresDictionary = new KeyedSemaphoresDictionary<int>(Environment.ProcessorCount, NumberOfLocks, EqualityComparer<int>.Default, TimeSpan.FromMilliseconds(10));
        _asyncKeyedLocker = new AsyncKeyedLocker<int>(concurrencyLevel: Environment.ProcessorCount, capacity: NumberOfLocks);
        _asyncKeyedLockerPooled = new AsyncKeyedLocker<int>(new AsyncKeyedLockOptions { PoolSize = NumberOfLocks, PoolInitialFill = Environment.ProcessorCount }, concurrencyLevel: Environment.ProcessorCount, capacity: NumberOfLocks);
        _stripedAsyncKeyedLocker = new StripedAsyncKeyedLocker<int>(NumberOfLocks, _taskIds.Length);
        _stripedAsyncLock = new StripedAsyncLock<int>(NumberOfLocks);
    }
    
    [Benchmark(Baseline = true)]
    public async Task KeyedSemaphoresCollection()
    {
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await _keyedSemaphoresCollection.LockAsync(key);

                await Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }
    
    [Benchmark]
    public async Task KeyedSemaphoresDictionary()
    {
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await _keyedSemaphoresDictionary.LockAsync(key);

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

    [Benchmark(Description = "AsyncKeyedLock with striped locking")]
    public async Task AsyncKeyedLockStriped()
    {
        var tasks = _taskIds
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await _stripedAsyncKeyedLocker.LockAsync(key);

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
