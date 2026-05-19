using System;
using System.Collections.Generic;
using NodeRadarPro.Core;
using Xunit;

namespace Core.Tests;

public class ConnectivityMonitorTests
{
    [Fact]
    public void UpdateTrackedDevices_AddsNewDevice()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var devices = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "00:11:22:33:44:55", CustomName = "Test Device" }
        };

        // Act
        monitor.UpdateTrackedDevices(devices);

        // Assert
        var allDevices = monitor.GetAllDevices();
        Assert.Single(allDevices);
        Assert.Equal("00:11:22:33:44:55", allDevices[0].MacAddress);
        Assert.Equal("Test Device", allDevices[0].CustomName);
    }

    [Fact]
    public void UpdateTrackedDevices_IgnoresUnknownMacAddress()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var devices = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "Unknown", CustomName = "Test Device" }
        };

        // Act
        monitor.UpdateTrackedDevices(devices);

        // Assert
        var allDevices = monitor.GetAllDevices();
        Assert.Empty(allDevices);
    }

    [Fact]
    public void UpdateTrackedDevices_UpdatesExistingDevice_KeepsOldValuesIfNewEmpty()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var initialDevice = new NetworkNode { MacAddress = "00:11:22:33:44:55", CustomName = "Initial Name", Notes = "Initial Notes", Location = "Initial Location", DeviceName = "Initial Device", DeviceModel = "Initial Model" };
        monitor.AddDevice(initialDevice);

        var updatedDevice = new NetworkNode { MacAddress = "00:11:22:33:44:55", CustomName = "New Name" }; // Other fields are empty string by default

        // Act
        monitor.UpdateTrackedDevices(new List<NetworkNode> { updatedDevice });

        // Assert
        var allDevices = monitor.GetAllDevices();
        Assert.Single(allDevices);
        var actualDevice = allDevices[0];
        Assert.Equal("New Name", actualDevice.CustomName);
        Assert.Equal("Initial Notes", actualDevice.Notes);
        Assert.Equal("Initial Location", actualDevice.Location);
        Assert.Equal("Initial Device", actualDevice.DeviceName);
        Assert.Equal("Initial Model", actualDevice.DeviceModel);
    }

    [Fact]
    public void UpdateTrackedDevices_UpdatesExistingDevice_OverwritesWithNewValues()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var initialDevice = new NetworkNode { MacAddress = "00:11:22:33:44:55", CustomName = "Initial Name", Notes = "Initial Notes" };
        monitor.AddDevice(initialDevice);

        var updatedDevice = new NetworkNode { MacAddress = "00:11:22:33:44:55", CustomName = "New Name", Notes = "New Notes", Location = "New Location", DeviceName = "New Device", DeviceModel = "New Model" };

        // Act
        monitor.UpdateTrackedDevices(new List<NetworkNode> { updatedDevice });

        // Assert
        var allDevices = monitor.GetAllDevices();
        Assert.Single(allDevices);
        var actualDevice = allDevices[0];
        Assert.Equal("New Name", actualDevice.CustomName);
        Assert.Equal("New Notes", actualDevice.Notes);
        Assert.Equal("New Location", actualDevice.Location);
        Assert.Equal("New Device", actualDevice.DeviceName);
        Assert.Equal("New Model", actualDevice.DeviceModel);
    }

    [Fact]
    public void UpdateTrackedDevices_UpdatesMultipleDevices()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        monitor.AddDevice(new NetworkNode { MacAddress = "11:11:11:11:11:11", CustomName = "Old 1" });

        var devices = new List<NetworkNode>
        {
            new NetworkNode { MacAddress = "11:11:11:11:11:11", CustomName = "New 1" },
            new NetworkNode { MacAddress = "22:22:22:22:22:22", CustomName = "Device 2" }
        };

        // Act
        monitor.UpdateTrackedDevices(devices);

        // Assert
        var allDevices = monitor.GetAllDevices();
        Assert.Equal(2, allDevices.Count);
        Assert.Contains(allDevices, d => d.MacAddress == "11:11:11:11:11:11" && d.CustomName == "New 1");
        Assert.Contains(allDevices, d => d.MacAddress == "22:22:22:22:22:22" && d.CustomName == "Device 2");
    }

    [Fact]
    public void UpdateTrackedDevices_IsRegisteredStaysTrue()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var initialDevice = new NetworkNode { MacAddress = "00:11:22:33:44:55", IsRegistered = true };
        monitor.AddDevice(initialDevice);

        var updatedDevice = new NetworkNode { MacAddress = "00:11:22:33:44:55", IsRegistered = false };

        // Act
        monitor.UpdateTrackedDevices(new List<NetworkNode> { updatedDevice });

        // Assert
        var allDevices = monitor.GetAllDevices();
        Assert.True(allDevices[0].IsRegistered);
    }

    [Fact]
    public void UpdateTrackedDevices_FirstSeenAndPingHistoryPreserved()
    {
        // Arrange
        var monitor = new ConnectivityMonitor();
        var firstSeen = new DateTime(2023, 1, 1);
        var pingHistory = new Queue<bool>(new[] { true, false, true });

        var initialDevice = new NetworkNode
        {
            MacAddress = "00:11:22:33:44:55",
            FirstSeen = firstSeen,
            PingHistory = pingHistory
        };
        monitor.AddDevice(initialDevice);

        var updatedDevice = new NetworkNode
        {
            MacAddress = "00:11:22:33:44:55",
            FirstSeen = new DateTime(2024, 1, 1),
            PingHistory = new Queue<bool>()
        };

        // Act
        monitor.UpdateTrackedDevices(new List<NetworkNode> { updatedDevice });

        // Assert
        var allDevices = monitor.GetAllDevices();
        Assert.Equal(firstSeen, allDevices[0].FirstSeen);
        Assert.Equal(pingHistory, allDevices[0].PingHistory);
    }
}
