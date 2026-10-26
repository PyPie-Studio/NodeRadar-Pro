using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Data;

namespace NodeRadarPro.Core;

/// <summary>
/// Background service that periodically checks all tracked devices
/// using ARP + ICMP + TCP fallback, records uptime snapshots,
/// calculates packet loss, and fires alert events.
/// </summary>
public class ConnectivityMonitor
{
    private const string MonitorLogCategory = "Monitor";

    private readonly ConcurrentDictionary<string, NetworkNode> _trackedDevices = new();
    private int _isRunningState = 0;
    private LocalDatabase? _db;

    // ── Alert cooldown trackers (prevent flooding) ──
    private readonly ConcurrentDictionary<string, DateTime> _lastLatencyAlert = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastPacketLossAlert = new();

    // Concurrency limit for synchronous ARP calls to prevent ThreadPool exhaustion
    private static readonly SemaphoreSlim _arpSemaphore = new SemaphoreSlim(10);

    private static readonly TimeSpan AlertCooldown = TimeSpan.FromMinutes(5);

    // ── Events ──
    public event Action<NetworkNode>? DeviceWentOffline;
    public event Action<NetworkNode>? DeviceCameOnline;
    public event Action<List<NetworkNode>>? StatusUpdated;
    public event Action<AlertEvent>? AlertTriggered;

    // ── Configuration ──
    public int IntervalSeconds { get; set; } = 60;
    public int TimeoutMs { get; set; } = 2000;
    public int LatencyThresholdMs { get; set; } = 200;
    public double PacketLossThresholdPct { get; set; } = 5.0;
    public bool EnableSynScan { get; set; } = false;
    public bool EnableToastAlerts { get; set; } = true;
    public bool EnableSoundAlerts { get; set; } = true;
    public bool EnableEmailAlerts { get; set; } = false;
    public string PreferredInterfaceName { get; set; } = "";
    public bool IsRunning => _isRunningState == 1;

    private readonly IPingProvider _pingProvider;
    private readonly IArpResolver _arpResolver;

    public ConnectivityMonitor(
        LocalDatabase? db = null,
        IPingProvider? pingProvider = null,
        IArpResolver? arpResolver = null)
    {
        _db = db;
        _pingProvider = pingProvider ?? new DefaultPingProvider();
        _arpResolver = arpResolver ?? new DefaultArpResolver();
    }

    public void SetDatabase(LocalDatabase db) => _db = db;

    public void UpdateTrackedDevices(List<NetworkNode> devices)
    {
        foreach (var device in devices)
        {
            UpdateTrackedDevice(device);
        }
    }

    private void UpdateTrackedDevice(NetworkNode device)
    {
        if (device.MacAddress == ArpResolver.UnknownMac)
        {
            return;
        }

        _trackedDevices.AddOrUpdate(device.MacAddress, device, (_, existing) => MergeDeviceProperties(device, existing));
    }

    private static NetworkNode MergeDeviceProperties(NetworkNode device, NetworkNode existing)
    {
        device.CustomName = PreferNonEmpty(device.CustomName, existing.CustomName);
        device.Notes = PreferNonEmpty(device.Notes, existing.Notes);
        device.Location = PreferNonEmpty(device.Location, existing.Location);
        device.DeviceName = PreferNonEmpty(device.DeviceName, existing.DeviceName);
        device.DeviceModel = PreferNonEmpty(device.DeviceModel, existing.DeviceModel);
        device.IsRegistered = existing.IsRegistered || device.IsRegistered;
        device.FirstSeen = existing.FirstSeen;
        device.PingHistory = existing.PingHistory;
        return device;
    }

    private static string PreferNonEmpty(string newValue, string existingValue) =>
        string.IsNullOrEmpty(newValue) ? existingValue : newValue;

    public void AddDevice(NetworkNode device)
    {
        if (device.MacAddress != ArpResolver.UnknownMac)
            _trackedDevices.UpdateNode(device);
    }

