using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core.Fingerprinting;

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

    private readonly IPingProvider _pingProvider;
    private readonly IArpResolver _arpResolver;

    public SubnetScanner(IPingProvider? pingProvider = null, IArpResolver? arpResolver = null)
    {
        _pingProvider = pingProvider ?? new DefaultPingProvider();
        _arpResolver = arpResolver ?? new DefaultArpResolver();
    }

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
                if (addr != null)
                {
                    _localBindingIp = addr.Address.ToString();
                    return;
                }
            }

            // Fallback: Bind to first active IPv4 address from any operational non-loopback interface
            var activeInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                     n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            if (activeInterface != null)
            {
                var addr = activeInterface.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                if (addr != null) _localBindingIp = addr.Address.ToString();
            }
        }
        catch { }
    }

    public async Task<List<NetworkNode>> ScanRangeAsync(string baseIp, int startIp, int endIp, CancellationToken token = default)
    {
        _ = DeepFingerprintEngine.Instance.StartDiscoverySweepAsync(token);
        ResolveBindingIp();
        var activeNodes = new ConcurrentBag<NetworkNode>();
        var registeredDevicesCache = Data.LocalDatabase.Instance.GetRegisteredDevices();
        var discoveredMacs = new ConcurrentDictionary<string, bool>();

        int totalIps = endIp - startIp + 1;
        if (totalIps <= 0) return new List<NetworkNode>();

        int completed = 0;
        using var semaphore = new SemaphoreSlim(32);

        var tasks = Enumerable.Range(startIp, totalIps).Select(async i =>
        {
            if (token.IsCancellationRequested) return;

            await semaphore.WaitAsync(token);
            try
            {
                string ip = $"{baseIp}.{i}";
                var node = await ProbeAndResolveAsync(ip, token, registeredDevicesCache);

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
                var arpTable = _arpResolver.GetFullArpTable();
                var postSweepTasks = new List<Task<NetworkNode>>();
                foreach (var (ip, mac) in arpTable)
                {
                    if (token.IsCancellationRequested) break;
                    if (discoveredMacs.ContainsKey(mac)) continue;

                    string[] parts = ip.Split('.');
                    if (parts.Length != 4) continue;
                    string ipSubnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
                    if (ipSubnet != baseIp) continue;
                    if (!int.TryParse(parts[3], out int lastOctet) || lastOctet < startIp || lastOctet > endIp) continue;

                    postSweepTasks.Add(Task.Run(async () =>
                    {
                        var node = BuildNode(ip, mac);
                        using var resolveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                        resolveCts.CancelAfter(5000);
                        try
                        {
                            await ResolveNodeMetadataAsync(node, resolveCts.Token).WaitAsync(resolveCts.Token);
                        }
                        catch (OperationCanceledException) { }
                        catch { }
                        return node;
                    }, token));
                }

                var resolvedNodes = await Task.WhenAll(postSweepTasks);
                foreach (var node in resolvedNodes)
                {
                    if (node != null && !token.IsCancellationRequested)
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

    public virtual async Task<List<NetworkNode>> ScanSubnetAsync(string baseIp, CancellationToken token = default)
    {
        _ = Task.Run(() => DeepFingerprintEngine.Instance.StartDiscoverySweepAsync(token), token);
        ResolveBindingIp();
        var activeNodes = new ConcurrentBag<NetworkNode>();
        var registeredDevicesCache = Data.LocalDatabase.Instance.GetRegisteredDevices();
        var discoveredMacs = new ConcurrentDictionary<string, bool>();

        // Pre-Sweep: Rapid ARP check for silent devices (Issue 8)
        try
        {
            var arpTable = _arpResolver.GetFullArpTable();
            var preSweepTasks = new List<Task<NetworkNode>>();
            foreach (var (ip, mac) in arpTable)
            {
                if (token.IsCancellationRequested) break;
                if (ip == "127.0.0.1" || mac == "00:00:00:00:00:00") continue;

                string[] parts = ip.Split('.');
                if (parts.Length == 4 && $"{parts[0]}.{parts[1]}.{parts[2]}" == baseIp)
                {
                    if (discoveredMacs.TryAdd(mac, true))
                    {
                        preSweepTasks.Add(Task.Run(async () =>
                        {
                            var node = BuildNode(ip, mac);
                            using var resolveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                            resolveCts.CancelAfter(5000);
                            try
                            {
                                await ResolveNodeMetadataAsync(node, resolveCts.Token).WaitAsync(resolveCts.Token);
                            }
                            catch (OperationCanceledException) { }
                            catch { }
                            return node;
                        }, token));
                    }
                }
            }

            var resolvedNodes = await Task.WhenAll(preSweepTasks);
            foreach (var node in resolvedNodes)
            {
                if (node != null && !token.IsCancellationRequested)
                {
                    activeNodes.Add(node);
                    NodeDiscovered?.Invoke(node);
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
                    var node = await ProbeAndResolveAsync(ip, token, registeredDevicesCache);

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
                var arpTable = _arpResolver.GetFullArpTable();
                var postSweepTasks = new List<Task<NetworkNode>>();
                foreach (var (ip, mac) in arpTable)
                {
                    if (token.IsCancellationRequested) break;
                    if (discoveredMacs.ContainsKey(mac)) continue;

                    string[] parts = ip.Split('.');
                    if (parts.Length != 4) continue;
                    string ipSubnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
                    if (!allSubnets.Contains(ipSubnet)) continue;

                    postSweepTasks.Add(Task.Run(async () =>
                    {
                        var node = BuildNode(ip, mac);
                        using var resolveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                        resolveCts.CancelAfter(5000);
                        try
                        {
                            await ResolveNodeMetadataAsync(node, resolveCts.Token).WaitAsync(resolveCts.Token);
                        }
                        catch (OperationCanceledException) { }
                        catch { }
                        return node;
                    }, token));
                }

                var resolvedNodes = await Task.WhenAll(postSweepTasks);
                foreach (var node in resolvedNodes)
                {
                    if (node != null && !token.IsCancellationRequested)
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

    private async Task<NetworkNode?> ProbeAndResolveAsync(string ip, CancellationToken token, List<NetworkNode>? registeredDevicesCache = null)
    {
        if (token.IsCancellationRequested) return null;

        bool isReachable = false;
        long latency = -1;
        string mac = "Unknown";
        int pingTimeout = Math.Min(TimeoutMs, 2000);

        try
        {
            var reply = await _pingProvider.SendPingAsync(ip, pingTimeout);
            if (reply.Status == IPStatus.Success)
            {
                isReachable = true;
                latency = reply.RoundtripTime;
            }
        }
        catch { }

        try
        {
            mac = _arpResolver.ResolveMacAddress(ip, _localBindingIp);

            // Fallback: If direct SendARP failed, check the full system ARP table
            if (mac == "Unknown")
            {
                var arpList = _arpResolver.GetFullArpTable();
                var found = arpList.FirstOrDefault(x => x.Ip == ip);
                if (!string.IsNullOrEmpty(found.Mac))
                {
                    mac = found.Mac;
                }
            }

            if (mac != "Unknown") isReachable = true;
        }
        catch { }

        // Broaden port sweep for "stealth" Windows devices that block ICMP and fail SendARP over VPNs
        if (!isReachable && !token.IsCancellationRequested)
        {
            int[] stealthPorts = { 135, 137, 139, 445, 80, 443, 5357 }; // Windows WSD, SMB, NetBIOS
            int stealthTimeout = Math.Min(TimeoutMs, 200);
            using var stealthCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            stealthCts.CancelAfter(stealthTimeout);

            var probeTasks = stealthPorts.Select(async port =>
            {
                try
                {
                    using var tcp = new TcpClient();
                    await tcp.ConnectAsync(ip, port, stealthCts.Token);
                    if (tcp.Connected)
                    {
                        stealthCts.Cancel();
                        return true;
                    }
                }
                catch { }
                return false;
            }).ToList();

            var probeResults = await Task.WhenAll(probeTasks);
            if (probeResults.Any(r => r))
            {
                isReachable = true;
                if (mac == "Unknown")
                {
                    mac = _arpResolver.ResolveMacAddress(ip, _localBindingIp);
                    if (mac == "Unknown")
                    {
                        var table = ArpResolver.GetFullArpTableAsDictionary();
                        if (table.TryGetValue(ip, out var foundMac) && !string.IsNullOrEmpty(foundMac)) mac = foundMac;
                    }
                }
            }
        }

        if (!isReachable) return null;

        // Final fallback: Use IP-based ID but flag it properly (Issue: prevents database link loss)
        if (mac == "Unknown")
        {
            // We check the database to see if we have a device that HAD this IP recently
            // This allows us to maintain identity even if ARP is transiently failing.
            var existing = registeredDevicesCache != null
                ? registeredDevicesCache.FirstOrDefault(d => d.IpAddress == ip)
                : Data.LocalDatabase.Instance.GetRegisteredDeviceByIp(ip);
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

        // Use a per-node resolution timeout (5s) to prevent hangs at 82%
        using var resolveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        resolveCts.CancelAfter(5000);

        try
        {
            await ResolveNodeMetadataAsync(node, resolveCts.Token).WaitAsync(resolveCts.Token);
        }
        catch (OperationCanceledException) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Metadata resolution timed out for {node.IpAddress}"); }
        catch (Exception ex) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Metadata resolution failed for {node.IpAddress}: {ex.Message}"); }

        return node;
    }

    private async Task ResolveNodeMetadataAsync(NetworkNode node, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        // 1. Network Lookups (May Timeout)
        try
        {
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

                    string netbios = await Task.Run(() => _arpResolver.TryResolveNetBiosName(node.IpAddress), cts.Token);
                    if (!string.IsNullOrEmpty(netbios))
                        node.Hostname = netbios;
                }
                catch (OperationCanceledException) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"NetBIOS resolution timed out for {node.IpAddress}"); }
                catch (Exception ex) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"NetBIOS resolution failed for {node.IpAddress}: {ex.Message}"); }
            }

            // Inline port scanning (if enabled)
            // OS detection and FastScan mode implicitly require port scanning to function properly
            bool shouldPortScan = EnableInlinePortScan || EnableOsDetection || FastScanMode;
            if (shouldPortScan && !token.IsCancellationRequested)
            {
                try
                {
                    var scanResults = await PortScanner.ScanPortsAsync(node.IpAddress, FastScanMode, Math.Min(TimeoutMs, 500), token);
                    node.OpenPorts = scanResults.Keys.OrderBy(p => p).ToList();
                    node.PortBanners = scanResults;
                }
                catch (OperationCanceledException) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Inline port scan/banner grabbing timed out for {node.IpAddress}"); }
                catch (Exception ex) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Inline port scan/banner grabbing failed for {node.IpAddress}: {ex.Message}"); }
            }
        }
        catch (OperationCanceledException) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Metadata resolution timed out for {node.IpAddress}"); }
        catch (Exception ex) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Metadata resolution failed for {node.IpAddress}: {ex.Message}"); }

        // 2. Deep Fingerprinting Engine
        try
        {
            var fingerprint = await DeepFingerprintEngine.Instance.FingerprintNodeAsync(node, token);
            node.Vendor = fingerprint.Vendor;
            node.DeviceType = fingerprint.TypeString;
            node.OsGuess = fingerprint.Os;
            node.IconPath = fingerprint.IconSvgKey;

            if (!string.IsNullOrEmpty(fingerprint.Model))
            {
                node.ExactModel = fingerprint.Model;
            }

            // If we found port banners, append them
            if (node.PortBanners != null)
            {
                foreach (var banner in node.PortBanners.Values.Where(b => !string.IsNullOrEmpty(b)))
                {
                    if (string.IsNullOrEmpty(node.ExactModel)) node.ExactModel = banner;
                    else if (!node.ExactModel.Contains(banner)) node.ExactModel += $" | {banner}";
                }
            }
        }
        catch (OperationCanceledException) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Deep fingerprinting timed out for {node.IpAddress}"); }
        catch (Exception ex) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Deep fingerprinting failed for {node.IpAddress}: {ex.Message}"); }

        // 3. Vulnerability Scoring
        VulnerabilityEngine.UpdateThreatLevel(node);

        // 4. UI Synchronization
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
            mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip, ""));

            // Fallback: Check full ARP table
            if (mac == "Unknown")
            {
                var table = ArpResolver.GetFullArpTableAsDictionary();
                if (table.TryGetValue(ip, out var foundMac) && !string.IsNullOrEmpty(foundMac)) mac = foundMac;
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
                        var table = ArpResolver.GetFullArpTableAsDictionary();
                        if (table.TryGetValue(ip, out var foundMac) && !string.IsNullOrEmpty(foundMac)) mac = foundMac;
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
                                    var table = ArpResolver.GetFullArpTableAsDictionary();
                                    if (table.TryGetValue(ip, out var foundMac) && !string.IsNullOrEmpty(foundMac)) mac = foundMac;
                                }
                            }
                            break;
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Port scan connection failed for {ip}:{port}: {ex.Message}"); }
                }
                catch (Exception ex) { Logger.Log(LogLevel.Warning, "SubnetScanner", $"Port scan setup failed for {ip}:{port}: {ex.Message}"); }
            }
        }

        // Database correlation fallback for background monitor
        if (isOnline && mac == "Unknown")
        {
            var existing = Data.LocalDatabase.Instance.GetRegisteredDeviceByIp(ip);
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
                if (ip.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                string[] parts = ip.Address.ToString().Split('.');
                if (parts.Length != 4) continue;

                string subnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
                if (subnet.StartsWith("169.254")) continue;
                if (subnets.Contains(subnet)) continue;

                subnets.Add(subnet);
                // S3: Track preferred interface subnet
                if (!string.IsNullOrEmpty(preferredInterface) &&
                    ni.Name.Contains(preferredInterface, StringComparison.OrdinalIgnoreCase))
                {
                    preferredSubnet = subnet;
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
        catch (OperationCanceledException)
        {
            Logger.Log(LogLevel.Warning, "SubnetScanner", $"Hostname resolution cancelled for {ip}");
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
        {
            // Expected when a host doesn't have a DNS entry
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, "SubnetScanner", $"Error resolving hostname for {ip}: {ex.Message}");
        }

        return "Unknown Device";
    }
}
