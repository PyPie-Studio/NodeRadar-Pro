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
    private const string DeviceTypeRouterGateway = "Router / Gateway";

    public Task DiscoverAsync(string baseIp, List<IPAddress> targetIps, Action<NetworkDevice> onDeviceDiscovered, CancellationToken ct)
    {
        return DiscoverAsync(baseIp, targetIps, onDeviceDiscovered, null, ct);
    }

    public async Task DiscoverAsync(string baseIp, List<IPAddress> targetIps, Action<NetworkDevice> onDeviceDiscovered, Func<UdpClient>? udpClientFactory = null, CancellationToken ct = default)
    {
        using var udp = udpClientFactory?.Invoke() ?? new UdpClient();
        try
        {
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Info, Name, $"Socket initialization failed: {ex.Message}");
            return;
        }

        string mSearch = "M-SEARCH * HTTP/1.1\r\n" +
                         "HOST: 239.255.255.250:1900\r\n" +
                         "MAN: \"ssdp:discover\"\r\n" +
                         "ST: ssdp:all\r\n" +
                         "MX: 2\r\n\r\n";

        byte[] query = Encoding.UTF8.GetBytes(mSearch);
        var target = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);

        // Listen loop in background
        _ = Task.Run(() => ProcessIncomingResponsesAsync(udp, Name, onDeviceDiscovered, ct), ct);

        // Send query
        try
        {
            await udp.SendAsync(query, query.Length, target);
            Logger.Log(LogLevel.Info, Name, "Broadcasted SSDP M-SEARCH discovery packet.");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Info, Name, $"Error sending M-SEARCH query: {ex.Message}");
        }

        // Keep listening for 3 seconds
        try
        {
            await Task.Delay(3000, ct);
        }
        catch (OperationCanceledException) { }
    }

    private static async Task ProcessIncomingResponsesAsync(
        UdpClient udp,
        string protocolName,
        Action<NetworkDevice> onDeviceDiscovered,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await udp.ReceiveAsync(ct);
                string senderIp = result.RemoteEndPoint.Address.ToString();
                if (senderIp.StartsWith("169.254", StringComparison.Ordinal))
                {
                    continue;
                }

                string data = Encoding.UTF8.GetString(result.Buffer);
                Logger.Log(LogLevel.Info, protocolName, $"Received response from {senderIp} ({result.Buffer.Length} bytes):\r\n{data}");

                var device = TryParseSsdpResponse(senderIp, data, protocolName);
                if (device != null)
                {
                    Logger.Log(LogLevel.Info, protocolName, $"Discovered UPnP: IP={senderIp}, Host={device.Hostname}, Vendor={device.Vendor}");
                    onDeviceDiscovered(device);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException ex)
            {
                Logger.Log(LogLevel.Info, protocolName, $"Socket closed or error in listening loop: {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Info, protocolName, $"Fatal error in listening loop: {ex.Message}");
                break;
            }
        }
    }

    private static (string Vendor, string Hostname, string DeviceType) IdentifyDevice(string server, string location)
    {
        if (server.Contains("Huawei", StringComparison.OrdinalIgnoreCase) ||
            location.Contains("huawei", StringComparison.OrdinalIgnoreCase))
        {
            return ("Huawei", "Huawei HG8120C ONU", DeviceTypeRouterGateway);
        }

        if (server.Contains("TP-Link", StringComparison.OrdinalIgnoreCase) ||
            location.Contains("tplink", StringComparison.OrdinalIgnoreCase))
        {
            return ("TP-Link", "TP-Link Router", DeviceTypeRouterGateway);
        }

        if (server.Contains("MikroTik", StringComparison.OrdinalIgnoreCase))
        {
            return ("MikroTik", "MikroTik Router", DeviceTypeRouterGateway);
        }

        if (!string.IsNullOrEmpty(server))
        {
            var vendor = ParseVendorFromServerHeader(server);
            return (vendor, $"{vendor} Router", DeviceTypeRouterGateway);
        }

        return ("Unknown Vendor", "Router / ONU", DeviceTypeRouterGateway);
    }

    private static NetworkDevice? TryParseSsdpResponse(string senderIp, string data, string protocolName)
    {
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

        if (string.IsNullOrEmpty(server) && string.IsNullOrEmpty(location))
        {
            return null;
        }

        var (vendor, hostname, deviceType) = IdentifyDevice(server, location);

        return new NetworkDevice
        {
            IpAddress = senderIp,
            Hostname = hostname,
            Vendor = vendor,
            DeviceType = deviceType,
            SourceProtocol = protocolName,
            IsOnline = true,
            RawDetails = $"Server: {server} | Location: {location}"
        };
    }

    public static string ParseVendorFromServerHeader(string? serverHeader)
    {
        if (string.IsNullOrWhiteSpace(serverHeader))
        {
            return "Generic Vendor";
        }

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
