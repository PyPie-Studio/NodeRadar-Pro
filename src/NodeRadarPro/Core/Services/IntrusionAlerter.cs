using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;

namespace NodeRadarPro.Core;

/// <summary>
/// Cross-platform desktop notifications for intrusion alerts and connectivity changes.
/// </summary>
public static class IntrusionAlerter
{
    private static INotificationManager? _notificationManager;
    private static bool _isTestMode;

    /// <summary>S4: Gate toast notifications behind settings.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Task 4: Global settings for email alerts.</summary>
    public static AppSettings? Settings { get; set; }

    public static void Initialize(Window mainWindow)
    {
        // Use Avalonia's built-in notification manager
        _notificationManager = new WindowNotificationManager(mainWindow)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3
        };
        _isTestMode = false;
    }

    public static void SetNotificationManagerForTesting(INotificationManager? manager)
    {
        _notificationManager = manager;
        _isTestMode = manager != null;
    }

    private static void ShowNotification(Notification notification)
    {
        if (_notificationManager == null) return;

        if (_isTestMode)
        {
            _notificationManager.Show(notification);
            return;
        }

        try
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                _notificationManager.Show(notification);
            }
            else
            {
                Dispatcher.UIThread.Post(() => _notificationManager?.Show(notification));
            }
        }
        catch
        {
            _notificationManager.Show(notification);
        }
    }

    public static void AlertDeviceOffline(NetworkNode node)
    {
        if (!Enabled || _notificationManager == null) return;
        AudioService.PlayAlert(true);
        ShowNotification(new Notification(
            "Device Disconnected",
            $"{node.DisplayName} ({node.IpAddress}) is no longer responding.",
            NotificationType.Error,
            TimeSpan.FromSeconds(8)));

        if (Settings != null)
        {
            _ = EmailService.SendAlertAsync(Settings,
                $"[NodeRadar Alert] Device Offline: {node.DisplayName}",
                $"Device {node.DisplayName} ({node.IpAddress}, MAC: {node.MacAddress}) is no longer responding.");
        }
    }

    public static void AlertDeviceReconnected(NetworkNode node)
    {
        if (!Enabled || _notificationManager == null) return;
        AudioService.PlayAlert(false);
        ShowNotification(new Notification(
            "Device Reconnected",
            $"{node.DisplayName} ({node.IpAddress}) is back online.",
            NotificationType.Success,
            TimeSpan.FromSeconds(5)));

        if (Settings != null)
        {
            _ = EmailService.SendAlertAsync(Settings,
                $"[NodeRadar Alert] Device Online: {node.DisplayName}",
                $"Device {node.DisplayName} ({node.IpAddress}, MAC: {node.MacAddress}) has reconnected.");
        }
    }

    public static void AlertNewDevice(NetworkNode node)
    {
        if (!Enabled || _notificationManager == null) return;
        AudioService.PlayAlert(false);
        ShowNotification(new Notification(
            "New Device Discovered",
            $"{node.DisplayName} ({node.IpAddress}) has appeared on the network.",
            NotificationType.Information,
            TimeSpan.FromSeconds(10)));

        if (Settings != null)
        {
            _ = EmailService.SendAlertAsync(Settings,
                $"[NodeRadar Alert] New Device Discovered: {node.DisplayName}",
                $"A new device {node.DisplayName} ({node.IpAddress}, MAC: {node.MacAddress}) has appeared on the network.");
        }
    }
}
