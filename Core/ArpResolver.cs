using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

/// <summary>
/// Handles cross-platform MAC address resolution (ARP).
/// Supports Windows natively and Linux by reading /proc/net/arp.
/// </summary>
public static class ArpResolver
{
    // Native Windows API for ARP resolution
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

    /// <summary>
    /// Attempts to resolve the MAC address for a given IP address.
    /// </summary>
    public static string ResolveMacAddress(string ipAddress)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ResolveWindows(ipAddress);
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ResolveLinux(ipAddress);
        }

        return "Unknown";
    }

    private static string ResolveWindows(string ipAddress)
    {
        try
        {
            IPAddress parsedIp = IPAddress.Parse(ipAddress);
            byte[] macAddr = new byte[6];
            uint macAddrLen = (uint)macAddr.Length;
            
            // Note: BitConverter is used for legacy compat. IPv4 only for ARP.
#pragma warning disable CS0618 // Type or member is obsolete
            int result = SendARP((int)parsedIp.Address, 0, macAddr, ref macAddrLen);
#pragma warning restore CS0618 

            if (result != 0) return "Unknown";

            return string.Join(":", macAddr.Select(b => b.ToString("X2")));
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string ResolveLinux(string ipAddress)
    {
        try
        {
            // Linux stores ARP tables in /proc/net/arp
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
}
