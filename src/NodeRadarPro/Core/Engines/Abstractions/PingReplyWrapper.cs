using System.Net.NetworkInformation;

namespace NodeRadarPro.Core.Engines.Abstractions;

public class PingReplyWrapper
{
    public IPStatus Status { get; set; }
    public long RoundtripTime { get; set; }
}
