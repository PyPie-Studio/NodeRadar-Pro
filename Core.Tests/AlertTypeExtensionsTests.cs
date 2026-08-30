using NodeRadarPro.Core;
using Xunit;

namespace Core.Tests;

public class AlertTypeExtensionsTests
{
    [Theory]
    [InlineData(AlertType.ConnectionLost, "🔴")]
    [InlineData(AlertType.HighLatency, "🟡")]
    [InlineData(AlertType.PacketLoss, "🟠")]
    [InlineData(AlertType.DeviceReconnected, "🟢")]
    [InlineData(AlertType.NewDeviceDiscovered, "🔵")]
    [InlineData((AlertType)999, "⚪")]
    public void GetIcon_ReturnsExpectedIcon(AlertType alertType, string expectedIcon)
    {
        var icon = alertType.GetIcon();
        Assert.Equal(expectedIcon, icon);
    }

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
