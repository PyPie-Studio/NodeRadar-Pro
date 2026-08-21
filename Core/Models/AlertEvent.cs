using LiteDB;
using System;

namespace NodeRadarPro.Core;

/// <summary>
/// Represents a triggered alert event persisted in the database.
/// </summary>
public class AlertEvent
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    public string MacAddress { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public AlertType AlertType { get; set; } = AlertType.ConnectionLost;
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
}

public enum AlertType
{
    ConnectionLost,
    HighLatency,
    PacketLoss,
    DeviceReconnected,
    NewDeviceDiscovered
}


public static class AlertTypeExtensions
{
    public static string GetIcon(this AlertType alertType)
    {
        return alertType switch { AlertType.ConnectionLost => "🔴", AlertType.HighLatency => "🟡", AlertType.PacketLoss => "🟠", AlertType.DeviceReconnected => "🟢", AlertType.NewDeviceDiscovered => "🔵", _ => "⚪" };
    }

    public static string GetDisplayName(this AlertType alertType)
    {
        return alertType switch { AlertType.ConnectionLost => "Connection Lost", AlertType.HighLatency => "High Latency", AlertType.PacketLoss => "Packet Loss", AlertType.DeviceReconnected => "Reconnected", AlertType.NewDeviceDiscovered => "New Device", _ => "Alert" };
    }
}
