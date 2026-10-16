using Logalyzer.Analysis;
using Logalyzer.Parsing;
using Logalyzer.Reporting;

namespace Logalyzer;

/// <summary>Classic Program-class entry point (no top-level statements, for testability).</summary>
public static class Program
{
    public static int Main(string[] args)
    {
        return Run(args, Console.Out, Console.Error);
    }

    /// <summary>Overload with injectable writers so the flow is unit-testable.</summary>
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (!CliOptions.TryParse(args, out CliOptions options, out string error))
        {
            stderr.WriteLine($"Error: {error}");
            return 2;
        }

        if (options.ShowHelp)
        {
            stdout.WriteLine(CliOptions.Usage);
            return 0;
        }

        if (!File.Exists(options.InputFile))
        {
            stderr.WriteLine($"Error: input file not found: {options.InputFile}");
            return 1;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(options.InputFile);
        }
        catch (Exception ex)
        {
            stderr.WriteLine($"Error: cannot read '{options.InputFile}': {ex.Message}");
            return 1;
        }

        var entries = LogParser.ParseLines(lines, options.Format);
        AnalysisResult result = LogAnalyzer.Analyze(
            entries,
            sourceFile: options.InputFile,
            from: options.From,
            to: options.To,
            topN: options.Top);

        ConsoleReporter.Report(result, stdout);

        if (!string.IsNullOrEmpty(options.HtmlOutput))
        {
            try
            {
                HtmlReporter.WriteHtml(result, options.HtmlOutput);
                stdout.WriteLine();
                stdout.WriteLine($"HTML report written to: {options.HtmlOutput}");
            }
            catch (Exception ex)
            {
                stderr.WriteLine($"Error: cannot write HTML report: {ex.Message}");
                return 1;
            }
        }

        return 0;
    }
}
