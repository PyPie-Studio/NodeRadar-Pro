using System.Net;
using System.Net.NetworkInformation;

namespace NodeRadarPro.Tests.Core.Engines;

public class MockPingReply : IPingReply
{
    public IPStatus Status { get; set; }
    public IPAddress? Address { get; set; }
    public long RoundtripTime { get; set; }
}

public class MockPingClient : IPingClient
{
    public Func<IPAddress, int, byte[], PingOptions, Task<IPingReply>> SendPingAsyncMock { get; set; } = default!;

    public Task<IPingReply> SendPingAsync(IPAddress address, int timeout, byte[] buffer, PingOptions options)
    {
        return SendPingAsyncMock(address, timeout, buffer, options);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public class TracerouteEngineTests
{
    private static Task<IPHostEntry> MockDnsResolver(string host, CancellationToken ct)
    {
        var entry = new IPHostEntry
        {
            HostName = host == "8.8.8.8" ? "dns.google" : (host == "192.168.1.1" ? "gateway.local" : host),
            AddressList = IPAddress.TryParse(host, out var parsed) ? new[] { parsed } : new[] { IPAddress.Loopback }
        };
        return Task.FromResult(entry);
    }

    [Fact]
    public async Task RunTracerouteAsync_SuccessfulTrace_ReturnsHopsAndCompletes()
    {
        var engine = new TracerouteEngine(MockDnsResolver);
        var hops = new List<RouteHop>();
        bool? completed = null;
        engine.HopDiscovered += h => hops.Add(h);
        engine.TracerouteCompleted += c => completed = c;

        var mockClient = new MockPingClient
        {
            SendPingAsyncMock = (address, timeout, buffer, options) =>
            {
                if (options.Ttl == 1)
                {
                    return Task.FromResult<IPingReply>(new MockPingReply
                    {
                        Status = IPStatus.TtlExpired,
                        Address = IPAddress.Parse("192.168.1.1"),
                        RoundtripTime = 1
                    });
                }
                else
                {
                    return Task.FromResult<IPingReply>(new MockPingReply
                    {
                        Status = IPStatus.Success,
                        Address = IPAddress.Parse("8.8.8.8"),
                        RoundtripTime = 10
                    });
                }
            }
        };

        await engine.RunTracerouteAsync("8.8.8.8", maxHops: 30, timeoutMs: 2000, pingClientFactory: () => mockClient, token: default);

        Assert.True(completed);
        Assert.Equal(2, hops.Count);
        Assert.Equal("192.168.1.1", hops[0].IpAddress);
        Assert.Equal("gateway.local", hops[0].Hostname);
        Assert.False(hops[0].IsDestination);
        Assert.Equal("8.8.8.8", hops[1].IpAddress);
        Assert.Equal("dns.google", hops[1].Hostname);
        Assert.True(hops[1].IsDestination);
    }

    [Fact]
    public async Task RunTracerouteAsync_Timeout_ReportsStatusFalse()
    {
        var engine = new TracerouteEngine();
        var hops = new List<RouteHop>();
        engine.HopDiscovered += h => hops.Add(h);

        var mockClient = new MockPingClient
        {
            SendPingAsyncMock = (address, timeout, buffer, options) =>
            {
                return Task.FromResult<IPingReply>(new MockPingReply
                {
                    Status = IPStatus.TimedOut,
                    Address = null,
                    RoundtripTime = 0
                });
            }
        };

        await engine.RunTracerouteAsync("192.0.2.1", maxHops: 1, timeoutMs: 2000, pingClientFactory: () => mockClient, token: default);

        Assert.Single(hops);
        Assert.False(hops[0].Status);
        Assert.Equal("—", hops[0].IpAddress);
    }

    [Fact]
    public async Task RunTracerouteAsync_CancellationRequested_StopsEarly()
    {
        var engine = new TracerouteEngine();
        var hops = new List<RouteHop>();
        engine.HopDiscovered += h => hops.Add(h);

        var mockClient = new MockPingClient
        {
            SendPingAsyncMock = (address, timeout, buffer, options) =>
            {
                return Task.FromResult<IPingReply>(new MockPingReply
                {
                    Status = IPStatus.TtlExpired,
                    Address = IPAddress.Parse($"192.168.1.{options.Ttl}"),
                    RoundtripTime = 5
                });
            }
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        await engine.RunTracerouteAsync("8.8.8.8", maxHops: 30, timeoutMs: 2000, pingClientFactory: () => mockClient, token: cts.Token);

        // Should not have discovered any hops since it was cancelled before loop execution
        Assert.Empty(hops);
    }

    [Fact]
    public async Task RunTracerouteAsync_ProbeHopException_SetsHopStatusFalse()
    {
        var engine = new TracerouteEngine();
        var hops = new List<RouteHop>();
        bool? completed = null;
        engine.HopDiscovered += h => hops.Add(h);
        engine.TracerouteCompleted += c => completed = c;

        await engine.RunTracerouteAsync("127.0.0.1", maxHops: 1, timeoutMs: 2000, pingClientFactory: () => throw new PingException("Simulated ping failure"), token: default);

        Assert.True(completed);
        Assert.Single(hops);
        Assert.False(hops[0].Status);
    }
}
