using LiteDB;
using System;

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

    // ── Was this device online in the previous monitor cycle? ──
    [BsonIgnore]
    public bool WasOnlinePreviously { get; set; } = false;

    // ── Number of consecutive failed checks before declaring offline ──
    [BsonIgnore]
    public int FailedCheckCount { get; set; } = 0;

    [BsonIgnore]
    public string DisplayName 
    {
        get
        {
            if (!string.IsNullOrEmpty(CustomName)) return CustomName;
            if (!string.IsNullOrEmpty(DeviceName)) return DeviceName;
            if (Hostname != "Unknown Device" && Hostname != "Manual Entry") return Hostname;
            // Show "Vendor (IP)" when we know the vendor — far more useful than just IP
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
            if (!string.IsNullOrEmpty(DeviceType)) parts = DeviceType;
            else if (!string.IsNullOrEmpty(Vendor) && Vendor != "Unknown Vendor") parts = Vendor;
            
            if (!string.IsNullOrEmpty(DeviceModel))
                parts = string.IsNullOrEmpty(parts) ? DeviceModel : $"{parts} • {DeviceModel}";

            return parts;
        }
    }

    [BsonIgnore]
    public string UptimeDisplay
    {
        get
        {
            if (!IsOnline) return "Offline";
            var sessionUptime = DateTime.UtcNow - FirstSeen;
            if (sessionUptime.TotalSeconds < 0 || sessionUptime.TotalMinutes < 1) return "Just now";
            if (sessionUptime.TotalDays >= 1) return $"{(int)sessionUptime.TotalDays}d {sessionUptime.Hours}h";
            if (sessionUptime.TotalHours >= 1) return $"{(int)sessionUptime.TotalHours}h {sessionUptime.Minutes}m";
            return $"{(int)sessionUptime.TotalMinutes}m";
        }
    }

    [BsonIgnore]
    public string StatusText => IsOnline ? $"Online • {PingLatencyMs}ms" : "Offline";
}
