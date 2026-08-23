using System;
using System.IO;
using System.Threading.Tasks;
using NodeRadarPro.Core;
using Xunit;

namespace Core.Tests
{
    public class LoggerTests
    {
        private readonly string _logDir;
        private readonly string _logFilePath;

        public LoggerTests()
        {
            _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "Logs");
            _logFilePath = Path.Combine(_logDir, "noderadar_system.log");
        }

        [Fact]
        public void GetLogDirectory_ReturnsValidDirectoryPath()
        {
            string logDir = Logger.GetLogDirectory();
            Assert.Equal(_logDir, logDir);
            Assert.True(Directory.Exists(logDir));
        }

        [Fact]
        public async Task Log_WritesFormattedLogToFile()
        {
            string source = "UnitTest";
            string message = "Test message " + Guid.NewGuid().ToString();
            string mac = "AA:BB:CC:DD:EE:FF";

            Logger.Log(LogLevel.Info, source, message, mac);

            bool logFound = false;
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(100);
                if (File.Exists(_logFilePath))
                {
                    string content = File.ReadAllText(_logFilePath);
                    if (content.Contains(message) && content.Contains(source) && content.Contains(mac) && content.Contains("INFO"))
                    {
                        logFound = true;
                        break;
                    }
                }
            }

            Assert.True(logFound, "Logged message was not found in log file within expected timeout.");
        }

        [Fact]
        public async Task Log_WithoutMac_WritesFormattedLogWithoutDeviceMac()
        {
            string source = "UnitTestNoMac";
            string message = "Test message " + Guid.NewGuid().ToString();

            Logger.Log(LogLevel.Error, source, message);

            bool logFound = false;
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(100);
                if (File.Exists(_logFilePath))
                {
                    string content = File.ReadAllText(_logFilePath);
                    if (content.Contains(message) && content.Contains(source) && content.Contains("ERROR"))
                    {
                        logFound = true;
                        break;
                    }
                }
            }

            Assert.True(logFound, "Logged message without MAC was not found in log file.");
        }
    }
}
