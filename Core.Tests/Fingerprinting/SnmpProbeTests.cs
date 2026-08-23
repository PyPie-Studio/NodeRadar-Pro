using System.Text;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace Core.Tests.Fingerprinting
{
    public class SnmpProbeTests
    {
        [Fact]
        public void BuildSnmpSysDescrRequest_ConstructsValidPacketLengths()
        {
            byte[] packet = SnmpProbe.BuildSnmpSysDescrRequest("public");

            Assert.NotNull(packet);
            Assert.Equal(0x30, packet[0]); // Outer Sequence Tag
            Assert.Equal(packet.Length - 2, packet[1]); // Outer Sequence Length

            // Check version tag and value
            Assert.Equal(0x02, packet[2]);
            Assert.Equal(0x01, packet[3]);
            Assert.Equal(0x01, packet[4]); // v2c

            // Check community string octet string tag and length
            Assert.Equal(0x04, packet[5]);
            Assert.Equal(6, packet[6]); // "public" length
            Assert.Equal("public", Encoding.ASCII.GetString(packet, 7, 6));

            // Check PDU GetRequest Tag
            Assert.Equal(0xa0, packet[13]);
        }

        [Fact]
        public void BuildSnmpSysDescrRequest_HandlesLongCommunityName()
        {
            string longCommunity = new string('a', 50);
            byte[] packet = SnmpProbe.BuildSnmpSysDescrRequest(longCommunity);

            Assert.NotNull(packet);
            Assert.Equal(0x30, packet[0]);
            Assert.Equal(packet.Length - 2, packet[1]);
            Assert.Equal(0x04, packet[5]);
            Assert.Equal(50, packet[6]);
            Assert.Equal(longCommunity, Encoding.ASCII.GetString(packet, 7, 50));
        }

        [Fact]
        public void ParseSnmpResponse_ParsesShortOctetString()
        {
            byte[] buffer = new byte[]
            {
                0x30, 0x18,
                0x04, 0x14, // Octet string, len 20
                (byte)'L', (byte)'i', (byte)'n', (byte)'u', (byte)'x', (byte)' ',
                (byte)'S', (byte)'e', (byte)'r', (byte)'v', (byte)'e', (byte)'r', (byte)' ',
                (byte)'1', (byte)'.', (byte)'0', (byte)'.', (byte)'0', (byte)'\r', (byte)'\n'
            };

            string sysDescr = SnmpProbe.ParseSnmpResponse(buffer);

            Assert.Equal("Linux Server 1.0.0", sysDescr);
        }

        [Fact]
        public void ParseSnmpResponse_ParsesLongOctetString()
        {
            string longDescr = "Cisco IOS Software, C2960 Software (C2960-LANBASEK9-M), Version 12.2(55)SE5, RELEASE SOFTWARE (fc1) Technical Support: http://www.cisco.com/techsupport Copyright (c) 1986-2012 by Cisco Systems, Inc.";
            byte[] descrBytes = Encoding.ASCII.GetBytes(longDescr);

            var bufferList = new System.Collections.Generic.List<byte>
            {
                0x04, 0x81, (byte)descrBytes.Length // Long form BER length (0x81 + 1 byte)
            };
            bufferList.AddRange(descrBytes);

            string sysDescr = SnmpProbe.ParseSnmpResponse(bufferList.ToArray());

            Assert.Equal(longDescr, sysDescr);
        }

        [Fact]
        public void ParseSnmpResponse_HandlesEmptyOrInvalidBuffer()
        {
            Assert.Equal("", SnmpProbe.ParseSnmpResponse(new byte[0]));
            Assert.Equal("", SnmpProbe.ParseSnmpResponse(new byte[] { 0x04, 0x10 }));
        }
    }
}
