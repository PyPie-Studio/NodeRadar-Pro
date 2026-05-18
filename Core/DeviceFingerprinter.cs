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

    /// <summary>
    /// Actively connects to open ports to "grab banners" and identify the device identity.
    /// This is a deterministic approach used by professional tools like Nmap.
    /// </summary>
    public static async Task<string> GetActiveBannerAsync(string ip, int port, CancellationToken token)
    {
        try
        {
            using var tcp = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(1500);

            try
            {
                await tcp.ConnectAsync(ip, port, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !token.IsCancellationRequested)
            {
                return string.Empty;
            }
            
            using var stream = tcp.GetStream();
            stream.ReadTimeout = 1500;
            
            if (port == 80 || port == 443 || port == 8080)
            {
                // HTTP Banner
                string req = $"GET / HTTP/1.1\r\nHost: {ip}\r\nConnection: close\r\n\r\n";
                byte[] reqBytes = Encoding.ASCII.GetBytes(req);
                await stream.WriteAsync(reqBytes, 0, reqBytes.Length, cts.Token);
                
                byte[] buffer = new byte[2048];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                string response = Encoding.UTF8.GetString(buffer, 0, read);
                
                var serverLine = response.Split('\n').FirstOrDefault(l => l.StartsWith("Server:", StringComparison.OrdinalIgnoreCase));
                string banner = serverLine?.Replace("Server:", "").Trim() ?? string.Empty;
                
                // Truncate and scrub (Huawei fix)
                banner = new string(banner.Where(c => c >= 32 && c < 127).Take(100).ToArray());
                return banner;
            }
            else
            {
                // TCP Greeting (SSH, FTP, etc.)
                byte[] buffer = new byte[512];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (read > 0)
                {
                    string greeting = Encoding.UTF8.GetString(buffer, 0, read).Trim();
                    // Truncate and scrub aggressively
                    greeting = new string(greeting.Where(c => c >= 32 && c < 127).Take(100).ToArray());
                    return greeting;
                }
            }
        }
        catch { }
        return string.Empty;
    }

    public static async Task<string> TryGetHttpServerBannerAsync(string ip)
    {
        return await GetActiveBannerAsync(ip, 80, CancellationToken.None);
    }

    /// <summary>
    /// Performs deep HTTP probes for specific indicators (Apple, IoT, Printers).
    /// </summary>
    public static async Task<string> ProbeHttpMetadataAsync(string ip)
    {
        // 1. Check for Apple devices via touch icon
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://{ip}/apple-touch-icon.png");
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            if (response.IsSuccessStatusCode)
            {
                // Professional Check: Ensure it's actually an image and not a redirected login page
                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != null && contentType.Contains("image/"))
                {
                    return "Web UI (Apple-Icon)";
                }
            }
        }
        catch { }

        // 2. Check for IoT / Printers via UPNP descriptors
        string[] upnpPaths = { "/upnp/desc.xml", "/description.xml", "/rootDesc.xml", "/device-description.xml" };
        foreach (var path in upnpPaths)
        {
            try
            {
                string url = $"http://{ip}{path}";
                string details = await TryFetchSsdpLocationXmlAsync(url);
                if (!string.IsNullOrEmpty(details)) return details;
            }
            catch { }
        }

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
            foreach (var service in data.Services.Distinct()) sb.Append($"{service} ");
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
        using var cts = new CancellationTokenSource(4000); // 4 seconds for a broad sweep
        
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
                "_ipp._tcp.local", "_smb._tcp.local", "_apple-mobdev2._tcp.local",
                "_companion-link._tcp.local", "_androidtv._tcp.local", "_amzn-alexa._tcp.local"
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
                
                string instanceName = ExtractMdnsInstanceName(result.Buffer);
                
                var data = _discoveryCache.GetOrAdd(senderIp, _ => new DiscoveryData());
                
                if (!string.IsNullOrEmpty(instanceName)) 
                {
                    if (string.IsNullOrEmpty(data.MdnsName) || data.MdnsName.Length < instanceName.Length)
                        data.MdnsName = instanceName;
                }

                string raw = Encoding.UTF8.GetString(result.Buffer);
                if (raw.Contains("Apple") || raw.Contains("AirPlay")) data.Services.Add("Apple AirPlay");
                if (raw.Contains("Google") || raw.Contains("Cast")) data.Services.Add("Google Cast");
                if (raw.Contains("Spotify")) data.Services.Add("Spotify");
                if (raw.Contains("Printer") || raw.Contains("ipp")) data.Services.Add("Network Printer");
                if (raw.Contains("Android") || raw.Contains("androidtv")) data.Services.Add("Android Device");
                if (raw.Contains("iPhone") || raw.Contains("iPad") || raw.Contains("MacBook") || raw.Contains("companion-link") || raw.Contains("apple-mobdev2")) data.Services.Add("Apple Device");
                if (raw.Contains("amzn-alexa")) data.Services.Add("Amazon Alexa");

                // Parse TXT records for model info
                ParseMdnsTxtRecords(result.Buffer, data);
            }
        }
        catch { }
    }

    private static void ParseMdnsTxtRecords(byte[] buffer, DiscoveryData data)
    {
        try
        {
            // Scan for common TXT keys: model=, am= (Apple Model), md= (Model Description)
            string[] keys = { "model=", "am=", "md=" };
            string raw = Encoding.UTF8.GetString(buffer);
            
            foreach (var key in keys)
            {
                int idx = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx != -1)
                {
                    int start = idx + key.Length;
                    // Find end of string (non-printable or next record)
                    int end = start;
                    while (end < raw.Length && raw[end] >= 32 && raw[end] < 127) end++;
                    
                    if (end > start)
                    {
                        string val = raw.Substring(start, end - start).Trim();
                        if (val.Length > 2 && !data.Services.Contains(val)) data.Services.Add(val);
                    }
                }
            }
        }
        catch { }
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
