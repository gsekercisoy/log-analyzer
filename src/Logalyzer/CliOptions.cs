using System.Globalization;
using Logalyzer.Parsing;

namespace Logalyzer;

/// <summary>Manually parsed CLI options (zero external dependencies).</summary>
public sealed class CliOptions
{
    public string InputFile { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Top { get; set; } = 5;
    public string? HtmlOutput { get; set; }
    public LogParser.FormatHint Format { get; set; } = LogParser.FormatHint.Auto;
    public bool ShowHelp { get; set; }

    public static string Usage => @"Usage: logalyzer <logfile> [options]

Arguments:
  <logfile>              Path to the log file to analyze.

Options:
  --from <datetime>      Only include entries at/after this time.
                         Formats: 'yyyy-MM-dd HH:mm:ss' or 'yyyy-MM-dd'.
  --to <datetime>        Only include entries at/before this time.
  --top <N>              How many rows to show for top paths/IPs (default: 5).
  --html <file>          Also write a self-contained HTML report to <file>.
  --format <auto|access|generic>
                         Force a parser instead of auto-detect (default: auto).
  -h, --help             Show this help.

Examples:
  logalyzer samples/access-sample.log
  logalyzer app.log --from ""2024-05-01"" --to ""2024-05-02"" --top 10
  logalyzer app.log --html report.html";

    public static bool TryParse(string[] args, out CliOptions options, out string error)
    {
        options = new CliOptions();
        error = string.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            switch (a)
            {
                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    return true;
                case "--from":
                    if (!TakeValue(args, ref i, out string fromRaw))
                    {
                        error = "Missing value for --from.";
                        return false;
                    }

                    if (!TryParseDate(fromRaw, out DateTime from))
                    {
                        error = $"Cannot parse --from value '{fromRaw}'. Use 'yyyy-MM-dd HH:mm:ss' or 'yyyy-MM-dd'.";
                        return false;
                    }

                    options.From = from;
                    break;
                case "--to":
                    if (!TakeValue(args, ref i, out string toRaw))
                    {
                        error = "Missing value for --to.";
                        return false;
                    }

                    if (!TryParseDate(toRaw, out DateTime to))
                    {
                        error = $"Cannot parse --to value '{toRaw}'. Use 'yyyy-MM-dd HH:mm:ss' or 'yyyy-MM-dd'.";
                        return false;
                    }

                    options.To = to;
                    break;
                case "--top":
                    if (!TakeValue(args, ref i, out string topRaw) ||
                        !int.TryParse(topRaw, out int top) || top < 1)
                    {
                        error = "Option --top needs a positive integer.";
                        return false;
                    }

                    options.Top = top;
                    break;
                case "--html":
                    if (!TakeValue(args, ref i, out string html))
                    {
                        error = "Missing value for --html.";
                        return false;
                    }

                    options.HtmlOutput = html;
                    break;
                case "--format":
                    if (!TakeValue(args, ref i, out string fmt) ||
                        !LogParser.TryParseFormatHint(fmt, out LogParser.FormatHint hint))
                    {
                        error = "Option --format must be one of: auto, access, generic.";
                        return false;
                    }

                    options.Format = hint;
                    break;
                default:
                    if (a.StartsWith("--", StringComparison.Ordinal))
                    {
                        error = $"Unknown option '{a}'.";
                        return false;
                    }

                    if (options.InputFile.Length > 0)
                    {
                        error = $"Unexpected extra argument '{a}'. Only one input file is supported.";
                        return false;
                    }

                    options.InputFile = a;
                    break;
            }
        }

        if (options.ShowHelp)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.InputFile))
        {
            error = "No input file given.\n\n" + Usage;
            return false;
        }

        if (options.From.HasValue && options.To.HasValue && options.From > options.To)
        {
            error = "Option --from must not be later than --to.";
            return false;
        }

        return true;
    }

    private static bool TakeValue(string[] args, ref int i, out string value)
    {
        value = string.Empty;
        if (i + 1 >= args.Length)
        {
            return false;
        }

        value = args[++i];
        return true;
    }

    internal static bool TryParseDate(string raw, out DateTime value)
    {
        string[] formats =
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "dd/MMM/yyyy:HH:mm:ss zzz",
        };

        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out value))
        {
            return true;
        }

        return DateTime.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out value);
    }
}
