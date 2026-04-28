using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Threading;

namespace NodeRadarPro.UI;

/// <summary>
/// System Logs page: event log viewer with level filter, search, export, and auto-scroll.
/// </summary>
public class SystemLogsPage : Border
{
    private readonly LocalDatabase _db;
    private readonly StackPanel _logBody;
    private readonly TextBox _searchBox;
    private readonly TextBlock _entryCount;
    private readonly StackPanel _filterRow;
    private readonly TextBlock _infoStatValue;
    private readonly TextBlock _warnStatValue;
    private readonly TextBlock _errorStatValue;
    private LogLevel? _levelFilter = null;
    private string? _deviceFilter = null;
    private readonly CheckBox _autoScrollToggle;
    private readonly ScrollViewer _logScroll;

    public SystemLogsPage(LocalDatabase db)
    {
        _db = db;
        Background = ThemeTokens.Surface;

        // Header
        var label = new TextBlock { Text = "SYSTEM TELEMETRY", FontSize = 13, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.NavAccentBorder, FontFamily = new FontFamily("Inter"), LetterSpacing = 2.5, Margin = new Thickness(0, 0, 0, 8) };
        var title = ThemeTokens.Headline("System Logs", 38);
        title.Margin = new Thickness(0, 0, 0, 6);
        var subtitle = ThemeTokens.Body("Event log viewer for scan events, device status changes, and alert triggers.", 16);

        // Export button
        var exportBtn = ThemeTokens.SecondaryButton("⬇  Export Logs");
        exportBtn.Width = 160;
        exportBtn.HorizontalAlignment = HorizontalAlignment.Right;
        exportBtn.VerticalAlignment = VerticalAlignment.Bottom;
        exportBtn.Click += OnExportLogs;

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var titleGroup = new StackPanel { Children = { label, title, subtitle } };
        Grid.SetColumn(titleGroup, 0); Grid.SetColumn(exportBtn, 1);
        headerGrid.Children.Add(titleGroup); headerGrid.Children.Add(exportBtn);

        // Quick Stats — TextBlocks are now class fields so they update on refresh (B5)
        var statsGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) }, Margin = new Thickness(0, 20, 0, 20) };

        _infoStatValue = new TextBlock { Text = "0", FontSize = 32, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter") };
        _warnStatValue = new TextBlock { Text = "0", FontSize = 32, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#FFCE50")), FontFamily = new FontFamily("Inter") };
        _errorStatValue = new TextBlock { Text = "0", FontSize = 32, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Error, FontFamily = new FontFamily("Inter") };

        var infoCard = MakeLogStatCard("ℹ", "Info", _infoStatValue);
        var warnCard = MakeLogStatCard("⚠", "Warnings", _warnStatValue);
        var errCard = MakeLogStatCard("❌", "Errors", _errorStatValue);
        Grid.SetColumn(infoCard, 0); Grid.SetColumn(warnCard, 1); Grid.SetColumn(errCard, 2);
        infoCard.Margin = new Thickness(0, 0, 8, 0); warnCard.Margin = new Thickness(4, 0, 4, 0); errCard.Margin = new Thickness(8, 0, 0, 0);
        statsGrid.Children.Add(infoCard); statsGrid.Children.Add(warnCard); statsGrid.Children.Add(errCard);

        // Filter Row — chips are rebuilt in RefreshLogs() (B4)
        _filterRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 12) };

        var clearBtn = new Button
        {
            Content = "Reset All Filters",
            FontSize = 12,
            Background = Brushes.Transparent,
            Foreground = ThemeTokens.Error,
            Padding = new Thickness(10, 4),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        clearBtn.Click += (s, e) => ClearAllFilters();

        var searchIcon = new TextBlock { Text = "🔍", FontSize = 13, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 8, 0) };
        _searchBox = ThemeTokens.Input("Search logs..."); _searchBox.Width = 200; _searchBox.FontSize = 14; _searchBox.Padding = new Thickness(10, 6); _searchBox.Background = Brushes.Transparent;
        _searchBox.TextChanged += (s, e) => RefreshLogs();
        var searchWrap = new Border { Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 0), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { searchIcon, _searchBox } } };

        _autoScrollToggle = new CheckBox { Content = "Auto-scroll", Foreground = ThemeTokens.OnSurfaceVariant, FontSize = 13, FontFamily = new FontFamily("Inter"), IsChecked = true, Margin = new Thickness(12, 0, 0, 0) };

        _entryCount = new TextBlock { Text = "(0 entries)", FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };

        var filterBarGrid = new Grid();
        filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        filterBarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        
        var leftControls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { _filterRow, clearBtn } };

        Grid.SetColumn(leftControls, 0); Grid.SetColumn(searchWrap, 1); Grid.SetColumn(_autoScrollToggle, 2); Grid.SetColumn(_entryCount, 3);
        filterBarGrid.Children.Add(leftControls); filterBarGrid.Children.Add(searchWrap); filterBarGrid.Children.Add(_autoScrollToggle); filterBarGrid.Children.Add(_entryCount);

        // Log Entries
        _logBody = new StackPanel { Spacing = 2 };
        _logScroll = new ScrollViewer { Content = _logBody, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };

        var logDock = new DockPanel();
        DockPanel.SetDock(filterBarGrid, Dock.Top);
        logDock.Children.Add(filterBarGrid); logDock.Children.Add(_logScroll);
        var logCard = ThemeTokens.Card(logDock, ThemeTokens.SurfaceContainerLow, 20);

        // Root
        var root = new DockPanel { Margin = new Thickness(32, 28) };
        DockPanel.SetDock(headerGrid, Dock.Top); DockPanel.SetDock(statsGrid, Dock.Top);
        root.Children.Add(headerGrid); root.Children.Add(statsGrid); root.Children.Add(logCard);

        Child = new ScrollViewer { Content = root, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };

        RefreshLogs();
    }

    public void RefreshLogs()
    {
        _logBody.Children.Clear();

        // Rebuild filter chips with current active state (B4 fix)
        _filterRow.Children.Clear();
        _filterRow.Children.Add(MakeLevelChip("All", null));
        _filterRow.Children.Add(MakeLevelChip("Info", LogLevel.Info));
        _filterRow.Children.Add(MakeLevelChip("Warning", LogLevel.Warning));
        _filterRow.Children.Add(MakeLevelChip("Error", LogLevel.Error));

        string search = _searchBox?.Text?.Trim().ToLower() ?? "";

        var logs = _db.GetLogs(500, _levelFilter, _deviceFilter);
        if (!string.IsNullOrEmpty(search))
            logs = logs.Where(l => l.Message.ToLower().Contains(search) || l.Source.ToLower().Contains(search) || (l.DeviceMac ?? "").ToLower().Contains(search)).ToList();

        _entryCount.Text = $"({logs.Count} entries)";

        // Update stat cards (B5 fix)
        var allLogs = _db.GetLogs(500);
        _infoStatValue.Text = allLogs.Count(l => l.Level == LogLevel.Info).ToString();
        _warnStatValue.Text = allLogs.Count(l => l.Level == LogLevel.Warning).ToString();
        _errorStatValue.Text = allLogs.Count(l => l.Level == LogLevel.Error).ToString();

        foreach (var log in logs)
            _logBody.Children.Add(BuildLogRow(log));

        if (_autoScrollToggle.IsChecked == true && _logBody.Children.Count > 0)
            _logScroll.ScrollToEnd();
    }

    public void FilterByDevice(string macAddress)
    {
        _deviceFilter = macAddress;
        RefreshLogs();
    }

    public void ClearDeviceFilter()
    {
        _deviceFilter = null;
        RefreshLogs();
    }

    public void ClearAllFilters()
    {
        _levelFilter = null;
        _deviceFilter = null;
        if (_searchBox != null) _searchBox.Text = "";
        RefreshLogs();
    }

    private Border BuildLogRow(LogEntry log)
    {
        string icon = log.Level switch { LogLevel.Info => "ℹ", LogLevel.Warning => "⚠", LogLevel.Error => "❌", _ => "•" };
        IBrush levelColor = log.Level switch { LogLevel.Info => ThemeTokens.Tertiary, LogLevel.Warning => new SolidColorBrush(Color.Parse("#FFCE50")), LogLevel.Error => ThemeTokens.Error, _ => ThemeTokens.OnSurfaceVariant };

        var iconTb = new TextBlock { Text = icon, FontSize = 14, Foreground = levelColor, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), Width = 20, TextAlignment = TextAlignment.Center };
        var timeTb = new TextBlock { Text = log.Timestamp.ToLocalTime().ToString("HH:mm:ss"), FontSize = 13, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center, Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        var srcTb = new Border { Background = ThemeTokens.SurfaceContainerHigh, CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 2), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), Child = new TextBlock { Text = log.Source, FontSize = 12, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium } };
        var msgTb = new TextBlock { Text = log.Message, FontSize = 14, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter"), TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        var dateTb = new TextBlock { Text = log.Timestamp.ToLocalTime().ToString("MMM dd"), FontSize = 12, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center };

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(iconTb, 0); Grid.SetColumn(timeTb, 1); Grid.SetColumn(srcTb, 2); Grid.SetColumn(msgTb, 3); Grid.SetColumn(dateTb, 4);
        row.Children.Add(iconTb); row.Children.Add(timeTb); row.Children.Add(srcTb); row.Children.Add(msgTb); row.Children.Add(dateTb);

        return new Border { Padding = new Thickness(10, 8), CornerRadius = new CornerRadius(6), Child = row };
    }

    private Border MakeLevelChip(string text, LogLevel? level)
    {
        bool active = _levelFilter == level;
        var chip = new Border { CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 6), Background = active ? ThemeTokens.PrimaryContainer : Brushes.Transparent, BorderBrush = active ? Brushes.Transparent : ThemeTokens.GhostBorder30, BorderThickness = new Thickness(1), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand), Child = new TextBlock { Text = text, FontSize = 14, Foreground = active ? ThemeTokens.OnPrimaryContainer : ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium } };
        chip.PointerPressed += (s, e) => { _levelFilter = level; RefreshLogs(); };
        return chip;
    }

    private static Border MakeLogStatCard(string icon, string label, TextBlock valueText)
    {
        var iconTb = new TextBlock { Text = icon, FontSize = 20, VerticalAlignment = VerticalAlignment.Top };
        var lblTb = new TextBlock { Text = label, FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 4, 0, 0) };
        return ThemeTokens.GlassCard(new StackPanel { Spacing = 4, Children = { iconTb, valueText, lblTb } }, 20);
    }

    private void OnExportLogs(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var logs = _db.GetLogs(2000);
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Level,Source,Message,Device");
            foreach (var log in logs)
                sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Level}\",\"{log.Source}\",\"{log.Message.Replace("\"", "\"\"")}\",\"{log.DeviceMac ?? ""}\"");

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro", $"logs_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.WriteAllText(path, sb.ToString());
            _db.Log(LogLevel.Info, "Export", $"Logs exported to {path}");
            RefreshLogs();
        }
        catch { }
    }
}
