using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StjDateTimeZone.Measure;

[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTime?))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(Dictionary<DateTime, int>))]
public sealed partial class ProbeJsonContext : JsonSerializerContext
{
}
