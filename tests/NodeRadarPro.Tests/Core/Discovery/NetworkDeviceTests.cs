using NodeRadarPro.Core.Discovery;

namespace NodeRadarPro.Tests;

public class NetworkDeviceTests
{
    [Fact]
    public void Constructor_DefaultValues_AreSetCorrectly()
    {
        // Act
        var device = new NetworkDevice();

        // Assert
        Assert.Equal(string.Empty, device.IpAddress);
        Assert.Equal("Unknown", device.MacAddress);
        Assert.Equal("Unknown Device", device.Hostname);
        Assert.Equal("Unknown Vendor", device.Vendor);
        Assert.Equal("Generic Device", device.DeviceType);
        Assert.Equal(string.Empty, device.SourceProtocol);
        Assert.Equal(string.Empty, device.RawDetails);
        Assert.False(device.IsOnline);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var device = new NetworkDevice
        {
            IpAddress = "192.168.1.100",
            MacAddress = "00:11:22:33:44:55",
            Hostname = "test-host",
            Vendor = "Test Vendor",
            DeviceType = "Printer",
            SourceProtocol = "mDNS",
            RawDetails = "service=printer",
            IsOnline = true
        };

        // Assert
        Assert.Equal("192.168.1.100", device.IpAddress);
        Assert.Equal("00:11:22:33:44:55", device.MacAddress);
        Assert.Equal("test-host", device.Hostname);
        Assert.Equal("Test Vendor", device.Vendor);
        Assert.Equal("Printer", device.DeviceType);
        Assert.Equal("mDNS", device.SourceProtocol);
        Assert.Equal("service=printer", device.RawDetails);
        Assert.True(device.IsOnline);
    }

    [Theory]
    [InlineData("10.0.0.1", "AA:BB:CC:DD:EE:FF", "router.local", "Cisco", "Router", "SSDP", "location=http://10.0.0.1/desc.xml", true)]
    [InlineData("172.16.0.5", "11:22:33:44:55:66", "nas.local", "Synology", "NAS", "ARP", "ip=172.16.0.5", false)]
    public void NetworkDevice_ObjectInitializer_SetsAllFields(
        string ipAddress,
        string macAddress,
        string hostname,
        string vendor,
        string deviceType,
        string sourceProtocol,
        string rawDetails,
        bool isOnline)
    {
        // Act
        var device = new NetworkDevice
        {
            IpAddress = ipAddress,
            MacAddress = macAddress,
            Hostname = hostname,
            Vendor = vendor,
            DeviceType = deviceType,
            SourceProtocol = sourceProtocol,
            RawDetails = rawDetails,
            IsOnline = isOnline
        };

        // Assert
        Assert.Equal(ipAddress, device.IpAddress);
        Assert.Equal(macAddress, device.MacAddress);
        Assert.Equal(hostname, device.Hostname);
        Assert.Equal(vendor, device.Vendor);
        Assert.Equal(deviceType, device.DeviceType);
        Assert.Equal(sourceProtocol, device.SourceProtocol);
        Assert.Equal(rawDetails, device.RawDetails);
        Assert.Equal(isOnline, device.IsOnline);
    }
}
