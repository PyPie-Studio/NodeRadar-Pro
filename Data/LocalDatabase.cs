using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using System.Security.Cryptography;
using NodeRadarPro.Core;

namespace NodeRadarPro.Data;

/// <summary>
/// Handles offline history, device registration, alerts, uptime snapshots,
/// and system logs using LiteDB.
/// </summary>
public class LocalDatabase : IDisposable
{
    private static Lazy<LocalDatabase> _instance = new(() => new LocalDatabase());
    public static LocalDatabase Instance => _instance.Value;

    private readonly string _dbPath;
    private LiteDatabase _db;

    private string _dbPassword;

    public LocalDatabase(string? customDbPath = null, string? customPassword = null)
    {
        if (customDbPath != null)
        {
            _dbPath = customDbPath;
            string dir = Path.GetDirectoryName(_dbPath) ?? Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _dbPassword = customPassword ?? GetOrGenerateSecurePassword(dir);
            _db = new LiteDatabase($"Filename={_dbPath};Password={_dbPassword};Connection=shared;");
        }
        else
        {
            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string myFolder = Path.Combine(myDocuments, "PyPie Studio", "NodeRadar Pro");
            Directory.CreateDirectory(myFolder);
            _dbPath = Path.Combine(myFolder, "noderadar.db");

            _dbPassword = GetOrGenerateSecurePassword(myFolder);

            // Ensure the db_key.bin is created (fallback)
            if (!File.Exists(Path.Combine(myFolder, "db_key.bin")))
            {
                SaveSecurePassword(myFolder, _dbPassword);
            }

            try
            {
                _db = new LiteDatabase($"Filename={_dbPath};Password={_dbPassword};Connection=shared;");
            }
            catch
            {
                try
                {
                    string corruptPath = Path.Combine(myFolder, $"noderadar.db.corrupt_{DateTime.Now:yyyyMMdd_HHmmss}");
                    if (File.Exists(_dbPath))
                    {
                        File.Move(_dbPath, corruptPath, true);
                    }
                    _db = new LiteDatabase($"Filename={_dbPath};Password={_dbPassword};Connection=shared;");
                }
                catch
                {
                    _db = new LiteDatabase(new MemoryStream());
                }
            }
        }
    }

