using System;
using System.IO;
using Shouldly;
using StjDateTimeZone.Bench;
using Xunit;

namespace StjDateTimeZone.Tests.Bench;

[Collection(ConsoleCollection.Name)]
public sealed class BenchmarkProgramTests
{
    [Fact]
    public void Main_ListsEveryBenchmarkWithoutRunningAny()
    {
        TextWriter original = Console.Out;
        using StringWriter output = new();
        Console.SetOut(output);
        try
        {
            Program.Main(["--list", "flat"]);
        }
        finally
        {
            Console.SetOut(original);
        }

        string listing = output.ToString();
        foreach (string name in new[]
                 {
                     "Read_BuiltIn", "Read_ThreadWorkaround", "Read_ProposedDefault", "Read_ProposedUtc",
                     "Write_BuiltIn", "Write_ThreadWorkaround", "Write_ProposedUtc",
                 })
        {
            listing.ShouldContain(name);
        }
    }
}
