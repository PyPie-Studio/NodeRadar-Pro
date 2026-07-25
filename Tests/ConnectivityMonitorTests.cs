using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using NodeRadarPro.Core;

namespace Tests;

public class ConnectivityMonitorTests
{
    [Fact]
    public async Task TryTcpProbeAsync_UnreachableHost_ReturnsFalse()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();

        var methodInfo = typeof(ConnectivityMonitor).GetMethod("TryTcpProbeAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Act
        // Using TEST-NET-1 unroutable IP to simulate timeout / unreachable host.
        // The method will timeout after 300ms for each port.
        var task = (Task<bool>)methodInfo.Invoke(monitor, new object[] { "192.0.2.1" })!;
        bool result = await task;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryTcpProbeAsync_InvalidHost_ReturnsFalse()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();

        var methodInfo = typeof(ConnectivityMonitor).GetMethod("TryTcpProbeAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Act
        // Simulates an immediate exception like DNS failure or invalid IP.
        var task = (Task<bool>)methodInfo.Invoke(monitor, new object[] { "invalid_host_name_for_testing" })!;
        bool result = await task;

        // Assert
        Assert.False(result);
    }
}
