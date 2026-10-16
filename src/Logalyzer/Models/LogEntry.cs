namespace Logalyzer.Models;

/// <summary>A single parsed (or unparsed) log line.</summary>
public sealed class LogEntry
{
    public bool Parsed { get; init; }

    /// <summary>Null when the line could not be parsed.</summary>
    public DateTime? Timestamp { get; init; }

    /// <summary>Upper-cased level (INFO/WARN/ERROR/DEBUG). Null when unparsed.</summary>
    public string? Level { get; init; }

    public string? Ip { get; init; }
    public string? Method { get; init; }
    public string? Path { get; init; }
    public int? StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Raw { get; init; } = string.Empty;

    public static LogEntry Unparsed(string raw) => new() { Parsed = false, Raw = raw };
}
