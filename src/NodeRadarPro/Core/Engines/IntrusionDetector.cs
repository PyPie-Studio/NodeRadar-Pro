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

    public int InitialDelaySeconds { get; set; } = 30;
    public string? BaseIpOverride { get; set; }

    public void Configure(AppSettings settings)
    {
        _scanner.PreferredInterfaceName = settings.SelectedInterfaceName;
        _scanner.TimeoutMs = settings.ResponseTimeoutMs;
        _scanner.EnableDnsResolve = settings.EnableDnsResolve;
        _scanner.FastScanMode = settings.EnableFastScan;
        _scanner.EnableOsDetection = settings.EnableOsDetection;
        _scanner.EnableInlinePortScan = settings.EnableInlinePortScan;
    }

    public Task SweepOnceAsync(CancellationToken token = default) => SweepOnceAsync(null, token);

    public async Task SweepOnceAsync(string? baseIp, CancellationToken token = default)
    {
        try
        {
            string targetBaseIp = !string.IsNullOrEmpty(baseIp)
                ? baseIp
                : (!string.IsNullOrEmpty(BaseIpOverride) ? BaseIpOverride : SubnetScanner.GetLocalBaseIp());
            var results = await _scanner.ScanSubnetAsync(targetBaseIp, token);

            var newNodes = Database.MergeWithHistoryBulk(results);
            foreach (var node in newNodes)
            {
                if (token.IsCancellationRequested) break;

                Database.InsertAlert(new AlertEvent
                {
                    Timestamp = DateTime.UtcNow,
                    AlertType = AlertType.NewDeviceDiscovered,
                    MacAddress = node.MacAddress,
                    DeviceName = node.DisplayName,
                    IpAddress = node.IpAddress,
                    Message = $"Unauthorized / new device discovered: {node.DisplayName} ({node.IpAddress} / {node.MacAddress})"
                });

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
        catch (OperationCanceledException)
        {
            // Clean cancellation during shutdown
        }
        catch (SocketException ex)
        {
            Database.Log(LogLevel.Error, "IntrusionDetector", $"Sweep failed: {ex.Message}");
        }
        catch (TimeoutException ex)
        {
            Database.Log(LogLevel.Error, "IntrusionDetector", $"Sweep failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Database.Log(LogLevel.Warning, "IntrusionDetector", $"Sweep completed with warning: {ex.Message}");
        }
    }

    public async Task StartAsync(CancellationToken token)
    {
        if (InitialDelaySeconds > 0)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(InitialDelaySeconds), token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

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
