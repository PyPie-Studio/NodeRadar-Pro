using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;

namespace NodeRadarPro.UI;

/// <summary>
/// Alert management dashboard: active/resolved alerts, counts, resolve actions.
/// </summary>
public class AlertsPage : Border
{
    private readonly LocalDatabase _db;
    private readonly StackPanel _alertListBody;
    private readonly TextBlock _activeCount;
    private readonly TextBlock _resolvedCount;
    private readonly TextBlock _totalCount;
    private readonly StackPanel _filterRow;
    private string _filterMode = "active";

    public event Action? AlertsChanged;

    public AlertsPage(LocalDatabase db)
    {
        _db = db;
        Background = ThemeTokens.Surface;

        // Header
        var label = new TextBlock { Text = "THREAT INTELLIGENCE", FontSize = 13, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.NavAccentBorder, FontFamily = new FontFamily("Inter"), LetterSpacing = 2.5, Margin = new Thickness(0, 0, 0, 8) };
        var title = ThemeTokens.Headline("Alert Center", 38);
        title.Margin = new Thickness(0, 0, 0, 6);
        var subtitle = ThemeTokens.Body("Monitor network anomalies, latency spikes, and connection events.", 16);
        var headerSection = new StackPanel { Margin = new Thickness(0, 0, 0, 24), Children = { label, title, subtitle } };

        // Quick Stats Cards
        _activeCount = new TextBlock { Text = "0", FontSize = 36, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Error, FontFamily = new FontFamily("Inter") };
        _resolvedCount = new TextBlock { Text = "0", FontSize = 36, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter") };
        _totalCount = new TextBlock { Text = "0", FontSize = 36, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter") };

        var statsGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) }, Margin = new Thickness(0, 0, 0, 20) };

        var activeCard = MakeStatCard("⚠", "Active Alerts", _activeCount, ThemeTokens.Error);
        ThemeTokens.SetToolTip(activeCard, "Outstanding network anomalies and security events requiring intervention.");
        var resolvedCard = MakeStatCard("✅", "Resolved", _resolvedCount, ThemeTokens.Tertiary);
        ThemeTokens.SetToolTip(resolvedCard, "Total count of alerts that have been acknowledged and cleared.");
        var totalCard = MakeStatCard("📊", "Total Events", _totalCount, ThemeTokens.Primary);
        ThemeTokens.SetToolTip(totalCard, "Cumulative history of all security and connectivity triggers in the current session.");

        Grid.SetColumn(activeCard, 0); Grid.SetColumn(resolvedCard, 1); Grid.SetColumn(totalCard, 2);
        activeCard.Margin = new Thickness(0, 0, 8, 0); resolvedCard.Margin = new Thickness(4, 0, 4, 0); totalCard.Margin = new Thickness(8, 0, 0, 0);
        statsGrid.Children.Add(activeCard); statsGrid.Children.Add(resolvedCard); statsGrid.Children.Add(totalCard);

