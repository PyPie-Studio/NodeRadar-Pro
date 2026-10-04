using Moq;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Tests.Fingerprinting;

[Collection("ProbeSweepStaticState")]
public class DeepFingerprintEngineTests
{
    [Fact]
    public async Task FingerprintNodeAsync_AggregatesResultsFromProbes()
    {
        // Arrange
        var mockProbe1 = new Mock<IFingerprintProbe>();
        mockProbe1.SetupGet(p => p.Name).Returns("MockProbe1");
        mockProbe1.SetupGet(p => p.Priority).Returns(10);
        mockProbe1.Setup(p => p.ProbeAsync(It.IsAny<NetworkNode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProbeResult
            {
                Source = "MAC OUI Lookup",
                RawData = new Dictionary<string, string> { { "Vendor", "Apple, Inc." } }
            });

        var mockProbe2 = new Mock<IFingerprintProbe>();
        mockProbe2.SetupGet(p => p.Name).Returns("MockProbe2");
        mockProbe2.SetupGet(p => p.Priority).Returns(20);
        mockProbe2.Setup(p => p.ProbeAsync(It.IsAny<NetworkNode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProbeResult
            {
                Source = "mDNS",
                RawData = new Dictionary<string, string> { { "Model", "MacBookPro18,1" } }
            });

        var engine = new DeepFingerprintEngine(new[] { mockProbe1.Object, mockProbe2.Object });
        var node = new NetworkNode { IpAddress = "192.168.1.50", MacAddress = "00:11:22:33:44:55" };

        // Act
        var result = await engine.FingerprintNodeAsync(node, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeviceTypeCategory.Desktop, result.Type);
        Assert.Equal("Apple Computer", result.TypeString);
        Assert.Equal("macOS/iOS", result.Os);
        Assert.True(result.ConfidenceScore > 0);
    }

    [Fact]
    public async Task FingerprintNodeAsync_FailingProbe_ContinuesExecution()
    {
        // Arrange
        var faultyProbe = new Mock<IFingerprintProbe>();
        faultyProbe.SetupGet(p => p.Name).Returns("FaultyProbe");
        faultyProbe.SetupGet(p => p.Priority).Returns(5);
        faultyProbe.Setup(p => p.ProbeAsync(It.IsAny<NetworkNode>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Probe failed"));

        var workingProbe = new Mock<IFingerprintProbe>();
        workingProbe.SetupGet(p => p.Name).Returns("WorkingProbe");
        workingProbe.SetupGet(p => p.Priority).Returns(10);
        workingProbe.Setup(p => p.ProbeAsync(It.IsAny<NetworkNode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProbeResult
            {
                Source = "MAC OUI Lookup",
                RawData = new Dictionary<string, string> { { "Vendor", "Synology" } }
            });

        var engine = new DeepFingerprintEngine(new[] { faultyProbe.Object, workingProbe.Object });
        var node = new NetworkNode { IpAddress = "192.168.1.10" };

        // Act
        var result = await engine.FingerprintNodeAsync(node, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeviceTypeCategory.NAS, result.Type);
    }

    [Fact]
    public async Task StartDiscoverySweepAsync_WithCanceledToken_CompletesPromptlyWithoutException()
    {
        // Arrange
        var engine = DeepFingerprintEngine.Instance;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => engine.StartDiscoverySweepAsync(cts.Token));
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartDiscoverySweepAsync_ClearsProbeCaches()
    {
        // Arrange
        MdnsProbe.InjectCacheForTesting("192.168.1.100", new MdnsProbe.MdnsData { InstanceName = "PreCachedDevice" });
        SsdpProbe.InjectCacheForTesting("192.168.1.101", new SsdpProbe.SsdpData { FriendlyName = "PreCachedTV" });

        var engine = new DeepFingerprintEngine();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        await engine.StartDiscoverySweepAsync(cts.Token);

        // Assert
        var mdnsProbe = new MdnsProbe();
        var mdnsResult = await mdnsProbe.ProbeAsync(new NetworkNode { IpAddress = "192.168.1.100" }, CancellationToken.None);
        Assert.Empty(mdnsResult.RawData);

        var ssdpProbe = new SsdpProbe();
        var ssdpResult = await ssdpProbe.ProbeAsync(new NetworkNode { IpAddress = "192.168.1.101" }, CancellationToken.None);
        Assert.Empty(ssdpResult.RawData);
    }

    [Fact]
    public async Task StartDiscoverySweepAsync_WithShortTimeout_ExecutesAndCompletesCleanly()
    {
        // Arrange
        var engine = new DeepFingerprintEngine();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => engine.StartDiscoverySweepAsync(cts.Token));
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartDiscoverySweepAsync_MultipleConcurrentCalls_ExecutesWithoutThrowing()
    {
        // Arrange
        var engine = DeepFingerprintEngine.Instance;
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        cts1.CancelAfter(50);
        cts2.CancelAfter(50);

        // Act & Assert
        var task1 = engine.StartDiscoverySweepAsync(cts1.Token);
        var task2 = engine.StartDiscoverySweepAsync(cts2.Token);

        var exception = await Record.ExceptionAsync(() => Task.WhenAll(task1, task2));
        Assert.Null(exception);
    }
}
