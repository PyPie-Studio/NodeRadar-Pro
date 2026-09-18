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
    private static Lazy<LocalDatabase> _instance = new(() => new LocalDatabase());
    public static LocalDatabase Instance => _instance.Value;

    private readonly string _dbPath;
    private LiteDatabase _db;
    private string _dbPassword;
    private readonly object _syncRoot = new();
    private readonly DatabaseBackupService _backupService;

    public DatabaseBackupService Backup => _backupService ?? new DatabaseBackupService(this);

    internal string DbPath => _dbPath;
    internal string DbPassword => _dbPassword;
    internal object SyncRoot => _syncRoot ?? this;

    internal LocalDatabase(LiteDatabase db)
    {
        _backupService = new DatabaseBackupService(this);
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _dbPath = ":memory:";
        _dbPassword = string.Empty;
        InitializeSchema();
    }

    public LocalDatabase(string? customDbPath = null, string? customPassword = null)
    {
        _backupService = new DatabaseBackupService(this);

        if (customDbPath != null)
        {
            _dbPath = customDbPath;
            string dir = Path.GetDirectoryName(_dbPath) ?? Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _dbPassword = customPassword ?? CredentialVault.GetOrGenerateDbPassword(dir);
            _db = new LiteDatabase($"Filename={_dbPath};Password={_dbPassword};Connection=shared;");
            InitializeSchema();
        }
        else
        {
            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string myFolder = Path.Combine(myDocuments, "PyPie Studio", "NodeRadar Pro");
            Directory.CreateDirectory(myFolder);
            _dbPath = Path.Combine(myFolder, "noderadar.db");

            _dbPassword = CredentialVault.GetOrGenerateDbPassword(myFolder);

            // Ensure the db_key.bin is created (fallback)
            if (!File.Exists(Path.Combine(myFolder, "db_key.bin")))
            {
                CredentialVault.SaveDbPassword(myFolder, _dbPassword);
            }

            try
            {
                _db = new LiteDatabase($"Filename={_dbPath};Password={_dbPassword};Connection=shared;");
            }
            catch (LiteException)
            {
                DatabaseBackupService.RotateCorruptDatabase(_dbPath);
                try
                {
                    _db = new LiteDatabase($"Filename={_dbPath};Password={_dbPassword};Connection=shared;");
                }
                catch
                {
                    _db = new LiteDatabase(new MemoryStream());
                }
            }
            catch (IOException)
            {
                // Transient file lock (antivirus, backup process) — retry once after backoff
                System.Threading.Thread.Sleep(500);
                try
                {
                    _db = new LiteDatabase($"Filename={_dbPath};Password={_dbPassword};Connection=shared;");
                }
                catch
                {
                    _db = new LiteDatabase(new MemoryStream());
                }
            }
            catch
            {
                _db = new LiteDatabase(new MemoryStream());
            }

            InitializeSchema();
        }
    }

    internal void DisposeConnection()
    {
        lock (SyncRoot)
        {
            _db?.Dispose();
            _db = null!;
        }
    }

    internal void ReplaceConnection(LiteDatabase newDb)
    {
        lock (SyncRoot)
        {
            _db = newDb;
            InitializeSchema();
        }
    }

    internal void ReopenDatabase()
    {
        lock (SyncRoot)
        {
            try
            {
                string dbPath = _dbPath;
                if (string.IsNullOrEmpty(dbPath))
                {
                    dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", "noderadar.db");
                }
                var connectionString = $"Filename={dbPath};Password={_dbPassword};Connection=shared";
                _db = new LiteDatabase(connectionString);
                InitializeSchema();
            }
            catch
            {
                try { _db = new LiteDatabase(new MemoryStream()); } catch { /* Ignore */ }
            }
        }
    }

    private void InitializeSchema()
    {
        lock (SyncRoot)
        {
            if (_db == null) return;

            // devices
            var devices = _db.GetCollection<NetworkNode>("devices");
            devices.EnsureIndex("MacAddress", "$.MacAddress", unique: true);
            devices.EnsureIndex("IpAddress", "$.IpAddress");

            // alerts
            var alerts = _db.GetCollection<AlertEvent>("alerts");
            alerts.EnsureIndex("Timestamp", "$.Timestamp");
            alerts.EnsureIndex("IsResolved", "$.IsResolved");

            // uptime
            var uptime = _db.GetCollection<UptimeSnapshot>("uptime");
            uptime.EnsureIndex("MacAddress", "$.MacAddress");
            uptime.EnsureIndex("Timestamp", "$.Timestamp");
            uptime.EnsureIndex("Mac_Time", "$.MacAddress + '_' + $.Timestamp");

            // logs
            var logs = _db.GetCollection<LogEntry>("logs");
            logs.EnsureIndex("Timestamp", "$.Timestamp");
            logs.EnsureIndex("DeviceMac", "$.DeviceMac");
            logs.EnsureIndex("Level", "$.Level");
        }
    }

    public void Checkpoint()
    {
        lock (SyncRoot)
        {
            _db?.Checkpoint();
        }
    }

    public void Dispose()
    {
        lock (SyncRoot)
        {
            _db?.Dispose();
        }
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
        lock (SyncRoot)
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
            }

            return newNodes;
        }
    }

    public void UpdateRegistration(string macAddress, string customName, string notes,
        string location, string deviceName, string deviceModel, string icon,
        string? ipAddress = null, int score = 0, ThreatLevel threat = ThreatLevel.Safe, string exactModel = "")
    {
        lock (SyncRoot)
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
            }
            Checkpoint();
        }
    }

    public void UpdateDeviceAlertPrefs(string macAddress, bool alertConnLost, bool alertHighLatency)
    {
        lock (SyncRoot)
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
    }

    public bool DeleteDevice(string macAddress)
    {
        lock (SyncRoot)
        {
            var collection = _db.GetCollection<NetworkNode>("devices");
            var existing = collection.FindOne(x => x.MacAddress == macAddress);
            if (existing != null) { collection.Delete(existing.Id); Checkpoint(); return true; }
            return false;
        }
    }

    public int DeleteDevices(IEnumerable<string> macAddresses)
    {
        lock (SyncRoot)
        {
            var collection = _db.GetCollection<NetworkNode>("devices");

            var macList = System.Linq.Enumerable.ToList(macAddresses);
            int deletedCount = collection.DeleteMany(x => macList.Contains(x.MacAddress));

            Log(LogLevel.Info, "Database", $"Bulk deleted {deletedCount} devices from database.");
            Checkpoint();
            return deletedCount;
        }
    }

    public NetworkNode? GetRegisteredDeviceByIp(string ip)
    {
        lock (SyncRoot)
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
    }

    public List<NetworkNode> GetRegisteredDevices()
    {
        lock (SyncRoot)
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
    }

    public List<NetworkNode> GetAllDevices()
    {
        lock (SyncRoot)
        {
            try
            {
                return _db.GetCollection<NetworkNode>("devices").FindAll().ToList();
            }
            catch
            {
                return new List<NetworkNode>();
            }
        }
    }

    // ══════════════════════════════════
    // ALERTS
    // ══════════════════════════════════

    public void InsertAlert(AlertEvent alert)
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<AlertEvent>("alerts");
            col.Insert(alert);
        }
    }

    public void InsertAlerts(IEnumerable<AlertEvent> alerts)
    {
        var list = alerts as IList<AlertEvent> ?? alerts.ToList();
        if (list.Count == 0) return;
        lock (SyncRoot)
        {
            var col = _db.GetCollection<AlertEvent>("alerts");
            col.InsertBulk(list);
        }
    }

    public List<AlertEvent> GetAlerts(int limit = 200)
    {
        lock (SyncRoot)
        {
            return _db.GetCollection<AlertEvent>("alerts")
                .Find(Query.All("Timestamp", Query.Descending), limit: limit).ToList();
        }
    }

    public int GetUnresolvedAlertCount()
    {
        lock (SyncRoot)
        {
            return _db.GetCollection<AlertEvent>("alerts").Count(x => !x.IsResolved);
        }
    }

    public void ResolveAlert(ObjectId alertId)
    {
        lock (SyncRoot)
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
    }

    public void ResolveAllAlerts()
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection("alerts");
            var now = DateTime.UtcNow;
            col.UpdateMany(
                BsonExpression.Create("{ IsResolved: true, ResolvedAt: @0 }", now),
                BsonExpression.Create("IsResolved = false")
            );
        }
    }

    // ══════════════════════════════════
    // UPTIME HISTORY
    // ══════════════════════════════════

    public void InsertUptimeSnapshots(List<UptimeSnapshot> snapshots)
    {
        if (snapshots.Count == 0) return;
        lock (SyncRoot)
        {
            var col = _db.GetCollection<UptimeSnapshot>("uptime");
            col.InsertBulk(snapshots);
        }
    }

    public List<UptimeSnapshot> GetUptimeHistory(string macAddress, int hours = 24)
    {
        lock (SyncRoot)
        {
            var cutoff = DateTime.UtcNow.AddHours(-hours);
            return _db.GetCollection<UptimeSnapshot>("uptime")
                .Find(x => x.MacAddress == macAddress && x.Timestamp >= cutoff)
                .OrderBy(x => x.Timestamp).ToList();
        }
    }

    // ══════════════════════════════════
    // SYSTEM LOGS
    // ══════════════════════════════════

    public void InsertLog(LogEntry entry)
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<LogEntry>("logs");
            col.Insert(entry);
        }
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
        lock (SyncRoot)
        {
            var col = _db.GetCollection<LogEntry>("logs");
            var query = col.Query();

            if (levelFilter.HasValue)
                query = query.Where(x => x.Level == levelFilter.Value);
            if (!string.IsNullOrEmpty(deviceMacFilter))
                query = query.Where(x => x.DeviceMac == deviceMacFilter);

            return query.OrderByDescending(x => x.Timestamp)
                .Limit(limit)
                .ToList();
        }
    }

    // ══════════════════════════════════
    // SETTINGS
    // ══════════════════════════════════

    public AppSettings LoadSettings()
    {
        lock (SyncRoot)
        {
            var bsonCol = _db.GetCollection("settings");
            var doc = bsonCol.FindById(1);

            if (doc != null && doc.ContainsKey("SmtpPassword") && !doc["SmtpPassword"].IsNull && !string.IsNullOrEmpty(doc["SmtpPassword"].AsString))
            {
                var plainPass = doc["SmtpPassword"].AsString;
                try
                {
                    doc["SmtpPasswordEncrypted"] = CredentialVault.EncryptSecret(plainPass);
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
                    settings.SmtpPassword = CredentialVault.DecryptSecret(settings.SmtpPasswordEncrypted);
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, "Database", $"Failed to decrypt SmtpPassword: {ex.Message}");
                    settings.SmtpPassword = "";
                }
            }

            return settings;
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        lock (SyncRoot)
        {
            if (!string.IsNullOrEmpty(settings.SmtpPassword))
            {
                try
                {
                    settings.SmtpPasswordEncrypted = CredentialVault.EncryptSecret(settings.SmtpPassword);
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
    }

    // ── Maintenance & Pruning ──

    /// <summary>
    /// Prunes old uptime snapshots, resolved alerts, and log entries to prevent unbounded DB growth.
    /// Called from the auto-backup timer or manually from maintenance routines.
    /// </summary>
    public int PruneOldData(int uptimeRetentionDays = 30, int logRetentionDays = 14, int resolvedAlertRetentionDays = 30)
    {
        lock (SyncRoot)
        {
            int deleted = 0;
            var now = DateTime.UtcNow;

            // Prune uptime snapshots
            var uptimeCutoff = now.AddDays(-uptimeRetentionDays);
            deleted += _db.GetCollection<UptimeSnapshot>("uptime")
                .DeleteMany(x => x.Timestamp < uptimeCutoff);

            // Prune old logs
            var logCutoff = now.AddDays(-logRetentionDays);
            deleted += _db.GetCollection<LogEntry>("logs")
                .DeleteMany(x => x.Timestamp < logCutoff);

            // Prune resolved alerts older than retention
            var alertCutoff = now.AddDays(-resolvedAlertRetentionDays);
            deleted += _db.GetCollection<AlertEvent>("alerts")
                .DeleteMany(x => x.IsResolved && x.ResolvedAt != null && x.ResolvedAt < alertCutoff);

            if (deleted > 0)
            {
                Checkpoint();
                Log(LogLevel.Info, "Database", $"Pruned {deleted} expired records (uptime>{uptimeRetentionDays}d, logs>{logRetentionDays}d, resolved alerts>{resolvedAlertRetentionDays}d).");
            }

            return deleted;
        }
    }
}