    private string GetOrGenerateSecurePassword(string folder)
    {
        if (Environment.GetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING") == "true")
            return "test_password";

        string keyFile = Path.Combine(folder, "db_key.bin");
        if (File.Exists(keyFile))
        {
            try
            {
                byte[] encrypted = File.ReadAllBytes(keyFile);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                // If DPAPI decryption fails (e.g., moved to another machine), fallback to a new password
                // Note: The existing DB won't be openable, but returning a new password avoids a crash here.
                // The DB open will fail, prompting user to restore from backup or clear DB.
            }
        }

        // Generate a new secure password
        byte[] secret = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secret);
        }
        string newPassword = Convert.ToBase64String(secret);
        try
        {
            SaveSecurePassword(folder, newPassword);

            // Rename database since the old key is lost
            string dbPath = Path.Combine(folder, "noderadar.db");
            if (File.Exists(dbPath))
            {
                string corruptPath = Path.Combine(folder, $"noderadar.db.corrupt_{DateTime.Now:yyyyMMdd_HHmmss}");
                File.Move(dbPath, corruptPath, true);
            }
        }
        catch { }
        return newPassword;
    }

    private void SaveSecurePassword(string folder, string password)
    {
        if (Environment.GetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING") == "true")
            return;

        string keyFile = Path.Combine(folder, "db_key.bin");
        byte[] secret = System.Text.Encoding.UTF8.GetBytes(password);
        byte[] encrypted = ProtectedData.Protect(secret, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyFile, encrypted);
    }

    public void Checkpoint()
    {
        _db?.Checkpoint();
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    // ══════════════════════════════════
    // DEVICES
    // ══════════════════════════════════

    public (NetworkNode node, bool isNew) MergeWithHistory(NetworkNode scannedNode)
    {
        var result = MergeWithHistoryBulk(new[] { scannedNode });
        bool isNew = result.Contains(scannedNode);
        return (scannedNode, isNew);
    }

    public List<NetworkNode> MergeWithHistoryBulk(IEnumerable<NetworkNode> scannedNodes)
    {
        var collection = _db.GetCollection<NetworkNode>("devices");
        var validNodes = scannedNodes.Where(n => n != null && n.MacAddress != "Unknown").ToList();
        if (validNodes.Count == 0) return new List<NetworkNode>();

        var macs = validNodes.Select(n => n.MacAddress).Distinct().ToList();
        var existingNodesList = collection.Find(x => macs.Contains(x.MacAddress)).ToList();
        var existingNodesDict = existingNodesList.ToDictionary(x => x.MacAddress);

        var toUpdate = new List<NetworkNode>();
        var toInsert = new List<NetworkNode>();
        var newNodes = new List<NetworkNode>();

        var now = DateTime.UtcNow;

        foreach (var scannedNode in validNodes)
        {
            if (existingNodesDict.TryGetValue(scannedNode.MacAddress, out var existing))
            {
                scannedNode.CustomName = existing.CustomName;
                scannedNode.Notes = existing.Notes;
                scannedNode.Location = existing.Location;
                if (!string.IsNullOrEmpty(existing.DeviceName)) scannedNode.DeviceName = existing.DeviceName;
                if (!string.IsNullOrEmpty(existing.DeviceModel)) scannedNode.DeviceModel = existing.DeviceModel;

                if (scannedNode.IconPath == "default_device" && !string.IsNullOrEmpty(existing.IconPath))
                    scannedNode.IconPath = existing.IconPath;

                if (string.IsNullOrEmpty(scannedNode.DeviceType) && !string.IsNullOrEmpty(existing.DeviceType))
                    scannedNode.DeviceType = existing.DeviceType;
                scannedNode.IsRegistered = existing.IsRegistered;
                scannedNode.FirstSeen = existing.FirstSeen;
                scannedNode.AlertOnConnectionLost = existing.AlertOnConnectionLost;
                scannedNode.AlertOnHighLatency = existing.AlertOnHighLatency;
                scannedNode.ThreatLevel = existing.ThreatLevel;
                scannedNode.VulnerabilityScore = existing.VulnerabilityScore;
                if (string.IsNullOrEmpty(scannedNode.ExactModel)) scannedNode.ExactModel = existing.ExactModel;

                if (string.IsNullOrEmpty(scannedNode.Vendor) || scannedNode.Vendor == "Unknown Vendor")
                    scannedNode.Vendor = existing.Vendor;

                if (existing.OpenPorts?.Count > 0 && (scannedNode.OpenPorts == null || scannedNode.OpenPorts.Count == 0))
                    scannedNode.OpenPorts = existing.OpenPorts;

                if (existing.PortBanners?.Count > 0 && (scannedNode.PortBanners == null || scannedNode.PortBanners.Count == 0))
                    scannedNode.PortBanners = existing.PortBanners;
                else if (scannedNode.PortBanners != null && existing.PortBanners != null)
                {
                    foreach (var kvp in existing.PortBanners)
                    {
                        if (!scannedNode.PortBanners.ContainsKey(kvp.Key))
                            scannedNode.PortBanners[kvp.Key] = kvp.Value;
                    }
                }

                if (!string.IsNullOrEmpty(existing.OsGuess) && string.IsNullOrEmpty(scannedNode.OsGuess))
                    scannedNode.OsGuess = existing.OsGuess;

                existing.IpAddress = scannedNode.IpAddress;
                existing.IsOnline = true;
                existing.PingLatencyMs = scannedNode.PingLatencyMs;
                existing.LastSeen = now;
                existing.Hostname = scannedNode.Hostname;
                if (!string.IsNullOrEmpty(scannedNode.Vendor) && scannedNode.Vendor != "Unknown Vendor")
                    existing.Vendor = scannedNode.Vendor;
                if (scannedNode.OpenPorts?.Count > 0)
                    existing.OpenPorts = scannedNode.OpenPorts;

                if (scannedNode.PortBanners?.Count > 0)
                    existing.PortBanners = scannedNode.PortBanners;

                if (!string.IsNullOrEmpty(scannedNode.OsGuess))
                    existing.OsGuess = scannedNode.OsGuess;

                existing.ThreatLevel = scannedNode.ThreatLevel;
                existing.VulnerabilityScore = scannedNode.VulnerabilityScore;
                if (!string.IsNullOrEmpty(scannedNode.ExactModel)) existing.ExactModel = scannedNode.ExactModel;
                if (!string.IsNullOrEmpty(scannedNode.DeviceType)) existing.DeviceType = scannedNode.DeviceType;
                if (!string.IsNullOrEmpty(scannedNode.IconPath) && scannedNode.IconPath != "default_device") existing.IconPath = scannedNode.IconPath;

                toUpdate.Add(existing);
            }
            else
            {
                scannedNode.FirstSeen = now;
                scannedNode.LastSeen = now;
                toInsert.Add(scannedNode);
                newNodes.Add(scannedNode);
            }
        }

        if (toUpdate.Count > 0)
            collection.Update(toUpdate);

        if (toInsert.Count > 0)
        {
            collection.InsertBulk(toInsert);
            collection.EnsureIndex(x => x.MacAddress);
        }
        Checkpoint();

        return newNodes;
    }



    public void UpdateRegistration(string macAddress, string customName, string notes,
        string location, string deviceName, string deviceModel, string icon,
        string? ipAddress = null, int score = 0, ThreatLevel threat = ThreatLevel.Safe, string exactModel = "")
    {
        var collection = _db.GetCollection<NetworkNode>("devices");

        var existing = collection.FindOne(x => x.MacAddress == macAddress);
        if (existing != null)
        {
            existing.CustomName = customName;
            existing.Notes = notes;
            existing.Location = location;
            existing.DeviceName = deviceName;
            existing.DeviceModel = deviceModel;
            existing.IconPath = icon;
            existing.IsRegistered = true;
            existing.VulnerabilityScore = score;
            existing.ThreatLevel = threat;
            if (!string.IsNullOrEmpty(exactModel)) existing.ExactModel = exactModel;

            if (!string.IsNullOrEmpty(ipAddress))
                existing.IpAddress = ipAddress;
            collection.Update(existing);
        }
        else
        {
            var node = new NetworkNode
            {
                MacAddress = macAddress,
                CustomName = customName,
                Notes = notes,
                Location = location,
                DeviceName = deviceName,
                DeviceModel = deviceModel,
                IconPath = icon,
                IpAddress = ipAddress ?? "0.0.0.0",
                IsRegistered = true,
                VulnerabilityScore = score,
                ThreatLevel = threat,
                ExactModel = exactModel,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };
            collection.Insert(node);
            collection.EnsureIndex(x => x.MacAddress);
        }
        Checkpoint();
    }



    public void UpdateDeviceAlertPrefs(string macAddress, bool alertConnLost, bool alertHighLatency)
    {
        var collection = _db.GetCollection<NetworkNode>("devices");
        var existing = collection.FindOne(x => x.MacAddress == macAddress);
        if (existing != null)
        {
            existing.AlertOnConnectionLost = alertConnLost;
            existing.AlertOnHighLatency = alertHighLatency;
            collection.Update(existing);
        }
    }



    public bool DeleteDevice(string macAddress)
    {
        var collection = _db.GetCollection<NetworkNode>("devices");
        var existing = collection.FindOne(x => x.MacAddress == macAddress);
        if (existing != null) { collection.Delete(existing.Id); Checkpoint(); return true; }
        return false;
    }

    public int DeleteDevices(IEnumerable<string> macAddresses)
    {
        var collection = _db.GetCollection<NetworkNode>("devices");

        var macList = System.Linq.Enumerable.ToList(macAddresses);
        int deletedCount = collection.DeleteMany(x => macList.Contains(x.MacAddress));

        Log(LogLevel.Info, "Database", $"Bulk deleted {deletedCount} devices from database.");
        Checkpoint();
        return deletedCount;
    }




    public NetworkNode? GetRegisteredDeviceByIp(string ip)
    {
        try
        {
            return _db.GetCollection<NetworkNode>("devices").FindOne(x => x.IsRegistered && x.IpAddress == ip);
        }
        catch
        {
            return null;
        }
    }

    public List<NetworkNode> GetRegisteredDevices()
    {
        try
        {
            return _db.GetCollection<NetworkNode>("devices").Find(x => x.IsRegistered).ToList();
        }
        catch
        {
            return new List<NetworkNode>();
        }
    }

    // ══════════════════════════════════
    // ALERTS
    // ══════════════════════════════════

    public void InsertAlert(AlertEvent alert)
    {
        var col = _db.GetCollection<AlertEvent>("alerts");
        col.Insert(alert);
        col.EnsureIndex(x => x.Timestamp);
    }

    public List<AlertEvent> GetAlerts(int limit = 200)
    {
        return _db.GetCollection<AlertEvent>("alerts")
            .Find(Query.All("Timestamp", Query.Descending), limit: limit).ToList();
    }



    public int GetUnresolvedAlertCount()
    {
        return _db.GetCollection<AlertEvent>("alerts").Count(x => !x.IsResolved);
    }

    public void ResolveAlert(ObjectId alertId)
    {
        var col = _db.GetCollection<AlertEvent>("alerts");
        var alert = col.FindById(alertId);
        if (alert != null)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            col.Update(alert);
        }
    }

    public void ResolveAllAlerts()
    {
        var col = _db.GetCollection<AlertEvent>("alerts");
        var unresolved = col.Find(x => !x.IsResolved).ToList();
        var now = DateTime.UtcNow;
        foreach (var a in unresolved)
        {
            a.IsResolved = true;
            a.ResolvedAt = now;
        }
        if (unresolved.Count > 0) col.Update(unresolved);
    }

    // ══════════════════════════════════
    // UPTIME HISTORY
    // ══════════════════════════════════



    public void InsertUptimeSnapshots(List<UptimeSnapshot> snapshots)
    {
        if (snapshots.Count == 0) return;
        var col = _db.GetCollection<UptimeSnapshot>("uptime");
        col.InsertBulk(snapshots);
    }

    public List<UptimeSnapshot> GetUptimeHistory(string macAddress, int hours = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);
        return _db.GetCollection<UptimeSnapshot>("uptime")
            .Find(x => x.MacAddress == macAddress && x.Timestamp >= cutoff)
            .OrderBy(x => x.Timestamp).ToList();
    }

    // ══════════════════════════════════
    // SYSTEM LOGS
    // ══════════════════════════════════

    public void InsertLog(LogEntry entry)
    {
        var col = _db.GetCollection<LogEntry>("logs");
        col.Insert(entry);
        col.EnsureIndex(x => x.Timestamp);
    }

    public void Log(LogLevel level, string source, string message, string? deviceMac = null)
    {
        // 1. Database log (for UI viewer)
        InsertLog(new LogEntry
        {
            Level = level,
            Source = source,
            Message = message,
            DeviceMac = deviceMac,
            Timestamp = DateTime.UtcNow
        });

        // 2. Physical File log (for Technical Support)
        Logger.Log(level, source, message, deviceMac);
    }

    public List<LogEntry> GetLogs(int limit = 500, LogLevel? levelFilter = null, string? deviceMacFilter = null)
    {
        var col = _db.GetCollection<LogEntry>("logs");

        IEnumerable<LogEntry> query = col.Find(Query.All("Timestamp", Query.Descending), limit: limit);

        if (levelFilter.HasValue)
            query = query.Where(x => x.Level == levelFilter.Value);
        if (!string.IsNullOrEmpty(deviceMacFilter))
            query = query.Where(x => x.DeviceMac == deviceMacFilter);

        return query.ToList();
    }

    // ══════════════════════════════════
    // SETTINGS
    // ══════════════════════════════════

    public AppSettings LoadSettings()
    {
        var bsonCol = _db.GetCollection("settings");
        var doc = bsonCol.FindById(1);

        if (doc != null && doc.ContainsKey("SmtpPassword") && !doc["SmtpPassword"].IsNull && !string.IsNullOrEmpty(doc["SmtpPassword"].AsString))
        {
            var plainPass = doc["SmtpPassword"].AsString;
            try
            {
                var secret = System.Text.Encoding.UTF8.GetBytes(plainPass);
                var encrypted = System.Security.Cryptography.ProtectedData.Protect(secret, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                doc["SmtpPasswordEncrypted"] = Convert.ToBase64String(encrypted);
            }
            catch (PlatformNotSupportedException)
            {
                var secret = System.Text.Encoding.UTF8.GetBytes(plainPass);
                doc["SmtpPasswordEncrypted"] = Convert.ToBase64String(secret);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Database", $"Failed to encrypt legacy SmtpPassword: {ex.Message}");
            }

            doc.Remove("SmtpPassword");
            bsonCol.Update(doc);
        }

        var collection = _db.GetCollection<AppSettings>("settings");
        var settings = collection.FindById(1) ?? new AppSettings();

        if (!string.IsNullOrEmpty(settings.SmtpPasswordEncrypted))
        {
            try
            {
                var encrypted = Convert.FromBase64String(settings.SmtpPasswordEncrypted);
                var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(encrypted, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                settings.SmtpPassword = System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch (PlatformNotSupportedException)
            {
                var decoded = Convert.FromBase64String(settings.SmtpPasswordEncrypted);
                settings.SmtpPassword = System.Text.Encoding.UTF8.GetString(decoded);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Database", $"Failed to decrypt SmtpPassword: {ex.Message}");
                settings.SmtpPassword = "";
            }
        }

        return settings;
    }

    public void SaveSettings(AppSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.SmtpPassword))
        {
            try
            {
                var secret = System.Text.Encoding.UTF8.GetBytes(settings.SmtpPassword);
                var encrypted = System.Security.Cryptography.ProtectedData.Protect(secret, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                settings.SmtpPasswordEncrypted = Convert.ToBase64String(encrypted);
            }
            catch (PlatformNotSupportedException)
            {
                var secret = System.Text.Encoding.UTF8.GetBytes(settings.SmtpPassword);
                settings.SmtpPasswordEncrypted = Convert.ToBase64String(secret);
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Database", $"Failed to encrypt SmtpPassword during save: {ex.Message}");
            }
        }
        else
        {
            settings.SmtpPasswordEncrypted = null;
        }

        var collection = _db.GetCollection<AppSettings>("settings");
        collection.Upsert(settings);
        Checkpoint();
    }

    // ── Backup & Maintenance ──

    public string BackupDatabase(string? customPath = null)
    {
        try
        {
            if (!File.Exists(_dbPath))
            {
                Log(LogLevel.Info, "Database", "Backup skipped: Database file not found (new install?)");
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
                backupDir = Path.Combine(Path.GetDirectoryName(_dbPath)!, "Backups");
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                destPath = Path.Combine(backupDir, $"noderadar_backup_{timestamp}.db");
            }

            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            File.Copy(_dbPath, destPath, true);
            Log(LogLevel.Info, "Database", $"Database backed up to: {destPath}");

            if (customPath == null) CleanupOldBackups(backupDir);

            return destPath;
        }
        catch (IOException ex)
        {
            Log(LogLevel.Error, "Database", $"Backup failed: {ex.Message}");
            return "";
        }
        catch (UnauthorizedAccessException ex)
        {
            Log(LogLevel.Error, "Database", $"Backup failed: {ex.Message}");
            return "";
        }
    }

    public bool RestoreDatabase(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath)) return false;

            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "noderadar.db");

            // We must close the current connection before overwriting the file
            _db.Dispose();

            File.Copy(backupPath, dbPath, true);

            // Re-initialize (Note: In a real app, we would probably trigger an app restart)
            var connectionString = $"Filename={dbPath};Password={_dbPassword};Connection=shared";
            _db = new LiteDatabase(connectionString);

            Log(LogLevel.Info, "Database", $"Database restored from: {backupPath}");
            return true;
        }
        catch (IOException ex)
        {
            TryReopenDatabase();
            Log(LogLevel.Error, "Database", $"Database restore failed: File in use or I/O error. {ex.Message}");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            TryReopenDatabase();
            Log(LogLevel.Error, "Database", $"Database restore failed: Permission denied when accessing file. {ex.Message}");
            return false;
        }
        catch (LiteException ex)
        {
            TryReopenDatabase();
            Log(LogLevel.Error, "Database", $"Database restore failed: Database structure invalid/corrupted. {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            TryReopenDatabase();
            Log(LogLevel.Error, "Database", $"Restore failed: {ex.Message}");
            return false;
        }
    }

    private void TryReopenDatabase()
    {
        // Try to re-open if possible
        try
        {
            if (_db == null)
            {
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "noderadar.db");
                var connectionString = $"Filename={dbPath};Password={_dbPassword};Connection=shared";
                _db = new LiteDatabase(connectionString);
            }
        }
        catch { }
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
                Log(LogLevel.Info, "Database", $"Cleaned up old backup: {file.Name}");
            }
        }
        catch { }
    }
}
