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
}
