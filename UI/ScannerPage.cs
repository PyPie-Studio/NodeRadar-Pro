using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace NodeRadarPro.UI;

/// <summary>
/// Network Scanner page — matches Network_Scanner_Form screenshot exactly.
/// IP range inputs, checkboxes, start/stop controls, progress, and results table with port badges.
/// </summary>
public class ScannerPage : Border
{
    private readonly LocalDatabase _db;
    private readonly SubnetScanner _scanner;
    private readonly List<NetworkNode> _activeNodes;
    private readonly List<NetworkNode> _scanResults = new();

    private readonly TextBox _startIp;
    private readonly TextBox _endIp;
    private readonly CheckBox _fastScanCheck;
    private readonly CheckBox _osDetectCheck;
    private readonly Button _startBtn;
    private readonly Button _stopBtn;
    private readonly TextBlock _statusText;
    private readonly TextBlock _progressPct;
    private readonly ProgressBar _progressBar;
    private readonly TextBlock _discoveredCount;
    private readonly TextBlock _elapsedTime;
    private readonly TextBlock _scanningBadgeText;
    private readonly Border _scanningBadge;
    private readonly StackPanel _resultsBody;
    private readonly TextBox _filterInput;

    private CancellationTokenSource? _scanCts;
    private bool _isScanning = false;
    private DateTime _scanStartTime;

    public event Action? DataChanged;
    public event Action<NetworkNode>? DeviceSaved;

