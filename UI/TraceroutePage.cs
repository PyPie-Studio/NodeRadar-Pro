using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace NodeRadarPro.UI;

/// <summary>
/// Hop-by-hop path visualization page.
/// </summary>
public class TraceroutePage : Border
{
    private readonly TracerouteEngine _engine = new();
    private readonly TextBox _destInput;
    private readonly Button _startBtn;
    private readonly Button _stopBtn;
    private readonly StackPanel _hopsContainer;
    private readonly ScrollViewer _scroll;
    private readonly TextBlock _statusText;
    private readonly ProgressBar _progressBar;

    private CancellationTokenSource? _cts;
    private bool _isTracing = false;

    public TraceroutePage()
    {
        Background = ThemeTokens.Surface;

        // Header
        var title = ThemeTokens.Headline("Visual Traceroute", 36);
        var subtitle = ThemeTokens.Body("Map the digital pathway to any network destination.", 14);

        _startBtn = ThemeTokens.PrimaryButton("▶  Start Trace");
        _startBtn.Width = 160;
        _startBtn.Height = 44;
        _startBtn.Click += OnStartTrace;
        ThemeTokens.SetToolTip(_startBtn, "Begin identifying intermediate hops to the target destination.");

        _stopBtn = ThemeTokens.SecondaryButton("■  Stop");
        _stopBtn.Width = 100;
        _stopBtn.Height = 44;
        _stopBtn.Foreground = ThemeTokens.Error;
        _stopBtn.BorderBrush = new SolidColorBrush(Color.Parse("#FFB4AB"), 0.3);
        _stopBtn.Click += OnStopTrace;
        _stopBtn.IsEnabled = false;

        var headerRight = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { _startBtn, _stopBtn } };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var titleGroup = new StackPanel { Children = { title, subtitle } };
        Grid.SetColumn(titleGroup, 0); Grid.SetColumn(headerRight, 1);
        headerGrid.Children.Add(titleGroup); headerGrid.Children.Add(headerRight);

        // Input Card
        var inputTitle = ThemeTokens.Label("TARGET DESTINATION", 10);
        inputTitle.LetterSpacing = 1.5;
        _destInput = ThemeTokens.Input("e.g., google.com or 8.8.8.8");
        _destInput.Text = "8.8.8.8";
        _destInput.Height = 46;
        _destInput.FontSize = 16;
        _destInput.VerticalContentAlignment = VerticalAlignment.Center;
        ThemeTokens.SetToolTip(_destInput, "Enter a hostname or IPv4 address to trace.");

        var inputContent = new StackPanel { Spacing = 8, Children = { inputTitle, _destInput } };
        var inputCard = ThemeTokens.Card(inputContent, ThemeTokens.SurfaceContainerLow, 24);
        inputCard.Margin = new Thickness(0, 20, 0, 0);

