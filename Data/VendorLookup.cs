using System;
using System.Collections.Generic;

namespace NodeRadarPro.Data;

/// <summary>
/// Maps MAC address prefixes (OUI) to hardware manufacturers.
/// Covers ~200 of the most common vendors seen on real networks.
/// </summary>
public static class VendorLookup
{
    private static readonly Dictionary<string, string> _vendors = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Apple ──
        { "00:03:93", "Apple" },  { "00:0A:95", "Apple" },  { "00:1B:63", "Apple" },
        { "04:69:F8", "Apple" },  { "28:CF:E9", "Apple" },  { "3C:15:C2", "Apple" },
        { "40:33:1A", "Apple" },  { "60:FA:CD", "Apple" },  { "7C:D1:C3", "Apple" },
        { "88:66:A5", "Apple" },  { "90:8D:6C", "Apple" },  { "A8:20:66", "Apple" },
        { "A8:88:08", "Apple" },  { "AC:BC:32", "Apple" },  { "B0:34:95", "Apple" },
        { "B8:53:AC", "Apple" },  { "B8:E8:56", "Apple" },  { "C0:A5:3E", "Apple" },
        { "C8:69:CD", "Apple" },  { "CC:08:E0", "Apple" },  { "D0:81:7A", "Apple" },
        { "D4:61:9D", "Apple" },  { "DC:A9:04", "Apple" },  { "E0:C7:67", "Apple" },
        { "F0:D1:A9", "Apple" },  { "F4:5C:89", "Apple" },  { "F8:1E:DF", "Apple" },

        // ── Samsung ──
        { "00:16:6B", "Samsung" }, { "00:16:DB", "Samsung" }, { "00:24:90", "Samsung" },
        { "08:FC:88", "Samsung" }, { "10:D5:42", "Samsung" }, { "14:49:E0", "Samsung" },
        { "1C:62:B8", "Samsung" }, { "24:18:1D", "Samsung" }, { "28:98:7B", "Samsung" },
        { "2C:AE:2B", "Samsung" }, { "30:CD:A7", "Samsung" }, { "34:23:BA", "Samsung" },
        { "38:01:46", "Samsung" }, { "3C:A1:0D", "Samsung" }, { "40:4E:36", "Samsung" },
        { "4C:BC:98", "Samsung" }, { "50:01:BB", "Samsung" }, { "54:92:BE", "Samsung" },
        { "58:C3:8B", "Samsung" }, { "6C:2F:2C", "Samsung" }, { "78:D6:F0", "Samsung" },
        { "78:BD:BC", "Samsung" }, { "84:38:38", "Samsung" }, { "8C:77:12", "Samsung" },
        { "9C:3A:AF", "Samsung" }, { "A0:CC:2B", "Samsung" }, { "A8:06:00", "Samsung" },
        { "AC:5F:3E", "Samsung" }, { "B4:07:F9", "Samsung" }, { "BC:14:EF", "Samsung" },
        { "C4:73:1E", "Samsung" }, { "CC:07:AB", "Samsung" }, { "D0:22:BE", "Samsung" },
        { "D0:66:7B", "Samsung" }, { "D8:90:E8", "Samsung" }, { "E4:7C:F9", "Samsung" },
        { "EC:1F:72", "Samsung" }, { "F0:25:B7", "Samsung" }, { "F4:7B:5E", "Samsung" },

        // ── Xiaomi ──
        { "04:CF:8C", "Xiaomi" }, { "0C:1D:AF", "Xiaomi" }, { "10:2A:B3", "Xiaomi" },
        { "14:F6:5A", "Xiaomi" }, { "18:59:36", "Xiaomi" }, { "20:47:DA", "Xiaomi" },
        { "28:6C:07", "Xiaomi" }, { "2C:28:B7", "Xiaomi" }, { "34:CE:00", "Xiaomi" },
        { "38:A4:ED", "Xiaomi" }, { "3C:BD:3E", "Xiaomi" }, { "44:23:7C", "Xiaomi" },
        { "50:64:2B", "Xiaomi" }, { "58:44:98", "Xiaomi" }, { "64:B4:73", "Xiaomi" },
        { "7C:1D:D9", "Xiaomi" }, { "8C:5A:C1", "Xiaomi" }, { "98:FA:E3", "Xiaomi" },

