using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Engines.Abstractions;

public interface ITcpPortProber
{
    Task<bool> ProbePortAsync(string ip, int port, CancellationToken token);
}
