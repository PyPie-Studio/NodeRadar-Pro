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
/// Core network scanning engine for Layer 3 discovery.
/// </summary>
public class SubnetScanner
{
    private const int TimeoutMs = 1500;
    
    // Fired whenever a new node is discovered
    public event Action<NetworkNode>? NodeDiscovered;
    public event Action<double>? ProgressUpdated;

    /// <summary>
    /// Scans a full /24 subnet asynchronously.
    /// E.g., "192.168.1"
    /// </summary>
    public async Task<List<NetworkNode>> ScanSubnetAsync(string baseIp, CancellationToken token = default)
    {
        var activeNodes = new ConcurrentBag<NetworkNode>();
        int totalPings = 254;
        int completedPings = 0;

        // Create tasks for IP 1 to 254
        var pingTasks = Enumerable.Range(1, 254).Select(async i =>
        {
            if (token.IsCancellationRequested) return;

            string ipToPing = $"{baseIp}.{i}";
            
            try
            {
                using var pinger = new Ping();
                PingReply reply = await pinger.SendPingAsync(ipToPing, TimeoutMs);

                if (reply.Status == IPStatus.Success)
                {
                    // Device is online. Resolve MAC and hostname.
                    var node = new NetworkNode
                    {
                        IpAddress = ipToPing,
                        IsOnline = true,
                        PingLatencyMs = reply.RoundtripTime
                    };

                    node.MacAddress = ArpResolver.ResolveMacAddress(ipToPing);
                    node.Hostname = await TryGetHostnameAsync(ipToPing);

                    activeNodes.Add(node);
                    
                    // Notify UI that a node was found
                    NodeDiscovered?.Invoke(node);
                }
            }
            catch
            {
                // Ignore unreachable hosts or network exceptions on specific threads
            }
            finally
            {
                int current = Interlocked.Increment(ref completedPings);
                // Ensure we don't spam the UI thread with 254 simultaneous updates by throttling the percentage math slightly
                double percentage = Math.Min(100.0, ((double)current / totalPings) * 100.0);
                if (current % 5 == 0 || current == totalPings) // Update UI every 5 pings to fix stutter
                {
                    ProgressUpdated?.Invoke(percentage);
                }
            }
        });

        // Run all 254 pings concurrently for extreme speed
        await Task.WhenAll(pingTasks);

        return activeNodes.ToList();
    }

    /// <summary>
    /// Gets the base IP of the current active network interface (e.g., returns "192.168.1" for "192.168.1.5").
    /// </summary>
    public static string GetLocalBaseIp()
    {
        string localIp = "192.168.1"; // Fallback

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up && 
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string[] parts = ip.Address.ToString().Split('.');
                        if (parts.Length == 4)
                        {
                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
                        }
                    }
                }
            }
        }
        return localIp;
    }

    private static async Task<string> TryGetHostnameAsync(string ip)
    {
        try
        {
            // Reverse DNS lookup
            var hostEntry = await Dns.GetHostEntryAsync(ip);
            return hostEntry.HostName;
        }
        catch
        {
            return "Unknown Device";
        }
    }
}