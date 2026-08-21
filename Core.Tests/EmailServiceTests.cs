using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using NodeRadarPro.Core;
using NodeRadarPro.Data;

namespace Core.Tests
{
    public class EmailServiceTests : IDisposable
    {
        private string _dbPath;
        private Lazy<LocalDatabase> _originalInstance;
        private string? _originalMockEnv;

        public EmailServiceTests()
        {
            _originalMockEnv = Environment.GetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING");
            Environment.SetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING", "true");

            _dbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");

            // Backup original instance
            var instanceField = typeof(LocalDatabase).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            if (instanceField != null)
            {
                _originalInstance = (Lazy<LocalDatabase>)instanceField.GetValue(null)!;

                // Set new Lazy instance wrapping a LocalDatabase with our custom path
                instanceField.SetValue(null, new Lazy<LocalDatabase>(() => new LocalDatabase(_dbPath, "test_password")));
            }
            else
            {
                throw new InvalidOperationException("Could not find _instance field");
            }

            // Force initialization of our mocked instance
            var _ = LocalDatabase.Instance;
        }

        public void Dispose()
        {
            // Restore original instance
            var instanceField = typeof(LocalDatabase).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            if (instanceField != null)
            {
                instanceField.SetValue(null, _originalInstance);
            }

            if (File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                }
                catch { } // Best effort cleanup
            }

            Environment.SetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING", _originalMockEnv);
        }

        [Fact]
        public async Task SendAlertAsync_SocketException_LogsError()
        {
            var settings = new AppSettings
            {
                EnableEmailAlerts = true,
                SmtpHost = "256.256.256.256", // Invalid IP causes SocketException during Connect
                SmtpPort = 25,
                SmtpEmail = "test@example.com"
            };

            await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body");

            // Verify that a log entry was created
            var logs = LocalDatabase.Instance.GetLogs();
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

            await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body");

            var logs = LocalDatabase.Instance.GetLogs();
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

            await EmailService.SendAlertAsync(settings, "Test Subject", "Test Body");

            var logs = LocalDatabase.Instance.GetLogs();
            Assert.Empty(logs);
        }
    }
}
