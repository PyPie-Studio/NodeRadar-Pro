using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using Avalonia.Threading;

namespace NodeRadarPro.UI;

/// <summary>
/// Main application layout — dark purple theme with drag-resizable panels.
/// 5-column grid: Sidebar | Splitter | Radar | Splitter | Detail Panel
/// </summary>
public class DarkPurpleTheme
{
    // ── PyPie Studio Branding ──
    public static readonly IBrush BgDark = SolidColorBrush.Parse("#1A1A2E"); 
    public static readonly IBrush BgSidebar = SolidColorBrush.Parse("#111120");
    public static readonly IBrush AccentPurple = SolidColorBrush.Parse("#8A2BE2");
    public static readonly IBrush AccentPurpleHover = SolidColorBrush.Parse("#9B3CF3");
    public static readonly IBrush TextWhite = Brushes.WhiteSmoke;
    public static readonly IBrush SubtextGrey = SolidColorBrush.Parse("#888899");
    public static readonly IBrush OnlineGreen = SolidColorBrush.Parse("#00FFcc");
    public static readonly IBrush OfflineRed = SolidColorBrush.Parse("#FF4444");
    public static readonly IBrush SeparatorColor = SolidColorBrush.Parse("#1E1E35");
    public static readonly IBrush SplitterColor = SolidColorBrush.Parse("#252540");

