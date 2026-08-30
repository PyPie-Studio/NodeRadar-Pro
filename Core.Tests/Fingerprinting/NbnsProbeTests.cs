using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting.Probes;
using Xunit;

namespace Core.Tests.Fingerprinting;

public class NbnsProbeTests
{
    [Fact]
    public async Task ProbeAsync_ReturnsProbeResultWithCorrectSource()
    {
        var probe = new NbnsProbe();
        var node = new NetworkNode { IpAddress = "127.0.0.1" };
        using var cts = new CancellationTokenSource(100);

        var result = await probe.ProbeAsync(node, cts.Token);

        Assert.NotNull(result);
        Assert.Equal("NetBIOS (NBNS)", result.Source);
    }
}
