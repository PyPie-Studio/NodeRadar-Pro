using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Discovery;

/// <summary>
/// Defines a contract for pluggable network discovery mechanisms (e.g. ARP, mDNS, SSDP).
/// </summary>
public interface IDiscoveryMethod
{
    /// <summary>
    /// Friendly protocol or discovery name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the discovery technique across the provided IP candidates.
    /// </summary>
    Task DiscoverAsync(
        string baseIp,
        List<IPAddress> targetIps,
        Action<NetworkDevice> onDeviceDiscovered,
        CancellationToken ct);
}
