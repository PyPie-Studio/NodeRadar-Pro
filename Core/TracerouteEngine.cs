using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

/// <summary>
/// Result of a single hop in a traceroute.
/// </summary>
public class RouteHop
{
    public int HopNumber { get; set; }
    public string IpAddress { get; set; } = "—";
    public string Hostname { get; set; } = "Request timed out";
    public long LatencyMs { get; set; } = -1;
    public bool IsDestination { get; set; }
    public bool Status { get; set; } // true if responsive
}

/// <summary>
/// Core engine for performing hop-by-hop path discovery.
/// </summary>
public class TracerouteEngine
{
    public event Action<RouteHop>? HopDiscovered;
    public event Action<bool>? TracerouteCompleted;

    public async Task RunTracerouteAsync(string destination, int maxHops = 30, int timeoutMs = 2000, CancellationToken token = default)
    {
        try
        {
            IPAddress? destAddr;
            if (!IPAddress.TryParse(destination, out destAddr))
            {
                var hostEntry = await Dns.GetHostEntryAsync(destination);
                destAddr = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            }

            if (destAddr == null)
            {
                TracerouteCompleted?.Invoke(false);
                return;
            }

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                if (token.IsCancellationRequested) break;

                var hop = await ProbeHopAsync(destAddr, ttl, timeoutMs);
                HopDiscovered?.Invoke(hop);

                if (hop.IsDestination || token.IsCancellationRequested)
                    break;
            }

            TracerouteCompleted?.Invoke(true);
        }
        catch
        {
            TracerouteCompleted?.Invoke(false);
        }
    }

    private async Task<RouteHop> ProbeHopAsync(IPAddress destination, int ttl, int timeoutMs)
    {
        var hop = new RouteHop { HopNumber = ttl };
        byte[] buffer = Encoding.ASCII.GetBytes("NodeRadar-Trace-Probe");
        var options = new PingOptions(ttl, true);

        try
        {
            using var pinger = new Ping();
            var reply = await pinger.SendPingAsync(destination, timeoutMs, buffer, options);

            if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success)
            {
                hop.IpAddress = reply.Address?.ToString() ?? "—";
                hop.LatencyMs = reply.RoundtripTime;
                hop.Status = true;
                hop.IsDestination = reply.Status == IPStatus.Success;

                if (hop.Status && hop.IpAddress != "—")
                {
                    try
                    {
                        var hostTask = Dns.GetHostEntryAsync(hop.IpAddress);
                        if (await Task.WhenAny(hostTask, Task.Delay(1000)) == hostTask)
                        {
                            var entry = await hostTask;
                            if (entry.HostName != hop.IpAddress)
                                hop.Hostname = entry.HostName;
                            else
                                hop.Hostname = "";
                        }
                    }
                    catch { hop.Hostname = ""; }
                }
            }
            else
            {
                hop.Status = false;
            }
        }
        catch
        {
            hop.Status = false;
        }

        return hop;
    }
}
