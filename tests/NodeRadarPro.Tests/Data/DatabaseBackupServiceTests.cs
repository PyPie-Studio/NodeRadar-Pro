using System.Reflection;
using LiteDB;
using NodeRadarPro.Core;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests.Data;

public class DatabaseBackupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;
    private readonly LocalDatabase _db;

    public DatabaseBackupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DatabaseBackupServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _dbPath = Path.Combine(_tempDir, "test_noderadar.db");
        _db = new LocalDatabase(_dbPath, "testpass");
    }

    public void Dispose()
    {
        try
        {
            _db.DisposeConnection();
        }
        catch { }

        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public void Constructor_NullDatabase_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DatabaseBackupService(null!));
    }

    [Fact]
    public void BackupDatabase_SourceFileDoesNotExist_ReturnsEmptyStringAndLogsInfo()
    {
        var nonExistentDbPath = Path.Combine(_tempDir, "non_existent_" + Guid.NewGuid().ToString("N") + ".db");
        var pathField = typeof(LocalDatabase).GetField("_dbPath", BindingFlags.NonPublic | BindingFlags.Instance);
        pathField!.SetValue(_db, nonExistentDbPath);

        var result = _db.Backup.BackupDatabase();

        Assert.Equal("", result);
        var logs = _db.GetLogs(levelFilter: LogLevel.Info);
        Assert.Contains(logs, l => l.Message.Contains("Backup skipped: Database file not found"));
    }

    [Fact]
    public void BackupDatabase_CustomPath_CopiesDatabaseToCustomPath()
    {
        _db.InsertLog(new LogEntry { Message = "Backup test log", Level = LogLevel.Info });

        string customBackupPath = Path.Combine(_tempDir, "custom_backups", "my_backup.db");
        string result = _db.Backup.BackupDatabase(customBackupPath);

        Assert.Equal(customBackupPath, result);
        Assert.True(File.Exists(customBackupPath));

        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Message.Contains($"Database backed up to: {customBackupPath}"));
    }

    [Fact]
    public void BackupDatabase_DefaultPath_CreatesBackupsFolderAndBacksUpFile()
    {
        _db.InsertLog(new LogEntry { Message = "Default backup test", Level = LogLevel.Info });

        string result = _db.Backup.BackupDatabase();

        Assert.NotEmpty(result);
        Assert.True(File.Exists(result));
        Assert.Contains("Backups", result);

        string backupDir = Path.Combine(Path.GetDirectoryName(_dbPath)!, "Backups");
        Assert.True(Directory.Exists(backupDir));
    }

    [Fact]
    public void BackupDatabase_IOException_ReturnsEmptyStringAndLogsError()
    {
        var invalidDirFile = Path.Combine(_tempDir, "file_as_dir");
        File.WriteAllText(invalidDirFile, "not a directory");

        var invalidCustomPath = Path.Combine(invalidDirFile, "subfolder", "backup.db");

        string result = _db.Backup.BackupDatabase(invalidCustomPath);

        Assert.Equal("", result);
        var errorLogs = _db.GetLogs(levelFilter: LogLevel.Error);
        Assert.Contains(errorLogs, l => l.Message.StartsWith("Backup failed:"));
    }

    [Fact]
    public void BackupDatabase_UnauthorizedAccessException_ReturnsEmptyStringAndLogsError()
    {
        string readonlyBackupPath = Path.Combine(_tempDir, "readonly_backup.db");
        File.WriteAllText(readonlyBackupPath, "dummy content");
        File.SetAttributes(readonlyBackupPath, FileAttributes.ReadOnly);

        try
        {
            string result = _db.Backup.BackupDatabase(readonlyBackupPath);

            Assert.Equal("", result);
            var errorLogs = _db.GetLogs(levelFilter: LogLevel.Error);
            Assert.Contains(errorLogs, l => l.Message.StartsWith("Backup failed:"));
        }
        finally
        {
            try { File.SetAttributes(readonlyBackupPath, FileAttributes.Normal); } catch { }
        }
    }

    [Fact]
    public void BackupDatabase_CleanupOldBackups_KeepsOnlyLatestSevenBackups()
    {
        string backupDir = Path.Combine(_tempDir, "Backups");
        Directory.CreateDirectory(backupDir);

        // Create 10 dummy backup files with staggered creation timestamps
        DateTime now = DateTime.Now;
        for (int i = 0; i < 10; i++)
        {
            string oldBackupPath = Path.Combine(backupDir, $"noderadar_backup_2026010{i}_120000.db");
            File.WriteAllText(oldBackupPath, $"backup content {i}");
            File.SetCreationTime(oldBackupPath, now.AddHours(-i * 2));
        }

        Assert.Equal(10, Directory.GetFiles(backupDir, "noderadar_backup_*.db").Length);

        // Perform backup using default path which triggers CleanupOldBackups
        string newBackup = _db.Backup.BackupDatabase();

        Assert.NotEmpty(newBackup);
        // Total backups should now be 7 (6 of the previous 10 + 1 new backup)
        var remainingFiles = Directory.GetFiles(backupDir, "noderadar_backup_*.db");
        Assert.Equal(7, remainingFiles.Length);
    }

    [Fact]
    public void RestoreDatabase_BackupFileDoesNotExist_ReturnsFalse()
    {
        string nonExistentBackup = Path.Combine(_tempDir, "non_existent_backup.db");
        bool result = _db.Backup.RestoreDatabase(nonExistentBackup);

        Assert.False(result);
    }

    [Fact]
    public void RestoreDatabase_ValidBackup_RestoresDataSuccessfully()
    {
        _db.InsertLog(new LogEntry { Message = "Pre-backup record", Level = LogLevel.Info });
        string backupPath = _db.Backup.BackupDatabase();
        Assert.NotEmpty(backupPath);

        // Modify the DB after backup
        _db.InsertLog(new LogEntry { Message = "Post-backup record", Level = LogLevel.Warning });

        bool restored = _db.Backup.RestoreDatabase(backupPath);

        Assert.True(restored);

        var logs = _db.GetLogs();
        Assert.Contains(logs, l => l.Message == "Pre-backup record");
        Assert.DoesNotContain(logs, l => l.Message == "Post-backup record");
    }

    [Fact]
    public void RestoreDatabase_CorruptBackup_ReturnsFalseAndReopensDatabase()
    {
        string corruptBackupPath = Path.Combine(_tempDir, "corrupt_backup.db");
        // Create a full page (8192 bytes) of invalid garbage bytes so LiteDB page header parsing fails
        byte[] garbage = new byte[16384];
        Array.Fill(garbage, (byte)0xFF);
        File.WriteAllBytes(corruptBackupPath, garbage);

        bool result = _db.Backup.RestoreDatabase(corruptBackupPath);

        Assert.False(result);

        // Database should have been re-opened, remaining operational
        var logs = _db.GetLogs(levelFilter: LogLevel.Error);
        Assert.Contains(logs, l => l.Message.Contains("Database restore failed: Database structure invalid/corrupted"));

        // Confirm database connection is usable after reopening
        _db.InsertLog(new LogEntry { Message = "Log after failed restore", Level = LogLevel.Info });
        var currentLogs = _db.GetLogs();
        Assert.Contains(currentLogs, l => l.Message == "Log after failed restore");
    }

    [Fact]
    public void RestoreDatabase_IOException_ReturnsFalseAndReopensDatabase()
    {
        // Create valid backup first
        string validBackup = _db.Backup.BackupDatabase();

        // Lock the source db file or destination path by holding an exclusive stream
        using (var stream = new FileStream(_dbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            bool result = _db.Backup.RestoreDatabase(validBackup);
            Assert.False(result);
        }

        var errorLogs = _db.GetLogs(levelFilter: LogLevel.Error);
        Assert.Contains(errorLogs, l => l.Message.Contains("Database restore failed: File in use or I/O error"));
    }

    [Fact]
    public void RotateCorruptDatabase_FileExists_RenamesFileWithCorruptSuffix()
    {
        string corruptDb = Path.Combine(_tempDir, "corrupt_test.db");
        File.WriteAllText(corruptDb, "corrupt data");

        DatabaseBackupService.RotateCorruptDatabase(corruptDb);

        Assert.False(File.Exists(corruptDb));
        var files = Directory.GetFiles(_tempDir, "noderadar.db.corrupt_*");
        Assert.Single(files);
    }

    [Fact]
    public void RotateCorruptDatabase_FileDoesNotExist_DoesNotThrow()
    {
        string nonExistentDb = Path.Combine(_tempDir, "non_existent_corrupt.db");
        var exception = Record.Exception(() => DatabaseBackupService.RotateCorruptDatabase(nonExistentDb));
        Assert.Null(exception);
    }
}
