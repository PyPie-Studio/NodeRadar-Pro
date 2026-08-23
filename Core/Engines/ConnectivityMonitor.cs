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
    private readonly ConcurrentDictionary<string, NetworkNode> _trackedDevices = new();
    private bool _isRunning = false;
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
    public bool IsRunning => _isRunning;

    public void SetDatabase(LocalDatabase db) => _db = db;

    public void UpdateTrackedDevices(List<NetworkNode> devices)
    {
        foreach (var device in devices)
        {
            if (device.MacAddress == "Unknown") continue;
            _trackedDevices.AddOrUpdate(device.MacAddress, device, (_, existing) =>
            {
                device.CustomName = string.IsNullOrEmpty(device.CustomName) ? existing.CustomName : device.CustomName;
                device.Notes = string.IsNullOrEmpty(device.Notes) ? existing.Notes : device.Notes;
                device.Location = string.IsNullOrEmpty(device.Location) ? existing.Location : device.Location;
                device.DeviceName = string.IsNullOrEmpty(device.DeviceName) ? existing.DeviceName : device.DeviceName;
                device.DeviceModel = string.IsNullOrEmpty(device.DeviceModel) ? existing.DeviceModel : device.DeviceModel;
                device.IsRegistered = existing.IsRegistered || device.IsRegistered;
                device.FirstSeen = existing.FirstSeen;
                device.PingHistory = existing.PingHistory;
                return device;
            });
        }
    }

    public void AddDevice(NetworkNode device)
    {
        if (device.MacAddress != "Unknown")
            _trackedDevices.UpdateNode(device);
    }

    public void RemoveDevice(string macAddress) => _trackedDevices.TryRemove(macAddress, out _);

    public List<NetworkNode> GetAllDevices() => _trackedDevices.Values.ToList();

    public async Task StartMonitoringAsync(CancellationToken token)
    {
        if (_isRunning) return;
        _isRunning = true;

        try
        {
            if (_trackedDevices.Count > 0 && !token.IsCancellationRequested)
                await CheckAllDevicesAsync(token);

            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), token); }
                catch (TaskCanceledException) { break; }
                await CheckAllDevicesAsync(token);
            }
        }
        finally { _isRunning = false; }
    }

    private async Task CheckAllDevicesAsync(CancellationToken token)
    {
        var devices = _trackedDevices.Values.ToList();
        if (devices.Count == 0) return;

        var uptimeSnapshots = new List<UptimeSnapshot>();

        var arpTableDict = await Task.Run(() => ArpResolver.GetFullArpTableAsDictionary());

        var tasks = devices.Select(async device =>
        {
            if (token.IsCancellationRequested) return;
            if (device.IpAddress == "0.0.0.0" || string.IsNullOrEmpty(device.IpAddress)) return;

            bool wasOnline = device.IsOnline;
            bool arpOnline = false;
            bool icmpOnline = false;
            long latency = -1;
            int pingTimeout = Math.Min(TimeoutMs, 2000);

            // Fast ARP check using cached ARP table
            if (arpTableDict.TryGetValue(device.IpAddress, out var arpMac) && !string.IsNullOrEmpty(arpMac) && arpMac != "Unknown")
            {
                arpOnline = true;
            }
            else
            {
                try
                {
                    await _arpSemaphore.WaitAsync(token);
                    try
                    {
                        string mac = await Task.Run(() => ArpResolver.ResolveMacAddress(device.IpAddress));
                        if (mac != "Unknown") arpOnline = true;
                    }
                    finally
                    {
                        _arpSemaphore.Release();
                    }
                }
                catch { }
            }

            // ICMP ping
            try
            {
                using var pinger = new Ping();
                var reply = await pinger.SendPingAsync(device.IpAddress, pingTimeout);
                if (reply.Status == IPStatus.Success)
                {
                    icmpOnline = true;
                    latency = reply.RoundtripTime;
                }
            }
            catch { }

            // TCP fallback
            bool tcpOnline = false;
            if (!arpOnline && !icmpOnline)
                tcpOnline = await TryTcpProbeAsync(device.IpAddress);

            bool nowOnline = arpOnline || icmpOnline || tcpOnline;

            // Record ping result
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

            // Record uptime snapshot
            lock (uptimeSnapshots)
            {
                uptimeSnapshots.Add(new UptimeSnapshot
                {
                    MacAddress = device.MacAddress,
                    IsOnline = device.IsOnline,
                    LatencyMs = device.PingLatencyMs,
                    Timestamp = DateTime.UtcNow
                });
            }

            // ── Alert checks ──

            // Connection Lost
            if (wasOnline && !device.IsOnline)
            {
                device.WasOnlinePreviously = true;
                DeviceWentOffline?.Invoke(device);

                if (device.AlertOnConnectionLost)
                {
                    var alert = new AlertEvent
                    {
                        MacAddress = device.MacAddress,
                        DeviceName = device.DisplayName,
                        IpAddress = device.IpAddress,
                        AlertType = AlertType.ConnectionLost,
                        Message = $"{device.DisplayName} ({device.IpAddress}) went offline"
                    };
                    try { _db?.InsertAlert(alert); } catch { }
                    try { _db?.Log(LogLevel.Warning, "Monitor", alert.Message, device.MacAddress); } catch { }
                    AlertTriggered?.Invoke(alert);
                }
            }
            // Device came online — fire alert event, not just log entry (I8)
            else if (!wasOnline && device.IsOnline)
            {
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
                try { _db?.InsertAlert(reconnectAlert); } catch { }
                try { _db?.Log(LogLevel.Info, "Monitor", reconnectAlert.Message, device.MacAddress); } catch { }
                AlertTriggered?.Invoke(reconnectAlert);
            }

            // High Latency — with 5-minute cooldown per device (B12)
            if (device.IsOnline && device.AlertOnHighLatency && latency > LatencyThresholdMs && latency > 0)
            {
                bool shouldAlert = !_lastLatencyAlert.TryGetValue(device.MacAddress, out var lastTime)
                    || (DateTime.UtcNow - lastTime) > AlertCooldown;

                if (shouldAlert)
                {
                    _lastLatencyAlert[device.MacAddress] = DateTime.UtcNow;
                    var alert = new AlertEvent
                    {
                        MacAddress = device.MacAddress,
                        DeviceName = device.DisplayName,
                        IpAddress = device.IpAddress,
                        AlertType = AlertType.HighLatency,
                        Message = $"{device.DisplayName} latency spike: {latency}ms (threshold: {LatencyThresholdMs}ms)"
                    };
                    try { _db?.InsertAlert(alert); } catch { }
                    AlertTriggered?.Invoke(alert);
                }
            }

            // Packet Loss — with 5-minute cooldown per device (B11)
            if (device.PingHistory.Count >= 10 && device.PacketLossPct > PacketLossThresholdPct)
            {
                bool shouldAlert = !_lastPacketLossAlert.TryGetValue(device.MacAddress, out var lastTime)
                    || (DateTime.UtcNow - lastTime) > AlertCooldown;

                if (shouldAlert)
                {
                    _lastPacketLossAlert[device.MacAddress] = DateTime.UtcNow;
                    var alert = new AlertEvent
                    {
                        MacAddress = device.MacAddress,
                        DeviceName = device.DisplayName,
                        IpAddress = device.IpAddress,
                        AlertType = AlertType.PacketLoss,
                        Message = $"{device.DisplayName} packet loss: {device.PacketLossPct:F1}% (threshold: {PacketLossThresholdPct}%)"
                    };
                    try { _db?.InsertAlert(alert); } catch { }
                    AlertTriggered?.Invoke(alert);
                }
            }
        });

        await Task.WhenAll(tasks);

        // Persist uptime snapshots
        try { _db?.InsertUptimeSnapshots(uptimeSnapshots); } catch { }

        StatusUpdated?.Invoke(_trackedDevices.Values.ToList());
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
