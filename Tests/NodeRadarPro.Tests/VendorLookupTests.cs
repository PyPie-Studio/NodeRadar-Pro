using System;
using Xunit;
using NodeRadarPro.Data;

namespace NodeRadarPro.Tests;

public class VendorLookupTests
{
    [Theory]
    [InlineData("00:50:56:00:00:00", "VMware")]
    [InlineData("00:50:56:C0:00:01", "VMware")]
    [InlineData("48:5B:39:XX:XX:XX", "Realtek")] // Broadcom / Realtek
    [InlineData("00:1E:AC:11:22:33", "Oppo")]
    [InlineData("F0:79:59:AA:BB:CC", "ASUS")]
    [InlineData("FC:FB:FB:12:34:56", "Cisco")]
    public void GetVendor_ShouldReturnCorrectVendor_ForKnownMac(string mac, string expectedVendor)
    {
        var result = VendorLookup.GetVendor(mac);
        Assert.Equal(expectedVendor, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00:50")] // Too short
    [InlineData("00:50:5")] // Too short
    public void GetVendor_ShouldReturnUnknownVendor_ForInvalidMac(string? mac)
    {
        var result = VendorLookup.GetVendor(mac!);
        Assert.Equal("Unknown Vendor", result);
    }

    [Theory]
    [InlineData("02:00:00:00:00:00")]
    [InlineData("06:11:22:33:44:55")]
    [InlineData("0A:AA:BB:CC:DD:EE")]
    [InlineData("0E:FF:FF:FF:FF:FF")]
    [InlineData("0a:00:00:00:00:00")] // Lowercase test
    [InlineData("12:34:56:78:90:AB")] // Random MAC test
    [InlineData("F6:EE:DD:CC:BB:AA")] // Another random MAC
    public void GetVendor_ShouldReturnRandomizedMac_ForLocallyAdministeredMac(string mac)
    {
        var result = VendorLookup.GetVendor(mac);
        Assert.Equal("Randomized MAC (Mobile/Privacy)", result);
    }

    [Fact]
    public void GetVendor_ShouldReturnUnknownVendor_ForUnknownMac()
    {
        // Not locally administered, and not in the dictionary
        var result = VendorLookup.GetVendor("00:00:00:00:00:00");
        Assert.Equal("Unknown Vendor", result);
    }

    [Theory]
    [InlineData("randomized mac", "iphone-13", "iPhone")]
    [InlineData("privacy", "some-ipad", "iPad")]
    [InlineData("Randomized MAC", "pixel 6", "Android Phone")]
    [InlineData("Randomized MAC", "unknown-device", "Mobile Device")]
    [InlineData("cisco", "switch-01", "Router/Network")]
    [InlineData("ubiquiti", "ap-02", "Router/Network")]
    [InlineData("unknown vendor", "home-router", "Router/Network")]
    [InlineData("apple", "johns-iphone", "iPhone")]
    [InlineData("apple", "marys-ipad", "iPad")]
    [InlineData("apple", "apple watch", "Apple Watch")]
    [InlineData("apple", "macbook-pro", "Mac")]
    [InlineData("apple", "unknown", "Mobile (Apple)")]
    [InlineData("samsung", "living-room-tv", "Smart TV")]
    [InlineData("samsung", "galaxy-s22", "Mobile Phone")]
    [InlineData("sony", "ps5", "PlayStation")]
    [InlineData("nintendo", "switch", "Nintendo Console")]
    [InlineData("microsoft", "xbox-one", "Xbox")]
    [InlineData("microsoft", "desktop-pc", "PC / Windows")]
    [InlineData("amazon", "fire-stick", "Fire TV")]
    [InlineData("amazon", "echo-dot", "Echo / Alexa")]
    [InlineData("synology", "diskstation", "NAS Storage")]
    [InlineData("sonos", "living-room", "Audio Speaker")]
    [InlineData("roku", "bedroom-tv", "Smart TV")]
    [InlineData("hikvision", "cam-01", "IP Camera")]
    [InlineData("vmware", "win-server", "Virtual Machine")]
    [InlineData("dell", "work-laptop", "Workstation")]
    [InlineData("intel", "unknown", "Computer")]
    [InlineData("unknown vendor", "unknown", "Network Device")]
    public void GuessDeviceType_ShouldReturnExpectedType(string vendor, string hostname, string expectedType)
    {
        var result = VendorLookup.GuessDeviceType(vendor, hostname);
        Assert.Equal(expectedType, result);
    }
}
