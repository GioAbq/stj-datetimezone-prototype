using System;
using System.IO;
using System.Text.Json;
using Shouldly;
using SourceGenConverterCtor;
using Xunit;

namespace StjDateTimeZone.Tests.Experiments;

/// <summary>
/// Pins the finding the proposal's "Alternative Designs" rests on: a converter that needs a constructor argument
/// is dropped by the source generator (SYSLIB1220, a warning), so the value is written without a Z.
/// If a future generator starts honoring it, these go red and the proposal text has to change.
/// </summary>
[Collection(ConsoleCollection.Name)]
public sealed class SourceGenConverterCtorTests
{
    [Fact]
    public void ParameterizedConverter_IsSilentlyIgnoredBySourceGeneration()
    {
        DateTime unspecified = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified);

        JsonSerializer.Serialize(unspecified, ExperimentContext.Default.DateTime).ShouldBe("\"2024-06-01T12:00:00\"");
    }

    [Fact]
    public void Main_PrintsTheUnchangedValue()
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

        output.ToString().Trim().ShouldBe("\"2024-06-01T12:00:00\"");
    }
}
