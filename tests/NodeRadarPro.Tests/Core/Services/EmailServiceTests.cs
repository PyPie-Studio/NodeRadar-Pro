using LiteDB;
using NodeRadarPro.Core;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests;

public class EmailServiceTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public EmailServiceTests()
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
    public async Task SendAlertAsync_SocketException_LogsErrorToDatabase()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = true,
            SmtpHost = "256.256.256.256", // Invalid IP causes SocketException during Connect
            SmtpPort = 25,
            SmtpEmail = "test@example.com"
        };

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db);

        // Verify that a log entry was created in our isolated in-memory DB
        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Source == "EmailService" && l.Message.Contains("Network error"));
    }

    [Fact]
    public async Task SendAlertAsync_Disabled_ReturnsEarly()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = false,
            SmtpHost = "256.256.256.256",
            SmtpPort = 25,
            SmtpEmail = "test@example.com"
        };

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db);

        var logs = _db.GetLogs();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task SendAlertAsync_NoHost_ReturnsEarly()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = true,
            SmtpHost = "",
            SmtpPort = 25,
            SmtpEmail = "test@example.com"
        };

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db);

        var logs = _db.GetLogs();
        Assert.Empty(logs);
    }
}
