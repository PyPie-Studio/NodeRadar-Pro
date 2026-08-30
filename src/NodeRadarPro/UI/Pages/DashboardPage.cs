using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeRadarPro.UI;

/// <summary>
/// Modern adaptive Bento Grid Dashboard with health gauges and glassmorphism.
/// </summary>
public class DashboardPage : Border
{
    private readonly RadarCanvas _radar;
    private readonly TextBlock _onlineCount;
    private readonly TextBlock _latencyValue;
    private readonly TextBlock _alertCount;
    private readonly StackPanel _pingsList;
    private List<NetworkNode> _nodes;

    private readonly Border _healthGauge;
    private readonly TextBlock _healthStatusText;
    private readonly TextBlock _healthPercent;

    public event Action<NetworkNode>? DeviceSelected;
    public event Action? ViewLogsRequested;

    public DashboardPage(List<NetworkNode> nodes)
    {
        _nodes = nodes;
        Background = ThemeTokens.Surface;

        // Subscribe to updates
        EventAggregator.Instance.Subscribe<NodesUpdatedMessage>(msg =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _nodes = msg.Nodes.ToList();
                RefreshData();
            });
        });
        // ── Health Gauge (Circular progress) ──
        _healthPercent = new TextBlock { Text = "100%", FontSize = 24, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.OnSurface, HorizontalAlignment = HorizontalAlignment.Center };
        _healthStatusText = new TextBlock { Text = "STABLE", FontSize = 10, FontWeight = FontWeight.Black, LetterSpacing = 1.2, Foreground = ThemeTokens.Tertiary, HorizontalAlignment = HorizontalAlignment.Center };

        var gaugeContent = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { _healthPercent, _healthStatusText } };
        _healthGauge = new Border
        {
            Width = 100,
            Height = 100,
            CornerRadius = new CornerRadius(50),
            BorderThickness = new Thickness(6),
            BorderBrush = ThemeTokens.Tertiary,
            Background = ThemeTokens.GlassSurface,
            Child = gaugeContent
        };

        var healthCard = ThemeTokens.GlassCard(new StackPanel
        {
            Spacing = 12,
            Children =
            {
                ThemeTokens.Label("NETWORK HEALTH", 10),
                _healthGauge
            }
        });
        ThemeTokens.SetToolTip(healthCard, "Calculated system stability based on uptime vs alerts.");

        // ── Radar Card (Large) ──
        _radar = new RadarCanvas();
        _radar.NodeSelected += n => DeviceSelected?.Invoke(n);
        _radar.NodeRightClicked += n => DeviceSelected?.Invoke(n);

        var radarHeader = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(GridLength.Auto) }, Margin = new Thickness(0, 0, 0, 16) };
        var radarTitle = new StackPanel { Children = { ThemeTokens.Headline("Active Sonar", 22), ThemeTokens.Body("Live topology mapping", 12) } };
        var legend = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { ThemeTokens.StatusBadge("Online", true), ThemeTokens.StatusBadge("Alert", false) } };
        Grid.SetColumn(radarTitle, 0); Grid.SetColumn(legend, 1);
        radarHeader.Children.Add(radarTitle); radarHeader.Children.Add(legend);

        var radarDock = new DockPanel { Children = { radarHeader, _radar } };
        DockPanel.SetDock(radarHeader, Dock.Top);
        var radarCard = ThemeTokens.Card(radarDock, ThemeTokens.SurfaceContainerHigh, 24);

        // ── Stats ──
        _onlineCount = new TextBlock { Text = "0", FontSize = 36, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.OnSurface };
        _latencyValue = new TextBlock { Text = "—", FontSize = 36, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Tertiary };
        _alertCount = new TextBlock { Text = "0", FontSize = 36, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Error };

        var devicesCard = MakeBentoStat("DEVICES ONLINE", _onlineCount, ThemeTokens.SvgDesktop, "Count of active hosts reachable on the subnet.");
        var latencyCard = MakeBentoStat("AVG. LATENCY", _latencyValue, ThemeTokens.SvgBolt, "Geometric mean of network response times.");

        var alertHeader = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(GridLength.Auto) } };
        var alertTitle = ThemeTokens.Label("ACTIVE ALERTS", 10, ThemeTokens.Error);
        var logBtn = ThemeTokens.TertiaryButton("LOGS →");
        logBtn.FontSize = 11; logBtn.Click += (s, e) => ViewLogsRequested?.Invoke();
        Grid.SetColumn(alertTitle, 0); Grid.SetColumn(logBtn, 1);
        alertHeader.Children.Add(alertTitle); alertHeader.Children.Add(logBtn);
        var alertCard = ThemeTokens.Card(new StackPanel { Children = { alertHeader, _alertCount } }, ThemeTokens.SurfaceContainerHigh, 20);
        alertCard.BorderBrush = ThemeTokens.ErrorBorderFaint;

        // ── Real-time Pings ──
        var pingHeader = new Border { Padding = new Thickness(0, 0, 0, 12), BorderBrush = ThemeTokens.GhostBorder, BorderThickness = new Thickness(0, 0, 0, 1), Child = ThemeTokens.Label("REAL-TIME TRAFFIC", 10) };
        _pingsList = new StackPanel { Spacing = 2 };
        var pingsScroll = new ScrollViewer { Content = _pingsList, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        var pingsCard = ThemeTokens.Card(new DockPanel { Children = { pingHeader, pingsScroll } }, ThemeTokens.SurfaceContainerHigh, 16);
        DockPanel.SetDock(pingHeader, Dock.Top);

        // ═══════════════════════
        // BENTO GRID ASSEMBLY
        // ═══════════════════════
        var bentoGrid = new Grid();
        bentoGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star))); // Col 0
        bentoGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.2, GridUnitType.Star))); // Col 1
        bentoGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star))); // Col 2

        bentoGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Row 0
        bentoGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star))); // Row 1

        // Row 0
        Grid.SetColumn(healthCard, 0); bentoGrid.Children.Add(healthCard);
        Grid.SetColumn(devicesCard, 1); bentoGrid.Children.Add(devicesCard);
        Grid.SetColumn(latencyCard, 2); bentoGrid.Children.Add(latencyCard);

        healthCard.Margin = new Thickness(0, 0, 10, 10);
        devicesCard.Margin = new Thickness(10, 0, 10, 10);
        latencyCard.Margin = new Thickness(10, 0, 0, 10);

        // Row 1
        Grid.SetRow(radarCard, 1); Grid.SetColumnSpan(radarCard, 2);
        bentoGrid.Children.Add(radarCard);
        radarCard.Margin = new Thickness(0, 10, 10, 0);

        var rightColumn = new StackPanel { Spacing = 20, Children = { alertCard, pingsCard } };
        Grid.SetRow(rightColumn, 1); Grid.SetColumn(rightColumn, 2);
        bentoGrid.Children.Add(rightColumn);
        rightColumn.Margin = new Thickness(10, 10, 0, 0);

        Child = new Border { Margin = new Thickness(32), Child = bentoGrid };
    }

    public void RefreshData()
    {
        int totalTracked = _nodes.Count;
        int online = _nodes.Count(n => n.IsOnline);
        _onlineCount.Text = online.ToString();

        var onlineNodes = _nodes.Where(n => n.IsOnline && n.PingLatencyMs >= 0).ToList();
        long avg = 0;
        if (onlineNodes.Count > 0)
        {
            avg = (long)onlineNodes.Average(n => n.PingLatencyMs);
            _latencyValue.Text = $"{avg}ms";
        }
        else _latencyValue.Text = "—";

        int alerts = _nodes.Count(n => !n.IsOnline && n.IsRegistered);
        _alertCount.Text = alerts.ToString();

        // Update Health Gauge
        double health = totalTracked > 0 ? (1.0 - ((double)alerts / totalTracked)) * 100 : 100;
        _healthPercent.Text = $"{(int)health}%";
        if (health > 90) { _healthStatusText.Text = "STABLE"; _healthStatusText.Foreground = ThemeTokens.Tertiary; _healthGauge.BorderBrush = ThemeTokens.Tertiary; }
        else if (health > 70) { _healthStatusText.Text = "WARNING"; _healthStatusText.Foreground = ThemeTokens.HealthWarning; _healthGauge.BorderBrush = ThemeTokens.HealthWarning; }
        else { _healthStatusText.Text = "CRITICAL"; _healthStatusText.Foreground = ThemeTokens.Error; _healthGauge.BorderBrush = ThemeTokens.Error; }

        _radar.UpdateNodes(_nodes);
        RefreshPingsList();
    }

    private void RefreshPingsList()
    {
        _pingsList.Children.Clear();
        var recent = _nodes.Where(n => n.IsOnline || n.IsRegistered).OrderByDescending(n => n.IsOnline).ThenByDescending(n => n.LastSeen).Take(10).ToList();
        foreach (var node in recent) _pingsList.Children.Add(MakePingRow(node));
    }

    private Border MakePingRow(NetworkNode node)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(GridLength.Auto) } };
        var dot = ThemeTokens.StatusDot(node.IsOnline, 6); dot.VerticalAlignment = VerticalAlignment.Center; dot.Margin = new Thickness(0, 0, 10, 0);
        var name = new TextBlock { Text = node.IpAddress, FontSize = 12, Foreground = ThemeTokens.OnSurface, FontFamily = ThemeTokens.DefaultFont };
        var lat = new TextBlock { Text = node.PingLatencyMs >= 0 ? $"{node.PingLatencyMs}ms" : (node.IsOnline ? "ARP" : "—"), FontSize = 11, Foreground = node.PingLatencyMs > 100 ? ThemeTokens.Error : ThemeTokens.Tertiary, FontFamily = ThemeTokens.DefaultFont, HorizontalAlignment = HorizontalAlignment.Right };

        Grid.SetColumn(dot, 0); Grid.SetColumn(name, 1); Grid.SetColumn(lat, 2);
        grid.Children.Add(dot); grid.Children.Add(name); grid.Children.Add(lat);

        var row = new Border { Padding = new Thickness(8, 6), CornerRadius = new CornerRadius(6), Background = ThemeTokens.SurfaceContainerLowest, Child = grid, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
        row.PointerPressed += (s, e) => DeviceSelected?.Invoke(node);
        return row;
    }

    private static Border MakeBentoStat(string label, TextBlock value, string icon, string tip)
    {
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(GridLength.Auto) } };
        var lbl = ThemeTokens.Label(label, 10);
        var ico = new TextBlock { Text = icon, FontSize = 14, Opacity = 0.3, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(lbl, 0); Grid.SetColumn(ico, 1);
        header.Children.Add(lbl); header.Children.Add(ico);

        var card = ThemeTokens.Card(new StackPanel { Spacing = 4, Children = { header, value } }, ThemeTokens.SurfaceContainerHigh, 20);
        ThemeTokens.SetToolTip(card, tip);
        return card;
    }
}
