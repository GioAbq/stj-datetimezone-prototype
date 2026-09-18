using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shouldly;
using StjDateTimeZone.Measure;
using Xunit;

namespace StjDateTimeZone.Tests.Measure;

public sealed class CurrentBehaviorProbeTests
{
    private static readonly DateTime Noon = new(2024, 6, 1, 12, 0, 0);
    private static readonly DateTime OffsetInputAsUtc = new(2024, 6, 1, 17, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SystemTextJson_Reads_AreConsistentAcrossAllApis()
    {
        IReadOnlyList<ReadProbe> reads = CurrentBehaviorProbe.SystemTextJson().Reads;

        reads.Count.ShouldBe(8);
        foreach (ReadProbe probe in reads)
        {
            ShouldBeUtcNoon(probe.Read(ProbeInputs.Utc));
            ShouldBeLocalInstant(probe.Read(ProbeInputs.Offset), OffsetInputAsUtc);
            ShouldBe(probe.Read(ProbeInputs.NoOffset), Noon, DateTimeKind.Unspecified);
        }
    }

    [Fact]
    public void SystemTextJson_Writes_FollowTheKind()
    {
        IReadOnlyList<WriteProbe> writes = CurrentBehaviorProbe.SystemTextJson().Writes;

        writes.Count.ShouldBe(5);
        foreach (WriteProbe probe in writes)
        {
            probe.Write(ProbeInputs.Value(DateTimeKind.Utc)).ShouldBe("2024-06-01T12:00:00Z");
            probe.Write(ProbeInputs.Value(DateTimeKind.Local)).ShouldBe("2024-06-01T12:00:00" + LocalOffsetSuffix());
            probe.Write(ProbeInputs.Value(DateTimeKind.Unspecified)).ShouldBe("2024-06-01T12:00:00");
        }
    }

    [Fact]
    public void Newtonsoft_Utc_AssumesUtcForOffsetLessInputAndConvertsOffsets()
    {
        ProbeSection section = CurrentBehaviorProbe.NewtonsoftJson();

        foreach (ReadProbe probe in section.Reads.Where(p => p.Api.EndsWith(", Utc", StringComparison.Ordinal)))
        {
            ShouldBeUtcNoon(probe.Read(ProbeInputs.Utc));
            ShouldBe(probe.Read(ProbeInputs.Offset), OffsetInputAsUtc, DateTimeKind.Utc);
            ShouldBe(probe.Read(ProbeInputs.NoOffset), Noon, DateTimeKind.Utc);
        }

        foreach (WriteProbe probe in section.Writes.Where(p => p.Api.EndsWith(", Utc", StringComparison.Ordinal)))
        {
            probe.Write(ProbeInputs.Value(DateTimeKind.Local)).ShouldBe(IsoUtc(ProbeInputs.Value(DateTimeKind.Local).ToUniversalTime()));
            probe.Write(ProbeInputs.Value(DateTimeKind.Unspecified)).ShouldBe("2024-06-01T12:00:00Z");
        }
    }

    [Fact]
    public void Newtonsoft_Unspecified_DropsKindAfterConvertingToMachineLocalTime()
    {
        ReadProbe probe = Single(CurrentBehaviorProbe.NewtonsoftJson().Reads, "JsonConvert.DeserializeObject<DateTime>, Unspecified");

        DateTime value = probe.Read(ProbeInputs.Offset);

        ShouldBe(value, TimeZoneInfo.ConvertTimeFromUtc(OffsetInputAsUtc, TimeZoneInfo.Local), DateTimeKind.Unspecified);
    }

    [Fact]
    public void DateTimeOffset_OffsetLessInput_AssumesMachineOffsetEvenWithNewtonsoftUtc()
    {
        TimeSpan machineOffset = TimeZoneInfo.Local.GetUtcOffset(Noon);

        foreach (OffsetReadProbe probe in CurrentBehaviorProbe.OffsetLessDateTimeOffset().OffsetReads)
        {
            DateTimeOffset value = probe.Read(ProbeInputs.NoOffset);
            value.DateTime.ShouldBe(Noon);
            value.Offset.ShouldBe(machineOffset);
            probe.Read(ProbeInputs.Offset).Offset.ShouldBe(TimeSpan.FromHours(-5));
        }
    }

    [Fact]
    public void MpashkovskiyWorkaround_DropsSubSecondPrecisionAndAssumesLocalForOffsetLessInput()
    {
        ProbeSection section = CurrentBehaviorProbe.ThreadWorkarounds();

        // Reading "...Z" through DateTime.Parse hands back machine-local time, not UTC.
        ShouldBeLocalInstant(Single(section.Reads, "@mpashkovskiy converter").Read(ProbeInputs.Utc), new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc));

        // Writing an Unspecified value treats it as local time and shifts it.
        WriteProbe write = Single(section.Writes, "@mpashkovskiy converter");
        write.Write(ProbeInputs.Value(DateTimeKind.Unspecified))
            .ShouldBe(DateTime.SpecifyKind(Noon, DateTimeKind.Local).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + "Z");
    }

    [Fact]
    public void MpashkovskiyWorkaround_LosesTheFractionalSecond()
    {
        WriteProbe write = Single(CurrentBehaviorProbe.ThreadWorkarounds().Writes, "@mpashkovskiy converter");

        string written = write.Write(new DateTime(2024, 6, 1, 12, 0, 0, 123, DateTimeKind.Utc));

        written.ShouldBe("2024-06-01T12:00:00Z");
        written.ShouldNotContain(".123");
    }

