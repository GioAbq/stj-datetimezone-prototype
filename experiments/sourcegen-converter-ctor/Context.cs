using System;
using System.Text.Json.Serialization;
using StjDateTimeZone;

namespace SourceGenConverterCtor;

// Experiment for dotnet/runtime#1566: can the "just ship a public converter" alternative be used
// declaratively? The converter needs a constructor argument to know which handling to apply, and the
// source generator only accepts converter types with an accessible parameterless constructor
// (gen/JsonSourceGenerator.Parser.cs:3034-3040).
[JsonSourceGenerationOptions(Converters = [typeof(DateTimeZoneHandlingConverter)])]
[JsonSerializable(typeof(DateTime))]
public sealed partial class ExperimentContext : JsonSerializerContext
{
}
