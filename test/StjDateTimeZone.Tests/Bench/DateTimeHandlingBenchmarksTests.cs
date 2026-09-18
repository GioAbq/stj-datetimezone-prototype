using System;
using System.Globalization;
using Shouldly;
using StjDateTimeZone.Bench;
using Xunit;

namespace StjDateTimeZone.Tests.Bench;

/// <summary>A benchmark that measures the wrong thing is worse than no benchmark, so pin what each case returns.</summary>
public sealed class DateTimeHandlingBenchmarksTests
{
    private static readonly DateTime Noon = new(2024, 6, 1, 12, 0, 0);

    [Fact]
    public void ReadCases_DifferOnlyInTheKindTheyProduce()
    {
        DateTimeHandlingBenchmarks benchmarks = new();

        benchmarks.Read_BuiltIn().ShouldBe(DateTime.SpecifyKind(Noon, DateTimeKind.Unspecified));
        benchmarks.Read_BuiltIn().Kind.ShouldBe(DateTimeKind.Unspecified);
        benchmarks.Read_ProposedDefault().Kind.ShouldBe(DateTimeKind.Unspecified);
        benchmarks.Read_ProposedUtc().Kind.ShouldBe(DateTimeKind.Utc);
        benchmarks.Read_ProposedUtc().ShouldBe(DateTime.SpecifyKind(Noon, DateTimeKind.Utc));
        benchmarks.Read_ThreadWorkaround().ShouldBe(DateTime.SpecifyKind(Noon, DateTimeKind.Local).ToUniversalTime());
    }

    [Fact]
    public void WriteCases_ProduceTheExpectedJson()
    {
        DateTimeHandlingBenchmarks benchmarks = new();

        benchmarks.Write_BuiltIn().ShouldBe("\"2024-06-01T12:00:00\"");
        benchmarks.Write_ProposedUtc().ShouldBe("\"2024-06-01T12:00:00Z\"");
        benchmarks.Write_ThreadWorkaround().ShouldBe("\"" + DateTime.SpecifyKind(Noon, DateTimeKind.Local).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + "Z\"");
    }
}
