using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests
{
    public class PortScannerTests
    {
        private static int GetAvailablePortFromList(int[] candidatePorts)
        {
            foreach (var port in candidatePorts)
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch
                {
                    // Port in use, try next
                }
            }
            throw new Exception("No available ports found in the candidate list.");
        }

        [Fact]
        public async Task ScanPortsAsync_ShouldDetectOpenPort_CommonScan()
        {
            // Arrange
            // 8080, 8443, 62078
            int[] candidatePorts = new[] { 62078, 8443, 8080, 5900 };
            int testPort = GetAvailablePortFromList(candidatePorts);

            var listener = new TcpListener(IPAddress.Loopback, testPort);
            listener.Start();

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
            int testPort = GetAvailablePortFromList(candidatePorts);

            var listener = new TcpListener(IPAddress.Loopback, testPort);
            listener.Start();

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
    }
}
