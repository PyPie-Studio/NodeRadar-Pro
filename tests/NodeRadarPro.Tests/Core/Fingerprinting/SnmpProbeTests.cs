using System.Text;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

public class SnmpProbeTests
{
    [Fact]
    public void ParseSnmpResponse_EmptyOrNullBuffer_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, SnmpProbe.ParseSnmpResponse(Array.Empty<byte>()));
    }

    [Fact]
    public void ParseSnmpResponse_ValidBerOctetString_ExtractsSysDescr()
    {
        // Arrange
        string expectedDescr = "Cisco IOS Software, C2960 Software (C2960-LANBASEK9-M), Version 15.0(2)SE4";
        byte[] descrBytes = Encoding.ASCII.GetBytes(expectedDescr);

        var packet = new List<byte>();
        // Add community string (Octet string: 0x04, len, "public")
        packet.Add(0x04);
        packet.Add(0x06);
        packet.AddRange(Encoding.ASCII.GetBytes("public"));

        // Add SysDescr (Octet string: 0x04, len, text)
        packet.Add(0x04);
        packet.Add((byte)descrBytes.Length);
        packet.AddRange(descrBytes);

        // Act
        string result = SnmpProbe.ParseSnmpResponse(packet.ToArray());

        // Assert
        Assert.Equal(expectedDescr, result);
    }

    [Fact]
    public void ParseSnmpResponse_LongLengthEncoding_ParsesCorrectly()
    {
        // Arrange - String with > 128 chars
        string longDescr = new string('A', 150);
        byte[] descrBytes = Encoding.ASCII.GetBytes(longDescr);

        var packet = new List<byte>
        {
            0x04,
            0x81, // Long form: 1 length byte follows
            (byte)descrBytes.Length
        };
        packet.AddRange(descrBytes);

        // Act
        string result = SnmpProbe.ParseSnmpResponse(packet.ToArray());

        // Assert
        Assert.Equal(longDescr, result);
    }
}
