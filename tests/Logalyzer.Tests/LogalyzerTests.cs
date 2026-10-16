using System.Reflection;
using Xunit;
using Logalyzer.Analysis;
using Logalyzer.Models;
using Logalyzer.Parsing;

namespace Logalyzer.Tests;

public sealed class TestDataHelper
{
    public static string ReadEmbedded(string fileName)
    {
        var asm = Assembly.GetExecutingAssembly();
        string? key = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(key);
        using Stream? s = asm.GetManifestResourceStream(key!);
        Assert.NotNull(s);
        using var reader = new StreamReader(s!);
        return reader.ReadToEnd();
    }

    public static List<LogEntry> ParseEmbedded(string fileName)
    {
        string text = ReadEmbedded(fileName);
        string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        return LogParser.ParseLines(lines);
    }
}

public sealed class ParserTests
{
    [Fact]
    public void AccessLog_ParsesCombinedLine()
    {
        string line = @"192.168.1.10 - - [10/May/2024:09:15:32 +0000] ""GET /index.html HTTP/1.1"" 200 1024 ""-"" ""Mozilla/5.0""";
        LogEntry e = LogParser.ParseLine(line);

        Assert.True(e.Parsed);
        Assert.Equal("192.168.1.10", e.Ip);
        Assert.Equal("GET", e.Method);
        Assert.Equal("/index.html", e.Path);
        Assert.Equal(200, e.StatusCode);
        Assert.Equal("INFO", e.Level);
        Assert.NotNull(e.Timestamp);
    }

    [Fact]
    public void AccessLog_StripsQueryString_FromPath()
    {
        string line = @"10.0.0.5 - - [10/May/2024:10:02:11 +0000] ""GET /api/users?page=2 HTTP/1.1"" 200 100 ""-"" ""x""";
        LogEntry e = LogParser.ParseLine(line);

        Assert.True(e.Parsed);
        Assert.Equal("/api/users", e.Path);
    }

    [Fact]
    public void AccessLog_MapsStatusToLevel()
    {
        var warn = LogParser.ParseLine(@"10.0.0.5 - - [10/May/2024:10:05:44 +0000] ""POST /api/login HTTP/1.1"" 401 128 ""-"" ""x""");
        var err = LogParser.ParseLine(@"10.0.0.5 - - [10/May/2024:10:06:02 +0000] ""GET /x HTTP/1.1"" 500 64 ""-"" ""x""");

        Assert.Equal("WARN", warn.Level);
        Assert.Equal("ERROR", err.Level);
    }

    [Fact]
    public void GenericLog_ParsesLevelAndTimestamp()
    {
        LogEntry e = LogParser.ParseLine("2024-05-01 09:16:33 [WARN] Slow query detected");

        Assert.True(e.Parsed);
        Assert.Equal("WARN", e.Level);
        Assert.Equal(new DateTime(2024, 5, 1, 9, 16, 33), e.Timestamp);
        Assert.Contains("Slow query", e.Message);
    }

    [Fact]
    public void UnknownLine_IsUnparsed_NotDropped()
    {
        LogEntry e = LogParser.ParseLine("this is garbage ((( ");

        Assert.False(e.Parsed);
        Assert.Null(e.Timestamp);
        Assert.Equal("this is garbage ((( ", e.Raw);
    }

    [Fact]
    public void EmbeddedAccessTestLog_HasExpectedParsedAndUnparsedCounts()
    {
        var entries = TestDataHelper.ParseEmbedded("access-test.log");

        Assert.Equal(7, entries.Count);
        Assert.Equal(6, entries.Count(e => e.Parsed));
        Assert.Equal(1, entries.Count(e => !e.Parsed));
    }

    [Fact]
    public void EmbeddedGenericTestLog_HasExpectedParsedAndUnparsedCounts()
    {
        var entries = TestDataHelper.ParseEmbedded("generic-test.log");

        Assert.Equal(8, entries.Count);
        Assert.Equal(7, entries.Count(e => e.Parsed));
        Assert.Single(entries.Where(e => !e.Parsed));
    }
}

