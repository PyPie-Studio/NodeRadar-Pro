using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("NodeRadarPro.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NodeRadarPro.Core.Fingerprinting.Probes;

public class SnmpProbe : IFingerprintProbe
{
    public string Name => "SNMP (sysDescr)";
    public int Priority => 40;

    public async Task<ProbeResult> ProbeAsync(NetworkNode node, CancellationToken ct)
    {
        var result = new ProbeResult { Source = Name };

        string[] communities = { "public", "private" };

        foreach (var community in communities)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                using var udp = new UdpClient();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(2000);

                var target = new IPEndPoint(IPAddress.Parse(node.IpAddress), 161);
                byte[] packet = BuildSnmpSysDescrRequest(community);

                await udp.SendAsync(packet, packet.Length, target).WaitAsync(timeoutCts.Token);

                var receiveResult = await udp.ReceiveAsync(timeoutCts.Token);
                string sysDescr = ParseSnmpResponse(receiveResult.Buffer);

                if (!string.IsNullOrEmpty(sysDescr))
                {
                    result.RawData["sysDescr"] = sysDescr;
                    result.RawData["Community"] = community;
                    break;
                }
            }
            catch { }
        }

        return result;
    }

    internal static byte[] BuildSnmpSysDescrRequest(string community)
    {
        var communityBytes = Encoding.ASCII.GetBytes(community);

        // OID: 1.3.6.1.2.1.1.1.0 (sysDescr.0)
        byte[] oid = { 0x2b, 0x06, 0x01, 0x02, 0x01, 0x01, 0x01, 0x00 };

        // Varbind = OID (Tag 0x06, len 8, bytes oid) + Null Value (0x05, 0x00)
        var varbind = new List<byte>();
        varbind.Add(0x06);
        varbind.Add((byte)oid.Length);
        varbind.AddRange(oid);
        varbind.AddRange(new byte[] { 0x05, 0x00 });

        // Varbind List = Sequence (0x30) + Length + Varbind bytes
        var varbindList = EncodeBerSequence(varbind.ToArray());

        // PDU Payload = Request ID (Integer) + Error Status (Integer) + Error Index (Integer) + Varbind List
        var pduPayload = new List<byte>();
        pduPayload.AddRange(new byte[] { 0x02, 0x04, 0x00, 0x00, 0x00, 0x01 }); // Request ID
        pduPayload.AddRange(new byte[] { 0x02, 0x01, 0x00 }); // Error Status
        pduPayload.AddRange(new byte[] { 0x02, 0x01, 0x00 }); // Error Index
        pduPayload.AddRange(varbindList);

        // PDU (GetRequest 0xa0) + Length + pduPayload
        var pdu = EncodeBerHeader(0xa0, pduPayload.ToArray());

        // Message Payload = Version (Integer v2c = 1) + Community String (OctetString) + PDU
        var messagePayload = new List<byte>();
        messagePayload.AddRange(new byte[] { 0x02, 0x01, 0x01 }); // Version v2c (value 1)
        messagePayload.Add(0x04); // Octet String
        messagePayload.Add((byte)communityBytes.Length);
        messagePayload.AddRange(communityBytes);
        messagePayload.AddRange(pdu);

        // Outer Message Sequence (0x30) + Length + messagePayload
        return EncodeBerSequence(messagePayload.ToArray());
    }

    private static byte[] EncodeBerSequence(byte[] content)
    {
        return EncodeBerHeader(0x30, content);
    }

    private static byte[] EncodeBerHeader(byte tag, byte[] content)
    {
        var result = new List<byte> { tag };
        int len = content.Length;
        if (len < 128)
        {
            result.Add((byte)len);
        }
        else if (len <= 255)
        {
            result.Add(0x81);
            result.Add((byte)len);
        }
        else
        {
            result.Add(0x82);
            result.Add((byte)(len >> 8));
            result.Add((byte)(len & 0xff));
        }
        result.AddRange(content);
        return result.ToArray();
    }

    internal static string ParseSnmpResponse(byte[] buffer)
    {
        try
        {
            // Simple BER parser looking for string values (Octet String 0x04)
            string sysDescr = "";
            int i = 0;
            while (i < buffer.Length)
            {
                if (buffer[i] == 0x04) // Octet string
                {
                    if (i + 1 >= buffer.Length) break;
                    int len = buffer[i + 1];
                    int headerLen = 2;

                    if (len > 128)
                    {
                        int numOctets = len & 0x7f;
                        if (i + 1 + numOctets >= buffer.Length) break;
                        len = 0;
                        for (int j = 0; j < numOctets; j++)
                        {
                            len = (len << 8) | buffer[i + 2 + j];
                        }
                        headerLen = 2 + numOctets;
                    }
                    else if (len == 128)
                    {
                        i++;
                        continue;
                    }

                    if (i + headerLen + len <= buffer.Length && len > 0)
                    {
                        string str = Encoding.ASCII.GetString(buffer, i + headerLen, len);
                        if (str.Length > sysDescr.Length) sysDescr = str;
                        i += headerLen + len;
                        continue;
                    }
                }
                i++;
            }

            // Clean non-printable characters
            sysDescr = new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(sysDescr, c => c >= 32 && c < 127)));
            return sysDescr.Trim();
        }
        catch { return ""; }
    }
}
