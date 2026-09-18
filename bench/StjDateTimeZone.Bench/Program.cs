using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace StjDateTimeZone.Bench;

public static class Program
{
    public static void Main(string[] args)
        => BenchmarkRunner.Run<DateTimeHandlingBenchmarks>(
            DefaultConfig.Instance.AddJob(Job.ShortRun.WithId("short")),
            args);
}
