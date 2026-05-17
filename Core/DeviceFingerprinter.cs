using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

public static class DeviceFingerprinter
{
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

    private class DiscoveryData
    {
        public string MdnsName { get; set; } = string.Empty;
        public string SsdpName { get; set; } = string.Empty;
        public List<string> Services { get; } = new List<string>();
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DiscoveryData> _discoveryCache = new();

    public static async Task<string> TryGetHttpServerBannerAsync(string ip)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"http://{ip}/");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.Headers.Server != null)
            {
                return response.Headers.Server.ToString();
            }
        }
        catch { }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{ip}/");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.Headers.Server != null)
            {
                return response.Headers.Server.ToString();
            }
        }
        catch { }

        return string.Empty;
    }

    /// <summary>
    /// Attempts to identify the exact device model using cached mDNS results.
    /// </summary>
    public static async Task<string> DiscoverExactModelViaMDnsAsync(string ip)
    {
        if (_discoveryCache.TryGetValue(ip, out var data))
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(data.MdnsName)) sb.Append($"{data.MdnsName} ");
            foreach (var service in data.Services) sb.Append($"{service} ");
            return sb.ToString().Trim();
        }
        return string.Empty;
    }

    private static byte[] CreateMdnsQuery(string serviceName)
    {
        var packet = new List<byte> { 
            0x00, 0x00, // Transaction ID
            0x00, 0x00, // Flags
            0x00, 0x01, // Questions
            0x00, 0x00, // Answers
            0x00, 0x00, // Authority
            0x00, 0x00  // Additional
        };

        var parts = serviceName.Split('.');
        foreach (var part in parts)
        {
            packet.Add((byte)part.Length);
            packet.AddRange(Encoding.ASCII.GetBytes(part));
        }
        packet.Add(0x00); // End of labels

        packet.Add(0x00); packet.Add(0x0c); // Type PTR
        packet.Add(0x00); packet.Add(0x01); // Class IN

        return packet.ToArray();
    }

    /// <summary>
    /// Attempts to identify device details using cached SSDP results.
    /// </summary>
    public static async Task<string> DiscoverExactModelViaSSDPAsync(string ip)
    {
        if (_discoveryCache.TryGetValue(ip, out var data))
        {
            return data.SsdpName;
        }
        return string.Empty;
    }

    private static async Task<string> TryFetchSsdpLocationXmlAsync(string url)
    {
        try
        {
            string xml = await _httpClient.GetStringAsync(url);
            string friendlyName = ExtractXmlValue(xml, "friendlyName");
            string manufacturer = ExtractXmlValue(xml, "manufacturer");
            string modelName = ExtractXmlValue(xml, "modelName");

            if (!string.IsNullOrEmpty(friendlyName)) return friendlyName;
            if (!string.IsNullOrEmpty(manufacturer) && !string.IsNullOrEmpty(modelName)) return $"{manufacturer} {modelName}";
            if (!string.IsNullOrEmpty(manufacturer)) return manufacturer;
        }
        catch { }
        return string.Empty;
    }

    private static string ExtractXmlValue(string xml, string tag)
    {
        try
        {
            string startTag = $"<{tag}>";
            string endTag = $"</{tag}>";
            int start = xml.IndexOf(startTag);
            if (start == -1) return string.Empty;
            int end = xml.IndexOf(endTag, start);
            if (end == -1) return string.Empty;
            return xml.Substring(start + startTag.Length, end - (start + startTag.Length)).Trim();
        }
        catch { return string.Empty; }
    }

    // ── NEW: Centralized Discovery Sweep ──

    public static async Task StartDiscoverySweepAsync()
    {
        _discoveryCache.Clear();
        using var cts = new CancellationTokenSource(3000);
        
        var mdnsTask = Task.Run(() => RunMdnsSweepAsync(cts.Token));
        var ssdpTask = Task.Run(() => RunSsdpSweepAsync(cts.Token));

        await Task.WhenAll(mdnsTask, ssdpTask);
    }

    private static async Task RunMdnsSweepAsync(CancellationToken token)
    {
        try
        {
            using var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            var target = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

            string[] services = { 
                "_airplay._tcp.local", "_googlecast._tcp.local", "_raop._tcp.local", 
                "_spotify-connect._tcp.local", "_workstation._tcp.local", "_printer._tcp.local",
                "_ipp._tcp.local", "_smb._tcp.local" 
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
                
                // Low-level byte extraction for "Instance Name"
                string instanceName = ExtractMdnsInstanceName(result.Buffer);
                
                var data = _discoveryCache.GetOrAdd(senderIp, _ => new DiscoveryData());
                
                if (!string.IsNullOrEmpty(instanceName)) 
                {
                    if (string.IsNullOrEmpty(data.MdnsName) || data.MdnsName.Length < instanceName.Length)
                        data.MdnsName = instanceName;
                }

                // Substring fallback for service matching
                string raw = Encoding.UTF8.GetString(result.Buffer);
                if (raw.Contains("Apple") || raw.Contains("AirPlay")) data.Services.Add("Apple/AirPlay");
                if (raw.Contains("Google") || raw.Contains("Cast")) data.Services.Add("Google Cast");
                if (raw.Contains("Spotify")) data.Services.Add("Spotify");
                if (raw.Contains("Printer") || raw.Contains("ipp")) data.Services.Add("Printer");
            }
        }
        catch { }
    }

    private static string ExtractMdnsInstanceName(byte[] buffer)
    {
        try
        {
            // Simple heuristic: look for patterns like [length][name][length]local
            // We search for ".local" in bytes: 05 6c 6f 63 61 6c 00
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
                    // Filter out generic service types (starting with _)
                    if (!name.StartsWith("_") && name.Length > 2) return name;
                }
            }
        }
        catch { }
        return string.Empty;
    }

    private static async Task RunSsdpSweepAsync(CancellationToken token)
    {
        try
        {
            using var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            string mSearch = "M-SEARCH * HTTP/1.1\r\n" +
                             "HOST: 239.255.255.250:1900\r\n" +
                             "MAN: \"ssdp:discover\"\r\n" +
                             "ST: ssdp:all\r\n" +
                             "MX: 2\r\n\r\n";

            byte[] query = Encoding.UTF8.GetBytes(mSearch);
            var target = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            await udp.SendAsync(query, query.Length, target);

            while (!token.IsCancellationRequested)
            {
                var result = await udp.ReceiveAsync(token);
                string data = Encoding.UTF8.GetString(result.Buffer);
                string senderIp = result.RemoteEndPoint.Address.ToString();

                var disc = _discoveryCache.GetOrAdd(senderIp, _ => new DiscoveryData());

                if (data.Contains("LOCATION:"))
                {
                    var locationLine = data.Split('\n').FirstOrDefault(l => l.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase));
                    if (locationLine != null)
                    {
                        string url = locationLine.Substring(9).Trim();
                        string xmlDetails = await TryFetchSsdpLocationXmlAsync(url);
                        if (!string.IsNullOrEmpty(xmlDetails)) disc.SsdpName = xmlDetails;
                    }
                }

                if (string.IsNullOrEmpty(disc.SsdpName) && data.Contains("SERVER:"))
                {
                    var serverLine = data.Split('\n').FirstOrDefault(l => l.StartsWith("SERVER:", StringComparison.OrdinalIgnoreCase));
                    if (serverLine != null) disc.SsdpName = serverLine.Substring(7).Trim();
                }
            }
        }
        catch { }
    }
}
