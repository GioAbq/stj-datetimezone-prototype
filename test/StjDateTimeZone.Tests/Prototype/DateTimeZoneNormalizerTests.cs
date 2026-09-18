using System;
using Shouldly;
using StjDateTimeZone;
using Xunit;

namespace StjDateTimeZone.Tests.Prototype;

public sealed class DateTimeZoneNormalizerTests
{
    private static readonly TimeZoneInfo Warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
    private static readonly DateTime Noon = new(2024, 6, 1, 12, 0, 0);

    [Theory]
    [InlineData(DateTimeKind.Utc, 12, DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local, 12, DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified, 12, DateTimeKind.Unspecified)]
    public void RoundtripKind_LeavesEverythingAlone(DateTimeKind kind, int expectedHour, DateTimeKind expectedKind)
    {
        DateTime value = Normalizer(JsonDateTimeZoneHandling.RoundtripKind).Normalize(Value(kind));

        value.Hour.ShouldBe(expectedHour);
        value.Kind.ShouldBe(expectedKind);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc, 12)]
    [InlineData(DateTimeKind.Local, 10)]
    [InlineData(DateTimeKind.Unspecified, 12)]
    public void Utc_ConvertsLocalAndAssumesUtcForUnspecified(DateTimeKind kind, int expectedHour)
    {
        DateTime value = Normalizer(JsonDateTimeZoneHandling.Utc).Normalize(Value(kind));

        value.Kind.ShouldBe(DateTimeKind.Utc);
        value.Hour.ShouldBe(expectedHour);
        value.Date.ShouldBe(Noon.Date);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc, 14)]
    [InlineData(DateTimeKind.Local, 12)]
    [InlineData(DateTimeKind.Unspecified, 12)]
    public void Local_ConvertsUtcAndAssumesLocalForUnspecified(DateTimeKind kind, int expectedHour)
    {
        DateTime value = Normalizer(JsonDateTimeZoneHandling.Local).Normalize(Value(kind));

        value.Kind.ShouldBe(DateTimeKind.Local);
        value.Hour.ShouldBe(expectedHour);
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        foreach (JsonDateTimeZoneHandling handling in Enum.GetValues<JsonDateTimeZoneHandling>())
        {
            DateTimeZoneNormalizer normalizer = Normalizer(handling);
            foreach (DateTimeKind kind in new[] { DateTimeKind.Utc, DateTimeKind.Local, DateTimeKind.Unspecified })
            {
                DateTime once = normalizer.Normalize(Value(kind));
                normalizer.Normalize(once).ShouldBe(once);
            }
        }
    }

    [Fact]
    public void AssumeOffset_UsesZeroForUtcAndTheZoneOffsetOtherwise()
    {
        Normalizer(JsonDateTimeZoneHandling.Utc).AssumeOffset(Noon).ShouldBe(new DateTimeOffset(Noon, TimeSpan.Zero));
        Normalizer(JsonDateTimeZoneHandling.Local).AssumeOffset(Noon).ShouldBe(new DateTimeOffset(Noon, TimeSpan.FromHours(2)));
        Normalizer(JsonDateTimeZoneHandling.RoundtripKind).AssumeOffset(Noon).ShouldBe(new DateTimeOffset(Noon, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void Utc_SaturatesInsteadOfThrowingAtTheEdgesOfTheRange()
    {
        DateTimeZoneNormalizer normalizer = Normalizer(JsonDateTimeZoneHandling.Utc);

        normalizer.Normalize(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local))
            .ShouldBe(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        normalizer.Normalize(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified))
            .ShouldBe(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
    }

    [Fact]
    public void Local_SaturatesAtTheUpperEdgeLikeToLocalTime()
    {
        DateTime max = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

        Normalizer(JsonDateTimeZoneHandling.Local).Normalize(max).ShouldBe(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local));
        max.ToLocalTime().ShouldBe(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local));
    }

    [Fact]
    public void MachineZone_MatchesTheBclConversions()
    {
        DateTimeZoneNormalizer toUtc = new(JsonDateTimeZoneHandling.Utc);
        DateTimeZoneNormalizer toLocal = new(JsonDateTimeZoneHandling.Local);

        foreach (DateTime sample in new[] { Noon, new DateTime(2024, 1, 15, 3, 0, 0), new DateTime(2024, 7, 4, 23, 30, 0) })
        {
            DateTime local = DateTime.SpecifyKind(sample, DateTimeKind.Local);
            DateTime utc = DateTime.SpecifyKind(sample, DateTimeKind.Utc);

            toUtc.Normalize(local).ShouldBe(local.ToUniversalTime());
            toLocal.Normalize(utc).ShouldBe(utc.ToLocalTime());
        }
    }

    [Fact]
    public void DstGap_LocalModeInventsAWallClockThatNeverHappened()
    {
        // Warsaw jumps 02:00 -> 03:00 on 2024-03-31, so 02:30 does not exist that day.
        DateTime gap = new(2024, 3, 31, 2, 30, 0);
        Warsaw.IsInvalidTime(gap).ShouldBeTrue();

        DateTime assumedLocal = Normalizer(JsonDateTimeZoneHandling.Local).Normalize(gap);
        DateTime backToUtc = Normalizer(JsonDateTimeZoneHandling.Utc).Normalize(assumedLocal);
        DateTime backToLocal = Normalizer(JsonDateTimeZoneHandling.Local).Normalize(backToUtc);

        // The zone answers with the daylight offset (+02:00) for a wall clock that never existed, so the
        // value comes back an hour earlier than it went in - 02:30 -> 00:30Z -> 01:30.
        assumedLocal.Kind.ShouldBe(DateTimeKind.Local);
        backToUtc.ShouldBe(new DateTime(2024, 3, 31, 0, 30, 0, DateTimeKind.Utc));
        backToLocal.ShouldNotBe(assumedLocal);
        backToLocal.TimeOfDay.ShouldBe(new TimeSpan(1, 30, 0));
    }

    [Fact]
    public void AmbiguousHour_UtcModePicksTheStandardTimeReading()
    {
        // Warsaw repeats 02:00-03:00 on 2024-10-27; 02:30 maps to either 00:30Z (daylight) or 01:30Z (standard).
        DateTime ambiguous = new(2024, 10, 27, 2, 30, 0);
        Warsaw.IsAmbiguousTime(ambiguous).ShouldBeTrue();

        DateTime asUtc = Normalizer(JsonDateTimeZoneHandling.Utc).Normalize(DateTime.SpecifyKind(ambiguous, DateTimeKind.Local));

        asUtc.ShouldBe(new DateTime(2024, 10, 27, 1, 30, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void UndefinedHandling_Throws(int handling)
        => Should.Throw<ArgumentOutOfRangeException>(() => new DateTimeZoneNormalizer((JsonDateTimeZoneHandling)handling, Warsaw));

    [Fact]
    public void NullTimeZone_Throws()
        => Should.Throw<ArgumentNullException>(() => new DateTimeZoneNormalizer(JsonDateTimeZoneHandling.Utc, null!));

    private static DateTimeZoneNormalizer Normalizer(JsonDateTimeZoneHandling handling) => new(handling, Warsaw);

    private static DateTime Value(DateTimeKind kind) => DateTime.SpecifyKind(Noon, kind);
}
