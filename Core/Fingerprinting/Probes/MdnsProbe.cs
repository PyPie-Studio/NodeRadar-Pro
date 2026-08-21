using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Fingerprinting.Probes;

public class MdnsProbe : IFingerprintProbe
{
    public string Name => "mDNS / Bonjour";
    public int Priority => 20;

    public class MdnsData
    {
        public string InstanceName { get; set; } = "";
        public List<string> Services { get; } = new();
        public string Model { get; set; } = "";
        public string Manufacturer { get; set; } = "";
    }

    private static readonly ConcurrentDictionary<string, MdnsData> _cache = new();

    public static async Task StartSweepAsync(CancellationToken token)
    {
        _cache.Clear();
        try
        {
            using var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            var target = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

            // Extended service list including device-info and sleep-proxy for better Apple identification
            string[] services = {
                "_services._dns-sd._udp.local",
                "_airplay._tcp.local", "_googlecast._tcp.local", "_raop._tcp.local",
                "_spotify-connect._tcp.local", "_workstation._tcp.local", "_printer._tcp.local",
                "_ipp._tcp.local", "_smb._tcp.local", "_apple-mobdev2._tcp.local",
                "_companion-link._tcp.local", "_androidtv._tcp.local", "_amzn-alexa._tcp.local",
                "_http._tcp.local", "_homekit._tcp.local", "_hap._tcp.local"
            };

            foreach (var service in services)
            {
                byte[] query = CreateMdnsQuery(service);
                await udp.SendAsync(query, query.Length, target);
            }

            while (!token.IsCancellationRequested)
            {
                var result = await udp.ReceiveAsync(token);
                string senderIp = result.RemoteEndPoint.Address.ToString();

                // Filter out link-local addresses
                if (senderIp.StartsWith("169.254.")) continue;

                var data = _cache.GetOrAdd(senderIp, _ => new MdnsData());

                string raw = Encoding.UTF8.GetString(result.Buffer);

                // Extract instance name from DNS answer section
                string instanceName = ExtractMdnsInstanceName(result.Buffer);

                if (!string.IsNullOrEmpty(instanceName) && (string.IsNullOrEmpty(data.InstanceName) || data.InstanceName.Length < instanceName.Length))
                {
                    data.InstanceName = instanceName;
                }

                if (raw.Contains("Apple") || raw.Contains("AirPlay") || raw.Contains("apple-mobdev2")) data.Services.Add("Apple Device");
                if (raw.Contains("Google") || raw.Contains("Cast")) data.Services.Add("Google Cast");
                if (raw.Contains("Spotify")) data.Services.Add("Spotify Connect");
                if (raw.Contains("Printer") || raw.Contains("ipp")) data.Services.Add("Printer");
                if (raw.Contains("Android") || raw.Contains("androidtv")) data.Services.Add("Android Device");
                if (raw.Contains("iPhone") || raw.Contains("iPad") || raw.Contains("MacBook") || raw.Contains("iMac")) data.Services.Add("Apple Device");
                if (raw.Contains("amzn-alexa")) data.Services.Add("Amazon Alexa");
                if (raw.Contains("homekit") || raw.Contains("hap")) data.Services.Add("Apple HomeKit");

                ParseTxtRecords(result.Buffer, data);
            }
        }
        catch { }
    }

    public Task<ProbeResult> ProbeAsync(NetworkNode node, CancellationToken ct)
    {
        var result = new ProbeResult { Source = Name };

        if (_cache.TryGetValue(node.IpAddress, out var data))
        {
            if (!string.IsNullOrEmpty(data.InstanceName))
                result.RawData["InstanceName"] = data.InstanceName;

            if (!string.IsNullOrEmpty(data.Model))
                result.RawData["Model"] = data.Model;

            if (!string.IsNullOrEmpty(data.Manufacturer))
                result.RawData["Manufacturer"] = data.Manufacturer;

            if (data.Services.Count > 0)
                result.RawData["Services"] = string.Join(", ", data.Services.Distinct());
        }

        return Task.FromResult(result);
    }

