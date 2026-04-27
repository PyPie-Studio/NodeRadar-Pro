using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

/// <summary>
/// Core network scanning engine using ARP-first discovery.
/// Supports configurable timeout, DNS toggle, inline port scanning, and OS detection.
/// </summary>
public class SubnetScanner
{
    public event Action<NetworkNode>? NodeDiscovered;
    public event Action<double>? ProgressUpdated;

    // ── Configurable properties ──
    public int TimeoutMs { get; set; } = 1500;
    public bool EnableDnsResolve { get; set; } = true;
    public bool EnableInlinePortScan { get; set; } = false;
    public bool FastScanMode { get; set; } = false;
    public bool EnableOsDetection { get; set; } = true;
    public string PreferredInterfaceName { get; set; } = ""; // S3: Interface preference

    public async Task<List<NetworkNode>> ScanRangeAsync(string baseIp, int startIp, int endIp, CancellationToken token = default)
    {
        var activeNodes = new ConcurrentBag<NetworkNode>();
        var discoveredMacs = new ConcurrentDictionary<string, bool>();

        int totalIps = endIp - startIp + 1;
        int completed = 0;

        var tasks = Enumerable.Range(startIp, totalIps).Select(async i =>
        {
            if (token.IsCancellationRequested) return;

            string ip = $"{baseIp}.{i}";
            var node = await ProbeAndResolveAsync(ip, token);

            if (node != null && !token.IsCancellationRequested)
            {
                discoveredMacs.TryAdd(node.MacAddress, true);
                activeNodes.Add(node);
                NodeDiscovered?.Invoke(node);
            }

            if (!token.IsCancellationRequested)
            {
                int current = Interlocked.Increment(ref completed);
                double pct = Math.Min(85.0, ((double)current / totalIps) * 85.0);
                if (current % 8 == 0 || current == totalIps)
                    ProgressUpdated?.Invoke(pct);
            }
        });

        await Task.WhenAll(tasks);

        if (token.IsCancellationRequested) return activeNodes.ToList();

        // ARP table sweep for missed devices
        if (!token.IsCancellationRequested)
        {
            try
            {
                var arpTable = ArpResolver.GetFullArpTable();
                foreach (var (ip, mac) in arpTable)
                {
                    if (token.IsCancellationRequested) break;
                    if (discoveredMacs.ContainsKey(mac)) continue;

                    string[] parts = ip.Split('.');
                    if (parts.Length != 4) continue;
                    string ipSubnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
                    if (ipSubnet != baseIp) continue;
                    if (!int.TryParse(parts[3], out int lastOctet) || lastOctet < startIp || lastOctet > endIp) continue;

                    var node = BuildNode(ip, mac);
                    await ResolveNodeMetadataAsync(node, token);
                    if (!token.IsCancellationRequested)
                    {
                        activeNodes.Add(node);
                        NodeDiscovered?.Invoke(node);
                    }
                }
            }
            catch { }
        }

        ProgressUpdated?.Invoke(100.0);
        return activeNodes.ToList();
    }