        // Status Card
        _statusText = ThemeTokens.Body("Ready to map pathway.", 13);
        _progressBar = new ProgressBar { Minimum = 0, Maximum = 30, Value = 0, Height = 4, Foreground = ThemeTokens.Tertiary, Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 10, 0, 0) };
        var statusContent = new StackPanel { Children = { _statusText, _progressBar } };
        var statusCard = ThemeTokens.GlassCard(statusContent, 24);
        statusCard.Margin = new Thickness(10, 20, 0, 0);

        var topBento = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(1.5, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) } };
        Grid.SetColumn(inputCard, 0); Grid.SetColumn(statusCard, 1);
        topBento.Children.Add(inputCard); topBento.Children.Add(statusCard);

        // Hops View
        _hopsContainer = new StackPanel { Spacing = 0, Margin = new Thickness(0, 20, 0, 40) };
        _scroll = new ScrollViewer { Content = _hopsContainer, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        
        var listCard = ThemeTokens.Card(_scroll, ThemeTokens.SurfaceContainerLow, 0);
        listCard.Margin = new Thickness(0, 20, 0, 0);

        var root = new DockPanel { Margin = new Thickness(32, 28) };
        DockPanel.SetDock(headerGrid, Dock.Top);
        DockPanel.SetDock(topBento, Dock.Top);
        root.Children.Add(headerGrid);
        root.Children.Add(topBento);
        root.Children.Add(listCard);

        Child = root;

        _engine.HopDiscovered += OnHopDiscovered;
        _engine.TracerouteCompleted += OnTraceCompleted;
    }

    private async void OnStartTrace(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isTracing) return;
        string target = _destInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(target)) return;

        _isTracing = true;
        _startBtn.IsEnabled = false;
        _stopBtn.IsEnabled = true;
        _hopsContainer.Children.Clear();
        _progressBar.Value = 0;
        _statusText.Text = $"Tracing route to {target}...";
        _cts = new CancellationTokenSource();

        await Task.Run(() => _engine.RunTracerouteAsync(target, 30, 2000, _cts.Token));
    }

    private void OnStopTrace(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _cts?.Cancel();
        _statusText.Text = "Trace cancelled.";
        ResetButtons();
    }

    private void OnHopDiscovered(RouteHop hop)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _hopsContainer.Children.Add(BuildHopRow(hop));
            _progressBar.Value = hop.HopNumber;
            _statusText.Text = $"Hop {hop.HopNumber}: {hop.IpAddress}";
            _scroll.ScrollToEnd();
        }, DispatcherPriority.Render);
    }

    private void OnTraceCompleted(bool success)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _statusText.Text = success ? "Traceroute complete." : "Trace failed to resolve target.";
            ResetButtons();
        });
    }

    private void ResetButtons()
    {
        _isTracing = false;
        _startBtn.IsEnabled = true;
        _stopBtn.IsEnabled = false;
    }

    private Border BuildHopRow(RouteHop hop)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(60))); // Hop #
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(180))); // IP
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star))); // Hostname
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(100))); // Latency

        // Visual Connector (Vertical line)
        var connector = new Border { Width = 2, Background = ThemeTokens.GhostBorder30, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch };
        var dot = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = hop.Status ? (hop.IsDestination ? ThemeTokens.Tertiary : ThemeTokens.Primary) : ThemeTokens.Error, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        if (hop.Status) dot.BoxShadow = new BoxShadows(new BoxShadow { Blur = 8, Color = hop.IsDestination ? Color.Parse("#4CD7F6") : Color.Parse("#7C3AED") });

        var visualStack = new Panel { Children = { connector, dot }, Margin = new Thickness(16, 0) };

        var hopNum = new TextBlock { Text = hop.HopNumber.ToString(), FontSize = 16, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var ipText = new TextBlock { Text = hop.IpAddress, FontSize = 14, Foreground = hop.Status ? ThemeTokens.OnSurface : ThemeTokens.Error, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center };
        var hostText = new TextBlock { Text = hop.Hostname, FontSize = 13, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        var latText = new TextBlock { Text = hop.LatencyMs >= 0 ? $"{hop.LatencyMs}ms" : "*", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.Tertiary, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 20, 0) };

        ThemeTokens.AddCopyAction(ipText);

        Grid.SetColumn(hopNum, 0); Grid.SetColumn(ipText, 1); Grid.SetColumn(hostText, 2); Grid.SetColumn(latText, 3);
        grid.Children.Add(hopNum); grid.Children.Add(ipText); grid.Children.Add(hostText); grid.Children.Add(latText);

        var content = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) } };
        Grid.SetColumn(visualStack, 0); Grid.SetColumn(grid, 1);
        content.Children.Add(visualStack); content.Children.Add(grid);

        var row = new Border { Background = Brushes.Transparent, Padding = new Thickness(0, 0), Height = 56, Child = content };
        
        row.PointerEntered += (s, e) => row.Background = ThemeTokens.SurfaceContainerLowest;
        row.PointerExited += (s, e) => row.Background = Brushes.Transparent;

        return row;
    }
}
