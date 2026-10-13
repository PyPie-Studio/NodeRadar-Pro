using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NodeRadarPro.Core.Fingerprinting;

public static class DeviceClassifierEngine
{
    private const string KeywordApple = "apple";
    private const string KeywordAndroid = "android";
    private const string KeywordIphone = "iphone";
    private const string KeywordIpad = "ipad";
    private const string KeywordOnePlus = "oneplus";
    private const string KeywordXiaomi = "xiaomi";
    private const string KeywordSamsung = "samsung";
    private const string KeywordOppo = "oppo";
    private const string KeywordVivo = "vivo";
    private const string KeywordHuawei = "huawei";
    private const string KeywordRandomized = "randomized";
    private const string KeywordPrivacy = "privacy";

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
            if (dhcpOs.Contains("ios", StringComparison.OrdinalIgnoreCase) || dhcpOs.Contains(KeywordApple, StringComparison.OrdinalIgnoreCase))
            {
                iosScore += 80;
            }
            else if (dhcpOs.Contains(KeywordAndroid, StringComparison.OrdinalIgnoreCase))
            {
                linuxScore += 60;
                iotScore += 40;
            }
            else if (dhcpOs.Contains("windows", StringComparison.OrdinalIgnoreCase))
            {
                winScore += 70;
            }
            else if (dhcpOs.Contains("linux", StringComparison.OrdinalIgnoreCase))
            {
                linuxScore += 50;
            }
            else if (dhcpOs.Contains("cisco", StringComparison.OrdinalIgnoreCase) || dhcpOs.Contains("voip", StringComparison.OrdinalIgnoreCase))
            {
                networkScore += 60;
            }
        }

        // DHCP Vendor Class provides strong OS hints
        if (!string.IsNullOrEmpty(dhcpVendorClass))
        {
            if (dhcpVendorClass.Contains("msft", StringComparison.OrdinalIgnoreCase)) winScore += 40;
            if (dhcpVendorClass.Contains(KeywordAndroid, StringComparison.OrdinalIgnoreCase)) { linuxScore += 50; iotScore += 30; }
            if (dhcpVendorClass.Contains(KeywordApple, StringComparison.OrdinalIgnoreCase) || dhcpVendorClass.Contains("dhcpc", StringComparison.OrdinalIgnoreCase)) iosScore += 30;
        }

        // DHCP Hostname pattern matching — very reliable for mobile identification
        string dhcpHostLower = dhcpHostname.ToLowerInvariant();
        if (!string.IsNullOrEmpty(dhcpHostLower))
        {
            // Apple patterns
            if (dhcpHostLower.Contains(KeywordIphone, StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains(KeywordIpad, StringComparison.OrdinalIgnoreCase) ||
                dhcpHostLower.Contains("macbook", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("imac", StringComparison.OrdinalIgnoreCase) ||
                dhcpHostLower.Contains(KeywordApple, StringComparison.OrdinalIgnoreCase))
            {
                iosScore += 60;
            }
            // Android phone patterns
            else if (dhcpHostLower.Contains("galaxy", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("pixel", StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains("redmi", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("poco", StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains(KeywordOnePlus, StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains(KeywordOppo, StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains(KeywordVivo, StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("realme", StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains("motorola", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("moto", StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains("nokia", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains(KeywordHuawei, StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains("honor", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains(KeywordXiaomi, StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains(KeywordSamsung, StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("sm-", StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains("note", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("a5", StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains(KeywordAndroid, StringComparison.OrdinalIgnoreCase))
            {
                linuxScore += 50;
                iotScore += 30;
            }
            // Windows patterns
            else if (dhcpHostLower.Contains("desktop-", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("laptop-", StringComparison.OrdinalIgnoreCase) ||
                     dhcpHostLower.Contains("windows", StringComparison.OrdinalIgnoreCase))
            {
                winScore += 30;
            }
        }

        // ═══════════════════════════════════════
        // 1. MAC & Vendor Scoring
        if (vendor.Contains(KeywordApple, StringComparison.OrdinalIgnoreCase)) iosScore += 30;

        if (vendor.Contains("microsoft", StringComparison.OrdinalIgnoreCase) || vendor.Contains("intel", StringComparison.OrdinalIgnoreCase) || vendor.Contains("dell", StringComparison.OrdinalIgnoreCase) || vendor.Contains("hp", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("lenovo", StringComparison.OrdinalIgnoreCase) || vendor.Contains("asus", StringComparison.OrdinalIgnoreCase) || vendor.Contains("acer", StringComparison.OrdinalIgnoreCase) || vendor.Contains("gigabyte", StringComparison.OrdinalIgnoreCase) || vendor.Contains("msi", StringComparison.OrdinalIgnoreCase))
            winScore += 10;

        if (vendor.Contains("raspberry", StringComparison.OrdinalIgnoreCase) || vendor.Contains("synology", StringComparison.OrdinalIgnoreCase) || vendor.Contains("qnap", StringComparison.OrdinalIgnoreCase)) linuxScore += 40;

        if (vendor.Contains(KeywordXiaomi, StringComparison.OrdinalIgnoreCase) || vendor.Contains("tuya", StringComparison.OrdinalIgnoreCase) || vendor.Contains("espressif", StringComparison.OrdinalIgnoreCase)) iotScore += 30;

        // Mobile vendors
        if (vendor.Contains(KeywordSamsung, StringComparison.OrdinalIgnoreCase) || vendor.Contains("google", StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordOnePlus, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordOppo, StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains(KeywordVivo, StringComparison.OrdinalIgnoreCase) || vendor.Contains("realme", StringComparison.OrdinalIgnoreCase) || vendor.Contains("motorola", StringComparison.OrdinalIgnoreCase) || vendor.Contains("lg", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("htc", StringComparison.OrdinalIgnoreCase) || vendor.Contains("nokia", StringComparison.OrdinalIgnoreCase) || vendor.Contains("sony mobile", StringComparison.OrdinalIgnoreCase))
        {
            linuxScore += 40;
            iotScore += 20;
        }

        // Routers, gateways, ONUs
        if (vendor.Contains("cisco", StringComparison.OrdinalIgnoreCase) || vendor.Contains("tp-link", StringComparison.OrdinalIgnoreCase) || vendor.Contains("ubiquiti", StringComparison.OrdinalIgnoreCase) || vendor.Contains("mikrotik", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains(KeywordHuawei, StringComparison.OrdinalIgnoreCase) || vendor.Contains("sagemcom", StringComparison.OrdinalIgnoreCase) || vendor.Contains("fiberhome", StringComparison.OrdinalIgnoreCase) || vendor.Contains("netgear", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("d-link", StringComparison.OrdinalIgnoreCase) || vendor.Contains("linksys", StringComparison.OrdinalIgnoreCase) || vendor.Contains("technicolor", StringComparison.OrdinalIgnoreCase) || vendor.Contains("zte", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("zyxel", StringComparison.OrdinalIgnoreCase) || vendor.Contains("tenda", StringComparison.OrdinalIgnoreCase) || vendor.Contains("mercusys", StringComparison.OrdinalIgnoreCase) || vendor.Contains("totolink", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("netis", StringComparison.OrdinalIgnoreCase) || vendor.Contains("router", StringComparison.OrdinalIgnoreCase) || vendor.Contains("gateway", StringComparison.OrdinalIgnoreCase))
        {
            networkScore += 30;
        }

        // Privacy MAC strongly hints mobile (assign equally to iOS and Linux/Android to avoid bias)
        if (vendor.Contains(KeywordRandomized, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordPrivacy, StringComparison.OrdinalIgnoreCase))
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
        if (ContainsData(KeywordAndroid)) { iotScore += 40; linuxScore += 20; }

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

        // 4. Hostname Keywords & NetBIOS Resolution
        if (hostname.Contains(KeywordIphone, StringComparison.OrdinalIgnoreCase) || hostname.Contains(KeywordIpad, StringComparison.OrdinalIgnoreCase) || hostname.Contains("apple-", StringComparison.OrdinalIgnoreCase)) iosScore += 50;
        if (hostname.Contains("windows", StringComparison.OrdinalIgnoreCase) || hostname.Contains("desktop-", StringComparison.OrdinalIgnoreCase) || hostname.Contains("laptop-", StringComparison.OrdinalIgnoreCase)) winScore += 30;
        if (hostname.Contains(KeywordAndroid, StringComparison.OrdinalIgnoreCase) || hostname.Contains("galaxy", StringComparison.OrdinalIgnoreCase)) iotScore += 30;

        var nbnsProbe = probeResults.FirstOrDefault(pr => pr.Source == "NetBIOS (NBNS)");
        string nbnsHostname = nbnsProbe?.GetValue("NbnsHostname") ?? "";
        string nbnsDomain = nbnsProbe?.GetValue("NbnsDomain") ?? "";
        if (!string.IsNullOrEmpty(nbnsHostname))
        {
            winScore += 50;
            if (string.IsNullOrEmpty(node.Hostname) || node.Hostname == "Unknown Device")
            {
                node.Hostname = nbnsHostname;
            }
        }
        if (!string.IsNullOrEmpty(nbnsDomain))
        {
            winScore += 20;
        }

        // ═══════════════════════════════════════
        // 5. "Silent Device" Heuristic
        // ═══════════════════════════════════════
        // Devices with Privacy MAC + no open ports + no banners are almost certainly mobile phones
        bool isPrivacyMac = vendor.Contains(KeywordPrivacy, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordRandomized, StringComparison.OrdinalIgnoreCase);
        bool hasNoPorts = node.OpenPorts == null || node.OpenPorts.Count == 0;
        bool hasNoBanners = node.PortBanners == null || node.PortBanners.Count == 0;

        string cleanedData = combinedData.Replace("privacy mac", "", StringComparison.OrdinalIgnoreCase).Replace("randomized mac", "", StringComparison.OrdinalIgnoreCase).Trim();
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
                if (combinedData.Contains(KeywordIphone, StringComparison.OrdinalIgnoreCase) || combinedData.Contains(KeywordIpad, StringComparison.OrdinalIgnoreCase) ||
                    vendor.Contains(KeywordRandomized, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordPrivacy, StringComparison.OrdinalIgnoreCase) ||
                    dhcpHostLower.Contains(KeywordIphone, StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains(KeywordIpad, StringComparison.OrdinalIgnoreCase))
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
                else if (ContainsData(KeywordAndroid) || (vendor.Contains(KeywordXiaomi, StringComparison.OrdinalIgnoreCase) && !ContainsData("light"))) { result.Type = DeviceTypeCategory.Mobile; result.TypeString = "Mobile Phone"; }
                else if (ContainsData("playstation") || ContainsData("xbox") || ContainsData("nintendo")) { result.Type = DeviceTypeCategory.GameConsole; result.TypeString = "Game Console"; }
                else { result.Type = DeviceTypeCategory.IoT; result.TypeString = "IoT Smart Device"; }
            }
            else if (winner.Key == "Linux/Android")
            {
                if (combinedData.Contains(KeywordAndroid, StringComparison.OrdinalIgnoreCase) || IsMobileVendor(vendor) ||
                    dhcpHostLower.Contains("galaxy", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("pixel", StringComparison.OrdinalIgnoreCase) ||
                    dhcpHostLower.Contains("redmi", StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("poco", StringComparison.OrdinalIgnoreCase) ||
                    dhcpHostLower.Contains(KeywordOnePlus, StringComparison.OrdinalIgnoreCase) || dhcpHostLower.Contains("sm-", StringComparison.OrdinalIgnoreCase) ||
                    dhcpOs.Contains(KeywordAndroid, StringComparison.OrdinalIgnoreCase))
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
        return vendor.Contains(KeywordApple, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordSamsung, StringComparison.OrdinalIgnoreCase) || vendor.Contains("google", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains(KeywordOnePlus, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordOppo, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordVivo, StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("realme", StringComparison.OrdinalIgnoreCase) || vendor.Contains("motorola", StringComparison.OrdinalIgnoreCase) || vendor.Contains("lg", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("htc", StringComparison.OrdinalIgnoreCase) || vendor.Contains("nokia", StringComparison.OrdinalIgnoreCase) || vendor.Contains("sony mobile", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains(KeywordXiaomi, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordHuawei, StringComparison.OrdinalIgnoreCase) || vendor.Contains(KeywordRandomized, StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains(KeywordPrivacy, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidModel(string model)
    {
        if (string.IsNullOrEmpty(model)) return false;
        string lower = model.ToLowerInvariant();
        if (lower.Contains("too many connections", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("try later", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("404 not found", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("400 bad request", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("403 forbidden", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("503 service unavailable", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("500 internal server error", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("bad request", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("nginx", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("apache", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("lighttpd", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("microhttpd", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("rompager", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("gws", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("upnp/", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("connection refused", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("no route", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return true;
    }
}
