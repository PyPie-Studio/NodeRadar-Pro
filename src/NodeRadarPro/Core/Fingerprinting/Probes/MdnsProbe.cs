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
    private static readonly string[] ModelKeys = { "model=", "am=", "md=", "rpMd=" };
    private static readonly string[] VendorKeys = { "man=", "mf=", "manufacturer=" };


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

    internal static void InjectCacheForTesting(string ip, MdnsData data)
    {
        _cache[ip] = data;
    }

    internal static void ClearCacheForTesting()
    {
        _cache.Clear();
    }

    internal static byte[] CreateMdnsQuery(string serviceName)
    {
        byte[] buffer = new byte[12 + serviceName.Length + 6];
        int idx = 0;

        // Transaction ID: 0x0000
        buffer[idx++] = 0x00; buffer[idx++] = 0x00;
        // Flags: Standard query (0x0000)
        buffer[idx++] = 0x00; buffer[idx++] = 0x00;
        // Questions: 1 (0x0001)
        buffer[idx++] = 0x00; buffer[idx++] = 0x01;
        // Answer RRs: 0
        buffer[idx++] = 0x00; buffer[idx++] = 0x00;
        // Authority RRs: 0
        buffer[idx++] = 0x00; buffer[idx++] = 0x00;
        // Additional RRs: 0
        buffer[idx++] = 0x00; buffer[idx++] = 0x00;

        // Question Name: format each label with its length prefix
        int start = 0;
        while (start < serviceName.Length)
        {
            int dotIdx = serviceName.IndexOf('.', start);
            if (dotIdx == -1) dotIdx = serviceName.Length;
            int len = dotIdx - start;
            buffer[idx++] = (byte)len;
            for (int i = 0; i < len; i++)
            {
                buffer[idx++] = (byte)serviceName[start + i];
            }
            start = dotIdx + 1;
        }

        buffer[idx++] = 0x00; // Null terminator for domain name
        buffer[idx++] = 0x00; buffer[idx++] = 0x0c; // QTYPE = PTR (12)
        buffer[idx++] = 0x00; buffer[idx++] = 0x01; // QCLASS = IN (1)

        if (idx != buffer.Length)
        {
            Array.Resize(ref buffer, idx);
        }
        return buffer;
    }

    internal static void ParseTxtRecords(byte[] buffer, MdnsData data)
    {
        try
        {
            string raw = Encoding.UTF8.GetString(buffer);

            // Model keys used by various devices
            var modelKeys = ModelKeys;
            var vendorKeys = VendorKeys;

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

    internal static string ExtractMdnsInstanceName(byte[] buffer)
    {
        try
        {
            byte[] localPattern = { 0x05, 0x6c, 0x6f, 0x63, 0x61, 0x6c, 0x00 };
            int localIdx = -1;
            for (int i = 0; i <= buffer.Length - 7; i++)
            {
                bool match = true;
                for (int j = 0; j < 7; j++) if (buffer[i + j] != localPattern[j]) { match = false; break; }
                if (match) { localIdx = i; break; }
            }

            if (localIdx > 2)
            {
                for (int len = 1; len <= 63 && localIdx - 1 - len >= 0; len++)
                {
                    if (buffer[localIdx - 1 - len] == len)
                    {
                        string name = Encoding.UTF8.GetString(buffer, localIdx - len, len);
                        if (!name.StartsWith("_") && name.Length > 2) return name;
                    }
                }
            }
        }
        catch { }
        return string.Empty;
    }
}
