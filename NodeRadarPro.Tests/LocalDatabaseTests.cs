#pragma warning disable SYSLIB0050 // FormatterServices is obsolete
using System.Reflection;
using System.Runtime.Serialization;
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
        _liteDb = new LiteDatabase(_ms);

        // Bypass constructor to avoid hitting the actual disk or triggering DPAPI calls which might fail in CI
        _db = (LocalDatabase)FormatterServices.GetUninitializedObject(typeof(LocalDatabase));

        // Inject the in-memory LiteDatabase instance
        var field = typeof(LocalDatabase).GetField("_db", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(_db, _liteDb);
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

        var result = _db.BackupDatabase();

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
                var result = _db.BackupDatabase(customBackupPath);

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

        var tempFilePath = Path.GetTempFileName();

        try
        {
            var pathField = typeof(LocalDatabase).GetField("_dbPath", BindingFlags.NonPublic | BindingFlags.Instance);
            pathField!.SetValue(_db, tempDbPath);

            var invalidCustomPath = Path.Combine(tempFilePath, "invalid_subfolder", "backup.db");

            var result = _db.BackupDatabase(invalidCustomPath);

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

        var readOnlyDir = Path.Combine(Path.GetTempPath(), "readonly_dir_" + Guid.NewGuid());
        Directory.CreateDirectory(readOnlyDir);

        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                new System.IO.DirectoryInfo(readOnlyDir).UnixFileMode = System.IO.UnixFileMode.UserRead | System.IO.UnixFileMode.UserExecute;
            }

            var pathField = typeof(LocalDatabase).GetField("_dbPath", BindingFlags.NonPublic | BindingFlags.Instance);
            pathField!.SetValue(_db, tempDbPath);

            var customBackupPath = Path.Combine(readOnlyDir, "backup.db");

            var result = _db.BackupDatabase(customBackupPath);

            Assert.Equal("", result);
            var logs = _db.GetLogs(levelFilter: LogLevel.Error);
            Assert.Contains(logs, l => l.Source == "Database" && l.Message.StartsWith("Backup failed:"));
        }
        finally
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                try { new System.IO.DirectoryInfo(readOnlyDir).UnixFileMode = System.IO.UnixFileMode.UserRead | System.IO.UnixFileMode.UserWrite | System.IO.UnixFileMode.UserExecute; } catch { }
            }
            if (Directory.Exists(readOnlyDir)) Directory.Delete(readOnlyDir, true);
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
}
#pragma warning restore SYSLIB0050
