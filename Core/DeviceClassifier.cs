using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeRadarPro.Core;

public static class DeviceClassifier
{
    /// <summary>
    /// Uses a weighted scoring system to identify the OS, Device Type, and Icon for a node.
    /// </summary>
    public static void ResolveDetails(NetworkNode node)
    {
        int iosScore = 0;
        int winScore = 0;
        int linuxScore = 0;
        int iotScore = 0;
        int printerScore = 0;

        string vendor = node.Vendor?.ToLower() ?? "";
        string hostname = node.Hostname?.ToLower() ?? "";
        string exact = node.ExactModel?.ToLower() ?? "";

        // ── 1. MAC Prefix Scoring ──
        if (vendor.Contains("apple")) iosScore += 30;
        if (vendor.Contains("microsoft") || vendor.Contains("intel") || vendor.Contains("dell") || vendor.Contains("hp")) winScore += 10;
        if (vendor.Contains("raspberry") || vendor.Contains("espressif")) linuxScore += 40;
        if (vendor.Contains("xiaomi") || vendor.Contains("samsung") || vendor.Contains("huawei") || vendor.Contains("google")) iotScore += 20;

        // ── 2. Protocol Signals (mDNS/SSDP) ──
        if (exact.Contains("apple") || exact.Contains("airplay") || exact.Contains("homekit")) iosScore += 60;
        if (exact.Contains("chromecast") || exact.Contains("google cast")) iotScore += 60;
        if (exact.Contains("spotify") || exact.Contains("speaker") || exact.Contains("sonos") || exact.Contains("bose")) iotScore += 40;
        if (exact.Contains("bulb") || exact.Contains("light") || exact.Contains("yeelight") || exact.Contains("hue") || exact.Contains("ring") || exact.Contains("nest") || exact.Contains("arlo")) iotScore += 50;
        if (exact.Contains("tv") || exact.Contains("samsung") || exact.Contains("tizen") || exact.Contains("webos") || exact.Contains("bravia") || exact.Contains("roku") || exact.Contains("vizio") || exact.Contains("lg")) iotScore += 50;
        if (exact.Contains("printer") || exact.Contains("ipp") || exact.Contains("canon") || exact.Contains("epson")) printerScore += 80;
        if (exact.Contains("workstation") || exact.Contains("windows")) winScore += 40;
        if (exact.Contains("smb") || exact.Contains("server")) linuxScore += 20;

        // ── 3. Port Fingerprinting ──
        foreach (int port in node.OpenPorts)
        {
            switch (port)
            {
                case 62078: iosScore += 80; break; // Apple mobile lockdown
                case 548: iosScore += 40; break;   // AFP
                case 135: case 445: case 3389: winScore += 50; break; // RPC/SMB/RDP
                case 5357: winScore += 40; break;  // WSD (Web Services for Devices)
                case 22: linuxScore += 30; break;  // SSH
                case 9100: case 631: printerScore += 80; break; // JetDirect/IPP
                case 8008: case 8009: iotScore += 40; break; // Google Cast
                case 5000: iotScore += 20; break; // Common IoT web
            }
        }

        // ── 4. Hostname Keywords ──
        if (hostname.Contains("iphone") || hostname.Contains("ipad") || hostname.Contains("apple")) iosScore += 40;
        if (hostname.Contains("windows") || hostname.Contains("desktop-") || hostname.Contains("laptop-")) winScore += 30;
        if (hostname.Contains("android") || hostname.Contains("galaxy") || hostname.Contains("pixel")) iotScore += 30;

        // ── 5. Final Decision ──
        var scores = new Dictionary<string, int>
        {
            { "macOS/iOS", iosScore },
            { "Windows", winScore },
            { "Linux/Android", linuxScore },
            { "IoT/Smart Device", iotScore },
            { "Printer", printerScore }
        };

        var winner = scores.OrderByDescending(x => x.Value).First();

        // Threshold for a confident guess
        if (winner.Value >= 30)
        {
            node.OsGuess = winner.Key;
            
            // Refine DeviceType and Icon based on winner
            if (winner.Key == "macOS/iOS")
            {
                if (hostname.Contains("iphone") || vendor.Contains("apple"))
                {
                    node.DeviceType = "Mobile Phone";
                    node.IconPath = "phone";
                }
                else
                {
                    node.DeviceType = "Computer";
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
                node.DeviceType = "Printer";
                node.IconPath = "printer";
            }
            else if (winner.Key == "IoT/Smart Device")
            {
                node.DeviceType = "IoT Device";
                node.IconPath = "iot"; 
                
                if (exact.Contains("tv") || hostname.Contains("tv") || exact.Contains("tizen") || exact.Contains("webos")) 
                {
                    node.DeviceType = "Smart TV";
                    node.IconPath = "tv";
                }
                else if (exact.Contains("speaker") || exact.Contains("sonos") || exact.Contains("bose") || exact.Contains("spotify")) 
                {
                    node.DeviceType = "Smart Speaker";
                    node.IconPath = "speaker";
                }
                else if (hostname.Contains("android") || vendor.Contains("xiaomi") || vendor.Contains("samsung") || vendor.Contains("huawei"))
                {
                    node.DeviceType = "Mobile Phone";
                    node.IconPath = "phone";
                }
            }
            else if (winner.Key == "Linux/Android")
            {
                node.DeviceType = "Linux Device";
                node.IconPath = "server";
                if (hostname.Contains("android"))
                {
                    node.DeviceType = "Mobile Phone";
                    node.IconPath = "phone";
                }
            }
        }
        
        // Final sanity check for common mobile vendors if still generic
        if (node.IconPath == "default_device" || node.IconPath == "router")
        {
            if (vendor.Contains("xiaomi") || vendor.Contains("samsung") || vendor.Contains("huawei") || vendor.Contains("oppo"))
            {
                node.DeviceType = "Mobile Phone";
                node.IconPath = "phone";
            }
        }
    }
}
