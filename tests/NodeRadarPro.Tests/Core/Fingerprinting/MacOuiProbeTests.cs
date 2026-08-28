using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

public class MacOuiProbeTests
{
    [Fact]
    public async Task ProbeAsync_WithKnownAppleMac_PopulatesVendor()
    {
        var probe = new MacOuiProbe();
        var node = new NetworkNode { MacAddress = "00:03:93:11:22:33" };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.Equal("MAC OUI Lookup", result.Source);
        Assert.True(result.RawData.ContainsKey("Vendor"));
        Assert.Equal("Apple", result.RawData["Vendor"]);
    }

    [Fact]
    public async Task ProbeAsync_WithRandomizedMac_IdentifiesPrivacyAddress()
    {
        var probe = new MacOuiProbe();
        var node = new NetworkNode { MacAddress = "DA:A1:19:22:33:44" }; // Second char 'A' is locally administered

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.True(result.RawData.ContainsKey("Vendor"));
        Assert.Equal("Randomized MAC (Mobile/Privacy)", result.RawData["Vendor"]);
    }

    [Fact]
    public async Task ProbeAsync_WithUnknownOrEmptyMac_ReturnsEmptyRawData()
    {
        var probe = new MacOuiProbe();
        var emptyNode = new NetworkNode { MacAddress = "" };
        var unknownNode = new NetworkNode { MacAddress = "Unknown" };

        var res1 = await probe.ProbeAsync(emptyNode, CancellationToken.None);
        var res2 = await probe.ProbeAsync(unknownNode, CancellationToken.None);

        Assert.Empty(res1.RawData);
        Assert.Empty(res2.RawData);
    }
}
