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
    private string _localBindingIp = "";

    private void ResolveBindingIp()
    {
        _localBindingIp = "";
        if (string.IsNullOrEmpty(PreferredInterfaceName)) return;

        try
        {
            var ni = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.Name.Contains(PreferredInterfaceName, StringComparison.OrdinalIgnoreCase));
            
            if (ni != null)
            {
                var addr = ni.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                if (addr != null) _localBindingIp = addr.Address.ToString();
            }
        }
        catch { }
    }

    public async Task<List<NetworkNode>> ScanRangeAsync(string baseIp, int startIp, int endIp, CancellationToken token = default)
    {
        await DeviceFingerprinter.StartDiscoverySweepAsync();
        ResolveBindingIp();
        var activeNodes = new ConcurrentBag<NetworkNode>();
        var discoveredMacs = new ConcurrentDictionary<string, bool>();

        int totalIps = endIp - startIp + 1;
        int completed = 0;
        using var semaphore = new SemaphoreSlim(32);

        var tasks = Enumerable.Range(startIp, totalIps).Select(async i =>
        {
            if (token.IsCancellationRequested) return;

            await semaphore.WaitAsync(token);
            try
            {
                string ip = $"{baseIp}.{i}";
                var node = await ProbeAndResolveAsync(ip, token);

                if (node != null && !token.IsCancellationRequested)
                {
                    discoveredMacs.TryAdd(node.MacAddress, true);
                    activeNodes.Add(node);
                    NodeDiscovered?.Invoke(node);
                }
            }
            finally
            {
                semaphore.Release();
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
        await DeviceFingerprinter.StartDiscoverySweepAsync();
        ResolveBindingIp();
        var activeNodes = new ConcurrentBag<NetworkNode>();
        var discoveredMacs = new ConcurrentDictionary<string, bool>();

        // Pre-Sweep: Rapid ARP check for silent devices (Issue 8)
        try
        {
            var arpTable = ArpResolver.GetFullArpTable();
            foreach (var (ip, mac) in arpTable)
            {
                if (token.IsCancellationRequested) break;
                if (ip == "127.0.0.1" || mac == "00:00:00:00:00:00") continue;
                
                string[] parts = ip.Split('.');
                if (parts.Length == 4 && $"{parts[0]}.{parts[1]}.{parts[2]}" == baseIp)
                {
                    if (discoveredMacs.TryAdd(mac, true))
                    {
                        var node = BuildNode(ip, mac);
                        await ResolveNodeMetadataAsync(node, token);
                        activeNodes.Add(node);
                        NodeDiscovered?.Invoke(node);
                    }
                }
            }
        }
        catch { }

        var allSubnets = GetAllLocalBaseIps();
        if (!allSubnets.Contains(baseIp))
            allSubnets.Insert(0, baseIp);

        int totalIps = allSubnets.Count * 254;
        int completed = 0;
        using var semaphore = new SemaphoreSlim(32);

        foreach (var subnet in allSubnets)
        {
            if (token.IsCancellationRequested) break;

            var tasks = Enumerable.Range(1, 254).Select(async i =>
            {
                if (token.IsCancellationRequested) return;

                await semaphore.WaitAsync(token);
                try
                {
                    string ip = $"{subnet}.{i}";
                    var node = await ProbeAndResolveAsync(ip, token);

                    if (node != null && !token.IsCancellationRequested)
                    {
                        discoveredMacs.TryAdd(node.MacAddress, true);
                        activeNodes.Add(node);
                        NodeDiscovered?.Invoke(node);
                    }
                }
                finally
                {
                    semaphore.Release();
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
            mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip, _localBindingIp));
            
            // Fallback: If direct SendARP failed, check the full system ARP table
            if (mac == "Unknown")
            {
                var table = ArpResolver.GetFullArpTable();
                var match = table.FirstOrDefault(x => x.Ip == ip);
                if (!string.IsNullOrEmpty(match.Mac))
                {
                    mac = match.Mac;
                }
            }

            if (mac != "Unknown") isReachable = true;
        }
        catch { }

        if (!isReachable) return null;

        // Final fallback: Use IP-based ID but flag it properly (Issue: prevents database link loss)
        if (mac == "Unknown") 
        {
            // We check the database to see if we have a device that HAD this IP recently
            // This allows us to maintain identity even if ARP is transiently failing.
            var registered = Data.LocalDatabase.Instance.GetRegisteredDevices();
            var existing = registered.FirstOrDefault(d => d.IpAddress == ip);
            if (existing != null)
            {
                mac = existing.MacAddress;
            }
            else
            {
                mac = "L3-ROUTED-" + ip;
            }
        }

        var node = new NetworkNode
        {
            IpAddress = ip,
            MacAddress = mac,
            IsOnline = true,
            PingLatencyMs = latency
        };

        // Fix: Use a per-node resolution timeout (5s) to prevent hangs at 82%
        using var resolveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        resolveCts.CancelAfter(5000);

        try
        {
            await ResolveNodeMetadataAsync(node, resolveCts.Token);
        }
        catch (OperationCanceledException) { }
        catch { }

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
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(3000);
                
                string netbios = await Task.Run(() => ArpResolver.TryResolveNetBiosName(node.IpAddress), cts.Token);
                if (!string.IsNullOrEmpty(netbios))
                    node.Hostname = netbios;
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
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

// ── Deep Intelligence: Deep HTTP Metadata Probes ──
        if (!token.IsCancellationRequested)
        {
            try
            {
                string deepMetadata = await DeviceFingerprinter.ProbeHttpMetadataAsync(node.IpAddress);
                if (!string.IsNullOrEmpty(deepMetadata))
                {
                    if (string.IsNullOrEmpty(node.ExactModel)) node.ExactModel = deepMetadata;
                    else if (!node.ExactModel.Contains(deepMetadata)) node.ExactModel = $"{deepMetadata} ({node.ExactModel})";
                }
            }
            catch { }
        }

        // ── Deep Intelligence: Protocol Discovery (mDNS / SSDP) ──
        if (!token.IsCancellationRequested)
        {
            try
            {
                string mdnsModel = await DeviceFingerprinter.DiscoverExactModelViaMDnsAsync(node.IpAddress);
                if (!string.IsNullOrEmpty(mdnsModel))
                {
                    node.ExactModel = mdnsModel;
                    Logger.Log(LogLevel.Info, "SubnetScanner", $"mDNS identification success for {node.IpAddress}: {mdnsModel}");
                }

                string ssdpModel = await DeviceFingerprinter.DiscoverExactModelViaSSDPAsync(node.IpAddress);
                if (!string.IsNullOrEmpty(ssdpModel))
                {
                    Logger.Log(LogLevel.Info, "SubnetScanner", $"SSDP identification success for {node.IpAddress}: {ssdpModel}");
                    if (string.IsNullOrEmpty(node.ExactModel)) node.ExactModel = ssdpModel;
                    else node.ExactModel = $"{node.ExactModel} ({ssdpModel})";
                }
            }
            catch { }
        }

        // Inline port scanning (if enabled)
        if (EnableInlinePortScan && !token.IsCancellationRequested)
        {
            try
            {
                var scanResults = await PortScanner.ScanPortsAsync(node.IpAddress, FastScanMode, Math.Min(TimeoutMs, 500), token);
                node.OpenPorts = scanResults.Keys.OrderBy(p => p).ToList();
                node.PortBanners = scanResults;

                // If we found banners, append them to exact model if useful
                foreach (var banner in node.PortBanners.Values.Where(b => !string.IsNullOrEmpty(b)))
                {
                    if (string.IsNullOrEmpty(node.ExactModel)) node.ExactModel = banner;
                    else if (!node.ExactModel.Contains(banner)) node.ExactModel += $" | {banner}";
                }
            }
            catch { }
        }

        // ── Deep Intelligence: Intelligent Classification ──
        DeviceClassifier.ResolveDetails(node);

        // ── Deep Intelligence: Vulnerability Scoring ──
        VulnerabilityEngine.UpdateThreatLevel(node);

        // ── UI Synchronization: Map discovery data to registration fields ──
        if (string.IsNullOrEmpty(node.DeviceName) && node.Hostname != "Unknown Device")
            node.DeviceName = node.Hostname;
        
        if (string.IsNullOrEmpty(node.DeviceModel) && !string.IsNullOrEmpty(node.ExactModel))
            node.DeviceModel = node.ExactModel;
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
            
            // Fallback: Check full ARP table
            if (mac == "Unknown")
            {
                var table = ArpResolver.GetFullArpTable();
                var match = table.FirstOrDefault(x => x.Ip == ip);
                if (!string.IsNullOrEmpty(match.Mac)) mac = match.Mac;
            }
            
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
                {
                    mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));
                    if (mac == "Unknown")
                    {
                        var table = ArpResolver.GetFullArpTable();
                        var match = table.FirstOrDefault(x => x.Ip == ip);
                        if (!string.IsNullOrEmpty(match.Mac)) mac = match.Mac;
                    }
                }
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
                    using var cts = new CancellationTokenSource(400);
                    var connectTask = tcp.ConnectAsync(ip, port, cts.Token);
                    try
                    {
                        await connectTask;
                        if (tcp.Connected)
                        {
                            isOnline = true;
                            if (mac == "Unknown")
                            {
                                mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));
                                if (mac == "Unknown")
                                {
                                    var table = ArpResolver.GetFullArpTable();
                                    var match = table.FirstOrDefault(x => x.Ip == ip);
                                    if (!string.IsNullOrEmpty(match.Mac)) mac = match.Mac;
                                }
                            }
                            break;
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                }
                catch { }
            }
        }

        // Database correlation fallback for background monitor
        if (isOnline && mac == "Unknown")
        {
            var registered = Data.LocalDatabase.Instance.GetRegisteredDevices();
            var existing = registered.FirstOrDefault(d => d.IpAddress == ip);
            if (existing != null) mac = existing.MacAddress;
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
        using var cts = new CancellationTokenSource(2000);
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(ip, cts.Token);
            if (hostEntry.HostName != ip)
                return hostEntry.HostName;
        }
        catch (OperationCanceledException) { }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound) { }
        catch (Exception) { }

        return "Unknown Device";
    }
}