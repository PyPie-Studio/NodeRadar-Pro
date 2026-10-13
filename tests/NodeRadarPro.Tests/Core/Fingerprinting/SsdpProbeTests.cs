using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

[Collection("ProbeSweepStaticState")]
public class SsdpProbeTests : IDisposable
{
    public SsdpProbeTests()
    {
        SsdpProbe.ClearCacheForTesting();
    }

    public void Dispose()
    {
        SsdpProbe.ClearCacheForTesting();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Properties_ReturnExpectedMetadata()
    {
        var probe = new SsdpProbe();
        Assert.Equal("SSDP / UPnP", probe.Name);
        Assert.Equal(30, probe.Priority);
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
    [InlineData("http://192.168.1.50/description.xml", "not-an-ip", false)]
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
    public void ExtractXmlValue_CaseInsensitiveTag_ExtractsValue()
    {
        string xml = "<root><FRIENDLYNAME>Bedroom Speaker</FRIENDLYNAME></root>";
        Assert.Equal("Bedroom Speaker", SsdpProbe.ExtractXmlValue(xml, "friendlyName"));
    }

    [Fact]
    public void ExtractXmlValue_MissingTag_ReturnsEmptyString()
    {
        string xml = "<root><deviceType>urn:schemas-upnp-org:device:MediaRenderer:1</deviceType></root>";
        Assert.Equal(string.Empty, SsdpProbe.ExtractXmlValue(xml, "manufacturer"));
    }

    [Fact]
    public void ExtractXmlValue_MissingClosingTag_ReturnsEmptyString()
    {
        string xml = "<root><friendlyName>Broken Tag Target";
        Assert.Equal(string.Empty, SsdpProbe.ExtractXmlValue(xml, "friendlyName"));
    }

    [Fact]
    public void ExtractXmlValue_NullOrEmptyXml_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, SsdpProbe.ExtractXmlValue("", "friendlyName"));
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

    [Fact]
    public async Task ProbeAsync_UncachedDevice_ReturnsEmptyRawData()
    {
        var probe = new SsdpProbe();
        var node = new NetworkNode { IpAddress = "10.0.0.99" };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.Equal("SSDP / UPnP", result.Source);
        Assert.Empty(result.RawData);
    }

    [Fact]
    public async Task ProbeAsync_PartiallyPopulatedData_OnlyIncludesNonEmptyFields()
    {
        SsdpProbe.InjectCacheForTesting("192.168.1.60", new SsdpProbe.SsdpData
        {
            Server = "CustomServer/1.0",
            FriendlyName = "",
            Manufacturer = "ACME",
            ModelName = "",
            DeviceType = ""
        });

        var probe = new SsdpProbe();
        var node = new NetworkNode { IpAddress = "192.168.1.60" };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.Equal("SSDP / UPnP", result.Source);
        Assert.Equal(2, result.RawData.Count);
        Assert.Equal("CustomServer/1.0", result.RawData["Server"]);
        Assert.Equal("ACME", result.RawData["Manufacturer"]);
        Assert.False(result.RawData.ContainsKey("FriendlyName"));
        Assert.False(result.RawData.ContainsKey("ModelName"));
        Assert.False(result.RawData.ContainsKey("DeviceType"));
    }

    [Fact]
    public async Task StartSweepAsync_CancelledToken_ExitsGracefully()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exception = await Record.ExceptionAsync(() => SsdpProbe.StartSweepAsync(cts.Token));
        Assert.Null(exception);
    }
}
