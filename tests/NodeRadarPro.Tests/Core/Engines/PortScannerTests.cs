using System.Net;
using System.Net.Sockets;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests
{
    public class PortScannerTests
    {
        private static (TcpListener listener, int port) BindAvailablePortFromList(int[] candidatePorts)
        {
            foreach (var port in candidatePorts)
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    return (listener, port);
                }
                catch
                {
                    // Port in use, try next
                }
            }
            throw new InvalidOperationException("No available ports found in the candidate list.");
        }

        [Fact]
        public async Task ScanPortsAsync_ShouldDetectOpenPort_CommonScan()
        {
            // Arrange
            int[] candidatePorts = new[] { 62078, 8443, 8080, 5900 };
            var (listener, testPort) = BindAvailablePortFromList(candidatePorts);

            try
            {
                // Act
                var result = await PortScanner.ScanPortsAsync("127.0.0.1", fastScan: false, timeoutMs: 2000);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.ContainsKey(testPort), $"Expected port {testPort} to be detected as open.");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ScanPortsAsync_ShouldDetectOpenPort_FastScan()
        {
            // Arrange
            int[] candidatePorts = new[] { 62078, 8443, 8080, 5900, 32768, 49152 };
            var (listener, testPort) = BindAvailablePortFromList(candidatePorts);

            try
            {
                // Act
                var result = await PortScanner.ScanPortsAsync("127.0.0.1", fastScan: true, timeoutMs: 2000);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.ContainsKey(testPort), $"Expected port {testPort} to be detected as open.");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ScanPortsAsync_ShouldCancel_WhenCancellationRequested()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            var result = await PortScanner.ScanPortsAsync("127.0.0.1", fastScan: true, timeoutMs: 2000, token: cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScanPortsAsync_ShouldDetectOpenPort()
        {
            // Arrange
            int[] candidatePorts = new[] { 5432, 5900, 3306 };
            var (listener, testPort) = BindAvailablePortFromList(candidatePorts);

            try
            {
                // Act
                var result = await PortScanner.ScanPortsAsync("127.0.0.1", fastScan: true, timeoutMs: 2000);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.ContainsKey(testPort), $"Expected port {testPort} to be detected as open.");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ScanPortsAsync_ShouldReturnEmpty_WhenNoPortsOpen()
        {
            // Arrange
            // Using TEST-NET-1 IP address to simulate timeout without hitting open ports on loopback
            string unreachableIp = "192.0.2.1";

            // Act
            var result = await PortScanner.ScanPortsAsync(unreachableIp, fastScan: true, timeoutMs: 50);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScanRangeAsync_ShouldDetectOpenPort()
        {
            // Arrange - use dynamic ephemeral port
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int testPort = ((IPEndPoint)listener.LocalEndpoint).Port;

            try
            {
                // Act
                var result = await PortScanner.ScanRangeAsync("127.0.0.1", testPort, testPort, timeoutMs: 2000);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.ContainsKey(testPort), $"Expected port {testPort} to be detected as open.");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ScanCommonPortsAsync_ShouldDetectOpenPort()
        {
            // Arrange
            int[] candidatePorts = new[] { 8080, 8443, 62078 };
            var (listener, testPort) = BindAvailablePortFromList(candidatePorts);

            try
            {
                // Act
                var result = await PortScanner.ScanCommonPortsAsync("127.0.0.1");

                // Assert
                Assert.NotNull(result);
                // ScanCommonPortsAsync returns List<string> like "8080 (HTTP Alt)"
                Assert.Contains(result, r => r.StartsWith($"{testPort} ("));
            }
            finally
            {
                listener.Stop();
            }
        }

        [Theory]
        [InlineData(new int[] { 62078 }, "iOS / iPadOS")]
        [InlineData(new int[] { 548, 5353, 22 }, "macOS")]
        [InlineData(new int[] { 548, 5353 }, "macOS")]
        [InlineData(new int[] { 3389, 445, 135 }, "Windows")]
        [InlineData(new int[] { 3389, 445 }, "Windows")]
        [InlineData(new int[] { 3389 }, "Windows (RDP)")]
        [InlineData(new int[] { 445, 135 }, "Windows")]
        [InlineData(new int[] { 22 }, "Linux / Unix")]
        [InlineData(new int[] { 22, 80 }, "Linux / Unix")]
        [InlineData(new int[] { 80, 443 }, "Network Device")]
        [InlineData(new int[] { 80, 443, 8080, 8443 }, "Network Device")]
        [InlineData(new int[] { 80, 443, 8080, 8443, 22 }, "Linux / Unix")]
        [InlineData(new int[] { }, "")]
        [InlineData(new int[] { 80 }, "")]
        public void GuessOs_ShouldReturnExpectedOs(int[] ports, string expectedOs)
        {
            // Arrange
            var openPorts = new System.Collections.Generic.List<int>(ports);

            // Act
            var result = PortScanner.GuessOs(openPorts);

            // Assert
            Assert.Equal(expectedOs, result);
        }

        [Theory]
        [InlineData(80, "HTTP")]
        [InlineData(443, "HTTPS")]
        [InlineData(22, "SSH")]
        [InlineData(3389, "RDP")]
        public void GetServiceName_ShouldReturnKnownServiceName(int port, string expectedName)
        {
            // Act
            string result = PortScanner.GetServiceName(port);

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(9999)]
        [InlineData(65535)]
        public void GetServiceName_ShouldReturnPortNumber_ForUnknownPort(int port)
        {
            // Act
            string result = PortScanner.GetServiceName(port);

            // Assert
            Assert.Equal($"Port {port}", result);
        }
    }
}
