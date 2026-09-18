using System;

namespace StjDateTimeZone;

/// <summary>
/// The semantics the proposed option would apply, isolated from System.Text.Json so that tests can
/// pin them against an explicit time zone instead of whatever zone the build agent happens to use.
/// </summary>
public sealed class DateTimeZoneNormalizer
{
    private readonly TimeZoneInfo _timeZone;

    public DateTimeZoneNormalizer(JsonDateTimeZoneHandling handling)
        : this(handling, TimeZoneInfo.Local)
    {
    }

    public DateTimeZoneNormalizer(JsonDateTimeZoneHandling handling, TimeZoneInfo timeZone)
    {
        if (handling is < JsonDateTimeZoneHandling.RoundtripKind or > JsonDateTimeZoneHandling.Local)
        {
            throw new ArgumentOutOfRangeException(nameof(handling));
        }

        ArgumentNullException.ThrowIfNull(timeZone);

        Handling = handling;
        _timeZone = timeZone;
    }

    public JsonDateTimeZoneHandling Handling { get; }

    /// <summary>Applies the configured handling to a value being read or written. Both directions share one rule.</summary>
    public DateTime Normalize(DateTime value) => Handling switch
    {
        JsonDateTimeZoneHandling.Utc => ToUtc(value),
        JsonDateTimeZoneHandling.Local => ToLocal(value),
        _ => value,
    };

    /// <summary>Builds the <see cref="DateTimeOffset"/> for JSON text that carried no offset at all.</summary>
    public DateTimeOffset AssumeOffset(DateTime wallClock)
    {
        DateTime unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);
        return Handling == JsonDateTimeZoneHandling.Utc
            ? new DateTimeOffset(unspecified, TimeSpan.Zero)
            : new DateTimeOffset(unspecified, _timeZone.GetUtcOffset(unspecified));
    }

    private DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => Shift(value, -_timeZone.GetUtcOffset(value).Ticks, DateTimeKind.Utc),
    };

    private DateTime ToLocal(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local),
        _ => Shift(value, _timeZone.GetUtcOffset(value).Ticks, DateTimeKind.Local),
    };

    /// <summary>Saturates like <see cref="DateTime.ToUniversalTime"/> instead of throwing at the edges of the range.</summary>
    private static DateTime Shift(DateTime value, long offsetTicks, DateTimeKind kind)
    {
        long ticks = value.Ticks + offsetTicks;
        if (ticks < DateTime.MinValue.Ticks)
        {
            return DateTime.SpecifyKind(DateTime.MinValue, kind);
        }

        if (ticks > DateTime.MaxValue.Ticks)
        {
            return DateTime.SpecifyKind(DateTime.MaxValue, kind);
        }

        return new DateTime(ticks, kind);
    }
}
