using LiteDB;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests.Core.Models;

public class AlertEventTests
{
    [Fact]
    public void DefaultValues_AreInitializedCorrectly()
    {
        // Arrange & Act
        var alert = new AlertEvent();

        // Assert
        Assert.NotEqual(ObjectId.Empty, alert.Id);
        Assert.Equal(string.Empty, alert.MacAddress);
        Assert.Equal(string.Empty, alert.DeviceName);
        Assert.Equal(string.Empty, alert.IpAddress);
        Assert.Equal(AlertType.ConnectionLost, alert.AlertType);
        Assert.Equal(string.Empty, alert.Message);
        Assert.True((DateTime.UtcNow - alert.Timestamp).TotalSeconds < 5);
        Assert.False(alert.IsResolved);
        Assert.Null(alert.ResolvedAt);
    }

    [Fact]
    public void PropertySettersAndGetters_RetainValues()
    {
        // Arrange
        var customId = ObjectId.NewObjectId();
        var timestamp = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var resolvedAt = new DateTime(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        var alert = new AlertEvent
        {
            Id = customId,
            MacAddress = "AA:BB:CC:DD:EE:FF",
            DeviceName = "Test-Device",
            IpAddress = "192.168.1.100",
            AlertType = AlertType.NewDeviceDiscovered,
            Message = "Unrecognized device discovered",
            Timestamp = timestamp,
            IsResolved = true,
            ResolvedAt = resolvedAt
        };

        // Act & Assert
        Assert.Equal(customId, alert.Id);
        Assert.Equal("AA:BB:CC:DD:EE:FF", alert.MacAddress);
        Assert.Equal("Test-Device", alert.DeviceName);
        Assert.Equal("192.168.1.100", alert.IpAddress);
        Assert.Equal(AlertType.NewDeviceDiscovered, alert.AlertType);
        Assert.Equal("Unrecognized device discovered", alert.Message);
        Assert.Equal(timestamp, alert.Timestamp);
        Assert.True(alert.IsResolved);
        Assert.Equal(resolvedAt, alert.ResolvedAt);
    }
}
