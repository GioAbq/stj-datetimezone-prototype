# Benchmark: what does the conversion cost?

Compares the built-in `DateTime` path, the converter most often copied out of
[dotnet/runtime#1566](https://github.com/dotnet/runtime/issues/1566) (`reader.GetDateTime().ToUniversalTime()`),
and the prototype of the proposed option. Input is offset-less text on read and a `DateTimeKind.Unspecified`
value on write - the case #122962 describes.

Reproduce with `dotnet run --project bench/StjDateTimeZone.Bench -c Release`.

## What survives repetition, and what does not

This was measured on a developer laptop with other work running, and the noise shows: StdDev reaches 25% of
the mean in the default-job run below, and in that run `Write_ProposedUtc` came out *faster* than
`Write_BuiltIn`, which cannot be true - the prototype does strictly more work on the same path. Treat the
absolute numbers as a property of this machine, not of .NET.

**Robust across every run:** the copied converter costs 1.8-2.3x the built-in path on read and 1.3-2.2x on
write, because it performs a time-zone conversion for every value.

**Not resolvable here:** the difference between the built-in path and the proposed option. It lands inside
the noise in both runs, so no number is claimed for it. What is structural rather than measured: treating
offset-less text as UTC is a `DateTime.SpecifyKind`, not a conversion, so the option needs no zone lookup in
the scenario the issue is about. For `Kind=Local` values every implementation pays the same lookup.

## Default job (15+ iterations)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
Intel Core i7-14700HX 2.30GHz, 1 CPU, 28 logical and 20 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                 | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Read_BuiltIn           |  99.20 ns |  5.947 ns | 17.536 ns |  1.04 |    0.31 |      - |         - |          NA |
| Read_ThreadWorkaround  | 177.13 ns | 14.489 ns | 42.720 ns |  1.86 |    0.63 |      - |         - |          NA |
| Read_ProposedDefault   | 130.21 ns |  5.171 ns | 15.247 ns |  1.37 |    0.36 |      - |         - |          NA |
| Read_ProposedUtc       | 138.86 ns |  2.745 ns |  2.819 ns |  1.46 |    0.34 |      - |         - |          NA |
| Write_BuiltIn          | 163.85 ns |  8.203 ns | 24.187 ns |  1.72 |    0.48 | 0.0036 |      64 B |          NA |
| Write_ThreadWorkaround | 213.65 ns | 18.497 ns | 54.539 ns |  2.24 |    0.79 | 0.0038 |      72 B |          NA |
| Write_ProposedUtc      | 122.29 ns |  8.533 ns | 25.158 ns |  1.28 |    0.41 | 0.0042 |      72 B |          NA |

## ShortRun job (3 iterations), for comparison

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
