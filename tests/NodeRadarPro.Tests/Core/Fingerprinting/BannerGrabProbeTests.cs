using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;
using Xunit;

namespace NodeRadarPro.Tests;

public class BannerGrabProbeTests
{
    [Fact]
    public void Properties_ReturnExpectedValues()
    {
        var probe = new BannerGrabProbe();
        Assert.Equal("Banner Grabbing", probe.Name);
        Assert.Equal(50, probe.Priority);
    }

    [Fact]
    public async Task ProbeAsync_WithNullOrEmptyOpenPorts_ReturnsEmptyResult()
    {
        var probe = new BannerGrabProbe();

        var nodeNull = new NetworkNode { IpAddress = "127.0.0.1", OpenPorts = null! };
        var resultNull = await probe.ProbeAsync(nodeNull, CancellationToken.None);
        Assert.Equal("Banner Grabbing", resultNull.Source);
        Assert.Empty(resultNull.RawData);

        var nodeEmpty = new NetworkNode { IpAddress = "127.0.0.1", OpenPorts = new List<int>() };
        var resultEmpty = await probe.ProbeAsync(nodeEmpty, CancellationToken.None);
        Assert.Equal("Banner Grabbing", resultEmpty.Source);
        Assert.Empty(resultEmpty.RawData);
    }

    [Fact]
    public async Task ProbeAsync_WithPortBannersContainingNullOrEmpty_FiltersEmptyValues()
    {
        var probe = new BannerGrabProbe();
        var node = new NetworkNode
        {
            IpAddress = "127.0.0.1",
            OpenPorts = new List<int> { 22, 80 },
            PortBanners = new Dictionary<int, string>
            {
                { 22, "SSH-2.0-OpenSSH_8.9" },
                { 80, "" }
            }
        };

        var result = await probe.ProbeAsync(node, CancellationToken.None);

        Assert.True(result.RawData.ContainsKey("Port_22_Banner"));
        Assert.False(result.RawData.ContainsKey("Port_80_Banner"));
        Assert.Equal("SSH-2.0-OpenSSH_8.9", result.RawData["Port_22_Banner"]);
    }