        // ── Huawei / Honor ──
        { "00:18:82", "Huawei" }, { "00:1E:10", "Huawei" }, { "00:46:4B", "Huawei" },
        { "04:F9:38", "Huawei" }, { "0C:37:DC", "Huawei" }, { "10:47:80", "Huawei" },
        { "14:B9:68", "Huawei" }, { "20:A6:80", "Huawei" }, { "24:09:95", "Huawei" },
        { "28:31:52", "Huawei" }, { "30:D1:7E", "Huawei" }, { "34:CD:B0", "Huawei" },
        { "38:4C:4F", "Huawei" }, { "40:4D:8E", "Huawei" }, { "48:46:FB", "Huawei" },
        { "4C:B1:6C", "Huawei" }, { "54:A5:1B", "Huawei" }, { "58:60:5F", "Huawei" },
        { "5C:7D:5E", "Huawei" }, { "60:DE:44", "Huawei" }, { "70:7B:E8", "Huawei" },
        { "78:F5:FD", "Huawei" }, { "80:41:26", "Huawei" }, { "88:28:B3", "Huawei" },
        { "9C:28:EF", "Huawei" }, { "A4:99:47", "Huawei" }, { "AC:CF:85", "Huawei" },
        { "B0:E1:A5", "Huawei" }, { "C0:70:09", "Huawei" }, { "CC:A2:23", "Huawei" },
        { "D0:7A:B5", "Huawei" }, { "E4:68:A3", "Huawei" }, { "F4:63:49", "Huawei" },

        // ── MikroTik ──
        { "00:0C:42", "MikroTik" }, { "2C:C8:1B", "MikroTik" }, { "4C:5E:0C", "MikroTik" },
        { "6C:3B:6B", "MikroTik" }, { "74:4D:28", "MikroTik" }, { "B8:69:F4", "MikroTik" },
        { "C4:AD:34", "MikroTik" }, { "CC:2D:E0", "MikroTik" }, { "D4:01:C3", "MikroTik" },
        { "DC:2C:6E", "MikroTik" }, { "E4:8D:8C", "MikroTik" }, { "48:A9:8A", "MikroTik" },
        { "08:55:31", "MikroTik" }, { "18:FD:74", "MikroTik" },

        // ── TP-Link ──
        { "00:31:92", "TP-Link" }, { "10:FE:ED", "TP-Link" }, { "14:CC:20", "TP-Link" },
        { "18:D6:C7", "TP-Link" }, { "1C:3B:F3", "TP-Link" }, { "30:B5:C2", "TP-Link" },
        { "50:C7:BF", "TP-Link" }, { "54:C8:0F", "TP-Link" }, { "5C:A6:E6", "TP-Link" },
        { "60:32:B1", "TP-Link" }, { "64:70:02", "TP-Link" }, { "6C:5A:B0", "TP-Link" },
        { "78:8C:B5", "TP-Link" }, { "90:F6:52", "TP-Link" }, { "98:DA:C4", "TP-Link" },
        { "A0:F3:C1", "TP-Link" }, { "AC:84:C6", "TP-Link" }, { "B0:4E:26", "TP-Link" },
        { "C0:06:C3", "TP-Link" }, { "C0:E3:FB", "TP-Link" }, { "D8:07:B6", "TP-Link" },
        { "E8:DE:27", "TP-Link" }, { "EC:08:6B", "TP-Link" }, { "F4:F2:6D", "TP-Link" },

        // ── Intel ──
        { "00:11:11", "Intel" },  { "00:23:14", "Intel" },  { "3C:97:0E", "Intel" },
        { "44:03:2C", "Intel" },  { "5C:87:9C", "Intel" },  { "68:05:CA", "Intel" },
        { "80:86:F2", "Intel" },  { "8C:8D:28", "Intel" },  { "A4:BF:01", "Intel" },
        { "B4:B6:76", "Intel" },  { "B4:96:91", "Intel" },  { "C8:D3:FF", "Intel" },
        { "D0:94:66", "Intel" },  { "F8:63:3F", "Intel" },  { "48:A4:72", "Intel" },

        // ── Cisco ──
        { "00:00:0C", "Cisco" },  { "00:25:9C", "Cisco" },  { "00:1E:7A", "Cisco" },
        { "00:22:BD", "Cisco" },  { "1C:E6:C7", "Cisco" },  { "34:62:88", "Cisco" },
        { "58:97:1E", "Cisco" },  { "6C:41:6A", "Cisco" },  { "A4:56:30", "Cisco" },
        { "BC:67:1C", "Cisco" },  { "CC:46:D6", "Cisco" },  { "F0:29:29", "Cisco" },

