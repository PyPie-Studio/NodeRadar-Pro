using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Fingerprinting.Probes;

public class BannerGrabProbe : IFingerprintProbe
{
    public string Name => "Banner Grabbing";
    public int Priority => 50;

    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

    public async Task<ProbeResult> ProbeAsync(NetworkNode node, CancellationToken ct)
    {
        var result = new ProbeResult { Source = Name };

        if (node.OpenPorts == null || node.OpenPorts.Count == 0)
        {
            return result;
        }

        if (node.PortBanners != null && node.PortBanners.Count > 0)
        {
            foreach (var kvp in node.PortBanners)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                    result.RawData[$"Port_{kvp.Key}_Banner"] = kvp.Value;
            }
        }
        else
        {
            foreach (int port in node.OpenPorts)
            {
                if (ct.IsCancellationRequested) break;

                string banner = await GrabBannerAsync(node.IpAddress, port, ct);
                if (!string.IsNullOrEmpty(banner))
                {
                    result.RawData[$"Port_{port}_Banner"] = banner;
                }
            }
        }

        // Additional HTTP Probes (Apple touch icon, UPnP descriptors not caught by SSDP)
        if (node.OpenPorts.Contains(80) || node.OpenPorts.Contains(443) || node.OpenPorts.Contains(8080))
        {
            string deepHttp = await ProbeHttpMetadataAsync(node.IpAddress, ct);
            if (!string.IsNullOrEmpty(deepHttp))
            {
                result.RawData["DeepHttpMetadata"] = deepHttp;
            }
        }

        return result;
    }

    public static async Task<string> GrabBannerAsync(string ip, int port, CancellationToken token)
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
            
            if (port == 80 || port == 443 || port == 8080 || port == 8443)
            {
                string req = $"GET / HTTP/1.1\r\nHost: {ip}\r\nConnection: close\r\n\r\n";
                byte[] reqBytes = Encoding.ASCII.GetBytes(req);
                await stream.WriteAsync(reqBytes, 0, reqBytes.Length, cts.Token);
                
                byte[] buffer = new byte[2048];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                string response = Encoding.UTF8.GetString(buffer, 0, read);
                
                var serverLine = response.Split('\n').FirstOrDefault(l => l.StartsWith("Server:", StringComparison.OrdinalIgnoreCase));
                string banner = serverLine?.Replace("Server:", "").Trim() ?? string.Empty;
                
                banner = new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(banner, c => c >= 32 && c < 127).Take(100)));
                return banner;
            }
            else
            {
                byte[] buffer = new byte[512];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (read > 0)
                {
                    string greeting = Encoding.UTF8.GetString(buffer, 0, read).Trim();
                    greeting = new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(greeting, c => c >= 32 && c < 127).Take(100)));
                    return greeting;
                }
            }
        }
        catch { }
        return string.Empty;
    }

    private static async Task<string> ProbeHttpMetadataAsync(string ip, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://{ip}/apple-touch-icon.png");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(2000);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != null && contentType.Contains("image/"))
                {
                    return "Web UI (Apple-Icon)";
                }
            }
        }
        catch { }
        return string.Empty;
    }
}
