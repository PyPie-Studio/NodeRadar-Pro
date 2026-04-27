using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeRadarPro.UI;

/// <summary>
/// Dashboard page — 2-column layout: Radar (left) + Stats/Pings (right).
/// Matches Main_Form HTML mockup.
/// </summary>
public class DashboardPage : Border
{
    private readonly RadarCanvas _radar;
    private readonly TextBlock _onlineCount;
    private readonly TextBlock _latencyValue;
    private readonly TextBlock _alertCount;
    private readonly StackPanel _pingsList;
    private readonly List<NetworkNode> _nodes;

    public event Action<NetworkNode>? DeviceSelected;
    public event Action? ViewLogsRequested;

    public DashboardPage(List<NetworkNode> nodes)
    {
        _nodes = nodes;
        Background = ThemeTokens.SurfaceContainerLow;

        // ═══════════════════════
        // LEFT: Radar Area
        // ═══════════════════════
        _radar = new RadarCanvas();
        _radar.NodeSelected += n => DeviceSelected?.Invoke(n);
        _radar.NodeRightClicked += n => DeviceSelected?.Invoke(n);

        var radarTitle = ThemeTokens.Headline("Live Network Topography", 22);
        radarTitle.Margin = new Thickness(0, 0, 0, 2);

        var radarSubtitle = ThemeTokens.Body("Real-time node status and latency mapping.", 13);

        // Status legend
        var activeBadge = ThemeTokens.StatusBadge("Active", true);
        var criticalBadge = ThemeTokens.StatusBadge("Critical", false);
        var legend = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Children = { activeBadge, criticalBadge }
        };

        var radarHeader = new Grid();
        radarHeader.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        radarHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var headerLeft = new StackPanel { Children = { radarTitle, radarSubtitle } };
        Grid.SetColumn(headerLeft, 0);
        Grid.SetColumn(legend, 1);
        radarHeader.Children.Add(headerLeft);
        radarHeader.Children.Add(legend);

        var radarPanel = new DockPanel();
        DockPanel.SetDock(radarHeader, Dock.Top);
        radarHeader.Margin = new Thickness(0, 0, 0, 12);
        radarPanel.Children.Add(radarHeader);
        radarPanel.Children.Add(_radar);

        var radarCard = ThemeTokens.Card(radarPanel, ThemeTokens.SurfaceContainerHigh, 24);

        // ═══════════════════════
        // RIGHT: Stats + Pings
        // ═══════════════════════

        // Stat cards
        _onlineCount = new TextBlock
        {
            Text = "0",
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeTokens.OnSurface,
            FontFamily = new FontFamily("Inter")
        };

        _latencyValue = new TextBlock
        {
            Text = "—",
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeTokens.Tertiary,
            FontFamily = new FontFamily("Inter")
        };

        _alertCount = new TextBlock
        {
            Text = "0",
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeTokens.OnSurface,
            FontFamily = new FontFamily("Inter")
        };

