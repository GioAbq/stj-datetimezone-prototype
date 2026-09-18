# Benchmark: what does the conversion cost?

Compares the built-in `DateTime` path, the converter most often copied out of
[dotnet/runtime#1566](https://github.com/dotnet/runtime/issues/1566) (`reader.GetDateTime().ToUniversalTime()`),
and the prototype of the proposed option. Input is offset-less text on read and a `DateTimeKind.Unspecified`
value on write - the case #122962 describes.

Reproduce with `dotnet run --project bench/StjDateTimeZone.Bench -c Release`.

Takeaway: converter dispatch is not what costs. The workaround performs a time-zone conversion on every value;
the proposed semantics treat offset-less text as UTC with `DateTime.SpecifyKind`, which needs no zone lookup.
The option left at its default is within noise of the built-in path.

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
Intel Core i7-14700HX 2.30GHz, 1 CPU, 28 logical and 20 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  short  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=short  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Read_BuiltIn           | 38.86 ns | 11.466 ns | 0.628 ns |  1.00 |    0.02 |      - |         - |          NA |
| Read_ThreadWorkaround  | 73.99 ns | 16.930 ns | 0.928 ns |  1.90 |    0.03 |      - |         - |          NA |
| Read_ProposedDefault   | 39.34 ns |  3.114 ns | 0.171 ns |  1.01 |    0.01 |      - |         - |          NA |
| Read_ProposedUtc       | 40.65 ns |  7.061 ns | 0.387 ns |  1.05 |    0.02 |      - |         - |          NA |
| Write_BuiltIn          | 55.39 ns | 12.408 ns | 0.680 ns |  1.43 |    0.02 | 0.0037 |      64 B |          NA |
| Write_ThreadWorkaround | 92.64 ns | 24.691 ns | 1.353 ns |  2.38 |    0.04 | 0.0042 |      72 B |          NA |
| Write_ProposedUtc      | 55.72 ns | 10.121 ns | 0.555 ns |  1.43 |    0.02 | 0.0042 |      72 B |          NA |