    private static byte[] CreateMdnsQuery(string serviceName)
    {
        var packet = new List<byte> {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        foreach (var part in serviceName.Split('.'))
        {
            packet.Add((byte)part.Length);
            packet.AddRange(Encoding.ASCII.GetBytes(part));
        }
        packet.Add(0x00);
        packet.Add(0x00); packet.Add(0x0c);
        packet.Add(0x00); packet.Add(0x01);
        return packet.ToArray();
    }

    private static void ParseTxtRecords(byte[] buffer, MdnsData data)
    {
        try
        {
            string raw = Encoding.UTF8.GetString(buffer);

            // Model keys used by various devices
            string[] modelKeys = { "model=", "am=", "md=", "rpMd=" };
            string[] vendorKeys = { "man=", "mf=", "manufacturer=" };
            string[] osKeys = { "osxvers=", "osvers=" };
            string[] deviceIdKeys = { "deviceid=", "id=" };

            foreach (var key in modelKeys)
            {
                int idx = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx != -1)
                {
                    int start = idx + key.Length;
                    int end = start;
                    while (end < raw.Length && raw[end] >= 32 && raw[end] < 127) end++;
                    if (end > start) data.Model = raw.Substring(start, end - start).Trim();
                }
            }

            foreach (var key in vendorKeys)
            {
                int idx = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx != -1)
                {
                    int start = idx + key.Length;
                    int end = start;
                    while (end < raw.Length && raw[end] >= 32 && raw[end] < 127) end++;
                    if (end > start) data.Manufacturer = raw.Substring(start, end - start).Trim();
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Extracts hostname from SRV record answers in the mDNS response.
    /// SRV records contain the target hostname of the service.
    /// </summary>
    private static string ExtractSrvHostname(byte[] buffer)
    {
        try
        {
            if (buffer.Length < 12) return string.Empty;

            // Parse DNS header
            int qdcount = (buffer[4] << 8) | buffer[5];
            int ancount = (buffer[6] << 8) | buffer[7];

            int offset = 12;

            // Skip question section
            for (int q = 0; q < qdcount && offset < buffer.Length; q++)
            {
                offset = SkipDnsName(buffer, offset);
                if (offset < 0 || offset + 4 > buffer.Length) return string.Empty;
                offset += 4; // Skip QTYPE and QCLASS
            }

            // Parse answer section looking for SRV records (type 33) or PTR records (type 12)
            for (int a = 0; a < ancount && offset < buffer.Length; a++)
            {
                offset = SkipDnsName(buffer, offset);
                if (offset < 0 || offset + 10 > buffer.Length) return string.Empty;

                int rtype = (buffer[offset] << 8) | buffer[offset + 1];
                offset += 8; // Skip type, class, TTL
                int rdlen = (buffer[offset] << 8) | buffer[offset + 1];
                offset += 2;

                if (rtype == 33 && offset + 6 < buffer.Length) // SRV record
                {
                    // SRV: 2 bytes priority, 2 bytes weight, 2 bytes port, then target hostname
                    int targetOffset = offset + 6;
                    string hostname = ReadDnsName(buffer, targetOffset);
                    if (!string.IsNullOrEmpty(hostname) && !hostname.StartsWith("_"))
                    {
                        // Remove trailing .local
                        if (hostname.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                            hostname = hostname.Substring(0, hostname.Length - 6);
                        if (hostname.EndsWith(".", StringComparison.Ordinal))
                            hostname = hostname.Substring(0, hostname.Length - 1);
                        return hostname;
                    }
                }

                offset += rdlen;
            }
        }
        catch { }
        return string.Empty;
    }

    private static int SkipDnsName(byte[] buffer, int offset)
    {
        if (offset >= buffer.Length) return -1;
        while (offset < buffer.Length)
        {
            byte len = buffer[offset];
            if (len == 0) return offset + 1;
            if ((len & 0xC0) == 0xC0) return offset + 2; // Pointer
            offset += len + 1;
        }
        return -1;
    }

    private static string ReadDnsName(byte[] buffer, int offset)
    {
        var parts = new List<string>();
        int maxJumps = 10;
        int jumps = 0;

        while (offset < buffer.Length && jumps < maxJumps)
        {
            byte len = buffer[offset];
            if (len == 0) break;
            if ((len & 0xC0) == 0xC0) // Pointer
            {
                int pointer = ((len & 0x3F) << 8) | buffer[offset + 1];
                offset = pointer;
                jumps++;
                continue;
            }
            offset++;
            if (offset + len > buffer.Length) break;
            parts.Add(Encoding.UTF8.GetString(buffer, offset, len));
            offset += len;
        }

        return parts.Count > 0 ? string.Join(".", parts) : string.Empty;
    }

    private static string ExtractMdnsInstanceName(byte[] buffer)
    {
        try
        {
            byte[] localPattern = { 0x05, 0x6c, 0x6f, 0x63, 0x61, 0x6c, 0x00 };
            int localIdx = -1;
            for (int i = 0; i < buffer.Length - 7; i++)
            {
                bool match = true;
                for (int j = 0; j < 7; j++) if (buffer[i + j] != localPattern[j]) { match = false; break; }
                if (match) { localIdx = i; break; }
            }

            if (localIdx > 2)
            {
                int nameLen = buffer[localIdx - 1];
                if (localIdx - 1 - nameLen >= 0)
                {
                    string name = Encoding.UTF8.GetString(buffer, localIdx - 1 - nameLen, nameLen);
                    if (!name.StartsWith("_") && name.Length > 2) return name;
                }
            }
        }
        catch { }
        return string.Empty;
    }
}
