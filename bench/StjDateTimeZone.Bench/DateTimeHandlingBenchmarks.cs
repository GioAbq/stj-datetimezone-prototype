using System;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using StjDateTimeZone.Measure;

namespace StjDateTimeZone.Bench;

/// <summary>
/// Checks the 2021 claim in dotnet/runtime#1566 that a hand-written converter is as fast as a built-in
/// feature would be, and measures what the proposed option costs when it is left at its default.
/// </summary>
[MemoryDiagnoser]
public class DateTimeHandlingBenchmarks
{
    private const string OffsetLessJson = "\"2024-06-01T12:00:00\"";

    private static readonly DateTime Value = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified);

    private static readonly JsonSerializerOptions BuiltIn = new();
    private static readonly JsonSerializerOptions ThreadWorkaround = new() { Converters = { new DalleUtcConverter() } };
    private static readonly JsonSerializerOptions ProposedDefault = new() { Converters = { new DateTimeZoneHandlingConverter(JsonDateTimeZoneHandling.RoundtripKind) } };
    private static readonly JsonSerializerOptions ProposedUtc = new() { Converters = { new DateTimeZoneHandlingConverter(JsonDateTimeZoneHandling.Utc) } };

    [Benchmark(Baseline = true)]
    public DateTime Read_BuiltIn() => JsonSerializer.Deserialize<DateTime>(OffsetLessJson, BuiltIn);

    [Benchmark]
    public DateTime Read_ThreadWorkaround() => JsonSerializer.Deserialize<DateTime>(OffsetLessJson, ThreadWorkaround);

    [Benchmark]
    public DateTime Read_ProposedDefault() => JsonSerializer.Deserialize<DateTime>(OffsetLessJson, ProposedDefault);

    [Benchmark]
    public DateTime Read_ProposedUtc() => JsonSerializer.Deserialize<DateTime>(OffsetLessJson, ProposedUtc);

    [Benchmark]
    public string Write_BuiltIn() => JsonSerializer.Serialize(Value, BuiltIn);

    [Benchmark]
    public string Write_ThreadWorkaround() => JsonSerializer.Serialize(Value, ThreadWorkaround);

    [Benchmark]
    public string Write_ProposedUtc() => JsonSerializer.Serialize(Value, ProposedUtc);
}
