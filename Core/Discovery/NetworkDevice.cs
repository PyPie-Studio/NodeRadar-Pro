namespace NodeRadarPro.Core.Discovery;

/// <summary>
/// Aggregates network discovery metadata from multiple protocols (ARP, mDNS, SSDP).
/// </summary>
public class NetworkDevice
{
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = "Unknown";
    public string Hostname { get; set; } = "Unknown Device";
    public string Vendor { get; set; } = "Unknown Vendor";
    public string DeviceType { get; set; } = "Generic Device";
    public string SourceProtocol { get; set; } = string.Empty;
    public string RawDetails { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = false;
}
