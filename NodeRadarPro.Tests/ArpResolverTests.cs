using System.Diagnostics;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class ArpResolverTests
{
    [Fact]
    public void ResolveMacAddress_InvalidIp_ReturnsUnknown()
    {
        // Arrange
        string invalidIp = "invalid-ip";

        // Act
        string result = ArpResolver.ResolveMacAddress(invalidIp);

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void ResolveMacAddress_NullOrEmptyIp_ReturnsUnknown()
    {
        // Act & Assert
        Assert.Equal("Unknown", ArpResolver.ResolveMacAddress(null!));
        Assert.Equal("Unknown", ArpResolver.ResolveMacAddress(string.Empty));
    }

    [Fact]
    public void TryResolveNetBiosName_InvalidIp_ReturnsEmptyString()
    {
        // Arrange
        string invalidIp = "invalid-ip";

        // Act
        string result = ArpResolver.TryResolveNetBiosName(invalidIp);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TryResolveNetBiosName_NonResponsiveIp_TimesOutAndReturnsEmptyString()
    {
        // Arrange
        string nonResponsiveIp = "192.0.2.1"; // TEST-NET-1 IP address
        var stopwatch = Stopwatch.StartNew();

        // Act
        string result = ArpResolver.TryResolveNetBiosName(nonResponsiveIp);
        stopwatch.Stop();

        // Assert
        Assert.Equal(string.Empty, result);
        // Ensure it timed out approximately after 1500ms (1.5 seconds)
        // Give some buffer for execution time
        Assert.True(stopwatch.ElapsedMilliseconds >= 1000, "Expected a timeout to take at least 1000ms");
    }

    [Fact]
    public void GetFullArpTable_ReturnsListWithoutExceptions()
    {
        // Act
        var result = ArpResolver.GetFullArpTable();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ResolveMacAddress_SendArpException_ReturnsUnknown()
    {
        // Arrange
        string validIp = "192.168.1.1";
        static int failingSendArp(int destIp, int srcIp, byte[] pMacAddr, ref uint phyAddrLen)
        {
            throw new Exception("Simulated SendARP failure");
        }

        // Act
        string result = ArpResolver.ResolveMacAddress(validIp, "", failingSendArp);

        // Assert
        Assert.Equal("Unknown", result);
    }
}
