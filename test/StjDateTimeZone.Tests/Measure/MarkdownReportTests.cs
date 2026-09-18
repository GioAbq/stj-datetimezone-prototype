using System;
using Shouldly;
using StjDateTimeZone.Measure;
using Xunit;

namespace StjDateTimeZone.Tests.Measure;

public sealed class MarkdownReportTests
{
    [Fact]
    public void Render_WritesEnvironmentAndOneTablePerNonEmptyProbeList()
    {
        ProbeReport report = new(
            ["Runtime: test"],
            [
                new("Sample",
                    [new ReadProbe("reader", _ => new DateTime(2024, 1, 2, 3, 4, 0, DateTimeKind.Utc))],
                    [new WriteProbe("writer", v => v.Kind.ToString())],
                    []),
            ]);

        string markdown = Render(report);

        markdown.ShouldStartWith("- Runtime: test\n");
        markdown.ShouldContain("### Read DateTime - Sample\n\n| API | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00-05:00` | `2024-06-01T12:00:00` |\n|---|---|---|---|\n");
        markdown.ShouldContain("| reader | `2024-01-02T03:04` Utc | `2024-01-02T03:04` Utc | `2024-01-02T03:04` Utc |");
        markdown.ShouldContain("### Write DateTime - Sample\n\n| API | Kind=Utc | Kind=Local | Kind=Unspecified |");
        markdown.ShouldContain("| writer | `Utc` | `Local` | `Unspecified` |");
        markdown.ShouldNotContain("### Sample");
    }

    [Fact]
    public void Render_UsesSectionTitleForOffsetReads()
    {
        ProbeReport report = new(
            [],
            [new("Offsets", [], [], [new OffsetReadProbe("offset", _ => new DateTimeOffset(2024, 1, 2, 3, 4, 0, TimeSpan.FromHours(-5)))])]);

        string markdown = Render(report);

        markdown.ShouldContain("### Offsets\n");
        markdown.ShouldContain("| offset | `2024-01-02T03:04-05:00` | `2024-01-02T03:04-05:00` | `2024-01-02T03:04-05:00` |");
        markdown.ShouldNotContain("Read DateTime");
        markdown.ShouldNotContain("Write DateTime");
    }

    [Fact]
    public void Cell_ReportsExceptionTypeInsteadOfThrowing()
        => MarkdownReport.Cell(() => throw new FormatException()).ShouldBe("throws FormatException");

    [Fact]
    public void Cell_ReturnsProducedValue()
        => MarkdownReport.Cell(() => "value").ShouldBe("value");

    [Fact]
    public void FormatRead_AppendsKind()
        => MarkdownReport.FormatRead(new DateTime(2024, 6, 1, 19, 0, 0, DateTimeKind.Local)).ShouldBe("`2024-06-01T19:00` Local");

    [Fact]
    public void FormatOffsetRead_IncludesOffset()
        => MarkdownReport.FormatOffsetRead(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.FromHours(2))).ShouldBe("`2024-06-01T12:00+02:00`");

    private static string Render(ProbeReport report) => MarkdownReport.Render(report).ReplaceLineEndings("\n");
}
