@jeffhandley Here is a revised proposal covering this thread and the scenario from #122962.

Everything below was measured on .NET 10.0.12 (SDK 10.0.401), Windows, with the machine time zone at UTC+02:00. The prototype that produced the "proposed" rows, its 84 tests and the benchmark are linked at the end.

## Background and motivation

Two concrete drivers, both raised here:

1. **Date-time text without a zone, from producers that are not .NET** (#122962, @wdnijdam): the payload carries `2024-06-01T12:00:00`, the values are known to be UTC, and there is no way to tell the serializer that. They deserialize as `DateTimeKind.Unspecified` and every later `ToUniversalTime()` in the application silently shifts them by the server's offset.
2. **Json.NET migrations** (@btecu, @xenod, @maulik-modi, @danilobreda, @seekingtheoptimal): `DateTimeZoneHandling = Utc` guaranteed a `Z` on the wire whatever `Kind` the data layer produced. `DateTime` values arriving from an ORM or a SQL `datetime` column are `Unspecified`, so today the same payload goes out without a suffix.

### Current behavior

`JsonSerializer` (reflection and source generation), `DateTime?`, `Dictionary<DateTime, T>` keys, `Utf8JsonReader.GetDateTime()`, `JsonElement.GetDateTime()` and `JsonNode.GetValue<DateTime>()` all agree:

| JSON | `DateTime` | `DateTimeOffset` |
|---|---|---|
| `"2024-06-01T12:00:00Z"` | `12:00` `Utc` | `12:00+00:00` |
| `"2024-06-01T12:00:00-05:00"` | `19:00` `Local` (machine zone) | `12:00-05:00` |
| `"2024-06-01T12:00:00"` | `12:00` `Unspecified` | `12:00+02:00` (machine offset) |

| `DateTime` value | written as |
|---|---|
| `Kind=Utc` | `2024-06-01T12:00:00Z` |
| `Kind=Local` | `2024-06-01T12:00:00+02:00` |
| `Kind=Unspecified` | `2024-06-01T12:00:00` |

The last row of the first table is why "use `DateTimeOffset` instead" does not answer driver 1: offset-less text silently takes the *reading* machine's offset, which is the bug the reporter is trying to avoid. Newtonsoft.Json 13.0.4 behaves the same way here, in every `DateTimeZoneHandling` mode.

### The workarounds in this thread do not hold

Measured with the converters exactly as posted:

| Converter | Measured defect |
|---|---|
| [@dalle](https://github.com/dotnet/runtime/issues/1566#issuecomment-745201271) `reader.GetDateTime().ToUniversalTime()` | Offset-less input is shifted by the machine offset: `12:00` becomes `10:00Z`. Writing an `Unspecified` value shifts it too. |
| [@amay5027](https://github.com/dotnet/runtime/issues/1566#issuecomment-833331501) `DateTime.Parse` + `SpecifyKind` | Reading `...Z` yields `14:00` `Local`; writing a `Local` value publishes `12:00:00Z` for an instant that is `10:00Z`. |
| [@jgador](https://github.com/dotnet/runtime/issues/1566#issuecomment-2408664669) `SpecifyKind` unless already UTC | Offset-bearing input yields the wrong instant: `12:00-05:00` becomes `19:00Z` instead of `17:00Z`. |

Two of these are among the most upvoted comments in the thread (30 and 13 reactions), and the third was posted as a correction to the first. Each is wrong in a different case. Seven years of people copying them is the strongest argument for putting the policy in the box.

### Three gaps a user-supplied converter cannot close

- **Dictionary keys.** A converter that does not override `ReadAsPropertyName`/`WriteAsPropertyName` silently falls back to the built-in converter for `Dictionary<DateTime, T>` keys, so the normalization does not happen. When it does override them, `Utf8JsonReader.GetDateTime()` throws on a `PropertyName` token and `Utf8JsonWriter.WritePropertyName(DateTime)` is `internal`, so the key has to be materialized as a string, re-parsed with `DateTime.Parse` and re-formatted by hand to match the trimmed ISO 8601 shape.
- **`JsonNode`.** `JsonValue.Create(dateTime).WriteTo(writer, options)` writes through the converter captured when the node was created, so `options.Converters` never reaches it. An option read by the built-in converter would.
- **Cost.** The conversion, not the converter dispatch, is what costs (BenchmarkDotNet, ShortRun job, .NET 10.0.12, single machine, offset-less input / `Unspecified` value; the read and write blocks each use their own built-in baseline):

| Case | Mean | Ratio |
|---|---|---|
| Read, built-in | 38.9 ns | 1.00 |
| Read, proposed option at its default | 39.3 ns | 1.01 |
| Read, proposed option set to `Utc` | 40.7 ns | 1.05 |
| Read, @dalle converter | 74.0 ns | 1.90 |
| Write, built-in | 55.4 ns | 1.00 |
| Write, proposed option set to `Utc` | 55.7 ns | 1.01 |
| Write, @dalle converter | 92.6 ns | 1.67 |

The workaround pays for a time-zone conversion on every value. The proposed semantics do not need one for the case #122962 describes, because "this offset-less text is already UTC" is a `SpecifyKind`, not a conversion. For `Kind=Local` values any implementation pays the same zone lookup.

## API Proposal

```csharp
namespace System.Text.Json.Serialization;

public enum JsonDateTimeZoneHandling
{
    RoundtripKind = 0, // today's behavior
    Utc = 1,
    Local = 2,
}

public partial class JsonSourceGenerationOptionsAttribute
{
    public JsonDateTimeZoneHandling DateTimeZoneHandling { get; set; }
}

namespace System.Text.Json;

public partial class JsonSerializerOptions
{
    // EXISTING
    // public JsonNumberHandling NumberHandling { get; set; }

    public JsonDateTimeZoneHandling DateTimeZoneHandling { get; set; }
}
```

The `Json` prefix on the enum is deliberate: `Newtonsoft.Json.DateTimeZoneHandling` already exists, and a project mid-migration imports both namespaces. An unprefixed name would be a source breaking change (CS0104) for exactly the audience this feature serves.

## API Usage

```csharp
// Driver 1: external services send local-looking text that is really UTC.
var options = new JsonSerializerOptions { DateTimeZoneHandling = JsonDateTimeZoneHandling.Utc };

var reading = JsonSerializer.Deserialize<Reading>("""{"takenAt":"2024-06-01T12:00:00"}""", options);
// reading.TakenAt == 2024-06-01T12:00:00, Kind == Utc

// Driver 2: whatever the data layer produced goes out with a Z.
JsonSerializer.Serialize(new Reading { TakenAt = fromDatabase }, options);
// {"takenAt":"2024-06-01T12:00:00Z"}
```

```csharp
[JsonSourceGenerationOptions(DateTimeZoneHandling = JsonDateTimeZoneHandling.Utc)]
[JsonSerializable(typeof(Reading))]
public partial class ReadingContext : JsonSerializerContext { }
```

## Semantics

Produced by running the prototype, not by description:

| | `RoundtripKind` (default) | `Utc` | `Local` |
|---|---|---|---|
| read `...Z` | `12:00` `Utc` | `12:00` `Utc` | `14:00` `Local` |
| read `...-05:00` | `19:00` `Local` | `17:00` `Utc` | `19:00` `Local` |
| read `...` (no zone) | `12:00` `Unspecified` | `12:00` `Utc` | `12:00` `Local` |
| write `Kind=Utc` | `...12:00:00Z` | `...12:00:00Z` | `...14:00:00+02:00` |
| write `Kind=Local` | `...12:00:00+02:00` | `...10:00:00Z` | `...12:00:00+02:00` |
| write `Kind=Unspecified` | `...12:00:00` | `...12:00:00Z` | `...12:00:00+02:00` |

This matches Newtonsoft.Json 13.0.4 cell for cell in the `Utc`, `Local` and `RoundtripKind` modes; the prototype's parity tests assert that against the real package rather than against a description of it.

Scope:

- `DateTime?` and `Dictionary<DateTime, T>` keys follow the values.
- `DateTimeOffset` **writing** never changes; only offset-less text on read is affected (see Open questions).
- `DateOnly`/`TimeOnly` have no zone component and are untouched.
- `Utf8JsonReader`/`Utf8JsonWriter`, `JsonDocument`/`JsonElement` and values parsed into `JsonNode` are untouched, since they take no `JsonSerializerOptions` - the same boundary `NumberHandling` already has.
- `JsonSerializerDefaults.Web` and `.Strict` keep `RoundtripKind`.

## Alternative Designs

- **A public parameterized converter instead of an option.** It cannot be used declaratively, because the handling has to come in through a constructor argument: the source generator only accepts converter types with an accessible parameterless constructor. I tried it - the build reports `SYSLIB1220` as a *warning*, the app compiles and runs, and the value comes out as `"2024-06-01T12:00:00"`, i.e. the converter was silently dropped. Shipping one parameterless type per mode brings back the key and `JsonNode` gaps listed above. `JsonStringEnumConverter<T>` had to go generic for the same reason, and source generation still needed `JsonSourceGenerationOptions.UseStringEnumConverter` on top.
- **`DateTimeUnspecifiedKindHandling { AssumeUtc, AssumeLocal }`** as proposed in #122962. It answers driver 1 but leaves driver 2 untouched: `Kind=Local` values still go out with an offset and offset-bearing input still lands as machine-local time.
- **Full Json.NET parity, including its `Unspecified` member.** Measured, that mode converts an offset to the machine's local time and then drops `Kind`, so the stored value depends on the server that read it. Reproducing that seemed worse than omitting it; it can be added later without breaking anyone.
- **A per-member `[JsonDateTimeZoneHandling]` attribute**, mirroring `JsonNumberHandling`. Nothing in seven years of this thread asks for per-property control - every request is an application-wide policy - so this is left out of v1 and stays additive.

## Risks

- **Behavioral compatibility**: `RoundtripKind = 0` is today's behavior, so a default `JsonSerializerOptions` is unchanged. The addition is binary and source additive; `System.Text.Json` also ships netstandard2.0 and net462, where `TimeZoneInfo` is available, so the surface stays a superset.
- **`Local` inherits the hazards of machine-local time.** In the Europe/Warsaw gap (2024-03-31, 02:00 -> 03:00), an offset-less `02:30` is assumed local, gets the daylight offset and comes back as `01:30` - an hour *before* the value that went in. In the ambiguous hour (2024-10-27) `02:30` resolves to the standard-time reading, `01:30Z`. This is inherent to `DateTime` + local time, is what Json.NET does today, and is an argument for documenting `Local` as the compatibility mode rather than the recommended one.
- **Range edges**: converting near `DateTime.MinValue`/`MaxValue` saturates, as `ToUniversalTime()`/`ToLocalTime()` already do. Note that `Local` output near the minimum (`0001-01-01T00:00:00+02:00`) cannot be read back - that is #70547, which this proposal neither fixes nor worsens.
- **Implementation surface**: the converter for `DateTime`/`DateTimeOffset`, the options field with setter validation, the copy constructor and the caching equality comparer, plus the source-generator parser and emitter for the attribute. The reading path should convert offset-bearing text to UTC directly rather than through machine-local time, so values near the range edges are not clamped on the way.

## Open questions

1. **Should offset-less text follow the option when the target is `DateTimeOffset`?** The prototype says yes (`Utc` gives `+00:00` instead of the machine offset), because that is the same failure the option exists to remove and it cannot be widened later without changing behavior for people who already opted in. Json.NET says no - its setting does not touch `DateTimeOffset` at all.
2. **Do `JsonSerializerDefaults.Web` and `.Strict` stay on `RoundtripKind`?** Tentatively yes; `Strict` could reasonably adopt `Utc` later, but that is a separate discussion.
3. **Per-member attribute now or later?** Tentatively later, as argued above.
4. **`DateTimeZoneHandling` or `DateTimeKindHandling`?** The first matches the name people are migrating from and searching for; the second describes what it actually manipulates.

## Usage in dotnet/runtime

Searching the VMR for `JsonConverter<DateTime>` and `JsonConverter<DateTimeOffset>` implementations turns up only test converters (for example `ReadScenarioTests.MyDateTimeConverter`), so there are no adoption sites in product code. Outside the repo the pattern is visible - OpenTelemetry's OneCollector exporter and `Microsoft.IdentityModel.Tokens` both call `ToUniversalTime()` before `WriteStringValue` - but both write through `Utf8JsonWriter` directly, which this option does not reach. So: no adoption sites inside dotnet/runtime, which is a signal worth stating rather than hiding.

## Prototype

The semantics, the parity with Newtonsoft.Json 13.0.4, the workaround defects, the `SYSLIB1220` result and the benchmark above all come from a prototype outside the repo: https://github.com/GioAbq/stj-datetimezone-prototype (converters implementing the proposed semantics, 84 tests against Newtonsoft.Json 13.0.4 and the built-in behavior, BenchmarkDotNet; the tables above are its `docs/current-behavior.md`). Happy to move it to a branch on a fork of dotnet/runtime, with `GenerateReferenceAssemblySource` output and the tests moved into `System.Text.Json.Tests`, if the shape above looks worth pursuing.

> [!NOTE]
> This comment was drafted with AI assistance (Claude Code). The measurements, prototype and test results it cites were produced by running the code described above.
