using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using NodeRadarPro.Core;

namespace NodeRadarPro.Data;

/// <summary>
/// Handles offline history and device registration using LiteDB.
/// </summary>
public class LocalDatabase
{
    private readonly string _dbPath;

    public LocalDatabase()
    {
        // Store in the app's local directory (cross-platform safe)
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string myFolder = Path.Combine(appDataFolder, "PyPieStudio", "NodeRadarPro");
        
        Directory.CreateDirectory(myFolder); // Ensure folder exists
        _dbPath = Path.Combine(myFolder, "history.db");
    }

    /// <summary>
    /// Upserts a discovered node based on its MAC address. 
    /// If we saw this MAC before, we remember its custom name/icon/notes/location/model.
    /// </summary>
    public NetworkNode MergeWithHistory(NetworkNode scannedNode)
    {
        // Don't merge devices without MACs
        if (scannedNode.MacAddress == "Unknown") return scannedNode;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<NetworkNode>("devices");

        // Primary key lookup
        var existing = collection.FindOne(x => x.MacAddress == scannedNode.MacAddress);

        if (existing != null)
        {
            // We know this device! Apply historical customizations.
            // User-saved fields always come from DB (they persist across IP changes)
            scannedNode.CustomName = existing.CustomName;
            scannedNode.Notes = existing.Notes;
            scannedNode.Location = existing.Location;
            scannedNode.DeviceName = existing.DeviceName;
            scannedNode.DeviceModel = existing.DeviceModel;
            scannedNode.IconPath = existing.IconPath;
            scannedNode.IsRegistered = existing.IsRegistered;
            scannedNode.FirstSeen = existing.FirstSeen;

            // Vendor from DB if empty on scan
            if (string.IsNullOrEmpty(scannedNode.Vendor) || scannedNode.Vendor == "Unknown Vendor")
            {
                scannedNode.Vendor = existing.Vendor;
            }

            // Update its latest IP and online status in the database
            existing.IpAddress = scannedNode.IpAddress;
            existing.IsOnline = true;
            existing.PingLatencyMs = scannedNode.PingLatencyMs;
            existing.LastSeen = DateTime.UtcNow;
            existing.Hostname = scannedNode.Hostname;
            if (!string.IsNullOrEmpty(scannedNode.Vendor) && scannedNode.Vendor != "Unknown Vendor")
            {
                existing.Vendor = scannedNode.Vendor;
            }
            
            collection.Update(existing);
        }
        else
        {
            // Brand new device
            scannedNode.FirstSeen = DateTime.UtcNow;
            scannedNode.LastSeen = DateTime.UtcNow;
            collection.Insert(scannedNode);
            collection.EnsureIndex(x => x.MacAddress);
        }

        return scannedNode;
    }

    /// <summary>
    /// Updates user registration fields for a device (by MAC address).
    /// These fields persist even if the device's IP changes via DHCP.
    /// </summary>
    public void UpdateRegistration(string macAddress, string customName, string notes, 
        string location, string deviceName, string deviceModel, string icon)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<NetworkNode>("devices");

        var existing = collection.FindOne(x => x.MacAddress == macAddress);
        if (existing != null)
        {
            existing.CustomName = customName;
            existing.Notes = notes;
            existing.Location = location;
            existing.DeviceName = deviceName;
            existing.DeviceModel = deviceModel;
            existing.IconPath = icon;
            existing.IsRegistered = true;
            collection.Update(existing);
        }
    }

    /// <summary>
    /// Updates online status and latency for a device (used by ConnectivityMonitor).
    /// </summary>
    public void UpdateOnlineStatus(string macAddress, bool isOnline, long latencyMs)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<NetworkNode>("devices");

        var existing = collection.FindOne(x => x.MacAddress == macAddress);
        if (existing != null)
        {
            existing.IsOnline = isOnline;
            existing.PingLatencyMs = latencyMs;
            if (isOnline) existing.LastSeen = DateTime.UtcNow;
            collection.Update(existing);
        }
    }

    /// <summary>
    /// Inserts a manually-added device into the database.
    /// </summary>
    public void InsertManualDevice(NetworkNode device)
    {
        if (device.MacAddress == "Unknown") return;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<NetworkNode>("devices");

        var existing = collection.FindOne(x => x.MacAddress == device.MacAddress);
        if (existing == null)
        {
            device.IsRegistered = true;
            device.FirstSeen = DateTime.UtcNow;
            device.LastSeen = DateTime.UtcNow;
            collection.Insert(device);
            collection.EnsureIndex(x => x.MacAddress);
        }
    }

    public List<NetworkNode> GetAllKnownDevices()
    {
        using var db = new LiteDatabase(_dbPath);
        return new List<NetworkNode>(db.GetCollection<NetworkNode>("devices").FindAll());
    }

    public List<NetworkNode> GetRegisteredDevices()
    {
        using var db = new LiteDatabase(_dbPath);
        return db.GetCollection<NetworkNode>("devices")
            .Find(x => x.IsRegistered).ToList();
    }

    // ── Settings ──

    public AppSettings LoadSettings()
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<AppSettings>("settings");
        return collection.FindById(1) ?? new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<AppSettings>("settings");
        collection.Upsert(settings);
    }
}