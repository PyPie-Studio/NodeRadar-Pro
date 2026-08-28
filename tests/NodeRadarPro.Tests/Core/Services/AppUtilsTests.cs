using System.Net;
using System.Net.NetworkInformation;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class AppUtilsTests
{
    [Fact]
    public void GetMacAddress_WithValidInterface_ReturnsFormattedMac()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "eth0",
                OperationalStatus = OperationalStatus.Up,
                NetworkInterfaceType = NetworkInterfaceType.Ethernet,
                PhysicalAddressBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 }
            }
        };

        string mac = AppUtils.GetMacAddress(() => interfaces);
        Assert.Equal("00:11:22:33:44:55", mac);
    }

    [Fact]
    public void GetMacAddress_WithEmptyInterfaces_ReturnsUnknown()
    {
        string mac = AppUtils.GetMacAddress(() => new List<NetworkInterfaceInfo>());
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetMacAddress_IgnoresDownInterfaces()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "eth0",
                OperationalStatus = OperationalStatus.Down,
                NetworkInterfaceType = NetworkInterfaceType.Ethernet,
                PhysicalAddressBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 }
            }
        };

        string mac = AppUtils.GetMacAddress(() => interfaces);
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetMacAddress_IgnoresLoopbackInterfaces()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "lo",
                OperationalStatus = OperationalStatus.Up,
                NetworkInterfaceType = NetworkInterfaceType.Loopback,
                PhysicalAddressBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 }
            }
        };

        string mac = AppUtils.GetMacAddress(() => interfaces);
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetMacAddress_IgnoresAllZeroMacAddresses()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "eth0",
                OperationalStatus = OperationalStatus.Up,
                NetworkInterfaceType = NetworkInterfaceType.Ethernet,
                PhysicalAddressBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
            }
        };

        string mac = AppUtils.GetMacAddress(() => interfaces);
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetMacAddress_WhenExceptionThrown_ReturnsUnknown()
    {
        string mac = AppUtils.GetMacAddress(() => throw new InvalidOperationException("Provider error"));
        Assert.Equal("Unknown", mac);
    }

    [Fact]
    public void GetLocalIpAddress_WithValidInterface_ReturnsIp()
    {
        var interfaces = new List<NetworkInterfaceInfo>
        {
            new NetworkInterfaceInfo
            {
                Name = "eth0",
                OperationalStatus = OperationalStatus.Up,
                NetworkInterfaceType = NetworkInterfaceType.Ethernet,
                UnicastAddresses = new List<IPAddress> { IPAddress.Parse("192.168.1.100") }
            }
        };

        string ip = AppUtils.GetLocalIpAddress(() => interfaces);
        Assert.Equal("192.168.1.100", ip);
    }

    [Fact]
    public void GetLocalIpAddress_WithEmptyInterfaces_ReturnsLoopback()
    {
        string ip = AppUtils.GetLocalIpAddress(() => new List<NetworkInterfaceInfo>());
        Assert.Equal("127.0.0.1", ip);
    }

    [Fact]
    public void GetLocalIpAddress_WhenExceptionThrown_ReturnsLoopback()
    {
        string ip = AppUtils.GetLocalIpAddress(() => throw new InvalidOperationException("Provider error"));
        Assert.Equal("127.0.0.1", ip);
    }

    [Fact]
    public void GetMacAddress_DefaultProvider_ExecutesWithoutThrowing()
    {
        string mac = AppUtils.GetMacAddress();
        Assert.NotNull(mac);
    }

    [Fact]
    public void GetLocalIpAddress_DefaultProvider_ExecutesWithoutThrowing()
    {
        string ip = AppUtils.GetLocalIpAddress();
        Assert.NotNull(ip);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("javascript:alert(1)")]
    public void OpenSafeUrl_InvalidOrNonHttpUrl_DoesNotThrow(string? invalidUrl)
    {
        var exception = Record.Exception(() => AppUtils.OpenSafeUrl(invalidUrl!));
        Assert.Null(exception);
    }

    [Fact]
    public void OpenSafeUrl_ValidHttpUrl_DoesNotThrow()
    {
        var exception = Record.Exception(() => AppUtils.OpenSafeUrl("https://example.com"));
        Assert.Null(exception);
    }
}
