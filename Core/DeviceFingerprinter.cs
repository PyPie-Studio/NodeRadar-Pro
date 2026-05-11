using System;
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
        try
        {
            // Note: In a real world production app, we would use a full mDNS library.
            // This is a simplified probe that checks for common service pointers.
            using var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            
            // Standard mDNS query for _services._dns-sd._udp.local
            byte[] query = {
                0x00, 0x00, // Transaction ID
                0x00, 0x00, // Flags
                0x00, 0x01, // Questions
                0x00, 0x00, // Answers
                0x00, 0x00, // Authority
                0x00, 0x00, // Additional
                0x09, 0x5f, 0x73, 0x65, 0x72, 0x76, 0x69, 0x63, 0x65, 0x73, // _services
                0x07, 0x5f, 0x64, 0x6e, 0x73, 0x2d, 0x73, 0x64,             // _dns-sd
                0x04, 0x5f, 0x75, 0x64, 0x70,                               // _udp
                0x05, 0x6c, 0x6f, 0x63, 0x61, 0x6c, 0x00,                   // local
                0x00, 0x0c, // Type PTR
                0x00, 0x01  // Class IN
            };

            var target = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
            await udp.SendAsync(query, query.Length, target);

            var result = await udp.ReceiveAsync(cts.Token);
            // Simplified parsing: Look for readable strings in the DNS packet
            string data = Encoding.UTF8.GetString(result.Buffer);
            if (data.Contains("Apple") || data.Contains("TV")) return "Apple TV / AirPlay";
            if (data.Contains("Chromecast")) return "Google Chromecast";
            if (data.Contains("Printer") || data.Contains("Canon") || data.Contains("HP")) return "Network Printer";
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }

        return string.Empty;
    }

    /// <summary>
    /// Attempts to identify device details using SSDP (Simple Service Discovery Protocol).
    /// </summary>
    public static async Task<string> DiscoverExactModelViaSSDPAsync(string ip)
    {
        using var cts = new CancellationTokenSource(1500);
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

            var result = await udp.ReceiveAsync(cts.Token);
            string data = Encoding.UTF8.GetString(result.Buffer);
            
            // Extract SERVER or friendlyName from SSDP response
            if (data.Contains("SERVER:"))
            {
                var lines = data.Split('\n');
                var serverLine = lines.FirstOrDefault(l => l.StartsWith("SERVER:", StringComparison.OrdinalIgnoreCase));
                if (serverLine != null) return serverLine.Substring(7).Trim();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }

        return string.Empty;
    }
}
