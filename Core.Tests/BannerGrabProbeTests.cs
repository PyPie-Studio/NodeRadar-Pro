using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace Core.Tests
{
    public class BannerGrabProbeTests
    {
        [Fact]
        public async Task ProbeAsync_WithNonHttpPort_ReadsBanner()
        {
            // Arrange
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var client = await listener.AcceptTcpClientAsync();
                    using var stream = client.GetStream();
                    byte[] data = Encoding.UTF8.GetBytes("Hello, Test Banner\r\n");
                    await stream.WriteAsync(data, 0, data.Length);
                }
                catch { }
            });

            var probe = new BannerGrabProbe();
            var node = new NetworkNode
            {
                IpAddress = "127.0.0.1",
                OpenPorts = new List<int> { port }
            };

            // Act
            var result = await probe.ProbeAsync(node, CancellationToken.None);

            // Assert
            Assert.Equal("Banner Grabbing", result.Source);
            Assert.True(result.RawData.ContainsKey($"Port_{port}_Banner"));
            Assert.Equal("Hello, Test Banner", result.RawData[$"Port_{port}_Banner"]);

            listener.Stop();
        }

        [Fact]
        public async Task ProbeAsync_WithHttpPort_ReadsServerBanner()
        {
            // Arrange
            int port = 8080;
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
            }
            catch (SocketException)
            {
                try
                {
                    port = 8443;
                    listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                }
                catch (SocketException)
                {
                    return; // Port in use, skip test
                }
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    using var client = await listener.AcceptTcpClientAsync();
                    using var stream = client.GetStream();

                    byte[] buffer = new byte[1024];
                    await stream.ReadAsync(buffer, 0, buffer.Length);

                    byte[] data = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nServer: MyTestHttpServer\r\n\r\n");
                    await stream.WriteAsync(data, 0, data.Length);
                }
                catch { }
            });

            var probe = new BannerGrabProbe();
            var node = new NetworkNode
            {
                IpAddress = "127.0.0.1",
                OpenPorts = new List<int> { port }
            };

            // Act
            var result = await probe.ProbeAsync(node, CancellationToken.None);

            // Assert
            Assert.True(result.RawData.ContainsKey($"Port_{port}_Banner"));
            Assert.Equal("MyTestHttpServer", result.RawData[$"Port_{port}_Banner"]);

            listener.Stop();
        }

        [Fact]
        public async Task ProbeAsync_WithExistingPortBanners_UsesExisting()
        {
            var probe = new BannerGrabProbe();
            var node = new NetworkNode
            {
                IpAddress = "127.0.0.1",
                OpenPorts = new List<int> { 22 },
                PortBanners = new Dictionary<int, string> { { 22, "ExistingSSH" } }
            };

            var result = await probe.ProbeAsync(node, CancellationToken.None);

            Assert.True(result.RawData.ContainsKey("Port_22_Banner"));
            Assert.Equal("ExistingSSH", result.RawData["Port_22_Banner"]);
        }

        [Fact]
        public async Task ProbeAsync_WhenCancelled_ReturnsEarly()
        {
            var probe = new BannerGrabProbe();
            var node = new NetworkNode
            {
                IpAddress = "127.0.0.1",
                OpenPorts = new List<int> { 22, 23 }
            };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await probe.ProbeAsync(node, cts.Token);

            Assert.Empty(result.RawData);
        }
    }
}
