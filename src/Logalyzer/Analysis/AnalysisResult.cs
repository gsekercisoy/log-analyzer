namespace Logalyzer.Analysis;

/// <summary>Aggregated result of analyzing a set of log entries.</summary>
public sealed class AnalysisResult
{
    public string SourceFile { get; init; } = string.Empty;
    public int TotalLines { get; init; }
    public int ParsedLines { get; init; }
    public int UnparsedLines { get; init; }
    public int TopN { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }

    public Dictionary<string, int> LevelCounts { get; init; } = new();
    public Dictionary<int, int> StatusCounts { get; init; } = new();
    public List<KeyValuePair<string, int>> TopPaths { get; init; } = new();
    public List<KeyValuePair<string, int>> TopIps { get; init; } = new();
    public Dictionary<int, int> HourlyHistogram { get; init; } = new();
}
