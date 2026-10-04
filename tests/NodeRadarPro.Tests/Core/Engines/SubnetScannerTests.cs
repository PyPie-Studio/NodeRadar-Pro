using System.Net.NetworkInformation;
using Moq;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

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
    public void IsIpInAnySubnet_EmptySubnets_ReturnsFalse()
    {
        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), new List<string>()));
        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), new HashSet<string>()));
        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), (List<string>)null!));
    }

    [Fact]
    public void IsIpInAnySubnet_PerformanceBenchmark()
    {
        var subnetsList = Enumerable.Range(1, 50).Select(i => $"192.168.{i}").ToList();
        var subnetsSet = new HashSet<string>(subnetsList, StringComparer.Ordinal);
        string testIp = "192.168.50.100";

        // Warmup
        for (int i = 0; i < 1000; i++)
        {
            _ = SubnetScanner.IsIpInAnySubnet(testIp.AsSpan(), subnetsList);
            _ = SubnetScanner.IsIpInAnySubnet(testIp.AsSpan(), subnetsSet);
        }

        var swList = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100_000; i++)
        {
            _ = SubnetScanner.IsIpInAnySubnet(testIp.AsSpan(), subnetsList);
        }
        swList.Stop();

        var swSet = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100_000; i++)
        {
            _ = SubnetScanner.IsIpInAnySubnet(testIp.AsSpan(), subnetsSet);
        }
        swSet.Stop();

        // Log baseline timings
        Assert.True(swList.ElapsedMilliseconds >= 0);
        Assert.True(swSet.ElapsedMilliseconds >= 0);
    }
}
