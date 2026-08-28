using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class LoggerTests : IDisposable
{
    private readonly string _testLogDir;
    private readonly string _testLogFilePath;

    public LoggerTests()
    {
        _testLogDir = Path.Combine(Path.GetTempPath(), "NodeRadarLoggerTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_testLogDir);
        _testLogFilePath = Path.Combine(_testLogDir, "noderadar_system.log");
        Logger.SetCustomLogDirectoryForTesting(_testLogDir);
    }

    public void Dispose()
    {
        Logger.SetCustomLogDirectoryForTesting(null);
        try
        {
            if (Directory.Exists(_testLogDir))
                Directory.Delete(_testLogDir, true);
        }
        catch { }
    }

    [Fact]
    public void GetLogDirectory_ReturnsValidDirectoryPath()
    {
        string logDir = Logger.GetLogDirectory();
        Assert.Equal(_testLogDir, logDir);
        Assert.True(Directory.Exists(logDir));
    }

    [Fact]
    public void Log_WritesFormattedLogToFile()
    {
        string source = "UnitTest";
        string message = "Test message " + Guid.NewGuid().ToString();
        string mac = "AA:BB:CC:DD:EE:FF";

        Logger.Log(LogLevel.Info, source, message, mac);
        Logger.FlushForTesting();

        Assert.True(File.Exists(_testLogFilePath));
        string content = File.ReadAllText(_testLogFilePath);
        Assert.Contains(message, content);
        Assert.Contains(source, content);
        Assert.Contains(mac, content);
        Assert.Contains("INFO", content);
    }

    [Fact]
    public void Log_WithoutMac_WritesFormattedLogWithoutDeviceMac()
    {
        string source = "UnitTestNoMac";
        string message = "Test message " + Guid.NewGuid().ToString();

        Logger.Log(LogLevel.Error, source, message);
        Logger.FlushForTesting();

        Assert.True(File.Exists(_testLogFilePath));
        string content = File.ReadAllText(_testLogFilePath);
        Assert.Contains(message, content);
        Assert.Contains(source, content);
        Assert.Contains("ERROR", content);
    }
}
