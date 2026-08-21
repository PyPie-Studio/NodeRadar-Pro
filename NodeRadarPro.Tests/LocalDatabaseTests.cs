#pragma warning disable SYSLIB0050 // FormatterServices is obsolete
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using LiteDB;
using NodeRadarPro.Data;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

public class LocalDatabaseTests : IDisposable
{
    private readonly MemoryStream _ms;
    private readonly LiteDatabase _liteDb;
    private readonly LocalDatabase _db;

    public LocalDatabaseTests()
    {
        _ms = new MemoryStream();
        _liteDb = new LiteDatabase(_ms);

        // Bypass constructor to avoid hitting the actual disk or triggering DPAPI calls which might fail in CI
        _db = (LocalDatabase)FormatterServices.GetUninitializedObject(typeof(LocalDatabase));

        // Inject the in-memory LiteDatabase instance
        var field = typeof(LocalDatabase).GetField("_db", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(_db, _liteDb);
    }

    public void Dispose()
    {
        _liteDb.Dispose();
        _ms.Dispose();
    }

    // -- DEVICE TESTS --

    [Fact]
    public void MergeWithHistory_NewDevice_IsInserted()
    {
        var node = new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:FF", IpAddress = "192.168.1.10" };
        var (resultNode, isNew) = _db.MergeWithHistory(node);

        Assert.True(isNew);
        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.Single(devices);
        Assert.Equal("AA:BB:CC:DD:EE:FF", devices[0].MacAddress);
    }

    [Fact]
    public void MergeWithHistory_ExistingDevice_IsUpdated()
    {
        // Setup initial device
        var initialNode = new NetworkNode { MacAddress = "11:22:33:44:55:66", IpAddress = "10.0.0.1", CustomName = "Old Name" };
        _db.MergeWithHistory(initialNode);

        // Update device
        var updatedNode = new NetworkNode { MacAddress = "11:22:33:44:55:66", IpAddress = "10.0.0.2" };
        var (resultNode, isNew) = _db.MergeWithHistory(updatedNode);

        Assert.False(isNew);
        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.Single(devices);
        Assert.Equal("10.0.0.2", devices[0].IpAddress);
        Assert.Equal("Old Name", devices[0].CustomName); // Preserves existing user fields
    }

    [Fact]
    public void UpdateRegistration_CreatesOrUpdatesDevice()
    {
        _db.UpdateRegistration("00:11:22:33:44:55", "Test TV", "Living Room", "Loc1", "Smart TV", "ModelX", "icon_tv");

        var registered = _db.GetRegisteredDevices();
        Assert.Single(registered);
        Assert.Equal("Test TV", registered[0].CustomName);
        Assert.True(registered[0].IsRegistered);

        // Update existing
        _db.UpdateRegistration("00:11:22:33:44:55", "Test TV 2", "Notes", "Loc2", "Smart TV", "ModelX", "icon_tv");
        var updated = _db.GetRegisteredDevices();
        Assert.Single(updated);
        Assert.Equal("Test TV 2", updated[0].CustomName);
    }

    [Fact]
    public void DeleteDevice_RemovesFromDatabase()
    {
        var node = new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:FF" };
        _db.MergeWithHistory(node);

        bool deleted = _db.DeleteDevice("AA:BB:CC:DD:EE:FF");
        Assert.True(deleted);

        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.Empty(devices);
    }

    [Fact]
    public void UpdateDeviceAlertPrefs_UpdatesCorrectly()
    {
        var node = new NetworkNode { MacAddress = "AA:BB:CC:DD:EE:FF" };
        _db.MergeWithHistory(node);

        _db.UpdateDeviceAlertPrefs("AA:BB:CC:DD:EE:FF", true, true);

        var devices = _liteDb.GetCollection<NetworkNode>("devices").FindAll().ToList();
        Assert.True(devices[0].AlertOnConnectionLost);
        Assert.True(devices[0].AlertOnHighLatency);
    }

    // -- ALERT TESTS --

    [Fact]
    public void Alerts_InsertAndRetrieve()
    {
        var alert = new AlertEvent { MacAddress = "AA:BB", Message = "Test Alert" };
        _db.InsertAlert(alert);

        var alerts = _db.GetAlerts();
        Assert.Single(alerts);
        Assert.Equal("Test Alert", alerts[0].Message);

        Assert.Equal(1, _db.GetUnresolvedAlertCount());

        _db.ResolveAlert(alerts[0].Id);
        Assert.Equal(0, _db.GetUnresolvedAlertCount());
    }

    [Fact]
    public void Alerts_ResolveAll()
    {
        _db.InsertAlert(new AlertEvent { MacAddress = "AA:BB" });
        _db.InsertAlert(new AlertEvent { MacAddress = "CC:DD" });

        Assert.Equal(2, _db.GetUnresolvedAlertCount());

        _db.ResolveAllAlerts();
        Assert.Equal(0, _db.GetUnresolvedAlertCount());
    }

    // -- LOG TESTS --

    [Fact]
    public void Logs_InsertAndRetrieve()
    {
        _db.InsertLog(new LogEntry { Message = "Test Log 1", Level = LogLevel.Info });
        _db.InsertLog(new LogEntry { Message = "Test Log 2", Level = LogLevel.Error, DeviceMac = "AA:BB" });

        var allLogs = _db.GetLogs();
        Assert.Equal(2, allLogs.Count);

        var errorLogs = _db.GetLogs(levelFilter: LogLevel.Error);
        Assert.Single(errorLogs);
        Assert.Equal("Test Log 2", errorLogs[0].Message);

        var macLogs = _db.GetLogs(deviceMacFilter: "AA:BB");
        Assert.Single(macLogs);
        Assert.Equal("AA:BB", macLogs[0].DeviceMac);
    }

    // -- UPTIME TESTS --

    [Fact]
    public void Uptime_InsertAndRetrieve()
    {
        var snapshot = new UptimeSnapshot { MacAddress = "AA:BB", IsOnline = true, LatencyMs = 15 };
        _db.InsertUptimeSnapshots(new List<UptimeSnapshot> { snapshot });

        var history = _db.GetUptimeHistory("AA:BB");
        Assert.Single(history);
        Assert.True(history[0].IsOnline);
        Assert.Equal(15, history[0].LatencyMs);
    }

    // -- SETTINGS TESTS --

    [Fact]
    public void Settings_SaveAndLoad()
    {
        var settings = new AppSettings { MonitorIntervalSeconds = 120, SelectedInterfaceName = "eth0" };
        _db.SaveSettings(settings);

        var loaded = _db.LoadSettings();
        Assert.Equal(120, loaded.MonitorIntervalSeconds);
        Assert.Equal("eth0", loaded.SelectedInterfaceName);
    }
}
#pragma warning restore SYSLIB0050
