using LiteDB;

namespace NodeRadarPro.Core;

/// <summary>
/// Persisted application settings stored in LiteDB.
/// </summary>
public class AppSettings
{
    [BsonId]
    public int Id { get; set; } = 1; // Singleton document

    // ── Monitor ──
    public int MonitorIntervalSeconds { get; set; } = 60;

    // ── Offline Device Visibility ──
    public bool EnableOfflineFadeOut { get; set; } = false;
    public int FadeOutSeconds { get; set; } = 300;

    // ── Scan Parameters ──
    public int SweepFrequencySeconds { get; set; } = 30;
    public int ResponseTimeoutMs { get; set; } = 1500;
    public bool EnableSynScan { get; set; } = false;
    public bool EnableDnsResolve { get; set; } = true;
    public bool EnableFastScan { get; set; } = false;
    public bool EnableOsDetection { get; set; } = true;
    public bool EnableInlinePortScan { get; set; } = false;

    // ── Network Interface ──
    public string SelectedInterfaceName { get; set; } = "";
    public bool EnablePromiscuous { get; set; } = false;

    // ── Alert Thresholds ──
    public int LatencyThresholdMs { get; set; } = 200;
    public double PacketLossThresholdPct { get; set; } = 5.0;

    // ── Notification Routing ──
    public bool EnableToastAlerts { get; set; } = true;
    public bool EnableSoundAlerts { get; set; } = true;
    public bool EnableEmailAlerts { get; set; } = false;
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpEmail { get; set; } = "admin@node.local";
    public string SmtpUser { get; set; } = "";
    [BsonIgnore]
    public string SmtpPassword { get; set; } = "";
    public string? SmtpPasswordEncrypted { get; set; }

    // ── Application Maintenance ──
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool EnableAutoBackup { get; set; } = true;
    public int AutoBackupIntervalHours { get; set; } = 24;

    // â”€â”€ Security â”€â”€
    public string? LastKnownGoodHash { get; set; }
}
