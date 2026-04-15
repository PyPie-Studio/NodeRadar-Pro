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
    /// <summary>Seconds between each background connectivity ping cycle.</summary>
    public int MonitorIntervalSeconds { get; set; } = 60;

    // ── Offline Device Visibility ──
    /// <summary>When true, offline devices fade out after FadeOutSeconds. When false, they stay visible indefinitely.</summary>
    public bool EnableOfflineFadeOut { get; set; } = false;

    /// <summary>Seconds after which an offline device is hidden from the radar (only when EnableOfflineFadeOut is true).</summary>
    public int FadeOutSeconds { get; set; } = 300; // 5 minutes default
}