    public async Task<List<NetworkNode>> ScanSubnetAsync(string baseIp, CancellationToken token = default)
    {
        var activeNodes = new ConcurrentBag<NetworkNode>();
        var discoveredMacs = new ConcurrentDictionary<string, bool>();

        var allSubnets = GetAllLocalBaseIps();
        if (!allSubnets.Contains(baseIp))
            allSubnets.Insert(0, baseIp);

        int totalIps = allSubnets.Count * 254;
        int completed = 0;

        foreach (var subnet in allSubnets)
        {
            if (token.IsCancellationRequested) break;

            var tasks = Enumerable.Range(1, 254).Select(async i =>
            {
                if (token.IsCancellationRequested) return;

                string ip = $"{subnet}.{i}";
                var node = await ProbeAndResolveAsync(ip, token);

                if (node != null && !token.IsCancellationRequested)
                {
                    discoveredMacs.TryAdd(node.MacAddress, true);
                    activeNodes.Add(node);
                    NodeDiscovered?.Invoke(node);
                }

                if (!token.IsCancellationRequested)
                {
                    int current = Interlocked.Increment(ref completed);
                    double pct = Math.Min(85.0, ((double)current / totalIps) * 85.0);
                    if (current % 8 == 0 || current == totalIps)
                        ProgressUpdated?.Invoke(pct);
                }
            });

            await Task.WhenAll(tasks);
        }

        if (token.IsCancellationRequested) return activeNodes.ToList();

        if (!token.IsCancellationRequested)
        {
            ProgressUpdated?.Invoke(88.0);

            try
            {
                var arpTable = ArpResolver.GetFullArpTable();
                foreach (var (ip, mac) in arpTable)
                {
                    if (token.IsCancellationRequested) break;
                    if (discoveredMacs.ContainsKey(mac)) continue;

                    string[] parts = ip.Split('.');
                    if (parts.Length != 4) continue;
                    string ipSubnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
                    if (!allSubnets.Contains(ipSubnet)) continue;

                    var node = BuildNode(ip, mac);
                    await ResolveNodeMetadataAsync(node, token);
                    if (!token.IsCancellationRequested)
                    {
                        activeNodes.Add(node);
                        NodeDiscovered?.Invoke(node);
                    }
                }
            }
            catch { }
        }

        if (!token.IsCancellationRequested)
            ProgressUpdated?.Invoke(100.0);

        return activeNodes.ToList();
    }

