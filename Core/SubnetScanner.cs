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
/// Phase 1: ARP sweep (SendARP) discovers ALL devices including WiFi phones/IoT.
/// Phase 2: ICMP ping for latency measurement.
/// Phase 3: Hostname resolution (DNS → NetBIOS fallback).
/// </summary>
public class SubnetScanner
{
    private const int PingTimeoutMs = 1500;
    
    public event Action<NetworkNode>? NodeDiscovered;
    public event Action<double>? ProgressUpdated;

    /// <summary>
    /// Scans a specific range of IPs in a subnet.
    /// </summary>
    public async Task<List<NetworkNode>> ScanRangeAsync(string baseIp, int startIp, int endIp, CancellationToken token = default)
    {
        var activeNodes = new ConcurrentBag<NetworkNode>();
        var discoveredMacs = new ConcurrentDictionary<string, bool>();

        int totalIps = endIp - startIp + 1;
        int completed = 0;

        var arpTasks = Enumerable.Range(startIp, totalIps).Select(async i =>
        {
            if (token.IsCancellationRequested) return;

            string ip = $"{baseIp}.{i}";
            
            try
            {
                // Try L3 Ping first
                using var pinger = new Ping();
                var reply = await pinger.SendPingAsync(ip, 800);
                bool isReachable = (reply.Status == IPStatus.Success);
                
                string mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));

                if (isReachable || mac != "Unknown")
                {
                    if (mac == "Unknown") mac = "L3-ROUTED-" + ip;
                    discoveredMacs.TryAdd(mac, true);

                    var node = new NetworkNode
                    {
                        IpAddress = ip,
                        MacAddress = mac,
                        IsOnline = true,
                        PingLatencyMs = reply?.Status == IPStatus.Success ? reply.RoundtripTime : -1
                    };
                    
                    activeNodes.Add(node);
                    NodeDiscovered?.Invoke(node);
                }
            }
            catch { }
            finally
            {
                int current = Interlocked.Increment(ref completed);
                double percentage = Math.Min(90.0, ((double)current / totalIps) * 90.0);
                if (current % 8 == 0 || current == totalIps)
                {
                    ProgressUpdated?.Invoke(percentage);
                }
            }
        });

        await Task.WhenAll(arpTasks);

        // Phase 2 & 3 for range scan
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
                
                if (int.TryParse(parts[3], out int lastOctet) && lastOctet >= startIp && lastOctet <= endIp)
                {
                    var node = new NetworkNode
                    {
                        IpAddress = ip,
                        MacAddress = mac,
                        IsOnline = true,
                        PingLatencyMs = -1
                    };

                    activeNodes.Add(node);
                    NodeDiscovered?.Invoke(node);
                }
            }
        }
        catch { }

        ProgressUpdated?.Invoke(90.0);

        var hostnameTasks = activeNodes.Select(async node =>
        {
            if (token.IsCancellationRequested) return;

            node.Hostname = await TryGetHostnameAsync(node.IpAddress);

            if (node.Hostname == "Unknown Device")
            {
                try
                {
                    string netbios = await Task.Run(() => ArpResolver.TryResolveNetBiosName(node.IpAddress));
                    if (!string.IsNullOrEmpty(netbios))
                        node.Hostname = netbios;
                }
                catch { }
            }

            // Fingerprinting: try HTTP server banner for better naming
            if (node.Hostname == "Unknown Device" || string.IsNullOrEmpty(node.Hostname))
            {
                string serverBanner = await Core.DeviceFingerprinter.TryGetHttpServerBannerAsync(node.IpAddress);
                if (!string.IsNullOrEmpty(serverBanner))
                {
                    node.Hostname = serverBanner;
                }
            }

            node.Vendor = Data.VendorLookup.GetVendor(node.MacAddress);
            node.DeviceType = Data.VendorLookup.GuessDeviceType(node.Vendor, node.Hostname);
            
            if (!string.IsNullOrEmpty(node.DeviceType))
            {
                string t = node.DeviceType.ToLower();
                if (t.Contains("phone") || t.Contains("iphone")) node.IconPath = "phone";
                else if (t.Contains("tv")) node.IconPath = "tv";
                else if (t.Contains("router") || t.Contains("network")) node.IconPath = "router";
                else if (t.Contains("pc") || t.Contains("computer") || t.Contains("desktop")) node.IconPath = "pc";
                else if (t.Contains("laptop") || t.Contains("macbook")) node.IconPath = "laptop";
                else if (t.Contains("camera")) node.IconPath = "camera";
                else if (t.Contains("speaker")) node.IconPath = "speaker";
                else if (t.Contains("nas") || t.Contains("server")) node.IconPath = "server";
            }
        });

        var hostnameCompletion = Task.WhenAll(hostnameTasks);
        await Task.WhenAny(hostnameCompletion, Task.Delay(15000, token));

        ProgressUpdated?.Invoke(100.0);
        return activeNodes.ToList();
    }

    /// <summary>
    /// Scans all active subnets using ARP-first discovery.
    /// </summary>
    public async Task<List<NetworkNode>> ScanSubnetAsync(string baseIp, CancellationToken token = default)
    {
        var activeNodes = new ConcurrentBag<NetworkNode>();
        var discoveredMacs = new ConcurrentDictionary<string, bool>();

        // ── Phase 0: Get all subnets to scan ──
        var allSubnets = GetAllLocalBaseIps();
        if (!allSubnets.Contains(baseIp))
            allSubnets.Insert(0, baseIp);

        int totalIps = allSubnets.Count * 254;
        int completed = 0;

        // ── Phase 1: ARP sweep — discovers ALL devices (even those blocking ICMP) ──
        foreach (var subnet in allSubnets)
        {
            // Run ARP probes in parallel with controlled concurrency
            var arpTasks = Enumerable.Range(1, 254).Select(async i =>
            {
                if (token.IsCancellationRequested) return;

                string ip = $"{subnet}.{i}";
                
                try
                {
            // SendARP works at L2 — phones/IoT MUST respond (protocol requirement)
                    using var pinger = new Ping();
                    var reply = await pinger.SendPingAsync(ip, 800);
                    bool isReachable = (reply.Status == IPStatus.Success);
                    string mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));

                    if (isReachable || mac != "Unknown")
                    {
                        if (mac == "Unknown") mac = "L3-ROUTED-" + ip;
                        
                        // Device is on the network
                        discoveredMacs.TryAdd(mac, true);

                        var node = new NetworkNode
                        {
                            IpAddress = ip,
                            MacAddress = mac,
                            IsOnline = true,
                            PingLatencyMs = reply?.Status == IPStatus.Success ? reply.RoundtripTime : -1
                        };

                        activeNodes.Add(node);
                        NodeDiscovered?.Invoke(node);
                    }
                }
                catch { }
                finally
                {
                    int current = Interlocked.Increment(ref completed);
                    double percentage = Math.Min(90.0, ((double)current / totalIps) * 90.0);
                    if (current % 8 == 0 || current == totalIps)
                    {
                        ProgressUpdated?.Invoke(percentage);
                    }
                }
            });

            await Task.WhenAll(arpTasks);
        }

        // ── Phase 2: Read full ARP table for any devices we might have missed ──
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

                var node = new NetworkNode
                {
                    IpAddress = ip,
                    MacAddress = mac,
                    IsOnline = true,
                    PingLatencyMs = -1
                };

                activeNodes.Add(node);
                NodeDiscovered?.Invoke(node);
            }
        }
        catch { }

        ProgressUpdated?.Invoke(90.0);

        // ── Phase 3: Hostname resolution (parallel, with timeout) ──
        var hostnameTasks = activeNodes.Select(async node =>
        {
            if (token.IsCancellationRequested) return;

            // DNS resolution
            node.Hostname = await TryGetHostnameAsync(node.IpAddress);

            // NetBIOS fallback for Windows devices
            if (node.Hostname == "Unknown Device")
            {
                try
                {
                    string netbios = await Task.Run(() => ArpResolver.TryResolveNetBiosName(node.IpAddress));
                    if (!string.IsNullOrEmpty(netbios))
                        node.Hostname = netbios;
                }
                catch { }
            }

            // Set vendor and device type
            node.Vendor = Data.VendorLookup.GetVendor(node.MacAddress);
            node.DeviceType = Data.VendorLookup.GuessDeviceType(node.Vendor, node.Hostname);
            
            // Set icon path based on type
            if (!string.IsNullOrEmpty(node.DeviceType))
            {
                string t = node.DeviceType.ToLower();
                if (t.Contains("phone") || t.Contains("iphone")) node.IconPath = "phone";
                else if (t.Contains("tv")) node.IconPath = "tv";
                else if (t.Contains("router") || t.Contains("network")) node.IconPath = "router";
                else if (t.Contains("pc") || t.Contains("computer") || t.Contains("desktop")) node.IconPath = "pc";
                else if (t.Contains("laptop") || t.Contains("macbook")) node.IconPath = "laptop";
                else if (t.Contains("camera")) node.IconPath = "camera";
                else if (t.Contains("speaker")) node.IconPath = "speaker";
                else if (t.Contains("nas") || t.Contains("server")) node.IconPath = "server";
            }
        });

        // Wait for hostname resolution with a global timeout
        var hostnameCompletion = Task.WhenAll(hostnameTasks);
        await Task.WhenAny(hostnameCompletion, Task.Delay(15000, token)); // 15s max for all hostnames

        ProgressUpdated?.Invoke(100.0);
        return activeNodes.ToList();
    }

    /// <summary>
    /// Quick ARP+ping check for a single device. Used for manual entry validation.
    /// Returns true if the device is reachable via ARP or ICMP.
    /// </summary>
    public static async Task<(bool IsOnline, string Mac, long LatencyMs)> QuickProbeAsync(string ip)
    {
        string mac = "Unknown";
        long latency = -1;
        bool isOnline = false;

        // Try ARP first
        try
        {
            mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));
            if (mac != "Unknown") isOnline = true;
        }
        catch { }

        // Try ICMP ping
        try
        {
            using var pinger = new Ping();
            var reply = await pinger.SendPingAsync(ip, 2000);
            if (reply.Status == IPStatus.Success)
            {
                isOnline = true;
                latency = reply.RoundtripTime;

                // If ARP failed, try again after ping populated ARP cache
                if (mac == "Unknown")
                    mac = await Task.Run(() => ArpResolver.ResolveMacAddress(ip));
            }
        }
        catch { }

        // TCP fallback — try common ports (phones often block ICMP but allow TCP)
        if (!isOnline)
        {
            int[] ports = { 80, 443, 22, 53, 8080, 8443, 62078 }; // 62078 = iPhone
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

    public static List<string> GetAllLocalBaseIps()
    {
        var subnets = new List<string>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            // Removed strict interface type filtering to allow VPNs, virtual adapters, and different WiFi adapters
            
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
                            subnets.Add(subnet);
                    }
                }
            }
        }

        return subnets;
    }

    private static async Task<string> TryGetHostnameAsync(string ip)
    {
        try
        {
            // Add a timeout wrapper around DNS resolution
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