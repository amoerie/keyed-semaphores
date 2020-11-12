# 🔑🔒 Keyed Semaphores

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2Famoerie%2Fkeyed-semaphores%2Fbadge%3Fref%3Dmaster&style=for-the-badge&label=Build)](https://actions-badge.atrox.dev/amoerie/keyed-semaphores/goto?ref=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/KeyedSemaphores?label=KeyedSemaphores&style=for-the-badge)](https://www.nuget.org/packages/KeyedSemaphores)

In multithreaded C#, it can be wasteful to use one single lock for all threads. Some examples:

- When creating a lot of directories, you can create one lock for threads that create directories, or you could create a lock object per "highest level" directory
- When processing a lot of orders, you must perform all steps for one order sequentially, but you can handle multiple orders in parallel.
- etc.

This library helps you create a lock object per key, and then use that lock object to improve the parallelism in your application.

# NuGet

- [KeyedSemaphores](https://www.nuget.org/packages/KeyedSemaphores/) 

# Changelog 

See the [CHANGELOG.MD](https://github.com/amoerie/dead-man-switch/tree/master/CHANGELOG.MD) file

# Contributors 

See the [CONTRIBUTORS.MD](https://github.com/amoerie/dead-man-switch/tree/master/CONTRIBUTORS.MD) file