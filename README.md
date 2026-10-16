> 🌐 Language: **English** | [Türkçe](README_tr.md)

# logalyzer

A zero-dependency .NET 8 CLI tool that analyzes log files and prints a summary table — with an optional single-file HTML report. Built as a hiring-portfolio piece: classic `Program` + class structure (no top-level statements), manual argument parsing, and xUnit tests.

## Features

- Parses **Apache/Nginx access logs (combined format)** and generic `yyyy-MM-dd HH:mm:ss [LEVEL] message` lines (auto-detect or `--format`).
- Unknown lines are counted as **unparsed**, never silently dropped.
- Level counts (`INFO/WARN/ERROR/DEBUG`), HTTP status distribution, **top-N paths**, **top-N IPs**, **hourly histogram**.
- `--from` / `--to` date filter, `--top N` parameter.
- Console summary table + `--html report.html` (single file, embedded CSS, no external assets).
- Zero NuGet dependencies in the app itself (hand-rolled arg parsing; no JSON config needed).

## Installation

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone <repo-url>
cd log-analyzer
dotnet build logalyzer.sln -c Release
```

Run directly:

```bash
dotnet run --project src/Logalyzer -- samples/access-sample.log
```

Or as a single-file exe:

```bash
dotnet publish src/Logalyzer -c Release -o out
./out/logalyzer samples/access-sample.log --html report.html
```

## Usage

```bash
logalyzer <logfile> [--from <datetime>] [--to <datetime>] [--top N] [--html <file>] [--format auto|access|generic]
```

Examples:

```bash
logalyzer samples/access-sample.log
logalyzer samples/generic-sample.log --top 3
logalyzer app.log --from "2024-06-12" --to "2024-06-12 12:00:00" --top 10
logalyzer app.log --html report.html --format access
```

Example output (`samples/access-sample.log`):

```text
Logalyzer report — samples/access-sample.log
========================================================
Total lines:        21
Parsed lines:       20
Unparsed lines:       1

Levels:
  ERROR         2
  INFO         14
  WARN          4

HTTP status codes:
  200          14
  401           1
  403           1
  404           2
  500           1
  503           1

Top 5 paths:
       4  /products
       2  /
       2  /healthz
       2  /products/widget-42
       1  /about

Top 5 IPs:
       7  203.0.113.7
       5  198.51.100.23
       4  192.0.2.44
       2  192.0.2.10
       2  198.51.100.99

Hourly histogram (hour: count bar):
  08:00      3  ########################################
  09:00      3  ########################################
  10:00      1  #############
  11:00      3  ########################################
  ...
```

## Supported formats

| Format | Example | Notes |
|---|---|---|
| Access (combined) | `203.0.113.7 - - [12/Jun/2024:08:12:04 +0000] "GET / HTTP/1.1" 200 1532 "-" "Mozilla/5.0"` | Level derived from status: `5xx→ERROR`, `4xx→WARN`, else `INFO`; query strings stripped from paths |
| Generic | `2024-06-12 08:00:01 [INFO] ShopApp starting` | Any `[LEVEL]`; level upper-cased |

Date filter accepts `yyyy-MM-dd HH:mm:ss` or `yyyy-MM-dd`. Unparsed lines have no timestamp: they are counted in the totals but excluded from the filtered aggregates. Access-log timestamps carry a zone offset and are normalized to UTC (hourly histogram uses UTC hours).

## Project structure

```text
log-analyzer/
├── logalyzer.sln
├── src/Logalyzer/
│   ├── Program.cs            # classic entry point, injectable Run() for testability
│   ├── CliOptions.cs         # manual arg parsing, zero dependencies
│   ├── Models/LogEntry.cs
│   ├── Parsing/              # AccessLogParser, GenericLogParser, LogParser (dispatcher)
│   ├── Analysis/             # LogAnalyzer + AnalysisResult
│   └── Reporting/            # ConsoleReporter, HtmlReporter
├── tests/Logalyzer.Tests/    # xUnit (14 tests) + embedded TestData/*.log
├── samples/                  # access-sample.log, generic-sample.log (fictional data)
└── README.md / README_tr.md
```

## Tests

```bash
dotnet test logalyzer.sln
```

14 tests cover: access-line parsing, query-string stripping, status→level mapping, generic-line parsing, unparsed handling, embedded-log counts, level/status/top-path/top-IP/hourly aggregation, `--top` limiting, `--from/--to` filtering, empty input, and CLI option parsing/validation.
