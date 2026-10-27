using Moq;
using System.Net;
using System.Net.Sockets;
using NodeRadarPro.Core.Discovery;

namespace NodeRadarPro.Tests;

public class MdnsDiscoveryMethodTests
{
    [Fact]
    public async Task DiscoverAsync_WhenSocketExceptionOccurs_CatchesAndExitsGracefully()
    {
        // Arrange
        var mdns = new MdnsDiscoveryMethod();
        var targetIps = new List<IPAddress> { IPAddress.Parse("127.0.0.1") };
        using var cts = new CancellationTokenSource(2000);

        var mockSocket = new Mock<IMulticastSocket>();
        mockSocket.Setup(s => s.ReceiveAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SocketException((int)SocketError.AccessDenied));

        var discoveredDevices = new List<NetworkDevice>();

        // Act & Assert
        await mdns.DiscoverAsync("127.0.0", targetIps, device => discoveredDevices.Add(device), () => mockSocket.Object, cts.Token);
        Assert.Empty(discoveredDevices);
    }

    [Fact]
    public async Task DiscoverAsync_WithSimulatedMulticastPacket_DiscoversDeviceSuccessfully()
    {
        var mdns = new MdnsDiscoveryMethod();
        var targetIps = new List<IPAddress> { IPAddress.Parse("192.168.1.80") };
        using var cts = new CancellationTokenSource(3000);

        byte[] packet = new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
            0x08, (byte)'p', (byte)'r', (byte)'i', (byte)'n', (byte)'t', (byte)'e', (byte)'r', (byte)'1',
            0x05, (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'l', 0x00
        };

        var remoteEp = new IPEndPoint(IPAddress.Parse("192.168.1.80"), 5353);
        var receiveResult = new UdpReceiveResult(packet, remoteEp);

        var mockSocket = new Mock<IMulticastSocket>();
        bool firstCall = true;
        mockSocket.Setup(s => s.ReceiveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    return Task.FromResult(receiveResult);
                }
                var tcs = new TaskCompletionSource<UdpReceiveResult>();
                token.Register(() => tcs.TrySetCanceled());
                return tcs.Task;
            });

        var discoveredDevices = new List<NetworkDevice>();
        var discoverTask = mdns.DiscoverAsync("192.168.1", targetIps, device =>
        {
            discoveredDevices.Add(device);
            cts.Cancel();
        }, () => mockSocket.Object, cts.Token);

        await discoverTask;

        Assert.Single(discoveredDevices);
        Assert.Equal("192.168.1.80", discoveredDevices[0].IpAddress);
        Assert.Equal("printer1.local", discoveredDevices[0].Hostname);
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
