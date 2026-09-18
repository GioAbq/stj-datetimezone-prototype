using System;

namespace StjDateTimeZone.Measure;

public static class Program
{
    public static int Main()
    {
        Console.Out.Write(MarkdownReport.Render(CurrentBehaviorProbe.Run()));
        return 0;
    }
}
