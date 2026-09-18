using System;
using Shouldly;
using StjDateTimeZone.Measure;
using Xunit;

namespace StjDateTimeZone.Tests.Measure;

public sealed class ProbeInputsTests
{
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Value_IsNoonWithRequestedKind(DateTimeKind kind)
    {
        DateTime value = ProbeInputs.Value(kind);

        value.Kind.ShouldBe(kind);
        value.Ticks.ShouldBe(new DateTime(2024, 6, 1, 12, 0, 0).Ticks);
    }

    [Fact]
    public void Quote_WrapsInJsonStringQuotes()
        => ProbeInputs.Quote("x").ShouldBe("\"x\"");

    [Fact]
    public void All_CoversUtcOffsetAndOffsetLessInputs()
        => ProbeInputs.All.ShouldBe([ProbeInputs.Utc, ProbeInputs.Offset, ProbeInputs.NoOffset]);
}
