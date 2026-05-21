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

        string combinedData = string.Join(" ", probeResults.SelectMany(pr => pr.RawData.Values)).ToLowerInvariant();
        string hostname = node.Hostname?.ToLowerInvariant() ?? "";
        
        string vendor = probeResults.FirstOrDefault(pr => pr.Source == "MAC OUI Lookup")?.GetValue("Vendor")?.ToLowerInvariant() ?? "";
        result.Vendor = string.IsNullOrEmpty(vendor) ? "Unknown Vendor" : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(vendor);

        // 1. MAC & Vendor Scoring
        if (vendor.Contains("apple")) iosScore += 30;
        if (vendor.Contains("microsoft") || vendor.Contains("intel") || vendor.Contains("dell") || vendor.Contains("hp")) winScore += 10;
        if (vendor.Contains("raspberry") || vendor.Contains("synology") || vendor.Contains("qnap")) linuxScore += 40;
        if (vendor.Contains("xiaomi") || vendor.Contains("tuya") || vendor.Contains("espressif")) iotScore += 30;
        if (vendor.Contains("cisco") || vendor.Contains("tp-link") || vendor.Contains("ubiquiti") || vendor.Contains("mikrotik") || vendor.Contains("huawei")) networkScore += 30;
        if (vendor.Contains("randomized mac") || vendor.Contains("privacy")) { iosScore += 20; iotScore += 20; }

        // 2. Banner & Protocol Data Scoring
        if (combinedData.Contains("openssh")) linuxScore += 40;
        if (combinedData.Contains("microsoft-httpapi") || combinedData.Contains("iis")) winScore += 50;
        if (combinedData.Contains("mikrotik") || combinedData.Contains("routeros")) networkScore += 100;
        if (combinedData.Contains("nginx") || combinedData.Contains("apache")) linuxScore += 30;
        if (combinedData.Contains("printer") || combinedData.Contains("ipp") || combinedData.Contains("canon") || combinedData.Contains("epson") || combinedData.Contains("hp jetdirect")) printerScore += 80;
        if (combinedData.Contains("apple-icon") || combinedData.Contains("apple device") || combinedData.Contains("homekit")) iosScore += 60;
        if (combinedData.Contains("cast") || combinedData.Contains("chromecast") || combinedData.Contains("spotify") || combinedData.Contains("speaker") || combinedData.Contains("tv") || combinedData.Contains("webos")) iotScore += 60;
        if (combinedData.Contains("android")) { iotScore += 40; linuxScore += 20; }

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
                if (combinedData.Contains("iphone") || combinedData.Contains("ipad"))
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
                if (combinedData.Contains("switch")) { result.Type = DeviceTypeCategory.Switch; result.TypeString = "Network Switch"; }
                else if (combinedData.Contains("firewall")) { result.Type = DeviceTypeCategory.Firewall; result.TypeString = "Firewall"; }
                else if (combinedData.Contains("unifi") || combinedData.Contains("ap")) { result.Type = DeviceTypeCategory.AccessPoint; result.TypeString = "Access Point"; }
                else { result.Type = DeviceTypeCategory.Router; result.TypeString = "Router / Gateway"; }
            }
            else if (winner.Key == "IoT")
            {
                if (combinedData.Contains("tv") || combinedData.Contains("tizen") || combinedData.Contains("webos")) { result.Type = DeviceTypeCategory.TV; result.TypeString = "Smart TV"; }
                else if (combinedData.Contains("speaker") || combinedData.Contains("sonos")) { result.Type = DeviceTypeCategory.Speaker; result.TypeString = "Smart Speaker"; }
                else if (combinedData.Contains("camera") || combinedData.Contains("hikvision") || combinedData.Contains("dahua")) { result.Type = DeviceTypeCategory.Camera; result.TypeString = "IP Camera"; }
                else if (combinedData.Contains("android") || (vendor.Contains("xiaomi") && !combinedData.Contains("light"))) { result.Type = DeviceTypeCategory.Mobile; result.TypeString = "Mobile Phone"; }
                else if (combinedData.Contains("playstation") || combinedData.Contains("xbox") || combinedData.Contains("nintendo")) { result.Type = DeviceTypeCategory.GameConsole; result.TypeString = "Game Console"; }
                else { result.Type = DeviceTypeCategory.IoT; result.TypeString = "IoT Smart Device"; }
            }
            else if (winner.Key == "Linux/Android")
            {
                if (combinedData.Contains("android") || vendor.Contains("samsung") || vendor.Contains("huawei"))
                {
                    result.Type = DeviceTypeCategory.Mobile;
                    result.TypeString = "Mobile Phone";
                }
                else if (combinedData.Contains("synology") || combinedData.Contains("qnap"))
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

        result.IconSvgKey = DeviceIconMapper.GetIconKey(result.Type, vendor, hostname, combinedData);

        return result;
    }
}
