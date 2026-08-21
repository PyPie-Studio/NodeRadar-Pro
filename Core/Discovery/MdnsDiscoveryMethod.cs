using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Discovery;

public class MdnsDiscoveryMethod : IDiscoveryMethod
{
    public string Name => "mDNS / Bonjour";

    public async Task DiscoverAsync(string baseIp, List<IPAddress> targetIps, Action<NetworkDevice> onDeviceDiscovered, CancellationToken ct)
    {
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        // Try to bind to port 5353 (standard mDNS port)
        try
        {
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 5353));
        }
        catch
        {
            // If port 5353 is locked, bind to an ephemeral port
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        }

        try
        {
            udp.JoinMulticastGroup(IPAddress.Parse("224.0.0.251"));
        }
        catch { }

        var target = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

        // Standard local service queries to trigger sleeping mobile/IoT devices
        string[] services = {
            "_services._dns-sd._udp.local",
            "_airplay._tcp.local",
            "_googlecast._tcp.local",
            "_spotify-connect._tcp.local",
            "_printer._tcp.local",
            "_ipp._tcp.local",
            "_apple-mobdev2._tcp.local",
            "_companion-link._tcp.local",
            "_androidtv._tcp.local",
            "_http._tcp.local",
            "_homekit._tcp.local",
            "_hap._tcp.local"
        };

        // Listen in background
        var listenTask = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await udp.ReceiveAsync(ct);
                    string senderIp = result.RemoteEndPoint.Address.ToString();
                    if (senderIp.StartsWith("169.254")) continue;
                    byte[] buffer = result.Buffer;

                    DiagnosticLogger.Log(Name, $"Received response from {senderIp} ({buffer.Length} bytes)");

                    string hostname = ParseMdnsHostname(buffer);
                    if (!string.IsNullOrEmpty(hostname) && hostname != "Unknown")
                    {
                        string deviceType = GetDeviceTypeFromMdns(hostname, buffer);
                        string vendor = GetVendorFromMdns(hostname, buffer);

                        var device = new NetworkDevice
                        {
                            IpAddress = senderIp,
                            Hostname = hostname,
                            Vendor = vendor,
                            DeviceType = deviceType,
                            SourceProtocol = Name,
                            IsOnline = true,
                            RawDetails = $"mDNS Hostname: {hostname} | Response Length: {buffer.Length} bytes"
                        };

                        DiagnosticLogger.Log(Name, $"Discovered Device: IP={senderIp}, Host={hostname}, Vendor={vendor}, Type={deviceType}");
                        onDeviceDiscovered(device);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (SocketException ex)
                {
                    DiagnosticLogger.Log(Name, $"Socket closed or error in listening loop: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    DiagnosticLogger.Log(Name, $"Fatal error in listening loop: {ex.Message}");
                    break;
                }
            }
        }, ct);

        // Send query packets
        foreach (var service in services)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                byte[] query = CreateMdnsQuery(service);
                await udp.SendAsync(query, query.Length, target);
                DiagnosticLogger.Log(Name, $"Sent query for service: {service}");
            }
            catch (Exception ex)
            {
                DiagnosticLogger.Log(Name, $"Error sending query for {service}: {ex.Message}");
            }
            await Task.Delay(50, ct);
        }

        // Wait for responses
        try
        {
            await Task.Delay(3000, ct);
        }
        catch (OperationCanceledException) { }
    }

    private byte[] CreateMdnsQuery(string serviceName)
    {
        var packet = new List<byte> {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        foreach (var part in serviceName.Split('.'))
        {
            if (string.IsNullOrEmpty(part)) continue;
            packet.Add((byte)part.Length);
            packet.AddRange(Encoding.ASCII.GetBytes(part));
        }
        packet.Add(0x00);
        packet.Add(0x00); packet.Add(0x0c); // Type: PTR
        packet.Add(0x00); packet.Add(0x01); // Class: IN
        return packet.ToArray();
    }

    public static string ParseMdnsHostname(byte[] buffer)
    {
        // 1. Try local pattern mapping
        try
        {
            byte[] localPattern = { 0x05, 0x6c, 0x6f, 0x63, 0x61, 0x6c, 0x00 }; // ".local"
            int localIdx = -1;
            for (int i = 0; i <= buffer.Length - 7; i++)
            {
                bool match = true;
                for (int j = 0; j < 7; j++)
                {
                    if (buffer[i + j] != localPattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    localIdx = i;
                    break;
                }
            }

            if (localIdx > 1)
            {
                int start = localIdx - 1;
                while (start >= 0)
                {
                    char c = (char)buffer[start];
                    if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    {
                        break;
                    }
                    start--;
                }

                if (start >= 0)
                {
                    int nameLen = buffer[start];
                    if (nameLen > 0 && start + 1 + nameLen == localIdx)
                    {
                        string name = Encoding.UTF8.GetString(buffer, start + 1, nameLen);
                        if (!name.StartsWith("_") && name.Length > 2)
                        {
                            return name + ".local";
                        }
                    }
                }
            }
        }
        catch { }

        // 2. Fallback: Search printable characters for ".local"
        try
        {
            string raw = Encoding.UTF8.GetString(buffer);
            int localIdx = raw.IndexOf(".local", StringComparison.OrdinalIgnoreCase);
            if (localIdx > 0)
            {
                int start = localIdx - 1;
                while (start >= 0 && (char.IsLetterOrDigit(raw[start]) || raw[start] == '-' || raw[start] == '_'))
                {
                    start--;
                }
                if (localIdx - start - 1 > 2)
                {
                    string hostname = raw.Substring(start + 1, localIdx - start - 1 + 6);
                    if (!hostname.StartsWith("_"))
                    {
                        return hostname;
                    }
                }
            }
        }
        catch { }

        return string.Empty;
    }

    private string GetDeviceTypeFromMdns(string hostname, byte[] buffer)
    {
        string hostLower = hostname.ToLowerInvariant();
        if (hostLower.Contains("iphone") || hostLower.Contains("ipad") || hostLower.Contains("phone"))
            return "Mobile Device";
        if (hostLower.Contains("android"))
            return "Mobile Device";
        if (hostLower.Contains("tv"))
            return "Smart TV";
        if (hostLower.Contains("printer"))
            return "Printer";

        string raw = Encoding.UTF8.GetString(buffer).ToLowerInvariant();
        if (raw.Contains("iphone") || raw.Contains("ipad") || raw.Contains("apple-mobdev2"))
            return "Mobile Device";
        if (raw.Contains("android") || raw.Contains("googlecast"))
            return "Mobile Device";

        return "Generic Device";
    }

    private string GetVendorFromMdns(string hostname, byte[] buffer)
    {
        string hostLower = hostname.ToLowerInvariant();
        if (hostLower.Contains("iphone") || hostLower.Contains("ipad") || hostLower.Contains("apple"))
            return "Apple";
        if (hostLower.Contains("android"))
            return "Google / Android";
        if (hostLower.Contains("huawei"))
            return "Huawei";
        if (hostLower.Contains("xiaomi"))
            return "Xiaomi";

        string raw = Encoding.UTF8.GetString(buffer).ToLowerInvariant();
        if (raw.Contains("apple") || raw.Contains("iphone") || raw.Contains("ipad"))
            return "Apple";
        if (raw.Contains("google"))
            return "Google";
        if (raw.Contains("huawei"))
            return "Huawei";
        if (raw.Contains("xiaomi"))
            return "Xiaomi";

        return "Unknown Vendor";
    }
}
