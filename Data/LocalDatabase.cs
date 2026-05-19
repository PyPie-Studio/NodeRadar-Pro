using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using NodeRadarPro.Core;

namespace NodeRadarPro.Data;

/// <summary>
/// Handles offline history, device registration, alerts, uptime snapshots,
/// and system logs using LiteDB.
/// </summary>
public class LocalDatabase : IDisposable
{
    private static readonly Lazy<LocalDatabase> _instance = new(() => new LocalDatabase());
    public static LocalDatabase Instance => _instance.Value;

    private readonly string _dbPath;
    private LiteDatabase _db;

    private LocalDatabase()
    {
        string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string myFolder = Path.Combine(myDocuments, "PyPie Studio", "NodeRadar Pro");
        Directory.CreateDirectory(myFolder);
        _dbPath = Path.Combine(myFolder, "noderadar.db");
        _db = new LiteDatabase($"Filename={_dbPath};Password=PyPie-NR-Pro-Sec-2026;Connection=shared;");
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
        if (scannedNode.MacAddress == "Unknown") return (scannedNode, false);

        var collection = _db.GetCollection<NetworkNode>("devices");

        var existing = collection.FindOne(x => x.MacAddress == scannedNode.MacAddress);
        bool isNew = false;

        if (existing != null)
        {
            scannedNode.CustomName = existing.CustomName;
            scannedNode.Notes = existing.Notes;
            scannedNode.Location = existing.Location;
            scannedNode.DeviceName = existing.DeviceName;
            scannedNode.DeviceModel = existing.DeviceModel;
            scannedNode.IconPath = existing.IconPath;
            scannedNode.IsRegistered = existing.IsRegistered;
            scannedNode.FirstSeen = existing.FirstSeen;
            scannedNode.AlertOnConnectionLost = existing.AlertOnConnectionLost;
            scannedNode.AlertOnHighLatency = existing.AlertOnHighLatency;
            scannedNode.ThreatLevel = existing.ThreatLevel;
            scannedNode.VulnerabilityScore = existing.VulnerabilityScore;
            if (string.IsNullOrEmpty(scannedNode.ExactModel)) scannedNode.ExactModel = existing.ExactModel;

            if (string.IsNullOrEmpty(scannedNode.Vendor) || scannedNode.Vendor == "Unknown Vendor")
                scannedNode.Vendor = existing.Vendor;

            // Merge open ports (keep existing + add new)
            if (existing.OpenPorts?.Count > 0 && scannedNode.OpenPorts.Count == 0)
                scannedNode.OpenPorts = existing.OpenPorts;
            
            // Merge port banners
            if (existing.PortBanners?.Count > 0 && (scannedNode.PortBanners == null || scannedNode.PortBanners.Count == 0))
                scannedNode.PortBanners = existing.PortBanners;
            else if (scannedNode.PortBanners != null && existing.PortBanners != null)
            {
                // Combine them, keeping existing if new doesn't have it, but new overrides if both have it
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
            existing.LastSeen = DateTime.UtcNow;
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

            collection.Update(existing);
        }
        else
        {
            isNew = true;
            scannedNode.FirstSeen = DateTime.UtcNow;
            scannedNode.LastSeen = DateTime.UtcNow;
            collection.Insert(scannedNode);
            collection.EnsureIndex(x => x.MacAddress);
        }

        return (scannedNode, isNew);
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
        if (existing != null) { collection.Delete(existing.Id); return true; }
        return false;
    }

    public int DeleteDevices(IEnumerable<string> macAddresses)
    {
        var collection = _db.GetCollection<NetworkNode>("devices");

        var macArray = macAddresses.ToArray();
        int deletedCount = collection.DeleteMany(x => macArray.Contains(x.MacAddress));

        Log(LogLevel.Info, "Database", $"Bulk deleted {deletedCount} devices from database.");
        return deletedCount;
    }



    public List<NetworkNode> GetRegisteredDevices()
    {
        return _db.GetCollection<NetworkNode>("devices").Find(x => x.IsRegistered).ToList();
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
        var collection = _db.GetCollection<AppSettings>("settings");
        return collection.FindById(1) ?? new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        var collection = _db.GetCollection<AppSettings>("settings");
        collection.Upsert(settings);
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
        catch (Exception ex)
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
            var connectionString = $"Filename={dbPath};Password=PyPie-NR-Pro-Sec-2026;Connection=shared";
            _db = new LiteDatabase(connectionString);
            
            Log(LogLevel.Info, "Database", $"Database restored from: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            // Try to re-open if possible
            try { if (_db == null) _db = new LiteDatabase(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "noderadar.db")); } catch { }
            Log(LogLevel.Error, "Database", $"Restore failed: {ex.Message}");
            return false;
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
                Log(LogLevel.Info, "Database", $"Cleaned up old backup: {file.Name}");
            }
        }
        catch { }
    }
}