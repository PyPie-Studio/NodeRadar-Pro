using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Discovery;

public class DiscoveryEngine
{
    private readonly List<IDiscoveryMethod> _methods;

    public DiscoveryEngine()
        : this(new IDiscoveryMethod[]
        {
            new ArpDiscoveryMethod(),
            new MdnsDiscoveryMethod(),
            new SsdpDiscoveryMethod()
        })
    {
    }

    public DiscoveryEngine(IEnumerable<IDiscoveryMethod> methods)
    {
        _methods = methods?.ToList() ?? new List<IDiscoveryMethod>();
    }

    /// <summary>
    /// Executes all discovery methods in parallel and aggregates their results.
    /// </summary>
    /// <param name="baseIp">Subnet base IP (e.g. "192.168.1").</param>
    /// <param name="startIp">Starting last octet (e.g. 1).</param>
    /// <param name="endIp">Ending last octet (e.g. 254).</param>
    /// <param name="onDeviceDiscovered">Callback triggered whenever a device is resolved/merged.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of aggregated devices found.</returns>
    public async Task<List<NetworkDevice>> RunDiscoveryAsync(
        string baseIp,
        int startIp,
        int endIp,
        Action<NetworkDevice> onDeviceDiscovered,
        CancellationToken ct)
    {
        var targetIps = new List<IPAddress>();
        for (int i = startIp; i <= endIp; i++)
        {
            if (IPAddress.TryParse($"{baseIp}.{i}", out var ip))
            {
                targetIps.Add(ip);
            }
        }

        var aggregatedDevices = new ConcurrentDictionary<string, NetworkDevice>();

        void HandleDiscoveredDevice(NetworkDevice device)
        {
            if (string.IsNullOrEmpty(device.IpAddress)) return;

            var merged = aggregatedDevices.AddOrUpdate(device.IpAddress, device, (ip, existing) =>
            {
                // MAC address resolution update
                if ((existing.MacAddress == "Unknown" || existing.MacAddress.StartsWith("L3-ROUTED-")) &&
                    device.MacAddress != "Unknown" && !device.MacAddress.StartsWith("L3-ROUTED-"))
                {
                    existing.MacAddress = device.MacAddress;
                }

                // Vendor resolution update (overwrite if previous was unknown or privacy MAC and we got a real vendor)
                if ((existing.Vendor == "Unknown Vendor" || existing.Vendor == "Privacy MAC") &&
                    device.Vendor != "Unknown Vendor")
                {
                    existing.Vendor = device.Vendor;
                }

                // Hostname resolution update
                if ((existing.Hostname == "Unknown Device" || existing.Hostname == "Unknown mDNS Device") &&
                    device.Hostname != "Unknown Device" && device.Hostname != "Unknown mDNS Device")
                {
                    existing.Hostname = device.Hostname;
                }

                // Device Type resolution update
                if (existing.DeviceType == "Generic Device" && device.DeviceType != "Generic Device")
                {
                    existing.DeviceType = device.DeviceType;
                }

                // Track protocols and log details
                if (!existing.SourceProtocol.Contains(device.SourceProtocol))
                {
                    existing.SourceProtocol += $", {device.SourceProtocol}";
                }
                existing.RawDetails += $"\r\n[{device.SourceProtocol}] {device.RawDetails}";

                return existing;
            });

            onDeviceDiscovered(merged);
        }

        // Run all protocols in parallel using Task Parallel Library
        var tasks = _methods.Select(method =>
        {
            return Task.Run(async () =>
            {
                try
                {
                    DiagnosticLogger.Log("DiscoveryEngine", $"Starting discovery method: {method.Name}...");
                    await method.DiscoverAsync(baseIp, targetIps, HandleDiscoveredDevice, ct);
                    DiagnosticLogger.Log("DiscoveryEngine", $"Completed discovery method: {method.Name}.");
                }
                catch (OperationCanceledException)
                {
                    DiagnosticLogger.Log("DiscoveryEngine", $"Discovery method cancelled: {method.Name}.");
                }
                catch (Exception ex)
                {
                    DiagnosticLogger.Log("DiscoveryEngine", $"Error in discovery method {method.Name}: {ex.Message}");
                }
            }, ct);
        });

        await Task.WhenAll(tasks);

        return aggregatedDevices.Values.ToList();
    }
}
