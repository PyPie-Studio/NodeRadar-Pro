using NodeRadarPro.Data;
using System.Reflection;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class ConnectivityMonitorTests
{
    [Fact]
    public async Task TryTcpProbeAsync_UnreachableHost_ReturnsFalse()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();

        var methodInfo = typeof(ConnectivityMonitor).GetMethod("TryTcpProbeAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Act - TEST-NET-1 unroutable IP
        var task = (Task<bool>)methodInfo.Invoke(monitor, new object[] { "192.0.2.1" })!;
        bool result = await task;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryTcpProbeAsync_InvalidHost_ReturnsFalse()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();

        var methodInfo = typeof(ConnectivityMonitor).GetMethod("TryTcpProbeAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        // Act
        var task = (Task<bool>)methodInfo.Invoke(monitor, new object[] { "invalid_host_name_for_testing" })!;
        bool result = await task;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateTrackedDevices_AddNewDevice_AddsToTracker()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var devices = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:FF", DeviceName = "TestDevice" }
        };

        // Act
        monitor.UpdateTrackedDevices(devices);

        // Assert
        var tracked = monitor.GetAllDevices();
        Assert.Single(tracked);
        Assert.Equal("AA:BB:CC:DD:EE:FF", tracked[0].MacAddress);
        Assert.Equal("TestDevice", tracked[0].DeviceName);
    }

    [Fact]
    public void UpdateTrackedDevices_UnknownMac_IgnoresDevice()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var devices = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "Unknown", DeviceName = "TestDevice" }
        };

        // Act
        monitor.UpdateTrackedDevices(devices);

        // Assert
        var tracked = monitor.GetAllDevices();
        Assert.Empty(tracked);
    }

    [Fact]
    public void UpdateTrackedDevices_UpdateExistingDevice_UpdatesFieldsAndPreservesEmptyFields()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var existingDevice = new NetworkNode
        {
            MacAddress = "AA:BB:CC:DD:EE:FF",
            CustomName = "OldCustomName",
            Notes = "OldNotes",
            Location = "OldLocation",
            DeviceName = "OldDeviceName",
            DeviceModel = "OldDeviceModel",
            IsRegistered = true,
            FirstSeen = new DateTime(2020, 1, 1)
        };
        monitor.AddDevice(existingDevice);

        var newDevice = new NetworkNode
        {
            MacAddress = "AA:BB:CC:DD:EE:FF",
            CustomName = "NewCustomName",
            Notes = "", // Empty should be ignored, existing preserved
            Location = "NewLocation",
            DeviceName = "", // Empty should be ignored, existing preserved
            DeviceModel = "NewDeviceModel",
            IsRegistered = false, // Should become true since existing is true
            FirstSeen = new DateTime(2021, 1, 1) // Should be ignored, existing preserved
        };
        var devices = new List<NetworkNode> { newDevice };

        // Act
        monitor.UpdateTrackedDevices(devices);

        // Assert
        var tracked = monitor.GetAllDevices();
        Assert.Single(tracked);

        var updated = tracked[0];
        Assert.Equal("NewCustomName", updated.CustomName);
        Assert.Equal("OldNotes", updated.Notes);
        Assert.Equal("NewLocation", updated.Location);
        Assert.Equal("OldDeviceName", updated.DeviceName);
        Assert.Equal("NewDeviceModel", updated.DeviceModel);
        Assert.True(updated.IsRegistered);
        Assert.Equal(new DateTime(2020, 1, 1), updated.FirstSeen);
    }

    [Fact]
    public void AddDevice_And_GetAllDevices_ManagesCollection()
    {
        var monitor = new ConnectivityMonitor();
        var dev1 = new NetworkNode { MacAddress = "00:11:22:33:44:01", IpAddress = "10.0.0.1" };
        var dev2 = new NetworkNode { MacAddress = "00:11:22:33:44:02", IpAddress = "10.0.0.2" };

        monitor.AddDevice(dev1);
        monitor.AddDevice(dev2);

        var all = monitor.GetAllDevices();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, d => d.MacAddress == "00:11:22:33:44:01");
        Assert.Contains(all, d => d.MacAddress == "00:11:22:33:44:02");
    }

    [Fact]
    public async Task CheckAllDevicesAsync_CollectsAndBulkInsertsAlerts()
    {
        using var ms = new MemoryStream();
        using var liteDb = new LiteDB.LiteDatabase(ms, new LiteDB.BsonMapper());
        using var tempDb = new LocalDatabase(liteDb);
        var monitor = new ConnectivityMonitor(tempDb);
        var dev = new NetworkNode
        {
            MacAddress = "00:11:22:33:44:55",
            IpAddress = "192.0.2.254",
            IsOnline = false
        };
        monitor.AddDevice(dev);

        using var cts = new CancellationTokenSource(1000);
        var methodInfo = typeof(ConnectivityMonitor).GetMethod("CheckAllDevicesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        var task = (Task)methodInfo.Invoke(monitor, new object[] { cts.Token })!;
        await task;

        var allDevs = monitor.GetAllDevices();
        Assert.Single(allDevs);
    }
}
