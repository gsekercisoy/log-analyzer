using System.Net;
using System.Text;
using Logalyzer.Analysis;

namespace Logalyzer.Reporting;

/// <summary>Writes a single-file HTML report with embedded CSS (no external assets).</summary>
public static class HtmlReporter
{
    public static void WriteHtml(AnalysisResult r, string outputPath)
    {
        var sb = new StringBuilder();
        sb.Append(@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>Logalyzer report</title>
<style>
body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;margin:2rem auto;max-width:900px;color:#1e293b;background:#f8fafc}
.card{background:#fff;border:1px solid #e2e8f0;border-radius:12px;padding:1.25rem 1.5rem;margin-bottom:1rem;box-shadow:0 1px 2px rgba(0,0,0,.04)}
h1{font-size:1.5rem;margin:0 0 .25rem}h2{font-size:1.1rem;margin:.25rem 0 .75rem}
table{border-collapse:collapse;width:100%;margin:.5rem 0}th,td{text-align:left;padding:.4rem .6rem;border-bottom:1px solid #e2e8f0}
th{background:#f1f5f9}td.num,th.num{text-align:right;font-variant-numeric:tabular-nums}
.bar{height:10px;background:#3b82f6;border-radius:4px}.muted{color:#64748b;font-size:.9rem}
</style>
</head>
<body>
");

        sb.Append("<div class=\"card\"><h1>Logalyzer report</h1>");
        sb.Append($"<div class=\"muted\">Source: {Esc(r.SourceFile)}");
        if (r.From.HasValue || r.To.HasValue)
        {
            sb.Append($" &middot; Filter: {Fmt(r.From)} .. {Fmt(r.To)}");
        }

        sb.Append("</div>");
        sb.Append("<table><tr><th>Total</th><th>Parsed</th><th>Unparsed</th></tr>");
        sb.Append($"<tr><td>{r.TotalLines}</td><td>{r.ParsedLines}</td><td>{r.UnparsedLines}</td></tr></table></div>");

        sb.Append("<div class=\"card\"><h2>Levels</h2>");
        AppendSimpleTable(sb, r.LevelCounts.OrderBy(kv => kv.Key)
            .Select(kv => (kv.Key, kv.Value)), "Level", "Count");

        sb.Append("<h2>HTTP status codes</h2>");
        if (r.StatusCounts.Count == 0)
        {
            sb.Append("<p class=\"muted\">No access-log entries.</p>");
        }
        else
        {
            AppendSimpleTable(sb, r.StatusCounts.OrderBy(kv => kv.Key)
                .Select(kv => (kv.Key.ToString(), kv.Value)), "Status", "Count");
        }

        sb.Append("</div><div class=\"card\">");
        sb.Append($"<h2>Top {r.TopN} paths</h2>");
        AppendSimpleTable(sb, r.TopPaths.Select(kv => (kv.Key, kv.Value)), "Path", "Count");
        sb.Append($"<h2>Top {r.TopN} IPs</h2>");
        AppendSimpleTable(sb, r.TopIps.Select(kv => (kv.Key, kv.Value)), "IP", "Count");
        sb.Append("</div><div class=\"card\"><h2>Hourly histogram</h2><table>");
        sb.Append("<tr><th>Hour</th><th class=\"num\">Count</th><th></th></tr>");
        int max = r.HourlyHistogram.Values.DefaultIfEmpty(0).Max();
        foreach (var kv in r.HourlyHistogram.OrderBy(kv => kv.Key))
        {
            int pct = max == 0 ? 0 : kv.Value * 100 / max;
            sb.Append($"<tr><td>{kv.Key:00}:00</td><td class=\"num\">{kv.Value}</td>");
            sb.Append($"<td style=\"width:60%\"><div class=\"bar\" style=\"width:{pct}%\"></div></td></tr>");
        }

        sb.Append("</table></div></body></html>");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendSimpleTable(
        StringBuilder sb, IEnumerable<(string Key, int Value)> rows, string col1, string col2)
    {
        sb.Append($"<table><tr><th>{Esc(col1)}</th><th class=\"num\">{Esc(col2)}</th></tr>");
        bool any = false;
        foreach (var row in rows)
        {
            any = true;
            sb.Append($"<tr><td>{Esc(row.Key)}</td><td class=\"num\">{row.Value}</td></tr>");
        }

        if (!any)
        {
            sb.Append("<tr><td class=\"muted\" colspan=\"2\">(none)</td></tr>");
        }

        sb.Append("</table>");
    }

    private static string Esc(string s) => WebUtility.HtmlEncode(s);
    private static string Fmt(DateTime? dt) => dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "*";
}
