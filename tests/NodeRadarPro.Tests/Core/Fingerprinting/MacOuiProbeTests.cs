using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

public class MacOuiProbeTests
{
    [Fact]
    public void ProbeProperties_ReturnExpectedValues()
    {
        var probe = new MacOuiProbe();
        Assert.Equal("MAC OUI Lookup", probe.Name);
        Assert.Equal(10, probe.Priority);
    }

    [Theory]
    [InlineData("00:03:93:11:22:33", "Apple")]
    [InlineData("00-03-93-11-22-33", "Apple")]
    [InlineData("00:00:F0:11:22:33", "Samsung")]
    [InlineData("48:00:B3:11:22:33", "Cisco")]
    [InlineData("B8:27:EB:11:22:33", "Raspberry Pi Foundation")]
    public async Task ProbeAsync_WithKnownMac_PopulatesVendor(string macAddress, string expectedVendor)
    {
        var probe = new MacOuiProbe();
        var node = new NetworkNode { MacAddress = macAddress };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.Equal("MAC OUI Lookup", result.Source);
        Assert.True(result.RawData.ContainsKey("Vendor"));
        Assert.Equal(expectedVendor, result.RawData["Vendor"]);
    }

    [Theory]
    [InlineData("02:00:00:11:22:33")]
    [InlineData("12:00:00:11:22:33")]
    [InlineData("06:00:00:11:22:33")]
    [InlineData("16:00:00:11:22:33")]
    [InlineData("0A:00:00:11:22:33")]
    [InlineData("0E:00:00:11:22:33")]
    [InlineData("0a:00:00:11:22:33")]
    [InlineData("0e:00:00:11:22:33")]
    public async Task ProbeAsync_WithRandomizedMac_IdentifiesPrivacyAddress(string macAddress)
    {
        var probe = new MacOuiProbe();
        var node = new NetworkNode { MacAddress = macAddress };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.True(result.RawData.ContainsKey("Vendor"));
        Assert.Equal("Randomized MAC (Mobile/Privacy)", result.RawData["Vendor"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown")]
    [InlineData("00:01")]
    [InlineData("00:03:9")]
    public async Task ProbeAsync_WithInvalidOrEmptyMac_ReturnsEmptyRawData(string? macAddress)
    {
        var probe = new MacOuiProbe();
        var node = new NetworkNode { MacAddress = macAddress! };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.Empty(result.RawData);
    }

    [Fact]
    public async Task ProbeAsync_WithUnknownVendorMac_DoesNotAddVendorKey()
    {
        var probe = new MacOuiProbe();
        var node = new NetworkNode { MacAddress = "FF:FF:FF:FF:FF:FF" };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.False(result.RawData.ContainsKey("Vendor"));
    }
}