        // ── Dell ──
        { "00:14:22", "Dell" },   { "14:FE:B5", "Dell" },   { "18:A9:05", "Dell" },
        { "24:B6:FD", "Dell" },   { "34:17:EB", "Dell" },   { "54:9F:35", "Dell" },
        { "74:86:7A", "Dell" },   { "90:B1:1C", "Dell" },   { "B0:83:FE", "Dell" },
        { "D4:81:D7", "Dell" },   { "F0:1F:AF", "Dell" },   { "F8:BC:12", "Dell" },

        // ── HP ──
        { "00:1A:4B", "HP" },     { "00:23:7D", "HP" },     { "10:1F:74", "HP" },
        { "14:58:D0", "HP" },     { "28:92:4A", "HP" },     { "30:8D:99", "HP" },
        { "3C:D9:2B", "HP" },     { "48:0F:CF", "HP" },     { "5C:B9:01", "HP" },
        { "6C:C2:17", "HP" },     { "8C:DC:D4", "HP" },     { "94:57:A5", "HP" },
        { "A0:D3:C1", "HP" },     { "B0:5A:DA", "HP" },     { "E4:11:5B", "HP" },

        // ── Lenovo ──
        { "00:1E:4F", "Lenovo" }, { "28:D2:44", "Lenovo" },
        { "40:B0:34", "Lenovo" }, { "54:EE:75", "Lenovo" }, { "68:F7:28", "Lenovo" },
        { "8C:16:45", "Lenovo" }, { "98:E7:F4", "Lenovo" }, { "C8:5B:76", "Lenovo" },

        // ── ASUS ──
        { "00:1E:8C", "ASUS" },   { "04:D4:C4", "ASUS" },   { "10:C3:7B", "ASUS" },
        { "2C:56:DC", "ASUS" },   { "30:5A:3A", "ASUS" },   { "40:B0:76", "ASUS" },
        { "50:46:5D", "ASUS" },   { "70:8B:CD", "ASUS" },   { "88:D7:F6", "ASUS" },
        { "D8:50:E6", "ASUS" },   { "F0:79:59", "ASUS" },   { "AC:9E:17", "ASUS" },

        // ── Netgear ──
        { "00:14:6C", "Netgear" }, { "00:24:B2", "Netgear" }, { "20:0C:C8", "Netgear" },
        { "2C:B0:5D", "Netgear" }, { "44:94:FC", "Netgear" }, { "6C:B0:CE", "Netgear" },
        { "84:1B:5E", "Netgear" }, { "9C:3D:CF", "Netgear" }, { "A4:2B:8C", "Netgear" },
        { "B0:7F:B9", "Netgear" }, { "C0:3F:0E", "Netgear" }, { "E0:46:9A", "Netgear" },

        // ── D-Link ──
        { "00:1C:F0", "D-Link" }, { "14:D6:4D", "D-Link" }, { "1C:7E:E5", "D-Link" },
        { "28:10:7B", "D-Link" }, { "34:08:04", "D-Link" }, { "60:63:4C", "D-Link" },
        { "78:54:2E", "D-Link" }, { "90:94:E4", "D-Link" }, { "B8:A3:86", "D-Link" },
        { "C8:D3:A3", "D-Link" }, { "F0:7D:68", "D-Link" }, { "FC:75:16", "D-Link" },

        // ── Ubiquiti ──
        { "00:15:6D", "Ubiquiti" }, { "04:18:D6", "Ubiquiti" }, { "18:E8:29", "Ubiquiti" },
        { "24:5A:4C", "Ubiquiti" }, { "44:D9:E7", "Ubiquiti" }, { "68:72:51", "Ubiquiti" },
        { "78:8A:20", "Ubiquiti" }, { "80:2A:A8", "Ubiquiti" }, { "B4:FB:E4", "Ubiquiti" },
        { "DC:9F:DB", "Ubiquiti" }, { "F0:9F:C2", "Ubiquiti" }, { "FC:EC:DA", "Ubiquiti" },

