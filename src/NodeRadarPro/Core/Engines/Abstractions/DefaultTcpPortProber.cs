using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Engines.Abstractions;

public class DefaultTcpPortProber : ITcpPortProber
{
    public async Task<bool> ProbePortAsync(string ip, int port, CancellationToken token)
    {
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(ip, port, token);
            return tcp.Connected;
        }
        catch
        {
            return false;
        }
    }
}
