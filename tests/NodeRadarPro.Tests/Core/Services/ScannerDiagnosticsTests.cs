using System.Net.NetworkInformation;
using Moq;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class ScannerDiagnosticsTests
{
    private static SubnetScanner CreateFastTestScanner()
    {
        var mockPing = new Mock<IPingProvider>();
        mockPing.Setup(p => p.SendPingAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PingReplyWrapper { Status = IPStatus.TimedOut, RoundtripTime = 0 });
        var mockArp = new Mock<IArpResolver>();
        mockArp.Setup(a => a.GetFullArpTable()).Returns(new List<(string, string)>());
        var mockTcp = new Mock<ITcpPortProber>();
        mockTcp.Setup(t => t.ProbePortAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return new SubnetScanner(mockPing.Object, mockArp.Object, mockTcp.Object)
        {
            TimeoutMs = 1,
            EnableDnsResolve = false,
            EnableInlinePortScan = false,
            EnableOsDetection = false,
            ScanAllSubnets = false
        };
    }

    [Fact]
    public async Task RunDiagnosticsAsync_ExecutesAndReturnsIntResult()
    {
        TextWriter originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        try
        {
            Console.SetOut(stringWriter);
            int result = await ScannerDiagnostics.RunDiagnosticsAsync(scannerOverride: CreateFastTestScanner());
            Assert.True(result == 0 || result == 1);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task RunDiagnosticsAsync_WhenNetworkInterfaceThrowsException_HandlesExceptionAndReturnsFailureCode()
    {
        TextWriter originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        try
        {
            Console.SetOut(stringWriter);
            int result = await ScannerDiagnostics.RunDiagnosticsAsync(
                getNetworkInterfaces: () => throw new InvalidOperationException("Simulated network interface failure"),
                scannerOverride: CreateFastTestScanner());
            Assert.Equal(1, result);
            string output = stringWriter.ToString();
            Assert.Contains("Test 1: Network Interfaces Detection... FAIL (Exception: Simulated network interface failure)", output);
            Assert.Contains(">>> ERROR: ONE OR MORE DIAGNOSTIC TESTS FAILED. <<<", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
