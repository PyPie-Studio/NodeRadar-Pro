using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core.Discovery;
using Xunit;

namespace Core.Tests;

public class SsdpDiscoveryMethodTests
{
    [Fact]
    public void Name_ReturnsCorrectProtocolName()
    {
        var ssdp = new SsdpDiscoveryMethod();
        Assert.Equal("UPnP / SSDP", ssdp.Name);
    }

    [Fact]
    public async Task DiscoverAsync_WhenCanceled_ExitsGracefully()
    {
        var ssdp = new SsdpDiscoveryMethod();
        var targetIps = new List<IPAddress> { IPAddress.Parse("127.0.0.1") };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var discoveredDevices = new List<NetworkDevice>();
        await ssdp.DiscoverAsync("127.0.0", targetIps, device => discoveredDevices.Add(device), cts.Token);
        Assert.Empty(discoveredDevices);
    }

    [Fact]
    public async Task DiscoverAsync_WhenSocketExceptionOccurs_CatchesAndExitsGracefully()
    {
        var ssdp = new SsdpDiscoveryMethod();
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
        await ssdp.DiscoverAsync("127.0.0", targetIps, device => discoveredDevices.Add(device), cts.Token, CustomUdpFactory);
        Assert.Empty(discoveredDevices);
    }

    [Theory]
    [InlineData("Linux/3.0 UPnP/1.0 Huawei-HG8120C/1.0", "Huawei")]
    [InlineData("TP-Link Router OS 1.0", "TP-Link")]
    [InlineData("MikroTik RouterOS 6.48", "MikroTik")]
    [InlineData("Cisco/1.0", "Cisco")]
    [InlineData("Netgear/2.0", "Netgear")]
    [InlineData("Linksys/1.1", "Linksys")]
    [InlineData("D-Link/3.0", "D-Link")]
    [InlineData("ASUS Router", "ASUS")]
    [InlineData("Xiaomi/1.0", "Xiaomi")]
    [InlineData("CustomVendor/1.0 UPnP/1.0", "CustomVendor")]
    [InlineData("UPnP/1.0", "Generic Vendor")]
    [InlineData("", "Generic Vendor")]
    public void ParseVendorFromServerHeader_ReturnsExpectedVendor(string serverHeader, string expectedVendor)
    {
        string actualVendor = SsdpDiscoveryMethod.ParseVendorFromServerHeader(serverHeader);
        Assert.Equal(expectedVendor, actualVendor);
    }
}
