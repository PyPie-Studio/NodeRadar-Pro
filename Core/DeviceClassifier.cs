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
        int androidScore = 0;
        int iosScore = 0;
        int winScore = 0;
        int linuxScore = 0;
        int iotScore = 0;
        int printerScore = 0;
        int networkScore = 0;
        int consoleScore = 0;

        string vendor = node.Vendor?.ToLower() ?? "";
        string hostname = node.Hostname?.ToLower() ?? "";
        string exact = node.ExactModel?.ToLower() ?? "";

        // ── 1. Active Banner / Version Scoring ──
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
        if (vendor.Contains("espressif") || vendor.Contains("tuya") || vendor.Contains("yeelink")) iotScore += 30;
        if (vendor.Contains("cisco") || vendor.Contains("tp-link") || vendor.Contains("ubiquiti") || vendor.Contains("netgear")) networkScore += 20;

        // High confidence mobile brands
        if (vendor.Contains("xiaomi") || vendor.Contains("samsung") || vendor.Contains("huawei") ||
            vendor.Contains("realme") || vendor.Contains("tecno") || vendor.Contains("redmi") ||
            vendor.Contains("poco") || vendor.Contains("vivo") || vendor.Contains("oppo") ||
            vendor.Contains("oneplus") || vendor.Contains("motorola") || vendor.Contains("nokia"))
        {
            androidScore += 40;
        }

        if (vendor.Contains("sony") && !vendor.Contains("mobile")) consoleScore += 30;
        if (vendor.Contains("nintendo")) consoleScore += 30;

        if (vendor.Contains("randomized mac") || vendor.Contains("privacy"))
        {
            iosScore += 30;
            androidScore += 30;
            iotScore += 10;
        }

        // ── 3. Protocol Signals (mDNS/SSDP) ──
        if (exact.Contains("apple") || exact.Contains("airplay") || exact.Contains("homekit")) 
        {
            if (!exact.Contains("huawei") && !exact.Contains("hg8120")) iosScore += 60;
        }
        if (exact.Contains("android") || exact.Contains("androidtv")) androidScore += 60;
        if (exact.Contains("chromecast") || exact.Contains("google cast") || exact.Contains("google-home")) iotScore += 60;
        if (exact.Contains("spotify") || exact.Contains("speaker") || exact.Contains("sonos") || exact.Contains("bose")) iotScore += 40;
        if (exact.Contains("bulb") || exact.Contains("light") || exact.Contains("hue") || exact.Contains("ring") || exact.Contains("nest") || exact.Contains("arlo")) iotScore += 50;
        if (exact.Contains("tv") || exact.Contains("tizen") || exact.Contains("webos") || exact.Contains("bravia") || exact.Contains("roku") || exact.Contains("vizio")) iotScore += 50;
        if (exact.Contains("printer") || exact.Contains("ipp") || exact.Contains("canon") || exact.Contains("epson") || exact.Contains("hp jetdirect")) printerScore += 80;
        if (exact.Contains("workstation") || exact.Contains("windows")) winScore += 40;
        if (exact.Contains("playstation") || exact.Contains("xbox") || exact.Contains("nintendo")) consoleScore += 60;

        // ── 4. Port Fingerprinting ──
        foreach (int port in node.OpenPorts)
        {
            switch (port)
            {
                case 62078: iosScore += 80; break; // Apple mobile lockdown
                case 5555: androidScore += 80; break; // Android ADB
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
        if (hostname.Contains("iphone") || hostname.Contains("ipad") || hostname.Contains("apple-") || exact.Contains("iphone") || exact.Contains("ipad")) iosScore += 50;
        if (hostname.Contains("macbook") || hostname.Contains("imac") || hostname.Contains("mac-mini")) iosScore += 40;
        if (hostname.Contains("windows") || hostname.Contains("desktop-") || hostname.Contains("laptop-")) winScore += 30;

        // Deep keyword analysis for mobile hostnames
        if (hostname.Contains("android") || hostname.Contains("galaxy") || hostname.Contains("pixel") ||
            hostname.Contains("mi-") || hostname.Contains("redmi") || hostname.Contains("poco") ||
            hostname.Contains("xiaomi") || hostname.Contains("huawei") || hostname.Contains("samsung") ||
            hostname.Contains("realme") || hostname.Contains("tecno") || hostname.Contains("oppo") ||
            hostname.Contains("vivo") || hostname.Contains("oneplus") || exact.Contains("android"))
        {
            androidScore += 50;
        }

        if (hostname.Contains("playstation") || hostname.Contains("xbox") || hostname.Contains("nintendo") || hostname.Contains("switch")) consoleScore += 50;
        if (hostname.Contains("router") || hostname.Contains("gateway") || hostname.Contains("ap") || hostname.Contains("wifi")) networkScore += 40;

        // ── 6. Final Decision ──
        var scores = new Dictionary<string, int>
        {
            { "macOS/iOS", iosScore },
            { "Android", androidScore },
            { "Windows", winScore },
            { "Linux", linuxScore },
            { "IoT/Smart Device", iotScore },
            { "Printer", printerScore },
            { "Infrastructure", networkScore },
            { "Game Console", consoleScore }
        };

        var winner = scores.OrderByDescending(x => x.Value).First();

        if (winner.Value >= 25)
        {
            node.OsGuess = winner.Key;
            
            if (winner.Key == "macOS/iOS")
            {
                if (hostname.Contains("iphone") || exact.Contains("iphone") || vendor.Contains("randomized mac") || vendor.Contains("privacy"))
                {
                    node.DeviceType = "iPhone";
                    node.IconPath = "phone";
                }
                else if (hostname.Contains("ipad") || exact.Contains("ipad"))
                {
                    node.DeviceType = "iPad";
                    node.IconPath = "phone"; // Or a tablet icon if available
                }
                else if (hostname.Contains("watch") || exact.Contains("watch"))
                {
                    node.DeviceType = "Apple Watch";
                    node.IconPath = "iot";
                }
                else
                {
                    node.DeviceType = "Mac";
                    node.IconPath = "pc";
                }
            }
            else if (winner.Key == "Android")
            {
                if (exact.Contains("tv") || hostname.Contains("tv"))
                {
                    node.DeviceType = "Smart TV";
                    node.IconPath = "tv";
                }
                else
                {
                    // Try to extract brand if we can for better naming
                    string[] brands = { "Samsung", "Xiaomi", "Huawei", "Realme", "Tecno", "Redmi", "Poco", "Vivo", "Oppo", "OnePlus", "Pixel", "Motorola" };
                    string foundBrand = "";
                    foreach (var b in brands)
                    {
                        if (hostname.Contains(b.ToLower()) || vendor.Contains(b.ToLower()) || exact.Contains(b.ToLower()))
                        {
                            foundBrand = b;
                            break;
                        }
                    }
                    node.DeviceType = string.IsNullOrEmpty(foundBrand) ? "Android Phone" : $"{foundBrand} Device";
                    node.IconPath = "phone";
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
            else if (winner.Key == "Game Console")
            {
                if (hostname.Contains("xbox") || exact.Contains("xbox")) node.DeviceType = "Xbox";
                else if (hostname.Contains("playstation") || vendor.Contains("sony")) node.DeviceType = "PlayStation";
                else if (hostname.Contains("nintendo") || vendor.Contains("nintendo")) node.DeviceType = "Nintendo Console";
                else node.DeviceType = "Game Console";
                node.IconPath = "iot";
            }
            else if (winner.Key == "IoT/Smart Device")
            {
                if (exact.Contains("bulb") || exact.Contains("light") || exact.Contains("yeelight"))
                {
                    node.DeviceType = "Smart Lighting";
                    node.IconPath = "iot";
                }
                else if (exact.Contains("tv") || hostname.Contains("tv") || exact.Contains("tizen") || vendor.Contains("lg") || vendor.Contains("roku") || vendor.Contains("vizio"))
                {
                    node.DeviceType = "Smart TV";
                    node.IconPath = "tv";
                }
                else if (exact.Contains("speaker") || exact.Contains("sonos") || exact.Contains("home-pod"))
                {
                    node.DeviceType = "Smart Speaker";
                    node.IconPath = "speaker";
                }
                else
                {
                    node.DeviceType = "IoT Device";
                    node.IconPath = "iot";
                }
            }
            else if (winner.Key == "Linux")
            {
                node.DeviceType = "Linux Host";
                node.IconPath = "server";
            }
        }
        else
        {
            // Confidence too low - keep it generic but try vendor-only guess as an absolute fallback
            if (vendor.Contains("xiaomi") || vendor.Contains("samsung") || vendor.Contains("huawei") ||
                vendor.Contains("realme") || vendor.Contains("tecno") || vendor.Contains("vivo") ||
                vendor.Contains("oppo") || vendor.Contains("oneplus") || vendor.Contains("motorola") ||
                vendor.Contains("nokia") || vendor.Contains("apple") || vendor.Contains("randomized mac") ||
                vendor.Contains("privacy"))
            {
                node.DeviceType = "Mobile Device";
                node.IconPath = "phone";
            }
            else if (vendor.Contains("lg") || vendor.Contains("roku") || vendor.Contains("vizio"))
            {
                node.DeviceType = "Smart TV";
                node.IconPath = "tv";
            }
            else if (vendor.Contains("sony") || vendor.Contains("nintendo"))
            {
                node.DeviceType = "Game Console";
                node.IconPath = "iot";
            }
            else
            {
                node.DeviceType = "Generic Network Device";
                node.IconPath = "router";
            }
        }
    }
}
