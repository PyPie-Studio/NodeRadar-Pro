using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Fingerprinting.Probes;

public class NbnsProbe : IFingerprintProbe
{
    public string Name => "NetBIOS (NBNS)";
    public int Priority => 35;

    public async Task<ProbeResult> ProbeAsync(NetworkNode node, CancellationToken ct)
    {
        var result = new ProbeResult { Source = Name };

        try
        {
            using var udp = new UdpClient();
            var target = new IPEndPoint(IPAddress.Parse(node.IpAddress), 137);

            // Build NBNS Node Status (NBSTAT) wildcard query
            var packet = new List<byte>();

            // Transaction ID (random 2 bytes)
            var rng = new Random();
            byte[] txId = new byte[2];
            rng.NextBytes(txId);
            packet.AddRange(txId);

            // Flags: 0x0000 (standard query)
            packet.Add(0x00); packet.Add(0x00);

            // QDCOUNT: 1
            packet.Add(0x00); packet.Add(0x01);

            // ANCOUNT, NSCOUNT, ARCOUNT: all 0
            packet.AddRange(new byte[6]);

            // Question section
            // Name length: 0x20 (32 bytes of encoded NetBIOS name)
            packet.Add(0x20);

            // Mangled wildcard name: CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA (encodes "*" padded with NULs)
            packet.AddRange(Encoding.ASCII.GetBytes("CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));

            // Terminator
            packet.Add(0x00);

            // Type: NBSTAT (0x0021)
            packet.Add(0x00); packet.Add(0x21);

            // Class: IN (0x0001)
            packet.Add(0x00); packet.Add(0x01);

            byte[] queryBytes = packet.ToArray();
            await udp.SendAsync(queryBytes, queryBytes.Length, target);

            // Receive with 1.5s timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(1500);

            var response = await udp.ReceiveAsync(cts.Token);
            byte[] buf = response.Buffer;

            // Parse the NBNS Node Status response
            ParseNodeStatusResponse(buf, result);
        }
        catch { /* Host unreachable, timeout, or other error – return empty result */ }

        return result;
    }

    private static void ParseNodeStatusResponse(byte[] buf, ProbeResult result)
    {
        // Minimum: 12 (header) + 34 (query name section) + 10 (RR header) + 1 (numNames) = 57
        if (buf.Length < 57) return;

        // Skip 12-byte DNS header
        int offset = 12;

        // Skip the response name field (could be compressed or full)
        // Handle DNS name compression pointer (0xC0 xx) or walk labels
        if (offset < buf.Length && (buf[offset] & 0xC0) == 0xC0)
        {
            // Compression pointer: 2 bytes
            offset += 2;
        }
        else
        {
            // Walk labels until terminator
            while (offset < buf.Length && buf[offset] != 0x00)
            {
                int labelLen = buf[offset];
                offset += 1 + labelLen;
            }
            offset++; // skip the 0x00 terminator
        }

        // Skip Type (2) + Class (2) + TTL (4) = 8 bytes of RR metadata
        offset += 8;

        // Read RDLENGTH (2 bytes, big-endian)
        if (offset + 2 > buf.Length) return;
        int rdataLength = (buf[offset] << 8) | buf[offset + 1];
        offset += 2;

        if (offset >= buf.Length) return;

        // Number of name entries
        int numNames = buf[offset];
        offset++;

        string computerName = "";
        string domain = "";

        // Each name entry: 15 bytes name + 1 byte suffix/type + 2 bytes flags = 18 bytes
        for (int i = 0; i < numNames; i++)
        {
            if (offset + 18 > buf.Length) break;

            string name = Encoding.ASCII.GetString(buf, offset, 15).TrimEnd();
            byte suffixType = buf[offset + 15];
            ushort flags = (ushort)((buf[offset + 16] << 8) | buf[offset + 17]);
            bool isGroup = (flags & 0x8000) != 0;

            if (suffixType == 0x00)
            {
                if (!isGroup && string.IsNullOrEmpty(computerName))
                {
                    computerName = name;
                }
                else if (isGroup && string.IsNullOrEmpty(domain))
                {
                    domain = name;
                }
            }

            offset += 18;
        }

        // After all name entries, the next 6 bytes are the unit's MAC address
        string nbnsMac = "";
        if (offset + 6 <= buf.Length)
        {
            nbnsMac = string.Join(":",
                buf[offset].ToString("x2"),
                buf[offset + 1].ToString("x2"),
                buf[offset + 2].ToString("x2"),
                buf[offset + 3].ToString("x2"),
                buf[offset + 4].ToString("x2"),
                buf[offset + 5].ToString("x2"));
        }

        if (!string.IsNullOrEmpty(computerName))
            result.RawData["NbnsHostname"] = computerName;

        if (!string.IsNullOrEmpty(domain))
            result.RawData["NbnsDomain"] = domain;

        if (!string.IsNullOrEmpty(nbnsMac) && nbnsMac != "00:00:00:00:00:00")
            result.RawData["NbnsMac"] = nbnsMac;
    }
}