    [Fact]
    public void DalleWorkaround_ShiftsOffsetLessInputByMachineOffset()
    {
        ReadProbe probe = Single(CurrentBehaviorProbe.ThreadWorkarounds().Reads, "@dalle converter");

        DateTime value = probe.Read(ProbeInputs.NoOffset);

        ShouldBe(value, DateTime.SpecifyKind(Noon, DateTimeKind.Local).ToUniversalTime(), DateTimeKind.Utc);
    }

    [Fact]
    public void DalleWorkaround_IsBypassedForDictionaryKeys()
    {
        ProbeSection section = CurrentBehaviorProbe.ThreadWorkarounds();
        ReadProbe read = Single(section.Reads, "@dalle converter, Dictionary<DateTime, int> key");
        WriteProbe write = Single(section.Writes, "@dalle converter, Dictionary<DateTime, int> key");

        ShouldBeLocalInstant(read.Read(ProbeInputs.Offset), OffsetInputAsUtc);
        ShouldBe(read.Read(ProbeInputs.NoOffset), Noon, DateTimeKind.Unspecified);
        write.Write(ProbeInputs.Value(DateTimeKind.Unspecified)).ShouldBe("2024-06-01T12:00:00");
    }

    [Fact]
    public void JgadorWorkaround_RelabelsConvertedLocalTimeAsUtc()
    {
        ReadProbe probe = Single(CurrentBehaviorProbe.ThreadWorkarounds().Reads, "@jgador converter");

        DateTime value = probe.Read(ProbeInputs.Offset);

        ShouldBe(value, TimeZoneInfo.ConvertTimeFromUtc(OffsetInputAsUtc, TimeZoneInfo.Local), DateTimeKind.Utc);
        ShouldBe(probe.Read(ProbeInputs.NoOffset), Noon, DateTimeKind.Utc);
    }

    [Fact]
    public void SpecifyKindWorkaround_WritesLocalWallClockAsUtc()
    {
        ProbeSection section = CurrentBehaviorProbe.ThreadWorkarounds();

        Single(section.Writes, "@amay5027 converter").Write(ProbeInputs.Value(DateTimeKind.Local)).ShouldBe("2024-06-01T12:00:00Z");
        ShouldBeLocalInstant(Single(section.Reads, "@amay5027 converter").Read(ProbeInputs.Utc), new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ProposedSemantics_ReportsOneRowPerMode()
    {
        ProbeSection section = CurrentBehaviorProbe.ProposedSemantics();

        section.Reads.Count.ShouldBe(3);
        section.Writes.Count.ShouldBe(3);
        section.OffsetReads.Count.ShouldBe(3);

        ShouldBe(Single(section.Reads, "DateTimeZoneHandling = Utc").Read(ProbeInputs.NoOffset), Noon, DateTimeKind.Utc);
        ShouldBe(Single(section.Reads, "DateTimeZoneHandling = RoundtripKind").Read(ProbeInputs.NoOffset), Noon, DateTimeKind.Unspecified);
        ShouldBe(Single(section.Reads, "DateTimeZoneHandling = Local").Read(ProbeInputs.Utc), TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local), DateTimeKind.Local);

        Single(section.Writes, "DateTimeZoneHandling = Utc").Write(ProbeInputs.Value(DateTimeKind.Unspecified)).ShouldBe("2024-06-01T12:00:00Z");
        section.OffsetReads.Single(p => p.Api.EndsWith("= Utc", StringComparison.Ordinal)).Read(ProbeInputs.NoOffset).Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Run_ContainsAllSectionsAndEnvironment()
    {
        ProbeReport report = CurrentBehaviorProbe.Run();

        report.Sections.Count.ShouldBe(5);
        report.Environment.Count.ShouldBe(4);
        report.Environment.ShouldContain(line => line.StartsWith("System.Text.Json: 10.", StringComparison.Ordinal));
        report.Environment.ShouldContain(line => line.StartsWith("Machine time zone: " + TimeZoneInfo.Local.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void FormatInvariant_UsesMinutePrecision()
        => CurrentBehaviorProbe.FormatInvariant(new DateTime(2024, 6, 1, 9, 5, 59)).ShouldBe("2024-06-01T09:05");

    private static T Single<T>(IEnumerable<T> probes, string api)
        where T : class
        => probes.Single(p => p switch
        {
            ReadProbe r => r.Api == api,
            WriteProbe w => w.Api == api,
            _ => false,
        });

    private static void ShouldBeUtcNoon(DateTime value) => ShouldBe(value, Noon, DateTimeKind.Utc);

    private static void ShouldBeLocalInstant(DateTime value, DateTime utcInstant)
        => ShouldBe(value, TimeZoneInfo.ConvertTimeFromUtc(utcInstant, TimeZoneInfo.Local), DateTimeKind.Local);

    private static void ShouldBe(DateTime value, DateTime expectedWallClock, DateTimeKind expectedKind)
    {
        value.Kind.ShouldBe(expectedKind);
        value.Ticks.ShouldBe(expectedWallClock.Ticks);
    }

    private static string IsoUtc(DateTime utc) => utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string LocalOffsetSuffix()
    {
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(Noon);
        return (offset < TimeSpan.Zero ? "-" : "+") + offset.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }
}
