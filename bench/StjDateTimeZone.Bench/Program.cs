using BenchmarkDotNet.Running;

namespace StjDateTimeZone.Bench;

public static class Program
{
    // Default job on purpose: a ShortRun cannot resolve a one-percent difference, and the proposal quotes these numbers.
    public static void Main(string[] args) => BenchmarkRunner.Run<DateTimeHandlingBenchmarks>(null, args);
}
