using System;

namespace StjDateTimeZone.Measure;

public static class ProbeInputs
{
    public const string Utc = "2024-06-01T12:00:00Z";
    public const string Offset = "2024-06-01T12:00:00-05:00";
    public const string NoOffset = "2024-06-01T12:00:00";

    public static readonly string[] All = [Utc, Offset, NoOffset];

    public static readonly DateTimeKind[] Kinds = [DateTimeKind.Utc, DateTimeKind.Local, DateTimeKind.Unspecified];

    public static DateTime Value(DateTimeKind kind) => new(2024, 6, 1, 12, 0, 0, kind);

    public static string Quote(string value) => "\"" + value + "\"";
}
