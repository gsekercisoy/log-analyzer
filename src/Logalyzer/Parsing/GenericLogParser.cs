using System.Globalization;
using System.Text.RegularExpressions;
using Logalyzer.Models;

namespace Logalyzer.Parsing;

/// <summary>Parses generic lines like: 2024-05-01 13:55:36 [ERROR] Something broke</summary>
public static class GenericLogParser
{
    private static readonly Regex GenericRegex = new(
        @"^(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>[A-Za-z]+)\] (?<msg>.*)$",
        RegexOptions.Compiled);

    public static bool TryParse(string line, out LogEntry entry)
    {
        entry = LogEntry.Unparsed(line);
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        Match m = GenericRegex.Match(line);
        if (!m.Success)
        {
            return false;
        }

        if (!DateTime.TryParseExact(m.Groups["ts"].Value, "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ts))
        {
            return false;
        }

        entry = new LogEntry
        {
            Parsed = true,
            Timestamp = ts,
            Level = m.Groups["level"].Value.ToUpperInvariant(),
            Message = m.Groups["msg"].Value,
            Raw = line,
        };
        return true;
    }
}
