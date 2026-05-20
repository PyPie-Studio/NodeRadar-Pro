using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

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

    // ── Cache for GetFullArpTable ──
    private static List<(string Ip, string Mac)> _cachedArpTable = new();
    private static DateTime _lastArpTableUpdate = DateTime.MinValue;
    private static readonly object _arpTableLock = new();

    /// <summary>
    /// Reads the full ARP table from the OS to discover ALL devices that have
    /// recently communicated on the network — including WiFi devices and phones
    /// that don't respond to ICMP ping.
    /// </summary>
    public static List<(string Ip, string Mac)> GetFullArpTable()
    {
        lock (_arpTableLock)
        {
            if ((DateTime.UtcNow - _lastArpTableUpdate).TotalSeconds < 5)
            {
                return new List<(string Ip, string Mac)>(_cachedArpTable);
            }
        }

        List<(string Ip, string Mac)> results = new();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            results = GetWindowsArpTable();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            results = GetLinuxArpTable();
        }

        lock (_arpTableLock)
        {
            _cachedArpTable = results;
            _lastArpTableUpdate = DateTime.UtcNow;
        }

        return results;
    }

    // ── Windows: Single IP resolution via SendARP API ──

    private static string ResolveWindows(string ipAddress, string sourceIp)
    {
        try
        {
            IPAddress parsedIp = IPAddress.Parse(ipAddress);
            IPAddress srcIp = string.IsNullOrEmpty(sourceIp) ? IPAddress.Any : IPAddress.Parse(sourceIp);

            byte[] macAddr = new byte[6];
            uint macAddrLen = (uint)macAddr.Length;

#pragma warning disable CS0618 
            int srcIpInt = (int)srcIp.Address;
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

    /// <summary>
    /// Attempts to resolve the NetBIOS name of a device via UDP port 137 (Professional fallback).
    /// </summary>
    public static string TryResolveNetBiosName(string ipAddress)
    {
        try
        {
            using var udp = new UdpClient();
            udp.Client.ReceiveTimeout = 1500;
            udp.Connect(ipAddress, 137);

            // Standard NetBIOS Node Status Query packet
            byte[] query = {
                0x80, 0x94, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x20, 0x43, 0x4b, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x00, 0x00, 0x21,
                0x00, 0x01
            };

            udp.Send(query, query.Length);
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] response = udp.Receive(ref remoteEndPoint);

            if (response.Length > 56)
            {
                int numberOfNames = response[56];
                if (numberOfNames > 0)
                {
                    // The first name in the response is typically the machine name
                    string name = Encoding.ASCII.GetString(response, 57, 15).Trim();
                    return name;
                }
            }
        }
        catch { }
        return string.Empty;
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
                FileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "arp.exe"),
                Arguments = "-a",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) 
            {
                Logger.Log(LogLevel.Error, "ArpResolver", "Failed to start 'arp -a' process.");
                return results;
            }

            // Read output with timeout protection
            string output = proc.StandardOutput.ReadToEnd();
            if (proc.WaitForExit(5000))
            {
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
            else
            {
                Logger.Log(LogLevel.Warning, "ArpResolver", "The 'arp -a' process timed out after 5000ms.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "ArpResolver", $"Exception in GetWindowsArpTable: {ex.Message}");
        }

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
