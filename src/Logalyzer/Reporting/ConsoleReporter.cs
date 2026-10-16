using Logalyzer.Analysis;

namespace Logalyzer.Reporting;

/// <summary>Writes the human-readable console summary table.</summary>
public static class ConsoleReporter
{
    public static void Report(AnalysisResult r, TextWriter writer)
    {
        writer.WriteLine($"Logalyzer report — {DisplaySource(r)}");
        writer.WriteLine(new string('=', 56));
        writer.WriteLine($"Total lines:    {r.TotalLines,6}");
        writer.WriteLine($"Parsed lines:   {r.ParsedLines,6}");
        writer.WriteLine($"Unparsed lines: {r.UnparsedLines,6}");
        if (r.From.HasValue || r.To.HasValue)
        {
            writer.WriteLine($"Date filter:    {FormatDate(r.From)} .. {FormatDate(r.To)}");
        }

        writer.WriteLine();
        writer.WriteLine("Levels:");
        if (r.LevelCounts.Count == 0)
        {
            writer.WriteLine("  (none)");
        }
        else
        {
            foreach (var kv in r.LevelCounts.OrderBy(kv => kv.Key))
            {
                writer.WriteLine($"  {kv.Key,-8} {kv.Value,6}");
            }
        }

        writer.WriteLine();
        writer.WriteLine("HTTP status codes:");
        if (r.StatusCounts.Count == 0)
        {
            writer.WriteLine("  (none — no access-log entries)");
        }
        else
        {
            foreach (var kv in r.StatusCounts.OrderBy(kv => kv.Key))
            {
                writer.WriteLine($"  {kv.Key,-8} {kv.Value,6}");
            }
        }

        writer.WriteLine();
        writer.WriteLine($"Top {r.TopN} paths:");
        WriteTopList(writer, r.TopPaths);

        writer.WriteLine();
        writer.WriteLine($"Top {r.TopN} IPs:");
        WriteTopList(writer, r.TopIps);

        writer.WriteLine();
        writer.WriteLine("Hourly histogram (hour: count bar):");
        int max = r.HourlyHistogram.Values.DefaultIfEmpty(0).Max();
        foreach (var kv in r.HourlyHistogram.OrderBy(kv => kv.Key))
        {
            string bar = max == 0 ? string.Empty : new string('#', kv.Value * 40 / max);
            writer.WriteLine($"  {kv.Key:00}:00  {kv.Value,5}  {bar}");
        }
    }

    private static void WriteTopList(TextWriter writer, List<KeyValuePair<string, int>> items)
    {
        if (items.Count == 0)
        {
            writer.WriteLine("  (none)");
            return;
        }

        foreach (var kv in items)
        {
            writer.WriteLine($"  {kv.Value,6}  {kv.Key}");
        }
    }

    private static string DisplaySource(AnalysisResult r) =>
        string.IsNullOrEmpty(r.SourceFile) ? "(stdin)" : r.SourceFile;

    private static string FormatDate(DateTime? dt) =>
        dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "*";
}