    public void RemoveDevice(string macAddress) => _trackedDevices.TryRemove(macAddress, out _);

    public List<NetworkNode> GetAllDevices() => _trackedDevices.Values.ToList();

    public async Task StartMonitoringAsync(CancellationToken token)
    {
        if (Interlocked.CompareExchange(ref _isRunningState, 1, 0) != 0) return;

        try
        {
            if (!_trackedDevices.IsEmpty && !token.IsCancellationRequested)
                await CheckAllDevicesAsync(token);

            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), token); }
                catch (TaskCanceledException) { break; }
                await CheckAllDevicesAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            // Monitoring loop gracefully stopped via cancellation token
        }
        finally { Interlocked.Exchange(ref _isRunningState, 0); }
    }

    private async Task CheckAllDevicesAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        var devices = _trackedDevices.Values.ToList();
        if (devices.Count == 0) return;

        var uptimeSnapshots = new List<UptimeSnapshot>();
        var alertsToInsert = new ConcurrentBag<AlertEvent>();

        var arpTableDict = await Task.Run(() => _arpResolver.GetFullArpTableAsDictionary())
                           ?? (IReadOnlyDictionary<string, string>)new Dictionary<string, string>();

        using var throttle = new SemaphoreSlim(32);
        var tasks = devices.Select(async device =>
        {
            try
            {
                await throttle.WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await CheckDeviceAsync(device, arpTableDict, uptimeSnapshots, alertsToInsert, token);
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation per device
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ConnectivityMonitor", $"Device connectivity check failed for {device.IpAddress}: {ex.Message}");
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (token.IsCancellationRequested) return;

        PersistAlertsAndSnapshots(alertsToInsert, uptimeSnapshots);
        StatusUpdated?.Invoke(_trackedDevices.Values.ToList());
    }

    private async Task CheckDeviceAsync(
        NetworkNode device,
        IReadOnlyDictionary<string, string> arpTableDict,
        List<UptimeSnapshot> uptimeSnapshots,
        ConcurrentBag<AlertEvent> alertsToInsert,
        CancellationToken token)
    {
        if (token.IsCancellationRequested || !IsValidIp(device.IpAddress))
        {
            return;
        }

        bool wasOnline = device.IsOnline;
        var (nowOnline, latency) = await ProbeDeviceConnectivityAsync(device.IpAddress, arpTableDict, token);

        if (token.IsCancellationRequested)
        {
            return;
        }

        ApplyProbeResult(device, nowOnline, latency);
        RecordUptimeSnapshot(uptimeSnapshots, device);
        EvaluateAlerts(device, wasOnline, latency, alertsToInsert);
    }

    private async Task<(bool nowOnline, long latency)> ProbeDeviceConnectivityAsync(
        string ip,
        IReadOnlyDictionary<string, string> arpTableDict,
        CancellationToken token)
    {
        bool arpOnline = await CheckArpStatusAsync(ip, arpTableDict, token);
        var (icmpOnline, latency) = await CheckIcmpStatusAsync(ip);

        bool tcpOnline = false;
        if (!arpOnline && !icmpOnline)
        {
            tcpOnline = await TryTcpProbeAsync(ip);
        }

        bool nowOnline = arpOnline || icmpOnline || tcpOnline;
        return (nowOnline, latency);
    }

    private static bool IsValidIp(string? ip) =>
        !string.IsNullOrEmpty(ip) && ip != "0.0.0.0";

    private static bool HasValidMac(string? mac) =>
        !string.IsNullOrEmpty(mac) && mac != ArpResolver.UnknownMac;

    private async Task<bool> CheckArpStatusAsync(
        string ip,
        IReadOnlyDictionary<string, string>? arpTableDict,
        CancellationToken token)
    {
        if (arpTableDict != null && arpTableDict.TryGetValue(ip, out var arpMac) && HasValidMac(arpMac))
        {
            return true;
        }

        return await ResolveArpWithThrottlingAsync(ip, token);
    }

    private async Task<bool> ResolveArpWithThrottlingAsync(string ip, CancellationToken token)
    {
        try
        {
            await _arpSemaphore.WaitAsync(token);
            try
            {
                string mac = await Task.Run(() => _arpResolver.ResolveMacAddress(ip), token);
                return mac != ArpResolver.UnknownMac;
            }
            finally
            {
                _arpSemaphore.Release();
            }
        }
        catch (Exception)
        {
            // Fallback to offline on ARP resolution failure
            return false;
        }
    }

    private async Task<(bool icmpOnline, long latency)> CheckIcmpStatusAsync(string ip)
    {
        int pingTimeout = Math.Min(TimeoutMs, 2000);
        try
        {
            var reply = await _pingProvider.SendPingAsync(ip, pingTimeout);
            if (reply.Status == IPStatus.Success)
            {
                return (true, reply.RoundtripTime);
            }
        }
        catch (PingException)
        {
            // Expected when host is offline or ICMP is filtered; fallback to TCP probe below
        }
        catch (OperationCanceledException)
        {
            // Cancellation observed; abort probe for this device
        }
        catch (Exception)
        {
            // Non-fatal probe failure; fallback to TCP probe below
        }

        return (false, -1);
    }

    private static void ApplyProbeResult(NetworkNode device, bool nowOnline, long latency)
    {
        device.RecordPing(nowOnline);

        if (nowOnline)
        {
            device.FailedCheckCount = 0;
            device.IsOnline = true;
            device.PingLatencyMs = latency;
            device.LastSeen = DateTime.UtcNow;
        }
        else
        {
            device.FailedCheckCount++;
            if (device.FailedCheckCount >= 3)
            {
                device.IsOnline = false;
                device.PingLatencyMs = -1;
            }
        }
    }

    private static void RecordUptimeSnapshot(List<UptimeSnapshot> snapshots, NetworkNode device)
    {
        var snapshot = new UptimeSnapshot
        {
            MacAddress = device.MacAddress,
            IsOnline = device.IsOnline,
            LatencyMs = device.PingLatencyMs,
            Timestamp = DateTime.UtcNow
        };

        lock (snapshots)
        {
            snapshots.Add(snapshot);
        }
    }

    private void EvaluateAlerts(
        NetworkNode device,
        bool wasOnline,
        long latency,
        ConcurrentBag<AlertEvent> alerts)
    {
        CheckConnectionLost(device, wasOnline, alerts);
        CheckReconnection(device, wasOnline, alerts);
        CheckHighLatency(device, latency, alerts);
        CheckPacketLoss(device, alerts);
    }

    private void CheckConnectionLost(NetworkNode device, bool wasOnline, ConcurrentBag<AlertEvent> alerts)
    {
        if (!wasOnline || device.IsOnline)
        {
            return;
        }

        device.WasOnlinePreviously = true;
        DeviceWentOffline?.Invoke(device);

        if (!device.AlertOnConnectionLost)
        {
            return;
        }

        var alert = new AlertEvent
        {
            MacAddress = device.MacAddress,
            DeviceName = device.DisplayName,
            IpAddress = device.IpAddress,
            AlertType = AlertType.ConnectionLost,
            Message = $"{device.DisplayName} ({device.IpAddress}) went offline"
        };
        alerts.Add(alert);
        LogMonitorEvent(LogLevel.Warning, alert.Message, device.MacAddress);
        AlertTriggered?.Invoke(alert);
    }

    private void CheckReconnection(NetworkNode device, bool wasOnline, ConcurrentBag<AlertEvent> alerts)
    {
        if (wasOnline || !device.IsOnline)
        {
            return;
        }

        device.WasOnlinePreviously = true;
        DeviceCameOnline?.Invoke(device);

        var reconnectAlert = new AlertEvent
        {
            MacAddress = device.MacAddress,
            DeviceName = device.DisplayName,
            IpAddress = device.IpAddress,
            AlertType = AlertType.DeviceReconnected,
            Message = $"{device.DisplayName} ({device.IpAddress}) came back online",
            IsResolved = true,
            ResolvedAt = DateTime.UtcNow
        };
        alerts.Add(reconnectAlert);
        LogMonitorEvent(LogLevel.Info, reconnectAlert.Message, device.MacAddress);
        AlertTriggered?.Invoke(reconnectAlert);
    }

    private void CheckHighLatency(NetworkNode device, long latency, ConcurrentBag<AlertEvent> alerts)
    {
        if (!device.IsOnline || !device.AlertOnHighLatency || latency <= LatencyThresholdMs || latency <= 0)
        {
            return;
        }

        if (!TryConsumeAlertCooldown(_lastLatencyAlert, device.MacAddress))
        {
            return;
        }

        var alert = new AlertEvent
        {
            MacAddress = device.MacAddress,
            DeviceName = device.DisplayName,
            IpAddress = device.IpAddress,
            AlertType = AlertType.HighLatency,
            Message = $"{device.DisplayName} latency spike: {latency}ms (threshold: {LatencyThresholdMs}ms)"
        };
        alerts.Add(alert);
        AlertTriggered?.Invoke(alert);
    }

    private void CheckPacketLoss(NetworkNode device, ConcurrentBag<AlertEvent> alerts)
    {
        if (device.PingHistory.Count < 10 || device.PacketLossPct <= PacketLossThresholdPct)
        {
            return;
        }

        if (!TryConsumeAlertCooldown(_lastPacketLossAlert, device.MacAddress))
        {
            return;
        }

        var alert = new AlertEvent
        {
            MacAddress = device.MacAddress,
            DeviceName = device.DisplayName,
            IpAddress = device.IpAddress,
            AlertType = AlertType.PacketLoss,
            Message = $"{device.DisplayName} packet loss: {device.PacketLossPct:F1}% (threshold: {PacketLossThresholdPct}%)"
        };
        alerts.Add(alert);
        AlertTriggered?.Invoke(alert);
    }

    private static bool TryConsumeAlertCooldown(ConcurrentDictionary<string, DateTime> tracker, string mac)
    {
        if (tracker.TryGetValue(mac, out var lastTime) && (DateTime.UtcNow - lastTime) <= AlertCooldown)
        {
            return false;
        }

        tracker[mac] = DateTime.UtcNow;
        return true;
    }

    private void LogMonitorEvent(LogLevel level, string message, string macAddress)
    {
        try
        {
            _db?.Log(level, MonitorLogCategory, message, macAddress);
        }
        catch (Exception)
        {
            // Suppress database logging failures during monitoring cycles
        }
    }

    private void PersistAlertsAndSnapshots(ConcurrentBag<AlertEvent> alerts, List<UptimeSnapshot> snapshots)
    {
        if (!alerts.IsEmpty)
        {
            try
            {
                _db?.InsertAlerts(alerts);
            }
            catch (Exception)
            {
                // Suppress persistence errors during monitoring cycle
            }
        }

        try
        {
            _db?.InsertUptimeSnapshots(snapshots);
        }
        catch (Exception)
        {
            // Suppress persistence errors during monitoring cycle
        }
    }

    private async Task<bool> TryTcpProbeAsync(string ip)
    {
        // Standard quick ports
        int[] ports = { 80, 443, 22, 53, 8080 };

        // S1: Enhanced TCP probe when SYN scan mode is enabled
        int[] extendedPorts = EnableSynScan
            ? new[] { 80, 443, 22, 53, 8080, 135, 139, 445, 3389, 5353, 62078, 548, 5900, 8443, 9100 }
            : ports;

        using var cts = new CancellationTokenSource(300);
        var tasks = extendedPorts.Select(async port =>
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(ip, port, cts.Token);
                if (tcp.Connected)
                {
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions (timeout, refused, etc.)
            }
            return false;
        }).ToList();

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);

            if (await completedTask)
            {
                cts.Cancel(); // Cancel remaining attempts
                return true;
            }
        }

        return false;
    }

}