        // ── Google / Nest ──
        { "00:1A:11", "Google" },  { "08:9E:08", "Google" },  { "3C:F7:A4", "Google" },
        { "3C:5A:B4", "Google" },  { "54:60:09", "Google" },  { "94:EB:2C", "Google" },
        { "A4:77:33", "Google" },  { "F4:F5:D8", "Google" },  { "F4:F5:E8", "Google" },

        // ── Amazon / Ring / Echo ──
        { "10:CE:A9", "Amazon" },  { "18:74:2E", "Amazon" },  { "34:D2:70", "Amazon" },
        { "40:B4:CD", "Amazon" },  { "44:65:0D", "Amazon" },  { "50:DC:E7", "Amazon" },
        { "68:54:FD", "Amazon" },  { "74:C2:46", "Amazon" },  { "84:D6:D0", "Amazon" },
        { "A0:02:DC", "Amazon" },  { "AC:63:BE", "Amazon" },  { "FC:65:DE", "Amazon" },

        // ── Microsoft / Xbox ──
        { "AC:87:A3", "Microsoft" }, { "CC:52:AF", "Microsoft" }, { "28:18:78", "Microsoft" },
        { "60:45:BD", "Microsoft" }, { "7C:1E:52", "Microsoft" }, { "DC:B4:C4", "Microsoft" },

        // ── Sony PlayStation ──
        { "00:D9:D1", "Sony" },    { "28:3F:69", "Sony" },    { "5C:A3:9D", "Sony" },
        { "70:9E:29", "Sony" },    { "A8:E3:EE", "Sony" },    { "E0:D4:64", "Sony" },

        // ── Nintendo ──
        { "00:17:AB", "Nintendo" }, { "00:22:D7", "Nintendo" }, { "00:24:F3", "Nintendo" },
        { "34:AF:2C", "Nintendo" }, { "40:F4:07", "Nintendo" }, { "58:2F:40", "Nintendo" },
        { "7C:BB:8A", "Nintendo" }, { "98:B6:E9", "Nintendo" },

        // ── VMware ──
        { "00:05:69", "VMware" },  { "00:0C:29", "VMware" },  { "00:50:56", "VMware" },

        // ── Realtek / Broadcom ──
        { "00:10:18", "Broadcom" }, { "00:E0:4C", "Realtek" }, { "48:5B:39", "Realtek" },
        { "52:54:00", "Realtek" },  { "80:1F:12", "Realtek" }, { "E8:4E:06", "Realtek" },

        // ── GIGABYTE / ASRock / MSI ──
        { "74:D4:35", "GIGABYTE" },{ "BC:5F:F4", "ASRock" },  { "00:D8:61", "MSI" },
        { "4C:E1:73", "MSI" },     { "04:7C:16", "MSI" },

        // ── Synology / QNAP ──
        { "00:11:32", "Synology" }, { "00:08:9B", "QNAP" },

        // ── Aruba / Juniper / Fortinet ──
        { "00:0B:86", "Aruba" },   { "00:1B:C5", "Aruba" },   { "24:DE:C6", "Aruba" },
        { "38:94:ED", "Aruba" },   { "6C:F3:7F", "Aruba" },
        { "00:23:9C", "Juniper" }, { "3C:61:04", "Juniper" }, { "4C:96:14", "Juniper" },
        { "00:09:0F", "Fortinet" },{ "70:4C:A5", "Fortinet" },

        // ── OnePlus / Oppo / Vivo ──
        { "94:65:2D", "OnePlus" }, { "C0:EE:FB", "OnePlus" },
        { "00:1E:AC", "Oppo" },    { "A4:3B:FA", "Oppo" },
        { "D0:53:49", "Liteon/Laptop WiFi" },

        // ── LG ──
        { "00:1E:75", "LG" },     { "00:22:A9", "LG" },     { "10:68:3F", "LG" },
        { "34:4D:F7", "LG" },     { "58:A2:B5", "LG" },     { "88:07:4B", "LG" },
        { "A8:23:FE", "LG" },     { "CC:FA:00", "LG" },     { "F8:0C:F3", "LG" },

        // ── Motorola / Google Pixel ──
        { "00:04:56", "Motorola" },{ "00:0C:E5", "Motorola" },{ "14:A5:1A", "Motorola" },
        { "34:BB:26", "Motorola" },{ "7C:46:85", "Motorola" },{ "D4:7A:E2", "Motorola" },

