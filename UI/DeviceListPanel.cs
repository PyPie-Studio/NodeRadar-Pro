using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using System;
using System.Collections.Generic;

namespace NodeRadarPro.UI;

/// <summary>
/// Scrollable sidebar device list with status indicators, vendor info, and device type.
/// Each row shows: Status dot • Display name • Vendor/Type • IP • Latency
/// </summary>
public class DeviceListPanel : Border
{
    private static readonly IBrush BgPanel = SolidColorBrush.Parse("#111120");
    private static readonly IBrush BgHover = SolidColorBrush.Parse("#1C1C35");
    private static readonly IBrush BgSelected = SolidColorBrush.Parse("#252548");
    private static readonly IBrush TextWhite = Brushes.WhiteSmoke;
    private static readonly IBrush TextGrey = SolidColorBrush.Parse("#6E6E82");
    private static readonly IBrush TextSubtle = SolidColorBrush.Parse("#555570");
    private static readonly IBrush OnlineGreen = SolidColorBrush.Parse("#00FFcc");
    private static readonly IBrush OfflineRed = SolidColorBrush.Parse("#FF4444");
    private static readonly IBrush RegisteredPurple = SolidColorBrush.Parse("#8A2BE2");

    private readonly StackPanel _listContainer;
    private readonly TextBlock _emptyLabel;
    private string? _selectedMac = null;

    public event Action<NetworkNode>? DeviceSelected;
    public event Action<NetworkNode>? DeviceRightClicked;

    public DeviceListPanel()
    {
        Background = BgPanel;

        _emptyLabel = new TextBlock
        {
            Text = "No devices discovered yet.\nClick SCAN NETWORK to begin.",
            Foreground = TextGrey,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(20, 40)
        };

        _listContainer = new StackPanel
        {
            Spacing = 0,
            Children = { _emptyLabel }
        };

        Child = new ScrollViewer
        {
            Content = _listContainer,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    public void UpdateDevices(List<NetworkNode> nodes)
    {
        _listContainer.Children.Clear();

        if (nodes.Count == 0)
        {
            _listContainer.Children.Add(_emptyLabel);
            return;
        }

        // Sort: online first, registered first within each group, then by display name
        nodes.Sort((a, b) =>
        {
            if (a.IsOnline != b.IsOnline) return b.IsOnline.CompareTo(a.IsOnline);
            if (a.IsRegistered != b.IsRegistered) return b.IsRegistered.CompareTo(a.IsRegistered);
            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var node in nodes)
        {
            _listContainer.Children.Add(BuildDeviceRow(node));
        }
    }

    public void SelectDevice(string? macAddress)
    {
        _selectedMac = macAddress;
        foreach (var child in _listContainer.Children)
        {
            if (child is Border border && border.Tag is NetworkNode rowNode)
            {
                border.Background = rowNode.MacAddress == _selectedMac ? BgSelected : Brushes.Transparent;
            }
        }
    }

    private Border BuildDeviceRow(NetworkNode node)
    {
        bool isSelected = node.MacAddress == _selectedMac;

        // ── Status indicator (pulsing dot) ──
        var statusDot = new Border
        {
            Width = 10,
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = node.IsOnline ? OnlineGreen : OfflineRed,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 10, 0),
            Opacity = node.IsOnline ? 1.0 : 0.6,
            // Glow effect for online
            BoxShadow = node.IsOnline 
                ? new BoxShadows(new BoxShadow { Blur = 6, Color = Color.Parse("#00FFcc") })
                : default
        };

        // ── Device name ──
        var nameText = new TextBlock
        {
            Text = node.DisplayName,
            Foreground = TextWhite,
            FontSize = 13,
            FontWeight = node.IsRegistered ? FontWeight.SemiBold : FontWeight.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 160
        };

        // ── Registered badge ──
        var registeredBadge = new TextBlock
        {
            Text = node.IsRegistered ? " ★" : "",
            Foreground = RegisteredPurple,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };

        var nameRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { nameText, registeredBadge }
        };

        // ── Subtitle: device type / vendor / model ──
        string subtitle = node.SubtitleText;

        var subtitleText = new TextBlock
        {
            Text = subtitle,
            Foreground = SolidColorBrush.Parse("#7B6FA0"),
            FontSize = 10.5,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 1, 0, 0),
            IsVisible = !string.IsNullOrEmpty(subtitle)
        };

        // ── IP + Latency + Uptime + Location line ──
        string infoLine = node.IpAddress;
        if (node.IsOnline && node.PingLatencyMs >= 0)
            infoLine += $"  •  {node.PingLatencyMs}ms";
        
        infoLine += $"  •  {node.UptimeDisplay}";

        if (!string.IsNullOrEmpty(node.Location))
            infoLine += $"  •  📍 {node.Location}";

        var infoText = new TextBlock
        {
            Text = infoLine,
            Foreground = TextSubtle,
            FontSize = 10.5,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 1, 0, 0)
        };

        // ── Right-side content ──
        var textContent = new StackPanel
        {
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { nameRow, subtitleText, infoText }
        };

        var rowContent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { statusDot, textContent }
        };

        var row = new Border
        {
            Background = isSelected ? BgSelected : Brushes.Transparent,
            Padding = new Thickness(14, 9, 14, 9),
            Tag = node,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            BorderBrush = SolidColorBrush.Parse("#1A1A30"),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        row.Child = rowContent;

        // ── Hover effect ──
        row.PointerEntered += (s, e) =>
        {
            if (row.Tag is NetworkNode n && n.MacAddress != _selectedMac)
                row.Background = BgHover;
        };
        row.PointerExited += (s, e) =>
        {
            if (row.Tag is NetworkNode n && n.MacAddress != _selectedMac)
                row.Background = Brushes.Transparent;
        };

        // ── Click events ──
        row.PointerPressed += (s, e) =>
        {
            if (row.Tag is not NetworkNode n) return;

            if (e.GetCurrentPoint(row).Properties.IsRightButtonPressed)
            {
                DeviceRightClicked?.Invoke(n);
            }
            else
            {
                _selectedMac = n.MacAddress;
                SelectDevice(_selectedMac);
                DeviceSelected?.Invoke(n);
            }
        };

        return row;
    }
}
