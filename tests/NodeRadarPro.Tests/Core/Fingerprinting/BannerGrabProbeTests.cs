using System.Net;
using System.Net.Sockets;
using System.Text;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests;

public class BannerGrabProbeTests
{
    [Fact]
    public async Task ProbeAsync_WithNonHttpPort_ReadsBanner()
    {
        // Arrange - bind ephemeral port 0
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes("Hello, Test Banner\r\n");
                await stream.WriteAsync(data.AsMemory());
            }
            catch { }
        });

        var probe = new BannerGrabProbe();
        var node = new NetworkNode
        {
            IpAddress = "127.0.0.1",
            OpenPorts = new List<int> { port }
        };

        try
        {
            // Act
            var result = await probe.ProbeAsync(node, CancellationToken.None);

            // Assert
            Assert.Equal("Banner Grabbing", result.Source);
            Assert.True(result.RawData.ContainsKey($"Port_{port}_Banner"));
            Assert.Equal("Hello, Test Banner", result.RawData[$"Port_{port}_Banner"]);
        }
        finally
        {
            listener.Stop();
            await serverTask;
        }
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

    [Fact]
    public void BannerGrabProbe_Properties_ReturnExpectedMetadata()
    {
        var probe = new BannerGrabProbe();
        Assert.Equal("Banner Grabbing", probe.Name);
        Assert.Equal(50, probe.Priority);
    }

    [Fact]
    public async Task ProbeAsync_WithClosedHttpPort_HandlesHttpClientExceptionGracefully()
    {
        var probe = new BannerGrabProbe();
        var node = new NetworkNode
        {
            IpAddress = "127.0.0.1",
            OpenPorts = new List<int> { 80 }
        };

        // Port 80 on localhost exercises HttpClient with SocketsHttpHandler PooledConnectionLifetime
        var result = await probe.ProbeAsync(node, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("Banner Grabbing", result.Source);
    }

    [Fact]
    public async Task ProbeAsync_WithCustomBannerPort_ReadsServerBanner()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes("CustomServer/2.0\r\n");
                await stream.WriteAsync(data);
            }
            catch (Exception)
            {
                /* Mock test server closed */
            }
        });

        var probe = new BannerGrabProbe();
        var node = new NetworkNode
        {
            IpAddress = "127.0.0.1",
            OpenPorts = new List<int> { port }
        };

        try
        {
            var result = await probe.ProbeAsync(node, CancellationToken.None);
            Assert.Equal("Banner Grabbing", result.Source);
            Assert.True(result.RawData.ContainsKey($"Port_{port}_Banner"));
            Assert.Equal("CustomServer/2.0", result.RawData[$"Port_{port}_Banner"]);
        }
        finally
        {
            listener.Stop();
            await serverTask;
        }
    }
}
