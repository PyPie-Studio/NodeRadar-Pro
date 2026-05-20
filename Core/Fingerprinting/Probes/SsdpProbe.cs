using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Fingerprinting.Probes;

public class SsdpProbe : IFingerprintProbe
{
    public string Name => "SSDP / UPnP";
    public int Priority => 30;

    public class SsdpData
    {
        public string Server { get; set; } = "";
        public string FriendlyName { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string DeviceType { get; set; } = "";
    }

    private static readonly ConcurrentDictionary<string, SsdpData> _cache = new();
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

    public static async Task StartSweepAsync(CancellationToken token)
    {
        _cache.Clear();
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

                var disc = _cache.GetOrAdd(senderIp, _ => new SsdpData());

                if (data.Contains("SERVER:", StringComparison.OrdinalIgnoreCase))
                {
                    var serverLine = data.Split('\n').FirstOrDefault(l => l.StartsWith("SERVER:", StringComparison.OrdinalIgnoreCase));
                    if (serverLine != null) disc.Server = serverLine.Substring(7).Trim();
                }

                if (data.Contains("LOCATION:", StringComparison.OrdinalIgnoreCase))
                {
                    var locationLine = data.Split('\n').FirstOrDefault(l => l.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase));
                    if (locationLine != null)
                    {
                        string url = locationLine.Substring(9).Trim();
                        await TryFetchSsdpLocationXmlAsync(url, disc);
                    }
                }
            }
        }
        catch { }
    }

    public Task<ProbeResult> ProbeAsync(NetworkNode node, CancellationToken ct)
    {
        var result = new ProbeResult { Source = Name };

        if (_cache.TryGetValue(node.IpAddress, out var data))
        {
            if (!string.IsNullOrEmpty(data.Server)) result.RawData["Server"] = data.Server;
            if (!string.IsNullOrEmpty(data.FriendlyName)) result.RawData["FriendlyName"] = data.FriendlyName;
            if (!string.IsNullOrEmpty(data.Manufacturer)) result.RawData["Manufacturer"] = data.Manufacturer;
            if (!string.IsNullOrEmpty(data.ModelName)) result.RawData["ModelName"] = data.ModelName;
            if (!string.IsNullOrEmpty(data.DeviceType)) result.RawData["DeviceType"] = data.DeviceType;
        }

        return Task.FromResult(result);
    }

    private static async Task TryFetchSsdpLocationXmlAsync(string url, SsdpData data)
    {
        try
        {
            string xml = await _httpClient.GetStringAsync(url);
            
            string fn = ExtractXmlValue(xml, "friendlyName");
            if (!string.IsNullOrEmpty(fn)) data.FriendlyName = fn;

            string mfg = ExtractXmlValue(xml, "manufacturer");
            if (!string.IsNullOrEmpty(mfg)) data.Manufacturer = mfg;

            string mn = ExtractXmlValue(xml, "modelName");
            if (!string.IsNullOrEmpty(mn)) data.ModelName = mn;

            string dt = ExtractXmlValue(xml, "deviceType");
            if (!string.IsNullOrEmpty(dt)) data.DeviceType = dt;
        }
        catch { }
    }

    private static string ExtractXmlValue(string xml, string tag)
    {
        try
        {
            string startTag = $"<{tag}>";
            string endTag = $"</{tag}>";
            int start = xml.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            if (start == -1) return string.Empty;
            int end = xml.IndexOf(endTag, start, StringComparison.OrdinalIgnoreCase);
            if (end == -1) return string.Empty;
            return xml.Substring(start + startTag.Length, end - (start + startTag.Length)).Trim();
        }
        catch { return string.Empty; }
    }
}
