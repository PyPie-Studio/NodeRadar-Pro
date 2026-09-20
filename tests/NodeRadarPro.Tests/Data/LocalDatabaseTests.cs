using System.Reflection;
using LiteDB;
using NodeRadarPro.Data;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class LocalDatabaseTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public LocalDatabaseTests()
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

    // -- DEVICE TESTS --

    [Fact]
    public void MergeWithHistory_NewDevice_IsInserted()
    {
        var node = new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:FF", IpAddress = "192.168.1.10" };
        var (resultNode, isNew) = _db.MergeWithHistory(node);

        Assert.True(isNew);
        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.Single(devices);
        Assert.Equal("AA:BB:CC:DD:EE:FF", devices[0].MacAddress);
    }

    [Fact]
    public void MergeWithHistory_ExistingDevice_IsUpdated()
    {
        // Setup initial device
        var initialNode = new NetworkNode { MacAddress = "11:22:33:44:55:66", IpAddress = "10.0.0.1", CustomName = "Old Name" };
        _db.MergeWithHistory(initialNode);

        // Update device
        var updatedNode = new NetworkNode { MacAddress = "11:22:33:44:55:66", IpAddress = "10.0.0.2" };
        var (resultNode, isNew) = _db.MergeWithHistory(updatedNode);

        Assert.False(isNew);
        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.Single(devices);
        Assert.Equal("10.0.0.2", devices[0].IpAddress);
        Assert.Equal("Old Name", devices[0].CustomName); // Preserves existing user fields
    }

    [Fact]
    public void MergeWithHistory_PortBanners_WhenScannedEmpty_PreservesExistingBanners()
    {
        var initialNode = new NetworkNode
        {
            MacAddress = "11:22:33:44:55:77",
            IpAddress = "10.0.0.1",
            PortBanners = new Dictionary<int, string> { { 80, "HTTP/1.1 200 OK" } }
        };
        _db.MergeWithHistory(initialNode);

        var updatedNode = new NetworkNode
        {
            MacAddress = "11:22:33:44:55:77",
            IpAddress = "10.0.0.1",
            PortBanners = new Dictionary<int, string>()
        };
        _db.MergeWithHistory(updatedNode);

        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.Single(devices);
        Assert.NotNull(devices[0].PortBanners);
        Assert.True(devices[0].PortBanners.ContainsKey(80));
        Assert.Equal("HTTP/1.1 200 OK", devices[0].PortBanners[80]);
    }

    [Fact]
    public void MergeWithHistory_PortBanners_WhenBothHaveBanners_MergesWithoutOverwritingScanned()
    {
        var initialNode = new NetworkNode
        {
            MacAddress = "11:22:33:44:55:88",
            IpAddress = "10.0.0.1",
            PortBanners = new Dictionary<int, string>
            {
                { 80, "Old HTTP" },
                { 22, "SSH-2.0-OpenSSH" }
            }
        };
        _db.MergeWithHistory(initialNode);

        var updatedNode = new NetworkNode
        {
            MacAddress = "11:22:33:44:55:88",
            IpAddress = "10.0.0.1",
            PortBanners = new Dictionary<int, string>
            {
                { 80, "New HTTP" },
                { 443, "HTTPS Server" }
            }
        };
        _db.MergeWithHistory(updatedNode);

        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.Single(devices);
        Assert.NotNull(devices[0].PortBanners);
        Assert.Equal(3, devices[0].PortBanners.Count);
        Assert.Equal("New HTTP", devices[0].PortBanners[80]);
        Assert.Equal("SSH-2.0-OpenSSH", devices[0].PortBanners[22]);
        Assert.Equal("HTTPS Server", devices[0].PortBanners[443]);
    }

    [Fact]
    public void UpdateRegistration_CreatesOrUpdatesDevice()
    {
        _db.UpdateRegistration("00:11:22:33:44:55", "Test TV", "Living Room", "Loc1", "Smart TV", "ModelX", "icon_tv");

        var registered = _db.GetRegisteredDevices();
        Assert.Single(registered);
        Assert.Equal("Test TV", registered[0].CustomName);
        Assert.True(registered[0].IsRegistered);

        // Update existing
        _db.UpdateRegistration("00:11:22:33:44:55", "Test TV 2", "Notes", "Loc2", "Smart TV", "ModelX", "icon_tv");
        var updated = _db.GetRegisteredDevices();
        Assert.Single(updated);
        Assert.Equal("Test TV 2", updated[0].CustomName);
    }

    [Fact]
    public void DeleteDevice_RemovesFromDatabase()
    {
        var node = new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:FF" };
        _db.MergeWithHistory(node);

        bool deleted = _db.DeleteDevice("AA:BB:CC:DD:EE:FF");
        Assert.True(deleted);

        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.Empty(devices);
    }

    [Fact]
    public void UpdateDeviceAlertPrefs_UpdatesCorrectly()
    {
        var node = new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:FF" };
        _db.MergeWithHistory(node);

        _db.UpdateDeviceAlertPrefs("AA:BB:CC:DD:EE:FF", true, true);

        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.True(devices[0].AlertOnConnectionLost);
        Assert.True(devices[0].AlertOnHighLatency);
    }

    // -- ALERT TESTS --

    [Fact]
    public void Alerts_InsertAndRetrieve()
    {
        var alert = new AlertEvent { MacAddress = "AA:BB", Message = "Test Alert" };
        _db.InsertAlert(alert);

        var alerts = _db.GetAlerts();
        Assert.Single(alerts);
        Assert.Equal("Test Alert", alerts[0].Message);

        Assert.Equal(1, _db.GetUnresolvedAlertCount());

        _db.ResolveAlert(alerts[0].Id);
        Assert.Equal(0, _db.GetUnresolvedAlertCount());
    }

    [Fact]
    public void Alerts_ResolveAll()
    {
        _db.InsertAlert(new AlertEvent { MacAddress = "AA:BB" });
        _db.InsertAlert(new AlertEvent { MacAddress = "CC:DD" });

        Assert.Equal(2, _db.GetUnresolvedAlertCount());

        _db.ResolveAllAlerts();
        Assert.Equal(0, _db.GetUnresolvedAlertCount());

        var resolvedAlerts = _db.GetAlerts().Where(a => a.IsResolved).ToList();
        Assert.Equal(2, resolvedAlerts.Count);
        Assert.All(resolvedAlerts, a =>
        {
            Assert.True(a.IsResolved);
            Assert.NotNull(a.ResolvedAt);
        });
    }

    [Fact]
    public void Alerts_ResolveAll_LargeDataset_PerformanceAndCorrectness()
    {
        var now = DateTime.UtcNow;
        var alerts = new List<AlertEvent>();
        for (int i = 0; i < 2000; i++)
        {
            alerts.Add(new AlertEvent
            {
                MacAddress = $"00:11:22:33:{(i % 256):X2}:{(i / 256):X2}",
                Message = $"Alert {i}",
                Timestamp = now.AddMinutes(-i),
                IsResolved = i >= 1000,
                ResolvedAt = i >= 1000 ? now.AddMinutes(-i) : null
            });
        }

        foreach (var alert in alerts)
        {
            _db.InsertAlert(alert);
        }

        Assert.Equal(1000, _db.GetUnresolvedAlertCount());

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _db.ResolveAllAlerts();
        sw.Stop();

        Assert.Equal(0, _db.GetUnresolvedAlertCount());
        var allAlerts = _db.GetAlerts(2500);
        Assert.Equal(2000, allAlerts.Count);
        Assert.All(allAlerts, a => Assert.True(a.IsResolved));
        Assert.True(sw.ElapsedMilliseconds < 500, $"ResolveAllAlerts took too long: {sw.ElapsedMilliseconds} ms");
    }

    // -- LOG TESTS --

    [Fact]
    public void Logs_InsertAndRetrieve()
    {
        _db.InsertLog(new LogEntry { Message = "Test Log 1", Level = LogLevel.Info });
        _db.InsertLog(new LogEntry { Message = "Test Log 2", Level = LogLevel.Error, DeviceMac = "AA:BB" });

        var allLogs = _db.GetLogs();
        Assert.Equal(2, allLogs.Count);

        var errorLogs = _db.GetLogs(levelFilter: LogLevel.Error);
        Assert.Single(errorLogs);
        Assert.Equal("Test Log 2", errorLogs[0].Message);

        var macLogs = _db.GetLogs(deviceMacFilter: "AA:BB");
        Assert.Single(macLogs);
        Assert.Equal("AA:BB", macLogs[0].DeviceMac);
    }

    // -- UPTIME TESTS --

    [Fact]
    public void Uptime_InsertAndRetrieve()
    {
        var snapshot = new UptimeSnapshot { MacAddress = "AA:BB", IsOnline = true, LatencyMs = 15 };
        _db.InsertUptimeSnapshots(new List<UptimeSnapshot> { snapshot });

        var history = _db.GetUptimeHistory("AA:BB");
        Assert.Single(history);
        Assert.True(history[0].IsOnline);
        Assert.Equal(15, history[0].LatencyMs);
    }

    // -- SETTINGS TESTS --

    [Fact]
    public void Settings_SaveAndLoad()
    {
        var settings = new AppSettings { MonitorIntervalSeconds = 120, SelectedInterfaceName = "eth0" };
        _db.SaveSettings(settings);

        var loaded = _db.LoadSettings();
        Assert.Equal(120, loaded.MonitorIntervalSeconds);
        Assert.Equal("eth0", loaded.SelectedInterfaceName);
    }

    // -- BACKUP DATABASE TESTS --

    [Fact]
    public void BackupDatabase_FileDoesNotExist_ReturnsEmptyString()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_" + Guid.NewGuid() + ".db");
        var pathField = typeof(LocalDatabase).GetField("_dbPath", BindingFlags.NonPublic | BindingFlags.Instance);
        pathField!.SetValue(_db, nonExistentPath);

        var result = _db.Backup.BackupDatabase();

        Assert.Equal("", result);
        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Message.Contains("Backup skipped: Database file not found"));
    }

    [Fact]
    public void BackupDatabase_SuccessfulBackup_ReturnsDestPath()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), "source_db_" + Guid.NewGuid() + ".db");
        using (var tempDb = new LiteDatabase($"Filename={tempDbPath};Password=test;Connection=shared;"))
        {
            tempDb.GetCollection<NetworkNode>("dummy").Insert(new NetworkNode { MacAddress = "AA:BB" });
        }

        try
        {
            var pathField = typeof(LocalDatabase).GetField("_dbPath", BindingFlags.NonPublic | BindingFlags.Instance);
            pathField!.SetValue(_db, tempDbPath);

            var customBackupPath = Path.Combine(Path.GetTempPath(), "custom_backup_" + Guid.NewGuid() + ".db");
            try
            {
                var result = _db.Backup.BackupDatabase(customBackupPath);

                Assert.Equal(customBackupPath, result);
                Assert.True(File.Exists(customBackupPath));
            }
            finally
            {
                if (File.Exists(customBackupPath)) File.Delete(customBackupPath);
            }
        }
        finally
        {
            if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
        }
    }

    [Fact]
    public void BackupDatabase_IOException_ReturnsEmptyStringAndLogsError()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), "source_db_" + Guid.NewGuid() + ".db");
        using (var tempDb = new LiteDatabase($"Filename={tempDbPath};Password=test;Connection=shared;"))
        {
            tempDb.GetCollection<NetworkNode>("dummy").Insert(new NetworkNode { MacAddress = "AA:BB" });
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(tempFilePath, "dummy");

        try
        {
            var pathField = typeof(LocalDatabase).GetField("_dbPath", BindingFlags.NonPublic | BindingFlags.Instance);
            pathField!.SetValue(_db, tempDbPath);

            var invalidCustomPath = Path.Combine(tempFilePath, "invalid_subfolder", "backup.db");

            var result = _db.Backup.BackupDatabase(invalidCustomPath);

            Assert.Equal("", result);
            var logs = _db.GetLogs(levelFilter: LogLevel.Error);
            Assert.Contains(logs, l => l.Source == "Database" && l.Message.StartsWith("Backup failed:"));
        }
        finally
        {
            if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
            if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
        }
    }

    [Fact]
    public void BackupDatabase_UnauthorizedAccessException_ReturnsEmptyStringAndLogsError()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), "source_db_" + Guid.NewGuid() + ".db");
        using (var tempDb = new LiteDatabase($"Filename={tempDbPath};Password=test;Connection=shared;"))
        {
            tempDb.GetCollection<NetworkNode>("dummy").Insert(new NetworkNode { MacAddress = "AA:BB" });
        }

        var customBackupPath = Path.Combine(Path.GetTempPath(), "readonly_backup_" + Guid.NewGuid() + ".db");
        File.WriteAllText(customBackupPath, "dummy");
        File.SetAttributes(customBackupPath, FileAttributes.ReadOnly);

        try
        {
            var pathField = typeof(LocalDatabase).GetField("_dbPath", BindingFlags.NonPublic | BindingFlags.Instance);
            pathField!.SetValue(_db, tempDbPath);

            var result = _db.Backup.BackupDatabase(customBackupPath);

            Assert.Equal("", result);
            var logs = _db.GetLogs(levelFilter: LogLevel.Error);
            Assert.Contains(logs, l => l.Source == "Database" && l.Message.StartsWith("Backup failed:"));
        }
        finally
        {
            try { File.SetAttributes(customBackupPath, FileAttributes.Normal); } catch { }
            if (File.Exists(customBackupPath)) File.Delete(customBackupPath);
            if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
        }
    }

    [Fact]
    public void Settings_SmtpPasswordEncryption_CrossPlatformSecure()
    {
        var settings = new AppSettings
        {
            MonitorIntervalSeconds = 120,
            SelectedInterfaceName = "eth0",
            SmtpPassword = "MySecretSmtpPassword123!"
        };
        _db.SaveSettings(settings);

        var loaded = _db.LoadSettings();
        Assert.Equal("MySecretSmtpPassword123!", loaded.SmtpPassword);

        var rawDoc = _liteDb.GetCollection("settings").FindById(1);
        Assert.NotNull(rawDoc);
        Assert.True(rawDoc.ContainsKey("SmtpPasswordEncrypted"));
        string encryptedBase64 = rawDoc["SmtpPasswordEncrypted"].AsString;

        byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes("MySecretSmtpPassword123!");
        string plainBase64 = Convert.ToBase64String(plainBytes);
        Assert.NotEqual(plainBase64, encryptedBase64);
    }

    // -- REGRESSION & PRUNING TESTS --

    [Fact]
    public void GetLogs_WithLevelFilter_ReturnsCorrectResultsRegardlessOfLimit()
    {
        // Insert 10 Info logs, then 5 Error logs, then 10 Info logs
        for (int i = 0; i < 10; i++)
        {
            _db.InsertLog(new LogEntry { Message = $"Info log {i}", Level = LogLevel.Info, Timestamp = DateTime.UtcNow.AddMinutes(-30 + i) });
        }
        for (int i = 0; i < 5; i++)
        {
            _db.InsertLog(new LogEntry { Message = $"Error log {i}", Level = LogLevel.Error, Timestamp = DateTime.UtcNow.AddMinutes(-15 + i) });
        }
        for (int i = 0; i < 10; i++)
        {
            _db.InsertLog(new LogEntry { Message = $"Recent Info {i}", Level = LogLevel.Info, Timestamp = DateTime.UtcNow.AddMinutes(i) });
        }

        // With limit 3 and filter Error, must return the 3 most recent Error logs, not 0 from query-level truncation
        var errorLogs = _db.GetLogs(limit: 3, levelFilter: LogLevel.Error);
        Assert.Equal(3, errorLogs.Count);
        Assert.All(errorLogs, l => Assert.Equal(LogLevel.Error, l.Level));
    }

    [Fact]
    public void PruneOldData_RemovesExpiredRecords()
    {
        var now = DateTime.UtcNow;

        // Uptime snapshots: 1 old (40d), 1 recent (1d)
        _db.InsertUptimeSnapshots(new List<UptimeSnapshot>
        {
            new UptimeSnapshot { MacAddress = "AA:BB", Timestamp = now.AddDays(-40), IsOnline = true },
            new UptimeSnapshot { MacAddress = "AA:BB", Timestamp = now.AddDays(-1), IsOnline = true }
        });

        // Logs: 1 old (20d), 1 recent (1d)
        _db.InsertLog(new LogEntry { Message = "Old log", Timestamp = now.AddDays(-20), Level = LogLevel.Info });
        _db.InsertLog(new LogEntry { Message = "Recent log", Timestamp = now.AddDays(-1), Level = LogLevel.Info });

        // Alerts: 1 resolved old (40d), 1 resolved recent (2d), 1 unresolved old (40d)
        _db.InsertAlert(new AlertEvent { Message = "Old resolved", Timestamp = now.AddDays(-45), IsResolved = true, ResolvedAt = now.AddDays(-40) });
        _db.InsertAlert(new AlertEvent { Message = "Recent resolved", Timestamp = now.AddDays(-5), IsResolved = true, ResolvedAt = now.AddDays(-2) });
        _db.InsertAlert(new AlertEvent { Message = "Old unresolved", Timestamp = now.AddDays(-40), IsResolved = false });

        int deleted = _db.PruneOldData(uptimeRetentionDays: 30, logRetentionDays: 14, resolvedAlertRetentionDays: 30);
        Assert.Equal(3, deleted); // 1 uptime + 1 log + 1 resolved alert

        // Verify remaining
        var uptime = _db.GetUptimeHistory("AA:BB", hours: 24 * 10);
        Assert.Single(uptime);

        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Message == "Recent log");
        Assert.DoesNotContain(logs, l => l.Message == "Old log");

        var alerts = _db.GetAlerts();
        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, a => a.Message == "Recent resolved");
        Assert.Contains(alerts, a => a.Message == "Old unresolved");
    }

    [Fact]
    public void PruneOldData_PreservesUnresolvedAlerts()
    {
        var now = DateTime.UtcNow;
        _db.InsertAlert(new AlertEvent { Message = "Persistent issue", Timestamp = now.AddDays(-90), IsResolved = false });

        int deleted = _db.PruneOldData(uptimeRetentionDays: 30, logRetentionDays: 14, resolvedAlertRetentionDays: 30);
        Assert.Equal(0, deleted);

        Assert.Equal(1, _db.GetUnresolvedAlertCount());
    }

    [Fact]
    public void InsertAlerts_BulkInsertsMultipleAlertsSuccessfully()
    {
        var alerts = new List<AlertEvent>
        {
            new AlertEvent { MacAddress = "11:22:33:44:55:66", Message = "Alert 1" },
            new AlertEvent { MacAddress = "11:22:33:44:55:67", Message = "Alert 2" }
        };

        _db.InsertAlerts(alerts);

        var retrieved = _db.GetAlerts(10);
        Assert.True(retrieved.Count >= 2);
        Assert.Contains(retrieved, a => a.Message == "Alert 1");
        Assert.Contains(retrieved, a => a.Message == "Alert 2");
    }
}
