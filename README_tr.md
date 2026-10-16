> 🌐 Dil: [English](README.md) | **Türkçe**

# logalyzer

Log dosyalarını analiz edip özet tablosu basan, bağımlılıksız bir .NET 8 CLI aracı — isteğe bağlı tek dosyalık HTML raporu da üretir. İşe alım vitrini olarak hazırlandı: klasik `Program` + sınıf yapısı (top-level statement yok), elle argüman ayrıştırma ve xUnit testleri.

## Özellikler

- **Apache/Nginx access log (combined)** ve `yyyy-MM-dd HH:mm:ss [LEVEL] mesaj` satırlarını ayrıştırır (otomatik algı veya `--format`).
- Bilinmeyen satırlar sessizce atılmaz, **unparsed** sayılır.
- Seviye sayımları (`INFO/WARN/ERROR/DEBUG`), HTTP status dağılımı, **top-N path**, **top-N IP**, **saatlik histogram**.
- `--from` / `--to` tarih filtresi, `--top N` parametresi.
- Konsol özet tablosu + `--html rapor.html` (tek dosya, gömülü CSS, harici dosya yok).
- Uygulamanın kendisinde **sıfır NuGet bağımlılığı** (elle argüman parse).

## Kurulum

Gereksinim: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone <repo-url>
cd log-analyzer
dotnet build logalyzer.sln -c Release
```

Doğrudan çalıştırma:

```bash
dotnet run --project src/Logalyzer -- samples/access-sample.log
```

Tek dosyalık exe olarak:

```bash
dotnet publish src/Logalyzer -c Release -o out
./out/logalyzer samples/access-sample.log --html rapor.html
```

## Kullanım

```bash
logalyzer <logdosyası> [--from <tarih>] [--to <tarih>] [--top N] [--html <dosya>] [--format auto|access|generic]
```

Örnekler:

```bash
logalyzer samples/access-sample.log
logalyzer samples/generic-sample.log --top 3
logalyzer app.log --from "2024-06-12" --to "2024-06-12 12:00:00" --top 10
logalyzer app.log --html rapor.html --format access
```

Örnek çıktı (`samples/access-sample.log`):

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

## Desteklenen formatlar

| Format | Örnek | Notlar |
|---|---|---|
| Access (combined) | `203.0.113.7 - - [12/Jun/2024:08:12:04 +0000] "GET / HTTP/1.1" 200 1532 "-" "Mozilla/5.0"` | Seviye status kodundan türetilir: `5xx→ERROR`, `4xx→WARN`, diğerleri `INFO`; path'lerdeki query string atılır |
| Generic | `2024-06-12 08:00:01 [INFO] ShopApp starting` | Herhangi bir `[SEVİYE]`; seviye büyük harfe çevrilir |

Tarih filtresi `yyyy-MM-dd HH:mm:ss` veya `yyyy-MM-dd` kabul eder. Unparsed satırların zaman damgası yoktur: toplamlara dahil edilir ama filtreli özetlere katılmaz. Access-log zaman damgalarındaki saat dilimi UTC'ye normalize edilir (saatlik histogram UTC saatlerini kullanır).

## Klasör yapısı

```text
log-analyzer/
├── logalyzer.sln
├── src/Logalyzer/
│   ├── Program.cs            # klasik giriş noktası, test edilebilir Run() metodu
│   ├── CliOptions.cs         # bağımlılıksız elle argüman ayrıştırma
│   ├── Models/LogEntry.cs
│   ├── Parsing/              # AccessLogParser, GenericLogParser, LogParser (yönlendirici)
│   ├── Analysis/             # LogAnalyzer + AnalysisResult
│   └── Reporting/            # ConsoleReporter, HtmlReporter
├── tests/Logalyzer.Tests/    # xUnit (14 test) + gömülü TestData/*.log
├── samples/                  # access-sample.log, generic-sample.log (uydurma veri)
└── README.md / README_tr.md
```

## Testler

```bash
dotnet test logalyzer.sln
```

14 test; kapsam: access satırı ayrıştırma, query-string temizliği, status→seviye eşlemesi, generic satır ayrıştırma, unparsed satırlar, gömülü log sayımları, seviye/status/top-path/top-IP/saatlik histogram toplama, `--top` sınırlama, `--from/--to` filtreleme, boş girdi ve CLI seçenek ayrıştırma/doğrulama.
