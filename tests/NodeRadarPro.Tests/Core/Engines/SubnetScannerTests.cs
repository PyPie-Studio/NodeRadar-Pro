using System.Net.NetworkInformation;
using Moq;

namespace NodeRadarPro.Tests.Core.Engines;

[Collection("ProbeSweepStaticState")]
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

    [Theory]
    [InlineData("192.168.1.50", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.16.5.100", true)]
    [InlineData("192.168.2.1", false)]
    [InlineData("192.168.1", false)]
    [InlineData("192.168.1.2.3", false)]
    [InlineData("192.168.1.999", false)]
    [InlineData("invalid_ip", false)]
    public void IsIpInAnySubnet_ListOverload_ValidatesCorrectly(string ip, bool expected)
    {
        var subnets = new List<string> { "10.0.0", "172.16.5", "192.168.1", "10.1.1", "10.2.2", "10.3.3" };
        bool actual = SubnetScanner.IsIpInAnySubnet(ip.AsSpan(), subnets);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("192.168.1.50", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("192.168.2.1", false)]
    public void IsIpInAnySubnet_HashSetOverload_ValidatesCorrectly(string ip, bool expected)
    {
        var subnets = new HashSet<string>(StringComparer.Ordinal) { "10.0.0", "172.16.5", "192.168.1" };
        bool actual = SubnetScanner.IsIpInAnySubnet(ip.AsSpan(), subnets);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("192.168.1.50", true)]
    [InlineData("192.168.2.1", false)]
    public void IsIpInAnySubnet_IEnumerableOverload_ValidatesCorrectly(string ip, bool expected)
    {
        IEnumerable<string> subnets = new string[] { "10.0.0", "172.16.5", "192.168.1" };
        bool actual = SubnetScanner.IsIpInAnySubnet(ip.AsSpan(), subnets);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsIpInAnySubnet_EmptyOrNullSubnets_ReturnsFalse()
    {
        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), new List<string>()));
        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), new HashSet<string>()));
        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), (List<string>)null!));
        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), (HashSet<string>)null!));
        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), (IEnumerable<string>)null!));
    }

    [Fact]
    public async Task ScanSubnetAsync_WithMockDependencies_ExecutesPreSweepAndDiscoversNodes()
    {
        // Arrange
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut, RoundtripTime = 0 });
        mockPing.Setup(p => p.SendPingAsync("192.168.1.100", It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.Success, RoundtripTime = 8 });

        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.ResolveMacAddress(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ArpResolver.UnknownMac);
        mockArp.Setup(a => a.ResolveMacAddress("192.168.1.100", It.IsAny<string>()))
            .Returns("00:11:22:33:44:AA");
        mockArp.Setup(a => a.GetFullArpTable())
            .Returns(new List<(string Ip, string Mac)>
            {
                ("192.168.1.50", "00:11:22:33:44:50")
            });

        var mockTcp = new Mock<ITcpPortProber>();
        mockTcp.Setup(t => t.ProbePortAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var scanner = new SubnetScanner(mockPing.Object, mockArp.Object, mockTcp.Object)
        {
            TimeoutMs = 1,
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false,
            ScanAllSubnets = false
        };

        var progressUpdates = new List<double>();
        scanner.ProgressUpdated += p => progressUpdates.Add(p);

        var discoveredNodes = new List<NetworkNode>();
        scanner.NodeDiscovered += n => discoveredNodes.Add(n);

        // Act
        var results = await scanner.ScanSubnetAsync("192.168.1", CancellationToken.None);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, n => n.IpAddress == "192.168.1.50" && n.MacAddress == "00:11:22:33:44:50");
        Assert.Contains(results, n => n.IpAddress == "192.168.1.100" && n.MacAddress == "00:11:22:33:44:AA");
        Assert.NotEmpty(progressUpdates);
        Assert.Equal(100.0, progressUpdates[^1]);
    }

    [Fact]
    public async Task ScanRangeAsync_SyntheticMacAssignment_WhenArpReturnsUnknown()
    {
        // Arrange
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync("192.168.1.75", It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.Success, RoundtripTime = 15 });

        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.ResolveMacAddress(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ArpResolver.UnknownMac);
        string unknown = ArpResolver.UnknownMac;
        mockArp.Setup(a => a.TryGetMacFromArpTable("192.168.1.75", out unknown))
            .Returns(false);

        var mockTcp = new Mock<ITcpPortProber>();
        mockTcp.Setup(t => t.ProbePortAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var scanner = new SubnetScanner(mockPing.Object, mockArp.Object, mockTcp.Object)
        {
            TimeoutMs = 1,
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false,
            ScanAllSubnets = false
        };

        // Act
        var results = await scanner.ScanRangeAsync("192.168.1", 75, 75, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("192.168.1.75", results[0].IpAddress);
        Assert.Equal("L3-ROUTED-192.168.1.75", results[0].MacAddress);
    }

    [Fact]
    public async Task ScanRangeAsync_StealthPortProbeFallback_DetectsNodeWhenPingTimesOut()
    {
        // Arrange
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync("192.168.1.50", It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut, RoundtripTime = 0 });

        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.ResolveMacAddress(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ArpResolver.UnknownMac);
        mockArp.Setup(a => a.GetFullArpTable()).Returns(new List<(string, string)>());

        var mockTcp = new Mock<ITcpPortProber>();
        // Mock TCP port 445 open, all other ports closed
        mockTcp.Setup(t => t.ProbePortAsync("192.168.1.50", 445, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockTcp.Setup(t => t.ProbePortAsync("192.168.1.50", It.IsNotIn(445), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var scanner = new SubnetScanner(mockPing.Object, mockArp.Object, mockTcp.Object)
        {
            TimeoutMs = 1,
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false,
            ScanAllSubnets = false
        };

        // Act
        var results = await scanner.ScanRangeAsync("192.168.1", 50, 50, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("192.168.1.50", results[0].IpAddress);
        Assert.True(results[0].IsOnline);
    }

    [Fact]
    public async Task QuickProbeAsync_Cascade_TestsArpPingTcpPhases()
    {
        // Tier 1: ARP success returns immediately
        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.ResolveMacAddress("192.168.1.1", It.IsAny<string>())).Returns("00:11:22:33:44:01");
        var resultArp = await SubnetScanner.QuickProbeAsync("192.168.1.1", arpResolver: mockArp.Object);
        Assert.True(resultArp.IsOnline);
        Assert.Equal("00:11:22:33:44:01", resultArp.Mac);

        // Tier 2: ARP fails but ICMP Ping succeeds
        var mockArpFail = new Mock<IArpResolver>();
        mockArpFail.Setup(a => a.ResolveMacAddress(It.IsAny<string>(), It.IsAny<string>())).Returns(ArpResolver.UnknownMac);
        var mockPingSuccess = new Mock<IPingProvider>();
        mockPingSuccess.Setup(p => p.SendPingAsync("192.168.1.2", It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.Success, RoundtripTime = 12 });
        var resultPing = await SubnetScanner.QuickProbeAsync("192.168.1.2", pingProvider: mockPingSuccess.Object, arpResolver: mockArpFail.Object);
        Assert.True(resultPing.IsOnline);
        Assert.Equal(12, resultPing.LatencyMs);

        // Tier 3: ARP and Ping fail, but TCP port probe succeeds
        var mockPingFail = new Mock<IPingProvider>();
        mockPingFail.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut, RoundtripTime = 0 });
        var mockTcpSuccess = new Mock<ITcpPortProber>();
        mockTcpSuccess.Setup(t => t.ProbePortAsync("192.168.1.3", 80, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var resultTcp = await SubnetScanner.QuickProbeAsync("192.168.1.3", pingProvider: mockPingFail.Object, arpResolver: mockArpFail.Object, tcpPortProber: mockTcpSuccess.Object);
        Assert.True(resultTcp.IsOnline);

        // Tier 4: All probes fail -> host offline
        var mockTcpFail = new Mock<ITcpPortProber>();
        mockTcpFail.Setup(t => t.ProbePortAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var resultOffline = await SubnetScanner.QuickProbeAsync("192.168.1.4", pingProvider: mockPingFail.Object, arpResolver: mockArpFail.Object, tcpPortProber: mockTcpFail.Object);
        Assert.False(resultOffline.IsOnline);
    }

    [Fact]
    public async Task ScanRangeAsync_WhenHostInArpTable_ShortCircuitsPingAndSendArp()
    {
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.Success, RoundtripTime = 5 });

        var mockArp = new Mock<IArpResolver>();
        string outMac = "00:AA:BB:CC:DD:EE";
        mockArp.Setup(a => a.TryGetMacFromArpTable("192.168.1.10", out outMac))
            .Returns(true);
        mockArp.Setup(a => a.GetFullArpTable())
            .Returns(new List<(string Ip, string Mac)>());

        var scanner = new SubnetScanner(mockPing.Object, mockArp.Object)
        {
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false
        };

        var results = await scanner.ScanRangeAsync("192.168.1", 10, 10, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("192.168.1.10", results[0].IpAddress);
        Assert.Equal("00:AA:BB:CC:DD:EE", results[0].MacAddress);
        mockPing.Verify(p => p.SendPingAsync("192.168.1.10", It.IsAny<int>()), Times.Never);
        mockArp.Verify(a => a.ResolveMacAddress("192.168.1.10", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ScanRangeAsync_WhenPingAndStealthProbesFail_NeverCallsSendArp()
    {
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut });

        var mockArp = new Mock<IArpResolver>();
        string outMac = "";
        mockArp.Setup(a => a.TryGetMacFromArpTable(It.IsAny<string>(), out outMac))
            .Returns(false);
        mockArp.Setup(a => a.GetFullArpTable())
            .Returns(new List<(string Ip, string Mac)>());

        var mockTcp = new Mock<ITcpPortProber>();
        mockTcp.Setup(t => t.ProbePortAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var scanner = new SubnetScanner(mockPing.Object, mockArp.Object, mockTcp.Object)
        {
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false
        };

        var results = await scanner.ScanRangeAsync("192.168.1", 99, 99, CancellationToken.None);

        Assert.Empty(results);
        mockArp.Verify(a => a.ResolveMacAddress("192.168.1.99", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ScanRangeAsync_WhenPingFails_StealthProbeSucceeds_ResolvesMacAddressViaSendArp()
    {
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut });

        var mockArp = new Mock<IArpResolver>();
        string outMac = "";
        mockArp.Setup(a => a.TryGetMacFromArpTable(It.IsAny<string>(), out outMac))
            .Returns(false);
        mockArp.Setup(a => a.ResolveMacAddress("192.168.1.55", It.IsAny<string>()))
            .Returns("11:22:33:44:55:66");
        mockArp.Setup(a => a.GetFullArpTable())
            .Returns(new List<(string Ip, string Mac)>());

        var mockTcp = new Mock<ITcpPortProber>();
        mockTcp.Setup(t => t.ProbePortAsync("192.168.1.55", 445, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var scanner = new SubnetScanner(mockPing.Object, mockArp.Object, mockTcp.Object)
        {
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false
        };

        var results = await scanner.ScanRangeAsync("192.168.1", 55, 55, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("192.168.1.55", results[0].IpAddress);
        Assert.Equal("11:22:33:44:55:66", results[0].MacAddress);
        mockArp.Verify(a => a.ResolveMacAddress("192.168.1.55", It.IsAny<string>()), Times.Once);
    }
}
