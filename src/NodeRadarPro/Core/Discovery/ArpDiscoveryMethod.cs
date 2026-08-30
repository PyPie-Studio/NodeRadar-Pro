using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Discovery;

public class ArpDiscoveryMethod : IDiscoveryMethod
{
    public string Name => "ARP / MAC Resolution";

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

    private static readonly Dictionary<string, string> BasicOuiDictionary = new(StringComparer.OrdinalIgnoreCase)
    {
        // Apple
        { "00:1C:B3", "Apple" },
        { "00:1F:5B", "Apple" },
        { "00:25:00", "Apple" },
        { "00:25:4B", "Apple" },
        { "00:26:BB", "Apple" },
        { "04:0C:CE", "Apple" },
        { "04:15:F6", "Apple" },
        { "04:26:65", "Apple" },
        { "04:52:F3", "Apple" },
        // Huawei
        { "00:18:82", "Huawei" },
        { "00:1E:10", "Huawei" },
        { "00:22:A1", "Huawei" },
        { "00:25:68", "Huawei" },
        { "00:46:4B", "Huawei" },
        { "20:0B:C7", "Huawei" },
        { "20:2B:20", "Huawei" },
        { "24:69:A5", "Huawei" },
        { "30:87:30", "Huawei" },
        { "40:4D:7F", "Huawei" },
        { "4C:F2:BF", "Huawei" },
        { "AC:E2:15", "Huawei" },
        // Xiaomi
        { "00:EC:0A", "Xiaomi" },
        { "18:59:36", "Xiaomi" },
        { "28:6C:07", "Xiaomi" },
        { "34:80:B3", "Xiaomi" },
        { "3C:BD:3E", "Xiaomi" },
        { "50:EC:50", "Xiaomi" },
        { "64:09:80", "Xiaomi" },
        { "7C:1D:D9", "Xiaomi" },
        { "8C:BE:BE", "Xiaomi" },
        { "98:FA:E3", "Xiaomi" },
        { "AC:F1:DF", "Xiaomi" }
    };

    public async Task DiscoverAsync(string baseIp, List<IPAddress> targetIps, Action<NetworkDevice> onDeviceDiscovered, CancellationToken ct)
    {
        // Limit concurrency to avoid overloading the network stack
        using var semaphore = new SemaphoreSlim(16);

        var tasks = targetIps.Select(async ip =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                if (ct.IsCancellationRequested) return;

                string mac = await Task.Run(() => ResolveMac(ip), ct);
                if (mac != "Unknown")
                {
                    string vendor = GetMacVendor(mac);
                    var device = new NetworkDevice
                    {
                        IpAddress = ip.ToString(),
                        MacAddress = mac,
                        Vendor = vendor,
                        IsOnline = true,
                        SourceProtocol = Name,
                        RawDetails = $"ARP Response: MAC successfully resolved to {mac}."
                    };

                    Logger.Log(LogLevel.Info, Name, $"Resolved IP {ip} -> MAC {mac} ({vendor})");
                    onDeviceDiscovered(device);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Info, Name, $"Error resolving {ip}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    public static string GetMacVendor(string mac)
    {
        if (string.IsNullOrEmpty(mac) || mac == "Unknown") return "Unknown Vendor";
        var normalized = mac.Replace("-", ":").ToUpperInvariant();

        // Check for Locally Administered Address (LAA) aka Privacy/Randomized MAC
        // under IEEE 802, if the second hex digit of the first octet is 2, 6, A, or E
        if (normalized.Length >= 2)
        {
            char secondChar = normalized[1];
            if (secondChar == '2' || secondChar == '6' || secondChar == 'A' || secondChar == 'E')
            {
                return "Privacy MAC";
            }
        }

        if (normalized.Length >= 8)
        {
            string oui = normalized.Substring(0, 8);
            if (BasicOuiDictionary.TryGetValue(oui, out string? vendor))
            {
                return vendor;
            }

            string fullLookup = Data.VendorLookup.GetVendor(mac);
            if (fullLookup != "Unknown Vendor" && !string.IsNullOrEmpty(fullLookup))
            {
                return fullLookup;
            }
        }

        return "Unknown Vendor";
    }

    private string ResolveMac(IPAddress ip)
    {
        try
        {
            byte[] macAddr = new byte[6];
            uint macAddrLen = (uint)macAddr.Length;

#pragma warning disable CS0618
            int destIp = (int)ip.Address;
#pragma warning restore CS0618

            int result = SendARP(destIp, 0, macAddr, ref macAddrLen);
            if (result == 0)
            {
                return ArpResolver.FormatMacAddress(macAddr);
            }
        }
        catch { }
        return "Unknown";
    }
}
