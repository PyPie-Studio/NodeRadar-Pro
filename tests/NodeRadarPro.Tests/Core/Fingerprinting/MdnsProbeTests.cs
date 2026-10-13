using System.Text;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

[Collection("ProbeSweepStaticState")]
public class MdnsProbeTests : IDisposable
{
    public MdnsProbeTests()
    {
        MdnsProbe.ClearCacheForTesting();
    }

    public void Dispose()
    {
        MdnsProbe.ClearCacheForTesting();
    }

    [Fact]
    public void Properties_ReturnExpectedValues()
    {
        var probe = new MdnsProbe();
        Assert.Equal("mDNS / Bonjour", probe.Name);
        Assert.Equal(20, probe.Priority);
    }

    [Fact]
    public async Task ProbeAsync_UncachedNode_ReturnsEmptyRawData()
    {
        var probe = new MdnsProbe();
        var node = new NetworkNode { IpAddress = "10.0.0.99" };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.Equal("mDNS / Bonjour", result.Source);
        Assert.Empty(result.RawData);
    }

    [Fact]
    public async Task ProbeAsync_WithCachedDataAndDuplicates_FormatsDistinctServices()
    {
        var mdnsData = new MdnsProbe.MdnsData
        {
            InstanceName = "Living Room TV",
            Model = "OLED55",
            Manufacturer = "LG"
        };
        mdnsData.Services.Add("Google Cast");
        mdnsData.Services.Add("Apple Device");
        mdnsData.Services.Add("Google Cast");

        MdnsProbe.InjectCacheForTesting("192.168.1.50", mdnsData);

        var probe = new MdnsProbe();
        var node = new NetworkNode { IpAddress = "192.168.1.50" };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.Equal("mDNS / Bonjour", result.Source);
        Assert.Equal("Living Room TV", result.RawData["InstanceName"]);
        Assert.Equal("OLED55", result.RawData["Model"]);
        Assert.Equal("LG", result.RawData["Manufacturer"]);
        Assert.Equal("Google Cast, Apple Device", result.RawData["Services"]);
    }

    [Fact]
    public void CreateMdnsQuery_FormatsDnsQuestionCorrectly()
    {
        string service = "_airplay._tcp.local";
        byte[] packet = MdnsProbe.CreateMdnsQuery(service);

        Assert.NotNull(packet);
        Assert.True(packet.Length > 12);
        Assert.Equal(0x00, packet[4]);
        Assert.Equal(0x01, packet[5]);

        Assert.Equal(8, packet[12]);
        string label1 = Encoding.ASCII.GetString(packet, 13, 8);
        Assert.Equal("_airplay", label1);
    }

    [Fact]
    public void ParseTxtRecords_ExtractsModelAndManufacturer()
    {
        var data = new MdnsProbe.MdnsData();
        string txt = "\u000fmodel=MacBookPro18,1\u0010manufacturer=Apple Inc.";
        byte[] txtBytes = Encoding.UTF8.GetBytes(txt);

        MdnsProbe.ParseTxtRecords(txtBytes, data);

        Assert.Equal("MacBookPro18,1", data.Model);
        Assert.Equal("Apple Inc.", data.Manufacturer);
    }

    [Theory]
    [InlineData("am=AppleTV5,3", "AppleTV5,3", "")]
    [InlineData("md=Chromecast", "Chromecast", "")]
    [InlineData("rpMd=HomePod", "HomePod", "")]
    [InlineData("man=Sonos", "", "Sonos")]
    [InlineData("mf=Samsung", "", "Samsung")]
    public void ParseTxtRecords_VariousModelAndVendorKeys_ExtractsCorrectly(string txt, string expectedModel, string expectedVendor)
    {
        var data = new MdnsProbe.MdnsData();
        byte[] txtBytes = Encoding.UTF8.GetBytes(txt);

        MdnsProbe.ParseTxtRecords(txtBytes, data);

        Assert.Equal(expectedModel, data.Model);
        Assert.Equal(expectedVendor, data.Manufacturer);
    }

