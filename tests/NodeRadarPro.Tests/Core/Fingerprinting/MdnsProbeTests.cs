using System.Text;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

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
    public void CreateMdnsQuery_FormatsDnsQuestionCorrectly()
    {
        string service = "_airplay._tcp.local";
        byte[] packet = MdnsProbe.CreateMdnsQuery(service);

        Assert.NotNull(packet);
        Assert.True(packet.Length > 12);
        // Questions count = 1 at offset 4-5
        Assert.Equal(0x00, packet[4]);
        Assert.Equal(0x01, packet[5]);

        // First label length for "_airplay" is 8
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

    [Fact]
    public void ExtractMdnsInstanceName_ValidLocalRecord_ExtractsHostname()
    {
        // Format: [prefix bytes] [name length] [name bytes] [0x05, 'l', 'o', 'c', 'a', 'l', 0x00]
        var bytes = new List<byte> { 0x00, 0x00, 0x00, 0x00 };
        string host = "Living-Room-AppleTV";
        bytes.Add((byte)host.Length);
        bytes.AddRange(Encoding.UTF8.GetBytes(host));
        bytes.AddRange(new byte[] { 0x05, (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'l', 0x00 });

        string extracted = MdnsProbe.ExtractMdnsInstanceName(bytes.ToArray());

        Assert.Equal(host, extracted);
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
