# ⏱️ Benchmarks

Look at the benchmarks source code here: https://github.com/amoerie/keyed-semaphores/blob/main/KeyedSemaphores.Benchmarks/Program.cs

July 20th, 2023 Results
---------------------------

``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1992/22H2/2022Update/SunValley2)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.306
  [Host]     : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2


```
|                                Method | NumberOfLocks | Contention |      Mean |    Error |   StdDev | Ratio | RatioSD |       Gen0 |      Gen1 |      Gen2 | Allocated | Alloc Ratio |
|-------------------------------------- |-------------- |----------- |----------:|---------:|---------:|------:|--------:|-----------:|----------:|----------:|----------:|------------:|
|             KeyedSemaphoresCollection |         10000 |        100 |  77.92 ms | 1.361 ms | 1.273 ms |  1.00 |    0.00 |  2285.7143 | 2142.8571 | 1000.0000 |   31.7 MB |        1.00 |
|             KeyedSemaphoresDictionary |         10000 |        100 | 138.84 ms | 2.687 ms | 3.588 ms |  1.78 |    0.06 | 31750.0000 | 3500.0000 | 1000.0000 |  207.2 MB |        6.54 |
|                        AsyncKeyedLock |         10000 |        100 | 135.64 ms | 2.679 ms | 4.762 ms |  1.72 |    0.06 | 30250.0000 | 3750.0000 |  750.0000 |    200 MB |        6.31 |
|         &#39;AsyncKeyedLock with pooling&#39; |         10000 |        100 | 438.58 ms | 1.135 ms | 1.006 ms |  5.63 |    0.09 |  8000.0000 | 1000.0000 | 1000.0000 |  69.87 MB |        2.20 |
| &#39;AsyncKeyedLock with striped locking&#39; |         10000 |        100 |  79.63 ms | 1.573 ms | 1.683 ms |  1.02 |    0.03 |  2500.0000 | 2333.3333 | 1166.6667 |   31.7 MB |        1.00 |
|                      StripedAsyncLock |         10000 |        100 |  89.35 ms | 1.764 ms | 3.760 ms |  1.18 |    0.04 |  2833.3333 | 2166.6667 |  833.3333 |  35.53 MB |        1.12 |
