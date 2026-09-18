using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Shouldly;
using StjDateTimeZone;
using Xunit;

namespace StjDateTimeZone.Tests.Prototype;

public sealed class ConverterSemanticsTests
{
    private const string UtcText = "\"2024-06-01T12:00:00Z\"";
    private const string OffsetText = "\"2024-06-01T12:00:00-05:00\"";
    private const string NoOffsetText = "\"2024-06-01T12:00:00\"";

    private static readonly DateTime OffsetInputAsUtc = new(2024, 6, 1, 17, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Utc_Read_NormalizesEveryInputShape()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.Utc);

        JsonSerializer.Deserialize<DateTime>(UtcText, options).ShouldBe(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        JsonSerializer.Deserialize<DateTime>(OffsetText, options).ShouldBe(OffsetInputAsUtc);
        JsonSerializer.Deserialize<DateTime>(NoOffsetText, options).ShouldBe(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        JsonSerializer.Deserialize<DateTime>(NoOffsetText, options).Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Utc_Read_WorksForNullableAndDictionaryKeys()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.Utc);

        JsonSerializer.Deserialize<DateTime?>(NoOffsetText, options)!.Value.Kind.ShouldBe(DateTimeKind.Utc);

        DateTime key = JsonSerializer.Deserialize<Dictionary<DateTime, int>>("{\"2024-06-01T12:00:00-05:00\":1}", options)!.Keys.Single();
        key.ShouldBe(OffsetInputAsUtc);
        key.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Utc_Write_NormalizesValuesAndDictionaryKeys()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.Utc);
        DateTime local = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Local);

        JsonSerializer.Serialize(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified), options).ShouldBe("\"2024-06-01T12:00:00Z\"");
        JsonSerializer.Serialize(local, options).ShouldBe("\"" + Iso(local.ToUniversalTime()) + "Z\"");
        JsonSerializer.Serialize(new Dictionary<DateTime, int> { [new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified)] = 1 }, options)
            .ShouldBe("{\"2024-06-01T12:00:00Z\":1}");
    }

    [Fact]
    public void Local_Read_And_Write_MirrorTheUtcMode()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.Local);

        DateTime fromUtcText = JsonSerializer.Deserialize<DateTime>(UtcText, options);
        fromUtcText.Kind.ShouldBe(DateTimeKind.Local);
        fromUtcText.ShouldBe(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc).ToLocalTime());

        DateTime unspecified = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified);
        JsonSerializer.Serialize(unspecified, options).ShouldBe("\"" + Iso(unspecified) + LocalOffsetSuffix(unspecified) + "\"");
    }

    [Fact]
    public void RoundtripKind_KeepsTodaysBehavior()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.RoundtripKind);

        JsonSerializer.Deserialize<DateTime>(NoOffsetText, options).Kind.ShouldBe(DateTimeKind.Unspecified);
        JsonSerializer.Deserialize<DateTime>(NoOffsetText, options).ShouldBe(JsonSerializer.Deserialize<DateTime>(NoOffsetText));
        JsonSerializer.Serialize(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified), options).ShouldBe("\"2024-06-01T12:00:00\"");
    }

    [Fact]
    public void Utc_SurvivesARoundTrip()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.Utc);

        foreach (string json in new[] { UtcText, OffsetText, NoOffsetText })
        {
            DateTime first = JsonSerializer.Deserialize<DateTime>(json, options);
            string written = JsonSerializer.Serialize(first, options);
            JsonSerializer.Deserialize<DateTime>(written, options).ShouldBe(first);
            written.ShouldEndWith("Z\"");
        }
    }

    [Fact]
    public void Utc_AppliesToSourceGeneratedContracts()
    {
        PrototypeJsonContext context = new(Options(JsonDateTimeZoneHandling.Utc));

        DateTime value = JsonSerializer.Deserialize(NoOffsetText, context.DateTime);

        value.Kind.ShouldBe(DateTimeKind.Utc);
        JsonSerializer.Serialize(value, context.DateTime).ShouldBe("\"2024-06-01T12:00:00Z\"");
    }

    [Fact]
    public void JsonNode_IsOutOfReachForAConverterBasedWorkaround()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.Utc);

        // Values parsed into a node keep going through JsonElement, which never sees any options at all.
        JsonNode.Parse(NoOffsetText)!.GetValue<DateTime>().Kind.ShouldBe(DateTimeKind.Unspecified);

        // A node built from a DateTime captures the built-in converter at creation time
        // (JsonValueOfTPrimitive.cs:42-57), so options.Converters cannot reach it either - while the
        // proposed option, read by that very built-in converter, would.
        JsonValue.Create(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified))
            .ToJsonString(options).ShouldBe("\"2024-06-01T12:00:00\"");

        // Deserializing through the serializer, by contrast, does honor the converter.
        JsonSerializer.Deserialize<DateTime>(JsonNode.Parse(NoOffsetText)!.ToJsonString(), options).Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Utc_GivesOffsetLessTextAZeroOffsetForDateTimeOffset()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.Utc);

        JsonSerializer.Deserialize<DateTimeOffset>(NoOffsetText, options).ShouldBe(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));
        JsonSerializer.Deserialize<DateTimeOffset>(OffsetText, options).ShouldBe(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.FromHours(-5)));
        JsonSerializer.Deserialize<DateTimeOffset>(UtcText, options).ShouldBe(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void DateTimeOffset_WriteIsNeverChanged()
    {
        DateTimeOffset value = new(2024, 6, 1, 12, 0, 0, TimeSpan.FromHours(-5));

        JsonSerializer.Serialize(value, Options(JsonDateTimeZoneHandling.Utc)).ShouldBe(JsonSerializer.Serialize(value));
    }

    [Fact]
    public void Local_LeavesOffsetLessDateTimeOffsetAsItIsToday()
    {
        JsonSerializerOptions options = Options(JsonDateTimeZoneHandling.Local);

        JsonSerializer.Deserialize<DateTimeOffset>(NoOffsetText, options).ShouldBe(JsonSerializer.Deserialize<DateTimeOffset>(NoOffsetText));
    }

    internal static JsonSerializerOptions Options(JsonDateTimeZoneHandling handling) => new()
    {
        Converters =
        {
            new DateTimeZoneHandlingConverter(handling),
            new DateTimeOffsetZoneHandlingConverter(handling),
        },
    };

    private static string Iso(DateTime value) => value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

    private static string LocalOffsetSuffix(DateTime value)
    {
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified));
        return (offset < TimeSpan.Zero ? "-" : "+") + offset.ToString(@"hh\:mm");
    }
}

[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
public sealed partial class PrototypeJsonContext : JsonSerializerContext
{
}
