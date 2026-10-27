using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Discovery;

/// <summary>
/// Production implementation of <see cref="IMulticastSocket"/> wrapping <see cref="UdpClient"/>.
/// </summary>
public class DefaultMulticastSocket : IMulticastSocket
{
    private readonly UdpClient _udp;

    public DefaultMulticastSocket(int bindPort = 0, IPAddress? multicastGroup = null)
    {
        _udp = new UdpClient();
        try
        {
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, bindPort));
        }
        catch (Exception) when (bindPort != 0)
        {
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        }

        if (multicastGroup != null)
        {
            try
            {
                _udp.JoinMulticastGroup(multicastGroup);
            }
            catch
            {
                // Group join may fail if multicast is disabled on the network adapter or in sandboxed CI
            }
        }
    }

    public Task<int> SendAsync(byte[] datagram, int bytes, IPEndPoint endPoint)
    {
        return _udp.SendAsync(datagram, bytes, endPoint);
    }

    public Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        return _udp.ReceiveAsync(cancellationToken).AsTask();
    }

    public void Dispose()
    {
        _udp.Dispose();
        GC.SuppressFinalize(this);
    }
}
