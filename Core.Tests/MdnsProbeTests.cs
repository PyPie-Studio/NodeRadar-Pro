using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core.Fingerprinting.Probes;
using Xunit;

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
