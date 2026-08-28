using System.Text;
using NodeRadarPro.Core.Fingerprinting;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

public class NbnsProbeTests
{
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

        // Compressed Name Pointer (2 bytes: 0xC0, 0x0C)
        packet.Add(0xC0);
        packet.Add(0x0C);

        // Type (2 bytes: 0x00, 0x21), Class (2 bytes: 0x00, 0x01), TTL (4 bytes)
        packet.AddRange(new byte[8]);

        // RDLength: 2 bytes (big endian) -> e.g. 1 (numNames) + 18*2 + 6 (MAC) = 43 bytes = 0x002B
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
}
