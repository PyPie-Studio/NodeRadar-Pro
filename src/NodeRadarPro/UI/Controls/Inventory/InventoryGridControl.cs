using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeRadarPro.UI.Controls.Inventory;

/// <summary>
/// Composite list container for Inventory device cards, handling card virtualization, selection mode, and status badges.
/// </summary>
public class InventoryGridControl : ScrollViewer
{
    private readonly StackPanel _deviceListContainer;
    private readonly HashSet<string> _selectedMacs = new();
    private bool _isSelectionMode;
    private string? _selectedMac;

    public event Action<NetworkNode>? DeviceCardClicked;
    public event Action? SelectionChanged;

    internal StackPanel DeviceListContainer => _deviceListContainer;
    internal HashSet<string> SelectedMacs => _selectedMacs;
    internal bool IsSelectionMode => _isSelectionMode;

    public InventoryGridControl()
    {
        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        _deviceListContainer = new StackPanel { Spacing = 4, Margin = new Thickness(8, 0) };
        Content = _deviceListContainer;
    }

    public void SetSelectionMode(bool enabled)
    {
        _isSelectionMode = enabled;
        if (!_isSelectionMode)
        {
            _selectedMacs.Clear();
        }
        SelectionChanged?.Invoke();
    }

    public void SetSelectedMac(string? mac)
    {
        _selectedMac = mac;
    }

    public void SelectAll(IEnumerable<NetworkNode> nodes, bool selectAll)
    {
        _selectedMacs.Clear();
        if (selectAll)
        {
            foreach (var n in nodes)
            {
                _selectedMacs.Add(n.MacAddress);
            }
        }
        SelectionChanged?.Invoke();
    }

    public void RenderCards(IEnumerable<NetworkNode> nodes)
    {
        _deviceListContainer.Children.Clear();
        foreach (var node in nodes)
        {
            _deviceListContainer.Children.Add(BuildDeviceCard(node));
        }
    }

    public void UpdateExistingCards(IEnumerable<NetworkNode> nodes)
    {
        var nodeMap = nodes.ToDictionary(n => n.MacAddress, StringComparer.OrdinalIgnoreCase);
        foreach (var child in _deviceListContainer.Children)
        {
            if (child is Border card && card.Tag is string mac && nodeMap.TryGetValue(mac, out var node))
            {
                UpdateCardUI(card, node);
            }
        }
    }

    public void UpdateCardUI(Border card, NetworkNode node)
    {
        if (card.Child is Grid grid)
        {
            var leftGroup = grid.Children.OfType<StackPanel>().FirstOrDefault(c => Grid.GetColumn(c) == 0);
            if (leftGroup != null && leftGroup.Children.Count > 0)
            {
                var iconIndex = _isSelectionMode ? 1 : 0;
                if (leftGroup.Children.Count > iconIndex && leftGroup.Children[iconIndex] is Border iconBox)
                {
                    iconBox.Child = ThemeTokens.VectorIcon(GetDeviceSvg(node), 18, node.IsOnline ? ThemeTokens.Tertiary : ThemeTokens.OnSurfaceVariant);
                }
            }

            var existingBadge = grid.Children.OfType<Border>().FirstOrDefault(c => Grid.GetColumn(c) == 1);
            var newBadge = node.IsOnline ? ThemeTokens.StatusBadge("Online", true) : (node.IsRegistered ? ThemeTokens.StatusBadge("Offline", false) : null);

            if (existingBadge != null)
            {
                if (newBadge == null)
                {
                    grid.Children.Remove(existingBadge);
                }
                else if (existingBadge.Child is TextBlock etb && newBadge.Child is TextBlock ntb && etb.Text != ntb.Text)
                {
                    grid.Children.Remove(existingBadge);
                    newBadge.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(newBadge, 1);
                    grid.Children.Add(newBadge);
                }
            }
            else if (newBadge != null)
            {
                newBadge.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(newBadge, 1);
                grid.Children.Add(newBadge);
            }
        }
    }

