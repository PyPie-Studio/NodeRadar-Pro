using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Core.Tests;

public class MdnsProbeTests
{
    [Fact]
    public async Task ProbeAsync_ReturnsResultFromCache()
    {
        var probe = new MdnsProbe();
        var node = new NetworkNode { IpAddress = "192.168.1.100" };
        var ct = CancellationToken.None;

        var result = await probe.ProbeAsync(node, ct);

        Assert.NotNull(result);
        Assert.Equal("mDNS / Bonjour", result.Source);
    }
}
