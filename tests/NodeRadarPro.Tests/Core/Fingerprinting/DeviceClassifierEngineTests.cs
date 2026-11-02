using NodeRadarPro.Core.Fingerprinting;

namespace NodeRadarPro.Tests.Core.Fingerprinting;

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValidModel_NullOrEmpty_ReturnsFalse(string? model)
    {
        Assert.False(DeviceClassifierEngine.IsValidModel(model!));
    }

    [Theory]
    [InlineData("iPhone 13 Pro")]
    [InlineData("Raspberry Pi 4 Model B")]
    [InlineData("DiskStation DS220+")]
    [InlineData("Cisco Catalyst 2960")]
    [InlineData("HP LaserJet Pro M404dn")]
    public void IsValidModel_ValidModelNames_ReturnsTrue(string model)
    {
        Assert.True(DeviceClassifierEngine.IsValidModel(model));
    }

    [Theory]
    [InlineData("HTTP/1.1 500 Internal Server Error")]
    [InlineData("HTTP/1.0 404 Not Found")]
    [InlineData("400 Bad Request")]
    [InlineData("403 Forbidden")]
    [InlineData("503 Service Unavailable")]
    [InlineData("Server: nginx/1.18.0")]
    [InlineData("apache/2.4.41")]
    [InlineData("lighttpd/1.4.55")]
    [InlineData("microhttpd")]
    [InlineData("RomPager/4.07 UPnP/1.0")]
    [InlineData("gws")]
    [InlineData("Error: Connection Refused")]
    [InlineData("Request Timed Out")]
    [InlineData("No route to host")]
    [InlineData("Too Many Connections")]
    [InlineData("Please try later")]
    public void IsValidModel_InvalidOrErrorStrings_ReturnsFalse(string model)
    {
        Assert.False(DeviceClassifierEngine.IsValidModel(model));
    }

    [Theory]
    [InlineData("Galaxy-S23")]
    [InlineData("Pixel-7-Pro")]
    [InlineData("Redmi-Note-11")]
    [InlineData("Poco-X5-Pro")]
    [InlineData("OnePlus-11")]
    [InlineData("Oppo-Find-X5")]
    [InlineData("Vivo-X90")]
    [InlineData("Realme-GT")]
    [InlineData("Motorola-Edge")]
    [InlineData("Nokia-G50")]
    [InlineData("Huawei-P50")]
    [InlineData("Honor-Magic-5")]
    [InlineData("Xiaomi-13")]
    [InlineData("Galaxy-Note-20")]
    [InlineData("Galaxy-A54")]
    public void Classify_MobilePhoneBrandHostnames_ClassifiedAsMobile(string mobileHostname)
    {
        var node = new NetworkNode { Hostname = mobileHostname };
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "DHCP Fingerprint",
                RawData = new Dictionary<string, string> { { "DhcpHostname", mobileHostname } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(DeviceTypeCategory.Mobile, result.Type);
        Assert.Equal("Mobile Phone", result.TypeString);
        Assert.Equal("Linux/Android", result.Os);
    }

    [Theory]
    [InlineData("Johns-iPhone", DeviceTypeCategory.Mobile, "Mobile Device")]
    [InlineData("Sarahs-iPad", DeviceTypeCategory.Mobile, "Mobile Device")]
    [InlineData("MacBook-Pro", DeviceTypeCategory.Desktop, "Apple Computer")]
    [InlineData("iMac-Studio", DeviceTypeCategory.Desktop, "Apple Computer")]
    public void Classify_AppleHostnames_ClassifiedCorrectly(string hostname, DeviceTypeCategory expectedCategory, string expectedTypeString)
    {
        var node = new NetworkNode { Hostname = hostname };
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "DHCP Fingerprint",
                RawData = new Dictionary<string, string> { { "DhcpHostname", hostname } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(expectedCategory, result.Type);
        Assert.Equal(expectedTypeString, result.TypeString);
        Assert.Equal("macOS/iOS", result.Os);
    }

    [Theory]
    [InlineData("DESKTOP-ABC12")]
    [InlineData("LAPTOP-XYZ89")]
    [InlineData("WINDOWS-DEV-PC")]
    public void Classify_WindowsHostnames_ClassifiedAsWindows(string hostname)
    {
        var node = new NetworkNode { Hostname = hostname };
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "DHCP Fingerprint",
                RawData = new Dictionary<string, string> { { "DhcpHostname", hostname } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(DeviceTypeCategory.Desktop, result.Type);
        Assert.Equal("Windows PC", result.TypeString);
        Assert.Equal("Windows", result.Os);
    }

    [Theory]
    [InlineData("iOS 16.5", "macOS/iOS")]
    [InlineData("Android 13 / Linux", "Linux/Android")]
    [InlineData("Windows 11 / Server 2022", "Windows")]
    [InlineData("Linux 5.15", "Linux/Android")]
    [InlineData("Cisco VoIP Phone", "Infrastructure")]
    public void Classify_DhcpOsSignatures_ClassifiedCorrectly(string dhcpOs, string expectedOs)
    {
        var node = new NetworkNode();
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "DHCP Fingerprint",
                RawData = new Dictionary<string, string> { { "DhcpOsFingerprint", dhcpOs } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(expectedOs, result.Os);
    }

    [Theory]
    [InlineData("MSFT 5.0", "Windows")]
    [InlineData("android-dhcp-13", "Linux/Android")]
    [InlineData("apple-ios", "macOS/iOS")]
    [InlineData("dhcpc:darwin", "macOS/iOS")]
    public void Classify_DhcpVendorClass_ClassifiedCorrectly(string vendorClass, string expectedOs)
    {
        var node = new NetworkNode();
        var probes = new List<ProbeResult>
        {
            new ProbeResult
            {
                Source = "DHCP Fingerprint",
                RawData = new Dictionary<string, string> { { "DhcpVendorClass", vendorClass } }
            }
        };

        var result = DeviceClassifierEngine.Classify(node, probes);

        Assert.Equal(expectedOs, result.Os);
    }
}