    private Border BuildDeviceCard(NetworkNode node)
    {
        bool isSelected = node.MacAddress == _selectedMac;

        var icon = ThemeTokens.VectorIcon(GetDeviceSvg(node), 18, node.IsOnline ? ThemeTokens.Tertiary : ThemeTokens.OnSurfaceVariant);

        var iconBox = new Border
        {
            Width = 38,
            Height = 38,
            CornerRadius = new CornerRadius(8),
            Background = ThemeTokens.SurfaceContainerLowest,
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            Child = icon
        };

        var nameText = new TextBlock
        {
            Text = node.DisplayName,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeTokens.OnSurface,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontFamily = ThemeTokens.DefaultFont
        };

        var subText = new TextBlock
        {
            Text = GetRichSubtitle(node),
            FontSize = 10,
            Foreground = ThemeTokens.OnSurfaceVariant,
            FontFamily = ThemeTokens.DefaultFont,
            Margin = new Thickness(0, 1, 0, 0)
        };

        var ipText = new TextBlock
        {
            Text = node.IpAddress,
            FontSize = 11,
            Foreground = ThemeTokens.Tertiary,
            FontFamily = ThemeTokens.DefaultFont,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var textCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { nameText, subText, ipText } };
        var leftGroup = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { iconBox, textCol } };

        if (_isSelectionMode)
        {
            var cardCheck = new CheckBox { IsChecked = _selectedMacs.Contains(node.MacAddress), Margin = new Thickness(0, 0, 8, 0), IsHitTestVisible = false };
            leftGroup.Children.Insert(0, cardCheck);
        }

