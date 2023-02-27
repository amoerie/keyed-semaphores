# ⏱️ Benchmarks

Look at the benchmarks source code here: https://github.com/amoerie/keyed-semaphores/blob/main/KeyedSemaphores.Benchmarks/Program.cs

February 27th, 2023 Results
---------------------------

``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22623.1255)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.200
  [Host]     : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2


```
|                                Method | NumberOfLocks | Contention |      Mean |    Error |   StdDev | Ratio | RatioSD |       Gen0 |      Gen1 |      Gen2 | Allocated | Alloc Ratio |
|-------------------------------------- |-------------- |----------- |----------:|---------:|---------:|------:|--------:|-----------:|----------:|----------:|----------:|------------:|
|                       KeyedSemaphores |         10000 |        100 |  75.99 ms | 1.488 ms | 2.795 ms |  1.00 |    0.00 |  2285.7143 | 2000.0000 | 1000.0000 |  31.71 MB |        1.00 |
|                        AsyncKeyedLock |         10000 |        100 | 149.27 ms | 2.961 ms | 6.181 ms |  1.97 |    0.11 | 30250.0000 | 4250.0000 |  750.0000 | 199.77 MB |        6.30 |
|         &#39;AsyncKeyedLock with pooling&#39; |         10000 |        100 | 446.59 ms | 1.968 ms | 1.745 ms |  5.91 |    0.14 |  8000.0000 | 1000.0000 | 1000.0000 |  69.87 MB |        2.20 |
| &#39;AsyncKeyedLock with striped locking&#39; |         10000 |        100 |  76.82 ms | 1.441 ms | 1.480 ms |  1.02 |    0.03 |  2285.7143 | 2142.8571 | 1000.0000 |  31.71 MB |        1.00 |
|                      StripedAsyncLock |         10000 |        100 |  86.55 ms | 1.699 ms | 2.695 ms |  1.14 |    0.05 |  3333.3333 | 2833.3333 | 1166.6667 |  36.12 MB |        1.14 |
