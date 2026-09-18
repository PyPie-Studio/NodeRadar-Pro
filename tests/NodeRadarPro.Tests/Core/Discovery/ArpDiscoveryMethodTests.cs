using System.Net;
using NodeRadarPro.Core.Discovery;

namespace NodeRadarPro.Tests;

public class ArpDiscoveryMethodTests
{
    [Fact]
    public void Name_ReturnsExpectedProtocolName()
    {
        var arp = new ArpDiscoveryMethod();
        Assert.Equal("ARP / MAC Resolution", arp.Name);
    }

    [Theory]
    [InlineData(null, "Unknown Vendor")]
    [InlineData("", "Unknown Vendor")]
    [InlineData("Unknown", "Unknown Vendor")]
    [InlineData("00:1C:B3:11:22:33", "Apple")]
    [InlineData("00-1C-B3-11-22-33", "Apple")]
    [InlineData("00:18:82:AA:BB:CC", "Huawei")]
    [InlineData("00:EC:0A:12:34:56", "Xiaomi")]
    [InlineData("02:00:00:00:00:01", "Privacy MAC")]
    [InlineData("06:12:34:56:78:90", "Privacy MAC")]
    [InlineData("0A:11:22:33:44:55", "Privacy MAC")]
    [InlineData("0E:AA:BB:CC:DD:EE", "Privacy MAC")]
    [InlineData("00:00:00:00:00:00", "Unknown Vendor")]
    [InlineData("10:34:56:78:90:AB", "Unknown Vendor")]
    public void GetMacVendor_ValidAndEdgeCases_ReturnsExpectedVendor(string mac, string expectedVendor)
    {
        string actual = ArpDiscoveryMethod.GetMacVendor(mac);
        Assert.Equal(expectedVendor, actual);
    }

    [Fact]
    public async Task DiscoverAsync_WhenMacResolved_DiscoversDevicesAndInvokesCallback()
    {
        var arp = new ArpDiscoveryMethod();
        var targetIps = new List<IPAddress>
        {
            IPAddress.Parse("192.168.1.10"),
            IPAddress.Parse("192.168.1.20")
        };
        var discovered = new List<NetworkDevice>();

        await arp.DiscoverAsync("192.168.1", targetIps, dev => discovered.Add(dev), CancellationToken.None, ip =>
        {
            return ip.ToString() == "192.168.1.10" ? "00:1C:B3:00:11:22" : "Unknown";
        });

        Assert.Single(discovered);
        var device = discovered[0];
        Assert.Equal("192.168.1.10", device.IpAddress);
        Assert.Equal("00:1C:B3:00:11:22", device.MacAddress);
        Assert.Equal("Apple", device.Vendor);
        Assert.True(device.IsOnline);
        Assert.Equal("ARP / MAC Resolution", device.SourceProtocol);
    }

    [Fact]
    public async Task DiscoverAsync_WhenCancelled_ExitsGracefullyWithoutDiscovering()
    {
        var arp = new ArpDiscoveryMethod();
        var targetIps = new List<IPAddress> { IPAddress.Parse("192.168.1.10") };
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var discovered = new List<NetworkDevice>();

        await arp.DiscoverAsync("192.168.1", targetIps, dev => discovered.Add(dev), cts.Token, _ => "00:1C:B3:00:11:22");

        Assert.Empty(discovered);
    }

    [Fact]
    public async Task DiscoverAsync_WhenResolverThrowsException_HandlesGracefully()
    {
        var arp = new ArpDiscoveryMethod();
        var targetIps = new List<IPAddress> { IPAddress.Parse("192.168.1.10") };
        var discovered = new List<NetworkDevice>();

        await arp.DiscoverAsync("192.168.1", targetIps, dev => discovered.Add(dev), CancellationToken.None, _ => throw new InvalidOperationException("ARP failure"));

        Assert.Empty(discovered);
    }
}
