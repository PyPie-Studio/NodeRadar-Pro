using System;
using System.Diagnostics;
using NodeRadarPro.Core;
using Xunit;

namespace NodeRadarPro.Tests;

public class SubnetScannerBenchmarkTests
{
    [Fact]
    public void Benchmark_IpParsing_Comparison()
    {
        var arpTable = new (string Ip, string Mac)[1000];
        for (int i = 0; i < 1000; i++)
        {
            arpTable[i] = ($"192.168.1.{i % 256}", $"00:11:22:33:44:{i % 100:D2}");
        }

        string baseIp = "192.168.1";
        int startIp = 1;
        int endIp = 254;

        // Warmup
        long baselineMatches = RunBaseline(arpTable, baseIp, startIp, endIp);
        long helperMatches = RunWithHelper(arpTable, baseIp, startIp, endIp);
        Assert.Equal(baselineMatches, helperMatches);

        long baselineAllocations = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        for (int r = 0; r < 1000; r++)
        {
            RunBaseline(arpTable, baseIp, startIp, endIp);
        }
        sw.Stop();
        long baselineTimeMs = sw.ElapsedMilliseconds;
        baselineAllocations = GC.GetAllocatedBytesForCurrentThread() - baselineAllocations;

        long helperAllocations = GC.GetAllocatedBytesForCurrentThread();
        sw.Restart();
        for (int r = 0; r < 1000; r++)
        {
            RunWithHelper(arpTable, baseIp, startIp, endIp);
        }
        sw.Stop();
        long helperTimeMs = sw.ElapsedMilliseconds;
        helperAllocations = GC.GetAllocatedBytesForCurrentThread() - helperAllocations;

        Console.WriteLine($"Baseline Time: {baselineTimeMs} ms, Allocations: {baselineAllocations} bytes");
        Console.WriteLine($"Helper Time: {helperTimeMs} ms, Allocations: {helperAllocations} bytes");

        Assert.True(helperAllocations < baselineAllocations, $"Expected helper allocations ({helperAllocations}) to be less than baseline allocations ({baselineAllocations})");
    }

    [Theory]
    [InlineData("192.168.1.10", "192.168.1", 1, 254, true)]
    [InlineData("192.168.1.255", "192.168.1", 1, 254, false)]
    [InlineData("192.168.2.10", "192.168.1", 1, 254, false)]
    [InlineData("invalid.ip.str", "192.168.1", 1, 254, false)]
    public void IsIpInSubnetAndRange_ValidatesCorrectly(string ip, string baseIp, int start, int end, bool expected)
    {
        bool result = SubnetScanner.IsIpInSubnetAndRange(ip.AsSpan(), baseIp, start, end);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("192.168.1.10", "192.168.1", true)]
    [InlineData("192.168.2.10", "192.168.1", false)]
    [InlineData("127.0.0.1", "192.168.1", false)]
    public void IsIpInSubnet_ValidatesCorrectly(string ip, string baseIp, bool expected)
    {
        bool result = SubnetScanner.IsIpInSubnet(ip.AsSpan(), baseIp);
        Assert.Equal(expected, result);
    }

    private static long RunBaseline((string Ip, string Mac)[] arpTable, string baseIp, int startIp, int endIp)
    {
        long matchCount = 0;
        foreach (var (ip, mac) in arpTable)
        {
            string[] parts = ip.Split('.');
            if (parts.Length != 4) continue;
            string ipSubnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
            if (ipSubnet != baseIp) continue;
            if (!int.TryParse(parts[3], out int lastOctet) || lastOctet < startIp || lastOctet > endIp) continue;
            matchCount++;
        }
        return matchCount;
    }

    private static long RunWithHelper((string Ip, string Mac)[] arpTable, string baseIp, int startIp, int endIp)
    {
        long matchCount = 0;
        foreach (var (ip, mac) in arpTable)
        {
            if (SubnetScanner.IsIpInSubnetAndRange(ip.AsSpan(), baseIp, startIp, endIp))
            {
                matchCount++;
            }
        }
        return matchCount;
    }
}
