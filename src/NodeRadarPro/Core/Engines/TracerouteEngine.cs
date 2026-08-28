using System;
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

    public async Task RunTracerouteAsync(string destination, int maxHops = 30, int timeoutMs = 2000, CancellationToken token = default, Func<IPingClient>? pingClientFactory = null)
    {
        pingClientFactory ??= () => new PingClientWrapper();
        try
        {
            if (!IPAddress.TryParse(destination, out IPAddress? destAddr))
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

                var hop = await ProbeHopAsync(destAddr, ttl, timeoutMs, pingClientFactory);
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

    private static readonly byte[] ProbeBuffer = Encoding.ASCII.GetBytes("NodeRadar-Trace-Probe");

    private async Task<RouteHop> ProbeHopAsync(IPAddress destination, int ttl, int timeoutMs, Func<IPingClient> pingClientFactory)
    {
        var hop = new RouteHop { HopNumber = ttl };
        byte[] buffer = ProbeBuffer;
        var options = new PingOptions(ttl, true);

        try
        {
            using var pinger = pingClientFactory();
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
                        using var cts = new CancellationTokenSource(1000);
                        var entry = await Dns.GetHostEntryAsync(hop.IpAddress, cts.Token);
                        if (entry.HostName != hop.IpAddress)
                            hop.Hostname = entry.HostName;
                        else
                            hop.Hostname = "";
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


public interface IPingClient : IDisposable
{
    Task<IPingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions options);
}

public interface IPingReply
{
    IPStatus Status { get; }
    IPAddress? Address { get; }
    long RoundtripTime { get; }
}

public class TraceroutePingReply : IPingReply
{
    private readonly PingReply _reply;
    public TraceroutePingReply(PingReply reply) => _reply = reply;

    public IPStatus Status => _reply.Status;
    public IPAddress? Address => _reply.Address;
    public long RoundtripTime => _reply.RoundtripTime;
}

public class PingClientWrapper : IPingClient
{
    private readonly Ping _ping = new Ping();

    public async Task<IPingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions options)
    {
        var reply = await _ping.SendPingAsync(address, timeout, buffer, options);
        return new TraceroutePingReply(reply);
    }

    public void Dispose()
    {
        _ping.Dispose();
    }
}
