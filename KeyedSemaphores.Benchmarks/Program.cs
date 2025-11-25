using AsyncKeyedLock;
using AsyncUtilities;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using KeyedSemaphores;

BenchmarkRunner.Run<KeyedSemaphoreBenchmarks>();

[MemoryDiagnoser]
[ShortRunJob]
public class KeyedSemaphoreBenchmarks
{
    private int[] _taskIds = default!;
    
    // ints
    private KeyedSemaphoresCollection<int> _keyedSemaphoresCollection = default!;
    private AsyncKeyedLocker<int> _asyncKeyedLocker = default!;
    private StripedAsyncKeyedLocker<int> _stripedAsyncKeyedLocker = default!;
    private StripedAsyncLock<int> _stripedAsyncLock = default!;
    private KeyedSemaphoresDictionary<int> _keyedSemaphoresDictionary = default!;
    
    // strings
    private KeyedSemaphoresCollection<string> _keyedSemaphoresCollectionStrings = default!;
    private AsyncKeyedLocker<string> _asyncKeyedLockerStrings = default!;
    private StripedAsyncKeyedLocker<string> _stripedAsyncKeyedLockerStrings = default!;
    private StripedAsyncLock<string> _stripedAsyncLockStrings = default!;
    private KeyedSemaphoresDictionary<string> _keyedSemaphoresDictionaryStrings = default!;

    [Params( 10000)] public int NumberOfLocks { get; set; }
    [Params( 100)] public int Contention { get; set; }
    [Params("int", "string")] public string? Type { get; set; }
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random();
        _taskIds = Enumerable.Range(0, Contention * NumberOfLocks).OrderBy(_ => random.Next()).ToArray();
        _keyedSemaphoresCollection = new KeyedSemaphoresCollection<int>(NumberOfLocks);
        _keyedSemaphoresDictionary = new KeyedSemaphoresDictionary<int>(Environment.ProcessorCount, NumberOfLocks, EqualityComparer<int>.Default, TimeSpan.FromMilliseconds(10));
        _asyncKeyedLocker = new AsyncKeyedLocker<int>(concurrencyLevel: Environment.ProcessorCount, capacity: NumberOfLocks);
        _stripedAsyncKeyedLocker = new StripedAsyncKeyedLocker<int>(NumberOfLocks, _taskIds.Length);
        _stripedAsyncLock = new StripedAsyncLock<int>(NumberOfLocks);
        _keyedSemaphoresCollectionStrings = new KeyedSemaphoresCollection<string>(NumberOfLocks);
        _keyedSemaphoresDictionaryStrings = new KeyedSemaphoresDictionary<string>(Environment.ProcessorCount, NumberOfLocks, EqualityComparer<string>.Default, TimeSpan.FromMilliseconds(10));
        _asyncKeyedLockerStrings = new AsyncKeyedLocker<string>(concurrencyLevel: Environment.ProcessorCount, capacity: NumberOfLocks);
        _stripedAsyncKeyedLockerStrings = new StripedAsyncKeyedLocker<string>(NumberOfLocks, _taskIds.Length);
        _stripedAsyncLockStrings = new StripedAsyncLock<string>(NumberOfLocks);
    }
    
    [Benchmark(Baseline = true)]
    public async Task KeyedSemaphoresCollection()
    {
        ParallelQuery<Task> tasks;
        switch (Type)
        {
            case "int":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = i % NumberOfLocks;
                        using var _ = await _keyedSemaphoresCollection.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            case "string":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = (i % NumberOfLocks).ToString();
                        using var _ = await _keyedSemaphoresCollectionStrings.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            default:
                throw new NotImplementedException();
        }
        await Task.WhenAll(tasks);
    }
    
    [Benchmark]
    public async Task KeyedSemaphoresDictionary()
    {
        ParallelQuery<Task> tasks;
        switch (Type)
        {
            case "int":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = i % NumberOfLocks;
                        using var _ = await _keyedSemaphoresDictionary.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            case "string":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = (i % NumberOfLocks).ToString();
                        using var _ = await _keyedSemaphoresDictionaryStrings.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            default:
                throw new NotImplementedException();
        }
        await Task.WhenAll(tasks);
    }
    
    [Benchmark]
    public async Task AsyncKeyedLock()
    {
        ParallelQuery<Task> tasks;
        switch (Type)
        {
            case "int":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = i % NumberOfLocks;
                        using var _ = await _asyncKeyedLocker.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            case "string":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = (i % NumberOfLocks).ToString();
                        using var _ = await _asyncKeyedLockerStrings.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            default:
                throw new NotImplementedException();
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task AsyncKeyedLockStriped()
    {
        ParallelQuery<Task> tasks;
        switch (Type)
        {
            case "int":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = i % NumberOfLocks;
                        using var _ = await _stripedAsyncKeyedLocker.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            case "string":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = (i % NumberOfLocks).ToString();
                        using var _ = await _stripedAsyncKeyedLockerStrings.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            default:
                throw new NotImplementedException();
        }
        await Task.WhenAll(tasks);
    }    
    
    [Benchmark]
    public async Task StripedAsyncLock()
    {
        ParallelQuery<Task> tasks;
        switch (Type)
        {
            case "int":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = i % NumberOfLocks;
                        using var _ = await _stripedAsyncLock.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            case "string":
                tasks = _taskIds
                    .AsParallel()
                    .Select(async i =>
                    {
                        var key = (i % NumberOfLocks).ToString();
                        using var _ = await _stripedAsyncLockStrings.LockAsync(key);
                        await Task.CompletedTask;
                    });
                break;
            default:
                throw new NotImplementedException();
        }
        await Task.WhenAll(tasks);
    }
    
}
