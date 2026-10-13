using System.Text;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

public class NbnsProbeTests
{
    [Fact]
    public void Properties_ReturnExpectedValues()
    {
        var probe = new NbnsProbe();
        Assert.Equal("NetBIOS (NBNS)", probe.Name);
        Assert.Equal(35, probe.Priority);
    }

    [Fact]
    public void ParseNodeStatusResponse_ShortBuffer_DoesNotThrowOrPopulate()
    {
        var result = new ProbeResult { Source = "NetBIOS (NBNS)" };
        byte[] shortBuf = new byte[30];

        NbnsProbe.ParseNodeStatusResponse(shortBuf, result);

        Assert.Empty(result.RawData);
    }

    [Fact]
    public void ParseNodeStatusResponse_ValidFrame_ExtractsComputerNameDomainAndMac()
    {
        // Arrange
        var result = new ProbeResult { Source = "NetBIOS (NBNS)" };

        // Construct mock NBNS response packet:
        // Header (12 bytes)
        var packet = new List<byte>(new byte[12]);

        // Query name / RR Name (e.g. 34-byte encoded wildcard name)
        packet.Add(0x20);
        packet.AddRange(Encoding.ASCII.GetBytes("CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));
        packet.Add(0x00);

        // Type (2 bytes: 0x00, 0x21), Class (2 bytes: 0x00, 0x01), TTL (4 bytes)
        packet.AddRange(new byte[8]);

        // RDLength: 2 bytes (big endian) -> 1 (numNames) + 18*2 + 6 (MAC) = 43 bytes = 0x002B
        packet.Add(0x00);
        packet.Add(0x2B);

        // Number of names: 2
        packet.Add(0x02);

        // Name 1: Computer name "MYDESKTOP      ", suffix 0x00, flags 0x0000 (unique)
        byte[] compBytes = Encoding.ASCII.GetBytes("MYDESKTOP      ");
        packet.AddRange(compBytes);
        packet.Add(0x00); // suffix
        packet.Add(0x00); packet.Add(0x00); // flags (unique)

        // Name 2: Workgroup "WORKGROUP      ", suffix 0x00, flags 0x8000 (group)
        byte[] groupBytes = Encoding.ASCII.GetBytes("WORKGROUP      ");
        packet.AddRange(groupBytes);
        packet.Add(0x00); // suffix
        packet.Add(0x80); packet.Add(0x00); // flags (group)

        // MAC Address (6 bytes: 0x00, 0x11, 0x22, 0x33, 0x44, 0x55)
        packet.AddRange(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 });

        // Act
        NbnsProbe.ParseNodeStatusResponse(packet.ToArray(), result);

        // Assert
        Assert.Equal("MYDESKTOP", result.RawData["NbnsHostname"]);
        Assert.Equal("WORKGROUP", result.RawData["NbnsDomain"]);
        Assert.Equal("00:11:22:33:44:55", result.RawData["NbnsMac"]);
    }

    [Fact]
    public void ParseNodeStatusResponse_CompressedPointerName_ExtractsInfoSuccessfully()
    {
        var result = new ProbeResult { Source = "NetBIOS (NBNS)" };
        var packet = new List<byte>(new byte[12]);

        // Compression pointer: 0xC0, 0x0C
        packet.Add(0xC0);
        packet.Add(0x0C);

        // Type (2), Class (2), TTL (4) = 8 bytes
        packet.AddRange(new byte[8]);

        // RDLength = 1 + 18*2 + 6 = 43 (0x002B)
        packet.Add(0x00);
        packet.Add(0x2B);

        // Number of names: 2
        packet.Add(0x02);

        // Name 1: "SERVER01       ", suffix 0x00, flags 0x0000
        packet.AddRange(Encoding.ASCII.GetBytes("SERVER01       "));
        packet.Add(0x00);
        packet.Add(0x00); packet.Add(0x00);

        // Name 2: "WORKGROUP      ", suffix 0x00, flags 0x8000
        packet.AddRange(Encoding.ASCII.GetBytes("WORKGROUP      "));
        packet.Add(0x00);
        packet.Add(0x80); packet.Add(0x00);

        // MAC Address: 00:0a:95:9d:68:16
        packet.AddRange(new byte[] { 0x00, 0x0a, 0x95, 0x9d, 0x68, 0x16 });

        NbnsProbe.ParseNodeStatusResponse(packet.ToArray(), result);

        Assert.Equal("SERVER01", result.RawData["NbnsHostname"]);
        Assert.Equal("WORKGROUP", result.RawData["NbnsDomain"]);
        Assert.Equal("00:0a:95:9d:68:16", result.RawData["NbnsMac"]);
    }

    [Fact]
    public void ParseNodeStatusResponse_AllZeroMac_OmitsMacFromRawData()
    {
        var result = new ProbeResult { Source = "NetBIOS (NBNS)" };
        var packet = new List<byte>(new byte[12]);

        packet.Add(0x20);
        packet.AddRange(Encoding.ASCII.GetBytes("CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));
        packet.Add(0x00);
        packet.AddRange(new byte[8]);

        packet.Add(0x00); packet.Add(0x19);
        packet.Add(0x01); // 1 name

        packet.AddRange(Encoding.ASCII.GetBytes("HOST1          "));
        packet.Add(0x00);
        packet.Add(0x00); packet.Add(0x00);

        // All zero MAC address
        packet.AddRange(new byte[6] { 0, 0, 0, 0, 0, 0 });

        NbnsProbe.ParseNodeStatusResponse(packet.ToArray(), result);

        Assert.Equal("HOST1", result.RawData["NbnsHostname"]);
        Assert.False(result.RawData.ContainsKey("NbnsMac"));
    }

    [Fact]
    public void ParseNodeStatusResponse_TruncatedRData_DoesNotThrow()
    {
        var result = new ProbeResult { Source = "NetBIOS (NBNS)" };
        var packet = new List<byte>(new byte[12]);

        packet.Add(0x20);
        packet.AddRange(Encoding.ASCII.GetBytes("CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));
        packet.Add(0x00);
        packet.AddRange(new byte[8]);

        // Claims 100 bytes of RDATA, but payload stops short
        packet.Add(0x00); packet.Add(0x64);
        packet.Add(0x05); // Claims 5 names, but no name bytes follow

        NbnsProbe.ParseNodeStatusResponse(packet.ToArray(), result);

        Assert.Empty(result.RawData);
    }

    [Fact]
    public async Task ProbeAsync_UnreachableHost_ReturnsEmptyResult()
    {
        var probe = new NbnsProbe();
        var node = new NetworkNode { IpAddress = "127.0.0.1" };

        using var cts = new CancellationTokenSource(200);
        var result = await probe.ProbeAsync(node, cts.Token);

        Assert.NotNull(result);
        Assert.Equal("NetBIOS (NBNS)", result.Source);
        Assert.Empty(result.RawData);
    }
}
