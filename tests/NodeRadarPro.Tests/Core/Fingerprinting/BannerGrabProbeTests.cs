using System.Net;
using System.Net.Sockets;
using System.Text;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests;

public class BannerGrabProbeTests
{
    [Fact]
    public void Properties_NameAndPriority_AreCorrect()
    {
        var probe = new BannerGrabProbe();
        Assert.Equal("Banner Grabbing", probe.Name);
        Assert.Equal(50, probe.Priority);
    }

    [Fact]
    public async Task ProbeAsync_WithNullOrEmptyOpenPorts_ReturnsEmptyResult()
    {
        var probe = new BannerGrabProbe();

        var nodeWithNull = new NetworkNode { IpAddress = "127.0.0.1", OpenPorts = null! };
        var resultNull = await probe.ProbeAsync(nodeWithNull, CancellationToken.None);
        Assert.Equal("Banner Grabbing", resultNull.Source);
        Assert.Empty(resultNull.RawData);

        var nodeWithEmpty = new NetworkNode { IpAddress = "127.0.0.1", OpenPorts = new List<int>() };
        var resultEmpty = await probe.ProbeAsync(nodeWithEmpty, CancellationToken.None);
        Assert.Equal("Banner Grabbing", resultEmpty.Source);
        Assert.Empty(resultEmpty.RawData);
    }

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
    public async Task ProbeAsync_WithExistingPortBanners_UsesExistingAndFiltersEmpty()
    {
        var probe = new BannerGrabProbe();
        var node = new NetworkNode
        {
            IpAddress = "127.0.0.1",
            OpenPorts = new List<int> { 22, 80 },
            PortBanners = new Dictionary<int, string>
            {
                { 22, "ExistingSSH" },
                { 80, "" }
            }
        };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.True(result.RawData.ContainsKey("Port_22_Banner"));
        Assert.Equal("ExistingSSH", result.RawData["Port_22_Banner"]);
        Assert.False(result.RawData.ContainsKey("Port_80_Banner"));
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
    public async Task GrabBannerAsync_ConnectionFailure_ReturnsEmptyString()
    {
        // Try connecting to a closed port
        string banner = await BannerGrabProbe.GrabBannerAsync("127.0.0.1", 59999, CancellationToken.None);
        Assert.Equal(string.Empty, banner);
    }

    [Fact]
    public async Task GrabBannerAsync_HttpServer_ExtractsAndSanitizesServerHeader()
    {
        int[] candidatePorts = { 8080, 8008, 9000, 10000, 5000, 5001, 8443, 9443 };
        TcpListener? listener = null;
        int httpPort = 0;

        foreach (var p in candidatePorts)
        {
            try
            {
                var l = new TcpListener(IPAddress.Loopback, p);
                l.Start();
                listener = l;
                httpPort = p;
                break;
            }
            catch
            {
                // Port in use, try next
            }
        }

        if (listener == null)
        {
            // Fallback if no candidate port was available
            return;
        }

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                byte[] buffer = new byte[1024];
                _ = await stream.ReadAsync(buffer, 0, buffer.Length);

                string response = "HTTP/1.1 200 OK\r\nServer: nginx/1.18.0 \x07(Ubuntu)\r\nContent-Length: 0\r\n\r\n";
                byte[] respBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(respBytes, 0, respBytes.Length);
            }
            catch { }
        });

        try
        {
            string banner = await BannerGrabProbe.GrabBannerAsync("127.0.0.1", httpPort, CancellationToken.None);
            Assert.Equal("nginx/1.18.0 (Ubuntu)", banner);
        }
        finally
        {
            listener.Stop();
            await serverTask;
        }
    }

    [Fact]
    public async Task ProbeAsync_WithHttpPort_ExecutesWithoutErrorWhenNoService()
    {
        var probe = new BannerGrabProbe();
        var node = new NetworkNode
        {
            IpAddress = "127.0.0.1",
            OpenPorts = new List<int> { 59998 } // Not 80, 443, or 8080
        };

        var result = await probe.ProbeAsync(node, CancellationToken.None);
        Assert.Equal("Banner Grabbing", result.Source);
        Assert.False(result.RawData.ContainsKey("DeepHttpMetadata"));
    }
}
