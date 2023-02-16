# 🔑🔒 Changelog

# 4.0 February 16th, 2023

The public API has not been modified, except for the constructor of `KeyedSemaphoresCollection`
The internal key bookkeeping mechanism has been heavily refactored.
Internally, keys now map to an entry in an array of SemaphoreSlims.

This has the following pros and cons:

PROS
----
There is no more dynamic creation and disposal of semaphores. 
This avoids subtle race conditions, and drastically speeds up typical usage and reduces allocation to almost zero.
The only real allocation happens when a semaphore is asynchronously awaited.
Furthermore, the semaphore will be awaited synchronously for a very short time even in the asynchronous methods to try to avoid this allocation.

CONS
----
This library still guarantees that no two threads will ever hold a lock with the same key.
However, depending on the amount of unique keys at runtime and the `numberOfSemaphores` parameter passed to the `KeyedSemaphoresCollection`,
it is now possible that two threads with different keys share the same underlying semaphore and have to wait for each other.

# 3.2 January 24th, 2023

- Add support to check if key already locked with IsInUse API (#45)

# 3.1 October 28th, 2022

- Add support for timeout with TryLock(Async) API
- Fix issue where keyed semaphores were not released when the cancellationToken triggers

# 3.0 October 20th, 2022

- Remove interfaces, just use KeyedSemaphore and KeyedSemaphoreCollection
- Remove a lot of API surface, just use KeyedSemaphore.Lock(Async) or KeyedSemaphoreCollection.Lock(Async)
- KeyedSemaphoreCollection and KeyedSemaphore are no longer IDisposable
- Drastically improve performance and reduce allocations 

# 2.2.1 June 23th, 2022

- Remove `IEquatable` constraint for keys 

# 2.2.0 June 16th, 2022

- Add benchmarks (#14)
- Add support for generic keys (#15)
- Add support for multiple keyed semaphore collections (#16)

# 2.1.0 September 29th, 2021

Add support for synchronous locking, a contribution by @roel-de-regt. See #1 for more info

# 2.0.0 November 18th, 2020

Properly dispose of inner SemaphoreSlim instances. This is only necessary if you actually use the AvailableWaitHandle on SemaphoreSlim - which this library doesn't - but it's
better to be a good citizen and call Dispose at the appropriate times.

To make sure disposal triggers immediate cancellation of any pending wait operations, the inner SemaphoreSlim instance is no longer exposed as a property. Instead, the "WaitAsync"
and "Release" methods are exposed on IKeyedSemaphore directly.

This is only relevant if you are already using KeyedSemaphore.GetOrCreate and using the IKeyedSemaphore directly. If you only use KeyedSemaphore.LockAsync, this change does not
impact you.

Because it is a breaking change, I'm marking it as 2.0.0, but fixing the breaking change in your code should be very straightforward. Simply replace all instances
of `keyedSemaphore.Semaphore.WaitAsync` with `keyedSemaphore.WaitAsync`. The same goes for `Release`.

If you were using properties of SemaphoreSlim other than the ones that I've surfaced on IKeyedSemaphore, raise an issue here and we'll talk about it!

# 0.1.2 October 5th, 2020

Add convenience method to acquire AND lock keyed semaphore in one go

# 0.1.1 October 3d, 2020

Fix namespace

# 0.1.0 October 3d, 2020

Initial release
