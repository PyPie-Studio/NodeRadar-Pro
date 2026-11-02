using System;
using System.IO;
using System.Linq;
using LiteDB;

namespace NodeRadarPro.Data;

/// <summary>
/// Handles database file backup, restore, rotation, and cleanup operations.
/// Operates on the physical file layer independently of document CRUD logic.
/// </summary>
public class DatabaseBackupService
{
    private const string LogSource = "Database";
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
                    _database.Log(LogLevel.Info, LogSource, "Backup skipped: Database file not found (new install?)");
                    return "";
                }

                string destPath;
                string backupDir;

                if (customPath != null)
                {
                    destPath = Path.GetFullPath(customPath);
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

                _database.Checkpoint();
                File.Copy(sourcePath, destPath, true);
                _database.Log(LogLevel.Info, LogSource, $"Database backed up to: {destPath}");

                if (customPath == null) CleanupOldBackups(backupDir);

                return destPath;
            }
            catch (Exception ex)
            {
                _database.Log(LogLevel.Error, LogSource, $"Backup failed: {ex.Message}");
                return "";
            }
        }
    }

    public bool RestoreDatabase(string backupPath)
    {
        lock (_database.SyncRoot)
        {
            string? stagedPath = null;
            bool connectionDisposed = false;
            string dbPath = _database.DbPath;
            if (string.IsNullOrEmpty(dbPath))
            {
                dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "noderadar.db");
            }

            try
            {
                if (!File.Exists(backupPath)) return false;

                // 1. Pre-validate incoming backup without modifying live storage
                var testConn = new ConnectionString { Filename = backupPath, Password = _database.DbPassword, Connection = ConnectionType.Direct };
                using (var testDb = new LiteDatabase(testConn))
                {
                    _ = testDb.GetCollectionNames().ToList();
                }

                // 2. Close active connection before staging and overwriting
                _database.DisposeConnection();
                connectionDisposed = true;

                // 3. Stage current database for instant rollback
                stagedPath = dbPath + ".pre_restore";
                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, stagedPath, true);
                }

                // 4. Copy verified backup to active database path
                File.Copy(backupPath, dbPath, true);

                // 5. Re-initialize connection to restored database with isolated rollback guard
                try
                {
                    var connectionString = new ConnectionString { Filename = dbPath, Password = _database.DbPassword, Connection = ConnectionType.Direct };
                    var newDb = new LiteDatabase(connectionString);
                    _database.ReplaceConnection(newDb);
                }
                catch (Exception initEx)
                {
                    RollbackAndReopen(stagedPath, dbPath, connectionDisposed);
                    try { _database.Log(LogLevel.Error, LogSource, $"Database restore failed during initialization; rolled back: {initEx.Message}"); } catch { /* Ignore logging failure */ }
                    return false;
                }

                // Restore committed: staging cleanup is best-effort and must not trigger rollback
                stagedPath = null;
                try
                {
                    string cleanupPath = dbPath + ".pre_restore";
                    if (File.Exists(cleanupPath)) File.Delete(cleanupPath);
                }
                catch
                {
                    // Cleanup error must not invalidate a successful restore
                }

                _database.Log(LogLevel.Info, LogSource, $"Database restored from: {backupPath}");
                return true;
            }
            catch (LiteException ex)
            {
                RollbackAndReopen(stagedPath, dbPath, connectionDisposed);
                try { _database.Log(LogLevel.Error, LogSource, $"Database restore pre-validation failed: Database structure invalid/corrupted. {ex.Message}"); } catch { /* Ignore logging failure */ }
                return false;
            }
            catch (IOException ex)
            {
                RollbackAndReopen(stagedPath, dbPath, connectionDisposed);
                try { _database.Log(LogLevel.Error, LogSource, $"Database restore failed: File in use or I/O error. {ex.Message}"); } catch { /* Ignore logging failure */ }
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                RollbackAndReopen(stagedPath, dbPath, connectionDisposed);
                try { _database.Log(LogLevel.Error, LogSource, $"Database restore failed: Permission denied when accessing file. {ex.Message}"); } catch { /* Ignore logging failure */ }
                return false;
            }
            catch (Exception ex)
            {
                RollbackAndReopen(stagedPath, dbPath, connectionDisposed);
                try { _database.Log(LogLevel.Error, LogSource, $"Restore failed: {ex.Message}"); } catch { /* Ignore logging failure */ }
                return false;
            }
        }
    }

    private void RollbackAndReopen(string? stagedPath, string dbPath, bool connectionDisposed)
    {
        try
        {
            if (connectionDisposed && stagedPath != null && File.Exists(stagedPath))
            {
                try { File.Copy(stagedPath, dbPath, true); } catch { }
                try { File.Delete(stagedPath); } catch { }
            }
            else if (stagedPath != null && File.Exists(stagedPath))
            {
                try { File.Delete(stagedPath); } catch { }
            }

            if (connectionDisposed)
            {
                _database.ReopenDatabase();
            }
        }
        catch
        {
            // Best effort rollback
        }
    }

    public static void RotateCorruptDatabase(string dbPath)
    {
        try
        {
            string? dir = Path.GetDirectoryName(dbPath);
            string folder = string.IsNullOrEmpty(dir) ? Environment.CurrentDirectory : dir;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (File.Exists(dbPath))
            {
                try
                {
                    string corruptPath = Path.Combine(folder, $"noderadar.db.corrupt_{timestamp}");
                    File.Move(dbPath, corruptPath, true);
                }
                catch
                {
                    // Best-effort rotation for database file
                }
            }

            string logPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(dbPath) + "-log.db");
            if (File.Exists(logPath))
            {
                try
                {
                    string corruptLogPath = Path.Combine(folder, $"noderadar-log.db.corrupt_{timestamp}");
                    File.Move(logPath, corruptLogPath, true);
                }
                catch
                {
                    // Best-effort rotation for log file
                }
            }
        }
        catch
        {
            // Best-effort rotation
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
                _database.Log(LogLevel.Info, LogSource, $"Cleaned up old backup: {file.Name}");
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
