# System.Text.Json `DateTimeZoneHandling` prototype

Supporting material for the API proposal in [dotnet/runtime#1566](https://github.com/dotnet/runtime/issues/1566) - a proposed `JsonSerializerOptions.DateTimeZoneHandling` option, with a matching property on `JsonSourceGenerationOptionsAttribute`.

This is **not** a fork of dotnet/runtime. It is a standalone prototype that implements the proposed semantics through public `JsonConverter<T>` types, so the behavior can be run, tested and benchmarked without building the runtime. If the shape is accepted, the same semantics and tests move into `System.Text.Json` itself.

## What it demonstrates

| Question | Where it is answered |
|---|---|
| What does System.Text.Json do with date-time text today? | [`docs/current-behavior.md`](docs/current-behavior.md), produced by running `measure/` |
| What would the proposed option do? | the last section of the same file, produced by the prototype |
| Does it match Newtonsoft.Json? | `test/StjDateTimeZone.Tests/Prototype/NewtonsoftParityTests.cs`, asserted against Newtonsoft.Json 13.0.4 |
| Are the converters posted in the issue correct? | `test/StjDateTimeZone.Tests/Measure/CurrentBehaviorProbeTests.cs` - each one is wrong in a different case |
| What does a converter cost compared to a built-in option? | [`docs/benchmark.md`](docs/benchmark.md) |
| Can a parameterized converter replace the option? | [`docs/sourcegen-experiment.md`](docs/sourcegen-experiment.md) - no, the source generator drops it with `SYSLIB1220` |
| How does it behave across a DST gap or an ambiguous hour? | `test/StjDateTimeZone.Tests/Prototype/DateTimeZoneNormalizerTests.cs` |

The proposal text itself is in [`proposal/issue-1566-comment.md`](proposal/issue-1566-comment.md).

## Layout

| Path | Contents |
|---|---|
| `src/StjDateTimeZone` | the proposed enum, the normalization rule and the two converters |
| `measure/StjDateTimeZone.Measure` | console app that prints the behavior tables used in the proposal |
| `test/StjDateTimeZone.Tests` | 84 tests: current behavior, proposed semantics, Newtonsoft parity, reader/writer limits |
| `bench/StjDateTimeZone.Bench` | BenchmarkDotNet comparison of built-in, workaround and proposed paths |
| `experiments/sourcegen-converter-ctor` | deliberately warns: the "just ship a converter" alternative, tried |

## Running it

Requires the .NET 10 SDK (measured with 10.0.401).

```
dotnet test --solution StjDateTimeZone.slnx -c Release
```

```
dotnet run --project measure/StjDateTimeZone.Measure -c Release
```

```
dotnet run --project bench/StjDateTimeZone.Bench -c Release
```

Results that depend on the machine time zone say so in the output; the tests that must be deterministic pin an explicit `Europe/Warsaw` zone instead of the machine one. The published tables were produced on a machine at UTC+02:00.

## License

MIT - see [LICENSE](LICENSE).