        // ── Hikvision / Dahua (Cameras) ──
        { "28:57:BE", "Hikvision" },{ "44:19:B6", "Hikvision" },{ "54:C4:15", "Hikvision" },
        { "C0:56:27", "Hikvision" },{ "F4:15:63", "Hikvision" },
        { "3C:EF:8C", "Dahua" },   { "A0:BD:1D", "Dahua" },

        // ── Sonos ──
        { "00:0E:58", "Sonos" },   { "34:7E:5C", "Sonos" },   { "5C:AA:FD", "Sonos" },
        { "78:28:CA", "Sonos" },   { "94:9F:3E", "Sonos" },   { "B8:E9:37", "Sonos" },

        // ── Roku / Smart TV ──
        { "00:0D:4B", "Roku" },    { "20:EF:BD", "Roku" },    { "B0:A7:37", "Roku" },
        { "CC:6D:A0", "Roku" },    { "D0:4D:C6", "Roku" },    { "D8:31:34", "Roku" },

        // ── Polycom ──
        { "64:16:7F", "Polycom" },

        // ── Withings ──
        { "00:24:E4", "Withings" },

        // ── Nokia ──
        { "4C:EB:42", "Nokia" }
    };

    public static string GetVendor(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress) || macAddress.Length < 8)
            return "Unknown Vendor";

        string prefix = macAddress[..8].Replace("-", ":").ToUpper();
        
        return _vendors.TryGetValue(prefix, out string? vendor) ? vendor : "Unknown Vendor";
    }

    /// <summary>
    /// Attempts to guess a device type from its vendor name and open ports.
    /// </summary>
    public static string GuessDeviceType(string vendor, string hostname)
    {
        string v = vendor.ToLower();
        string h = hostname.ToLower();

        if (v.Contains("mikrotik") || v.Contains("cisco") || v.Contains("juniper") || 
            v.Contains("fortinet") || v.Contains("aruba") || v.Contains("ubiquiti") ||
            h.Contains("router") || h.Contains("gateway"))
            return "Router/Network";

        if (v.Contains("apple"))
        {
            if (h.Contains("iphone")) return "iPhone";
            if (h.Contains("ipad")) return "iPad";
            if (h.Contains("watch")) return "Apple Watch";
            if (h.Contains("macbook") || h.Contains("mac-mini") || h.Contains("imac")) return "Mac";
            return "Mobile (Apple)";
        }

        if (v.Contains("samsung") || v.Contains("xiaomi") || v.Contains("huawei") || 
            v.Contains("oneplus") || v.Contains("oppo") || v.Contains("vivo") || 
            v.Contains("motorola") || v.Contains("google") || v.Contains("hmd") || v.Contains("nokia"))
        {
            if (h.Contains("tv")) return "Smart TV";
            return "Mobile Phone";
        }

        if (v.Contains("sony") && !v.Contains("mobile"))
            return "PlayStation";
        if (v.Contains("nintendo"))
            return "Nintendo Console";
        if (v.Contains("microsoft"))
            return h.Contains("xbox") ? "Xbox" : "PC / Windows";
        
        if (v.Contains("amazon"))
            return h.Contains("fire") ? "Fire TV" : "Echo / Alexa";
        
        if (v.Contains("synology") || v.Contains("qnap") || v.Contains("wd") || v.Contains("terramaster"))
            return "NAS Storage";
            
        if (v.Contains("sonos") || v.Contains("bose") || v.Contains("yamaha") || v.Contains("denon"))
            return "Audio Speaker";
            
        if (v.Contains("roku") || v.Contains("lg") || v.Contains("vizio") || v.Contains("panasonic"))
            return "Smart TV";
            
        if (v.Contains("hikvision") || v.Contains("dahua") || v.Contains("reolink") || v.Contains("axis") || v.Contains("bosch"))
            return "IP Camera";

        if (v.Contains("vmware") || v.Contains("virtual") || v.Contains("oracle"))
            return "Virtual Machine";
            
        if (v.Contains("dell") || v.Contains("hp") || v.Contains("lenovo") || v.Contains("asus") || v.Contains("acer") || v.Contains("msi") || v.Contains("gigabyte"))
            return "Workstation";

        if (v.Contains("intel") || v.Contains("realtek") || v.Contains("broadcom"))
            return "Computer";

        return "Network Device";
    }
}