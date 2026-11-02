using System.Threading.Tasks;

namespace NodeRadarPro.Core.Engines.Abstractions;

public interface IPingProvider
{
    Task<PingReplyWrapper> SendPingAsync(string hostNameOrAddress, int timeout);
}
