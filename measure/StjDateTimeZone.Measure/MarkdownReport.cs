using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace StjDateTimeZone.Measure;

public static class MarkdownReport
{
    public static string Render(ProbeReport report)
    {
        StringBuilder builder = new();
        foreach (string line in report.Environment)
        {
            builder.Append("- ").AppendLine(line);
        }

        foreach (ProbeSection section in report.Sections)
        {
            if (section.Reads.Count > 0)
            {
                AppendTable(builder, $"Read DateTime - {section.Title}", ProbeInputs.All.Select(Code),
                    section.Reads.Select(p => (p.Api, ProbeInputs.All.Select(input => Cell(() => FormatRead(p.Read(input)))))));
            }

            if (section.Writes.Count > 0)
            {
                AppendTable(builder, $"Write DateTime - {section.Title}", ProbeInputs.Kinds.Select(k => $"Kind={k}"),
                    section.Writes.Select(p => (p.Api, ProbeInputs.Kinds.Select(kind => Cell(() => Code(p.Write(ProbeInputs.Value(kind))))))));
            }

            if (section.OffsetReads.Count > 0)
            {
                AppendTable(builder, section.Title, ProbeInputs.All.Select(Code),
                    section.OffsetReads.Select(p => (p.Api, ProbeInputs.All.Select(input => Cell(() => FormatOffsetRead(p.Read(input)))))));
            }
        }

        return builder.ToString();
    }

    public static string FormatRead(DateTime value) => $"{Code(CurrentBehaviorProbe.FormatInvariant(value))} {value.Kind}";

    public static string FormatOffsetRead(DateTimeOffset value)
        => Code(value.ToString("yyyy-MM-dd'T'HH:mmzzz", CultureInfo.InvariantCulture));

    public static string Cell(Func<string> produce)
    {
        try
        {
            return produce();
        }
        catch (Exception exception)
        {
            return $"throws {exception.GetType().Name}";
        }
    }

    private static string Code(string value) => $"`{value}`";

    private static void AppendTable(StringBuilder builder, string title, IEnumerable<string> headers, IEnumerable<(string Api, IEnumerable<string> Cells)> rows)
    {
        List<string> columns = ["API", .. headers];
        builder.AppendLine().Append("### ").AppendLine(title).AppendLine();
        builder.Append("| ").AppendJoin(" | ", columns).AppendLine(" |");
        builder.Append('|').Append(string.Concat(Enumerable.Repeat("---|", columns.Count))).AppendLine();
        foreach ((string api, IEnumerable<string> cells) in rows)
        {
            builder.Append("| ").Append(api).Append(" | ").AppendJoin(" | ", cells).AppendLine(" |");
        }
    }
}
