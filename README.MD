# 🔑🔒 Keyed Semaphores

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2Famoerie%2Fkeyed-semaphores%2Fbadge%3Fref%3Dmaster&style=for-the-badge&label=Build)](https://actions-badge.atrox.dev/amoerie/keyed-semaphores/goto?ref=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/KeyedSemaphores?label=KeyedSemaphores&style=for-the-badge)](https://www.nuget.org/packages/KeyedSemaphores)

In multithreaded C#, it can be wasteful to use one single lock for all threads. Some examples:

- When creating a lot of directories, you can create one lock for threads that create directories, or you could create a lock object per "highest level" directory
- When processing a lot of bank transactions, you can process transactions in parallel, except when they come from the same person. In that case you probably want to run those transactions sequentially. In that case, you would create a keyed semaphore where the key is the ID of that person.
- etc.

This library helps you create a lock object per key, and then use that lock object to improve the parallelism in your application.

# Sample

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


See working code sample at https://github.com/amoerie/keyed-semaphores/blob/master/KeyedSemaphores.Samples/Program.cs

# Changelog 

See the [CHANGELOG.MD](https://github.com/amoerie/dead-man-switch/tree/master/CHANGELOG.MD) file

# Contributors 

See the [CONTRIBUTORS.MD](https://github.com/amoerie/dead-man-switch/tree/master/CONTRIBUTORS.MD) file
