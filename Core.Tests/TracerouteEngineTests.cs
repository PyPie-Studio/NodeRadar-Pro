using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NodeRadarPro.Core;

namespace NodeRadarPro.Core.Tests;

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

    public void Dispose() {}
}

public class TracerouteEngineTests
{
    [Fact]
    public async Task RunTracerouteAsync_SuccessfulTrace_ReturnsHopsAndCompletes()
    {
        var engine = new TracerouteEngine();
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

        await engine.RunTracerouteAsync("8.8.8.8", maxHops: 30, timeoutMs: 2000, token: default, () => mockClient);

        Assert.True(completed);
        Assert.Equal(2, hops.Count);
        Assert.Equal("192.168.1.1", hops[0].IpAddress);
        Assert.False(hops[0].IsDestination);
        Assert.Equal("8.8.8.8", hops[1].IpAddress);
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

        await engine.RunTracerouteAsync("192.0.2.1", maxHops: 1, timeoutMs: 2000, token: default, () => mockClient);

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

        await engine.RunTracerouteAsync("8.8.8.8", maxHops: 30, timeoutMs: 2000, token: cts.Token, () => mockClient);

        // Should not have discovered any hops since it was cancelled before loop execution
        Assert.Empty(hops);
    }
}
