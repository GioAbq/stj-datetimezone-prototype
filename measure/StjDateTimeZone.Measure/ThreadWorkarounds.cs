using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StjDateTimeZone.Measure;

// Converters copied from the dotnet/runtime#1566 thread, kept verbatim in behavior to measure what users run today.

/// <summary>dotnet/runtime#1566 comment by @mpashkovskiy (2020-05-31, 35 upvotes) - the root the others extend.</summary>
public sealed class MpashkovskiyUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.Parse(reader.GetString()!, CultureInfo.CurrentCulture);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssZ", CultureInfo.CurrentCulture));
}

/// <summary>dotnet/runtime#1566 comment by @dalle (2020-12-15, 30 upvotes).</summary>
public sealed class DalleUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime().ToUniversalTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime());
}

/// <summary>dotnet/runtime#1566 comment by @amay5027 (2021-05-06, 11 upvotes).</summary>
public sealed class SpecifyKindUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.Parse(reader.GetString()!, CultureInfo.CurrentCulture);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

/// <summary>dotnet/runtime#1566 comment by @jgador (2024-10-12), read side only; write side from @dalle.</summary>
public sealed class JgadorUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DateTime date = reader.GetDateTime();
        return date.Kind == DateTimeKind.Utc ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime());
}