    public static Window BuildMainWindow()
    {
        var window = new Window
        {
            Title = "NodeRadar Pro — Network Monitor",
            Width = 1340,
            Height = 840,
            MinWidth = 720,
            MinHeight = 500,
            Background = BgDark,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            // DPI scaling handled automatically by Avalonia — we just use logical units
        };

        // ── Core Services ──
        var db = new LocalDatabase();
        var scanner = new SubnetScanner();
        var monitor = new ConnectivityMonitor();
        var settings = db.LoadSettings();
        monitor.IntervalSeconds = settings.MonitorIntervalSeconds;

        var activeNodes = new List<NetworkNode>();
        var cts = new CancellationTokenSource();

        // ═══════════════════════════════════════════
        // ██  SIDEBAR (Left Column)
        // ═══════════════════════════════════════════

        var appTitle = new TextBlock
        {
            Text = "NodeRadar Pro",
            Foreground = AccentPurple,
            FontSize = 21,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(20, 20, 20, 2)
        };

        var appSubtitle = new TextBlock
        {
            Text = "by PyPie Studio",
            Foreground = SubtextGrey,
            FontSize = 10.5,
            Margin = new Thickness(20, 0, 20, 18)
        };

        var btn_Scan = new Button
        {
            Content = "⚡  SCAN NETWORK",
            Background = AccentPurple,
            Foreground = TextWhite,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 44,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(16, 0, 16, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        };

        var progressBar = new ProgressBar
        {
            Minimum = 0, Maximum = 100, Value = 0,
            Height = 5,
            Margin = new Thickness(16, 10, 16, 0),
            Foreground = AccentPurple,
            Background = SolidColorBrush.Parse("#1E1E33"),
            CornerRadius = new CornerRadius(3)
        };

        var txt_Count = new TextBlock
        {
            Text = "0 devices found",
            Foreground = SubtextGrey,
            FontSize = 11.5,
            Margin = new Thickness(18, 6, 18, 0)
        };

        // ── Device List Header with Add Button ──
        var deviceListTitle = new TextBlock
        {
            Text = "DEVICES",
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Foreground = SolidColorBrush.Parse("#555566"),
            VerticalAlignment = VerticalAlignment.Center,
            LetterSpacing = 1.5
        };

        var btn_AddDevice = new Button
        {
            Content = "+",
            Background = SolidColorBrush.Parse("#252540"),
            Foreground = TextWhite,
            Width = 28, Height = 28,
            CornerRadius = new CornerRadius(14),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 16,
            Padding = new Thickness(0)
        };

        var deviceListHeader = new DockPanel { Margin = new Thickness(16, 14, 16, 6) };
        DockPanel.SetDock(deviceListTitle, Dock.Left);
        DockPanel.SetDock(btn_AddDevice, Dock.Right);
        deviceListHeader.Children.Add(btn_AddDevice);
        deviceListHeader.Children.Add(deviceListTitle);

        var deviceList = new DeviceListPanel();

        var btn_Settings = new Button
        {
            Content = "⚙  Settings",
            Background = SolidColorBrush.Parse("#1A1A30"),
            Foreground = SubtextGrey,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 36,
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(16, 4, 16, 8),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 12
        };

        // ── Sidebar Assembly ──
        var sidebarTop = new StackPanel
        {
            Spacing = 0,
            Children =
            {
                appTitle, appSubtitle, btn_Scan, progressBar, txt_Count,
                new Border { Height = 1, Background = SeparatorColor, Margin = new Thickness(16, 12, 16, 0) },
                deviceListHeader
            }
        };
        DockPanel.SetDock(sidebarTop, Dock.Top);

        var sidebarBottom = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Children =
            {
                new Border { Height = 1, Background = SeparatorColor, Margin = new Thickness(16, 0) },
                btn_Settings
            }
        };
        DockPanel.SetDock(sidebarBottom, Dock.Bottom);

        var sidebar = new DockPanel
        {
            Background = BgSidebar,
            LastChildFill = true
        };
        sidebar.Children.Add(sidebarTop);
        sidebar.Children.Add(sidebarBottom);
        sidebar.Children.Add(deviceList);

        // ═══════════════════════════════════════════
        // ██  RADAR CANVAS (Center)
        // ═══════════════════════════════════════════

        var radarCanvas = new RadarCanvas();

        // ── Status Bar ──
        var statusOnline = new TextBlock { Text = "● 0 Online", Foreground = OnlineGreen, FontSize = 11, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        var statusOffline = new TextBlock { Text = "● 0 Offline", Foreground = OfflineRed, FontSize = 11, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        var statusMonitor = new TextBlock { Text = "Monitor: Idle", Foreground = SubtextGrey, FontSize = 11, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        var statusLastScan = new TextBlock { Text = "", Foreground = SubtextGrey, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };

        var statusBar = new Border
        {
            Background = SolidColorBrush.Parse("#0E0E1C"),
            Padding = new Thickness(16, 7),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { statusOnline, statusOffline, statusMonitor, statusLastScan }
            }
        };

        var mainArea = new DockPanel { Background = BgDark };
        DockPanel.SetDock(statusBar, Dock.Bottom);
        mainArea.Children.Add(statusBar);
        mainArea.Children.Add(radarCanvas);

        // ═══════════════════════════════════════════
        // ██  RIGHT PANEL (Detail / Settings)
        // ═══════════════════════════════════════════

        var detailPanel = new DeviceDetailPanel(db, monitor);
        var settingsPanel = new SettingsPanel(db);

        var rightPanelContainer = new Grid
        {
            MinWidth = 0,
            Background = SolidColorBrush.Parse("#151525")
        };
        rightPanelContainer.Children.Add(settingsPanel);
        rightPanelContainer.Children.Add(detailPanel);

        // ═══════════════════════════════════════════
        // ██  ROOT LAYOUT — 5-column Grid with splitters
        // ═══════════════════════════════════════════

        var sidebarColumn = new ColumnDefinition { Width = new GridLength(270), MinWidth = 180, MaxWidth = 450 };
        var leftSplitterCol = new ColumnDefinition(GridLength.Auto);
        var radarColumn = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 200 };
        var rightSplitterCol = new ColumnDefinition(GridLength.Auto);
        var detailColumn = new ColumnDefinition { Width = new GridLength(330), MinWidth = 0, MaxWidth = 520 };

        var rootGrid = new Grid
        {
            ColumnDefinitions = { sidebarColumn, leftSplitterCol, radarColumn, rightSplitterCol, detailColumn }
        };

        // ── GridSplitter: drag-to-resize sidebar ──
        var leftSplitter = new GridSplitter
        {
            Width = 5,
            Background = SplitterColor,
            ResizeDirection = GridResizeDirection.Columns,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeWestEast)
        };

        // ── GridSplitter: drag-to-resize detail panel ──
        var rightSplitter = new GridSplitter
        {
            Width = 5,
            Background = SplitterColor,
            ResizeDirection = GridResizeDirection.Columns,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeWestEast)
        };

        Grid.SetColumn(sidebar, 0);
        Grid.SetColumn(leftSplitter, 1);
        Grid.SetColumn(mainArea, 2);
        Grid.SetColumn(rightSplitter, 3);
        Grid.SetColumn(rightPanelContainer, 4);

        rootGrid.Children.Add(sidebar);
        rootGrid.Children.Add(leftSplitter);
        rootGrid.Children.Add(mainArea);
        rootGrid.Children.Add(rightSplitter);
        rootGrid.Children.Add(rightPanelContainer);

        window.Content = rootGrid;

        // ═══════════════════════════════════════════
        // ██  EVENT WIRING
        // ═══════════════════════════════════════════

        IntrusionAlerter.Initialize(window);

        // ── Helper: Update status bar ──
        void UpdateStatusBar()
        {
            int online = activeNodes.Count(n => n.IsOnline);
            int offline = activeNodes.Count(n => !n.IsOnline);
            Dispatcher.UIThread.Post(() =>
            {
                statusOnline.Text = $"● {online} Online";
                statusOffline.Text = $"● {offline} Offline";
            });
        }

