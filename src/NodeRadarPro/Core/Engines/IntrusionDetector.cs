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
    private readonly SubnetScanner _scanner;
    private readonly LocalDatabase? _dbOverride;
    private readonly Action<NetworkNode>? _onAlertOverride;

    private LocalDatabase Database => _dbOverride ?? LocalDatabase.Instance;

    public IntrusionDetector(SubnetScanner? scanner = null, LocalDatabase? db = null, Action<NetworkNode>? onAlert = null)
    {
        _scanner = scanner ?? new SubnetScanner();
        _dbOverride = db;
        _onAlertOverride = onAlert;
    }

    public void Configure(AppSettings settings)
    {
        _scanner.PreferredInterfaceName = settings.SelectedInterfaceName;
        _scanner.TimeoutMs = settings.ResponseTimeoutMs;
        _scanner.EnableDnsResolve = settings.EnableDnsResolve;
        _scanner.FastScanMode = settings.EnableFastScan;
        _scanner.EnableOsDetection = settings.EnableOsDetection;
        _scanner.EnableInlinePortScan = settings.EnableInlinePortScan;
    }

    public async Task SweepOnceAsync(CancellationToken token = default)
    {
        try
        {
            string baseIp = SubnetScanner.GetLocalBaseIp();
            var results = await _scanner.ScanSubnetAsync(baseIp, token);

            var newNodes = Database.MergeWithHistoryBulk(results);
            foreach (var node in newNodes)
            {
                if (token.IsCancellationRequested) break;
                if (_onAlertOverride != null)
                {
                    _onAlertOverride(node);
                }
                else
                {
                    IntrusionAlerter.AlertNewDevice(node);
                }
            }
        }
        catch (SocketException ex)
        {
            Database.Log(LogLevel.Error, "IntrusionDetector", $"Sweep failed: {ex.Message}");
        }
        catch (TimeoutException ex)
        {
            Database.Log(LogLevel.Error, "IntrusionDetector", $"Sweep failed: {ex.Message}");
        }
    }

    public async Task StartAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await SweepOnceAsync(token);

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
