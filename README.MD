# 🔑🔒 Keyed Semaphores

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2Famoerie%2Fkeyed-semaphores%2Fbadge%3Fref%3main&style=for-the-badge&label=Build)](https://actions-badge.atrox.dev/amoerie/keyed-semaphores/goto?ref=main) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/KeyedSemaphores?label=KeyedSemaphores&style=for-the-badge)](https://www.nuget.org/packages/KeyedSemaphores)

In multithreaded C#, it can be wasteful to use one single lock for all threads. Some examples:

- When creating a lot of directories, you can create one lock for threads that create directories, or you could create a lock object per "highest level" directory
- When processing a lot of bank transactions, you can process transactions in parallel, except when they come from the same person. In that case you probably want to run those
  transactions sequentially. In that case, you would create a keyed semaphore where the key is the ID of that person.
- etc.

This library helps you create a lock object per key, and then use that lock object to improve the parallelism in your application.

# Summary

Old version: no transactions run in parallel, because they might come from the same person, in which case the transactions must be processed sequentially

```csharp
public class BankTransactionProcessor 
{
  private readonly object _lock = new object();
  
  public async Task Process(BankTransaction transaction) 
  {
    lock(_lock) 
    {
       ...
    }
  }
}
``` 

New version: all transactions can run in parallel, except the ones with the same person ID

```csharp
public class BankTransactionProcessor
{
  public async Task Process(BankTransaction transaction) 
  {
    var key = transaction.Person.Id.ToString();
    using (await KeyedSemaphore.LockAsync(key))
    {
      ...
    }
  }
}
``` 

# Usage

The static method `KeyedSemaphore.LockAsync(string key)` covers most use cases, this sample snippet shows how it works:

```csharp
var tasks = Enumerable.Range(1, 4)
    .Select(async i =>
    {
        var key = "Key" + Math.Ceiling((double)i / 2);
        Log($"Task {i:0}: I am waiting for key '{key}'");
        using (await KeyedSemaphore.LockAsync(key))
        {
            Log($"Task {i:0}: Hello world! I have key '{key}' now!");
            await Task.Delay(50);
        }

        Log($"Task {i:0}: I have released '{key}'");
    });
await Task.WhenAll(tasks.AsParallel());

void Log(string message)
{
    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} #{Thread.CurrentThread.ManagedThreadId:000} {message}");
}


/*
 * Output:

09:32:06.987 #001 Task 1: I am waiting for key 'Key1'
09:32:06.996 #001 Task 1: Hello world! I have key 'Key1' now!
09:32:07.001 #001 Task 2: I am waiting for key 'Key1'
09:32:07.002 #001 Task 3: I am waiting for key 'Key2'
09:32:07.002 #001 Task 3: Hello world! I have key 'Key2' now!
09:32:07.002 #001 Task 4: I am waiting for key 'Key2'
09:32:07.060 #006 Task 4: Hello world! I have key 'Key2' now!
09:32:07.060 #007 Task 2: Hello world! I have key 'Key1' now!
09:32:07.062 #005 Task 1: I have released 'Key1'
09:32:07.062 #004 Task 3: I have released 'Key2'
09:32:07.121 #004 Task 4: I have released 'Key2'
09:32:07.121 #005 Task 2: I have released 'Key1'

 */
 ```

Internally, `KeyedSemaphore.LockAsync(string key)` uses a static singleton instance of `KeyedSemaphoresCollection<string>`. 
If you want to have multiple collections, or want to use something other than `string` as `TKey`, you can instantiate your own `KeyedSemaphoresCollection<TKey>` like this:

```csharp
var collection1 = new KeyedSemaphoresCollection<int>();
var collection2 = new KeyedSemaphoresCollection<int>();
var collection1Tasks = Enumerable.Range(1, 4)
    .Select(async i =>
    {
        var key = (int) Math.Ceiling((double)i / 2);
        Log($"Collection 1 - Task {i:0}: I am waiting for key '{key}'");
        using (await collection1.LockAsync(key))
        {
            Log($"Collection 1 - Task {i:0}: Hello world! I have key '{key}' now!");
            await Task.Delay(50);
        }
        Log($"Collection 1 - Task {i:0}: I have released '{key}'");
    });
var collection2Tasks = Enumerable.Range(1, 4)
    .Select(async i =>
    {
        var key = (int) Math.Ceiling((double)i / 2);
        Log($"Collection 2 - Task {i:0}: I am waiting for key '{key}'");
        using (await collection2.LockAsync(key))
        {
            Log($"Collection 2 - Task {i:0}: Hello world! I have key '{key}' now!");
            await Task.Delay(50);
        }

        Log($"Collection 2 - Task {i:0}: I have released '{key}'");
    });
await Task.WhenAll(collection1Tasks.Concat(collection2Tasks).AsParallel());

void Log(string message)
{
    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} #{Thread.CurrentThread.ManagedThreadId:000} {message}");
}

/*
 * Output:

13:23:41.284 #001 Collection 1 - Task 1: I am waiting for key '1'
13:23:41.297 #001 Collection 1 - Task 1: Hello world! I have key '1' now!
13:23:41.299 #001 Collection 1 - Task 2: I am waiting for key '1'
13:23:41.302 #001 Collection 1 - Task 3: I am waiting for key '2'
13:23:41.302 #001 Collection 1 - Task 3: Hello world! I have key '2' now!
13:23:41.302 #001 Collection 1 - Task 4: I am waiting for key '2'
13:23:41.303 #001 Collection 2 - Task 1: I am waiting for key '1'
13:23:41.303 #001 Collection 2 - Task 1: Hello world! I have key '1' now!
13:23:41.304 #001 Collection 2 - Task 2: I am waiting for key '1'
13:23:41.304 #001 Collection 2 - Task 3: I am waiting for key '2'
13:23:41.306 #001 Collection 2 - Task 3: Hello world! I have key '2' now!
13:23:41.306 #001 Collection 2 - Task 4: I am waiting for key '2'
13:23:41.370 #005 Collection 2 - Task 3: I have released '2'
13:23:41.370 #008 Collection 1 - Task 3: I have released '2'
13:23:41.370 #009 Collection 1 - Task 1: I have released '1'
13:23:41.370 #007 Collection 2 - Task 1: I have released '1'
13:23:41.371 #010 Collection 1 - Task 4: Hello world! I have key '2' now!
13:23:41.371 #012 Collection 2 - Task 4: Hello world! I have key '2' now!
13:23:41.371 #013 Collection 2 - Task 2: Hello world! I have key '1' now!
13:23:41.371 #011 Collection 1 - Task 2: Hello world! I have key '1' now!
13:23:41.437 #008 Collection 1 - Task 2: I have released '1'
13:23:41.437 #009 Collection 2 - Task 2: I have released '1'
13:23:41.437 #010 Collection 1 - Task 4: I have released '2'
13:23:41.437 #011 Collection 2 - Task 4: I have released '2'

 */
```

# Timeout and cancellation

Keyed semaphores supports parameters for both timeout and cancellation handling, similar to the SemaphoreSlim API.

## Cancellation only

The following methods support cancellation and will throw an `OperationCanceledException` if necessary.

```csharp
class KeyedSemaphoresCollection<TKey> 
{
  IDisposable Lock(TKey key, CancellationToken cancellationToken = default)
  Task<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default);
}
```

Usage:

```csharp
CancellationToken cancellationToken = ...;
using(await KeyedSemaphore.LockAsync("123", cancellationToken)) 
{
  // Your code here
}
```

## Timeout + cancellation

The following methods support timeout _and_ cancellation. 
Note that these methods return a boolean. Your lock-constrained code must be provided  through the _callback_ parameter.

```csharp
class KeyedSemaphoresCollection<TKey> 
{
  bool TryLock(TKey key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default)
  Task<bool> TryLockAsync(TKey key, TimeSpan timeout, Action callback, CancellationToken cancellationToken = default)
  Task<bool> TryLockAsync(TKey key, TimeSpan timeout, Func<Task> callback, CancellationToken cancellationToken = default)
}
```

Usage:

```csharp
CancellationToken cancellationToken = ...;
if(await KeyedSemaphore.TryLockAsync("123", TimeSpan.FromSeconds(10), CallbackAsync, cancellationToken)) 
{
  Console.WriteLine("Lock was acquired within 10 seconds!");
}

async Task CallbackAsync() 
{
  // Your code here
}
```

# Benchmarks

KeyedSemaphores is not the only game in town, I am aware of [AsyncKeyedLock](https://github.com/MarkCiliaVincenti/AsyncKeyedLock) and [StripedAsyncLock](https://github.com/i3arnon/AsyncUtilities)

These are some benchmarks comparing KeyedSemaphores with the others:

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22623.746)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.401
  [Host]     : .NET 6.0.9 (6.0.922.41905), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.9 (6.0.922.41905), X64 RyuJIT AVX2


```
|           Method | NumberOfLocks | Contention |        Mean |      Error |     StdDev |      Median | Ratio | RatioSD |     Gen0 |     Gen1 |     Gen2 |  Allocated | Alloc Ratio |
|----------------- |-------------- |----------- |------------:|-----------:|-----------:|------------:|------:|--------:|---------:|---------:|---------:|-----------:|------------:|
|  **KeyedSemaphores** |            **10** |          **1** |    **43.33 μs** |   **1.385 μs** |   **4.083 μs** |    **42.71 μs** |  **1.00** |    **0.00** |  **12.5732** |   **2.3193** |        **-** |    **74.5 KB** |        **1.00** |
|   AsyncKeyedLock |            10 |          1 |    46.61 μs |   1.731 μs |   5.103 μs |    45.61 μs |  1.08 |    0.15 |  12.6953 |   2.4414 |        - |   74.42 KB |        1.00 |
| StripedAsyncLock |            10 |          1 |    39.62 μs |   1.331 μs |   3.841 μs |    38.91 μs |  0.92 |    0.11 |  12.2070 |   2.2583 |        - |   73.29 KB |        0.98 |
|                  |               |            |             |            |            |             |       |         |          |          |          |            |             |
|  **KeyedSemaphores** |            **10** |         **10** |   **102.04 μs** |   **2.037 μs** |   **4.801 μs** |   **101.16 μs** |  **1.00** |    **0.00** |  **18.0664** |   **3.4180** |        **-** |  **110.39 KB** |        **1.00** |
|   AsyncKeyedLock |            10 |         10 |    95.60 μs |   2.788 μs |   7.344 μs |    95.02 μs |  0.95 |    0.08 |  18.5547 |   3.4180 |        - |  114.29 KB |        1.04 |
| StripedAsyncLock |            10 |         10 |    62.79 μs |   1.125 μs |   1.105 μs |    62.18 μs |  0.60 |    0.04 |  20.7520 |   4.2725 |        - |  125.79 KB |        1.14 |
|                  |               |            |             |            |            |             |       |         |          |          |          |            |             |
|  **KeyedSemaphores** |          **1000** |          **1** |   **504.96 μs** |   **9.895 μs** |  **16.258 μs** |   **502.33 μs** |  **1.00** |    **0.00** | **102.0508** |  **38.0859** |        **-** |  **614.81 KB** |        **1.00** |
|   AsyncKeyedLock |          1000 |          1 |   662.22 μs |  13.119 μs |  12.271 μs |   665.21 μs |  1.32 |    0.06 |  96.6797 |  32.2266 |        - |  574.06 KB |        0.93 |
| StripedAsyncLock |          1000 |          1 |   344.14 μs |   6.869 μs |  13.398 μs |   337.10 μs |  0.69 |    0.04 |  63.9648 |  21.4844 |        - |  385.63 KB |        0.63 |
|                  |               |            |             |            |            |             |       |         |          |          |          |            |             |
|  **KeyedSemaphores** |          **1000** |         **10** | **4,689.56 μs** |  **90.039 μs** |  **96.341 μs** | **4,660.67 μs** |  **1.00** |    **0.00** | **679.6875** | **265.6250** | **132.8125** | **4249.36 KB** |        **1.00** |
|   AsyncKeyedLock |          1000 |         10 | 6,905.11 μs |  32.046 μs |  25.020 μs | 6,911.31 μs |  1.47 |    0.03 | 742.1875 | 296.8750 | 148.4375 | 4612.04 KB |        1.09 |
| StripedAsyncLock |          1000 |         10 | 6,056.96 μs | 120.564 μs | 217.401 μs | 6,094.15 μs |  1.27 |    0.05 | 937.5000 | 382.8125 | 187.5000 | 5849.46 KB |        1.38 |

From these numbers, I conclude that in high contention scenarios (= a lot of threads competing for the same key), KeyedSemaphores wins in terms of both speed and allocations.
However, in (very) low contention scenarios, StripedAsyncLock seems the better bet.

Choose wisely!

# Changelog

See the [CHANGELOG.MD](https://github.com/amoerie/keyed-semaphores/tree/main/CHANGELOG.MD) file

# Contributors

See the [CONTRIBUTORS.MD](https://github.com/amoerie/keyed-semaphores/tree/main/CONTRIBUTORS.MD) file
