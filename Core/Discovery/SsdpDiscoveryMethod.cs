using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Discovery;

public class SsdpDiscoveryMethod : IDiscoveryMethod
{
    public string Name => "UPnP / SSDP";

    public async Task DiscoverAsync(string baseIp, List<IPAddress> targetIps, Action<NetworkDevice> onDeviceDiscovered, CancellationToken ct)
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

        // Listen loop in background
        var listenTask = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await udp.ReceiveAsync(ct);
                    string senderIp = result.RemoteEndPoint.Address.ToString();
                    if (senderIp.StartsWith("169.254")) continue;
                    string data = Encoding.UTF8.GetString(result.Buffer);

                    DiagnosticLogger.Log(Name, $"Received response from {senderIp} ({result.Buffer.Length} bytes):\r\n{data}");

                    // Parse SSDP response headers
                    string server = string.Empty;
                    string location = string.Empty;

                    var lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("SERVER:", StringComparison.OrdinalIgnoreCase))
                        {
                            server = line.Substring(7).Trim();
                        }
                        else if (line.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase))
                        {
                            location = line.Substring(9).Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(server) || !string.IsNullOrEmpty(location))
                    {
                        string vendor = "Unknown Vendor";
                        string deviceType = "Router / Gateway";
                        string hostname = "Router / ONU";

                        // Fingerprint ONU/Huawei HG8120C and routers from analysis guidelines
                        if (server.Contains("Huawei", StringComparison.OrdinalIgnoreCase) ||
                            location.Contains("huawei", StringComparison.OrdinalIgnoreCase))
                        {
                            vendor = "Huawei";
                            hostname = "Huawei HG8120C ONU";
                            deviceType = "Router / Gateway";
                        }
                        else if (server.Contains("TP-Link", StringComparison.OrdinalIgnoreCase) ||
                                 location.Contains("tplink", StringComparison.OrdinalIgnoreCase))
                        {
                            vendor = "TP-Link";
                            hostname = "TP-Link Router";
                        }
                        else if (server.Contains("MikroTik", StringComparison.OrdinalIgnoreCase))
                        {
                            vendor = "MikroTik";
                            hostname = "MikroTik Router";
                        }
                        else if (!string.IsNullOrEmpty(server))
                        {
                            vendor = ParseVendorFromServerHeader(server);
                            hostname = $"{vendor} Router";
                        }

                        var device = new NetworkDevice
                        {
                            IpAddress = senderIp,
                            Hostname = hostname,
                            Vendor = vendor,
                            DeviceType = deviceType,
                            SourceProtocol = Name,
                            IsOnline = true,
                            RawDetails = $"Server: {server} | Location: {location}"
                        };

                        DiagnosticLogger.Log(Name, $"Discovered UPnP: IP={senderIp}, Host={hostname}, Vendor={vendor}");
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

        // Send query
        try
        {
            await udp.SendAsync(query, query.Length, target);
            DiagnosticLogger.Log(Name, "Broadcasted SSDP M-SEARCH discovery packet.");
        }
        catch (Exception ex)
        {
            DiagnosticLogger.Log(Name, $"Error sending M-SEARCH query: {ex.Message}");
        }

        // Keep listening for 3 seconds
        try
        {
            await Task.Delay(3000, ct);
        }
        catch (OperationCanceledException) { }
    }

    private string ParseVendorFromServerHeader(string serverHeader)
    {
        string[] knownVendors = { "Huawei", "TP-Link", "MikroTik", "Cisco", "Netgear", "Linksys", "D-Link", "ASUS", "Xiaomi" };
        foreach (var vendor in knownVendors)
        {
            if (serverHeader.Contains(vendor, StringComparison.OrdinalIgnoreCase))
            {
                return vendor;
            }
        }

        // Extract first word from Server header as a fallback vendor
        var parts = serverHeader.Split(new[] { ' ', '/', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && parts[0].Length > 2 && !parts[0].Equals("UPnP", StringComparison.OrdinalIgnoreCase))
        {
            return parts[0];
        }

        return "Generic Vendor";
    }
}
