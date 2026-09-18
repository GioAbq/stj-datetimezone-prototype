using System;
using System.IO;
using Shouldly;
using StjDateTimeZone.Measure;
using Xunit;

namespace StjDateTimeZone.Tests.Measure;

public sealed class ProgramTests
{
    [Fact]
    public void Main_PrintsTheCurrentBehaviorReport()
    {
        TextWriter original = Console.Out;
        using StringWriter output = new();
        Console.SetOut(output);
        try
        {
            Program.Main().ShouldBe(0);
        }
        finally
        {
            Console.SetOut(original);
        }

        string report = output.ToString();
        report.ShouldStartWith("- Runtime: ");
        report.ShouldContain("### Read DateTime - System.Text.Json (no configuration exists)");
        report.ShouldContain("### Write DateTime - custom converters posted in dotnet/runtime#1566");
        report.ShouldNotContain("throws ");
    }
}
