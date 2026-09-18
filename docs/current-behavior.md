# Measured behavior

Output of `dotnet run --project measure/StjDateTimeZone.Measure -c Release`. Every table in the
[proposal](../proposal/issue-1566-comment.md) comes from this run, not from a description of the behavior.

- Runtime: .NET 10.0.12 (Microsoft Windows 10.0.26200)
- System.Text.Json: 10.0.12+95017c711e6afc1085133d440e42b4bd78155701
- Newtonsoft.Json: 13.0.4+4e13299d4b0ec96bd4df9954ef646bd2d1b5bf2a
- Machine time zone: Central European Standard Time, UTC+02:00 on 2024-06-01T12:00

### Read DateTime - System.Text.Json (no configuration exists)

| API | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00-05:00` | `2024-06-01T12:00:00` |
|---|---|---|---|
| JsonSerializer.Deserialize<DateTime> (reflection) | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| JsonSerializer.Deserialize<DateTime?> (reflection) | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| JsonSerializer.Deserialize<DateTime> (source generator) | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| JsonSerializer.Deserialize<DateTime?> (source generator) | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| Dictionary<DateTime, int> key (reflection) | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| Utf8JsonReader.GetDateTime() | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| JsonElement.GetDateTime() | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| JsonNode.GetValue<DateTime>() | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |

### Write DateTime - System.Text.Json (no configuration exists)

| API | Kind=Utc | Kind=Local | Kind=Unspecified |
|---|---|---|---|
| JsonSerializer.Serialize<DateTime> (reflection) | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00` |
| JsonSerializer.Serialize<DateTime> (source generator) | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00` |
| Dictionary<DateTime, int> key (reflection) | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00` |
| Utf8JsonWriter.WriteStringValue(DateTime) | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00` |
| JsonValue.Create(DateTime).ToJsonString() | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00` |

### Read DateTime - Newtonsoft.Json DateTimeZoneHandling (prior art)

| API | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00-05:00` | `2024-06-01T12:00:00` |
|---|---|---|---|
| JsonConvert.DeserializeObject<DateTime>, RoundtripKind | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| JsonConvert.DeserializeObject<DateTime>, Utc | `2024-06-01T12:00` Utc | `2024-06-01T17:00` Utc | `2024-06-01T12:00` Utc |
| JsonConvert.DeserializeObject<DateTime>, Local | `2024-06-01T14:00` Local | `2024-06-01T19:00` Local | `2024-06-01T12:00` Local |
| JsonConvert.DeserializeObject<DateTime>, Unspecified | `2024-06-01T12:00` Unspecified | `2024-06-01T19:00` Unspecified | `2024-06-01T12:00` Unspecified |
| Dictionary<DateTime, int> key, Utc | `2024-06-01T12:00` Utc | `2024-06-01T17:00` Utc | `2024-06-01T12:00` Utc |

### Write DateTime - Newtonsoft.Json DateTimeZoneHandling (prior art)

| API | Kind=Utc | Kind=Local | Kind=Unspecified |
|---|---|---|---|
| JsonConvert.SerializeObject(DateTime), RoundtripKind | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00` |
| JsonConvert.SerializeObject(DateTime), Utc | `2024-06-01T12:00:00Z` | `2024-06-01T10:00:00Z` | `2024-06-01T12:00:00Z` |
| JsonConvert.SerializeObject(DateTime), Local | `2024-06-01T14:00:00+02:00` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00+02:00` |
| JsonConvert.SerializeObject(DateTime), Unspecified | `2024-06-01T12:00:00` | `2024-06-01T12:00:00` | `2024-06-01T12:00:00` |
| Dictionary<DateTime, int> key, Utc | `2024-06-01T12:00:00Z` | `2024-06-01T10:00:00Z` | `2024-06-01T12:00:00Z` |

### Read DateTimeOffset

| API | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00-05:00` | `2024-06-01T12:00:00` |
|---|---|---|---|
| System.Text.Json JsonSerializer.Deserialize<DateTimeOffset> (reflection) | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+02:00` |
| System.Text.Json JsonSerializer.Deserialize<DateTimeOffset> (source generator) | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+02:00` |
| Newtonsoft.Json DeserializeObject<DateTimeOffset>, RoundtripKind | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+02:00` |
| Newtonsoft.Json DeserializeObject<DateTimeOffset>, Utc | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+02:00` |
| Newtonsoft.Json DeserializeObject<DateTimeOffset>, Local | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+02:00` |
| Newtonsoft.Json DeserializeObject<DateTimeOffset>, Unspecified | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+02:00` |

### Read DateTime - custom converters posted in dotnet/runtime#1566

| API | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00-05:00` | `2024-06-01T12:00:00` |
|---|---|---|---|
| @mpashkovskiy converter | `2024-06-01T14:00` Local | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| @dalle converter | `2024-06-01T12:00` Utc | `2024-06-01T17:00` Utc | `2024-06-01T10:00` Utc |
| @dalle converter, Dictionary<DateTime, int> key | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| @amay5027 converter | `2024-06-01T14:00` Local | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| @jgador converter | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Utc | `2024-06-01T12:00` Utc |

### Write DateTime - custom converters posted in dotnet/runtime#1566

| API | Kind=Utc | Kind=Local | Kind=Unspecified |
|---|---|---|---|
| @mpashkovskiy converter | `2024-06-01T12:00:00Z` | `2024-06-01T10:00:00Z` | `2024-06-01T10:00:00Z` |
| @dalle converter | `2024-06-01T12:00:00Z` | `2024-06-01T10:00:00Z` | `2024-06-01T10:00:00Z` |
| @dalle converter, Dictionary<DateTime, int> key | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00` |
| @amay5027 converter | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00Z` |

### Read DateTime - proposed JsonSerializerOptions.DateTimeZoneHandling (prototype)

| API | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00-05:00` | `2024-06-01T12:00:00` |
|---|---|---|---|
| DateTimeZoneHandling = RoundtripKind | `2024-06-01T12:00` Utc | `2024-06-01T19:00` Local | `2024-06-01T12:00` Unspecified |
| DateTimeZoneHandling = Utc | `2024-06-01T12:00` Utc | `2024-06-01T17:00` Utc | `2024-06-01T12:00` Utc |
| DateTimeZoneHandling = Local | `2024-06-01T14:00` Local | `2024-06-01T19:00` Local | `2024-06-01T12:00` Local |

### Write DateTime - proposed JsonSerializerOptions.DateTimeZoneHandling (prototype)

| API | Kind=Utc | Kind=Local | Kind=Unspecified |
|---|---|---|---|
| DateTimeZoneHandling = RoundtripKind | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00` |
| DateTimeZoneHandling = Utc | `2024-06-01T12:00:00Z` | `2024-06-01T10:00:00Z` | `2024-06-01T12:00:00Z` |
| DateTimeZoneHandling = Local | `2024-06-01T14:00:00+02:00` | `2024-06-01T12:00:00+02:00` | `2024-06-01T12:00:00+02:00` |

### proposed JsonSerializerOptions.DateTimeZoneHandling (prototype)

| API | `2024-06-01T12:00:00Z` | `2024-06-01T12:00:00-05:00` | `2024-06-01T12:00:00` |
|---|---|---|---|
| DateTimeOffset, DateTimeZoneHandling = RoundtripKind | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+02:00` |
| DateTimeOffset, DateTimeZoneHandling = Utc | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+00:00` |
| DateTimeOffset, DateTimeZoneHandling = Local | `2024-06-01T12:00+00:00` | `2024-06-01T12:00-05:00` | `2024-06-01T12:00+02:00` |
