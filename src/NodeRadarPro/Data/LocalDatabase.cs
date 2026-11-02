using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using LiteDB;

namespace NodeRadarPro.Data;

/// <summary>
/// Handles offline history, device registration, alerts, uptime snapshots,
/// and system logs using LiteDB.
/// </summary>
public class LocalDatabase : IDisposable
{
    private const string DevicesCollection = "devices";
    private const string AlertsCollection = "alerts";
    private const string UptimeCollection = "uptime";
    private const string LogsCollection = "logs";
    private const string SettingsCollection = "settings";
    private const string DatabaseSource = "Database";
    private const string SmtpPasswordKey = "SmtpPassword";
    private const string SmtpPasswordEncryptedKey = "SmtpPasswordEncrypted";
    private const string TimestampField = "Timestamp";
    private const string MacAddressField = "MacAddress";
    private const string IpAddressField = "IpAddress";

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

        string myFolder;
        if (customDbPath != null)
        {
            _dbPath = customDbPath;
            myFolder = Path.GetDirectoryName(_dbPath) ?? Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(myFolder) && !Directory.Exists(myFolder)) Directory.CreateDirectory(myFolder);
            _dbPassword = customPassword ?? CredentialVault.GetOrGenerateDbPassword(myFolder);
        }
        else
        {
            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            myFolder = Path.Combine(myDocuments, "PyPie Studio", "NodeRadar Pro");
            Directory.CreateDirectory(myFolder);
            _dbPath = Path.Combine(myFolder, "noderadar.db");

            _dbPassword = CredentialVault.GetOrGenerateDbPassword(myFolder);

            // Ensure the db_key.bin is created (fallback)
            if (!File.Exists(Path.Combine(myFolder, "db_key.bin")))
            {
                CredentialVault.SaveDbPassword(myFolder, _dbPassword);
            }
        }

        try
        {
            var connectionString = new ConnectionString { Filename = _dbPath, Password = _dbPassword, Connection = ConnectionType.Direct };
            _db = new LiteDatabase(connectionString);
            InitializeSchema();
        }
        catch (LiteException)
        {
            DatabaseBackupService.RotateCorruptDatabase(_dbPath);
            try
            {
                var connectionString = new ConnectionString { Filename = _dbPath, Password = _dbPassword, Connection = ConnectionType.Direct };
                _db = new LiteDatabase(connectionString);
                InitializeSchema();
            }
            catch
            {
                _db = new LiteDatabase(new MemoryStream());
                InitializeSchema();
            }
        }
        catch (IOException)
        {
            // Transient file lock (antivirus, backup process) — retry once after backoff
            System.Threading.Thread.Sleep(500);
            try
            {
                var connectionString = new ConnectionString { Filename = _dbPath, Password = _dbPassword, Connection = ConnectionType.Direct };
                _db = new LiteDatabase(connectionString);
                InitializeSchema();
            }
            catch
            {
                _db = new LiteDatabase(new MemoryStream());
                InitializeSchema();
            }
        }
        catch
        {
            _db = new LiteDatabase(new MemoryStream());
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
                var connectionString = new ConnectionString { Filename = dbPath, Password = _dbPassword, Connection = ConnectionType.Direct };
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
            var devices = _db.GetCollection<NetworkNode>(DevicesCollection);
            devices.EnsureIndex(MacAddressField, "$.MacAddress", unique: true);
            devices.EnsureIndex(IpAddressField, "$.IpAddress");
            devices.EnsureIndex("IsRegistered", "$.IsRegistered");

            // alerts
            var alerts = _db.GetCollection<AlertEvent>(AlertsCollection);
            alerts.EnsureIndex(TimestampField, "$.Timestamp");
            alerts.EnsureIndex("IsResolved", "$.IsResolved");

            // uptime
            var uptime = _db.GetCollection<UptimeSnapshot>(UptimeCollection);
            uptime.EnsureIndex(MacAddressField, "$.MacAddress");
            uptime.EnsureIndex(TimestampField, "$.Timestamp");

            // logs
            var logs = _db.GetCollection<LogEntry>(LogsCollection);
            logs.EnsureIndex(TimestampField, "$.Timestamp");
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
            _db = null!;
        }
        GC.SuppressFinalize(this);
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
            var collection = _db.GetCollection<NetworkNode>(DevicesCollection);
            var validNodes = scannedNodes.Where(n => n != null && !string.IsNullOrEmpty(n.MacAddress) && n.MacAddress != "Unknown")
                                         .DistinctBy(n => n.MacAddress)
                                         .ToList();
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
                    toUpdate.Add(MergeExistingNode(scannedNode, existing, now));
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

    private static NetworkNode MergeExistingNode(NetworkNode scannedNode, NetworkNode existing, DateTime now)
    {
        CopyUserMetadata(scannedNode, existing, now);
        InheritDeviceAttributes(scannedNode, existing);
        MergeOpenPorts(scannedNode, existing);
        MergePortBanners(scannedNode, existing);
        UpdateExistingNodeState(existing, scannedNode, now);
        SyncThreatAssessment(scannedNode, existing);

        return existing;
    }

    private static void CopyUserMetadata(NetworkNode scannedNode, NetworkNode existing, DateTime now)
    {
        scannedNode.Id = existing.Id;
        scannedNode.LastSeen = now;
        scannedNode.IsOnline = true;

        scannedNode.CustomName = existing.CustomName;
        scannedNode.Notes = existing.Notes;
        scannedNode.Location = existing.Location;
        scannedNode.IsRegistered = existing.IsRegistered;
        scannedNode.FirstSeen = existing.FirstSeen;
        scannedNode.AlertOnConnectionLost = existing.AlertOnConnectionLost;
        scannedNode.AlertOnHighLatency = existing.AlertOnHighLatency;
    }

    private static void InheritDeviceAttributes(NetworkNode scannedNode, NetworkNode existing)
    {
        if (!string.IsNullOrEmpty(existing.DeviceName)) scannedNode.DeviceName = existing.DeviceName;
        if (!string.IsNullOrEmpty(existing.DeviceModel)) scannedNode.DeviceModel = existing.DeviceModel;

        if (scannedNode.IconPath == "default_device" && !string.IsNullOrEmpty(existing.IconPath))
            scannedNode.IconPath = existing.IconPath;

        if (string.IsNullOrEmpty(scannedNode.DeviceType) && !string.IsNullOrEmpty(existing.DeviceType))
            scannedNode.DeviceType = existing.DeviceType;

        if (string.IsNullOrEmpty(scannedNode.ExactModel)) scannedNode.ExactModel = existing.ExactModel;

        if (string.IsNullOrEmpty(scannedNode.Vendor) || scannedNode.Vendor == "Unknown Vendor")
            scannedNode.Vendor = existing.Vendor;

        if (!string.IsNullOrEmpty(existing.OsGuess) && string.IsNullOrEmpty(scannedNode.OsGuess))
            scannedNode.OsGuess = existing.OsGuess;
    }

    private static void MergeOpenPorts(NetworkNode scannedNode, NetworkNode existing)
    {
        if (existing.OpenPorts?.Count > 0 && scannedNode.OpenPorts?.Count > 0)
        {
            var mergedPorts = existing.OpenPorts.Union(scannedNode.OpenPorts).Distinct().OrderBy(p => p).ToList();
            scannedNode.OpenPorts = mergedPorts;
            existing.OpenPorts = mergedPorts;
        }
        else if (existing.OpenPorts?.Count > 0)
        {
            scannedNode.OpenPorts = existing.OpenPorts;
        }
        else if (scannedNode.OpenPorts?.Count > 0)
        {
            existing.OpenPorts = scannedNode.OpenPorts;
        }
    }

    private static void UpdateExistingNodeState(NetworkNode existing, NetworkNode scannedNode, DateTime now)
    {
        existing.IpAddress = scannedNode.IpAddress;
        existing.IsOnline = true;
        existing.PingLatencyMs = scannedNode.PingLatencyMs;
        existing.LastSeen = now;
        existing.Hostname = scannedNode.Hostname;

        if (!string.IsNullOrEmpty(scannedNode.Vendor) && scannedNode.Vendor != "Unknown Vendor")
            existing.Vendor = scannedNode.Vendor;

        if (scannedNode.PortBanners?.Count > 0)
            existing.PortBanners = scannedNode.PortBanners;

        if (!string.IsNullOrEmpty(scannedNode.OsGuess))
            existing.OsGuess = scannedNode.OsGuess;

        if (!string.IsNullOrEmpty(scannedNode.ExactModel)) existing.ExactModel = scannedNode.ExactModel;
        if (!string.IsNullOrEmpty(scannedNode.DeviceType)) existing.DeviceType = scannedNode.DeviceType;
        if (!string.IsNullOrEmpty(scannedNode.IconPath) && scannedNode.IconPath != "default_device") existing.IconPath = scannedNode.IconPath;
    }

    private static void SyncThreatAssessment(NetworkNode scannedNode, NetworkNode existing)
    {
        if (existing.OpenPorts?.Count > 0)
        {
            VulnerabilityEngine.UpdateThreatLevel(existing);
            scannedNode.ThreatLevel = existing.ThreatLevel;
            scannedNode.VulnerabilityScore = existing.VulnerabilityScore;
            return;
        }

        if (scannedNode.ThreatLevel != ThreatLevel.Safe || scannedNode.VulnerabilityScore > 0)
        {
            existing.ThreatLevel = scannedNode.ThreatLevel;
            existing.VulnerabilityScore = scannedNode.VulnerabilityScore;
        }
        else
        {
            scannedNode.ThreatLevel = existing.ThreatLevel;
            scannedNode.VulnerabilityScore = existing.VulnerabilityScore;
        }
    }

    private static void MergePortBanners(NetworkNode scannedNode, NetworkNode existing)
    {
        if (existing.PortBanners?.Count > 0 && (scannedNode.PortBanners == null || scannedNode.PortBanners.Count == 0))
        {
            scannedNode.PortBanners = existing.PortBanners;
        }
        else if (scannedNode.PortBanners != null && existing.PortBanners != null)
        {
            foreach (var kvp in existing.PortBanners)
            {
                if (!scannedNode.PortBanners.ContainsKey(kvp.Key))
                    scannedNode.PortBanners[kvp.Key] = kvp.Value;
            }
        }
    }

    public void UpdateRegistration(NetworkNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        lock (SyncRoot)
        {
            var collection = _db.GetCollection<NetworkNode>(DevicesCollection);

            var existing = collection.FindOne(x => x.MacAddress == node.MacAddress);
            if (existing != null)
            {
                existing.CustomName = node.CustomName;
                existing.Notes = node.Notes;
                existing.Location = node.Location;
                existing.DeviceName = node.DeviceName;
                existing.DeviceModel = node.DeviceModel;
                existing.IconPath = node.IconPath;
                existing.IsRegistered = true;
                existing.VulnerabilityScore = node.VulnerabilityScore;
                existing.ThreatLevel = node.ThreatLevel;
                if (!string.IsNullOrEmpty(node.ExactModel)) existing.ExactModel = node.ExactModel;

                if (!string.IsNullOrEmpty(node.IpAddress))
                    existing.IpAddress = node.IpAddress;
                collection.Update(existing);
            }
            else
            {
                var newNode = new NetworkNode
                {
                    MacAddress = node.MacAddress,
                    CustomName = node.CustomName,
                    Notes = node.Notes,
                    Location = node.Location,
                    DeviceName = node.DeviceName,
                    DeviceModel = node.DeviceModel,
                    IconPath = node.IconPath,
                    IpAddress = !string.IsNullOrEmpty(node.IpAddress) ? node.IpAddress : "0.0.0.0",
                    IsRegistered = true,
                    VulnerabilityScore = node.VulnerabilityScore,
                    ThreatLevel = node.ThreatLevel,
                    ExactModel = node.ExactModel,
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };
                collection.Insert(newNode);
            }
            Checkpoint();
        }
    }

    public void UpdateDeviceAlertPrefs(string macAddress, bool alertConnLost, bool alertHighLatency)
    {
        lock (SyncRoot)
        {
            var collection = _db.GetCollection<NetworkNode>(DevicesCollection);
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
            var collection = _db.GetCollection<NetworkNode>(DevicesCollection);
            var existing = collection.FindOne(x => x.MacAddress == macAddress);
            if (existing != null) { collection.Delete(existing.Id); Checkpoint(); return true; }
            return false;
        }
    }

    public int DeleteDevices(IEnumerable<string> macAddresses)
    {
        lock (SyncRoot)
        {
            var collection = _db.GetCollection<NetworkNode>(DevicesCollection);

            var macList = System.Linq.Enumerable.ToList(macAddresses);
            int deletedCount = collection.DeleteMany(x => macList.Contains(x.MacAddress));

            Log(LogLevel.Info, DatabaseSource, $"Bulk deleted {deletedCount} devices from database.");
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
                return _db.GetCollection<NetworkNode>(DevicesCollection).FindOne(x => x.IsRegistered && x.IpAddress == ip);
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
                return _db.GetCollection<NetworkNode>(DevicesCollection).Find(x => x.IsRegistered).ToList();
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
                return _db.GetCollection<NetworkNode>(DevicesCollection).FindAll().ToList();
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
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            col.Insert(alert);
        }
    }

    public void InsertAlerts(IEnumerable<AlertEvent> alerts)
    {
        var list = alerts as IList<AlertEvent> ?? alerts.ToList();
        if (list.Count == 0) return;
        lock (SyncRoot)
        {
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            col.InsertBulk(list);
        }
    }

    public List<AlertEvent> GetAlerts(int limit = 200) => GetAlerts(null, limit);

    public List<AlertEvent> GetAlerts(bool? isResolved, int limit = 200)
    {
        lock (SyncRoot)
        {
            if (_db == null) return new List<AlertEvent>();
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            var query = col.Query();
            if (isResolved.HasValue)
            {
                query = query.Where(x => x.IsResolved == isResolved.Value);
            }
            return query.OrderByDescending(x => x.Timestamp).Limit(limit).ToList();
        }
    }

    public int GetTotalAlertCount()
    {
        lock (SyncRoot)
        {
            return _db?.GetCollection<AlertEvent>(AlertsCollection).Count() ?? 0;
        }
    }

    public int GetUnresolvedAlertCount()
    {
        lock (SyncRoot)
        {
            return _db?.GetCollection<AlertEvent>(AlertsCollection).Count(x => !x.IsResolved) ?? 0;
        }
    }

    public void ResolveAlert(ObjectId alertId)
    {
        lock (SyncRoot)
        {
            if (_db == null) return;
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            var alert = col.FindById(alertId);
            if (alert != null)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                col.Update(alert);
                Checkpoint();
            }
        }
    }

    public void ResolveAllAlerts()
    {
        lock (SyncRoot)
        {
            if (_db == null) return;
            var col = _db.GetCollection(AlertsCollection);
            var now = DateTime.UtcNow;
            col.UpdateMany(
                BsonExpression.Create("{ IsResolved: true, ResolvedAt: @0 }", now),
                BsonExpression.Create("IsResolved = false")
            );
            Checkpoint();
        }
    }

    public bool DeleteAlert(ObjectId alertId)
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            bool deleted = col.Delete(alertId);
            if (deleted)
            {
                Checkpoint();
            }
            return deleted;
        }
    }

    public int DeleteResolvedAlerts()
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            int deleted = col.DeleteMany(x => x.IsResolved);
            if (deleted > 0)
            {
                Checkpoint();
                Logger.Log(LogLevel.Info, DatabaseSource, $"Deleted {deleted} resolved alerts.");
            }
            return deleted;
        }
    }

    public int DeleteAlerts(IEnumerable<ObjectId> alertIds)
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            var idList = alertIds as IList<ObjectId> ?? alertIds.ToList();
            if (idList.Count == 0) return 0;
            int deleted = col.DeleteMany(x => idList.Contains(x.Id));
            if (deleted > 0)
            {
                Checkpoint();
                Logger.Log(LogLevel.Info, DatabaseSource, $"Deleted {deleted} alerts.");
            }
            return deleted;
        }
    }

    public int DeleteAlertsOlderThan(DateTime cutoff, bool resolvedOnly = false)
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            int deleted = resolvedOnly
                ? col.DeleteMany(x => x.IsResolved && x.Timestamp < cutoff)
                : col.DeleteMany(x => x.Timestamp < cutoff);

            if (deleted > 0)
            {
                Checkpoint();
                Logger.Log(LogLevel.Info, DatabaseSource, $"Deleted {deleted} alerts older than {cutoff:yyyy-MM-dd hh:mm:ss tt}.");
            }
            return deleted;
        }
    }

    public int ClearAllAlerts()
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<AlertEvent>(AlertsCollection);
            int count = col.DeleteAll();
            Checkpoint();
            Logger.Log(LogLevel.Info, DatabaseSource, $"Cleared all {count} alerts.");
            return count;
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
            var col = _db.GetCollection<UptimeSnapshot>(UptimeCollection);
            col.InsertBulk(snapshots);
        }
    }

    public List<UptimeSnapshot> GetUptimeHistory(string macAddress, int hours = 24, int maxPoints = 120)
    {
        if (hours <= 0 || string.IsNullOrEmpty(macAddress)) return new List<UptimeSnapshot>();
        lock (SyncRoot)
        {
            if (_db == null) return new List<UptimeSnapshot>();
            var now = DateTime.UtcNow;
            var cutoff = now.AddHours(-hours);
            var raw = _db.GetCollection<UptimeSnapshot>(UptimeCollection)
                .Find(x => x.MacAddress == macAddress && x.Timestamp >= cutoff)
                .OrderBy(x => x.Timestamp).ToList();

            if (maxPoints <= 0 || raw.Count <= maxPoints)
                return raw;

            return DownsampleSnapshots(raw, macAddress, cutoff, now, maxPoints);
        }
    }

    private static List<UptimeSnapshot> DownsampleSnapshots(
        List<UptimeSnapshot> rawSnapshots,
        string macAddress,
        DateTime cutoff,
        DateTime now,
        int maxPoints)
    {
        if (rawSnapshots.Count <= maxPoints || maxPoints <= 0)
            return rawSnapshots;

        (cutoff, now) = AlignTimeKinds(cutoff, now, rawSnapshots[0].Timestamp.Kind);

        double totalSeconds = (now - cutoff).TotalSeconds;
        if (totalSeconds <= 0)
            return rawSnapshots;

        double intervalSeconds = totalSeconds / maxPoints;
        var downsampled = new List<UptimeSnapshot>(maxPoints);

        int rawIndex = 0;

        for (int i = 0; i < maxPoints; i++)
        {
            var bucketStart = cutoff.AddSeconds(i * intervalSeconds);
            var bucketEnd = GetBucketEnd(cutoff, intervalSeconds, i, maxPoints);
            var context = new BucketContext(bucketStart, bucketEnd, intervalSeconds, macAddress);

            if (TryCreateBucketSnapshot(rawSnapshots, ref rawIndex, in context, out var snapshot))
            {
                downsampled.Add(snapshot);
            }
        }

        return downsampled;
    }

    private static (DateTime cutoff, DateTime now) AlignTimeKinds(DateTime cutoff, DateTime now, DateTimeKind targetKind)
    {
        if (targetKind == DateTimeKind.Local)
            return (cutoff.ToLocalTime(), now.ToLocalTime());

        if (targetKind == DateTimeKind.Utc)
            return (cutoff.ToUniversalTime(), now.ToUniversalTime());

        return (cutoff, now);
    }

    private static DateTime GetBucketEnd(DateTime cutoff, double intervalSeconds, int index, int maxPoints)
    {
        return index == maxPoints - 1
            ? DateTime.MaxValue
            : cutoff.AddSeconds((index + 1) * intervalSeconds);
    }

    private static bool TryCreateBucketSnapshot(
        List<UptimeSnapshot> rawSnapshots,
        ref int rawIndex,
        in BucketContext context,
        [NotNullWhen(true)] out UptimeSnapshot? snapshot)
    {
        var accumulator = new BucketAccumulator();

        while (rawIndex < rawSnapshots.Count && rawSnapshots[rawIndex].Timestamp < context.End)
        {
            accumulator.Accumulate(rawSnapshots[rawIndex], context.Start);
            rawIndex++;
        }

        if (accumulator.TotalCount == 0)
        {
            snapshot = null;
            return false;
        }

        bool isOnline = accumulator.OnlineCount * 2 >= accumulator.TotalCount;
        long avgLatency = accumulator.ComputeAverageLatency();

        snapshot = new UptimeSnapshot
        {
            MacAddress = context.MacAddress,
            IsOnline = isOnline,
            LatencyMs = avgLatency,
            Timestamp = context.Start.AddSeconds(context.IntervalSeconds / 2)
        };
        return true;
    }

    private readonly struct BucketContext(DateTime start, DateTime end, double intervalSeconds, string macAddress)
    {
        public DateTime Start { get; } = start;
        public DateTime End { get; } = end;
        public double IntervalSeconds { get; } = intervalSeconds;
        public string MacAddress { get; } = macAddress;
    }

    private struct BucketAccumulator
    {
        public int TotalCount;
        public int OnlineCount;
        public int LatencyCount;
        public long LatencySum;

        public void Accumulate(UptimeSnapshot item, DateTime bucketStart)
        {
            if (item.Timestamp < bucketStart)
                return;

            TotalCount++;
            if (!item.IsOnline)
                return;

            OnlineCount++;
            if (item.LatencyMs >= 0)
            {
                LatencySum += item.LatencyMs;
                LatencyCount++;
            }
        }

        public readonly long ComputeAverageLatency()
        {
            if (LatencyCount > 0)
                return (long)Math.Round((double)LatencySum / LatencyCount);

            return (OnlineCount * 2 >= TotalCount) ? 0 : -1;
        }
    }

    // ══════════════════════════════════
    // SYSTEM LOGS
    // ══════════════════════════════════

    public void InsertLog(LogEntry entry)
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<LogEntry>(LogsCollection);
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
            var col = _db.GetCollection<LogEntry>(LogsCollection);
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

    public bool DeleteLog(ObjectId logId)
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<LogEntry>(LogsCollection);
            bool deleted = col.Delete(logId);
            if (deleted)
            {
                Checkpoint();
            }
            return deleted;
        }
    }

    public int DeleteLogsOlderThan(DateTime cutoff)
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<LogEntry>(LogsCollection);
            int deleted = col.DeleteMany(x => x.Timestamp < cutoff);
            if (deleted > 0)
            {
                Checkpoint();
                Logger.Log(LogLevel.Info, DatabaseSource, $"Deleted {deleted} logs older than {cutoff:yyyy-MM-dd hh:mm:ss tt}.");
            }
            return deleted;
        }
    }

    public int ClearLogs()
    {
        lock (SyncRoot)
        {
            var col = _db.GetCollection<LogEntry>(LogsCollection);
            int count = col.DeleteAll();
            Checkpoint();
            Logger.Log(LogLevel.Info, DatabaseSource, $"Cleared all {count} system logs.");
            return count;
        }
    }

    // ══════════════════════════════════
    // SETTINGS
    // ══════════════════════════════════

    public AppSettings LoadSettings()
    {
        lock (SyncRoot)
        {
            var bsonCol = _db.GetCollection(SettingsCollection);
            var doc = bsonCol.FindById(1);

            if (doc != null && doc.ContainsKey(SmtpPasswordKey) && !doc[SmtpPasswordKey].IsNull && !string.IsNullOrEmpty(doc[SmtpPasswordKey].AsString))
            {
                var plainPass = doc[SmtpPasswordKey].AsString;
                try
                {
                    doc[SmtpPasswordEncryptedKey] = CredentialVault.EncryptSecret(plainPass);
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, DatabaseSource, $"Failed to encrypt legacy SmtpPassword: {ex.Message}");
                }

                doc.Remove(SmtpPasswordKey);
                bsonCol.Update(doc);
            }

            var collection = _db.GetCollection<AppSettings>(SettingsCollection);
            var settings = collection.FindById(1) ?? new AppSettings();

            if (!string.IsNullOrEmpty(settings.SmtpPasswordEncrypted))
            {
                try
                {
                    settings.SmtpPassword = CredentialVault.DecryptSecret(settings.SmtpPasswordEncrypted);
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, DatabaseSource, $"Failed to decrypt SmtpPassword: {ex.Message}");
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
                    Log(LogLevel.Error, DatabaseSource, $"Failed to encrypt SmtpPassword during save: {ex.Message}");
                }
            }
            else
            {
                settings.SmtpPasswordEncrypted = null;
            }

            var collection = _db.GetCollection<AppSettings>(SettingsCollection);
            collection.Upsert(settings);
            Checkpoint();
        }
    }

    // ── Maintenance & Pruning ──

    /// <summary>
    /// Prunes old uptime snapshots, resolved alerts, and log entries to prevent unbounded DB growth.
    /// Called from the auto-backup timer or manually from maintenance routines.
    /// </summary>
    public int PruneOldData(int? uptimeRetentionDays = null, int? logRetentionDays = null, int? resolvedAlertRetentionDays = null)
    {
        lock (SyncRoot)
        {
            AppSettings? settings = null;
            int uptimeDays = uptimeRetentionDays ?? (settings ??= LoadSettings()).UptimeRetentionDays;
            int logDays = logRetentionDays ?? (settings ??= LoadSettings()).LogRetentionDays;
            int alertDays = resolvedAlertRetentionDays ?? (settings ??= LoadSettings()).ResolvedAlertRetentionDays;

            int deleted = 0;
            var now = DateTime.UtcNow;

            // Prune uptime snapshots
            var uptimeCutoff = now.AddDays(-uptimeDays);
            deleted += _db.GetCollection<UptimeSnapshot>(UptimeCollection)
                .DeleteMany(x => x.Timestamp < uptimeCutoff);

            // Prune old logs
            var logCutoff = now.AddDays(-logDays);
            deleted += _db.GetCollection<LogEntry>(LogsCollection)
                .DeleteMany(x => x.Timestamp < logCutoff);

            // Prune resolved alerts older than retention
            var alertCutoff = now.AddDays(-alertDays);
            deleted += _db.GetCollection<AlertEvent>(AlertsCollection)
                .DeleteMany(x => x.IsResolved && x.ResolvedAt != null && x.ResolvedAt < alertCutoff);

            if (deleted > 0)
            {
                Checkpoint();
                if (deleted >= 100)
                {
                    try
                    {
                        _db.Rebuild();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Warning, DatabaseSource, $"Storage compaction skipped: {ex.Message}");
                    }
                }
                Logger.Log(LogLevel.Info, DatabaseSource, $"Pruned {deleted} expired records and compacted storage.");
            }

            return deleted;
        }
    }
}
