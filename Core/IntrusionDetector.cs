using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Data;

namespace NodeRadarPro.Core;

/// <summary>
/// Background service that periodically sweeps the network to detect new devices.
/// </summary>
public class IntrusionDetector
{
    private readonly SubnetScanner _scanner = new();

    public async Task StartAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                string baseIp = SubnetScanner.GetLocalBaseIp();
                var results = await _scanner.ScanSubnetAsync(baseIp, token);

                var newNodes = LocalDatabase.Instance.MergeWithHistoryBulk(results);
                foreach (var node in newNodes)
                {
                    if (token.IsCancellationRequested) break;
                    IntrusionAlerter.AlertNewDevice(node);
                }
            }
            catch (SocketException ex)
            {
                LocalDatabase.Instance.Log(LogLevel.Error, "IntrusionDetector", $"Sweep failed: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                LocalDatabase.Instance.Log(LogLevel.Error, "IntrusionDetector", $"Sweep failed: {ex.Message}");
            }

            try
            {
                // Wait for 5 minutes between sweeps as per Task 2 requirements.
                await Task.Delay(TimeSpan.FromMinutes(5), token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
