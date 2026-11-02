using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core.Networking.Sockets;

/// <summary>
/// Abstraction for sending and receiving multicast UDP datagrams without binding to physical sockets during tests.
/// </summary>
public interface IMulticastSocket : IDisposable
{
    /// <summary>
    /// Sends a UDP datagram to the specified destination endpoint.
    /// </summary>
    Task<int> SendAsync(byte[] datagram, int bytes, IPEndPoint endPoint);

    /// <summary>
    /// Receives a UDP datagram asynchronously.
    /// </summary>
    Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken);
}
