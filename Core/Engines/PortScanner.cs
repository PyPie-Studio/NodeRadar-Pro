using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core.Fingerprinting.Probes;

namespace NodeRadarPro.Core;

/// <summary>
/// Fast async port scanner with common port list, top-100 fast scan, and OS fingerprinting.
/// </summary>
public static class PortScanner
{
    private static readonly Dictionary<int, string> _commonPorts = new()
    {
        { 21, "FTP" }, { 22, "SSH" }, { 23, "Telnet" }, { 25, "SMTP" },
        { 53, "DNS" }, { 80, "HTTP" }, { 110, "POP3" }, { 135, "RPC" },
        { 139, "NetBIOS" }, { 143, "IMAP" }, { 443, "HTTPS" }, { 445, "SMB" },
        { 993, "IMAPS" }, { 995, "POP3S" },
        { 1433, "SQL Server" }, { 1521, "Oracle" }, { 3306, "MySQL" },
        { 3389, "RDP" }, { 5432, "PostgreSQL" }, { 5900, "VNC" },
        { 8080, "HTTP Alt" }, { 8443, "HTTPS Alt" },
        { 62078, "iphone-sync" }, { 5353, "mDNS" }, { 548, "AFP" },
        { 631, "IPP" }, { 9100, "Print" }
    };

    // Top 100 ports for fast scan mode
    private static readonly int[] _top100Ports = {
        7, 9, 13, 21, 22, 23, 25, 26, 37, 53, 79, 80, 81, 88, 106, 110, 111,
        113, 119, 135, 139, 143, 144, 179, 199, 389, 427, 443, 444, 445, 465,
        513, 514, 515, 543, 544, 548, 554, 587, 631, 646, 873, 990, 993, 995,
        1025, 1026, 1027, 1028, 1029, 1110, 1433, 1720, 1723, 1755, 1900, 2000,
        2001, 2049, 2121, 2717, 3000, 3128, 3306, 3389, 3986, 4899, 5000, 5009,
        5051, 5060, 5101, 5190, 5357, 5432, 5631, 5666, 5800, 5900, 6000, 6001,
        6646, 7070, 8000, 8008, 8009, 8080, 8081, 8443, 8888, 9100, 9999, 10000,
        32768, 49152, 49153, 49154, 49155, 49156, 49157, 62078
    };

    /// <summary>Scans common ports and returns human-readable results.</summary>
    public static async Task<List<string>> ScanCommonPortsAsync(string ipAddress, CancellationToken token = default)
    {
        var openPorts = new ConcurrentBag<string>();
        using var semaphore = new SemaphoreSlim(64);

        var tasks = _commonPorts.Keys.Select(async port =>
        {
            if (token.IsCancellationRequested) return;

            await semaphore.WaitAsync(token);
            try
            {
                if (token.IsCancellationRequested) return;
                if (await IsPortOpenAsync(ipAddress, port, 500, token))
                    openPorts.Add($"{port} ({_commonPorts[port]})");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return new List<string>(openPorts);
    }

    /// <summary>Scans ports and returns port numbers mapped to their banners. Supports fast scan mode.</summary>
    public static async Task<Dictionary<int, string>> ScanPortsAsync(string ipAddress, bool fastScan = false, int timeoutMs = 500, CancellationToken token = default)
    {
        var results = new ConcurrentDictionary<int, string>();
        var ports = fastScan ? _top100Ports : _commonPorts.Keys.ToArray();
        using var semaphore = new SemaphoreSlim(64);

        var tasks = ports.Select(async port =>
        {
            if (token.IsCancellationRequested) return;

            await semaphore.WaitAsync(token);
            try
            {
                if (token.IsCancellationRequested) return;
                if (await IsPortOpenAsync(ipAddress, port, timeoutMs, token))
                {
                    // Grab banner
                    string banner = await BannerGrabProbe.GrabBannerAsync(ipAddress, port, token);
                    results.TryAdd(port, banner);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>Scans a custom range of ports.</summary>
    public static async Task<Dictionary<int, string>> ScanRangeAsync(string ipAddress, int startPort, int endPort, int timeoutMs = 500, CancellationToken token = default)
    {
        var results = new ConcurrentDictionary<int, string>();
        using var semaphore = new SemaphoreSlim(64);

        var tasks = Enumerable.Range(startPort, endPort - startPort + 1).Select(async port =>
        {
            if (token.IsCancellationRequested) return;

            await semaphore.WaitAsync(token);
            try
            {
                if (token.IsCancellationRequested) return;
                if (await IsPortOpenAsync(ipAddress, port, timeoutMs, token))
                {
                    string banner = await BannerGrabProbe.GrabBannerAsync(ipAddress, port, token);
                    results.TryAdd(port, banner);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>Gets the service name for a well-known port.</summary>
    public static string GetServiceName(int port)
    {
        return _commonPorts.TryGetValue(port, out var name) ? name : $"Port {port}";
    }

    /// <summary>Guesses the OS based on open port signatures.</summary>
    public static string GuessOs(List<int> openPorts)
    {
        if (openPorts.Count == 0) return "";

        bool has3389 = openPorts.Contains(3389);
        bool has445 = openPorts.Contains(445);
        bool has135 = openPorts.Contains(135);
        bool has22 = openPorts.Contains(22);
        bool has548 = openPorts.Contains(548);
        bool has62078 = openPorts.Contains(62078);
        bool has5353 = openPorts.Contains(5353);

        if (has62078) return "iOS / iPadOS";
        if (has548 && has5353 && has22) return "macOS";
        if (has548 && has5353) return "macOS";
        if (has3389 && has445 && has135) return "Windows";
        if (has3389 && has445) return "Windows";
        if (has3389) return "Windows (RDP)";
        if (has445 && has135) return "Windows";
        if (has22 && !has445) return "Linux / Unix";
        if (has22) return "Linux / Unix";
        if (openPorts.Contains(80) && openPorts.Contains(443) && openPorts.Count <= 4) return "Network Device";

        return "";
    }

    private static async Task<bool> IsPortOpenAsync(string ipAddress, int port, int timeoutMs = 500, CancellationToken token = default)
    {
        try
        {
            using var tcpClient = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeoutMs);
            try
            {
                await tcpClient.ConnectAsync(ipAddress, port, cts.Token);
                return tcpClient.Connected;
            }
            catch (OperationCanceledException) { return false; }
            catch { return false; }
        }
        catch { return false; }
    }
}