    [Fact]
    public void ParseTxtRecords_StopsAtNonPrintableCharacter()
    {
        var data = new MdnsProbe.MdnsData();
        byte[] prefix = Encoding.UTF8.GetBytes("model=ProductModel");
        byte[] suffix = Encoding.UTF8.GetBytes("extra");
        var bytes = new List<byte>(prefix);
        bytes.Add(0x00);
        bytes.AddRange(suffix);

        MdnsProbe.ParseTxtRecords(bytes.ToArray(), data);

        Assert.Equal("ProductModel", data.Model);
    }

    [Fact]
    public void ParseTxtRecords_HandlesInvalidBufferGracefully()
    {
        var data = new MdnsProbe.MdnsData();
        MdnsProbe.ParseTxtRecords(Array.Empty<byte>(), data);
        Assert.Equal(string.Empty, data.Model);
    }

    [Fact]
    public void ExtractMdnsInstanceName_ValidLocalRecord_ExtractsHostname()
    {
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        string host = "Living-Room-AppleTV";
        bytes.Add((byte)host.Length);
        bytes.AddRange(Encoding.UTF8.GetBytes(host));
        bytes.AddRange(new byte[] { 0x05, (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'l', 0x00 });

        string extracted = MdnsProbe.ExtractMdnsInstanceName(bytes.ToArray());

        Assert.Equal(host, extracted);
    }

    [Fact]
    public void ExtractMdnsInstanceName_ServiceTypeStartingWithUnderscore_ReturnsEmpty()
    {
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        string host = "_googlecast";
        bytes.Add((byte)host.Length);
        bytes.AddRange(Encoding.UTF8.GetBytes(host));
        bytes.AddRange(new byte[] { 0x05, (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'l', 0x00 });

        string extracted = MdnsProbe.ExtractMdnsInstanceName(bytes.ToArray());

        Assert.Equal(string.Empty, extracted);
    }

    [Fact]
    public void ExtractMdnsInstanceName_ShortHostName_ReturnsEmpty()
    {
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        string host = "ab";
        bytes.Add((byte)host.Length);
        bytes.AddRange(Encoding.UTF8.GetBytes(host));
        bytes.AddRange(new byte[] { 0x05, (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'l', 0x00 });

        string extracted = MdnsProbe.ExtractMdnsInstanceName(bytes.ToArray());

        Assert.Equal(string.Empty, extracted);
    }

    [Fact]
    public void ExtractMdnsInstanceName_NoLocalPattern_ReturnsEmpty()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("Random data without mdns pattern");
        string extracted = MdnsProbe.ExtractMdnsInstanceName(bytes);
        Assert.Equal(string.Empty, extracted);
    }

    [Fact]
    public void ExtractMdnsInstanceName_HandlesEmptyOrSmallBufferGracefully()
    {
        Assert.Equal(string.Empty, MdnsProbe.ExtractMdnsInstanceName(Array.Empty<byte>()));
        Assert.Equal(string.Empty, MdnsProbe.ExtractMdnsInstanceName(new byte[] { 0x01, 0x02 }));
    }

    [Fact]
    public async Task ProbeAsync_WithCachedData_PopulatesResult()
    {
        var mdnsData = new MdnsProbe.MdnsData
        {
            InstanceName = "Office-Printer",
            Model = "HP LaserJet Pro MFP",
            Manufacturer = "HP"
        };
        mdnsData.Services.Add("_ipp._tcp.local");
        MdnsProbe.InjectCacheForTesting("192.168.1.100", mdnsData);

        var probe = new MdnsProbe();
        var node = new NetworkNode { IpAddress = "192.168.1.100" };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.Equal("mDNS / Bonjour", result.Source);
        Assert.Equal("Office-Printer", result.RawData["InstanceName"]);
        Assert.Equal("HP LaserJet Pro MFP", result.RawData["Model"]);
        Assert.Equal("HP", result.RawData["Manufacturer"]);
        Assert.Equal("_ipp._tcp.local", result.RawData["Services"]);
    }
}
