using NodeRadarPro.Data;

namespace NodeRadarPro.Tests;

public class VendorLookupTests
{
    [Fact]
    public void GetVendor_KnownMacAddress_ReturnsCorrectVendor()
    {
        // Apple
        Assert.Equal("Apple", VendorLookup.GetVendor("00:03:93:11:22:33"));
        Assert.Equal("Apple", VendorLookup.GetVendor("00-03-93-11-22-33")); // Should handle dashes

        // Google
        Assert.Equal("Google", VendorLookup.GetVendor("A4:77:33:44:55:66"));

        // Raspberry Pi Foundation
        Assert.Equal("Raspberry Pi Foundation", VendorLookup.GetVendor("B8:27:EB:AA:BB:CC"));
    }

    [Fact]
    public void GetVendor_UnknownMacAddress_ReturnsUnknownVendor()
    {
        Assert.Equal("Unknown Vendor", VendorLookup.GetVendor("FF:FF:FF:FF:FF:FF"));
        Assert.Equal("Unknown Vendor", VendorLookup.GetVendor("11:22:33:44:55:66"));
    }

    [Fact]
    public void GetVendor_NullOrEmptyMacAddress_ReturnsUnknownVendor()
    {
        Assert.Equal("Unknown Vendor", VendorLookup.GetVendor(null!));
        Assert.Equal("Unknown Vendor", VendorLookup.GetVendor(""));
        Assert.Equal("Unknown Vendor", VendorLookup.GetVendor("  "));
    }

    [Fact]
    public void GetVendor_InvalidFormat_HandlesGracefully()
    {
        Assert.Equal("Unknown Vendor", VendorLookup.GetVendor("invalid"));
        Assert.Equal("Unknown Vendor", VendorLookup.GetVendor("12:34")); // Too short
    }

    [Theory]
    [InlineData("02:00:00:00:00:00")]
    [InlineData("12:00:00:00:00:00")] // Last digit '2'
    [InlineData("06:00:00:00:00:00")]
    [InlineData("16:00:00:00:00:00")] // Last digit '6'
    [InlineData("0A:00:00:00:00:00")]
    [InlineData("0E:00:00:00:00:00")]
    [InlineData("0a:00:00:00:00:00")]
    [InlineData("0e:00:00:00:00:00")]
    public void GetVendor_RandomizedMacAddress_ReturnsRandomizedMessage(string mac)
    {
        Assert.Equal("Randomized MAC (Mobile/Privacy)", VendorLookup.GetVendor(mac));
    }

#pragma warning disable CS0618
    [Theory]
    [InlineData("Apple", "iPhone")]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("Cisco", null)]
    [InlineData(null, "Switch")]
    [InlineData("", "Router")]
    [InlineData("Netgear", "")]
    [InlineData("Custom@Vendor!#$", "Host_123--Name")]
    [InlineData("VeryLongVendorNameThatExceedsNormalLengthLimitsToTestBoundaryHandling", "VeryLongHostNameThatExceedsNormalLengthLimitsToTestBoundaryHandling")]
    public void GuessDeviceType_WithVariousEdgeCaseInputs_ReturnsGenericNetworkDevice(string? vendor, string? hostname)
    {
        Assert.Equal("Generic Network Device", VendorLookup.GuessDeviceType(vendor!, hostname!));
    }
#pragma warning restore CS0618
}
