using System;

namespace NodeRadarPro.Core.Fingerprinting;

public static class DeviceIconMapper
{
    public static string GetIconKey(DeviceTypeCategory type, string vendor, string hostname, Func<string, bool> containsData)
    {
        string v = vendor.ToLowerInvariant();
        string h = hostname.ToLowerInvariant();

        // 1. Apple Overrides
        if (v.Contains("apple"))
        {
            if (h.Contains("iphone") || containsData("iphone")) return "phone";
            if (h.Contains("ipad") || containsData("ipad")) return "tablet";
            if (h.Contains("macbook") || h.Contains("imac") || h.Contains("mac-mini")) return "pc";
            return "phone"; // Default Apple to phone icon (iOS heavily dominates)
        }

        // 2. Category Mapping
        return type switch
        {
            DeviceTypeCategory.Mobile => "phone",
            DeviceTypeCategory.Desktop => "pc",
            DeviceTypeCategory.Server => "server",
            DeviceTypeCategory.NAS => "nas",
            DeviceTypeCategory.Router => "router",
            DeviceTypeCategory.Switch => "switch",
            DeviceTypeCategory.Firewall => "firewall",
            DeviceTypeCategory.AccessPoint => "accesspoint",
            DeviceTypeCategory.Printer => "printer",
            DeviceTypeCategory.TV => "tv",
            DeviceTypeCategory.Speaker => "speaker",
            DeviceTypeCategory.Camera => "camera",
            DeviceTypeCategory.DVR => "dvr",
            DeviceTypeCategory.GameConsole => "gamepad",
            DeviceTypeCategory.IoT => "iot",
            _ => "pc" // Default fallback
        };
    }
}
