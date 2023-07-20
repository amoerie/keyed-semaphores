# 🔑🔒 Keyed Semaphores

[![Build Status](https://img.shields.io/github/actions/workflow/status/amoerie/keyed-semaphores/build.yml?label=Build&style=for-the-badge)](https://github.com/amoerie/keyed-semaphores/actions/workflows/build.yml)
[![Codecov](https://img.shields.io/codecov/c/github/amoerie/keyed-semaphores?label=Coverage&logo=codecov&style=for-the-badge)](https://app.codecov.io/gh/amoerie/keyed-semaphores)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/KeyedSemaphores?label=KeyedSemaphores&style=for-the-badge)](https://www.nuget.org/packages/KeyedSemaphores)

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

# Multiple dictionaries

Internally, `KeyedSemaphore.LockAsync(string key)` uses a static singleton instance of `KeyedSemaphoresDictionary<string>`.
You can instantiate your own dictionaries too.
Possible reasons to do so:
- Using something other than `string` as keys
- Not needing to worry about collisions between unrelated code
- Changing the default parameters of capacity, synchronous wait duration, etc.
- ...

Example usage of using your own dictionaries:

```csharp
var dictionary1 = new KeyedSemaphoresDictionary<int>();
var dictionary2 = new KeyedSemaphoresDictionary<int>();
var dictionary1Tasks = Enumerable.Range(1, 4)
    .Select(async i =>
    {
        var key = (int) Math.Ceiling((double)i / 2);
        Log($"Dictionary 1 - Task {i:0}: I am waiting for key '{key}'");
        using (await dictionary1.LockAsync(key))
        {
            Log($"Dictionary 1 - Task {i:0}: Hello world! I have key '{key}' now!");
            await Task.Delay(50);
        }
        Log($"Dictionary 1 - Task {i:0}: I have released '{key}'");
    });
var dictionary2Tasks = Enumerable.Range(1, 4)
    .Select(async i =>
    {
        var key = (int) Math.Ceiling((double)i / 2);
        Log($"Dictionary 2 - Task {i:0}: I am waiting for key '{key}'");
        using (await dictionary2.LockAsync(key))
        {
            Log($"Dictionary 2 - Task {i:0}: Hello world! I have key '{key}' now!");
            await Task.Delay(50);
        }

        Log($"Dictionary 2 - Task {i:0}: I have released '{key}'");
    });
await Task.WhenAll(dictionary1Tasks.Concat(dictionary2Tasks).AsParallel());

void Log(string message)
{
    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} #{Thread.CurrentThread.ManagedThreadId:000} {message}");
}

/*
 * Output:

13:23:41.284 #001 Dictionary 1 - Task 1: I am waiting for key '1'
13:23:41.297 #001 Dictionary 1 - Task 1: Hello world! I have key '1' now!
13:23:41.299 #001 Dictionary 1 - Task 2: I am waiting for key '1'
13:23:41.302 #001 Dictionary 1 - Task 3: I am waiting for key '2'
13:23:41.302 #001 Dictionary 1 - Task 3: Hello world! I have key '2' now!
13:23:41.302 #001 Dictionary 1 - Task 4: I am waiting for key '2'
13:23:41.303 #001 Dictionary 2 - Task 1: I am waiting for key '1'
13:23:41.303 #001 Dictionary 2 - Task 1: Hello world! I have key '1' now!
13:23:41.304 #001 Dictionary 2 - Task 2: I am waiting for key '1'
13:23:41.304 #001 Dictionary 2 - Task 3: I am waiting for key '2'
13:23:41.306 #001 Dictionary 2 - Task 3: Hello world! I have key '2' now!
13:23:41.306 #001 Dictionary 2 - Task 4: I am waiting for key '2'
13:23:41.370 #005 Dictionary 2 - Task 3: I have released '2'
13:23:41.370 #008 Dictionary 1 - Task 3: I have released '2'
13:23:41.370 #009 Dictionary 1 - Task 1: I have released '1'
13:23:41.370 #007 Dictionary 2 - Task 1: I have released '1'
13:23:41.371 #010 Dictionary 1 - Task 4: Hello world! I have key '2' now!
13:23:41.371 #012 Dictionary 2 - Task 4: Hello world! I have key '2' now!
13:23:41.371 #013 Dictionary 2 - Task 2: Hello world! I have key '1' now!
13:23:41.371 #011 Dictionary 1 - Task 2: Hello world! I have key '1' now!
13:23:41.437 #008 Dictionary 1 - Task 2: I have released '1'
13:23:41.437 #009 Dictionary 2 - Task 2: I have released '1'
13:23:41.437 #010 Dictionary 1 - Task 4: I have released '2'
13:23:41.437 #011 Dictionary 2 - Task 4: I have released '2'

 */
```

# Multiple collections

Next to `KeyedSemaphoresDictionary`, it is also possible to use a `KeyedSemaphoresCollection`.
See the next chapter on how to decide between the two.

The API is exactly the same:

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

# Choosing between `KeyedSemaphoresCollection` and `KeyedSemaphoresDictionary`

There are two implementations of keyed semaphore locking in this library. The default, `KeyedSemaphoresDictionary`, is dictionary based and maps each key to a unique `SemaphoreSlim` using a `ConcurrentDictionary`.
A ref counting mechanism ensures unused keyed semaphores are cleaned up when the last usage has finished.

While this is correct and fast enough for a lot of use cases, a much faster alternative exists: `KeyedSemaphoresCollection`.
This implementations pre-allocates a fixed array of `SemaphoreSlim` instances and maps each key via its hash code to one of the semaphores.

In benchmarks, the array based approach is ~2x faster and allocates ~6x fewer total memory.
An important limitation of the array based approach is that nested locking is not supported:

```csharp
// Never do this
var collection = new KeyedSemaphoresCollection<int>();
using(await collection.LockAsync("123", cancellationToken)) 
{
  using(await collection.LockAsync("456", cancellationToken)) 
  {
    // Your code here
  }
}
```

Even if the keys are different, they could map to the same underlying `SemaphoreSlim` and cause a deadlock.
If you need support for nested locking, use `KeyedSemaphoresDictionary`.

Here are my personal guidelines for when to use `KeyedSemaphoresCollection`:

- When the collection can be long lived
- When nested locking is not needed


# Controlling the number of semaphores of a `KeyedSemaphoresCollection`

The `KeyedSemaphoresCollection` constructor takes a single parameter called `numberOfSemaphores`
Internally, every key maps to one semaphore. This means that, if your number of unique keys if higher than the number of semaphores, it is possible that
two different keys share the same semaphore and therefore do not run concurrently.
If you know the number of unique keys beforehand, you can initialize the collection like this:

```csharp
int numberOfUniqueKeys = 25000;
var collection = new KeyedSemaphoresCollection<int>(numberOfUniqueKeys);
```

# Timeout and cancellation

Keyed semaphores supports parameters for both timeout and cancellation handling, similar to the SemaphoreSlim API.

## Cancellation only

The following methods support cancellation and will throw an `OperationCanceledException` if necessary.

```csharp
interface IKeyedSemaphoresCollection<TKey> 
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
interface IKeyedSemaphoresCollection<TKey> 
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

# Alternatives

KeyedSemaphores is not the only game in town, I am aware of 
- [AsyncKeyedLock](https://github.com/MarkCiliaVincenti/AsyncKeyedLock)
- [StripedAsyncLock](https://github.com/i3arnon/AsyncUtilities)

If you're interested in how they compare performance-wise, I have some benchmarks here: https://github.com/amoerie/keyed-semaphores/blob/main/BENCHMARKS.MD

Don't just take these numbers at face value though.
You should be aware that every library makes different tradeoffs, and that benchmarks probably don't represent your exact use case.
Try before you buy!

# Changelog

See the [CHANGELOG.MD](https://github.com/amoerie/keyed-semaphores/tree/main/CHANGELOG.MD) file

# Contributors

See the [CONTRIBUTORS.MD](https://github.com/amoerie/keyed-semaphores/tree/main/CONTRIBUTORS.MD) file