    private async Task<NetworkNode?> ProbeAndResolveAsync(string ip, CancellationToken token)
    {
        if (token.IsCancellationRequested) return null;

        bool isReachable = false;
        long latency = -1;
        string mac = "Unknown";
        int pingTimeout = Math.Min(TimeoutMs, 2000);

        try
        {
            using var pinger = new Ping();
            var reply = await pinger.SendPingAsync(ip, pingTimeout);
            if (reply.Status == IPStatus.Success)
            {
                isReachable = true;
                latency = reply.RoundtripTime;
            }
        }
        catch { }

        try
        {
            mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));
            if (mac != "Unknown") isReachable = true;
        }
        catch { }

        if (!isReachable) return null;

        if (mac == "Unknown") mac = "L3-ROUTED-" + ip;

        var node = new NetworkNode
        {
            IpAddress = ip,
            MacAddress = mac,
            IsOnline = true,
            PingLatencyMs = latency
        };

        await ResolveNodeMetadataAsync(node, token);

        return node;
    }

    private async Task ResolveNodeMetadataAsync(NetworkNode node, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        // Vendor lookup (instant)
        node.Vendor = Data.VendorLookup.GetVendor(node.MacAddress);

        // DNS hostname (only if enabled)
        if (EnableDnsResolve)
            node.Hostname = await TryGetHostnameAsync(node.IpAddress);
        else
            node.Hostname = "Unknown Device";

        // NetBIOS fallback
        if (node.Hostname == "Unknown Device" && EnableDnsResolve && !token.IsCancellationRequested)
        {
            try
            {
                var nbTask = Task.Run(() => ArpResolver.TryResolveNetBiosName(node.IpAddress), token);
                if (await Task.WhenAny(nbTask, Task.Delay(3000, token)) == nbTask)
                {
                    string netbios = await nbTask;
                    if (!string.IsNullOrEmpty(netbios))
                        node.Hostname = netbios;
                }
            }
            catch { }
        }

        // HTTP banner fallback
        if (node.Hostname == "Unknown Device" && !token.IsCancellationRequested)
        {
            try
            {
                string banner = await DeviceFingerprinter.TryGetHttpServerBannerAsync(node.IpAddress);
                if (!string.IsNullOrEmpty(banner))
                    node.Hostname = banner;
            }
            catch { }
        }

        // Device type guessing
        node.DeviceType = Data.VendorLookup.GuessDeviceType(node.Vendor, node.Hostname);

        // Icon assignment
        if (!string.IsNullOrEmpty(node.DeviceType))
        {
            string t = node.DeviceType.ToLower();
            if (t.Contains("phone") || t.Contains("iphone")) node.IconPath = "phone";
            else if (t.Contains("tv")) node.IconPath = "tv";
            else if (t.Contains("router") || t.Contains("network") || t.Contains("access point")) node.IconPath = "router";
            else if (t.Contains("pc") || t.Contains("computer") || t.Contains("desktop")) node.IconPath = "pc";
            else if (t.Contains("laptop") || t.Contains("macbook")) node.IconPath = "laptop";
            else if (t.Contains("camera")) node.IconPath = "camera";
            else if (t.Contains("speaker")) node.IconPath = "speaker";
            else if (t.Contains("nas") || t.Contains("server")) node.IconPath = "server";
        }

        // Inline port scanning (if enabled)
        if (EnableInlinePortScan && !token.IsCancellationRequested)
        {
            try
            {
                node.OpenPorts = await PortScanner.ScanPortsAsync(node.IpAddress, FastScanMode, Math.Min(TimeoutMs, 500), token);
                if (EnableOsDetection && node.OpenPorts.Count > 0)
                    node.OsGuess = PortScanner.GuessOs(node.OpenPorts);
            }
            catch { }
        }
    }

    private static NetworkNode BuildNode(string ip, string mac) => new()
    {
        IpAddress = ip,
        MacAddress = mac,
        IsOnline = true,
        PingLatencyMs = -1
    };

    public static async Task<(bool IsOnline, string Mac, long LatencyMs)> QuickProbeAsync(string ip)
    {
        string mac = "Unknown";
        long latency = -1;
        bool isOnline = false;

        try
        {
            mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));
            if (mac != "Unknown") isOnline = true;
        }
        catch { }

        try
        {
            using var pinger = new Ping();
            var reply = await pinger.SendPingAsync(ip, 2000);
            if (reply.Status == IPStatus.Success)
            {
                isOnline = true;
                latency = reply.RoundtripTime;
                if (mac == "Unknown")
                    mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));
            }
        }
        catch { }

        if (!isOnline)
        {
            int[] ports = { 80, 443, 22, 53, 8080, 8443, 62078 };
            foreach (int port in ports)
            {
                try
                {
                    using var tcp = new TcpClient();
                    var connectTask = tcp.ConnectAsync(ip, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(400)) == connectTask && tcp.Connected)
                    {
                        isOnline = true;
                        if (mac == "Unknown")
                            mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));
                        break;
                    }
                }
                catch { }
            }
        }

        return (isOnline, mac, latency);
    }

    public static string GetLocalBaseIp()
    {
        var all = GetAllLocalBaseIps();
        return all.Count > 0 ? all[0] : "192.168.1";
    }

    public static List<string> GetAllLocalBaseIps(string preferredInterface = "")
    {
        var subnets = new List<string>();
        string? preferredSubnet = null;

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            foreach (var ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    string[] parts = ip.Address.ToString().Split('.');
                    if (parts.Length == 4)
                    {
                        string subnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
                        if (subnet.StartsWith("169.254")) continue;
                        if (!subnets.Contains(subnet))
                        {
                            subnets.Add(subnet);
                            // S3: Track preferred interface subnet
                            if (!string.IsNullOrEmpty(preferredInterface) &&
                                ni.Name.Contains(preferredInterface, StringComparison.OrdinalIgnoreCase))
                                preferredSubnet = subnet;
                        }
                    }
                }
            }
        }

        // S3: Move preferred interface's subnet to front
        if (preferredSubnet != null && subnets.Contains(preferredSubnet))
        {
            subnets.Remove(preferredSubnet);
            subnets.Insert(0, preferredSubnet);
        }

        return subnets;
    }

    private static async Task<string> TryGetHostnameAsync(string ip)
    {
        try
        {
            var dnsTask = Dns.GetHostEntryAsync(ip);
            if (await Task.WhenAny(dnsTask, Task.Delay(2000)) == dnsTask)
            {
                var hostEntry = await dnsTask;
                if (hostEntry.HostName != ip)
                    return hostEntry.HostName;
            }
        }
        catch { }
        return "Unknown Device";
    }
}