    [Fact]
    public async Task ProbeAsync_WithNonHttpPort_ReadsBanner()
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
            var result = await probe.ProbeAsync(node, CancellationToken.None);

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
    public async Task GrabBannerAsync_WithHttpServerResponse_ParsesServerHeaderAndStripsNonPrintable()
    {
        var (listener, port) = StartListenerOnAvailablePort(8080, 8008, 9000, 5000, 8000, 8888);

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                byte[] buffer = new byte[1024];
                _ = await stream.ReadAsync(buffer, 0, buffer.Length);

                string response = "HTTP/1.1 200 OK\r\nServer: CustomHttpServer/1.2\x07Special\r\nContent-Length: 0\r\n\r\n";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            catch { }
        });

        try
        {
            string banner = await BannerGrabProbe.GrabBannerAsync("127.0.0.1", port, CancellationToken.None);
            Assert.Equal("CustomHttpServer/1.2Special", banner);
        }
        finally
        {
            listener.Stop();
            await serverTask;
        }
    }

    [Fact]
    public async Task ProbeAsync_WithHttpPort_ExecutesHttpProbesAndHandlesMetadataFailureGracefully()
    {
        var (listener, port) = StartListenerOnAvailablePort(8080, 8008, 9000, 5000, 8000, 8888);

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                byte[] buffer = new byte[1024];
                _ = await stream.ReadAsync(buffer, 0, buffer.Length);

                string resp = "HTTP/1.1 200 OK\r\nServer: TestHttp/1.0\r\nContent-Length: 0\r\n\r\n";
                byte[] respBytes = Encoding.UTF8.GetBytes(resp);
                await stream.WriteAsync(respBytes, 0, respBytes.Length);
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
            var result = await probe.ProbeAsync(node, CancellationToken.None);

            Assert.Equal("Banner Grabbing", result.Source);
            Assert.True(result.RawData.ContainsKey($"Port_{port}_Banner"));
            Assert.Equal("TestHttp/1.0", result.RawData[$"Port_{port}_Banner"]);
        }
        finally
        {
            listener.Stop();
            await serverTask;
        }
    }

    [Fact]
    public async Task GrabBannerAsync_WithTlsNonHttpPort_ExtractsSslCertificateCn()
    {
        var (listener, port) = StartListenerOnAvailablePort(5061, 10000, 5001, 9443);
        using var cert = CreateSelfSignedCertificate("TestDevice.local");

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var sslStream = new SslStream(client.GetStream(), false);
                await sslStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
            }
            catch { }
        });

        try
        {
            string banner = await BannerGrabProbe.GrabBannerAsync("127.0.0.1", port, CancellationToken.None);
            Assert.Equal("[SSL CN: TestDevice.local]", banner);
        }
        finally
        {
            listener.Stop();
            await serverTask;
        }
    }

    [Fact]
    public async Task GrabBannerAsync_WithTlsAndHttpPort_CombinesServerBannerAndSslCn()
    {
        var (listener, port) = StartListenerOnAvailablePort(8443, 9443, 5001, 10000);
        using var cert = CreateSelfSignedCertificate("SecureRouter.local");

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var sslStream = new SslStream(client.GetStream(), false);
                await sslStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);

                byte[] buffer = new byte[1024];
                _ = await sslStream.ReadAsync(buffer, 0, buffer.Length);

                string response = "HTTP/1.1 200 OK\r\nServer: Lighttpd/1.4.65\r\nContent-Length: 0\r\n\r\n";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await sslStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            catch { }
        });

        try
        {
            string banner = await BannerGrabProbe.GrabBannerAsync("127.0.0.1", port, CancellationToken.None);
            Assert.Equal("Lighttpd/1.4.65 [SSL CN: SecureRouter.local]", banner);
        }
        finally
        {
            listener.Stop();
            await serverTask;
        }
    }

    [Fact]
    public async Task GrabBannerAsync_WithRdpPort3389_PerformsRdpNegotiationAndTlsHandshake()
    {
        TcpListener listener;
        int port;
        try
        {
            (listener, port) = StartListenerOnAvailablePort(3389);
        }
        catch (InvalidOperationException)
        {
            // Port 3389 is in use by the host OS (e.g. Windows Remote Desktop Service); skip test on host
            return;
        }

        using var cert = CreateSelfSignedCertificate("RdpHost.domain.local");

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                var stream = client.GetStream();

                byte[] req = new byte[1024];
                int read = await stream.ReadAsync(req, 0, req.Length);

                if (read > 0)
                {
                    byte[] rdpResp = new byte[] { 0x03, 0x00, 0x00, 0x0b, 0x06, 0xd0, 0x00, 0x00, 0x12, 0x00, 0x00 };
                    await stream.WriteAsync(rdpResp, 0, rdpResp.Length);

                    using var sslStream = new SslStream(stream, false);
                    await sslStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                }
            }
            catch { }
        });

        try
        {
            string banner = await BannerGrabProbe.GrabBannerAsync("127.0.0.1", port, CancellationToken.None);
            Assert.Equal("[SSL CN: RdpHost.domain.local]", banner);
        }
        finally
        {
            listener.Stop();
            await serverTask;
        }
    }

    [Fact]
    public async Task GrabBannerAsync_WhenConnectionRefused_ReturnsEmptyString()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int closedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        string banner = await BannerGrabProbe.GrabBannerAsync("127.0.0.1", closedPort, CancellationToken.None);
        Assert.Equal(string.Empty, banner);
    }

    private static (TcpListener listener, int port) StartListenerOnAvailablePort(params int[] preferredPorts)
    {
        foreach (var port in preferredPorts)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return (listener, port);
            }
            catch (SocketException) { }
        }

        throw new InvalidOperationException($"Could not bind any preferred port ({string.Join(", ", preferredPorts)}).");
    }

    private static X509Certificate2 CreateSelfSignedCertificate(string commonName)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var req = new CertificateRequest($"CN={commonName}", ecdsa, HashAlgorithmName.SHA256);
        using var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        byte[] pfxData = cert.Export(X509ContentType.Pfx);
        return X509CertificateLoader.LoadPkcs12(pfxData, null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
    }
}
