using AsyncKeyedLock;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using KeyedSemaphores;

BenchmarkRunner.Run<KeyedSemaphoreBenchmarks>();

/*var b = new KeyedSemaphoreBenchmarks
{
    Contention = 10,
    NumberOfLocks = 1000
};

for (var i = 0; i < 100; i++)
{
    Console.WriteLine(i);
    await b.KeyedSemaphores();
}*/

[MemoryDiagnoser]
public class KeyedSemaphoreBenchmarks
{
    [Params( /*10, */1000)] public int NumberOfLocks { get; set; }

    [Params( /*1, */10)] public int Contention { get; set; }

    [Benchmark]
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

    [Benchmark(Baseline = true)]
    public async Task AsyncKeyedLock()
    {
        var locker = new AsyncKeyedLocker();
        var tasks = Enumerable.Range(0, Contention * NumberOfLocks)
            .AsParallel()
            .Select(async i =>
            {
                var key = i % NumberOfLocks;

                using var _ = await locker.LockAsync(key);

                await Task.Yield();
            });

        await Task.WhenAll(tasks);
    }
}
