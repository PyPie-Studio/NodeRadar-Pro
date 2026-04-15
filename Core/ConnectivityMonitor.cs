using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

/// <summary>
/// Background service that periodically pings all tracked devices 
/// and fires events when connectivity status changes.
/// </summary>
public class ConnectivityMonitor
{
    private const int PingTimeoutMs = 2000;
    
    private readonly ConcurrentDictionary<string, NetworkNode> _trackedDevices = new();
    
    /// <summary>Fired when a previously-online device stops responding.</summary>
    public event Action<NetworkNode>? DeviceWentOffline;
    
    /// <summary>Fired when a previously-offline device starts responding.</summary>
    public event Action<NetworkNode>? DeviceCameOnline;
    
    /// <summary>Fired after each complete ping cycle with the full device list.</summary>
    public event Action<List<NetworkNode>>? StatusUpdated;

    /// <summary>Current monitor interval in seconds.</summary>
    public int IntervalSeconds { get; set; } = 60;

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
                // Preserve user-edited fields from the existing entry
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
    /// Returns a snapshot of all tracked devices.
    /// </summary>
    public List<NetworkNode> GetAllDevices() => _trackedDevices.Values.ToList();

    /// <summary>
    /// Starts the background monitoring loop. Runs until the token is cancelled.
    /// </summary>
    public async Task StartMonitoringAsync(CancellationToken token)
    {
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

            await PingAllDevicesAsync(token);
        }
    }

    private async Task PingAllDevicesAsync(CancellationToken token)
    {
        var devices = _trackedDevices.Values.ToList();
        if (devices.Count == 0) return;

        var tasks = devices.Select(async device =>
        {
            if (token.IsCancellationRequested) return;

            bool wasOnline = device.IsOnline;

            try
            {
                using var pinger = new Ping();
                var reply = await pinger.SendPingAsync(device.IpAddress, PingTimeoutMs);

                device.IsOnline = reply.Status == IPStatus.Success;
                device.PingLatencyMs = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                
                if (device.IsOnline)
                {
                    device.LastSeen = DateTime.UtcNow;
                }
            }
            catch
            {
                device.IsOnline = false;
                device.PingLatencyMs = -1;
            }

            // Detect state transitions
            if (wasOnline && !device.IsOnline)
            {
                device.WasOnlinePreviously = true;
                DeviceWentOffline?.Invoke(device);
            }
            else if (!wasOnline && device.IsOnline && device.WasOnlinePreviously)
            {
                DeviceCameOnline?.Invoke(device);
            }
        });

        await Task.WhenAll(tasks);
        StatusUpdated?.Invoke(_trackedDevices.Values.ToList());
    }
}