public sealed class AnalyzerTests
{
    [Fact]
    public void Analyze_CountsLevels_StatusCodes_TopPaths_TopIps_Hourly()
    {
        var entries = TestDataHelper.ParseEmbedded("access-test.log");
        AnalysisResult r = LogAnalyzer.Analyze(entries, topN: 5);

        Assert.Equal(7, r.TotalLines);
        Assert.Equal(1, r.UnparsedLines);
        Assert.Equal(6, r.ParsedLines);

        // 200 x3 -> INFO, 401+404 -> WARN, 500 -> ERROR
        Assert.Equal(3, r.LevelCounts["INFO"]);
        Assert.Equal(2, r.LevelCounts["WARN"]);
        Assert.Equal(1, r.LevelCounts["ERROR"]);

        Assert.Equal(3, r.StatusCounts[200]);
        Assert.Equal(1, r.StatusCounts[500]);

        Assert.Equal("/index.html", r.TopPaths[0].Key);
        Assert.Equal(2, r.TopPaths[0].Value);

        Assert.Equal("192.168.1.10", r.TopIps[0].Key);
        Assert.Equal(3, r.TopIps[0].Value);

        Assert.Equal(2, r.HourlyHistogram[9]);
        Assert.Equal(3, r.HourlyHistogram[10]);
        Assert.Equal(1, r.HourlyHistogram[11]);
    }

    [Fact]
    public void Analyze_TopN_LimitsResults()
    {
        var entries = TestDataHelper.ParseEmbedded("access-test.log");
        AnalysisResult r = LogAnalyzer.Analyze(entries, topN: 2);

        Assert.Equal(2, r.TopPaths.Count);
        Assert.Equal(2, r.TopIps.Count);
    }

    [Fact]
    public void Analyze_FromTo_FiltersByTimestamp()
    {
        var entries = TestDataHelper.ParseEmbedded("generic-test.log");

        AnalysisResult r = LogAnalyzer.Analyze(
            entries,
            from: new DateTime(2024, 5, 2, 0, 0, 0),
            to: new DateTime(2024, 5, 2, 23, 59, 59));

        Assert.Equal(2, r.ParsedLines); // only the two 2024-05-02 entries
        Assert.Equal(1, r.UnparsedLines); // unparsed still counted
        Assert.Equal(2, r.LevelCounts.Count); // ERROR + INFO on that day
        Assert.Equal(1, r.LevelCounts["ERROR"]);
        Assert.Equal(1, r.LevelCounts["INFO"]);
    }

    [Fact]
    public void Analyze_EmptyInput_ReturnsZeroedResult()
    {
        AnalysisResult r = LogAnalyzer.Analyze(Enumerable.Empty<LogEntry>());

        Assert.Equal(0, r.TotalLines);
        Assert.Equal(0, r.UnparsedLines);
        Assert.Empty(r.TopPaths);
        Assert.Empty(r.TopIps);
        Assert.Equal(24, r.HourlyHistogram.Count);
    }
}

public sealed class FilterAndCliTests
{
    [Fact]
    public void CliOptions_ParsesFrom_To_Top_Html()
    {
        bool ok = CliOptions.TryParse(
            new[] { "app.log", "--from", "2024-05-01", "--to", "2024-05-02 10:00:00", "--top", "3", "--html", "r.html" },
            out CliOptions o,
            out string err);

        Assert.True(ok, err);
        Assert.Equal("app.log", o.InputFile);
        Assert.Equal(new DateTime(2024, 5, 1), o.From);
        Assert.Equal(3, o.Top);
        Assert.Equal("r.html", o.HtmlOutput);
    }

    [Fact]
    public void CliOptions_RejectsFromAfterTo()
    {
        bool ok = CliOptions.TryParse(
            new[] { "app.log", "--from", "2024-05-03", "--to", "2024-05-01" },
            out _,
            out string err);

        Assert.False(ok);
        Assert.Contains("from", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CliOptions_RejectsUnknownFlag()
    {
        Assert.False(CliOptions.TryParse(new[] { "a.log", "--bogus" }, out _, out _));
    }
}
