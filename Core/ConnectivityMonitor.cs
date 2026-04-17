using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

/// <summary>
/// Background service that periodically checks all tracked devices
/// using ARP + ICMP + TCP fallback, and fires events on status changes.
/// </summary>
public class ConnectivityMonitor
{
    private const int PingTimeoutMs = 2000;
    
    private readonly ConcurrentDictionary<string, NetworkNode> _trackedDevices = new();
    private bool _isRunning = false;
    
    /// <summary>Fired when a previously-online device stops responding.</summary>
    public event Action<NetworkNode>? DeviceWentOffline;
    
    /// <summary>Fired when a previously-offline device starts responding.</summary>
    public event Action<NetworkNode>? DeviceCameOnline;
    
    /// <summary>Fired after each complete ping cycle with the full device list.</summary>
    public event Action<List<NetworkNode>>? StatusUpdated;

    /// <summary>Current monitor interval in seconds.</summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>Returns true if monitoring loop is already running.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Replaces or adds devices to the tracked pool (keyed by MAC).
    /// </summary>
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
                return device;
            });
        }
    }

    /// <summary>
    /// Adds a single device (e.g., manually added by the user).
    /// </summary>
    public void AddDevice(NetworkNode device)
    {
        if (device.MacAddress != "Unknown")
        {
            _trackedDevices.AddOrUpdate(device.MacAddress, device, (_, _) => device);
        }
    }

    /// <summary>
    /// Removes a device from tracking.
    /// </summary>
    public void RemoveDevice(string macAddress)
    {
        _trackedDevices.TryRemove(macAddress, out _);
    }

    /// <summary>
    /// Returns a snapshot of all tracked devices.
    /// </summary>
    public List<NetworkNode> GetAllDevices() => _trackedDevices.Values.ToList();

    /// <summary>
    /// Starts the background monitoring loop. Runs until the token is cancelled.
    /// Performs an initial check immediately before entering the periodic loop.
    /// </summary>
    public async Task StartMonitoringAsync(CancellationToken token)
    {
        if (_isRunning) return;
        _isRunning = true;

        try
        {
            // Initial check immediately on startup (no waiting)
            if (_trackedDevices.Count > 0 && !token.IsCancellationRequested)
            {
                await CheckAllDevicesAsync(token);
            }

            // Periodic monitoring loop
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await CheckAllDevicesAsync(token);
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task CheckAllDevicesAsync(CancellationToken token)
    {
        var devices = _trackedDevices.Values.ToList();
        if (devices.Count == 0) return;

        var tasks = devices.Select(async device =>
        {
            if (token.IsCancellationRequested) return;
            if (device.IpAddress == "0.0.0.0" || string.IsNullOrEmpty(device.IpAddress)) return;

            bool wasOnline = device.IsOnline;
            bool nowOnline = false;
            long latency = -1;

            // Method 1: ARP check (works for phones that block ICMP)
            try
            {
                string mac = await Task.Run(() => ArpResolver.ResolveMacAddress(device.IpAddress));
                if (mac != "Unknown") nowOnline = true;
            }
            catch { }

            // Method 2: ICMP ping for latency
            try
            {
                using var pinger = new Ping();
                var reply = await pinger.SendPingAsync(device.IpAddress, PingTimeoutMs);

                if (reply.Status == IPStatus.Success)
                {
                    nowOnline = true;
                    latency = reply.RoundtripTime;
                }
            }
            catch { }

            // Method 3: TCP connect fallback (for strict firewall devices)
            if (!nowOnline)
            {
                nowOnline = await TryTcpProbeAsync(device.IpAddress);
            }

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

            // Detect state transitions
            if (wasOnline && !device.IsOnline)
            {
                device.WasOnlinePreviously = true;
                DeviceWentOffline?.Invoke(device);
            }
            else if (!wasOnline && device.IsOnline)
            {
                device.WasOnlinePreviously = true;
                DeviceCameOnline?.Invoke(device);
            }
        });

        await Task.WhenAll(tasks);
        StatusUpdated?.Invoke(_trackedDevices.Values.ToList());
    }

    /// <summary>
    /// Tries to connect to a few common TCP ports as a last-resort reachability check.
    /// </summary>
    private static async Task<bool> TryTcpProbeAsync(string ip)
    {
        int[] ports = { 80, 443, 22, 53, 8080 };
        foreach (int port in ports)
        {
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(ip, port);
                if (await Task.WhenAny(connectTask, Task.Delay(300)) == connectTask && tcp.Connected)
                    return true;
            }
            catch { }
        }
        return false;
    }
}