        // ── Helper: Filter nodes for radar (respecting fade-out setting) ──
        List<NetworkNode> GetVisibleNodes()
        {
            if (!settings.EnableOfflineFadeOut) return activeNodes;
            return activeNodes.Where(n =>
            {
                if (n.IsOnline) return true;
                var offline = DateTime.UtcNow - n.LastSeen;
                return offline.TotalSeconds <= settings.FadeOutSeconds;
            }).ToList();
        }

        // ── Helper: Refresh all UI components ──
        void RefreshAll()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var visible = GetVisibleNodes();
                radarCanvas.UpdateNodes(visible);
                deviceList.UpdateDevices(visible);
                txt_Count.Text = $"{activeNodes.Count} devices found";
                UpdateStatusBar();
            });
        }

        // ── Scanner Events ──
        scanner.ProgressUpdated += (percentage) =>
        {
            Dispatcher.UIThread.Post(() => progressBar.Value = percentage);
        };

        scanner.NodeDiscovered += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                node.Vendor = VendorLookup.GetVendor(node.MacAddress);
                node.DeviceType = VendorLookup.GuessDeviceType(node.Vendor, node.Hostname);
                node = db.MergeWithHistory(node);
                
                var existing = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
                if (existing >= 0)
                    activeNodes[existing] = node;
                else
                    activeNodes.Add(node);

                RefreshAll();
            });
        };

        // ── Scan Button ──
        btn_Scan.Click += async (sender, e) =>
        {
            // Prompt the user for scan type (All vs Range)
            var scanWindow = new Window
            {
                Title = "Scan Options",
                Width = 350,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = BgDark,
                WindowDecorations = WindowDecorations.BorderOnly
            };

            var optTitle = new TextBlock { Text = "Choose Scan Target", Foreground = TextWhite, FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0,0,0,10) };
            
            var btnAll = new Button { Content = "Scan All Subnets", Background = AccentPurple, Foreground = TextWhite, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0,5), Height = 35 };
            
            var rangePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0,10) };
            var txtBase = new TextBox { Text = SubnetScanner.GetLocalBaseIp(), Width = 120, Background = BgDark, Foreground = TextWhite };
            var txtStart = new TextBox { Text = "1", Width = 50, Background = BgDark, Foreground = TextWhite };
            var txtEnd = new TextBox { Text = "254", Width = 50, Background = BgDark, Foreground = TextWhite };
            rangePanel.Children.Add(txtBase);
            rangePanel.Children.Add(new TextBlock { Text=".", VerticalAlignment = VerticalAlignment.Center, Foreground=TextWhite});
            rangePanel.Children.Add(txtStart);
            rangePanel.Children.Add(new TextBlock { Text="-", VerticalAlignment = VerticalAlignment.Center, Foreground=TextWhite});
            rangePanel.Children.Add(txtEnd);

            var btnRange = new Button { Content = "Scan Range", Background = SolidColorBrush.Parse("#252540"), Foreground = TextWhite, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0,5), Height = 35 };
            var btnCancel = new Button { Content = "Cancel", Background = SolidColorBrush.Parse("#D32F2F"), Foreground = TextWhite, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0,15,0,0), Height = 35 };

            var mainStack = new StackPanel { Margin = new Thickness(20), Children = { optTitle, btnAll, rangePanel, btnRange, btnCancel } };
            scanWindow.Content = mainStack;

            bool isCanceled = true;
            bool isRangeScan = false;
            string rangeBase = "";
            int rStart = 1, rEnd = 254;

            btnCancel.Click += (s, ev) => scanWindow.Close();
            btnAll.Click += (s, ev) => { isCanceled = false; isRangeScan = false; scanWindow.Close(); };
            btnRange.Click += (s, ev) => {
                if (int.TryParse(txtStart.Text, out rStart) && int.TryParse(txtEnd.Text, out rEnd))
                {
                    rangeBase = txtBase.Text ?? "";
                    isCanceled = false;
                    isRangeScan = true;
                    scanWindow.Close();
                }
            };

            await scanWindow.ShowDialog(window);

            if (isCanceled) return;

            btn_Scan.IsEnabled = false;
            btn_Scan.Content = "⏳  SCANNING...";

            // Preserve registered devices, mark offline until confirmed
            foreach (var node in activeNodes)
            {
                if (node.IsRegistered) node.IsOnline = false;
            }
            activeNodes.RemoveAll(n => !n.IsRegistered);

            radarCanvas.UpdateNodes(activeNodes);
            deviceList.UpdateDevices(activeNodes);
            txt_Count.Text = $"{activeNodes.Count} devices found";
            progressBar.Value = 0;

            if (isRangeScan)
            {
                await scanner.ScanRangeAsync(rangeBase, rStart, rEnd);
            }
            else
            {
                string localSubnet = SubnetScanner.GetLocalBaseIp();
                await scanner.ScanSubnetAsync(localSubnet);
            }

            btn_Scan.Content = "⚡  SCAN NETWORK";
            btn_Scan.IsEnabled = true;
            statusLastScan.Text = $"Last scan: {DateTime.Now:HH:mm:ss}";

            // Feed results to monitor
            monitor.UpdateTrackedDevices(activeNodes);

            // Start monitoring if not already running
            if (!monitor.IsRunning)
            {
                statusMonitor.Text = $"Monitor: Active ({monitor.IntervalSeconds}s)";
                _ = Task.Run(() => monitor.StartMonitoringAsync(cts.Token));
            }
        };

        // ── Connectivity Monitor Events ──
        monitor.DeviceWentOffline += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IntrusionAlerter.AlertDeviceOffline(node);
                var idx = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
                if (idx >= 0) activeNodes[idx] = node;
                RefreshAll();
                detailPanel.RefreshStatus(node);
            });
        };

        monitor.DeviceCameOnline += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IntrusionAlerter.AlertDeviceReconnected(node);
                var idx = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
                if (idx >= 0) activeNodes[idx] = node;
                RefreshAll();
                detailPanel.RefreshStatus(node);
            });
        };

        monitor.StatusUpdated += (updatedNodes) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var updated in updatedNodes)
                {
                    var idx = activeNodes.FindIndex(n => n.MacAddress == updated.MacAddress);
                    if (idx >= 0) activeNodes[idx] = updated;
                }
                RefreshAll();
            });
        };

        // ── Device Selection ──
        Action<NetworkNode> showDeviceDetail = (node) =>
        {
            settingsPanel.IsVisible = false;
            detailPanel.ShowDevice(node);
            radarCanvas.SelectNode(node.MacAddress);
            deviceList.SelectDevice(node.MacAddress);
        };

        deviceList.DeviceSelected += showDeviceDetail;
        deviceList.DeviceRightClicked += showDeviceDetail;
        radarCanvas.NodeSelected += showDeviceDetail;
        radarCanvas.NodeRightClicked += showDeviceDetail;

        // ── Detail Panel Events ──
        detailPanel.CloseRequested += () =>
        {
            radarCanvas.SelectNode(null);
            deviceList.SelectDevice(null);
        };

        detailPanel.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            RefreshAll();
        };

        detailPanel.DeviceStatusChanged += (node) =>
        {
            var idx = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
            if (idx >= 0) activeNodes[idx] = node;
            RefreshAll();
        };

        detailPanel.DeviceDeleted += (node) =>
        {
            activeNodes.RemoveAll(n => n.MacAddress == node.MacAddress);
            radarCanvas.SelectNode(null);
            deviceList.SelectDevice(null);
            RefreshAll();
        };

        // ── Settings Panel ──
        btn_Settings.Click += (s, e) =>
        {
            detailPanel.IsVisible = false;
            radarCanvas.SelectNode(null);
            deviceList.SelectDevice(null);
            settingsPanel.Show();
        };

        settingsPanel.CloseRequested += () => { };

        settingsPanel.SettingsSaved += (newSettings) =>
        {
            settings = newSettings;
            monitor.IntervalSeconds = newSettings.MonitorIntervalSeconds;
            statusMonitor.Text = $"Monitor: Active ({monitor.IntervalSeconds}s)";
            RefreshAll();
        };

        // ── Add Device Button ──
        btn_AddDevice.Click += (s, e) =>
        {
            settingsPanel.IsVisible = false;

            var manualNode = new NetworkNode
            {
                IpAddress = "0.0.0.0",
                MacAddress = $"MANUAL-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                Hostname = "Manual Entry",
                IsOnline = false,
                IsRegistered = true,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            activeNodes.Add(manualNode);
            RefreshAll();
            detailPanel.ShowDevice(manualNode);
        };

        // ── Load saved devices + start background monitoring on startup ──
        window.Opened += (s, e) =>
        {
            var knownDevices = db.GetAllKnownDevices();
            foreach (var device in knownDevices)
            {
                device.IsOnline = false; // Will be confirmed by monitor
                if (!activeNodes.Any(n => n.MacAddress == device.MacAddress))
                {
                    activeNodes.Add(device);
                }
            }

            if (activeNodes.Count > 0)
            {
                RefreshAll();

                // Start background monitoring immediately so saved devices
                // get their status checked without waiting for a manual scan
                monitor.UpdateTrackedDevices(activeNodes);
                statusMonitor.Text = $"Monitor: Active ({monitor.IntervalSeconds}s)";
                _ = Task.Run(() => monitor.StartMonitoringAsync(cts.Token));
            }
        };

        // ── Cleanup ──
        window.Closing += (s, e) =>
        {
            cts.Cancel();
        };

        return window;
    }
}