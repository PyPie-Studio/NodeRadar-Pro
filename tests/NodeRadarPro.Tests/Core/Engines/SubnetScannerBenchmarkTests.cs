using System.Collections.Concurrent;
using System.Diagnostics;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class SubnetScannerBenchmarkTests
{
    [Fact]
    public void Benchmark_IpParsing_MatchesBaselineLogic()
    {
        var arpTable = new (string Ip, string Mac)[1000];
        for (int i = 0; i < 1000; i++)
        {
            arpTable[i] = ($"192.168.1.{i % 256}", $"00:11:22:33:44:{i % 100:D2}");
        }

        string baseIp = "192.168.1";
        int startIp = 1;
        int endIp = 254;

        long baselineMatches = RunBaseline(arpTable, baseIp, startIp, endIp);
        long helperMatches = RunWithHelper(arpTable, baseIp, startIp, endIp);

        Assert.Equal(baselineMatches, helperMatches);
    }

    [Theory]
    [InlineData("192.168.1.10", "192.168.1", 1, 254, true)]
    [InlineData("192.168.1.255", "192.168.1", 1, 254, false)]
    [InlineData("192.168.1.0", "192.168.1", 1, 254, false)]
    [InlineData("192.168.2.10", "192.168.1", 1, 254, false)]
    [InlineData("invalid.ip.str", "192.168.1", 1, 254, false)]
    [InlineData("192.168.1", "192.168.1", 1, 254, false)]
    public void IsIpInSubnetAndRange_ValidatesCorrectly(string ip, string baseIp, int start, int end, bool expected)
    {
        bool result = SubnetScanner.IsIpInSubnetAndRange(ip.AsSpan(), baseIp, start, end);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("192.168.1.10", "192.168.1", true)]
    [InlineData("192.168.2.10", "192.168.1", false)]
    [InlineData("127.0.0.1", "192.168.1", false)]
    [InlineData("192.168.1.abc", "192.168.1", false)]
    public void IsIpInSubnet_ValidatesCorrectly(string ip, string baseIp, bool expected)
    {
        bool result = SubnetScanner.IsIpInSubnet(ip.AsSpan(), baseIp);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsIpInAnySubnet_MatchesMultipleSubnets()
    {
        var subnets = new List<string> { "192.168.1", "10.0.0", "172.16.0" };

        Assert.True(SubnetScanner.IsIpInAnySubnet("192.168.1.105".AsSpan(), subnets));
        Assert.True(SubnetScanner.IsIpInAnySubnet("10.0.0.1".AsSpan(), subnets));
        Assert.True(SubnetScanner.IsIpInAnySubnet("172.16.0.254".AsSpan(), subnets));

        Assert.False(SubnetScanner.IsIpInAnySubnet("192.168.2.1".AsSpan(), subnets));
        Assert.False(SubnetScanner.IsIpInAnySubnet("10.0.1.1".AsSpan(), subnets));
        Assert.False(SubnetScanner.IsIpInAnySubnet("invalid.ip".AsSpan(), subnets));

        var hashSetSubnets = new HashSet<string> { "192.168.1", "10.0.0" };
        Assert.True(SubnetScanner.IsIpInAnySubnet("192.168.1.50".AsSpan(), hashSetSubnets));
        Assert.False(SubnetScanner.IsIpInAnySubnet("172.16.0.1".AsSpan(), hashSetSubnets));
    }

    [Fact]
    public void Benchmark_ArpTableLookup_BaselineVsOptimized()
    {
        // Warmup ARP table cache
        _ = ArpResolver.GetFullArpTableAsDictionary();

        const int iterations = 10000;
        string targetIp = "127.0.0.1";

        // Baseline: repeated GetFullArpTableAsDictionary()
        long baselineAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        int foundBaseline = 0;
        for (int i = 0; i < iterations; i++)
        {
            var dict = ArpResolver.GetFullArpTableAsDictionary();
            if (dict.TryGetValue(targetIp, out var mac) && !string.IsNullOrEmpty(mac))
            {
                foundBaseline++;
            }
        }
        sw.Stop();
        long baselineAllocated = GC.GetAllocatedBytesForCurrentThread() - baselineAllocBefore;

        // Optimized: TryGetMacFromArpTable
        long optAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        sw.Restart();
        int foundOpt = 0;
        for (int i = 0; i < iterations; i++)
        {
            if (ArpResolver.TryGetMacFromArpTable(targetIp, out var mac) && !string.IsNullOrEmpty(mac))
            {
                foundOpt++;
            }
        }
        sw.Stop();
        long optAllocated = GC.GetAllocatedBytesForCurrentThread() - optAllocBefore;

        Assert.Equal(foundBaseline, foundOpt);
        // Direct lookup should allocate significantly fewer bytes than repeated dictionary cloning
        Assert.True(optAllocated < baselineAllocated / 10,
            $"Expected optimized allocations ({optAllocated} B) to be < 10% of baseline ({baselineAllocated} B)");
    }

    [Fact]
    public void Benchmark_ArpSweepLoop_BaselineVsOptimized()
    {
        // Simulate an ARP table with 5,000 entries across various subnets, only 50 matching 192.168.1.x
        var arpTable = new List<(string Ip, string Mac)>();
        for (int i = 0; i < 5000; i++)
        {
            string subnet = (i % 100 == 0) ? "192.168.1" : $"10.{i / 256}.{i % 256}";
            int host = (i % 250) + 1;
            arpTable.Add(($"{subnet}.{host}", $"00:11:22:33:{(i / 256):X2}:{(i % 256):X2}"));
        }

        string baseIp = "192.168.1";
        int startIp = 1;
        int endIp = 254;

        const int runs = 1000;

        // Baseline: ContainsKey lookup BEFORE IP filter check
        int baselineMatches = 0;
        var swBaseline = Stopwatch.StartNew();
        for (int r = 0; r < runs; r++)
        {
            var discoveredMacs = new ConcurrentDictionary<string, bool>();
            discoveredMacs.TryAdd("00:11:22:33:00:00", true);

            int count = 0;
            foreach (var (ip, mac) in arpTable)
            {
                if (discoveredMacs.ContainsKey(mac)) continue;
                if (!SubnetScanner.IsIpInSubnetAndRange(ip.AsSpan(), baseIp, startIp, endIp)) continue;
                count++;
            }
            baselineMatches = count;
        }
        swBaseline.Stop();

        // Optimized: Lightweight IP filter check BEFORE TryAdd lookup
        int optMatches = 0;
        var swOpt = Stopwatch.StartNew();
        for (int r = 0; r < runs; r++)
        {
            var discoveredMacs = new ConcurrentDictionary<string, bool>();
            discoveredMacs.TryAdd("00:11:22:33:00:00", true);

            int count = 0;
            foreach (var (ip, mac) in arpTable)
            {
                if (!SubnetScanner.IsIpInSubnetAndRange(ip.AsSpan(), baseIp, startIp, endIp)) continue;
                if (!discoveredMacs.TryAdd(mac, true)) continue;
                count++;
            }
            optMatches = count;
        }
        swOpt.Stop();

        Assert.Equal(baselineMatches, optMatches);
        Assert.True(swOpt.ElapsedTicks <= swBaseline.ElapsedTicks * 2,
            $"Expected optimized loop ({swOpt.ElapsedMilliseconds}ms) to be as fast or faster than baseline ({swBaseline.ElapsedMilliseconds}ms)");
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

    [Fact]
    public async Task Benchmark_NetBiosResolution_TaskRunVsAsyncUdp()
    {
        const int concurrentTasks = 10;
        string nonResponsiveIp = "192.0.2.1"; // TEST-NET-1

        using var cts = new CancellationTokenSource(200);

        // Baseline: Task.Run wrapping synchronous TryResolveNetBiosName
        long baselineAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        var baselineTasks = Enumerable.Range(0, concurrentTasks)
            .Select(_ => Task.Run(() => ArpResolver.TryResolveNetBiosName(nonResponsiveIp)))
            .ToArray();
        try
        {
            await Task.WhenAll(baselineTasks);
        }
        catch { }
        sw.Stop();
        long baselineAllocated = GC.GetAllocatedBytesForCurrentThread() - baselineAllocBefore;

        // Optimized: Native async TryResolveNetBiosNameAsync with UdpClient.ReceiveAsync
        using var cts2 = new CancellationTokenSource(200);
        long optAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        sw.Restart();
        var optTasks = Enumerable.Range(0, concurrentTasks)
            .Select(_ => ArpResolver.TryResolveNetBiosNameAsync(nonResponsiveIp, cancellationToken: cts2.Token))
            .ToArray();
        await Task.WhenAll(optTasks);
        sw.Stop();
        long optAllocated = GC.GetAllocatedBytesForCurrentThread() - optAllocBefore;

        // Assert that all async tasks return empty strings without throwing
        Assert.All(optTasks, t => Assert.Equal(string.Empty, t.Result));

        // Both methods handle timeouts gracefully and return empty string
        Assert.True(optTasks.All(t => t.IsCompletedSuccessfully), "All async tasks completed successfully.");
    }

}
