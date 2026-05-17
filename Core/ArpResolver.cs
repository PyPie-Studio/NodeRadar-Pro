using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace NodeRadarPro.Core;

/// <summary>
/// Handles cross-platform MAC address resolution (ARP).
/// Supports Windows natively and Linux by reading /proc/net/arp.
/// Also provides full ARP table reading to discover WiFi/non-ping devices.
/// </summary>
public static class ArpResolver
{
    // Native Windows API for ARP resolution
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

    /// <summary>
    /// Attempts to resolve the MAC address for a given IP address.
    /// </summary>
    public static string ResolveMacAddress(string ipAddress, string sourceIp = "")
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ResolveWindows(ipAddress, sourceIp);
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ResolveLinux(ipAddress);
        }

        return "Unknown";
    }

    /// <summary>
    /// Reads the full ARP table from the OS to discover ALL devices that have
    /// recently communicated on the network — including WiFi devices and phones
    /// that don't respond to ICMP ping.
    /// </summary>
    public static List<(string Ip, string Mac)> GetFullArpTable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsArpTable();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxArpTable();
        }

        return new List<(string, string)>();
    }

    /// <summary>
    /// Attempts to resolve a device's NetBIOS name (Windows only).
    /// Works for Windows PCs, printers, and NAS devices on the LAN.
    /// </summary>
    public static string TryResolveNetBiosName(string ipAddress)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return string.Empty;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nbtstat",
                Arguments = $"-A {ipAddress}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return string.Empty;

            // Give nbtstat 3 seconds max
            if (!proc.WaitForExit(3000))
            {
                try { proc.Kill(); } catch { }
                return string.Empty;
            }

            string output = proc.StandardOutput.ReadToEnd();
            
            // Parse nbtstat output for the <00> UNIQUE entry (device name)
            foreach (var line in output.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Contains("<00>") && trimmed.Contains("UNIQUE"))
                {
                    // Line format: "DESKTOP-ABC   <00>  UNIQUE  ..."
                    string name = trimmed.Split('<')[0].Trim();
                    if (!string.IsNullOrEmpty(name) && name != "")
                        return name;
                }
            }
        }
        catch { }

        return string.Empty;
    }

    // ── Windows: Single IP resolution via SendARP ──

    private static string ResolveWindows(string ipAddress, string sourceIp = "")
    {
        try
        {
            IPAddress parsedIp = IPAddress.Parse(ipAddress);
            int srcIpInt = 0;
            if (!string.IsNullOrEmpty(sourceIp) && IPAddress.TryParse(sourceIp, out var sIp))
            {
#pragma warning disable CS0618
                srcIpInt = (int)sIp.Address;
#pragma warning restore CS0618
            }

            byte[] macAddr = new byte[6];
            uint macAddrLen = (uint)macAddr.Length;
            
            // Note: BitConverter is used for legacy compat. IPv4 only for ARP.
#pragma warning disable CS0618 // Type or member is obsolete
            int result = SendARP((int)parsedIp.Address, srcIpInt, macAddr, ref macAddrLen);
#pragma warning restore CS0618 

            if (result != 0) return "Unknown";

            return string.Join(":", macAddr.Select(b => b.ToString("X2")));
        }
        catch
        {
            return "Unknown";
        }
    }

    // ── Linux: Single IP resolution from /proc/net/arp ──

    private static string ResolveLinux(string ipAddress)
    {
        try
        {
            string arpTable = System.IO.File.ReadAllText("/proc/net/arp");
            
            string[] lines = arpTable.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines.Skip(1)) // Skip header
            {
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4 && parts[0] == ipAddress)
                {
                    return parts[3].ToUpper().Replace("-", ":");
                }
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // ── Windows: Full ARP table via `arp -a` ──

    private static List<(string Ip, string Mac)> GetWindowsArpTable()
    {
        var results = new List<(string, string)>();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "arp",
                Arguments = "-a",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return results;
            
            // Read output with timeout protection
            var outputTask = proc.StandardOutput.ReadToEndAsync();
            if (proc.WaitForExit(5000))
            {
                string output = outputTask.Result;
                // Parse lines like: "  192.168.1.100    aa-bb-cc-dd-ee-ff     dynamic"
                foreach (var line in output.Split('\n'))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        string ip = parts[0];
                        string mac = parts[1];
                        string type = parts[2].ToLower();

                        // Validate IP format and skip broadcast/multicast
                        if (!IPAddress.TryParse(ip, out var addr)) continue;
                        if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                        
                        // Skip invalid MACs
                        if (mac.Length < 11) continue; // "aa-bb-cc-dd-ee-ff" = 17 chars
                        if (mac == "ff-ff-ff-ff-ff-ff") continue; // Broadcast
                        if (mac.StartsWith("01-00-5e")) continue; // Multicast
                        if (type == "static" && mac == "ff-ff-ff-ff-ff-ff") continue;

                        // Normalize MAC format: aa-bb-cc → AA:BB:CC
                        string normalizedMac = mac.Replace("-", ":").ToUpper();
                        results.Add((ip, normalizedMac));
                    }
                }
            }
        }
        catch { }

        return results;
    }

    // ── Linux: Full ARP table from /proc/net/arp ──

    private static List<(string Ip, string Mac)> GetLinuxArpTable()
    {
        var results = new List<(string, string)>();

        try
        {
            string arpTable = System.IO.File.ReadAllText("/proc/net/arp");
            string[] lines = arpTable.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines.Skip(1)) // Skip header
            {
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    string ip = parts[0];
                    string mac = parts[3].ToUpper().Replace("-", ":");

                    if (mac == "00:00:00:00:00:00") continue;
                    if (!IPAddress.TryParse(ip, out _)) continue;

                    results.Add((ip, mac));
                }
            }
        }
        catch { }

        return results;
    }
}
