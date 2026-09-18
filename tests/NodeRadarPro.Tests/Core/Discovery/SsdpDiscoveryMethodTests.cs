using System.Net;
using System.Net.Sockets;
using NodeRadarPro.Core.Discovery;

namespace NodeRadarPro.Tests;

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
    // Known vendors with exact and varied casing
    [InlineData("Linux/3.0 UPnP/1.0 Huawei-HG8120C/1.0", "Huawei")]
    [InlineData("huawei-hg8120c/1.0", "Huawei")]
    [InlineData("TP-Link Router OS 1.0", "TP-Link")]
    [InlineData("tp-link OS", "TP-Link")]
    [InlineData("MikroTik RouterOS 6.48", "MikroTik")]
    [InlineData("mikrotik router", "MikroTik")]
    [InlineData("Cisco/1.0", "Cisco")]
    [InlineData("cisco-ios/12.2", "Cisco")]
    [InlineData("Netgear/2.0", "Netgear")]
    [InlineData("netgear-readynas", "Netgear")]
    [InlineData("Linksys/1.1", "Linksys")]
    [InlineData("linksys-wrt", "Linksys")]
    [InlineData("D-Link/3.0", "D-Link")]
    [InlineData("d-link-dir", "D-Link")]
    [InlineData("ASUS Router", "ASUS")]
    [InlineData("asus router", "ASUS")]
    [InlineData("Xiaomi/1.0", "Xiaomi")]
    [InlineData("xiaomi router", "Xiaomi")]
    public void ParseVendorFromServerHeader_KnownVendors_ReturnsVendor(string serverHeader, string expectedVendor)
    {
        string actualVendor = SsdpDiscoveryMethod.ParseVendorFromServerHeader(serverHeader);
        Assert.Equal(expectedVendor, actualVendor);
    }

    [Theory]
    // Fallback logic for non-known vendors (>2 chars, non-UPnP)
    [InlineData("CustomVendor/1.0 UPnP/1.0", "CustomVendor")]
    [InlineData("Synology DiskStation/7.0", "Synology")]
    [InlineData("NetOS, UPnP/1.0", "NetOS")]
    // Short tokens (<= 2 chars) falling back to Generic Vendor
    [InlineData("HP/1.0 UPnP/1.0", "Generic Vendor")]
    [InlineData("Mi UPnP/1.0", "Generic Vendor")]
    // UPnP tokens falling back to Generic Vendor
    [InlineData("UPnP/1.0", "Generic Vendor")]
    [InlineData("upnp/2.0", "Generic Vendor")]
    // Empty, whitespace, or null headers
    [InlineData("", "Generic Vendor")]
    [InlineData("   ", "Generic Vendor")]
    [InlineData(null, "Generic Vendor")]
    public void ParseVendorFromServerHeader_FallbackAndEdgeCases_ReturnsExpectedVendor(string? serverHeader, string expectedVendor)
    {
        string actualVendor = SsdpDiscoveryMethod.ParseVendorFromServerHeader(serverHeader);
        Assert.Equal(expectedVendor, actualVendor);
    }
}
