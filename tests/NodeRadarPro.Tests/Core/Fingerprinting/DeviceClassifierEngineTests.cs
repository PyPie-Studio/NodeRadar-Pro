using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting;

namespace NodeRadarPro.Tests.Fingerprinting;

public class DeviceClassifierEngineTests
{
    [Fact]
    public void Classify_WithEmptyInputs_ReturnsGenericDevice()
    {
        var node = new NetworkNode();
        var probes = new List<ProbeResult>();

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(DeviceTypeCategory.Unknown, result.Type);
        Assert.Equal("Generic Device", result.TypeString);
        Assert.Equal(0, result.ConfidenceScore);
    }

    [Fact]
    public void Classify_WithAppleVendor_ReturnsMacOrIOS()
    {
        var node = new NetworkNode();
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "MAC OUI Lookup",
                RawData = new Dictionary<string, string> { { "Vendor", "Apple, Inc." } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(DeviceTypeCategory.Desktop, result.Type);
        Assert.Equal("Apple Computer", result.TypeString);
        Assert.Equal("macOS/iOS", result.Os);
        Assert.True(result.ConfidenceScore > 0);
    }

    [Fact]
    public void Classify_WithWindowsKeywords_ReturnsWindowsPC()
    {
        var node = new NetworkNode { Hostname = "DESKTOP-ABC" };
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "MAC OUI Lookup",
                RawData = new Dictionary<string, string> { { "Vendor", "Microsoft Corporation" } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(DeviceTypeCategory.Desktop, result.Type);
        Assert.Equal("Windows PC", result.TypeString);
        Assert.Equal("Windows", result.Os);
        Assert.True(result.ConfidenceScore > 0);
    }

    [Fact]
    public void Classify_WithPrinterPorts_ReturnsPrinter()
    {
        var node = new NetworkNode { OpenPorts = new List<int> { 9100 } };
        var probes = new List<ProbeResult>();

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(DeviceTypeCategory.Printer, result.Type);
        Assert.Equal("Network Printer", result.TypeString);
        Assert.Equal("Printer", result.Os);
    }

    [Fact]
    public void Classify_WithSynologyVendor_ReturnsNas()
    {
        var node = new NetworkNode();
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "MAC OUI Lookup",
                RawData = new Dictionary<string, string> { { "Vendor", "Synology" } }
            },
            new ProbeResult
            {
                Source = "Banner",
                RawData = new Dictionary<string, string> { { "Banner", "Synology" } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(DeviceTypeCategory.NAS, result.Type);
    }

    [Fact]
    public void Classify_WithInfrastructure_ReturnsRouter()
    {
        var node = new NetworkNode();
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "MAC OUI Lookup",
                RawData = new Dictionary<string, string> { { "Vendor", "Ubiquiti" } }
            },
            new ProbeResult
            {
                Source = "mDNS",
                RawData = new Dictionary<string, string> { { "Service", "routeros" } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(DeviceTypeCategory.Router, result.Type);
        Assert.Equal("Infrastructure", result.Os);
    }
}
