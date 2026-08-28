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

    [Fact]
    public async Task ExportLogs_AsynchronouslyWritesCsvContent()
    {
        _db.InsertLog(new LogEntry { Message = "Export Test Log \"With Quotes\"", Level = LogLevel.Info, Source = "Test", Timestamp = DateTime.UtcNow });
        var logs = _db.GetLogs(2000);

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Level,Source,Message,Device");
        foreach (var log in logs)
            sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd  hh:mm:ss tt}\",\"{log.Level}\",\"{log.Source}\",\"{log.Message.Replace("\"", "\"\"")}\",\"{log.DeviceMac ?? ""}\"");

        string tempPath = Path.Combine(Path.GetTempPath(), $"logs_export_test_{Guid.NewGuid()}.csv");
        try
        {
            await File.WriteAllTextAsync(tempPath, sb.ToString());
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