    public ScannerPage(LocalDatabase db, SubnetScanner scanner, List<NetworkNode> activeNodes)
    {
        _db = db;
        _scanner = scanner;
        _activeNodes = activeNodes;
        Background = ThemeTokens.Surface;

        // ═══════════════════════
        // HEADER: Title + Buttons
        // ═══════════════════════
        var title = ThemeTokens.Headline("Active Sonar", 36);
        title.Margin = new Thickness(0, 0, 0, 4);
        var subtitle = ThemeTokens.Body("Deep subnet reconnaissance and port enumeration.", 14);

        _startBtn = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "▶", FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White },
                    new TextBlock { Text = "Start Scan", FontSize = 14, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White, FontFamily = new FontFamily("Inter") }
                }
            },
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse("#7C3AED"), 0), new GradientStop(Color.Parse("#6B21A8"), 1) }
            },
            Height = 44,
            Padding = new Thickness(24, 0),
            CornerRadius = new CornerRadius(8),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        _startBtn.Click += OnStartScan;

        _stopBtn = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "■", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Foreground = ThemeTokens.Error },
                    new TextBlock { Text = "Stop Scan", FontSize = 14, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Foreground = ThemeTokens.Error, FontFamily = new FontFamily("Inter") }
                }
            },
            Background = Brushes.Transparent,
            Height = 44,
            Padding = new Thickness(24, 0),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.Parse("#FFB4AB"), 0.3),
            BorderThickness = new Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        _stopBtn.Click += OnStopScan;

        var btnGroup = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Children = { _startBtn, _stopBtn }
        };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var titleGroup = new StackPanel { Children = { title, subtitle } };
        Grid.SetColumn(titleGroup, 0);
        Grid.SetColumn(btnGroup, 1);
        headerGrid.Children.Add(titleGroup);
        headerGrid.Children.Add(btnGroup);

        // ═══════════════════════
        // TARGET VECTORS CARD
        // ═══════════════════════
        string baseIp = SubnetScanner.GetLocalBaseIp();

        var vectorsTitle = ThemeTokens.Headline("Target Vectors", 20);
        vectorsTitle.Foreground = ThemeTokens.Primary;
        vectorsTitle.Margin = new Thickness(0, 0, 0, 20);

        var startLabel = ThemeTokens.Label("START IP ADDRESS", 10);
        startLabel.LetterSpacing = 1.5;
        startLabel.Margin = new Thickness(0, 0, 0, 8);

        _startIp = ThemeTokens.Input("192.168.1.1");
        _startIp.Text = $"{baseIp}.1";

        // Icon prefix for input (simulated with inline text)
        var startIpWrap = new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(ThemeTokens.InputRadius),
            Padding = new Thickness(14, 0, 0, 0),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock { Text = "◎", FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) },
                    _startIp
                }
            }
        };
        _startIp.Background = Brushes.Transparent;
        _startIp.Padding = new Thickness(0, 10);

        var endLabel = ThemeTokens.Label("END IP ADDRESS", 10);
        endLabel.LetterSpacing = 1.5;
        endLabel.Margin = new Thickness(0, 0, 0, 8);

        _endIp = ThemeTokens.Input("192.168.1.254");
        _endIp.Text = $"{baseIp}.254";

        var endIpWrap = new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(ThemeTokens.InputRadius),
            Padding = new Thickness(14, 0, 0, 0),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock { Text = "◎", FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) },
                    _endIp
                }
            }
        };
        _endIp.Background = Brushes.Transparent;
        _endIp.Padding = new Thickness(0, 10);

        var dash = new TextBlock
        {
            Text = "—",
            FontSize = 20,
            Foreground = ThemeTokens.Outline,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(12, 24, 12, 0)
        };

        var ipGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star))
            }
        };

        var startCol = new StackPanel { Children = { startLabel, startIpWrap } };
        var endCol = new StackPanel { Children = { endLabel, endIpWrap } };
        Grid.SetColumn(startCol, 0);
        Grid.SetColumn(dash, 1);
        Grid.SetColumn(endCol, 2);
        ipGrid.Children.Add(startCol);
        ipGrid.Children.Add(dash);
        ipGrid.Children.Add(endCol);

        // Checkboxes row
        _fastScanCheck = new CheckBox
        {
            Content = "Fast Scan (Top 100 ports)",
            Foreground = ThemeTokens.OnSurface,
            FontSize = 13,
            FontFamily = new FontFamily("Inter"),
            Margin = new Thickness(0, 0, 24, 0)
        };

        _osDetectCheck = new CheckBox
        {
            Content = "OS Detection",
            Foreground = ThemeTokens.OnSurface,
            FontSize = 13,
            FontFamily = new FontFamily("Inter"),
            IsChecked = true
        };

        var checkboxRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 16, 0, 0),
            Children = { _fastScanCheck, _osDetectCheck }
        };

        var vectorsContent = new StackPanel { Children = { vectorsTitle, ipGrid, checkboxRow } };
        var vectorsCard = ThemeTokens.Card(vectorsContent, ThemeTokens.SurfaceContainerLow, 28);

        // ═══════════════════════
        // SWEEP STATUS CARD
        // ═══════════════════════
        var sweepTitle = ThemeTokens.Headline("Sweep Status", 18);

        _scanningBadgeText = new TextBlock
        {
            Text = "Idle",
            FontSize = 11,
            Foreground = ThemeTokens.Tertiary,
            FontFamily = new FontFamily("Inter"),
            FontWeight = FontWeight.Medium
        };
        _scanningBadge = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#005362"), 0.3),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 4),
            Child = _scanningBadgeText
        };

        var sweepHeader = new Grid();
        sweepHeader.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        sweepHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(sweepTitle, 0);
        Grid.SetColumn(_scanningBadge, 1);
        sweepHeader.Children.Add(sweepTitle);
        sweepHeader.Children.Add(_scanningBadge);

        _statusText = ThemeTokens.Body("Ready to scan.", 13);
        _statusText.Margin = new Thickness(0, 6, 0, 20);

        var progressLabel = new TextBlock
        {
            Text = "Progress",
            FontSize = 12,
            Foreground = ThemeTokens.OnSurfaceVariant,
            FontFamily = new FontFamily("Inter"),
            FontWeight = FontWeight.Medium
        };
        _progressPct = new TextBlock
        {
            Text = "0%",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeTokens.Primary,
            FontFamily = new FontFamily("Inter")
        };

        var progHeader = new Grid();
        progHeader.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        progHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(progressLabel, 0);
        Grid.SetColumn(_progressPct, 1);
        progHeader.Children.Add(progressLabel);
        progHeader.Children.Add(_progressPct);

        _progressBar = new ProgressBar
        {
            Minimum = 0, Maximum = 100, Value = 0,
            Height = 8,
            Foreground = ThemeTokens.Tertiary,
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 6, 0, 0)
        };

        _discoveredCount = ThemeTokens.Label("DISCOVERED: 0", 10);
        _discoveredCount.LetterSpacing = 1.2;
        _elapsedTime = ThemeTokens.Label("ELAPSED: 00:00", 10);
        _elapsedTime.LetterSpacing = 1.2;
        _elapsedTime.HorizontalAlignment = HorizontalAlignment.Right;

        var footerStats = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        footerStats.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        footerStats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(_discoveredCount, 0);
        Grid.SetColumn(_elapsedTime, 1);
        footerStats.Children.Add(_discoveredCount);
        footerStats.Children.Add(_elapsedTime);

        var sweepContent = new StackPanel { Children = { sweepHeader, _statusText, progHeader, _progressBar, footerStats } };
        var sweepCard = ThemeTokens.GlassCard(sweepContent, 28);

        // Bento row
        var bentoGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(2, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star))
            },
            Margin = new Thickness(0, 20, 0, 0)
        };
        Grid.SetColumn(vectorsCard, 0);
        Grid.SetColumn(sweepCard, 1);
        vectorsCard.Margin = new Thickness(0, 0, 10, 0);
        sweepCard.Margin = new Thickness(10, 0, 0, 0);
        bentoGrid.Children.Add(vectorsCard);
        bentoGrid.Children.Add(sweepCard);

        // ═══════════════════════
        // TABLE: Discovered Entities
        // ═══════════════════════
        var tableTitle = ThemeTokens.Headline("Discovered Entities", 20);
        tableTitle.FontWeight = FontWeight.Bold;

        _filterInput = ThemeTokens.Input("Filter results...");
        _filterInput.Width = 180;
        _filterInput.FontSize = 12;
        _filterInput.Padding = new Thickness(10, 6);
        _filterInput.TextChanged += (s, e) => RefreshFilteredResults();

        var searchIcon = new TextBlock
        {
            Text = "🔍",
            FontSize = 12,
            Foreground = ThemeTokens.OnSurfaceVariant,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var exportBtn = new Button
        {
            Content = "⬇",
            Background = Brushes.Transparent,
            Foreground = ThemeTokens.OnSurfaceVariant,
            Width = 36, Height = 36,
            FontSize = 16,
            CornerRadius = new CornerRadius(6),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        exportBtn.Click += OnExportResults;

        var filterRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { searchIcon, _filterInput, exportBtn }
        };

        var tableTitleRow = new Grid { Margin = new Thickness(20, 16) };
        tableTitleRow.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        tableTitleRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(tableTitle, 0);
        Grid.SetColumn(filterRow, 1);
        filterRow.VerticalAlignment = VerticalAlignment.Center;
        tableTitleRow.Children.Add(tableTitle);
        tableTitleRow.Children.Add(filterRow);

        var tableHeader = MakeTableHeader();
        _resultsBody = new StackPanel { Spacing = 0 };

        var tableScroll = new ScrollViewer
        {
            Content = _resultsBody,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var tableDock = new DockPanel();
        DockPanel.SetDock(tableTitleRow, Dock.Top);
        DockPanel.SetDock(tableHeader, Dock.Top);
        tableDock.Children.Add(tableTitleRow);
        tableDock.Children.Add(tableHeader);
        tableDock.Children.Add(tableScroll);

        var tableCard = ThemeTokens.Card(tableDock, ThemeTokens.SurfaceContainerLow, 0);
        tableCard.Margin = new Thickness(0, 20, 0, 0);

        // ═══════════════════════
        // ROOT
        // ═══════════════════════
        var root = new DockPanel { Margin = new Thickness(32, 28) };
        DockPanel.SetDock(headerGrid, Dock.Top);
        DockPanel.SetDock(bentoGrid, Dock.Top);
        headerGrid.Margin = new Thickness(0, 0, 0, 0);
        root.Children.Add(headerGrid);
        root.Children.Add(bentoGrid);
        root.Children.Add(tableCard);

        Child = new ScrollViewer
        {
            Content = root,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        _scanner.ProgressUpdated += OnProgress;
        _scanner.NodeDiscovered += OnNodeDiscovered;
    }

    private void OnProgress(double pct)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _progressBar.Value = pct;
            _progressPct.Text = $"{(int)pct}%";
            var elapsed = DateTime.UtcNow - _scanStartTime;
            _elapsedTime.Text = $"ELAPSED: {elapsed:mm\\:ss}";
        });
    }

    private void OnNodeDiscovered(NetworkNode node)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var (mergedNode, isNew) = _db.MergeWithHistory(node);
            node = mergedNode;

            var existing = _activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
            if (existing >= 0) _activeNodes[existing] = node;
            else _activeNodes.Add(node);

            _scanResults.Add(node);
            _discoveredCount.Text = $"DISCOVERED: {_scanResults.Count}";
            _resultsBody.Children.Add(MakeTableRow(node, _scanResults.Count % 2 == 0));
            DataChanged?.Invoke();
        });
    }

    private async void OnStartScan(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isScanning) return;
        string startIp = _startIp.Text?.Trim() ?? "";
        string endIp = _endIp.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(startIp) || string.IsNullOrEmpty(endIp)) return;

        _isScanning = true;
        _scanCts = new CancellationTokenSource();
        _scanResults.Clear();
        _resultsBody.Children.Clear();
        _progressBar.Value = 0;
        _scanStartTime = DateTime.UtcNow;
        _statusText.Text = $"Probing {startIp}...";
        _scanningBadgeText.Text = "Scanning";
        _scanningBadge.Background = new SolidColorBrush(Color.Parse("#6B21A8"), 0.4);
        _scanningBadgeText.Foreground = ThemeTokens.Primary;

        // Wire checkboxes to scanner settings
        _scanner.FastScanMode = _fastScanCheck.IsChecked == true;
        _scanner.EnableOsDetection = _osDetectCheck.IsChecked == true;
        _scanner.EnableInlinePortScan = _fastScanCheck.IsChecked == true; // Enable port scan when Fast Scan is checked

        string[] startParts = startIp.Split('.');
        string[] endParts = endIp.Split('.');

        try
        {
            if (startParts.Length == 4 && endParts.Length == 4 &&
                int.TryParse(startParts[3], out int rStart) && int.TryParse(endParts[3], out int rEnd))
            {
                string baseIp = $"{startParts[0]}.{startParts[1]}.{startParts[2]}";
                await _scanner.ScanRangeAsync(baseIp, rStart, rEnd, _scanCts.Token);
            }
            else
            {
                await _scanner.ScanSubnetAsync(SubnetScanner.GetLocalBaseIp(), _scanCts.Token);
            }

            if (!_scanCts.IsCancellationRequested)
                _statusText.Text = $"Scan complete — {_scanResults.Count} devices found.";
        }
        catch (OperationCanceledException)
        {
            _statusText.Text = "Scan cancelled.";
        }
        finally
        {
            _isScanning = false;
            _progressBar.Value = _scanCts?.IsCancellationRequested == true ? 0 : 100;
            _progressPct.Text = _scanCts?.IsCancellationRequested == true ? "0%" : "100%";
            _scanningBadgeText.Text = "Idle";
            _scanningBadge.Background = new SolidColorBrush(Color.Parse("#005362"), 0.3);
            _scanningBadgeText.Foreground = ThemeTokens.Tertiary;
            DataChanged?.Invoke();
        }
    }

    private void OnStopScan(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _scanCts?.Cancel();
        _isScanning = false; // Allow immediate restart
        _statusText.Text = "Scan cancelled.";
        _scanningBadgeText.Text = "Idle";
        _scanningBadge.Background = new SolidColorBrush(Color.Parse("#005362"), 0.3);
        _scanningBadgeText.Foreground = ThemeTokens.Tertiary;
    }

    private void RefreshFilteredResults()
    {
        string filter = _filterInput.Text?.Trim().ToLower() ?? "";
        _resultsBody.Children.Clear();
        int idx = 0;
        foreach (var node in _scanResults)
        {
            if (!string.IsNullOrEmpty(filter) &&
                !node.IpAddress.ToLower().Contains(filter) &&
                !node.MacAddress.ToLower().Contains(filter) &&
                !node.DisplayName.ToLower().Contains(filter))
                continue;
            _resultsBody.Children.Add(MakeTableRow(node, idx++ % 2 == 0));
        }
    }

    private static Border MakeTableHeader()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(60)));   // Status
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(140)));  // IP
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(170)));  // MAC
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star))); // Device
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(160)));  // Open Ports
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(70)));   // Action

        string[] headers = { "STATUS", "IP ADDRESS", "MAC ADDRESS", "DEVICE IDENTITY", "OPEN PORTS", "ACTION" };
        for (int i = 0; i < headers.Length; i++)
        {
            var tb = ThemeTokens.Label(headers[i], 10, ThemeTokens.OnSurfaceVariant);
            tb.LetterSpacing = 1.5;
            tb.FontWeight = FontWeight.Medium;
            tb.Margin = new Thickness(16, 0);
            tb.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(tb, i);
            grid.Children.Add(tb);
        }

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#070E1D"), 0.5),
            Padding = new Thickness(0, 12),
            Child = grid
        };
    }

    private Border MakeTableRow(NetworkNode node, bool alternate)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(60)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(140)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(170)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(160)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(70)));

        // Status dot
        var dot = ThemeTokens.StatusDot(node.IsOnline, 10);
        dot.Margin = new Thickness(24, 0, 0, 0);
        dot.VerticalAlignment = VerticalAlignment.Center;

        // IP
        var ipText = new TextBlock { Text = node.IpAddress, FontSize = 13, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0) };

        // MAC
        var macText = new TextBlock { Text = node.MacAddress, FontSize = 12, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0) };

        // Device identity with icon
        string devIcon = node.DeviceType switch
        {
            "Router" => "⊞",
            "Phone" or "Mobile" => "📱",
            "Computer" or "Desktop" => "🖥",
            _ => "⊟"
        };
        var devPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0),
            Children =
            {
                new TextBlock { Text = devIcon, FontSize = 14, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = node.DisplayName, FontSize = 13, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter"), TextTrimming = TextTrimming.CharacterEllipsis }
            }
        };

        // Open Ports — show actual discovered open ports as badges
        var portsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0)
        };
        if (node.OpenPorts != null && node.OpenPorts.Count > 0)
        {
            int shown = 0;
            foreach (var port in node.OpenPorts)
            {
                if (shown >= 4) { portsPanel.Children.Add(new TextBlock { Text = $"+{node.OpenPorts.Count - shown}", FontSize = 12, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Center }); break; }
                portsPanel.Children.Add(MakePortBadge(port, port == 80 || port == 443 || port == 3389));
                shown++;
            }
        }
        else
        {
            portsPanel.Children.Add(new TextBlock { Text = "—", FontSize = 13, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Center });
        }

        // I9: Action column — Save button to register device
        var saveBtn = new Button
        {
            Content = "💾",
            FontSize = 13,
            Background = Brushes.Transparent,
            Foreground = ThemeTokens.Tertiary,
            Width = 32, Height = 28,
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0)
        };
        saveBtn.Click += (s, e) =>
        {
            node.IsRegistered = true;
            DeviceSaved?.Invoke(node);
            saveBtn.Content = "✓";
            saveBtn.IsEnabled = false;
        };

        Grid.SetColumn(dot, 0);
        Grid.SetColumn(ipText, 1);
        Grid.SetColumn(macText, 2);
        Grid.SetColumn(devPanel, 3);
        Grid.SetColumn(portsPanel, 4);
        Grid.SetColumn(saveBtn, 5);

        grid.Children.Add(dot);
        grid.Children.Add(ipText);
        grid.Children.Add(macText);
        grid.Children.Add(devPanel);
        grid.Children.Add(portsPanel);
        grid.Children.Add(saveBtn);

        return new Border
        {
            Background = alternate ? ThemeTokens.SurfaceContainerLowest : Brushes.Transparent,
            Padding = new Thickness(0, 14),
            Child = grid
        };
    }

    /// <summary>Helper: creates a port badge pill</summary>
    public static Border MakePortBadge(int port, bool isHighlight = false)
    {
        return new Border
        {
            Background = isHighlight
                ? new SolidColorBrush(Color.Parse("#6B21A8"), 0.4)
                : new SolidColorBrush(Color.Parse("#232A3A")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3),
            Child = new TextBlock
            {
                Text = port.ToString(),
                FontSize = 11,
                Foreground = isHighlight ? ThemeTokens.Primary : ThemeTokens.OnSurface,
                FontFamily = new FontFamily("Inter"),
                FontWeight = FontWeight.SemiBold
            }
        };
    }

    private void OnExportResults(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_scanResults.Count == 0) return;
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("IP Address,MAC Address,Hostname,Vendor,Device Type,Status,Latency (ms),Open Ports,OS Guess");
            foreach (var node in _scanResults)
            {
                string ports = node.OpenPorts?.Count > 0 ? string.Join(";", node.OpenPorts) : "";
                sb.AppendLine($"\"{node.IpAddress}\",\"{node.MacAddress}\",\"{node.Hostname}\",\"{node.Vendor}\",\"{node.DeviceType}\",\"{(node.IsOnline ? "Online" : "Offline")}\",\"{node.PingLatencyMs}\",\"{ports}\",\"{node.OsGuess}\"");
            }
            string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro");
            System.IO.Directory.CreateDirectory(folder);
            string path = System.IO.Path.Combine(folder, $"scan_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            System.IO.File.WriteAllText(path, sb.ToString());
            _statusText.Text = $"Exported {_scanResults.Count} devices to {path}";
        }
        catch (Exception ex) { _statusText.Text = $"Export failed: {ex.Message}"; }
    }
}
