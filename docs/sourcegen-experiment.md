# Experiment: can a parameterized converter replace the option?

One alternative to a `JsonSerializerOptions` property is to ship a public converter that takes the handling as a constructor argument, and let people register it themselves. This experiment checks whether such a converter can be used declaratively, which is what source generation requires.

`experiments/sourcegen-converter-ctor/Context.cs`:

```csharp
[JsonSourceGenerationOptions(Converters = [typeof(DateTimeZoneHandlingConverter)])]
[JsonSerializable(typeof(DateTime))]
public sealed partial class ExperimentContext : JsonSerializerContext
{
}
```

Reproduce with:

```
dotnet run --project experiments/sourcegen-converter-ctor -c Release
```

## Result

The build emits a **warning**, not an error:

```
experiments\sourcegen-converter-ctor\Context.cs(11,2): warning SYSLIB1220: The 'JsonConverterAttribute' type
'StjDateTimeZone.DateTimeZoneHandlingConverter' specified on member 'SourceGenConverterCtor.ExperimentContext'
is not a converter type or does not contain an accessible parameterless constructor.
(https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1220)
```

The application compiles, runs, and serializes a `DateTimeKind.Unspecified` value as:

```
"2024-06-01T12:00:00"
```

No `Z`. The converter was silently dropped, because the source generator only accepts converter types with an accessible parameterless constructor (`src/libraries/System.Text.Json/gen/JsonSourceGenerator.Parser.cs`, `GetConverterTypeFromAttribute`).

## Why it matters

A converter whose behavior comes from a constructor argument cannot be used from `[JsonSourceGenerationOptions]` or `[JsonConverter]` at all. Working around that means shipping one parameterless type per mode - which is exactly what `JsonStringEnumConverter<T>` had to do, and source generation still needed a separate `JsonSourceGenerationOptions.UseStringEnumConverter` switch on top of it.
