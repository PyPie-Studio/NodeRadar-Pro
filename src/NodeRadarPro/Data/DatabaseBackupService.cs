using System;
using System.IO;
using System.Linq;
using LiteDB;
using NodeRadarPro.Core;

namespace NodeRadarPro.Data;

/// <summary>
/// Handles database file backup, restore, rotation, and cleanup operations.
/// Operates on the physical file layer independently of document CRUD logic.
/// </summary>
public class DatabaseBackupService
{
    private readonly LocalDatabase _database;

    public DatabaseBackupService(LocalDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public string BackupDatabase(string? customPath = null)
    {
        lock (_database.SyncRoot)
        {
            try
            {
                string sourcePath = _database.DbPath;
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                {
                    _database.Log(LogLevel.Info, "Database", "Backup skipped: Database file not found (new install?)");
                    return "";
                }

                string destPath;
                string backupDir;

                if (customPath != null)
                {
                    destPath = customPath;
                    backupDir = Path.GetDirectoryName(destPath) ?? "";
                }
                else
                {
                    backupDir = Path.Combine(Path.GetDirectoryName(sourcePath)!, "Backups");
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    destPath = Path.Combine(backupDir, $"noderadar_backup_{timestamp}.db");
                }

                if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                File.Copy(sourcePath, destPath, true);
                _database.Log(LogLevel.Info, "Database", $"Database backed up to: {destPath}");

                if (customPath == null) CleanupOldBackups(backupDir);

                return destPath;
            }
            catch (IOException ex)
            {
                _database.Log(LogLevel.Error, "Database", $"Backup failed: {ex.Message}");
                return "";
            }
            catch (UnauthorizedAccessException ex)
            {
                _database.Log(LogLevel.Error, "Database", $"Backup failed: {ex.Message}");
                return "";
            }
        }
    }

    public bool RestoreDatabase(string backupPath)
    {
        lock (_database.SyncRoot)
        {
            try
            {
                if (!File.Exists(backupPath)) return false;

                string dbPath = _database.DbPath;
                if (string.IsNullOrEmpty(dbPath))
                {
                    dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "noderadar.db");
                }

                // Close current connection before overwriting file
                _database.DisposeConnection();

                File.Copy(backupPath, dbPath, true);

                // Re-initialize connection
                var connectionString = $"Filename={dbPath};Password={_database.DbPassword};Connection=shared";
                var newDb = new LiteDatabase(connectionString);
                _database.ReplaceConnection(newDb);

                _database.Log(LogLevel.Info, "Database", $"Database restored from: {backupPath}");
                return true;
            }
            catch (IOException ex)
            {
                _database.ReopenDatabase();
                _database.Log(LogLevel.Error, "Database", $"Database restore failed: File in use or I/O error. {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                _database.ReopenDatabase();
                _database.Log(LogLevel.Error, "Database", $"Database restore failed: Permission denied when accessing file. {ex.Message}");
                return false;
            }
            catch (LiteException ex)
            {
                _database.ReopenDatabase();
                _database.Log(LogLevel.Error, "Database", $"Database restore failed: Database structure invalid/corrupted. {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _database.ReopenDatabase();
                _database.Log(LogLevel.Error, "Database", $"Restore failed: {ex.Message}");
                return false;
            }
        }
    }

    public static void RotateCorruptDatabase(string dbPath)
    {
        if (File.Exists(dbPath))
        {
            try
            {
                string? dir = Path.GetDirectoryName(dbPath);
                string folder = string.IsNullOrEmpty(dir) ? Environment.CurrentDirectory : dir;
                string corruptPath = Path.Combine(folder, $"noderadar.db.corrupt_{DateTime.Now:yyyyMMdd_HHmmss}");
                File.Move(dbPath, corruptPath, true);
            }
            catch
            {
                // Best-effort rotation
            }
        }
    }

    private void CleanupOldBackups(string backupDir)
    {
        try
        {
            var files = Directory.GetFiles(backupDir, "noderadar_backup_*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Skip(7) // Keep last 7 backups
                .ToList();

            foreach (var file in files)
            {
                file.Delete();
                _database.Log(LogLevel.Info, "Database", $"Cleaned up old backup: {file.Name}");
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
