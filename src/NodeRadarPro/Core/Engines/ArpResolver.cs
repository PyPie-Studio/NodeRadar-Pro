using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public delegate int SendArpDelegate(int destIp, int srcIp, byte[] pMacAddr, ref uint phyAddrLen);
    // Native Windows API for ARP resolution
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetIpNetTable(IntPtr pIpNetTable, ref int pdwSize, bool bOrder);

    /// <summary>
    /// Attempts to resolve the MAC address for a given IP address.
    /// </summary>
    public static string ResolveMacAddress(string ipAddress, string sourceIp = "", SendArpDelegate? sendArp = null)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || sendArp != null)
        {
            return ResolveWindows(ipAddress, sourceIp, sendArp);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ResolveLinux(ipAddress);
        }

        return "Unknown";
    }

    // ── Cache for GetFullArpTable ──
    private static List<(string Ip, string Mac)> _cachedArpTable = new();
    private static Dictionary<string, string> _cachedArpDictionary = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _lastArpTableUpdate = DateTime.MinValue;
    private static readonly object _arpTableLock = new();

    /// <summary>
    /// Reads the full ARP table from the OS to discover ALL devices that have
    /// recently communicated on the network — including WiFi devices and phones
    /// that don't respond to ICMP ping.
    /// </summary>
    public static List<(string Ip, string Mac)> GetFullArpTable()
    {
        UpdateCacheIfNeeded();
        lock (_arpTableLock)
        {
            return new List<(string Ip, string Mac)>(_cachedArpTable);
        }
    }

    /// <summary>
    /// Gets the full ARP table as a dictionary mapping IP address to MAC address for O(1) lookups.
    /// </summary>
    public static Dictionary<string, string> GetFullArpTableAsDictionary()
    {
        UpdateCacheIfNeeded();
        lock (_arpTableLock)
        {
            return new Dictionary<string, string>(_cachedArpDictionary, StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void UpdateCacheIfNeeded()
    {
        lock (_arpTableLock)
        {
            if ((DateTime.UtcNow - _lastArpTableUpdate).TotalSeconds < 5)
            {
                return;
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

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in results)
        {
            dict[item.Ip] = item.Mac;
        }

        lock (_arpTableLock)
        {
            _cachedArpTable = results;
            _cachedArpDictionary = dict;
            _lastArpTableUpdate = DateTime.UtcNow;
        }
    }

    // ── Windows: Single IP resolution via SendARP API ──

    public static string FormatMacAddress(ReadOnlySpan<byte> macAddr)
    {
        if (macAddr.Length < 6) return "Unknown";
        return string.Create(17, (macAddr[0], macAddr[1], macAddr[2], macAddr[3], macAddr[4], macAddr[5]), (span, bytes) =>
        {
            bytes.Item1.TryFormat(span.Slice(0, 2), out _, "X2");
            span[2] = ':';
            bytes.Item2.TryFormat(span.Slice(3, 2), out _, "X2");
            span[5] = ':';
            bytes.Item3.TryFormat(span.Slice(6, 2), out _, "X2");
            span[8] = ':';
            bytes.Item4.TryFormat(span.Slice(9, 2), out _, "X2");
            span[11] = ':';
            bytes.Item5.TryFormat(span.Slice(12, 2), out _, "X2");
            span[14] = ':';
            bytes.Item6.TryFormat(span.Slice(15, 2), out _, "X2");
        });
    }

    private static string ResolveWindows(string ipAddress, string sourceIp, SendArpDelegate? sendArp)
    {
        try
        {
            IPAddress parsedIp = IPAddress.Parse(ipAddress);
            IPAddress srcIp = string.IsNullOrEmpty(sourceIp) ? IPAddress.Any : IPAddress.Parse(sourceIp);

            byte[] macAddr = new byte[6];
            uint macAddrLen = (uint)macAddr.Length;

#pragma warning disable CS0618 
            int srcIpInt = (int)srcIp.Address;
            int result = sendArp != null
                ? sendArp((int)parsedIp.Address, srcIpInt, macAddr, ref macAddrLen)
                : SendARP((int)parsedIp.Address, srcIpInt, macAddr, ref macAddrLen);
#pragma warning restore CS0618 

            if (result != 0) return "Unknown";

            return FormatMacAddress(macAddr);
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Attempts to resolve the NetBIOS name of a device via UDP port 137 (Professional fallback).
    /// </summary>
    public static string TryResolveNetBiosName(string ipAddress, int port = 137)
    {
        try
        {
            using var udp = new UdpClient();
            udp.Client.ReceiveTimeout = 1500;
            udp.Connect(ipAddress, port);

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
            using var reader = new System.IO.StreamReader("/proc/net/arp");
            string? line = reader.ReadLine(); // Skip header
            if (line == null) return "Unknown";

            while ((line = reader.ReadLine()) != null)
            {
                if (TryParseProcNetArpLine(line, out ReadOnlySpan<char> ipSpan, out ReadOnlySpan<char> macSpan))
                {
                    if (ipSpan.Equals(ipAddress, StringComparison.Ordinal))
                    {
                        return FormatMacSpan(macSpan);
                    }
                }
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // ── Windows: Full ARP table via native Win32 P/Invoke with CLI fallback ──

    private static List<(string Ip, string Mac)> GetWindowsArpTable()
    {
        try
        {
            return GetWindowsArpTableNative();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "ArpResolver", $"Exception in GetWindowsArpTable: {ex.Message}");
            return new List<(string, string)>();
        }
    }

    private static List<(string Ip, string Mac)> GetWindowsArpTableNative()
    {
        var results = new List<(string, string)>();
        int bytesNeeded = 0;
        int result = GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);
        // 122 = ERROR_INSUFFICIENT_BUFFER
        if (result != 122 && result != 0) return results;

        IntPtr buffer = Marshal.AllocHGlobal(bytesNeeded);
        try
        {
            result = GetIpNetTable(buffer, ref bytesNeeded, false);
            if (result == 0)
            {
                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr currentPtr = IntPtr.Add(buffer, 4);
                // MIB_IPNETROW is 24 bytes (4 byte dwIndex, 4 byte dwPhysAddrLen, 8 byte bPhysAddr, 4 byte dwAddr, 4 byte dwType)
                for (int i = 0; i < numEntries; i++)
                {
                    int physAddrLen = Marshal.ReadInt32(currentPtr, 4);
                    int ipAddrInt = Marshal.ReadInt32(currentPtr, 16);
                    int type = Marshal.ReadInt32(currentPtr, 20);

                    // Skip invalid entries (type 2)
                    if (type != 2 && physAddrLen >= 6)
                    {
                        byte[] macBytes = new byte[physAddrLen];
                        Marshal.Copy(IntPtr.Add(currentPtr, 8), macBytes, 0, Math.Min(physAddrLen, 6));

                        var ipAddr = new IPAddress((uint)ipAddrInt);
                        string ipStr = ipAddr.ToString();
                        string macStr = string.Join(":", macBytes.Take(6).Select(b => b.ToString("X2")));

                        if (macStr != "00:00:00:00:00:00" && macStr != "FF:FF:FF:FF:FF:FF" &&
                            !macStr.StartsWith("01:00:5E", StringComparison.OrdinalIgnoreCase) &&
                            !ipStr.StartsWith("127.") && !ipStr.StartsWith("224.") && !ipStr.StartsWith("239."))
                        {
                            results.Add((ipStr, macStr));
                        }
                    }

                    currentPtr = IntPtr.Add(currentPtr, 24);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return results;
    }

    // ── Linux: Full ARP table from /proc/net/arp ──

    private static List<(string Ip, string Mac)> GetLinuxArpTable()
    {
        var results = new List<(string, string)>();

        try
        {
            using var reader = new System.IO.StreamReader("/proc/net/arp");
            string? line = reader.ReadLine(); // Skip header
            if (line == null) return results;

            while ((line = reader.ReadLine()) != null)
            {
                if (TryParseProcNetArpLine(line, out ReadOnlySpan<char> ipSpan, out ReadOnlySpan<char> macSpan))
                {
                    if (macSpan.Equals("00:00:00:00:00:00", StringComparison.Ordinal)) continue;
                    if (!IPAddress.TryParse(ipSpan, out _)) continue;

                    string ip = ipSpan.ToString();
                    string mac = FormatMacSpan(macSpan);

                    results.Add((ip, mac));
                }
            }
        }
        catch { }

        return results;
    }

    internal static bool TryParseProcNetArpLine(string line, out ReadOnlySpan<char> ipSpan, out ReadOnlySpan<char> macSpan)
    {
        ipSpan = default;
        macSpan = default;

        ReadOnlySpan<char> span = line.AsSpan().Trim();
        if (span.IsEmpty) return false;

        // Part 0: IP address
        int ipEnd = span.IndexOf(' ');
        if (ipEnd < 0) return false;
        ipSpan = span.Slice(0, ipEnd);

        // Part 1: HW type
        span = span.Slice(ipEnd).TrimStart();
        int p1End = span.IndexOf(' ');
        if (p1End < 0) return false;

        // Part 2: Flags
        span = span.Slice(p1End).TrimStart();
        int p2End = span.IndexOf(' ');
        if (p2End < 0) return false;

        // Part 3: HW address
        span = span.Slice(p2End).TrimStart();
        int macEnd = span.IndexOf(' ');
        macSpan = macEnd < 0 ? span : span.Slice(0, macEnd);

        return !macSpan.IsEmpty;
    }

    internal static string FormatMacSpan(ReadOnlySpan<char> macSpan)
    {
        return string.Create(macSpan.Length, macSpan, (span, state) =>
        {
            for (int i = 0; i < state.Length; i++)
            {
                char c = state[i];
                if (c == '-') span[i] = ':';
                else span[i] = char.ToUpperInvariant(c);
            }
        });
    }
}
