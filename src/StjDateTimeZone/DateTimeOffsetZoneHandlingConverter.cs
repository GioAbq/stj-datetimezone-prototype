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
        DateTime asDateTime = DateTimeZoneHandlingConverter.ParsePropertyName(ref reader);
        return asDateTime.Kind == DateTimeKind.Unspecified
            ? _normalizer.AssumeOffset(asDateTime)
            : new DateTimeOffset(asDateTime);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFzzz", CultureInfo.InvariantCulture));
}
