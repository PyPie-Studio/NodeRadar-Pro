using LiteDB;
using System;
using System.Collections.Generic;

namespace NodeRadarPro.Core;

/// <summary>
/// Represents a discovered node/device on the network.
/// </summary>
public class NetworkNode
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    // ── Network Identity ──
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = "Unknown";
    public string Hostname { get; set; } = "Unknown Device";
    public long PingLatencyMs { get; set; } = -1;
    public bool IsOnline { get; set; } = false;

    // ── User Registration Fields ──
    public string CustomName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public bool IsRegistered { get; set; } = false;

    // ── Vendor & Icon ──
    public string Vendor { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string IconPath { get; set; } = "default_device";

    // ── Uptime & Connectivity Tracking ──
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    // ── Port Scanning Results ──
    public List<int> OpenPorts { get; set; } = new();
    public Dictionary<int, string> PortBanners { get; set; } = new();
    public string OsGuess { get; set; } = "";

    // ── Per-Device Alert Preferences ──
    public bool AlertOnConnectionLost { get; set; } = true;
    public bool AlertOnHighLatency { get; set; } = false;

    // ── Deep Intelligence & Vulnerability ──
    public int VulnerabilityScore { get; set; } = 0;
    public ThreatLevel ThreatLevel { get; set; } = ThreatLevel.Safe;
    public string ExactModel { get; set; } = string.Empty;

    // ── Runtime-only fields (not persisted) ──

    [BsonIgnore]
    public bool WasOnlinePreviously { get; set; } = false;

    [BsonIgnore]
    public int FailedCheckCount { get; set; } = 0;

    /// <summary>Ring buffer of last 100 ping results (true=success, false=failure) for packet loss calculation.</summary>
    [BsonIgnore]
    public Queue<bool> PingHistory { get; set; } = new();

    /// <summary>Calculated packet loss percentage from PingHistory.</summary>
    [BsonIgnore]
    public double PacketLossPct
    {
        get
        {
            if (PingHistory.Count == 0) return 0;
            int failed = 0;
            foreach (var p in PingHistory) if (!p) failed++;
            return (double)failed / PingHistory.Count * 100.0;
        }
    }

    /// <summary>Records a ping result into the ring buffer (max 100 entries).</summary>
    public void RecordPing(bool success)
    {
        PingHistory.Enqueue(success);
        while (PingHistory.Count > 100) PingHistory.Dequeue();
    }

    [BsonIgnore]
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(CustomName)) return CustomName;
            if (!string.IsNullOrEmpty(DeviceName)) return DeviceName;
            if (Hostname != "Unknown Device" && Hostname != "Manual Entry") return Hostname;
            if (!string.IsNullOrEmpty(DeviceModel)) return DeviceModel;
            if (!string.IsNullOrEmpty(ExactModel)) return ExactModel;
            if (!string.IsNullOrEmpty(Vendor) && Vendor != "Unknown Vendor")
                return $"{Vendor} ({IpAddress})";
            return IpAddress;
        }
    }

    /// <summary>
    /// A short subtitle line for lists: shows device type + vendor info.
    /// </summary>
    [BsonIgnore]
    public string SubtitleText
    {
        get
        {
            string parts = "";

            // Prioritize Detected OS for accuracy
            if (!string.IsNullOrEmpty(OsGuess)) parts = OsGuess;
            else if (!string.IsNullOrEmpty(DeviceType)) parts = DeviceType;
            else if (!string.IsNullOrEmpty(Vendor) && Vendor != "Unknown Vendor") parts = Vendor;

            if (!string.IsNullOrEmpty(ExactModel) && ExactModel != DisplayName)
                parts = string.IsNullOrEmpty(parts) ? ExactModel : $"{parts} • {ExactModel}";
            else if (!string.IsNullOrEmpty(DeviceModel))
                parts = string.IsNullOrEmpty(parts) ? DeviceModel : $"{parts} • {DeviceModel}";

            return parts;
        }
    }

}

public enum ThreatLevel
{
    Safe,
    Warning,
    Critical
}
