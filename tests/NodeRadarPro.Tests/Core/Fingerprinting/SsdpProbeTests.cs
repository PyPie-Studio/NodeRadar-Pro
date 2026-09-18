using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

public class SsdpProbeTests : IDisposable
{
    public SsdpProbeTests()
    {
        SsdpProbe.ClearCacheForTesting();
    }

    public void Dispose()
    {
        SsdpProbe.ClearCacheForTesting();
    }


    [Theory]
    [InlineData("http://192.168.1.50/description.xml", "192.168.1.50", true)]
    [InlineData("https://192.168.1.50:8080/description.xml", "192.168.1.50", true)]
    [InlineData("http://192.168.1.50:1900/rootDesc.xml", "192.168.1.50", true)]
    [InlineData("http://192.168.1.100/description.xml", "192.168.1.50", false)]
    [InlineData("http://127.0.0.1/description.xml", "192.168.1.50", false)]
    [InlineData("http://169.254.169.254/latest/meta-data/", "192.168.1.50", false)]
    [InlineData("ftp://192.168.1.50/description.xml", "192.168.1.50", false)]
    [InlineData("file:///etc/passwd", "192.168.1.50", false)]
    [InlineData("gopher://192.168.1.50/", "192.168.1.50", false)]
    [InlineData("http://example.com/description.xml", "192.168.1.50", false)]
    [InlineData("invalid-url", "192.168.1.50", false)]
    [InlineData("", "192.168.1.50", false)]
    [InlineData("http://192.168.1.50/description.xml", "", false)]
    public void IsValidSsdpLocationUrl_ValidationScenarios(string url, string senderIp, bool expected)
    {
        bool result = SsdpProbe.IsValidSsdpLocationUrl(url, senderIp);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractXmlValue_ValidTag_ReturnsTrimmedValue()
    {
        string xml = "<root><friendlyName> Living Room TV </friendlyName><modelName>OLED55</modelName></root>";

        Assert.Equal("Living Room TV", SsdpProbe.ExtractXmlValue(xml, "friendlyName"));
        Assert.Equal("OLED55", SsdpProbe.ExtractXmlValue(xml, "modelName"));
    }

    [Fact]
    public void ExtractXmlValue_MissingTag_ReturnsEmptyString()
    {
        string xml = "<root><deviceType>urn:schemas-upnp-org:device:MediaRenderer:1</deviceType></root>";
        Assert.Equal(string.Empty, SsdpProbe.ExtractXmlValue(xml, "manufacturer"));
    }

    [Fact]
    public async Task ProbeAsync_CachedDevice_PopulatesRawData()
    {
        // Arrange
        SsdpProbe.InjectCacheForTesting("192.168.1.55", new SsdpProbe.SsdpData
        {
            Server = "Linux/3.14 UPnP/1.0 Sonos/60.1-80120",
            FriendlyName = "Kitchen Sonos",
            Manufacturer = "Sonos, Inc.",
            ModelName = "Play:1",
            DeviceType = "urn:schemas-upnp-org:device:ZonePlayer:1"
        });

        var probe = new SsdpProbe();
        var node = new NetworkNode { IpAddress = "192.168.1.55" };

        // Act
        var result = await probe.ProbeAsync(node, CancellationToken.None);

        // Assert
        Assert.Equal("SSDP / UPnP", result.Source);
        Assert.Equal("Linux/3.14 UPnP/1.0 Sonos/60.1-80120", result.RawData["Server"]);
        Assert.Equal("Kitchen Sonos", result.RawData["FriendlyName"]);
        Assert.Equal("Sonos, Inc.", result.RawData["Manufacturer"]);
        Assert.Equal("Play:1", result.RawData["ModelName"]);
        Assert.Equal("urn:schemas-upnp-org:device:ZonePlayer:1", result.RawData["DeviceType"]);
    }
}
