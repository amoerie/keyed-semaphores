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
|                        Method | NumberOfLocks | Contention |     Mean |    Error |    StdDev |   Median | Ratio | RatioSD |       Gen0 |       Gen1 |      Gen2 | Allocated | Alloc Ratio |
|------------------------------ |-------------- |----------- |---------:|---------:|----------:|---------:|------:|--------:|-----------:|-----------:|----------:|----------:|------------:|
|               KeyedSemaphores |         10000 |        100 | 294.5 ms |  5.66 ms |  13.67 ms | 291.3 ms |  1.00 |    0.00 | 27000.0000 | 10000.0000 | 2000.0000 | 176.69 MB |        1.00 |
|                AsyncKeyedLock |         10000 |        100 | 950.1 ms | 36.42 ms | 106.25 ms | 937.6 ms |  3.25 |    0.35 | 59000.0000 | 21000.0000 | 2000.0000 | 364.06 MB |        2.06 |
| &#39;AsyncKeyedLock with pooling&#39; |         10000 |        100 | 899.5 ms | 20.26 ms |  59.42 ms | 889.2 ms |  3.06 |    0.23 | 42000.0000 | 16000.0000 | 2000.0000 | 263.39 MB |        1.49 |
|              StripedAsyncLock |         10000 |        100 | 550.1 ms | 18.80 ms |  53.03 ms | 534.7 ms |  1.89 |    0.18 | 40000.0000 | 17000.0000 | 2000.0000 | 252.16 MB |        1.43 |
