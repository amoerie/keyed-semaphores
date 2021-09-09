# 🔑🔒 Changelog

# 2.0.0 November 18th, 2020

Properly dispose of inner SemaphoreSlim instances. This is only necessary if you actually use the AvailableWaitHandle on SemaphoreSlim - which this library doesn't - but it's better to be a good citizen and call Dispose at the appropriate times.

To make sure disposal triggers immediate cancellation of any pending wait operations,  the inner SemaphoreSlim instance is no longer exposed as a property. Instead, the "WaitAsync" and "Release" methods are exposed on IKeyedSemaphore directly.

This is only relevant if you are already using KeyedSemaphore.GetOrCreate and using the IKeyedSemaphore directly. If you only use KeyedSemaphore.LockAsync, this change does not impact you. 

Because it is a breaking change, I'm marking it as 2.0.0, but fixing the breaking change in your code should be very straightforward. Simply replace all instances of `keyedSemaphore.Semaphore.WaitAsync` with `keyedSemaphore.WaitAsync`. The same goes for `Release`. 

If you were using properties of SemaphoreSlim other than the ones that I've surfaced on IKeyedSemaphore, raise an issue here and we'll talk about it! 

# 0.1.2 October 5th, 2020

Add convenience method to acquire AND lock keyed semaphore in one go

# 0.1.1 October 3d, 2020

Fix namespace

# 0.1.0 October 3d, 2020

Initial release
