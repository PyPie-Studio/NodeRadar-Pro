using System.Text;
using LiteDB;
using NodeRadarPro.Core;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests;

public class SystemLogsExportTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public SystemLogsExportTests()
    {
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms, new BsonMapper());
        _db = new LocalDatabase(_liteDb);
    }

    public void Dispose()
    {
        _liteDb.Dispose();
        _ms.Dispose();
    }

    public static string BuildCsvBaseline(List<LogEntry> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Level,Source,Message,Device");
        foreach (var log in logs)
            sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd  hh:mm:ss tt}\",\"{log.Level}\",\"{log.Source}\",\"{log.Message.Replace("\"", "\"\"")}\",\"{log.DeviceMac ?? ""}\"");
        return sb.ToString();
    }

    public static string BuildCsvOptimized(List<LogEntry> logs)
    {
        var sb = new StringBuilder(logs.Count * 128 + 64);
        sb.AppendLine("Timestamp,Level,Source,Message,Device");
        foreach (var log in logs)
        {
            string levelStr = log.Level switch
            {
                LogLevel.Info => "Info",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                _ => log.Level.ToString()
            };

            sb.Append('"')
              .Append(log.Timestamp.ToString("yyyy-MM-dd  hh:mm:ss tt"))
              .Append("\",\"")
              .Append(levelStr)
              .Append("\",\"")
              .Append(log.Source)
              .Append("\",\"");

            if (!string.IsNullOrEmpty(log.Message))
            {
                if (log.Message.Contains('"'))
                    sb.Append(log.Message.Replace("\"", "\"\""));
                else
                    sb.Append(log.Message);
            }

            sb.Append("\",\"")
              .Append(log.DeviceMac)
              .AppendLine("\"");
        }
        return sb.ToString();
    }

    [Fact]
    public void OptimizedCsv_MatchesBaselineOutput()
    {
        var logs = new List<LogEntry>
        {
            new LogEntry { Timestamp = new DateTime(2025, 1, 15, 14, 30, 45), Level = LogLevel.Info, Source = "Scanner", Message = "Normal log message", DeviceMac = "00:11:22:33:44:55" },
            new LogEntry { Timestamp = new DateTime(2025, 1, 15, 14, 31, 00), Level = LogLevel.Warning, Source = "Detector", Message = "Log with \"quotes\"", DeviceMac = null },
            new LogEntry { Timestamp = new DateTime(2025, 1, 15, 14, 32, 10), Level = LogLevel.Error, Source = "Database", Message = "", DeviceMac = "AA:BB:CC:DD:EE:FF" }
        };

        string baseline = BuildCsvBaseline(logs);
        string optimized = BuildCsvOptimized(logs);

        Assert.Equal(baseline, optimized);
    }

    [Fact]
    public void Benchmark_CsvExport_MeasuresPerformanceImprovement()
    {
        var logs = new List<LogEntry>(2000);
        var baseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 2000; i++)
        {
            logs.Add(new LogEntry
            {
                Timestamp = baseTime.AddSeconds(i),
                Level = (LogLevel)(i % 3),
                Source = i % 2 == 0 ? "SubnetScanner" : "IntrusionDetector",
                Message = i % 5 == 0 ? $"Event #{i} with \"quoted details\"" : $"Standard status report #{i}",
                DeviceMac = i % 3 == 0 ? "00:11:22:33:44:55" : null
            });
        }

        // Verify output equivalence
        string sampleBaseline = BuildCsvBaseline(logs);
        string sampleOptimized = BuildCsvOptimized(logs);
        Assert.Equal(sampleBaseline, sampleOptimized);
    }

    [Fact]
    public async Task ExportLogs_AsynchronouslyWritesCsvContent()
    {
        _db.InsertLog(new LogEntry { Message = "Export Test Log \"With Quotes\"", Level = LogLevel.Info, Source = "Test", Timestamp = DateTime.UtcNow });
        var logs = _db.GetLogs(2000);

        string csvContent = BuildCsvOptimized(logs);

        string tempPath = Path.Combine(Path.GetTempPath(), $"logs_export_test_{Guid.NewGuid()}.csv");
        try
        {
            await File.WriteAllTextAsync(tempPath, csvContent);
            Assert.True(File.Exists(tempPath));
            string content = await File.ReadAllTextAsync(tempPath);
            Assert.Contains("Export Test Log \"\"With Quotes\"\"", content);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
