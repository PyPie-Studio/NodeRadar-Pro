using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeRadarPro.Core;

public static class DeviceClassifier
{
    /// <summary>
    /// Uses a weighted scoring system to identify the OS, Device Type, and Icon for a node.
    /// Incorporates Active Banner data, MAC prefixes, mDNS, SSDP, and Port Fingerprinting.
    /// </summary>
    public static void ResolveDetails(NetworkNode node)
    {
        int iosScore = 0;
        int winScore = 0;
        int linuxScore = 0;
        int iotScore = 0;
        int printerScore = 0;
        int networkScore = 0;

        string vendor = node.Vendor?.ToLower() ?? "";
        string hostname = node.Hostname?.ToLower() ?? "";
        string exact = node.ExactModel?.ToLower() ?? "";

        // ── 1. Active Banner / Version Scoring ──
        // (Banners gathered via TCP ports 22, 80, etc. in SubnetScanner)
        if (exact.Contains("openssh")) linuxScore += 40;
        if (exact.Contains("microsoft-httpapi") || exact.Contains("iis")) winScore += 50;
        if (exact.Contains("mikrotik") || exact.Contains("routeros")) networkScore += 100;
        if (exact.Contains("nginx") || exact.Contains("apache")) linuxScore += 30;
        if (exact.Contains("yeelight") || exact.Contains("mi-light")) iotScore += 100;
        if (exact.Contains("samsung") && exact.Contains("tv")) iotScore += 80;
        if (exact.Contains("huawei") || exact.Contains("hg8120")) networkScore += 100;

        // ── 2. MAC Prefix Scoring ──
        if (vendor.Contains("apple")) iosScore += 30;
        if (vendor.Contains("microsoft") || vendor.Contains("intel") || vendor.Contains("dell") || vendor.Contains("hp") || vendor.Contains("asustek")) winScore += 10;
        if (vendor.Contains("raspberry") || vendor.Contains("synology") || vendor.Contains("qnap")) linuxScore += 40;
        if (vendor.Contains("espressif") || vendor.Contains("xiaomi") || vendor.Contains("tuya") || vendor.Contains("yeelink") || vendor.Contains("huawei")) 
        {
            if (vendor.Contains("huawei")) networkScore += 100;
            else iotScore += 30;
        }
        if (vendor.Contains("cisco") || vendor.Contains("tp-link") || vendor.Contains("ubiquiti") || vendor.Contains("netgear")) networkScore += 20;

        // ── 3. Protocol Signals (mDNS/SSDP) ──
        if (exact.Contains("apple") || exact.Contains("airplay") || exact.Contains("homekit")) 
        {
            // Safeguard: Don't tag as Apple if banner explicitly says Huawei or HG8120
            if (!exact.Contains("huawei") && !exact.Contains("hg8120")) iosScore += 60;
        }
        if (exact.Contains("chromecast") || exact.Contains("google cast") || exact.Contains("google-home")) iotScore += 60;
        if (exact.Contains("spotify") || exact.Contains("speaker") || exact.Contains("sonos") || exact.Contains("bose")) iotScore += 40;
        if (exact.Contains("bulb") || exact.Contains("light") || exact.Contains("hue") || exact.Contains("ring") || exact.Contains("nest") || exact.Contains("arlo")) iotScore += 50;
        if (exact.Contains("tv") || exact.Contains("tizen") || exact.Contains("webos") || exact.Contains("bravia") || exact.Contains("roku") || exact.Contains("vizio")) iotScore += 50;
        if (exact.Contains("printer") || exact.Contains("ipp") || exact.Contains("canon") || exact.Contains("epson") || exact.Contains("hp jetdirect")) printerScore += 80;
        if (exact.Contains("workstation") || exact.Contains("windows")) winScore += 40;

        // ── 4. Port Fingerprinting ──
        foreach (int port in node.OpenPorts)
        {
            switch (port)
            {
                case 62078: iosScore += 80; break; // Apple mobile lockdown
                case 548: iosScore += 40; break;   // AFP
                case 135: case 445: case 3389: winScore += 50; break; // RPC/SMB/RDP
                case 5357: winScore += 40; break;  // WSD (Web Services for Devices)
                case 22: linuxScore += 30; break;  // SSH
                case 23: case 21: networkScore += 30; break; // Telnet/FTP (common on routers)
                case 9100: case 631: printerScore += 80; break; // JetDirect/IPP
                case 8008: case 8009: iotScore += 40; break; // Google Cast
            }
        }

        // ── 5. Hostname Keywords ──
        if (hostname.Contains("iphone") || hostname.Contains("ipad") || hostname.Contains("apple-")) iosScore += 50;
        if (hostname.Contains("windows") || hostname.Contains("desktop-") || hostname.Contains("laptop-")) winScore += 30;
        if (hostname.Contains("android") || hostname.Contains("galaxy") || hostname.Contains("pixel") || hostname.Contains("mi-")) iotScore += 30;

        // ── 6. Final Decision ──
        var scores = new Dictionary<string, int>
        {
            { "macOS/iOS", iosScore },
            { "Windows", winScore },
            { "Linux/Android", linuxScore },
            { "IoT/Smart Device", iotScore },
            { "Printer", printerScore },
            { "Infrastructure", networkScore }
        };

        var winner = scores.OrderByDescending(x => x.Value).First();

        if (winner.Value >= 25)
        {
            node.OsGuess = winner.Key;
            
            // Refine DeviceType and Icon based on specific keyword strength
            if (winner.Key == "macOS/iOS")
            {
                if (hostname.Contains("iphone") || hostname.Contains("ipad") || exact.Contains("iphone"))
                {
                    node.DeviceType = "Mobile Phone";
                    node.IconPath = "phone";
                }
                else
                {
                    node.DeviceType = "Apple Computer";
                    node.IconPath = "pc";
                }
            }
            else if (winner.Key == "Windows")
            {
                node.DeviceType = "Windows PC";
                node.IconPath = "pc";
            }
            else if (winner.Key == "Printer")
            {
                node.DeviceType = "Network Printer";
                node.IconPath = "printer";
            }
            else if (winner.Key == "Infrastructure")
            {
                node.DeviceType = "Network Router/Switch";
                node.IconPath = "router";
            }
            else if (winner.Key == "IoT/Smart Device")
            {
                // Sub-classification for IoT
                if (exact.Contains("bulb") || exact.Contains("light") || exact.Contains("yeelight"))
                {
                    node.DeviceType = "Smart Lighting";
                    node.IconPath = "iot";
                }
                else if (exact.Contains("tv") || hostname.Contains("tv") || exact.Contains("tizen"))
                {
                    node.DeviceType = "Smart TV";
                    node.IconPath = "tv";
                }
                else if (exact.Contains("speaker") || exact.Contains("sonos") || exact.Contains("home-pod"))
                {
                    node.DeviceType = "Smart Speaker";
                    node.IconPath = "speaker";
                }
                else if (hostname.Contains("android") || (vendor.Contains("xiaomi") && !exact.Contains("light")))
                {
                    node.DeviceType = "Mobile Phone";
                    node.IconPath = "phone";
                }
                else
                {
                    node.DeviceType = "IoT Device";
                    node.IconPath = "iot";
                }
            }
            else if (winner.Key == "Linux/Android")
            {
                if (hostname.Contains("android") || vendor.Contains("samsung") || vendor.Contains("huawei"))
                {
                    node.DeviceType = "Mobile Phone";
                    node.IconPath = "phone";
                }
                else
                {
                    node.DeviceType = "Linux Host";
                    node.IconPath = "server";
                }
            }
        }
        else
        {
            // Confidence too low - keep it generic but try vendor-only guess
            if (vendor.Contains("xiaomi") || vendor.Contains("samsung") || vendor.Contains("apple"))
            {
                node.DeviceType = "Mobile Device";
                node.IconPath = "phone";
            }
            else
            {
                node.DeviceType = "Generic Network Device";
                node.IconPath = "router";
            }
        }
    }
}
