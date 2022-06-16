using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using KeyedSemaphores;

BenchmarkRunner.Run<KeyedSemaphoreBenchmarks>();

[MemoryDiagnoser]
public class KeyedSemaphoreBenchmarks
{
    [Params(1, 10, 100)] 
    public int NumberOfLocks { get; set; }

    [Params(1, 10, 100)] 
    public int Contention { get; set; }

    [Benchmark(Baseline = true)]
    public async Task DictionaryOfSemaphoreSlims()
    {
        var semaphores = new ConcurrentDictionary<string, SemaphoreSlim>(Environment.ProcessorCount, NumberOfLocks);
        var tasks = Enumerable.Range(0, Contention * NumberOfLocks).Select(async i =>
        {
            string key = i.ToString();
            SemaphoreSlim? semaphore;
            
            // get or create a semaphore
            while (true)
            {
                if (semaphores.TryGetValue(key, out semaphore))
                    break;

                semaphore = new SemaphoreSlim(1, 1);

                if (semaphores.TryAdd(key, semaphore))
                    break;
                
                semaphore.Dispose();
            }

            await semaphore.WaitAsync();
            try
            {
                await Task.Delay(1);
            }
            finally
            {
                semaphore.Release();
            }

            return Task.CompletedTask;
        });

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task KeyedSemaphores()
    {
        var tasks = Enumerable.Range(0, Contention * NumberOfLocks).Select(async i =>
        {
            string key = i.ToString();

            using var _ = await KeyedSemaphore.LockAsync(key);
            
            await Task.Delay(1);
        });

        await Task.WhenAll(tasks);
    }
}
