using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

/// <summary>
/// Fast async port scanner to determine what services are running on a discovered node.
/// </summary>
public static class PortScanner
{
    // The most common ports IT admins care about
    private static readonly Dictionary<int, string> _commonPorts = new()
    {
        { 21, "FTP" },
        { 22, "SSH" },
        { 23, "Telnet" },
        { 25, "SMTP" },
        { 53, "DNS" },
        { 80, "HTTP" },
        { 110, "POP3" },
        { 135, "RPC" },
        { 139, "NetBIOS" },
        { 143, "IMAP" },
        { 443, "HTTPS" },
        { 445, "SMB" },
        { 1433, "SQL Server" },
        { 3306, "MySQL" },
        { 3389, "RDP" },
        { 5432, "MySQL" },
        { 5900, "VNC" },
        { 8080, "HTTP Alternate" }
    };

    public static async Task<List<string>> ScanCommonPortsAsync(string ipAddress, CancellationToken token = default)
    {
        var openPorts = new ConcurrentBag<string>();
        
        var tasks = new List<Task>();
        foreach (var port in _commonPorts.Keys)
        {
            tasks.Add(Task.Run(async () => 
            {
                if (token.IsCancellationRequested) return;

                if (await IsPortOpenAsync(ipAddress, port))
                {
                    openPorts.Add($"{port} ({_commonPorts[port]})");
                }
            }));
        }

        await Task.WhenAll(tasks);
        return new List<string>(openPorts);
    }

    private static async Task<bool> IsPortOpenAsync(string ipAddress, int port)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(ipAddress, port);
            // 500ms timeout so we don't hang the UI waiting for firewalls to drop packets
            var timeoutTask = Task.Delay(500); 

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            if (completedTask == timeoutTask) return false;

            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }
}