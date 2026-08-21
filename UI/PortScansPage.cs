using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace NodeRadarPro.UI;

/// <summary>
/// Dedicated Port Scanner page with custom port ranges, scan profiles, and results.
/// </summary>
public class PortScansPage : Border
{
    private readonly TextBox _targetIp;
    private readonly TextBox _startPort;
    private readonly TextBox _endPort;
    private readonly Button _scanBtn;
    private readonly Button _stopBtn;
    private readonly TextBlock _statusText;
    private readonly TextBlock _progressPct;
    private readonly ProgressBar _progressBar;
    private readonly StackPanel _resultsBody;
    private readonly TextBlock _openCount;
    private readonly TextBlock _elapsedTime;
    private DateTime _scanStartTime;
    private CancellationTokenSource? _cts;
    private bool _isScanning;

    public PortScansPage()
    {
        Background = ThemeTokens.Surface;

        // Header
        var label = new TextBlock { Text = "PORT RECONNAISSANCE", FontSize = 13, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.NavAccentBorder, FontFamily = new FontFamily("Inter"), LetterSpacing = 2.5, Margin = new Thickness(0, 0, 0, 8) };
        var title = ThemeTokens.Headline("Port Scanner", 38);
        title.Margin = new Thickness(0, 0, 0, 6);
        var subtitle = ThemeTokens.Body("Probe individual hosts for open ports, running services, and OS fingerprints.", 16);

        _scanBtn = new Button
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = "▶", FontSize = 15, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = "Start Port Scan", FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center } } },
            Background = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative), GradientStops = { new GradientStop(Color.Parse("#7C3AED"), 0), new GradientStop(Color.Parse("#6B21A8"), 1) } },
            Height = 46, Padding = new Thickness(28, 0), CornerRadius = new CornerRadius(8), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        _scanBtn.Click += OnStartScan;
        ThemeTokens.SetToolTip(_scanBtn, "Begin an exhaustive port enumeration on the target IP.");

        _stopBtn = new Button
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = "■", FontSize = 13, Foreground = ThemeTokens.Error, VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = "Stop", FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.Error, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center } } },
            Background = Brushes.Transparent, Height = 46, Padding = new Thickness(24, 0), CornerRadius = new CornerRadius(8), BorderBrush = new SolidColorBrush(Color.Parse("#FFB4AB"), 0.3), BorderThickness = new Thickness(1), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center
        };
        _stopBtn.Click += OnStopScan;
        ThemeTokens.SetToolTip(_stopBtn, "Immediately terminate the active port sweep.");

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Children = { _scanBtn, _stopBtn } };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var titleGroup = new StackPanel { Children = { label, title, subtitle } };
        Grid.SetColumn(titleGroup, 0); Grid.SetColumn(btnRow, 1);
        headerGrid.Children.Add(titleGroup); headerGrid.Children.Add(btnRow);

        // Target Configuration Card
        var cfgTitle = ThemeTokens.Headline("Target Configuration", 22);
        cfgTitle.Foreground = ThemeTokens.Primary;
        cfgTitle.Margin = new Thickness(0, 0, 0, 20);

        var ipLabel = ThemeTokens.Label("TARGET IP ADDRESS", 12); ipLabel.LetterSpacing = 1.5; ipLabel.Margin = new Thickness(0, 0, 0, 8);
        _targetIp = ThemeTokens.Input("192.168.1.1");
        _targetIp.Text = $"{SubnetScanner.GetLocalBaseIp()}.1";
        ThemeTokens.SetToolTip(_targetIp, "The IPv4 address of the host you wish to probe.");

        var portStartLabel = ThemeTokens.Label("START PORT", 12); portStartLabel.LetterSpacing = 1.5; portStartLabel.Margin = new Thickness(0, 0, 0, 8);
        _startPort = ThemeTokens.Input("1"); _startPort.Text = "1";
        ThemeTokens.SetToolTip(_startPort, "The lower bound of the TCP port range (1-65535).");

        var portEndLabel = ThemeTokens.Label("END PORT", 12); portEndLabel.LetterSpacing = 1.5; portEndLabel.Margin = new Thickness(0, 0, 0, 8);
        _endPort = ThemeTokens.Input("1024"); _endPort.Text = "1024";
        ThemeTokens.SetToolTip(_endPort, "The upper bound of the TCP port range (1-65535).");

        var portGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Auto)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) } };
        var startCol = new StackPanel { Children = { portStartLabel, _startPort } };
        var endCol = new StackPanel { Children = { portEndLabel, _endPort } };
        var dash = new TextBlock { Text = "—", FontSize = 22, Foreground = ThemeTokens.Outline, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(12, 24, 12, 0) };
        Grid.SetColumn(startCol, 0); Grid.SetColumn(dash, 1); Grid.SetColumn(endCol, 2);
        portGrid.Children.Add(startCol); portGrid.Children.Add(dash); portGrid.Children.Add(endCol);

        // Quick profile buttons
        var profileLabel = ThemeTokens.Label("QUICK PROFILES", 12); profileLabel.LetterSpacing = 1.5; profileLabel.Margin = new Thickness(0, 16, 0, 8);
        var profileRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        profileRow.Children.Add(MakeProfileChip("Common", "1", "1024", "Scan standard service ports (1-1024)."));
        profileRow.Children.Add(MakeProfileChip("Web", "80", "8443", "Scan common web and application server ports."));
        profileRow.Children.Add(MakeProfileChip("Database", "1433", "5432", "Scan SQL, Redis, and Mongo database ports."));
        profileRow.Children.Add(MakeProfileChip("Full", "1", "65535", "Perform an exhaustive scan of all 65,535 possible TCP ports."));

        var cfgContent = new StackPanel { Children = { cfgTitle, ipLabel, _targetIp, new Panel { Height = 16 }, portGrid, profileLabel, profileRow } };
        var cfgCard = ThemeTokens.Card(cfgContent, ThemeTokens.SurfaceContainerLow, 28);

        // Status Card
        var sweepTitle = ThemeTokens.Headline("Scan Status", 20);
        _statusText = ThemeTokens.Body("Ready to scan.", 15); _statusText.Margin = new Thickness(0, 6, 0, 20);
        _progressPct = new TextBlock { Text = "0%", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter") };
        var progLabel = new TextBlock { Text = "Progress", FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium };
        var progHeader = new Grid();
        progHeader.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        progHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(progLabel, 0); Grid.SetColumn(_progressPct, 1);
        progHeader.Children.Add(progLabel); progHeader.Children.Add(_progressPct);
        _progressBar = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Height = 8, Foreground = ThemeTokens.Tertiary, Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 6, 0, 0) };
        _openCount = ThemeTokens.Label("OPEN PORTS: 0", 12); _openCount.LetterSpacing = 1.2;
        _elapsedTime = ThemeTokens.Label("ELAPSED: 00:00", 12); _elapsedTime.LetterSpacing = 1.2; _elapsedTime.HorizontalAlignment = HorizontalAlignment.Right;

        var footerStats = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        footerStats.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        footerStats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(_openCount, 0);
        Grid.SetColumn(_elapsedTime, 1);
        footerStats.Children.Add(_openCount);
        footerStats.Children.Add(_elapsedTime);

        var statusContent = new StackPanel { Children = { sweepTitle, _statusText, progHeader, _progressBar, footerStats } };
        var statusCard = ThemeTokens.GlassCard(statusContent, 28);

        var bentoGrid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(2, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) }, Margin = new Thickness(0, 20, 0, 0) };
        Grid.SetColumn(cfgCard, 0); Grid.SetColumn(statusCard, 1);
        cfgCard.Margin = new Thickness(0, 0, 10, 0); statusCard.Margin = new Thickness(10, 0, 0, 0);
        bentoGrid.Children.Add(cfgCard); bentoGrid.Children.Add(statusCard);

        // Results Table
        var tableTitle = ThemeTokens.Headline("Discovered Services", 22); tableTitle.FontWeight = FontWeight.Bold;
        var tableTitleRow = new Border { Margin = new Thickness(20, 16), Child = tableTitle };

        var tableHeader = MakeTableHeader();
        _resultsBody = new StackPanel { Spacing = 0 };
        var tableScroll = new ScrollViewer { Content = _resultsBody, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };

        var tableDock = new DockPanel();
        DockPanel.SetDock(tableTitleRow, Dock.Top); DockPanel.SetDock(tableHeader, Dock.Top);
        tableDock.Children.Add(tableTitleRow); tableDock.Children.Add(tableHeader); tableDock.Children.Add(tableScroll);

        var tableCard = ThemeTokens.Card(tableDock, ThemeTokens.SurfaceContainerLow, 0);
        tableCard.Margin = new Thickness(0, 20, 0, 0);

        var root = new DockPanel { Margin = new Thickness(32, 28) };
        DockPanel.SetDock(headerGrid, Dock.Top); DockPanel.SetDock(bentoGrid, Dock.Top);
        root.Children.Add(headerGrid); root.Children.Add(bentoGrid); root.Children.Add(tableCard);

        Child = new ScrollViewer { Content = root, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
    }

    private async void OnStartScan(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isScanning) return;
        string ip = _targetIp.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(ip)) return;
        if (!int.TryParse(_startPort.Text, out int sp) || !int.TryParse(_endPort.Text, out int ep)) return;

        _isScanning = true;
        _cts = new CancellationTokenSource();
        _resultsBody.Children.Clear();
        _progressBar.Value = 0;
        _scanStartTime = DateTime.UtcNow;
        _elapsedTime.Text = "ELAPSED: 00:00";
        _openCount.Text = "OPEN PORTS: 0";
        _statusText.Text = $"Scanning {ip}:{sp}-{ep}...";
        int totalPorts = ep - sp + 1;
        int scanned = 0;
        int found = 0;

        var throttle = new SemaphoreSlim(200);

        try
        {
            var openPorts = new ConcurrentBag<int>();
            var token = _cts.Token;
            var tasks = Enumerable.Range(sp, totalPorts).Select(async port =>
            {
                if (token.IsCancellationRequested) return;
                try
                {
                    await throttle.WaitAsync(token);
                }
                catch (OperationCanceledException) { return; }
                catch (ObjectDisposedException) { return; }

                try
                {
                    if (token.IsCancellationRequested) return;
                    using var tcp = new System.Net.Sockets.TcpClient();
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    cts.CancelAfter(500);

                    var ct = tcp.ConnectAsync(ip, port, cts.Token);
                    try
                    {
                        await ct;
                        if (tcp.Connected)
                        {
                            openPorts.Add(port);
                            int currentFound = Interlocked.Increment(ref found);
                            Dispatcher.UIThread.Post(() =>
                            {
                                _openCount.Text = $"OPEN PORTS: {currentFound}";
                                _resultsBody.Children.Add(MakeResultRow(port, ip, currentFound % 2 == 0));
                            });
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                }
                catch (OperationCanceledException) { }
                catch { }
                finally
                {
                    try { throttle.Release(); } catch (ObjectDisposedException) { }
                }

                int c = Interlocked.Increment(ref scanned);
                if (c % 50 == 0 || c == totalPorts)
                {
                    double pct = (double)c / totalPorts * 100;
                    Dispatcher.UIThread.Post(() => {
                        _progressBar.Value = pct;
                        _progressPct.Text = $"{(int)pct}%";
                        var elapsed = DateTime.UtcNow - _scanStartTime;
                        _elapsedTime.Text = $"ELAPSED: {elapsed:mm\\:ss}";
                    });
                }
            });
            await Task.WhenAll(tasks);

            if (!_cts.IsCancellationRequested)
            {
                _statusText.Text = $"Scan complete — {found} open ports found.";
                string os = PortScanner.GuessOs(openPorts.ToList());
                if (!string.IsNullOrEmpty(os)) _statusText.Text += $" (OS: {os})";
            }
        }
        catch (OperationCanceledException) { _statusText.Text = "Scan cancelled."; }
        finally
        {
            _isScanning = false;
            _progressBar.Value = _cts?.IsCancellationRequested == true ? 0 : 100;
            _progressPct.Text = _cts?.IsCancellationRequested == true ? "0%" : "100%";
            throttle.Dispose();
        }
    }

    private void OnStopScan(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _cts?.Cancel();
        _isScanning = false;
        _statusText.Text = "Scan cancelled.";
        _progressBar.Value = 0;
        _progressPct.Text = "0%";
    }

    private Border MakeProfileChip(string name, string start, string end, string tooltip)
    {
        var chip = new Border
        {
            CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 6), Background = Brushes.Transparent,
            BorderBrush = ThemeTokens.GhostBorder30, BorderThickness = new Thickness(1),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Child = new TextBlock { Text = name, FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium }
        };
        chip.PointerPressed += (s, e) => { _startPort.Text = start; _endPort.Text = end; };
        ThemeTokens.SetToolTip(chip, tooltip);
        return chip;
    }

    private static Border MakeTableHeader()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(100)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(200)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(120)));
        string[] h = { "PORT", "SERVICE", "PROTOCOL", "STATE" };
        for (int i = 0; i < h.Length; i++)
        {
            var tb = ThemeTokens.Label(h[i], 12, ThemeTokens.OnSurfaceVariant); tb.LetterSpacing = 1.5; tb.FontWeight = FontWeight.Medium; tb.Margin = new Thickness(16, 0); tb.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(tb, i); grid.Children.Add(tb);
        }
        return new Border { Background = new SolidColorBrush(Color.Parse("#070E1D"), 0.5), Padding = new Thickness(0, 14), Child = grid };
    }

    private static Border MakeResultRow(int port, string ip, bool alt)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(100)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(200)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(120)));

        bool isRisky = port == 21 || port == 23 || port == 445 || port == 1433 || port == 3389;

        var portTb = new TextBlock { Text = port.ToString(), FontSize = 15, Foreground = isRisky ? ThemeTokens.Error : ThemeTokens.Primary, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0) };
        var svcTb = new TextBlock { Text = PortScanner.GetServiceName(port), FontSize = 15, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0) };
        var protoTb = new TextBlock { Text = "TCP", FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0) };
        
        var badgeBg = isRisky ? ThemeTokens.ErrorContainer : new SolidColorBrush(Color.Parse("#005362"), 0.3);
        var badgeFg = isRisky ? ThemeTokens.Error : ThemeTokens.Tertiary;
        var stateBadge = new Border { Background = badgeBg, CornerRadius = new CornerRadius(4), Padding = new Thickness(10, 4), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0), Child = new TextBlock { Text = isRisky ? "Risk" : "Open", FontSize = 13, Foreground = badgeFg, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium } };

        if (isRisky) ThemeTokens.SetToolTip(stateBadge, "This port is associated with unencrypted or highly exploitable services.");

        Grid.SetColumn(portTb, 0); Grid.SetColumn(svcTb, 1); Grid.SetColumn(protoTb, 2); Grid.SetColumn(stateBadge, 3);
        grid.Children.Add(portTb); grid.Children.Add(svcTb); grid.Children.Add(protoTb); grid.Children.Add(stateBadge);
        return new Border { Background = alt ? ThemeTokens.SurfaceContainerLowest : Brushes.Transparent, Padding = new Thickness(0, 14), Child = grid };
    }
}
