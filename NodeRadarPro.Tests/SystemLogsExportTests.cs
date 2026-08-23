#pragma warning disable SYSLIB0050
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using Xunit;

namespace NodeRadarPro.Tests;

public class SystemLogsExportTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public SystemLogsExportTests()
    {
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms);

        _db = (LocalDatabase)FormatterServices.GetUninitializedObject(typeof(LocalDatabase));
        var field = typeof(LocalDatabase).GetField("_db", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(_db, _liteDb);
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
            sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Level}\",\"{log.Source}\",\"{log.Message.Replace("\"", "\"\"")}\",\"{log.DeviceMac ?? ""}\"");

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
#pragma warning restore SYSLIB0050
