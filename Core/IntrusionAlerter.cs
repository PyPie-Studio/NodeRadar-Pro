using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace NodeRadarPro.Core;

/// <summary>
/// Cross-platform desktop notifications for intrusion alerts and connectivity changes.
/// </summary>
public static class IntrusionAlerter
{
    private static INotificationManager? _notificationManager;

    public static void Initialize(Window mainWindow)
    {
        // Use Avalonia's built-in notification manager
        _notificationManager = new WindowNotificationManager(mainWindow)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3
        };
    }

    public static void AlertNewDevice(NetworkNode node)
    {
        if (_notificationManager == null) return;

        string message = $"A new device joined the network:\n" +
                         $"IP: {node.IpAddress}\n" +
                         $"MAC: {node.MacAddress}\n" +
                         $"Vendor: {node.Vendor}";

        _notificationManager.Show(
            new Notification(
                "🚨 New Device Detected",
                message,
                NotificationType.Warning,
                TimeSpan.FromSeconds(10)));
    }

    public static void AlertDeviceOffline(NetworkNode node)
    {
        _notificationManager?.Show(new Notification(
            "⚠️ Device Disconnected",
            $"{node.DisplayName} ({node.IpAddress}) is no longer responding.",
            NotificationType.Error,
            TimeSpan.FromSeconds(8)));
    }

    public static void AlertDeviceReconnected(NetworkNode node)
    {
        _notificationManager?.Show(new Notification(
            "✅ Device Reconnected",
            $"{node.DisplayName} ({node.IpAddress}) is back online.",
            NotificationType.Success,
            TimeSpan.FromSeconds(5)));
    }
}