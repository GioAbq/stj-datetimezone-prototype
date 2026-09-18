using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;
using StjDateTimeZone;
using Xunit;

namespace StjDateTimeZone.Tests.Prototype;

/// <summary>
/// Evidence for the proposal: outside System.Text.Json a converter cannot reuse the built-in key parser,
/// because Utf8JsonReader.GetDateTime() rejects a PropertyName token and GetDateTimeNoValidation() is internal.
/// </summary>
public sealed class ReaderLimitationTests
{
    [Fact]
    public void GetDateTime_RejectsAPropertyNameToken()
    {
        JsonSerializerOptions options = new() { Converters = { new BuiltInReaderKeyConverter() } };

        JsonException exception = Should.Throw<JsonException>(
            () => JsonSerializer.Deserialize<Dictionary<DateTime, int>>("{\"2024-06-01T12:00:00Z\":1}", options));

        exception.InnerException.ShouldBeOfType<InvalidOperationException>()
            .Message.ShouldContain("PropertyName");
    }

    [Fact]
    public void StringParsingFallback_MatchesTheBuiltInKeyParserForIsoInput()
    {
        JsonSerializerOptions mine = ConverterSemanticsTests.Options(JsonDateTimeZoneHandling.RoundtripKind);

        foreach (string key in new[] { "2024-06-01T12:00:00Z", "2024-06-01T12:00:00-05:00", "2024-06-01T12:00:00" })
        {
            string json = "{\"" + key + "\":1}";
            Dictionary<DateTime, int> withConverter = JsonSerializer.Deserialize<Dictionary<DateTime, int>>(json, mine)!;
            Dictionary<DateTime, int> builtIn = JsonSerializer.Deserialize<Dictionary<DateTime, int>>(json)!;

            foreach ((DateTime actual, DateTime expected) in Pairs(withConverter, builtIn))
            {
                actual.ShouldBe(expected);
                actual.Kind.ShouldBe(expected.Kind);
            }
        }
    }

    [Theory]
    [InlineData("2024-06-01T12:00:00Z")]
    [InlineData("2024-06-01T12:00:00.1234567Z")]
    [InlineData("2024-06-01T12:00:00.5Z")]
    [InlineData("2024-06-01T12:00:00")]
    public void HandFormattedKey_MatchesTheBuiltInKeyWriter(string input)
    {
        DateTime value = DateTime.Parse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        Dictionary<DateTime, int> map = new() { [value] = 1 };

        string mine = JsonSerializer.Serialize(map, ConverterSemanticsTests.Options(JsonDateTimeZoneHandling.RoundtripKind));
        string builtIn = JsonSerializer.Serialize(map);

        mine.ShouldBe(builtIn);
    }

    private static IEnumerable<(DateTime Actual, DateTime Expected)> Pairs(Dictionary<DateTime, int> actual, Dictionary<DateTime, int> expected)
    {
        using IEnumerator<DateTime> left = actual.Keys.GetEnumerator();
        using IEnumerator<DateTime> right = expected.Keys.GetEnumerator();
        while (left.MoveNext() && right.MoveNext())
        {
            yield return (left.Current, right.Current);
        }
    }

    private sealed class BuiltInReaderKeyConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);

        public override DateTime ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTime();

        // Utf8JsonWriter.WritePropertyName(DateTime) is internal too, so even this minimal stand-in has to format by hand.
        public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WritePropertyName(value.ToString("O", CultureInfo.InvariantCulture));
    }
}
