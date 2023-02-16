# ⏱️ Benchmarks

Look at the benchmarks source code here: https://github.com/amoerie/keyed-semaphores/blob/main/KeyedSemaphores.Benchmarks/Program.cs

February 16th, 2023 Results
---------------------------

``` ini

BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22623.1255)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2


```
|                        Method | NumberOfLocks | Contention |      Mean |    Error |   StdDev | Ratio | RatioSD |       Gen0 |      Gen1 |      Gen2 | Allocated | Alloc Ratio |
|------------------------------ |-------------- |----------- |----------:|---------:|---------:|------:|--------:|-----------:|----------:|----------:|----------:|------------:|
|               KeyedSemaphores |         10000 |        100 |  73.06 ms | 1.374 ms | 1.471 ms |  1.00 |    0.00 |  2285.7143 | 1714.2857 | 1000.0000 |  31.71 MB |        1.00 |
|                AsyncKeyedLock |         10000 |        100 | 154.64 ms | 3.092 ms | 4.629 ms |  2.14 |    0.08 | 30250.0000 | 2500.0000 |  750.0000 | 199.68 MB |        6.30 |
| &#39;AsyncKeyedLock with pooling&#39; |         10000 |        100 | 470.00 ms | 1.629 ms | 1.523 ms |  6.45 |    0.14 |  8000.0000 | 1000.0000 | 1000.0000 |  69.86 MB |        2.20 |
|              StripedAsyncLock |         10000 |        100 |  85.41 ms | 1.692 ms | 3.533 ms |  1.15 |    0.05 |  3500.0000 | 2500.0000 | 1166.6667 |  38.08 MB |        1.20 |