        var statGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) }, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) } };

        var devicesCard = MakeStatCard("DEVICES ONLINE", _onlineCount, "🖥");
        var latencyCard = MakeStatCard("AVG. LATENCY", _latencyValue, "⚡");

        var alertContent = new Grid();
        alertContent.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        alertContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var alertLeft = new StackPanel
        {
            Children =
            {
                ThemeTokens.Label("⚠ ACTIVE ALERTS", 10, ThemeTokens.Error),
                _alertCount
            }
        };
        var viewLogsBtn = ThemeTokens.SecondaryButton("View Logs");
        viewLogsBtn.Width = 90;
        viewLogsBtn.Height = 32;
        viewLogsBtn.FontSize = 11;
        viewLogsBtn.Foreground = ThemeTokens.Tertiary;
        viewLogsBtn.VerticalAlignment = VerticalAlignment.Center;
        viewLogsBtn.Click += (s, e) => ViewLogsRequested?.Invoke();

        Grid.SetColumn(alertLeft, 0);
        Grid.SetColumn(viewLogsBtn, 1);
        alertContent.Children.Add(alertLeft);
        alertContent.Children.Add(viewLogsBtn);

        var alertCard = ThemeTokens.Card(alertContent, ThemeTokens.SurfaceContainerHigh);
        alertCard.BorderBrush = new SolidColorBrush(Color.Parse("#FFB4AB"), 0.2);

        Grid.SetColumn(devicesCard, 0);
        Grid.SetColumn(latencyCard, 1);
        Grid.SetRow(alertCard, 1);
        Grid.SetColumnSpan(alertCard, 2);

        statGrid.Children.Add(devicesCard);
        statGrid.Children.Add(latencyCard);
        statGrid.Children.Add(alertCard);

        // Spacer between stat cards
        devicesCard.Margin = new Thickness(0, 0, 6, 8);
        latencyCard.Margin = new Thickness(6, 0, 0, 8);

        // Active Pings panel
        var pingsTitle = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "⇌", FontSize = 16, Foreground = ThemeTokens.Tertiary, VerticalAlignment = VerticalAlignment.Center },
                ThemeTokens.Headline("Active Pings", 16)
            }
        };

        var pingsHeader = new Border
        {
            Padding = new Thickness(16, 12),
            Background = new SolidColorBrush(Color.Parse("#070E1D"), 0.5),
            Child = pingsTitle
        };

        _pingsList = new StackPanel { Spacing = 4, Margin = new Thickness(8) };

        var pingsScroll = new ScrollViewer
        {
            Content = _pingsList,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var pingsDock = new DockPanel();
        DockPanel.SetDock(pingsHeader, Dock.Top);
        pingsDock.Children.Add(pingsHeader);
        pingsDock.Children.Add(pingsScroll);

        var pingsCard = ThemeTokens.Card(pingsDock, ThemeTokens.SurfaceContainerHigh, 0);

        // Right column assembly
        var rightCol = new DockPanel();
        DockPanel.SetDock(statGrid, Dock.Top);
        rightCol.Children.Add(statGrid);
        rightCol.Children.Add(pingsCard);

        // ═══════════════════════
        // ROOT GRID
        // ═══════════════════════
        var rootGrid = new Grid
        {
            Margin = new Thickness(24),
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(2, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star))
            }
        };

        Grid.SetColumn(radarCard, 0);
        Grid.SetColumn(rightCol, 1);
        radarCard.Margin = new Thickness(0, 0, 12, 0);
        rightCol.Margin = new Thickness(12, 0, 0, 0);

        rootGrid.Children.Add(radarCard);
        rootGrid.Children.Add(rightCol);

        Child = rootGrid;
    }

    public void RefreshData()
    {
        int online = _nodes.Count(n => n.IsOnline);
        _onlineCount.Text = online.ToString();

        var onlineNodes = _nodes.Where(n => n.IsOnline && n.PingLatencyMs >= 0).ToList();
        if (onlineNodes.Count > 0)
        {
            long avg = (long)onlineNodes.Average(n => n.PingLatencyMs);
            _latencyValue.Text = $"{avg}ms";
        }
        else
        {
            _latencyValue.Text = "—";
        }

        int alerts = _nodes.Count(n => !n.IsOnline && n.IsRegistered);
        _alertCount.Text = alerts.ToString();

        _radar.UpdateNodes(_nodes);
        RefreshPingsList();
    }

    private void RefreshPingsList()
    {
        _pingsList.Children.Clear();

        var recentNodes = _nodes
            .Where(n => n.IsOnline || n.IsRegistered)
            .OrderByDescending(n => n.IsOnline)
            .ThenByDescending(n => n.LastSeen)
            .Take(20)
            .ToList();

        if (recentNodes.Count == 0)
        {
            _pingsList.Children.Add(ThemeTokens.Body("No active pings yet. Run a scan to discover devices.", 12));
            return;
        }

        foreach (var node in recentNodes)
        {
            _pingsList.Children.Add(MakePingRow(node));
        }
    }

    private Border MakePingRow(NetworkNode node)
    {
        var statusDot = ThemeTokens.StatusDot(node.IsOnline, 8);
        statusDot.VerticalAlignment = VerticalAlignment.Center;

        var nameText = new TextBlock
        {
            Text = node.IpAddress,
            FontSize = 13,
            Foreground = ThemeTokens.OnSurface,
            FontFamily = new FontFamily("Inter"),
            FontWeight = FontWeight.Medium
        };

        var subText = new TextBlock
        {
            Text = node.DisplayName != node.IpAddress ? node.DisplayName : "",
            FontSize = 11,
            Foreground = ThemeTokens.OnSurfaceVariant,
            FontFamily = new FontFamily("Inter")
        };

        var leftInfo = new StackPanel { Spacing = 1, Children = { nameText, subText } };
        var leftGroup = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { statusDot, leftInfo }
        };

        string latencyStr = node.IsOnline && node.PingLatencyMs >= 0
            ? $"{node.PingLatencyMs}ms"
            : (node.IsOnline ? "ARP" : "—");
        var latencyColor = node.PingLatencyMs > 80 ? SolidColorBrush.Parse("#EAB308") : ThemeTokens.Tertiary;

        var latencyText = new TextBlock
        {
            Text = latencyStr,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = node.IsOnline ? (IBrush)latencyColor : ThemeTokens.Error,
            FontFamily = new FontFamily("Inter"),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var timeAgo = new TextBlock
        {
            Text = GetTimeAgo(node.LastSeen),
            FontSize = 9,
            Foreground = ThemeTokens.OnSurfaceVariant,
            FontFamily = new FontFamily("Inter"),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var rightInfo = new StackPanel { Spacing = 1, HorizontalAlignment = HorizontalAlignment.Right, Children = { latencyText, timeAgo } };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(leftGroup, 0);
        Grid.SetColumn(rightInfo, 1);
        grid.Children.Add(leftGroup);
        grid.Children.Add(rightInfo);

        var row = new Border
        {
            Padding = new Thickness(12, 10),
            CornerRadius = new CornerRadius(8),
            Background = ThemeTokens.SurfaceContainerLowest,
            Child = grid,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };

        row.PointerEntered += (s, e) => row.Background = ThemeTokens.SurfaceVariant;
        row.PointerExited += (s, e) => row.Background = ThemeTokens.SurfaceContainerLowest;
        row.PointerPressed += (s, e) => DeviceSelected?.Invoke(node);

        return row;
    }

    private static Border MakeStatCard(string label, TextBlock valueBlock, string icon)
    {
        var iconText = new TextBlock
        {
            Text = icon,
            FontSize = 28,
            Opacity = 0.1,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };

        var labelText = ThemeTokens.Label(label, 10);
        labelText.LetterSpacing = 1.2;
        labelText.Margin = new Thickness(0, 0, 0, 4);

        var content = new StackPanel { Children = { labelText, valueBlock } };
        var panel = new Grid { Children = { content, iconText } };

        return ThemeTokens.Card(panel, ThemeTokens.SurfaceContainerHigh, 16);
    }

    private static string GetTimeAgo(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        if (diff.TotalSeconds < 10) return "Just now";
        if (diff.TotalSeconds < 60) return $"{(int)diff.TotalSeconds}s ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        return $"{(int)diff.TotalHours}h ago";
    }
}
