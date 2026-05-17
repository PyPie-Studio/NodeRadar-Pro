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
    /// Attempts to identify the exact device model using mDNS (Multicast DNS).
    /// </summary>
    public static async Task<string> DiscoverExactModelViaMDnsAsync(string ip)
    {
        using var cts = new CancellationTokenSource(1500);
        var discoveredServices = new StringBuilder();
        try
        {
            using var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            
            var target = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

            string[] servicesToQuery = {
                "_services._dns-sd._udp.local",
                "_airplay._tcp.local",
                "_raop._tcp.local",
                "_workstation._tcp.local",
                "_smb._tcp.local",
                "_googlecast._tcp.local",
                "_spotify-connect._tcp.local"
            };

            foreach (var service in servicesToQuery)
            {
                byte[] query = CreateMdnsQuery(service);
                await udp.SendAsync(query, query.Length, target);
            }

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var receiveTask = udp.ReceiveAsync(cts.Token);
                    if (await Task.WhenAny(receiveTask.AsTask(), Task.Delay(400, cts.Token)) == receiveTask.AsTask())
                    {
                        var result = await receiveTask;
                        string data = Encoding.UTF8.GetString(result.Buffer);

                        if (data.Contains("Apple") || data.Contains("AirPlay") || data.Contains("HomeKit")) 
                            discoveredServices.Append("Apple Device (mDNS) ");
                        if (data.Contains("Chromecast") || data.Contains("Google"))
                            discoveredServices.Append("Google Cast ");
                        if (data.Contains("Spotify"))
                            discoveredServices.Append("Spotify Connect ");
                        if (data.Contains("Printer") || data.Contains("ipp"))
                            discoveredServices.Append("Network Printer ");
                        if (data.Contains("Workstation") || data.Contains("smb"))
                            discoveredServices.Append("PC/Server ");
                        
                        // If we already have multiple hits, we might have enough info
                        if (discoveredServices.Length > 40) break;
                    }
                    else break;
                }
                catch { break; }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }

        return discoveredServices.ToString().Trim();
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
    /// Attempts to identify device details using SSDP (Simple Service Discovery Protocol).
    /// </summary>
    public static async Task<string> DiscoverExactModelViaSSDPAsync(string ip)
    {
        using var cts = new CancellationTokenSource(2000);
        try
        {
            using var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            string mSearch = "M-SEARCH * HTTP/1.1\r\n" +
                             "HOST: 239.255.255.250:1900\r\n" +
                             "MAN: \"ssdp:discover\"\r\n" +
                             "ST: ssdp:all\r\n" +
                             "MX: 1\r\n\r\n";

            byte[] query = Encoding.UTF8.GetBytes(mSearch);
            var target = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            await udp.SendAsync(query, query.Length, target);

            while (!cts.Token.IsCancellationRequested)
            {
                var receiveTask = udp.ReceiveAsync(cts.Token);
                if (await Task.WhenAny(receiveTask.AsTask(), Task.Delay(800, cts.Token)) == receiveTask.AsTask())
                {
                    var result = await receiveTask;
                    string data = Encoding.UTF8.GetString(result.Buffer);
                    
                    if (data.Contains("LOCATION:"))
                    {
                        var lines = data.Split('\n');
                        var locationLine = lines.FirstOrDefault(l => l.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase));
                        if (locationLine != null)
                        {
                            string url = locationLine.Substring(9).Trim();
                            string xmlDetails = await TryFetchSsdpLocationXmlAsync(url);
                            if (!string.IsNullOrEmpty(xmlDetails)) return xmlDetails;
                        }
                    }

                    if (data.Contains("SERVER:"))
                    {
                        var lines = data.Split('\n');
                        var serverLine = lines.FirstOrDefault(l => l.StartsWith("SERVER:", StringComparison.OrdinalIgnoreCase));
                        if (serverLine != null) return serverLine.Substring(7).Trim();
                    }
                }
                else break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }

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
}
