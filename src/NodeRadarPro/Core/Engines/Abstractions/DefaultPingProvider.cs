using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Engines.Abstractions;

public class DefaultPingProvider : IPingProvider
{
    public async Task<PingReplyWrapper> SendPingAsync(string hostNameOrAddress, int timeout)
    {
        using var pinger = new Ping();
        var reply = await pinger.SendPingAsync(hostNameOrAddress, timeout);
        return new PingReplyWrapper
        {
            Status = reply.Status,
            RoundtripTime = reply.RoundtripTime
        };
    }
}
