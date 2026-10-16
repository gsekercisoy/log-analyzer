using Logalyzer.Models;

namespace Logalyzer.Parsing;

/// <summary>
/// Dispatches each line to the access parser and/or the generic parser.
/// Unknown lines are returned as unparsed entries (never dropped).
/// </summary>
public static class LogParser
{
    public enum FormatHint
    {
        Auto,
        Access,
        Generic,
    }

    public static LogEntry ParseLine(string line, FormatHint hint = FormatHint.Auto)
    {
        switch (hint)
        {
            case FormatHint.Access:
                return AccessLogParser.TryParse(line, out LogEntry a) ? a : LogEntry.Unparsed(line);
            case FormatHint.Generic:
                return GenericLogParser.TryParse(line, out LogEntry g) ? g : LogEntry.Unparsed(line);
            default:
                // Generic first: its yyyy-MM-dd prefix is unambiguous.
                if (GenericLogParser.TryParse(line, out LogEntry generic))
                {
                    return generic;
                }

                if (AccessLogParser.TryParse(line, out LogEntry access))
                {
                    return access;
                }

                return LogEntry.Unparsed(line);
        }
    }

    public static List<LogEntry> ParseLines(IEnumerable<string> lines, FormatHint hint = FormatHint.Auto)
    {
        var result = new List<LogEntry>();
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            result.Add(ParseLine(line, hint));
        }

        return result;
    }

    public static bool TryParseFormatHint(string? value, out FormatHint hint)
    {
        hint = FormatHint.Auto;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "auto":
                hint = FormatHint.Auto;
                return true;
            case "access":
                hint = FormatHint.Access;
                return true;
            case "generic":
                hint = FormatHint.Generic;
                return true;
            default:
                return false;
        }
    }
}
