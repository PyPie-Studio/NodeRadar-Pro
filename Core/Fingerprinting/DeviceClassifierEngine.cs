using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NodeRadarPro.Core.Fingerprinting;

public static class DeviceClassifierEngine
{
    public static FingerprintResult Classify(NetworkNode node, List<ProbeResult> probeResults)
    {
        var result = new FingerprintResult();

        int iosScore = 0;
        int winScore = 0;
        int linuxScore = 0;
        int iotScore = 0;
        int printerScore = 0;
        int networkScore = 0;

        bool ContainsData(string term)
        {
            foreach (var pr in probeResults)
            {
                foreach (var val in pr.RawData.Values)
                {
                    if (val.Contains(term, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }
        string hostname = node.Hostname?.ToLowerInvariant() ?? "";

        string vendor = probeResults.FirstOrDefault(pr => pr.Source == "MAC OUI Lookup")?.GetValue("Vendor")?.ToLowerInvariant() ?? "";
        result.Vendor = string.IsNullOrEmpty(vendor) ? "Unknown Vendor" : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(vendor);

        string combinedData = string.Join(" ", probeResults.SelectMany(pr => pr.RawData.Values)).ToLowerInvariant();

        // ═══════════════════════════════════════
        // 0. DHCP Fingerprint Scoring (Highest confidence for mobile devices)
        // ═══════════════════════════════════════
        var dhcpProbe = probeResults.FirstOrDefault(pr => pr.Source == "DHCP Fingerprint");
        string dhcpOs = dhcpProbe?.GetValue("DhcpOsFingerprint")?.ToLowerInvariant() ?? "";
        string dhcpHostname = dhcpProbe?.GetValue("DhcpHostname") ?? "";
        string dhcpVendorClass = dhcpProbe?.GetValue("DhcpVendorClass")?.ToLowerInvariant() ?? "";

        if (!string.IsNullOrEmpty(dhcpOs))
        {
            if (dhcpOs.Contains("ios") || dhcpOs.Contains("apple"))
            {
                iosScore += 80;
            }
            else if (dhcpOs.Contains("android"))
            {
                linuxScore += 60;
                iotScore += 40;
            }
            else if (dhcpOs.Contains("windows"))
            {
                winScore += 70;
            }
            else if (dhcpOs.Contains("linux"))
            {
                linuxScore += 50;
            }
            else if (dhcpOs.Contains("cisco") || dhcpOs.Contains("voip"))
            {
                networkScore += 60;
            }
        }

        // DHCP Vendor Class provides strong OS hints
        if (!string.IsNullOrEmpty(dhcpVendorClass))
        {
            if (dhcpVendorClass.Contains("msft")) winScore += 40;
            if (dhcpVendorClass.Contains("android")) { linuxScore += 50; iotScore += 30; }
            if (dhcpVendorClass.Contains("apple") || dhcpVendorClass.Contains("dhcpc")) iosScore += 30;
        }

        // DHCP Hostname pattern matching — very reliable for mobile identification
        string dhcpHostLower = dhcpHostname.ToLowerInvariant();
        if (!string.IsNullOrEmpty(dhcpHostLower))
        {
            // Apple patterns
            if (dhcpHostLower.Contains("iphone") || dhcpHostLower.Contains("ipad") ||
                dhcpHostLower.Contains("macbook") || dhcpHostLower.Contains("imac") ||
                dhcpHostLower.Contains("apple"))
            {
                iosScore += 60;
            }
            // Android phone patterns
            else if (dhcpHostLower.Contains("galaxy") || dhcpHostLower.Contains("pixel") ||
                     dhcpHostLower.Contains("redmi") || dhcpHostLower.Contains("poco") ||
                     dhcpHostLower.Contains("oneplus") || dhcpHostLower.Contains("oppo") ||
                     dhcpHostLower.Contains("vivo") || dhcpHostLower.Contains("realme") ||
                     dhcpHostLower.Contains("motorola") || dhcpHostLower.Contains("moto") ||
                     dhcpHostLower.Contains("nokia") || dhcpHostLower.Contains("huawei") ||
                     dhcpHostLower.Contains("honor") || dhcpHostLower.Contains("xiaomi") ||
                     dhcpHostLower.Contains("samsung") || dhcpHostLower.Contains("sm-") ||
                     dhcpHostLower.Contains("note") || dhcpHostLower.Contains("a5") ||
                     dhcpHostLower.Contains("android"))
            {
                linuxScore += 50;
                iotScore += 30;
            }
            // Windows patterns
            else if (dhcpHostLower.Contains("desktop-") || dhcpHostLower.Contains("laptop-") ||
                     dhcpHostLower.Contains("windows"))
            {
                winScore += 30;
            }
        }

        // ═══════════════════════════════════════
        // 1. MAC & Vendor Scoring
        if (vendor.Contains("apple")) iosScore += 30;

        if (vendor.Contains("microsoft") || vendor.Contains("intel") || vendor.Contains("dell") || vendor.Contains("hp") ||
            vendor.Contains("lenovo") || vendor.Contains("asus") || vendor.Contains("acer") || vendor.Contains("gigabyte") || vendor.Contains("msi"))
            winScore += 10;

        if (vendor.Contains("raspberry") || vendor.Contains("synology") || vendor.Contains("qnap")) linuxScore += 40;

        if (vendor.Contains("xiaomi") || vendor.Contains("tuya") || vendor.Contains("espressif")) iotScore += 30;

        // Mobile vendors
        if (vendor.Contains("samsung") || vendor.Contains("google") || vendor.Contains("oneplus") || vendor.Contains("oppo") ||
            vendor.Contains("vivo") || vendor.Contains("realme") || vendor.Contains("motorola") || vendor.Contains("lg") ||
            vendor.Contains("htc") || vendor.Contains("nokia") || vendor.Contains("sony mobile"))
        {
            linuxScore += 40;
            iotScore += 20;
        }

        // Routers, gateways, ONUs
        if (vendor.Contains("cisco") || vendor.Contains("tp-link") || vendor.Contains("ubiquiti") || vendor.Contains("mikrotik") ||
            vendor.Contains("huawei") || vendor.Contains("sagemcom") || vendor.Contains("fiberhome") || vendor.Contains("netgear") ||
            vendor.Contains("d-link") || vendor.Contains("linksys") || vendor.Contains("technicolor") || vendor.Contains("zte") ||
            vendor.Contains("zyxel") || vendor.Contains("tenda") || vendor.Contains("mercusys") || vendor.Contains("totolink") ||
            vendor.Contains("netis") || vendor.Contains("router") || vendor.Contains("gateway"))
        {
            networkScore += 30;
        }

        // Privacy MAC strongly hints mobile (assign equally to iOS and Linux/Android to avoid bias)
        if (vendor.Contains("randomized") || vendor.Contains("privacy"))
        {
            iosScore += 20;
            linuxScore += 20;
            iotScore += 10;
        }

        // ═══════════════════════════════════════
        // 2. Banner & Protocol Data Scoring
        if (ContainsData("openssh")) linuxScore += 40;
        if (ContainsData("microsoft-httpapi") || ContainsData("iis")) winScore += 50;
        if (ContainsData("mikrotik") || ContainsData("routeros")) networkScore += 100;
        if (ContainsData("nginx") || ContainsData("apache")) linuxScore += 30;
        if (ContainsData("printer") || ContainsData("ipp") || ContainsData("canon") || ContainsData("epson") || ContainsData("hp jetdirect")) printerScore += 80;
        if (ContainsData("apple-icon") || ContainsData("apple device") || ContainsData("homekit")) iosScore += 60;
        if (ContainsData("cast") || ContainsData("chromecast") || ContainsData("spotify") || ContainsData("speaker") || ContainsData("tv") || ContainsData("webos")) iotScore += 60;
        if (ContainsData("android")) { iotScore += 40; linuxScore += 20; }

        // 3. Port Fingerprinting
        if (node.OpenPorts != null)
        {
            foreach (int port in node.OpenPorts)
            {
                switch (port)
                {
                    case 62078: iosScore += 80; break;
                    case 548: iosScore += 40; break;
                    case 135: case 445: case 3389: winScore += 50; break;
                    case 22: linuxScore += 30; break;
                    case 23: networkScore += 30; break;
                    case 9100: case 631: printerScore += 80; break;
                    case 8008: case 8009: iotScore += 40; break;
                }
            }
        }

        // 4. Hostname Keywords
        if (hostname.Contains("iphone") || hostname.Contains("ipad") || hostname.Contains("apple-")) iosScore += 50;
        if (hostname.Contains("windows") || hostname.Contains("desktop-") || hostname.Contains("laptop-")) winScore += 30;
        if (hostname.Contains("android") || hostname.Contains("galaxy")) iotScore += 30;

        // ═══════════════════════════════════════
        // 5. "Silent Device" Heuristic
        // ═══════════════════════════════════════
        // Devices with Privacy MAC + no open ports + no banners are almost certainly mobile phones
        bool isPrivacyMac = vendor.Contains("privacy") || vendor.Contains("randomized");
        bool hasNoPorts = node.OpenPorts == null || node.OpenPorts.Count == 0;
        bool hasNoBanners = node.PortBanners == null || node.PortBanners.Count == 0;

        string cleanedData = combinedData.Replace("privacy mac", "").Replace("randomized mac", "").Trim();
        bool hasNoProtocolData = string.IsNullOrWhiteSpace(cleanedData);

        if (isPrivacyMac && hasNoPorts && hasNoBanners)
        {
            if (hasNoProtocolData)
            {
                // Completely silent Privacy MAC device. We know it's a mobile phone/device, but we don't know the OS (iOS vs Android).
                result.Type = DeviceTypeCategory.Mobile;
                result.TypeString = "Mobile Device";
                result.Os = "Android / iOS";
                result.IconSvgKey = "mobile";
                result.ConfidenceScore = 40;
                return result;
            }
            else
            {
                // If we do have some protocol data, adjust scores but make them equal if no specific hints
                iosScore += 20;
                linuxScore += 20;
                iotScore += 10;
            }
        }

        // ═══════════════════════════════════════
        // Extract specific models from probes
        var mdnsProbe = probeResults.FirstOrDefault(pr => pr.Source == "mDNS / Bonjour");
        var ssdpProbe = probeResults.FirstOrDefault(pr => pr.Source == "SSDP / UPnP");
        var snmpProbe = probeResults.FirstOrDefault(pr => pr.Source == "SNMP (sysDescr)");

        List<string> modelParts = new();
        if (mdnsProbe?.GetValue("Model") is string mdnsModel && !string.IsNullOrEmpty(mdnsModel)) modelParts.Add(mdnsModel);
        if (ssdpProbe?.GetValue("ModelName") is string ssdpModel && !string.IsNullOrEmpty(ssdpModel)) modelParts.Add(ssdpModel);
        if (ssdpProbe?.GetValue("FriendlyName") is string friendlyName && !string.IsNullOrEmpty(friendlyName)) modelParts.Add(friendlyName);
        if (snmpProbe?.GetValue("sysDescr") is string sysDescr && !string.IsNullOrEmpty(sysDescr)) modelParts.Add(sysDescr);

        result.Model = modelParts.Count > 0 ? string.Join(" | ", modelParts.Distinct()) : "";

        // Determine Winner
        var scores = new Dictionary<string, int>
        {
            { "macOS/iOS", iosScore },
            { "Windows", winScore },
            { "Linux/Android", linuxScore },
            { "IoT", iotScore },
            { "Printer", printerScore },
            { "Infrastructure", networkScore }
        };

        var winner = scores.OrderByDescending(x => x.Value).First();
        result.ConfidenceScore = winner.Value;

        if (winner.Value <= 0)
        {
            if (node.OpenPorts != null && (node.OpenPorts.Contains(80) || node.OpenPorts.Contains(443)))
            {
                result.Type = DeviceTypeCategory.Unknown;
                result.TypeString = "Network Device";
                result.Os = "";
            }
            else
            {
                result.Type = DeviceTypeCategory.Unknown;
                result.TypeString = "Generic Device";
                result.Os = "";
            }
        }
        else
        {
            result.Os = winner.Key;

            if (winner.Key == "macOS/iOS")
            {
                if (combinedData.Contains("iphone") || combinedData.Contains("ipad") ||
                    vendor.Contains("randomized") || vendor.Contains("privacy") ||
                    dhcpHostLower.Contains("iphone") || dhcpHostLower.Contains("ipad"))
                {
                    result.Type = DeviceTypeCategory.Mobile;
                    result.TypeString = "Mobile Device";
                }
                else
                {
                    result.Type = DeviceTypeCategory.Desktop;
                    result.TypeString = "Apple Computer";
                }
            }
            else if (winner.Key == "Windows")
            {
                result.Type = DeviceTypeCategory.Desktop;
                result.TypeString = "Windows PC";
            }
            else if (winner.Key == "Printer")
            {
                result.Type = DeviceTypeCategory.Printer;
                result.TypeString = "Network Printer";
            }
            else if (winner.Key == "Infrastructure")
            {
                if (ContainsData("switch")) { result.Type = DeviceTypeCategory.Switch; result.TypeString = "Network Switch"; }
                else if (ContainsData("firewall")) { result.Type = DeviceTypeCategory.Firewall; result.TypeString = "Firewall"; }
                else if (ContainsData("unifi") || ContainsData("ap")) { result.Type = DeviceTypeCategory.AccessPoint; result.TypeString = "Access Point"; }
                else { result.Type = DeviceTypeCategory.Router; result.TypeString = "Router / Gateway"; }
            }
            else if (winner.Key == "IoT")
            {
                if (ContainsData("tv") || ContainsData("tizen") || ContainsData("webos")) { result.Type = DeviceTypeCategory.TV; result.TypeString = "Smart TV"; }
                else if (ContainsData("speaker") || ContainsData("sonos")) { result.Type = DeviceTypeCategory.Speaker; result.TypeString = "Smart Speaker"; }
                else if (ContainsData("camera") || ContainsData("hikvision") || ContainsData("dahua")) { result.Type = DeviceTypeCategory.Camera; result.TypeString = "IP Camera"; }
                else if (ContainsData("android") || (vendor.Contains("xiaomi") && !ContainsData("light"))) { result.Type = DeviceTypeCategory.Mobile; result.TypeString = "Mobile Phone"; }
                else if (ContainsData("playstation") || ContainsData("xbox") || ContainsData("nintendo")) { result.Type = DeviceTypeCategory.GameConsole; result.TypeString = "Game Console"; }
                else { result.Type = DeviceTypeCategory.IoT; result.TypeString = "IoT Smart Device"; }
            }
            else if (winner.Key == "Linux/Android")
            {
                if (combinedData.Contains("android") || IsMobileVendor(vendor) ||
                    dhcpHostLower.Contains("galaxy") || dhcpHostLower.Contains("pixel") ||
                    dhcpHostLower.Contains("redmi") || dhcpHostLower.Contains("poco") ||
                    dhcpHostLower.Contains("oneplus") || dhcpHostLower.Contains("sm-") ||
                    dhcpOs.Contains("android"))
                {
                    result.Type = DeviceTypeCategory.Mobile;
                    result.TypeString = "Mobile Phone";
                }
                else if (ContainsData("synology") || ContainsData("qnap"))
                {
                    result.Type = DeviceTypeCategory.NAS;
                    result.TypeString = "NAS Storage";
                }
                else
                {
                    result.Type = DeviceTypeCategory.Server;
                    result.TypeString = "Linux Host";
                }
            }
        }

        result.IconSvgKey = DeviceIconMapper.GetIconKey(result.Type, vendor, hostname, ContainsData);

        return result;
    }

    private static bool IsMobileVendor(string vendor)
    {
        return vendor.Contains("apple") || vendor.Contains("samsung") || vendor.Contains("google") ||
               vendor.Contains("oneplus") || vendor.Contains("oppo") || vendor.Contains("vivo") ||
               vendor.Contains("realme") || vendor.Contains("motorola") || vendor.Contains("lg") ||
               vendor.Contains("htc") || vendor.Contains("nokia") || vendor.Contains("sony mobile") ||
               vendor.Contains("xiaomi") || vendor.Contains("huawei") || vendor.Contains("randomized") ||
               vendor.Contains("privacy");
    }

    public static bool IsValidModel(string model)
    {
        if (string.IsNullOrEmpty(model)) return false;
        string lower = model.ToLowerInvariant();
        if (lower.Contains("too many connections") ||
            lower.Contains("try later") ||
            lower.Contains("404 not found") ||
            lower.Contains("400 bad request") ||
            lower.Contains("403 forbidden") ||
            lower.Contains("503 service unavailable") ||
            lower.Contains("500 internal server error") ||
            lower.Contains("bad request") ||
            lower.Contains("nginx") ||
            lower.Contains("apache") ||
            lower.Contains("lighttpd") ||
            lower.Contains("microhttpd") ||
            lower.Contains("rompager") ||
            lower.Contains("gws") ||
            lower.Contains("upnp/") ||
            lower.Contains("connection refused") ||
            lower.Contains("timed out") ||
            lower.Contains("no route"))
        {
            return false;
        }
        return true;
    }
}
