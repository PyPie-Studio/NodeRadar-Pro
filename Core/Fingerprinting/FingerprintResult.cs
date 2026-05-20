using System.Collections.Generic;

namespace NodeRadarPro.Core.Fingerprinting;

public enum DeviceTypeCategory
{
    Mobile,
    Desktop,
    Router,
    Switch,
    Firewall,
    AccessPoint,
    Printer,
    IoT,
    Camera,
    DVR,
    Server,
    NAS,
    TV,
    Speaker,
    GameConsole,
    Unknown
}

public class FingerprintResult
{
    public string Vendor { get; set; } = "Unknown Vendor";
    public string Os { get; set; } = "";
    public string Model { get; set; } = "";
    public DeviceTypeCategory Type { get; set; } = DeviceTypeCategory.Unknown;
    public string TypeString { get; set; } = "Generic Device";
    public string IconSvgKey { get; set; } = "pc";
    public int ConfidenceScore { get; set; } = 0;
}

public class ProbeResult
{
    public string Source { get; set; } = "";
    public Dictionary<string, string> RawData { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);

    public string? GetValue(string key)
    {
        return RawData.TryGetValue(key, out string? value) ? value : null;
    }
}
