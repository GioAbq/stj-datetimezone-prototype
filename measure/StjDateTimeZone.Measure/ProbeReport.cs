using System;
using System.Collections.Generic;

namespace StjDateTimeZone.Measure;

public sealed record ReadProbe(string Api, Func<string, DateTime> Read);

public sealed record WriteProbe(string Api, Func<DateTime, string> Write);

public sealed record OffsetReadProbe(string Api, Func<string, DateTimeOffset> Read);

public sealed record ProbeSection(
    string Title,
    IReadOnlyList<ReadProbe> Reads,
    IReadOnlyList<WriteProbe> Writes,
    IReadOnlyList<OffsetReadProbe> OffsetReads);

public sealed record ProbeReport(IReadOnlyList<string> Environment, IReadOnlyList<ProbeSection> Sections);
