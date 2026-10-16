using Logalyzer.Models;

namespace Logalyzer.Analysis;

/// <summary>Aggregates parsed log entries into an <see cref="AnalysisResult"/>.</summary>
public static class LogAnalyzer
{
    public static AnalysisResult Analyze(
        IEnumerable<LogEntry> entries,
        string sourceFile = "",
        DateTime? from = null,
        DateTime? to = null,
        int topN = 5)
    {
        var all = entries.ToList();
        int unparsed = all.Count(e => !e.Parsed);

        // Date filter applies to parsed entries with a real timestamp.
        // Unparsed lines have no timestamp, so they are counted but excluded
        // from the filtered aggregates.
        List<LogEntry> filtered = all
            .Where(e => e.Parsed && e.Timestamp.HasValue)
            .Where(e => (!from.HasValue || e.Timestamp!.Value >= from.Value) &&
                        (!to.HasValue || e.Timestamp!.Value <= to.Value))
            .ToList();

        var levelCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var statusCounts = new Dictionary<int, int>();
        var pathCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var ipCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var hourly = new Dictionary<int, int>();
        for (int h = 0; h < 24; h++)
        {
            hourly[h] = 0;
        }

        foreach (LogEntry e in filtered)
        {
            string level = (e.Level ?? "INFO").ToUpperInvariant();
            levelCounts[level] = levelCounts.TryGetValue(level, out int c) ? c + 1 : 1;

            if (e.StatusCode.HasValue)
            {
                int s = e.StatusCode.Value;
                statusCounts[s] = statusCounts.TryGetValue(s, out int sc) ? sc + 1 : 1;
            }

            if (!string.IsNullOrEmpty(e.Path))
            {
                pathCounts[e.Path] = pathCounts.TryGetValue(e.Path, out int pc) ? pc + 1 : 1;
            }

            if (!string.IsNullOrEmpty(e.Ip))
            {
                ipCounts[e.Ip] = ipCounts.TryGetValue(e.Ip, out int ic) ? ic + 1 : 1;
            }

            hourly[e.Timestamp!.Value.Hour]++;
        }

        return new AnalysisResult
        {
            SourceFile = sourceFile,
            TotalLines = all.Count,
            ParsedLines = filtered.Count,
            UnparsedLines = unparsed,
            TopN = topN,
            From = from,
            To = to,
            LevelCounts = levelCounts,
            StatusCounts = statusCounts,
            TopPaths = TakeTop(pathCounts, topN),
            TopIps = TakeTop(ipCounts, topN),
            HourlyHistogram = hourly,
        };
    }

    private static List<KeyValuePair<string, int>> TakeTop(
        Dictionary<string, int> counts, int topN)
    {
        return counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(Math.Max(1, topN))
            .ToList();
    }
}
