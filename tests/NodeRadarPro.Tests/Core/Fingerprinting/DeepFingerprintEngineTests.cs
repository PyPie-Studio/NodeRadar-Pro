using Moq;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting;

namespace NodeRadarPro.Tests.Fingerprinting;

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
}
