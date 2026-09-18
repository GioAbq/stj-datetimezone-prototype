namespace StjDateTimeZone;

/// <summary>
/// Prototype of the proposed <c>System.Text.Json.Serialization.JsonDateTimeZoneHandling</c> enum
/// for https://github.com/dotnet/runtime/issues/1566.
/// </summary>
public enum JsonDateTimeZoneHandling
{
    /// <summary>Current System.Text.Json behavior: the <see cref="System.DateTimeKind"/> follows the JSON text.</summary>
    RoundtripKind = 0,

    /// <summary>Date-time text without a time zone is UTC, and every value is normalized to UTC.</summary>
    Utc = 1,

    /// <summary>Date-time text without a time zone is local time, and every value is normalized to local time.</summary>
    Local = 2,
}
