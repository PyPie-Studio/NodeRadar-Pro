using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core.Discovery;
using Xunit;

namespace Core.Tests;

public class MdnsDiscoveryMethodTests
{
    [Fact]
    public async Task DiscoverAsync_WhenSocketExceptionOccurs_CatchesAndExitsGracefully()
    {
        // Arrange
        var mdns = new MdnsDiscoveryMethod();
        var targetIps = new List<IPAddress> { IPAddress.Parse("127.0.0.1") };
        using var cts = new CancellationTokenSource(2000);

        UdpClient CustomUdpFactory()
        {
            var udp = new UdpClient();
            Task.Run(async () =>
            {
                await Task.Delay(20);
                udp.Client.Close();
            });
            return udp;
        }

        var discoveredDevices = new List<NetworkDevice>();

        // Act & Assert
        await mdns.DiscoverAsync("127.0.0", targetIps, device => discoveredDevices.Add(device), cts.Token, CustomUdpFactory);
        Assert.Empty(discoveredDevices);
    }

    [Fact]
    public async Task DiscoverAsync_WhenCanceled_ExitsGracefully()
    {
        // Arrange
        var mdns = new MdnsDiscoveryMethod();
        var targetIps = new List<IPAddress> { IPAddress.Parse("127.0.0.1") };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var discoveredDevices = new List<NetworkDevice>();

        // Act & Assert
        await mdns.DiscoverAsync("127.0.0", targetIps, device => discoveredDevices.Add(device), cts.Token);
        Assert.Empty(discoveredDevices);
    }

    [Fact]
    public void ParseMdnsHostname_WithValidBuffer_ReturnsHostname()
    {
        // Arrange
        byte[] buffer = new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
            0x07, (byte)'m', (byte)'y', (byte)'d', (byte)'e', (byte)'v', (byte)'i', (byte)'c',
            0x05, (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'l', 0x00
        };

        // Act
        string hostname = MdnsDiscoveryMethod.ParseMdnsHostname(buffer);

        // Assert
        Assert.Equal("mydevic.local", hostname);
    }

    [Fact]
    public void ParseMdnsHostname_WithEmptyBuffer_ReturnsEmptyString()
    {
        // Arrange
        byte[] buffer = Array.Empty<byte>();

        // Act
        string hostname = MdnsDiscoveryMethod.ParseMdnsHostname(buffer);

        // Assert
        Assert.Equal(string.Empty, hostname);
    }
}
