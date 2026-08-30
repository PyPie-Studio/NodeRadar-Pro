using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Fingerprinting;
using Xunit;

namespace Core.Tests.Fingerprinting;

public class DeepFingerprintEngineTests
{
    [Fact]
    public void Instance_ReturnsSameSingletonInstance()
    {
        var instance1 = DeepFingerprintEngine.Instance;
        var instance2 = DeepFingerprintEngine.Instance;

        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public async Task StartDiscoverySweepAsync_ExecutesWithoutThrowing()
    {
        var exception = await Record.ExceptionAsync(() => DeepFingerprintEngine.Instance.StartDiscoverySweepAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task FingerprintNodeAsync_ValidNode_ReturnsFingerprintResult()
    {
        var node = new NetworkNode
        {
            IpAddress = "192.168.1.50",
            MacAddress = "00:11:22:33:44:55",
            Hostname = "TestNode"
        };

        using var cts = new CancellationTokenSource(5000);
        var result = await DeepFingerprintEngine.Instance.FingerprintNodeAsync(node, cts.Token);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FingerprintNodeAsync_CancelledToken_HandlesCancellationGracefully()
    {
        var node = new NetworkNode
        {
            IpAddress = "192.168.1.51",
            MacAddress = "00:11:22:33:44:56"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await DeepFingerprintEngine.Instance.FingerprintNodeAsync(node, cts.Token);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FingerprintNodeAsync_ConcurrentCalls_ExecutesSafely()
    {
        var node1 = new NetworkNode { IpAddress = "192.168.1.100", MacAddress = "00:11:22:33:44:01" };
        var node2 = new NetworkNode { IpAddress = "192.168.1.101", MacAddress = "00:11:22:33:44:02" };

        using var cts = new CancellationTokenSource(5000);

        var task1 = DeepFingerprintEngine.Instance.FingerprintNodeAsync(node1, cts.Token);
        var task2 = DeepFingerprintEngine.Instance.FingerprintNodeAsync(node2, cts.Token);

        var results = await Task.WhenAll(task1, task2);

        Assert.NotNull(results[0]);
        Assert.NotNull(results[1]);
    }
}
