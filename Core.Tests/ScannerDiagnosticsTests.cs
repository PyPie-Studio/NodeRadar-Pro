using NodeRadarPro.Core;

namespace Core.Tests;

public class ScannerDiagnosticsTests
{
    [Fact]
    public async Task RunDiagnosticsAsync_ExecutesAndReturnsIntResult()
    {
        TextWriter originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        try
        {
            Console.SetOut(stringWriter);
            int result = await ScannerDiagnostics.RunDiagnosticsAsync();
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
            int result = await ScannerDiagnostics.RunDiagnosticsAsync(getNetworkInterfaces: () => throw new InvalidOperationException("Simulated network interface failure"));
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
