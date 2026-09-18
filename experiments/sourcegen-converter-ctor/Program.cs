using System;
using System.Text.Json;

namespace SourceGenConverterCtor;

/// <summary>
/// Second half of the experiment: the build only warns (SYSLIB1220), so does the converter actually apply?
/// If it did, an Unspecified value would be written with a "Z" by the Utc handling.
/// </summary>
public static class Program
{
    public static int Main()
    {
        string json = JsonSerializer.Serialize(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified), ExperimentContext.Default.DateTime);
        Console.WriteLine(json);
        return 0;
    }
}
