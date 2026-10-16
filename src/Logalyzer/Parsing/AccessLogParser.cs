using System.Globalization;
using System.Text.RegularExpressions;
using Logalyzer.Models;

namespace Logalyzer.Parsing;

/// <summary>Parses Apache/Nginx access log lines in "combined" format.</summary>
public static class AccessLogParser
{
    // 127.0.0.1 - frank [10/Oct/2000:13:55:36 -0700] "GET /apache_pb.gif HTTP/1.0" 200 2326 "ref" "ua"
    private static readonly Regex AccessRegex = new(
        @"^(?<ip>\S+) \S+ \S+ \[(?<time>[^\]]+)\] ""(?<method>[A-Z]+) (?<path>\S+)[^""]*"" (?<status>\d{3}) (?<size>\S+)(?: ""[^""]*"" ""[^""]*"")?",
        RegexOptions.Compiled);

    private static readonly string[] TimeFormats =
    {
        "dd/MMM/yyyy:HH:mm:ss zzz",
        "dd/MMM/yyyy:HH:mm:ss",
    };

    public static bool TryParse(string line, out LogEntry entry)
    {
        entry = LogEntry.Unparsed(line);
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        Match m = AccessRegex.Match(line);
        if (!m.Success)
        {
            return false;
        }

        if (!int.TryParse(m.Groups["status"].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out int status))
        {
            return false;
        }

        string timeRaw = m.Groups["time"].Value;
        DateTime timestamp = ParseAccessTime(timeRaw);

        string level = status >= 500 ? "ERROR" : status >= 400 ? "WARN" : "INFO";

        entry = new LogEntry
        {
            Parsed = true,
            Timestamp = timestamp,
            Level = level,
            Ip = m.Groups["ip"].Value,
            Method = m.Groups["method"].Value,
            Path = StripQueryString(m.Groups["path"].Value),
            StatusCode = status,
            Message = $"{m.Groups["method"].Value} {m.Groups["path"].Value} -> {status}",
            Raw = line,
        };
        return true;
    }

    private static DateTime ParseAccessTime(string raw)
    {
        // Normalize "+0000" -> "+00:00" so the "zzz" specifier parses it.
        string normalized = Regex.Replace(raw, @"([+-]\d{2})(\d{2})\s*$", "$1:$2");

        foreach (string fmt in TimeFormats)
        {
            if (DateTimeOffset.TryParseExact(normalized, fmt, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTimeOffset dto))
            {
                return dto.UtcDateTime;
            }
        }

        // Last resort: let the framework try (handles minor variants).
        if (DateTimeOffset.TryParse(normalized, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset fallback))
        {
            return fallback.UtcDateTime;
        }

        return DateTime.MinValue;
    }

    private static string StripQueryString(string path)
    {
        int q = path.IndexOf('?');
        return q >= 0 ? path.Substring(0, q) : path;
    }
}
