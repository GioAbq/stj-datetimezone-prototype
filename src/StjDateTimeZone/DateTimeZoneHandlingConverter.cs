using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StjDateTimeZone;

/// <summary>
/// Stands in for what the built-in <c>DateTimeConverter</c> would do once it read the proposed option.
/// </summary>
public sealed class DateTimeZoneHandlingConverter : JsonConverter<DateTime>
{
    private readonly DateTimeZoneNormalizer _normalizer;

    public DateTimeZoneHandlingConverter(JsonDateTimeZoneHandling handling)
        : this(new DateTimeZoneNormalizer(handling))
    {
    }

    public DateTimeZoneHandlingConverter(DateTimeZoneNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(normalizer);
        _normalizer = normalizer;
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => _normalizer.Normalize(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(_normalizer.Normalize(value));

    public override DateTime ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => _normalizer.Normalize(ParsePropertyName(ref reader));

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WritePropertyName(FormatPropertyName(_normalizer.Normalize(value)));

    /// <summary>
    /// Utf8JsonReader.GetDateTime() rejects a PropertyName token; only the library-internal
    /// GetDateTimeNoValidation() accepts one. A converter written outside the library therefore has to
    /// materialize the key as a string and parse it again.
    /// </summary>
    internal static DateTime ParsePropertyName(ref Utf8JsonReader reader)
        => DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <summary>
    /// Utf8JsonWriter.WritePropertyName(DateTime) is internal as well, so the trimmed ISO 8601 shape
    /// the built-in converter writes has to be reproduced by hand.
    /// </summary>
    internal const string PropertyNameFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

    private static string FormatPropertyName(DateTime value) => value.ToString(PropertyNameFormat, CultureInfo.InvariantCulture);
}