        // Filter Row + Actions
        _filterRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 16) };

        var resolveAllBtn = ThemeTokens.SecondaryButton("✓  Resolve All");
        ThemeTokens.SetToolTip(resolveAllBtn, "Mass-resolve all pending alerts and clear the active threat list.");
        resolveAllBtn.Width = 160; resolveAllBtn.HorizontalAlignment = HorizontalAlignment.Right;
        resolveAllBtn.Click += (s, e) => { _db.ResolveAllAlerts(); RefreshAlerts(); AlertsChanged?.Invoke(); };

        var filterGrid = new Grid();
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(_filterRow, 0); Grid.SetColumn(resolveAllBtn, 1);
        filterGrid.Children.Add(_filterRow); filterGrid.Children.Add(resolveAllBtn);

        // Alert list
        _alertListBody = new StackPanel { Spacing = 6 };
        var listScroll = new ScrollViewer { Content = _alertListBody, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };

        var listCard = ThemeTokens.Card(new DockPanel { Children = { filterGrid, listScroll } }, ThemeTokens.SurfaceContainerLow, 20);
        DockPanel.SetDock(filterGrid, Dock.Top);
        ThemeTokens.SetToolTip(listCard, "A chronological list of network events and security findings.");

        // Root
        var root = new DockPanel { Margin = new Thickness(32, 28) };
        DockPanel.SetDock(headerSection, Dock.Top);
        DockPanel.SetDock(statsGrid, Dock.Top);
        root.Children.Add(headerSection); root.Children.Add(statsGrid); root.Children.Add(listCard);

        Child = new ScrollViewer { Content = root, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };

        RefreshAlerts();
    }

    public void RefreshAlerts()
    {
        _alertListBody.Children.Clear();

        // Rebuild filter chips with current active state (B3 fix)
        _filterRow.Children.Clear();
        _filterRow.Children.Add(MakeFilterChip("Active", "active", _filterMode == "active"));
        _filterRow.Children.Add(MakeFilterChip("Resolved", "resolved", _filterMode == "resolved"));
        _filterRow.Children.Add(MakeFilterChip("All", "all", _filterMode == "all"));

        var allAlerts = _db.GetAlerts(500);

        int activeCount = 0;
        int resolvedCount = 0;
        var filtered = new List<NodeRadarPro.Core.AlertEvent>();

        foreach (var a in allAlerts)
        {
            if (a.IsResolved)
            {
                resolvedCount++;
                if (_filterMode == "resolved") filtered.Add(a);
            }
            else
            {
                activeCount++;
                if (_filterMode == "active") filtered.Add(a);
            }
        }

        if (_filterMode == "all")
        {
            filtered = allAlerts;
        }

        _activeCount.Text = activeCount.ToString();
        _resolvedCount.Text = resolvedCount.ToString();
        _totalCount.Text = allAlerts.Count.ToString();

        if (filtered.Count == 0)
        {
            _alertListBody.Children.Add(new TextBlock { Text = _filterMode == "active" ? "✅  No active alerts. Your network is healthy." : "No alerts in this category.", FontSize = 16, Foreground = ThemeTokens.OnSurfaceVariant, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40), FontFamily = new FontFamily("Inter") });
            return;
        }

        foreach (var alert in filtered)
            _alertListBody.Children.Add(BuildAlertCard(alert));
    }

    private Border BuildAlertCard(AlertEvent alert)
    {
        string icon = alert.AlertType switch { AlertType.ConnectionLost => "🔴", AlertType.HighLatency => "🟡", AlertType.PacketLoss => "🟠", AlertType.DeviceReconnected => "🟢", AlertType.NewDeviceDiscovered => "🔵", _ => "⚪" };
        string typeText = alert.AlertType switch { AlertType.ConnectionLost => "Connection Lost", AlertType.HighLatency => "High Latency", AlertType.PacketLoss => "Packet Loss", AlertType.DeviceReconnected => "Reconnected", AlertType.NewDeviceDiscovered => "New Device", _ => "Alert" };

        var iconTb = new TextBlock { Text = icon, FontSize = 20, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
        var titleTb = new TextBlock { Text = $"{typeText}: {alert.DeviceName}", FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter"), TextTrimming = TextTrimming.CharacterEllipsis };
        var msgTb = new TextBlock { Text = alert.Message, FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 0) };
        var timeTb = new TextBlock { Text = GetTimeAgo(alert.Timestamp), FontSize = 13, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter") };

        var textCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { titleTb, msgTb } };

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(iconTb, 0); Grid.SetColumn(textCol, 1); Grid.SetColumn(timeTb, 2);
        timeTb.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(iconTb); row.Children.Add(textCol); row.Children.Add(timeTb);

        if (!alert.IsResolved)
        {
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var resolveBtn = new Button { Content = "✓", Background = Brushes.Transparent, Foreground = ThemeTokens.Tertiary, Width = 36, Height = 36, CornerRadius = new CornerRadius(6), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, FontSize = 16, Margin = new Thickness(8, 0, 0, 0), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
            ThemeTokens.SetToolTip(resolveBtn, "Mark this specific alert as resolved and clear it from the active list.");
            resolveBtn.Click += (s, e) => { _db.ResolveAlert(alert.Id); RefreshAlerts(); AlertsChanged?.Invoke(); };
            Grid.SetColumn(resolveBtn, 3); row.Children.Add(resolveBtn);
        }
        else
        {
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var resolvedBadge = new Border { Background = new SolidColorBrush(Color.Parse("#005362"), 0.2), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Child = new TextBlock { Text = "Resolved", FontSize = 12, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter") } };
            Grid.SetColumn(resolvedBadge, 3); row.Children.Add(resolvedBadge);
        }

        return new Border
        {
            Background = alert.IsResolved ? Brushes.Transparent : ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(10), Padding = new Thickness(16, 14),
            BorderBrush = ThemeTokens.GhostBorder, BorderThickness = new Thickness(1), Child = row
        };
    }

    private Border MakeFilterChip(string text, string key, bool active)
    {
        var chip = new Border { CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 6), Background = active ? ThemeTokens.PrimaryContainer : Brushes.Transparent, BorderBrush = active ? Brushes.Transparent : ThemeTokens.GhostBorder30, BorderThickness = new Thickness(1), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand), Child = new TextBlock { Text = text, FontSize = 14, Foreground = active ? ThemeTokens.OnPrimaryContainer : ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium } };
        chip.PointerPressed += (s, e) => { _filterMode = key; RefreshAlerts(); };
        return chip;
    }

    private static Border MakeStatCard(string icon, string label, TextBlock valueText, IBrush color)
    {
        var iconTb = new TextBlock { Text = icon, FontSize = 22, VerticalAlignment = VerticalAlignment.Top };
        var labelTb = new TextBlock { Text = label, FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 4, 0, 0) };
        return ThemeTokens.GlassCard(new StackPanel { Spacing = 4, Children = { iconTb, valueText, labelTb } }, 20);
    }

    private static string GetTimeAgo(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        if (diff.TotalSeconds < 10) return "Just now";
        if (diff.TotalMinutes < 1) return $"{(int)diff.TotalSeconds}s ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }
}
