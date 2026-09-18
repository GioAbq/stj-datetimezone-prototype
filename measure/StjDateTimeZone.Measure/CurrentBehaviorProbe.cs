using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using JsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings;
using NsDateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling;
using StjSerializer = System.Text.Json.JsonSerializer;

namespace StjDateTimeZone.Measure;

public static class CurrentBehaviorProbe
{
    private static readonly NsDateTimeZoneHandling[] NewtonsoftModes =
    [
        NsDateTimeZoneHandling.RoundtripKind,
        NsDateTimeZoneHandling.Utc,
        NsDateTimeZoneHandling.Local,
        NsDateTimeZoneHandling.Unspecified,
    ];

    public static ProbeReport Run() => new(
        DescribeEnvironment(),
        [SystemTextJson(), NewtonsoftJson(), OffsetLessDateTimeOffset(), ThreadWorkarounds(), ProposedSemantics()]);

    public static ProbeSection ProposedSemantics()
    {
        static JsonSerializerOptions Proposed(JsonDateTimeZoneHandling handling) => new()
        {
            Converters =
            {
                new DateTimeZoneHandlingConverter(handling),
                new DateTimeOffsetZoneHandlingConverter(handling),
            },
        };

        JsonDateTimeZoneHandling[] modes = [JsonDateTimeZoneHandling.RoundtripKind, JsonDateTimeZoneHandling.Utc, JsonDateTimeZoneHandling.Local];

        return new(
            "proposed JsonSerializerOptions.DateTimeZoneHandling (prototype)",
            [.. modes.Select(mode => new ReadProbe($"DateTimeZoneHandling = {mode}", s => StjSerializer.Deserialize<DateTime>(ProbeInputs.Quote(s), Proposed(mode))))],
            [.. modes.Select(mode => new WriteProbe($"DateTimeZoneHandling = {mode}", v => Unquote(StjSerializer.Serialize(v, Proposed(mode)))))],
            [.. modes.Select(mode => new OffsetReadProbe($"DateTimeOffset, DateTimeZoneHandling = {mode}", s => StjSerializer.Deserialize<DateTimeOffset>(ProbeInputs.Quote(s), Proposed(mode))))]);
    }

    public static ProbeSection SystemTextJson() => new(
        "System.Text.Json (no configuration exists)",
        [
            new("JsonSerializer.Deserialize<DateTime> (reflection)", s => StjSerializer.Deserialize<DateTime>(ProbeInputs.Quote(s))),
            new("JsonSerializer.Deserialize<DateTime?> (reflection)", s => StjSerializer.Deserialize<DateTime?>(ProbeInputs.Quote(s))!.Value),
            new("JsonSerializer.Deserialize<DateTime> (source generator)", s => StjSerializer.Deserialize(ProbeInputs.Quote(s), ProbeJsonContext.Default.DateTime)),
            new("JsonSerializer.Deserialize<DateTime?> (source generator)", s => StjSerializer.Deserialize(ProbeInputs.Quote(s), ProbeJsonContext.Default.NullableDateTime)!.Value),
            new("Dictionary<DateTime, int> key (reflection)", s => StjSerializer.Deserialize<Dictionary<DateTime, int>>(DictionaryJson(s))!.Keys.Single()),
            new("Utf8JsonReader.GetDateTime()", ReadWithUtf8JsonReader),
            new("JsonElement.GetDateTime()", ReadWithJsonElement),
            new("JsonNode.GetValue<DateTime>()", s => JsonNode.Parse(ProbeInputs.Quote(s))!.GetValue<DateTime>()),
        ],
        [
            new("JsonSerializer.Serialize<DateTime> (reflection)", v => Unquote(StjSerializer.Serialize(v))),
            new("JsonSerializer.Serialize<DateTime> (source generator)", v => Unquote(StjSerializer.Serialize(v, ProbeJsonContext.Default.DateTime))),
            new("Dictionary<DateTime, int> key (reflection)", v => FirstKey(StjSerializer.Serialize(new Dictionary<DateTime, int> { [v] = 1 }))),
            new("Utf8JsonWriter.WriteStringValue(DateTime)", WriteWithUtf8JsonWriter),
            new("JsonValue.Create(DateTime).ToJsonString()", v => Unquote(JsonValue.Create(v).ToJsonString())),
        ],
        []);

    public static ProbeSection NewtonsoftJson() => new(
        "Newtonsoft.Json DateTimeZoneHandling (prior art)",
        [
            .. NewtonsoftModes.Select(mode => new ReadProbe(
                $"JsonConvert.DeserializeObject<DateTime>, {mode}",
                s => JsonConvert.DeserializeObject<DateTime>(ProbeInputs.Quote(s), NewtonsoftSettings(mode)))),
            new("Dictionary<DateTime, int> key, Utc", s => JsonConvert.DeserializeObject<Dictionary<DateTime, int>>(DictionaryJson(s), NewtonsoftSettings(NsDateTimeZoneHandling.Utc))!.Keys.Single()),
        ],
        [
            .. NewtonsoftModes.Select(mode => new WriteProbe(
                $"JsonConvert.SerializeObject(DateTime), {mode}",
                v => Unquote(JsonConvert.SerializeObject(v, NewtonsoftSettings(mode))))),
            new("Dictionary<DateTime, int> key, Utc", v => FirstKey(JsonConvert.SerializeObject(new Dictionary<DateTime, int> { [v] = 1 }, NewtonsoftSettings(NsDateTimeZoneHandling.Utc)))),
        ],
        []);

