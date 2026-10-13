using LiteDB;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Moq;
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
        GC.SuppressFinalize(this);
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
    public async Task SendAlertAsync_AuthenticationException_LogsErrorToDatabase()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUser = "user@example.com",
            SmtpPassword = "wrongpassword",
            SmtpEmail = "test@example.com"
        };

        var mockClient = new Mock<ISmtpClient>();
        mockClient.Setup(c => c.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, default))
                  .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword, default))
                  .ThrowsAsync(new AuthenticationException("Invalid username or password"));

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db, mockClient.Object);

        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Source == "EmailService" && l.Message.Contains("SMTP Authentication failed: Invalid username or password"));
    }

    [Fact]
    public async Task SendAlertAsync_SmtpCommandException_LogsErrorToDatabase()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpEmail = "test@example.com"
        };

        var mockClient = new Mock<ISmtpClient>();
        mockClient.Setup(c => c.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, default))
                  .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), default, null))
                  .ThrowsAsync(new SmtpCommandException(SmtpErrorCode.MessageNotAccepted, SmtpStatusCode.TransactionFailed, "Message refused"));

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db, mockClient.Object);

        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Source == "EmailService" && l.Message.Contains("SMTP Command failed: Message refused"));
    }

    [Fact]
    public async Task SendAlertAsync_SmtpProtocolException_LogsErrorToDatabase()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpEmail = "test@example.com"
        };

        var mockClient = new Mock<ISmtpClient>();
        mockClient.Setup(c => c.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, default))
                  .ThrowsAsync(new SmtpProtocolException("Unexpected response from SMTP server"));

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db, mockClient.Object);

        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Source == "EmailService" && l.Message.Contains("SMTP Protocol error: Unexpected response from SMTP server"));
    }

    [Fact]
    public async Task SendAlertAsync_IOException_LogsErrorToDatabase()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpEmail = "test@example.com"
        };

        var mockClient = new Mock<ISmtpClient>();
        mockClient.Setup(c => c.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, default))
                  .ThrowsAsync(new IOException("Connection reset by peer"));

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db, mockClient.Object);

        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Source == "EmailService" && l.Message.Contains("I/O error: Connection reset by peer"));
    }

    [Fact]
    public async Task SendAlertAsync_GenericException_LogsErrorToDatabase()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpEmail = "test@example.com"
        };

        var mockClient = new Mock<ISmtpClient>();
        mockClient.Setup(c => c.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, default))
                  .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db, mockClient.Object);

        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Source == "EmailService" && l.Message.Contains("Failed to send email alert: Unexpected error"));
    }

    [Fact]
    public async Task SendAlertAsync_Success_SendsEmailAndDisconnects()
    {
        var settings = new AppSettings
        {
            EnableEmailAlerts = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUser = "user@example.com",
            SmtpPassword = "password",
            SmtpEmail = "test@example.com"
        };

        var mockClient = new Mock<ISmtpClient>();
        mockClient.Setup(c => c.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, default))
                  .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword, default))
                  .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), default, null))
                  .ReturnsAsync("OK");
        mockClient.Setup(c => c.DisconnectAsync(true, default))
                  .Returns(Task.CompletedTask);

        await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body", _db, mockClient.Object);

        var logs = _db.GetLogs();
        Assert.Empty(logs);

        mockClient.Verify(c => c.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, default), Times.Once);
        mockClient.Verify(c => c.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword, default), Times.Once);
        mockClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), default, null), Times.Once);
        mockClient.Verify(c => c.DisconnectAsync(true, default), Times.Once);
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
