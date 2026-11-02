using System;
using System.Collections.Concurrent;

using System.Linq;
using System.Net;
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
        public ConcurrentBag<string> Services { get; } = new();
        public string Model { get; set; } = "";
        public string Manufacturer { get; set; } = "";
    }

    private static readonly ConcurrentDictionary<string, MdnsData> _cache = new();
    private static readonly string[] ModelKeys = ["model=", "am=", "md=", "rpMd="];
    private static readonly string[] VendorKeys = ["man=", "mf=", "manufacturer="];

    private static readonly string[] DefaultQueryServices =
    [
        "_services._dns-sd._udp.local",
        "_airplay._tcp.local", "_googlecast._tcp.local", "_raop._tcp.local",
        "_spotify-connect._tcp.local", "_workstation._tcp.local", "_printer._tcp.local",
        "_ipp._tcp.local", "_smb._tcp.local", "_apple-mobdev2._tcp.local",
        "_companion-link._tcp.local", "_androidtv._tcp.local", "_amzn-alexa._tcp.local",
        "_http._tcp.local", "_homekit._tcp.local", "_hap._tcp.local"
    ];

    private static readonly (string[] Patterns, string Service)[] ServiceSignatures =
    [
        (["Apple", "AirPlay", "apple-mobdev2", "iPhone", "iPad", "MacBook", "iMac"], "Apple Device"),
        (["Google", "Cast"], "Google Cast"),
        (["Spotify"], "Spotify Connect"),
        (["Printer", "ipp"], "Printer"),
        (["Android", "androidtv"], "Android Device"),
        (["amzn-alexa"], "Amazon Alexa"),
        (["homekit", "hap"], "Apple HomeKit")
    ];

    private static ReadOnlySpan<byte> LocalSuffix => [0x05, 0x6c, 0x6f, 0x63, 0x61, 0x6c, 0x00];

    public static async Task StartSweepAsync(CancellationToken token, Func<NodeRadarPro.Core.Networking.Sockets.IMulticastSocket>? socketFactory = null)
    {
        _cache.Clear();
        try
        {
            using var udp = socketFactory != null ? socketFactory() : new NodeRadarPro.Core.Networking.Sockets.DefaultMulticastSocket(0, IPAddress.Parse("224.0.0.251"));
            var target = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

            await SendInitialQueriesAsync(udp, target);

            while (!token.IsCancellationRequested)
            {
                var result = await udp.ReceiveAsync(token);
                string senderIp = result.RemoteEndPoint.Address.ToString();
                ProcessResponse(senderIp, result.Buffer);
            }
        }
        catch { }
    }

    private static async Task SendInitialQueriesAsync(NodeRadarPro.Core.Networking.Sockets.IMulticastSocket udp, IPEndPoint target)
    {
        for (int i = 0; i < DefaultQueryServices.Length; i++)
        {
            byte[] query = CreateMdnsQuery(DefaultQueryServices[i]);
            await udp.SendAsync(query, query.Length, target);
        }
    }

    private static void ProcessResponse(string senderIp, byte[] buffer)
    {
        if (senderIp.StartsWith("169.254.", StringComparison.Ordinal))
        {
            return;
        }

        var data = _cache.GetOrAdd(senderIp, _ => new MdnsData());
        ApplyInstanceName(data, buffer);

        string raw = Encoding.UTF8.GetString(buffer);
        ClassifyServices(raw, data);
        ParseTxtRecords(buffer, data);
    }

    private static void ApplyInstanceName(MdnsData data, byte[] buffer)
    {
        string instanceName = ExtractMdnsInstanceName(buffer);
        if (!string.IsNullOrEmpty(instanceName) && (string.IsNullOrEmpty(data.InstanceName) || data.InstanceName.Length < instanceName.Length))
        {
            data.InstanceName = instanceName;
        }
    }

    private static void ClassifyServices(string raw, MdnsData data)
    {
        for (int i = 0; i < ServiceSignatures.Length; i++)
        {
            var (patterns, service) = ServiceSignatures[i];
            if (MatchesAnyPattern(raw, patterns))
            {
                data.Services.Add(service);
            }
        }
    }

    private static bool MatchesAnyPattern(string raw, string[] patterns)
    {
        for (int i = 0; i < patterns.Length; i++)
        {
            if (raw.Contains(patterns[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
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

            if (!data.Services.IsEmpty)
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
            string? model = ExtractFirstMatchingValue(raw, ModelKeys);
            if (!string.IsNullOrEmpty(model))
            {
                data.Model = model;
            }

            string? manufacturer = ExtractFirstMatchingValue(raw, VendorKeys);
            if (!string.IsNullOrEmpty(manufacturer))
            {
                data.Manufacturer = manufacturer;
            }
        }
        catch { }
    }

    private static string? ExtractFirstMatchingValue(string raw, string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            string? value = ExtractValueForKey(raw, keys[i]);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        return null;
    }

    private static string? ExtractValueForKey(string raw, string key)
    {
        int idx = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx == -1)
        {
            return null;
        }

        int start = idx + key.Length;
        int end = start;
        while (end < raw.Length && raw[end] >= 32 && raw[end] < 127)
        {
            end++;
        }

        if (end > start)
        {
            return raw.Substring(start, end - start).Trim();
        }

        return null;
    }

    internal static string ExtractMdnsInstanceName(byte[] buffer)
    {
        try
        {
            ReadOnlySpan<byte> span = buffer;
            int localIdx = span.IndexOf(LocalSuffix);
            if (localIdx > 2)
            {
                return FindInstanceLabel(buffer, localIdx);
            }
        }
        catch { }
        return string.Empty;
    }

    private static string FindInstanceLabel(byte[] buffer, int localIdx)
    {
        for (int len = 1; len <= 63 && localIdx - 1 - len >= 0; len++)
        {
            if (buffer[localIdx - 1 - len] == len)
            {
                string name = Encoding.UTF8.GetString(buffer, localIdx - len, len);
                if (!name.StartsWith('_') && name.Length > 2)
                {
                    return name;
                }
            }
        }
        return string.Empty;
    }
}
