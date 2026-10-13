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
    private const string LogSource = "DiscoveryEngine";
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

            var merged = aggregatedDevices.AddOrUpdate(device.IpAddress, device, (ip, existing) => MergeDevice(existing, device));
            onDeviceDiscovered(merged);
        }

        // Run all protocols in parallel using Task Parallel Library
        var tasks = _methods.Select(method =>
        {
            return Task.Run(async () =>
            {
                try
                {
                    Logger.Log(LogLevel.Info, LogSource, $"Starting discovery method: {method.Name}...");
                    await method.DiscoverAsync(baseIp, targetIps, HandleDiscoveredDevice, ct);
                    Logger.Log(LogLevel.Info, LogSource, $"Completed discovery method: {method.Name}.");
                }
                catch (OperationCanceledException)
                {
                    Logger.Log(LogLevel.Info, LogSource, $"Discovery method cancelled: {method.Name}.");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Info, LogSource, $"Error in discovery method {method.Name}: {ex.Message}");
                }
            }, ct);
        });

        await Task.WhenAll(tasks);

        return aggregatedDevices.Values.ToList();
    }

    private static NetworkDevice MergeDevice(NetworkDevice existing, NetworkDevice incoming)
    {
        // MAC address resolution update
        if ((existing.MacAddress == "Unknown" || existing.MacAddress.StartsWith("L3-ROUTED-", StringComparison.Ordinal)) &&
            incoming.MacAddress != "Unknown" && !incoming.MacAddress.StartsWith("L3-ROUTED-", StringComparison.Ordinal))
        {
            existing.MacAddress = incoming.MacAddress;
        }

        // Vendor resolution update (overwrite if previous was unknown or privacy MAC and we got a real vendor)
        if ((existing.Vendor == "Unknown Vendor" || existing.Vendor == "Privacy MAC") &&
            incoming.Vendor != "Unknown Vendor")
        {
            existing.Vendor = incoming.Vendor;
        }

        // Hostname resolution update
        if ((existing.Hostname == "Unknown Device" || existing.Hostname == "Unknown mDNS Device") &&
            incoming.Hostname != "Unknown Device" && incoming.Hostname != "Unknown mDNS Device")
        {
            existing.Hostname = incoming.Hostname;
        }

        // Device Type resolution update
        if (existing.DeviceType == "Generic Device" && incoming.DeviceType != "Generic Device")
        {
            existing.DeviceType = incoming.DeviceType;
        }

        // Track protocols and log details
        if (!existing.SourceProtocol.Contains(incoming.SourceProtocol, StringComparison.OrdinalIgnoreCase))
        {
            existing.SourceProtocol += $", {incoming.SourceProtocol}";
        }
        existing.RawDetails += $"\r\n[{incoming.SourceProtocol}] {incoming.RawDetails}";

        return existing;
    }
}
