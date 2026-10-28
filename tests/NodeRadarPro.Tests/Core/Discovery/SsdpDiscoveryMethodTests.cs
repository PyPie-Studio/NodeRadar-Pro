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

        static UdpClient CustomUdpFactory()
        {
            var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            udp.Client.Close();
            return udp;
        }

        var discoveredDevices = new List<NetworkDevice>();
        var ex = await Record.ExceptionAsync(() => ssdp.DiscoverAsync("127.0.0", targetIps, device => discoveredDevices.Add(device), CustomUdpFactory, cts.Token));
        Assert.Null(ex);
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

    [Fact]
    public void ParseSsdpResponse_WithRealHuaweiPacket_ExtractsDeviceDetails()
    {
        string packet = "HTTP/1.1 200 OK\r\n" +
                        "CACHE-CONTROL: max-age=1800\r\n" +
                        "LOCATION: http://192.168.1.1:37215/desc.xml\r\n" +
                        "SERVER: Linux/3.0 UPnP/1.0 Huawei-HG8120C/1.0\r\n" +
                        "ST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n" +
                        "USN: uuid:upnp-InternetGatewayDevice-1_0::urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n\r\n";

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(packet);
        var device = SsdpDiscoveryMethod.ParseSsdpResponse("192.168.1.1", buffer);

        Assert.NotNull(device);
        Assert.Equal("192.168.1.1", device.IpAddress);
        Assert.Equal("Huawei", device.Vendor);
        Assert.Equal("Huawei HG8120C ONU", device.Hostname);
        Assert.Equal("Router / Gateway", device.DeviceType);
        Assert.Contains("http://192.168.1.1:37215/desc.xml", device.RawDetails);
        Assert.Contains("urn:schemas-upnp-org:device:InternetGatewayDevice:1", device.RawDetails);
        Assert.Contains("uuid:upnp-InternetGatewayDevice-1_0", device.RawDetails);
    }

    [Fact]
    public void ParseSsdpResponse_WithTpLinkPacket_ExtractsDeviceDetails()
    {
        string packet = "HTTP/1.1 200 OK\r\n" +
                        "LOCATION: http://192.168.0.1:1900/igd.xml\r\n" +
                        "SERVER: TP-Link Router OS 1.0\r\n\r\n";

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(packet);
        var device = SsdpDiscoveryMethod.ParseSsdpResponse("192.168.0.1", buffer);

        Assert.NotNull(device);
        Assert.Equal("TP-Link", device.Vendor);
        Assert.Equal("TP-Link Router", device.Hostname);
    }

    [Fact]
    public void ParseSsdpResponse_WithMikrotikPacket_ExtractsDeviceDetails()
    {
        string packet = "HTTP/1.1 200 OK\r\n" +
                        "SERVER: MikroTik RouterOS 7.10\r\n\r\n";

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(packet);
        var device = SsdpDiscoveryMethod.ParseSsdpResponse("10.0.0.1", buffer);

        Assert.NotNull(device);
        Assert.Equal("MikroTik", device.Vendor);
        Assert.Equal("MikroTik Router", device.Hostname);
    }

    [Fact]
    public void ParseSsdpResponse_WithoutServerOrLocation_ReturnsNull()
    {
        string packet = "HTTP/1.1 200 OK\r\n" +
                        "CACHE-CONTROL: max-age=1800\r\n\r\n";

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(packet);
        var device = SsdpDiscoveryMethod.ParseSsdpResponse("192.168.1.5", buffer);

        Assert.Null(device);
    }
}
