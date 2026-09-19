using Xunit;

namespace StjDateTimeZone.Tests;

/// <summary>Console.SetOut is process-wide, so every test that captures it must not run beside another one.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCollection
{
    public const string Name = "console";
}
