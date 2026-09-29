using NodeRadarPro.Core;

namespace NodeRadarPro.Tests.Core.Models;

public class AlertTypeExtensionsTests
{

    [Theory]
    [InlineData(AlertType.ConnectionLost, "Connection Lost")]
    [InlineData(AlertType.HighLatency, "High Latency")]
    [InlineData(AlertType.PacketLoss, "Packet Loss")]
    [InlineData(AlertType.DeviceReconnected, "Reconnected")]
    [InlineData(AlertType.NewDeviceDiscovered, "New Device")]
    [InlineData((AlertType)999, "Alert")]
    public void GetDisplayName_ReturnsExpectedDisplayName(AlertType alertType, string expectedDisplayName)
    {
        var displayName = alertType.GetDisplayName();
        Assert.Equal(expectedDisplayName, displayName);
    }
}
