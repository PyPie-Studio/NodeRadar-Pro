using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using System;
using System.Collections.Generic;

namespace NodeRadarPro.UI;

/// <summary>
/// Scrollable sidebar device list. Each row shows:
/// Status dot (green/red) • Display name • IP • Latency
/// </summary>
public class DeviceListPanel : Border
{
    private static readonly IBrush BgPanel = SolidColorBrush.Parse("#131322");
    private static readonly IBrush BgHover = SolidColorBrush.Parse("#1E1E35");
    private static readonly IBrush BgSelected = SolidColorBrush.Parse("#252545");
    private static readonly IBrush TextWhite = Brushes.WhiteSmoke;
    private static readonly IBrush TextGrey = SolidColorBrush.Parse("#777788");
    private static readonly IBrush OnlineGreen = SolidColorBrush.Parse("#00FFcc");
    private static readonly IBrush OfflineRed = SolidColorBrush.Parse("#FF4444");
    private static readonly IBrush RegisteredPurple = SolidColorBrush.Parse("#8A2BE2");
    private static readonly IBrush SeparatorColor = SolidColorBrush.Parse("#1A1A30");

    private readonly StackPanel _listContainer;
    private readonly ScrollViewer _scrollViewer;
    private readonly TextBlock _emptyLabel;
    private string? _selectedMac = null;

    /// <summary>Fired when a device row is clicked (left click).</summary>
    public event Action<NetworkNode>? DeviceSelected;

    /// <summary>Fired when a device row is right-clicked.</summary>
    public event Action<NetworkNode>? DeviceRightClicked;

    public DeviceListPanel()
    {
        Background = BgPanel;
        CornerRadius = new CornerRadius(0);

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
            Spacing = 1,
            Children = { _emptyLabel }
        };

        _scrollViewer = new ScrollViewer
        {
            Content = _listContainer,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        Child = _scrollViewer;
    }

    /// <summary>
    /// Rebuilds the entire device list from the given nodes.
    /// </summary>
    public void UpdateDevices(List<NetworkNode> nodes)
    {
        _listContainer.Children.Clear();

        if (nodes.Count == 0)
        {
            _listContainer.Children.Add(_emptyLabel);
            return;
        }

        // Sort: online first, then by display name
        nodes.Sort((a, b) =>
        {
            if (a.IsOnline != b.IsOnline) return b.IsOnline.CompareTo(a.IsOnline);
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
        // Re-highlight rows
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

        // Status dot
        var statusDot = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = node.IsOnline ? OnlineGreen : OfflineRed,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };

        // Device name
        var nameText = new TextBlock
        {
            Text = node.DisplayName,
            Foreground = TextWhite,
            FontSize = 13,
            FontWeight = node.IsRegistered ? FontWeight.SemiBold : FontWeight.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 130
        };

        // Registered badge
        var registeredBadge = new TextBlock
        {
            Text = node.IsRegistered ? "★" : "",
            Foreground = RegisteredPurple,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };

        var topRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { statusDot, nameText, registeredBadge }
        };

        // IP + Latency line
        string subText = node.IpAddress;
        if (node.IsOnline && node.PingLatencyMs >= 0)
            subText += $"  •  {node.PingLatencyMs}ms";
        if (!string.IsNullOrEmpty(node.Location))
            subText += $"  •  {node.Location}";

        var ipText = new TextBlock
        {
            Text = subText,
            Foreground = TextGrey,
            FontSize = 11,
            Margin = new Thickness(18, 0, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var content = new StackPanel
        {
            Spacing = 2,
            Children = { topRow, ipText }
        };

        var row = new Border
        {
            Background = isSelected ? BgSelected : Brushes.Transparent,
            Padding = new Thickness(12, 8),
            Tag = node,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };

        row.Child = content;

        // Hover effect
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

        // Click events
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
