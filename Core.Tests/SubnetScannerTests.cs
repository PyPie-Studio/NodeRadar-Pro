using System.Collections.Concurrent;

namespace NodeRadarPro.Core.Tests;

public class SubnetScannerTests
{
    [Fact]
    public async Task ScanRangeAsync_CompletesSuccessfully()
    {
        var scanner = new SubnetScanner
        {
            TimeoutMs = 100,
            EnableDnsResolve = false,
            EnableOsDetection = false
        };

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var nodes = await scanner.ScanRangeAsync("192.0.2", 1, 2, cts.Token);

        Assert.NotNull(nodes);
    }

    [Fact]
    public async Task ScanRangeAsync_WithCancellation_ReturnsEarly()
    {
        var scanner = new SubnetScanner();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var nodes = await scanner.ScanRangeAsync("192.168.1", 1, 10, cts.Token);

        Assert.NotNull(nodes);
        Assert.Empty(nodes);
    }

    [Fact]
    public async Task ScanRangeAsync_FiresProgressEvent()
    {
        var scanner = new SubnetScanner
        {
            TimeoutMs = 10,
            EnableDnsResolve = false,
            EnableOsDetection = false
        };

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var progressEvents = new ConcurrentBag<double>();
        scanner.ProgressUpdated += p => progressEvents.Add(p);

        await scanner.ScanRangeAsync("192.0.2", 1, 10, cts.Token);

        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, p => p > 0);
        Assert.Contains(progressEvents, p => p == 100.0);
    }

    [Fact]
    public async Task ScanRangeAsync_EmptyRange_ReturnsEmpty()
    {
        var scanner = new SubnetScanner();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var nodes = await scanner.ScanRangeAsync("192.168.1", 10, 5, cts.Token);

        Assert.NotNull(nodes);
        Assert.Empty(nodes);
    }

    [Fact]
    public void GetAllLocalBaseIps_ReturnsNonEmptyList()
    {
        var subnets = SubnetScanner.GetAllLocalBaseIps();
        Assert.NotNull(subnets);
    }

    [Fact]
    public void GetLocalBaseIp_ReturnsValidIpPrefix()
    {
        var baseIp = SubnetScanner.GetLocalBaseIp();
        Assert.NotNull(baseIp);
        Assert.NotEmpty(baseIp);
        Assert.Equal(3, baseIp.Split('.').Length);
    }
}
