using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Shouldly;
using StjDateTimeZone;
using Xunit;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using JsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings;
using NsDateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling;

namespace StjDateTimeZone.Tests.Prototype;

/// <summary>
/// The migration promise in dotnet/runtime#1566 is "the setting I had in Json.NET". These tests hold the
/// prototype against the real Newtonsoft.Json 13.0.4 instead of against a description of it.
/// </summary>
public sealed class NewtonsoftParityTests
{
    public static TheoryData<string> Inputs => new("2024-06-01T12:00:00Z", "2024-06-01T12:00:00-05:00", "2024-06-01T12:00:00");

    public static TheoryData<DateTimeKind> Kinds => new(DateTimeKind.Utc, DateTimeKind.Local, DateTimeKind.Unspecified);

    [Theory]
    [MemberData(nameof(Inputs))]
    public void Utc_ReadsLikeNewtonsoft(string input)
        => ReadsLikeNewtonsoft(input, JsonDateTimeZoneHandling.Utc, NsDateTimeZoneHandling.Utc);

    [Theory]
    [MemberData(nameof(Inputs))]
    public void Local_ReadsLikeNewtonsoft(string input)
        => ReadsLikeNewtonsoft(input, JsonDateTimeZoneHandling.Local, NsDateTimeZoneHandling.Local);

    [Theory]
    [MemberData(nameof(Inputs))]
    public void RoundtripKind_ReadsLikeNewtonsoft(string input)
        => ReadsLikeNewtonsoft(input, JsonDateTimeZoneHandling.RoundtripKind, NsDateTimeZoneHandling.RoundtripKind);

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Utc_WritesLikeNewtonsoft(DateTimeKind kind)
        => WritesLikeNewtonsoft(kind, JsonDateTimeZoneHandling.Utc, NsDateTimeZoneHandling.Utc);

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Local_WritesLikeNewtonsoft(DateTimeKind kind)
        => WritesLikeNewtonsoft(kind, JsonDateTimeZoneHandling.Local, NsDateTimeZoneHandling.Local);

    [Theory]
    [MemberData(nameof(Kinds))]
    public void RoundtripKind_WritesLikeNewtonsoft(DateTimeKind kind)
        => WritesLikeNewtonsoft(kind, JsonDateTimeZoneHandling.RoundtripKind, NsDateTimeZoneHandling.RoundtripKind);

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Utc_WritesDictionaryKeysLikeNewtonsoft(DateTimeKind kind)
    {
        Dictionary<DateTime, int> map = new() { [DateTime.SpecifyKind(new DateTime(2024, 6, 1, 12, 0, 0), kind)] = 1 };

        string mine = FirstKey(JsonSerializer.Serialize(map, ConverterSemanticsTests.Options(JsonDateTimeZoneHandling.Utc)));
        string theirs = FirstKey(JsonConvert.SerializeObject(map, Settings(NsDateTimeZoneHandling.Utc)));

        mine.ShouldBe(theirs);
    }

    [Fact]
    public void Utc_DivergesFromNewtonsoftOnlyForOffsetLessDateTimeOffset()
    {
        const string json = "\"2024-06-01T12:00:00\"";

        DateTimeOffset mine = JsonSerializer.Deserialize<DateTimeOffset>(json, ConverterSemanticsTests.Options(JsonDateTimeZoneHandling.Utc));
        DateTimeOffset theirs = JsonConvert.DeserializeObject<DateTimeOffset>(json, Settings(NsDateTimeZoneHandling.Utc));

        mine.Offset.ShouldBe(TimeSpan.Zero);
        theirs.Offset.ShouldBe(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2024, 6, 1, 12, 0, 0)));
    }

    private static void ReadsLikeNewtonsoft(string input, JsonDateTimeZoneHandling mine, NsDateTimeZoneHandling theirs)
    {
        string json = "\"" + input + "\"";

        DateTime actual = JsonSerializer.Deserialize<DateTime>(json, ConverterSemanticsTests.Options(mine));
        DateTime expected = JsonConvert.DeserializeObject<DateTime>(json, Settings(theirs));

        actual.ShouldBe(expected);
        actual.Kind.ShouldBe(expected.Kind);
    }

    private static void WritesLikeNewtonsoft(DateTimeKind kind, JsonDateTimeZoneHandling mine, NsDateTimeZoneHandling theirs)
    {
        DateTime value = DateTime.SpecifyKind(new DateTime(2024, 6, 1, 12, 0, 0), kind);

        string actual = JsonSerializer.Serialize(value, ConverterSemanticsTests.Options(mine));
        string expected = JsonConvert.SerializeObject(value, Settings(theirs));

        actual.ShouldBe(expected);
    }

    private static JsonSerializerSettings Settings(NsDateTimeZoneHandling handling) => new() { DateTimeZoneHandling = handling };

    private static string FirstKey(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject().Single().Name;
    }
}
