using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StjDateTimeZone;

/// <summary>
/// The DateTimeOffset half of the proposal: only JSON text without any offset is affected, and only on read.
/// </summary>
public sealed class DateTimeOffsetZoneHandlingConverter : JsonConverter<DateTimeOffset>
{
    private readonly DateTimeZoneNormalizer _normalizer;

    public DateTimeOffsetZoneHandlingConverter(JsonDateTimeZoneHandling handling)
        : this(new DateTimeZoneNormalizer(handling))
    {
    }

    public DateTimeOffsetZoneHandlingConverter(DateTimeZoneNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(normalizer);
        _normalizer = normalizer;
    }

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // A DateTime parsed as Unspecified is exactly the case where the JSON text carried no "Z" and no offset.
        DateTime asDateTime = reader.GetDateTime();
        return asDateTime.Kind == DateTimeKind.Unspecified
            ? _normalizer.AssumeOffset(asDateTime)
            : reader.GetDateTimeOffset();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);

    public override DateTimeOffset ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // The key has to be parsed twice: once as DateTime to learn whether the text carried an offset at
        // all, and then as DateTimeOffset, because DateTime.Parse converts an offset to machine-local time
        // and the original offset would be lost.
        string text = reader.GetString()!;
        DateTime asDateTime = DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        return asDateTime.Kind == DateTimeKind.Unspecified
            ? _normalizer.AssumeOffset(asDateTime)
            : DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFzzz", CultureInfo.InvariantCulture));
}
