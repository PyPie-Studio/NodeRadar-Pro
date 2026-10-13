using System.Text;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

public class SnmpProbeTests
{
    [Fact]
    public void Properties_ReturnExpectedValues()
    {
        var probe = new SnmpProbe();
        Assert.Equal("SNMP (sysDescr)", probe.Name);
        Assert.Equal(40, probe.Priority);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    public void BuildSnmpSysDescrRequest_ContainsCommunityAndOid(string community)
    {
        byte[] request = SnmpProbe.BuildSnmpSysDescrRequest(community);

        Assert.NotNull(request);
        Assert.NotEmpty(request);

        // Sequence starts with 0x30
        Assert.Equal(0x30, request[0]);

        // Community string should be embedded in request
        byte[] communityBytes = Encoding.ASCII.GetBytes(community);
        string requestString = Encoding.ASCII.GetString(request);
        Assert.Contains(community, requestString);

        // OID bytes for 1.3.6.1.2.1.1.1.0 (0x2b, 0x06, 0x01, 0x02, 0x01, 0x01, 0x01, 0x00)
        byte[] expectedOid = new byte[] { 0x2b, 0x06, 0x01, 0x02, 0x01, 0x01, 0x01, 0x00 };
        bool oidFound = false;
        for (int i = 0; i <= request.Length - expectedOid.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < expectedOid.Length; j++)
            {
                if (request[i + j] != expectedOid[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                oidFound = true;
                break;
            }
        }
        Assert.True(oidFound, "SNMP request packet should contain sysDescr OID");
    }

    [Fact]
    public void ParseSnmpResponse_EmptyOrNullBuffer_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, SnmpProbe.ParseSnmpResponse(Array.Empty<byte>()));
    }

    [Fact]
    public void ParseSnmpResponse_ValidBerOctetString_ExtractsSysDescr()
    {
        string expectedDescr = "Cisco IOS Software, C2960 Software (C2960-LANBASEK9-M), Version 15.0(2)SE4";
        byte[] descrBytes = Encoding.ASCII.GetBytes(expectedDescr);

        var packet = new List<byte>();
        packet.Add(0x04);
        packet.Add(0x06);
        packet.AddRange(Encoding.ASCII.GetBytes("public"));

        packet.Add(0x04);
        packet.Add((byte)descrBytes.Length);
        packet.AddRange(descrBytes);

        string result = SnmpProbe.ParseSnmpResponse(packet.ToArray());
        Assert.Equal(expectedDescr, result);
    }

    [Fact]
    public void ParseSnmpResponse_LongLengthEncoding_ParsesCorrectly()
    {
        string longDescr = new string('A', 150);
        byte[] descrBytes = Encoding.ASCII.GetBytes(longDescr);

        var packet = new List<byte>
        {
            0x04,
            0x81,
            (byte)descrBytes.Length
        };
        packet.AddRange(descrBytes);

        string result = SnmpProbe.ParseSnmpResponse(packet.ToArray());
        Assert.Equal(longDescr, result);
    }

    [Fact]
    public void ParseSnmpResponse_StripsNonPrintableCharacters()
    {
        byte[] descrBytes = Encoding.ASCII.GetBytes("Linux\0\r\nServer");

        var packet = new List<byte>
        {
            0x04,
            (byte)descrBytes.Length
        };
        packet.AddRange(descrBytes);

        string result = SnmpProbe.ParseSnmpResponse(packet.ToArray());
        Assert.Equal("LinuxServer", result);
    }

    [Fact]
    public async Task ProbeAsync_UnreachableHost_ReturnsEmptyResult()
    {
        var probe = new SnmpProbe();
        var node = new NetworkNode { IpAddress = "127.0.0.1" };
        using var cts = new CancellationTokenSource(100);

        var result = await probe.ProbeAsync(node, cts.Token);

        Assert.NotNull(result);
        Assert.Equal("SNMP (sysDescr)", result.Source);
        Assert.Empty(result.RawData);
    }

    [Fact]
    public async Task ProbeAsync_CancelledToken_ReturnsEmptyResult()
    {
        var probe = new SnmpProbe();
        var node = new NetworkNode { IpAddress = "192.168.1.1" };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await probe.ProbeAsync(node, cts.Token);

        Assert.NotNull(result);
        Assert.Equal("SNMP (sysDescr)", result.Source);
        Assert.Empty(result.RawData);
    }
}