        if (node.ThreatLevel != ThreatLevel.Safe)
        {
            var riskDot = new Border
            {
                Width = 6,
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = node.ThreatLevel == ThreatLevel.Critical ? ThemeTokens.Error : ThemeTokens.Warning,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 0, 0)
            };
            ThemeTokens.SetToolTip(riskDot, $"Security Risk: {node.ThreatLevel}");
            leftGroup.Children.Insert(1, riskDot);
        }

        Border? badge = null;
        if (node.IsOnline) badge = ThemeTokens.StatusBadge("Online", true);
        else if (node.IsRegistered && !node.IsOnline) badge = ThemeTokens.StatusBadge("Offline", false);

        var cardGrid = new Grid();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        if (badge != null) cardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(leftGroup, 0);
        cardGrid.Children.Add(leftGroup);
        if (badge != null)
        {
            badge.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(badge, 1);
            cardGrid.Children.Add(badge);
        }

        var card = new Border
        {
            Tag = node.MacAddress,
            Padding = new Thickness(10, 10),
            CornerRadius = new CornerRadius(10),
            Background = isSelected ? ThemeTokens.SurfaceContainerHigh : Brushes.Transparent,
            BorderBrush = isSelected ? ThemeTokens.GhostBorder20 : Brushes.Transparent,
            BorderThickness = new Thickness(1),
            Child = cardGrid,
            Cursor = ThemeTokens.HandCursor
        };

        if (isSelected)
        {
            card.BoxShadow = new BoxShadows(new BoxShadow { OffsetY = 4, Blur = 20, Color = Color.FromArgb(50, 0, 0, 0) });
        }

        card.PointerEntered += (s, e) => { if (node.MacAddress != _selectedMac) card.Background = ThemeTokens.SurfaceContainerHigh; };
        card.PointerExited += (s, e) => { if (node.MacAddress != _selectedMac) card.Background = Brushes.Transparent; };
        card.PointerPressed += (s, e) =>
        {
            if (_isSelectionMode)
            {
                if (_selectedMacs.Contains(node.MacAddress)) _selectedMacs.Remove(node.MacAddress);
                else _selectedMacs.Add(node.MacAddress);
                SelectionChanged?.Invoke();
            }
            else
            {
                DeviceCardClicked?.Invoke(node);
            }
        };

        return card;
    }

    public static string GetDeviceSvg(NetworkNode node)
    {
        string icon = (node.IconPath ?? "").ToLower();
        if (icon is "phone" or "mobile") return ThemeTokens.SvgPhone;
        if (icon == "tablet") return ThemeTokens.SvgTablet;
        if (icon == "server") return ThemeTokens.SvgServer;
        if (icon == "nas") return ThemeTokens.SvgNas;
        if (icon is "router" or "network") return ThemeTokens.SvgRouter;
        if (icon == "switch") return ThemeTokens.SvgSwitch;
        if (icon == "firewall") return ThemeTokens.SvgFirewall;
        if (icon == "accesspoint") return ThemeTokens.SvgAccessPoint;
        if (icon == "printer") return ThemeTokens.SvgPrinter;
        if (icon == "tv") return ThemeTokens.SvgTv;
        if (icon is "iot" or "light") return ThemeTokens.SvgIot;
        if (icon == "speaker") return ThemeTokens.SvgSpeaker;
        if (icon == "camera") return ThemeTokens.SvgCamera;
        if (icon == "dvr") return ThemeTokens.SvgDvr;
        if (icon == "gamepad") return ThemeTokens.SvgGamepad;
        if (icon is "pc" or "laptop") return ThemeTokens.SvgDesktop;

        string type = node.DeviceType ?? "";
        if (type.Contains("router", StringComparison.OrdinalIgnoreCase) || type.Contains("gateway", StringComparison.OrdinalIgnoreCase) || type.Contains("network", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgRouter;
        if (type.Contains("server", StringComparison.OrdinalIgnoreCase) || type.Contains("nas", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgServer;
        if (type.Contains("phone", StringComparison.OrdinalIgnoreCase) || type.Contains("mobile", StringComparison.OrdinalIgnoreCase) || type.Contains("iphone", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgPhone;
        if (type.Contains("printer", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgPrinter;
        if (type.Contains("tv", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgTv;
        if (type.Contains("iot", StringComparison.OrdinalIgnoreCase) || type.Contains("smart", StringComparison.OrdinalIgnoreCase) || type.Contains("bulb", StringComparison.OrdinalIgnoreCase) || type.Contains("light", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgIot;

        string vendor = node.Vendor ?? "";
        if (vendor.Contains("apple", StringComparison.OrdinalIgnoreCase) || vendor.Contains("samsung", StringComparison.OrdinalIgnoreCase) || vendor.Contains("xiaomi", StringComparison.OrdinalIgnoreCase) || vendor.Contains("google", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgPhone;
        if (vendor.Contains("cisco", StringComparison.OrdinalIgnoreCase) || vendor.Contains("tp-link", StringComparison.OrdinalIgnoreCase) || vendor.Contains("ubiquiti", StringComparison.OrdinalIgnoreCase) || vendor.Contains("netgear", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgRouter;
        if (vendor.Contains("dell", StringComparison.OrdinalIgnoreCase) || vendor.Contains("hp", StringComparison.OrdinalIgnoreCase) || vendor.Contains("intel", StringComparison.OrdinalIgnoreCase) || vendor.Contains("vmware", StringComparison.OrdinalIgnoreCase)) return ThemeTokens.SvgServer;

        return ThemeTokens.SvgDesktop;
    }

    public static string GetRichSubtitle(NetworkNode node)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(node.OsGuess)) parts.Add(node.OsGuess);

        if (!string.IsNullOrEmpty(node.Vendor) && node.Vendor != "Unknown Vendor" && node.Vendor != "Unknown")
            parts.Add(node.Vendor);

        if (!string.IsNullOrEmpty(node.DeviceType) && node.DeviceType != "Generic Device")
            parts.Add(node.DeviceType);

        var uniqueParts = parts.Distinct().ToList();

        if (!string.IsNullOrEmpty(node.ExactModel))
        {
            bool alreadyCovered = uniqueParts.Any(p => node.ExactModel.Contains(p, StringComparison.OrdinalIgnoreCase));
            if (!alreadyCovered) uniqueParts.Add(node.ExactModel);
        }

        if (uniqueParts.Count == 0) return "Generic Network Device";
        return string.Join(" • ", uniqueParts);
    }
}