    public static ProbeSection OffsetLessDateTimeOffset() => new(
        "Read DateTimeOffset",
        [],
        [],
        [
            new("System.Text.Json JsonSerializer.Deserialize<DateTimeOffset> (reflection)", s => StjSerializer.Deserialize<DateTimeOffset>(ProbeInputs.Quote(s))),
            new("System.Text.Json JsonSerializer.Deserialize<DateTimeOffset> (source generator)", s => StjSerializer.Deserialize(ProbeInputs.Quote(s), ProbeJsonContext.Default.DateTimeOffset)),
            .. NewtonsoftModes.Select(mode => new OffsetReadProbe(
                $"Newtonsoft.Json DeserializeObject<DateTimeOffset>, {mode}",
                s => JsonConvert.DeserializeObject<DateTimeOffset>(ProbeInputs.Quote(s), NewtonsoftSettings(mode)))),
        ]);

    public static ProbeSection ThreadWorkarounds()
    {
        JsonSerializerOptions mpashkovskiy = WithConverter(new MpashkovskiyUtcConverter());
        JsonSerializerOptions dalle = WithConverter(new DalleUtcConverter());
        JsonSerializerOptions specifyKind = WithConverter(new SpecifyKindUtcConverter());
        JsonSerializerOptions jgador = WithConverter(new JgadorUtcConverter());

        return new(
            "custom converters posted in dotnet/runtime#1566",
            [
                new("@mpashkovskiy converter", s => StjSerializer.Deserialize<DateTime>(ProbeInputs.Quote(s), mpashkovskiy)),
                new("@dalle converter", s => StjSerializer.Deserialize<DateTime>(ProbeInputs.Quote(s), dalle)),
                new("@dalle converter, Dictionary<DateTime, int> key", s => StjSerializer.Deserialize<Dictionary<DateTime, int>>(DictionaryJson(s), dalle)!.Keys.Single()),
                new("@amay5027 converter", s => StjSerializer.Deserialize<DateTime>(ProbeInputs.Quote(s), specifyKind)),
                new("@jgador converter", s => StjSerializer.Deserialize<DateTime>(ProbeInputs.Quote(s), jgador)),
            ],
            [
                new("@mpashkovskiy converter", v => Unquote(StjSerializer.Serialize(v, mpashkovskiy))),
                new("@dalle converter", v => Unquote(StjSerializer.Serialize(v, dalle))),
                new("@dalle converter, Dictionary<DateTime, int> key", v => FirstKey(StjSerializer.Serialize(new Dictionary<DateTime, int> { [v] = 1 }, dalle))),
                new("@amay5027 converter", v => Unquote(StjSerializer.Serialize(v, specifyKind))),
            ],
            []);
    }

    public static IReadOnlyList<string> DescribeEnvironment()
    {
        DateTime sample = ProbeInputs.Value(DateTimeKind.Unspecified);
        TimeSpan localOffset = TimeZoneInfo.Local.GetUtcOffset(sample);
        string sign = localOffset < TimeSpan.Zero ? "-" : "+";
        return
        [
            $"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.OSDescription})",
            $"System.Text.Json: {InformationalVersion(typeof(StjSerializer).Assembly)}",
            $"Newtonsoft.Json: {InformationalVersion(typeof(JsonConvert).Assembly)}",
            $"Machine time zone: {TimeZoneInfo.Local.Id}, UTC{sign}{localOffset.ToString(@"hh\:mm", CultureInfo.InvariantCulture)} on {FormatInvariant(sample)}",
        ];
    }

    public static string FormatInvariant(DateTime value) => value.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);

    private static string InformationalVersion(Assembly assembly)
        => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version!.ToString();

    private static DateTime ReadWithUtf8JsonReader(string input)
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(ProbeInputs.Quote(input)));
        reader.Read();
        return reader.GetDateTime();
    }

    private static DateTime ReadWithJsonElement(string input)
    {
        using JsonDocument document = JsonDocument.Parse(ProbeInputs.Quote(input));
        return document.RootElement.GetDateTime();
    }

    private static string WriteWithUtf8JsonWriter(DateTime value)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStringValue(value);
        }

        return Unquote(Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static JsonSerializerSettings NewtonsoftSettings(NsDateTimeZoneHandling mode) => new() { DateTimeZoneHandling = mode };

    private static JsonSerializerOptions WithConverter(JsonConverter converter) => new() { Converters = { converter } };

    private static string DictionaryJson(string key) => "{" + ProbeInputs.Quote(key) + ":1}";

    private static string Unquote(string json) => json.Trim('"');

    private static string FirstKey(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject().Single().Name;
    }
}
