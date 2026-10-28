using NodeRadarPro.Core;
using NodeRadarPro.Core.Services;

namespace NodeRadarPro.Tests;

public class SystemLogsExportTests
{
    [Fact]
    public void ExportLogsToCsv_ValidLogs_FormatsRfc4180With12HourAmPmTimestamps()
    {
        var logs = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = new DateTime(2025, 1, 15, 14, 30, 45),
                Level = LogLevel.Info,
                Source = "Scanner",
                Message = "Standard message",
                DeviceMac = "00:11:22:33:44:55"
            },
            new LogEntry
            {
                Timestamp = new DateTime(2025, 1, 15, 9, 15, 0),
                Level = LogLevel.Warning,
                Source = "Detector",
                Message = "Warning alert",
                DeviceMac = null
            }
        };

        string csv = CsvExportService.ExportLogsToCsv(logs);

        Assert.StartsWith("Timestamp,Level,Source,Message,Device", csv);
        Assert.Contains("\"2025-01-15  02:30:45 PM\",\"Info\",\"Scanner\",\"Standard message\",\"00:11:22:33:44:55\"", csv);
        Assert.Contains("\"2025-01-15  09:15:00 AM\",\"Warning\",\"Detector\",\"Warning alert\",\"\"", csv);
    }

    [Fact]
    public void ExportLogsToCsv_EscapesQuotesAndHandlesSpecialChars()
    {
        var logs = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = new DateTime(2025, 5, 10, 16, 0, 0),
                Level = LogLevel.Error,
                Source = "Database",
                Message = "Error with \"quotes\" and details",
                DeviceMac = "AA:BB:CC:DD:EE:FF"
            }
        };

        string csv = CsvExportService.ExportLogsToCsv(logs);

        Assert.Contains("\"Error with \"\"quotes\"\" and details\"", csv);
    }

    [Fact]
    public void ExportLogsToCsv_MultiLineMessageWithQuotes_PreservesRfc4180Formatting()
    {
        var logs = new List<LogEntry>
        {
            new LogEntry
            {
                Timestamp = new DateTime(2025, 6, 1, 10, 30, 0),
                Level = LogLevel.Error,
                Source = "Engine",
                Message = "First line of message.\r\nSecond line with \"nested quotes\".\r\nThird line.",
                DeviceMac = "00:11:22:33:44:55"
            }
        };

        string csv = CsvExportService.ExportLogsToCsv(logs);

        Assert.Contains("\"First line of message.\r\nSecond line with \"\"nested quotes\"\".\r\nThird line.\"", csv);
    }

    [Fact]
    public void ExportLogsToCsv_NullOrEmptyCollection_ReturnsHeaderOnly()
    {
        string csvNull = CsvExportService.ExportLogsToCsv(null);
        Assert.Equal("Timestamp,Level,Source,Message,Device\r\n", csvNull);

        string csvEmpty = CsvExportService.ExportLogsToCsv(new List<LogEntry>());
        Assert.Equal("Timestamp,Level,Source,Message,Device\r\n", csvEmpty);
    }

    [Fact]
    public void ExportNodesToCsv_ValidNodes_FormatsCsvWithSemicolonDelimitedPorts()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode
            {
                IpAddress = "192.168.1.100",
                MacAddress = "00:11:22:33:44:55",
                Hostname = "Workstation",
                Vendor = "Intel Corp",
                DeviceType = "Desktop",
                IsOnline = true,
                PingLatencyMs = 4,
                OpenPorts = new List<int> { 80, 443, 22 },
                OsGuess = "Windows 11"
            },
            new NetworkNode
            {
                IpAddress = "192.168.1.200",
                MacAddress = "AA:BB:CC:DD:EE:00",
                Hostname = "Printer",
                Vendor = "HP Inc.",
                DeviceType = "Printer",
                IsOnline = false,
                PingLatencyMs = 0,
                OpenPorts = new List<int>(),
                OsGuess = ""
            }
        };

        string csv = CsvExportService.ExportNodesToCsv(nodes);

        Assert.StartsWith("IP Address,MAC Address,Hostname,Vendor,Device Type,Status,Latency (ms),Open Ports,OS Guess", csv);
        Assert.Contains("\"192.168.1.100\",\"00:11:22:33:44:55\",\"Workstation\",\"Intel Corp\",\"Desktop\",\"Online\",\"4\",\"80;443;22\",\"Windows 11\"", csv);
        Assert.Contains("\"192.168.1.200\",\"AA:BB:CC:DD:EE:00\",\"Printer\",\"HP Inc.\",\"Printer\",\"Offline\",\"0\",\"\",\"\"", csv);
    }

    [Fact]
    public void ExportNodesToCsv_EscapesQuotesInHostnamesAndVendors()
    {
        var nodes = new List<NetworkNode>
        {
            new NetworkNode
            {
                IpAddress = "10.0.0.1",
                MacAddress = "11:22:33:44:55:66",
                Hostname = "John's \"Fast\" PC",
                Vendor = "Custom \"OEM\" Vendor",
                DeviceType = "PC",
                IsOnline = true,
                PingLatencyMs = 2
            }
        };

        string csv = CsvExportService.ExportNodesToCsv(nodes);

        Assert.Contains("\"John's \"\"Fast\"\" PC\"", csv);
        Assert.Contains("\"Custom \"\"OEM\"\" Vendor\"", csv);
    }

    [Fact]
    public void ExportNodesToCsv_NullOrEmptyCollection_ReturnsHeaderOnly()
    {
        string csvNull = CsvExportService.ExportNodesToCsv(null);
        Assert.Equal("IP Address,MAC Address,Hostname,Vendor,Device Type,Status,Latency (ms),Open Ports,OS Guess\r\n", csvNull);

        string csvEmpty = CsvExportService.ExportNodesToCsv(new List<NetworkNode>());
        Assert.Equal("IP Address,MAC Address,Hostname,Vendor,Device Type,Status,Latency (ms),Open Ports,OS Guess\r\n", csvEmpty);
    }

    [Fact]
    public async Task ExportLogs_AsynchronouslyWritesCsvContentToFile()
    {
        var logs = new List<LogEntry>
        {
            new LogEntry { Message = "Export Test Log \"With Quotes\"", Level = LogLevel.Info, Source = "Test", Timestamp = DateTime.UtcNow }
        };

        string csvContent = CsvExportService.ExportLogsToCsv(logs);
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
