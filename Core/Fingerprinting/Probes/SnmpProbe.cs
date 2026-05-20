using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

    private byte[] BuildSnmpSysDescrRequest(string community)
    {
        var communityBytes = Encoding.ASCII.GetBytes(community);
        
        // OID: 1.3.6.1.2.1.1.1.0 (sysDescr.0)
        byte[] oid = { 0x2b, 0x06, 0x01, 0x02, 0x01, 0x01, 0x01, 0x00 };
        
        var packet = new List<byte>
        {
            0x30, 0x00, // Sequence, length (placeholder)
            0x02, 0x01, 0x01, // Version 2c
            0x04, (byte)communityBytes.Length // Community string
        };
        packet.AddRange(communityBytes);
        
        // PDU GET Request
        var pdu = new List<byte>
        {
            0xa0, 0x00, // GET Request, length (placeholder)
            0x02, 0x04, 0x00, 0x00, 0x00, 0x01, // Request ID
            0x02, 0x01, 0x00, // Error status
            0x02, 0x01, 0x00, // Error index
            0x30, 0x00, // Varbind list, length
            0x30, 0x00, // Varbind, length
            0x06, (byte)oid.Length // Object identifier
        };
        pdu.AddRange(oid);
        pdu.AddRange(new byte[] { 0x05, 0x00 }); // Null value
        
        // Fix lengths
        pdu[15] = (byte)(pdu.Count - 17); // Varbind length
        pdu[13] = (byte)(pdu.Count - 15); // Varbind list length
        pdu[1] = (byte)(pdu.Count - 2);   // PDU length
        
        packet.AddRange(pdu);
        packet[1] = (byte)(packet.Count - 2); // Sequence length
        
        return packet.ToArray();
    }

    private string ParseSnmpResponse(byte[] buffer)
    {
        try
        {
            // Simple BER parser looking for the last octet string (usually the sysDescr value)
            string sysDescr = "";
            for (int i = 0; i < buffer.Length - 1; i++)
            {
                if (buffer[i] == 0x04) // Octet string
                {
                    int len = buffer[i + 1];
                    if (len < 128 && i + 2 + len <= buffer.Length)
                    {
                        string str = Encoding.ASCII.GetString(buffer, i + 2, len);
                        if (str.Length > sysDescr.Length) sysDescr = str;
                        i += len + 1;
                    }
                }
            }
            
            // Clean non-printable characters
            sysDescr = new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(sysDescr, c => c >= 32 && c < 127)));
            return sysDescr.Trim();
        }
        catch { return ""; }
    }
}
