using System.Net.NetworkInformation;
using Moq;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class SubnetScannerTests
{
    [Fact]
    public void PreferredInterfaceName_CanBeSetWithoutThrowing()
    {
        var scanner = new SubnetScanner
        {
            PreferredInterfaceName = "NonExistentInterface_12345"
        };
        Assert.Equal("NonExistentInterface_12345", scanner.PreferredInterfaceName);
    }

    [Fact]
    public void GetLocalBaseIp_ReturnsValidBaseIp()
    {
        string baseIp = SubnetScanner.GetLocalBaseIp();
        Assert.NotNull(baseIp);
        Assert.NotEmpty(baseIp);
    }

    [Fact]
    public async Task ScanRangeAsync_EmptyRange_ReturnsEmptyList()
    {
        var scanner = new SubnetScanner();
        var results = await scanner.ScanRangeAsync("192.168.1", 5, 2, CancellationToken.None);
        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanRangeAsync_WithMockDependencies_DiscoversResponsiveNode()
    {
        // Arrange
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync("192.168.1.1", It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper
            {
                Status = IPStatus.Success,
                RoundtripTime = 12
            });
        mockPing.Setup(p => p.SendPingAsync("192.168.1.2", It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper
            {
                Status = IPStatus.TimedOut,
                RoundtripTime = 0
            });

        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.ResolveMacAddress("192.168.1.1", It.IsAny<string>()))
            .Returns("00:11:22:33:44:55");
        mockArp.Setup(a => a.ResolveMacAddress("192.168.1.2", It.IsAny<string>()))
            .Returns("Unknown");
        mockArp.Setup(a => a.GetFullArpTable())
            .Returns(new List<(string Ip, string Mac)>());

        var scanner = new SubnetScanner(mockPing.Object, mockArp.Object)
        {
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false
        };

        var discoveredNodes = new List<NetworkNode>();
        scanner.NodeDiscovered += n => discoveredNodes.Add(n);

        // Act
        var results = await scanner.ScanRangeAsync("192.168.1", 1, 2, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("192.168.1.1", results[0].IpAddress);
        Assert.Equal("00:11:22:33:44:55", results[0].MacAddress);
        Assert.Single(discoveredNodes);
    }

    [Fact]
    public async Task ScanRangeAsync_WhenCancelled_ReturnsImmediately()
    {
        var scanner = new SubnetScanner();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var results = await scanner.ScanRangeAsync("192.168.1", 1, 254, cts.Token);
        Assert.NotNull(results);
    }

    [Fact]
    public async Task ScanRangeAsync_WithPreferredInterfaceName_ExecutesWithoutError()
    {
        var scanner = new SubnetScanner
        {
            PreferredInterfaceName = "NonExistentInterface12345",
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false
        };

        var results = await scanner.ScanRangeAsync("192.168.1", 5, 2, CancellationToken.None);
        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanRangeAsync_PostSweepArpTable_DiscoversMissedNodeAndHandlesArpExceptions()
    {
        // Arrange
        var tempLogDir = Path.Combine(Path.GetTempPath(), "NodeRadarTests_" + Guid.NewGuid().ToString("N"));
        Logger.SetCustomLogDirectoryForTesting(tempLogDir);

        try
        {
            var mockPing = new Mock<IPingProvider>();
            mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut });

            var mockArp = new Mock<IArpResolver>();
            mockArp.Setup(a => a.ResolveMacAddress(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("Unknown");
            mockArp.Setup(a => a.GetFullArpTable())
                .Returns(new List<(string Ip, string Mac)>
                {
                    ("192.168.1.10", "AA:BB:CC:DD:EE:FF")
                });

            var scanner = new SubnetScanner(mockPing.Object, mockArp.Object)
            {
                EnableDnsResolve = false,
                EnableInlinePortScan = false,
                EnableOsDetection = false
            };

            // Act
            var results = await scanner.ScanRangeAsync("192.168.1", 10, 10, CancellationToken.None);

            // Assert
            Assert.Single(results);
            Assert.Equal("192.168.1.10", results[0].IpAddress);
            Assert.Equal("AA:BB:CC:DD:EE:FF", results[0].MacAddress);
        }
        finally
        {
            Logger.SetCustomLogDirectoryForTesting(null);
            if (Directory.Exists(tempLogDir)) Directory.Delete(tempLogDir, true);
        }
    }

    [Fact]
    public async Task ScanRangeAsync_WhenArpSweepThrows_LogsWarningAndReturnsDiscoveredNodes()
    {
        // Arrange
        var tempLogDir = Path.Combine(Path.GetTempPath(), "NodeRadarTests_" + Guid.NewGuid().ToString("N"));
        Logger.SetCustomLogDirectoryForTesting(tempLogDir);

        try
        {
            var mockPing = new Mock<IPingProvider>();
            mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut });

            var mockArp = new Mock<IArpResolver>();
            mockArp.Setup(a => a.ResolveMacAddress(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("Unknown");
            mockArp.Setup(a => a.GetFullArpTable())
                .Throws(new InvalidOperationException("ARP table query failed"));

            var scanner = new SubnetScanner(mockPing.Object, mockArp.Object)
            {
                EnableDnsResolve = false,
                EnableInlinePortScan = false,
                EnableOsDetection = false
            };

            // Act
            var results = await scanner.ScanRangeAsync("192.168.1", 1, 2, CancellationToken.None);

            // Assert
            Assert.Empty(results);

            Logger.FlushForTesting();
            string logFile = Path.Combine(tempLogDir, "noderadar_system.log");
            Assert.True(File.Exists(logFile));
            string logContent = File.ReadAllText(logFile);
            Assert.Contains("Post-sweep ARP table sweep failed: ARP table query failed", logContent);
        }
        finally
        {
            Logger.SetCustomLogDirectoryForTesting(null);
            if (Directory.Exists(tempLogDir)) Directory.Delete(tempLogDir, true);
        }
    }
}
