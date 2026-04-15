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
/// A pure C# fluent UI layout matching the PyPie Studio dark purple theme.
/// Full 3-column layout: Sidebar → Radar → Detail/Settings Panel.
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

    public static Window BuildMainWindow()
    {
        var window = new Window
        {
            Title = "NodeRadar Pro | PyPie Studio",
            Width = 1280,
            Height = 800,
            MinWidth = 900,
            MinHeight = 600,
            Background = BgDark,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
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
        // ██  SIDEBAR (Left Column - 260px)
        // ═══════════════════════════════════════════

        // ── App Title ──
        var appTitle = new TextBlock
        {
            Text = "NodeRadar Pro",
            Foreground = AccentPurple,
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(20, 24, 20, 4)
        };

        var appSubtitle = new TextBlock
        {
            Text = "by PyPie Studio",
            Foreground = SubtextGrey,
            FontSize = 11,
            Margin = new Thickness(20, 0, 20, 20)
        };

        // ── Scan Button ──
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
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Name = "btn_Scan"
        };

        // ── Progress Bar ──
        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 6,
            Margin = new Thickness(16, 10, 16, 0),
            Foreground = AccentPurple,
            Background = SolidColorBrush.Parse("#252535"),
            CornerRadius = new CornerRadius(3)
        };

        // ── Device Count ──
        var txt_Count = new TextBlock
        {
            Text = "0 devices found",
            Foreground = SubtextGrey,
            FontSize = 12,
            Margin = new Thickness(18, 6, 18, 0),
            Name = "txt_Count"
        };

        // ── Device List Header ──
        var deviceListHeader = new DockPanel
        {
            Margin = new Thickness(16, 16, 16, 6)
        };
        
        var deviceListTitle = new TextBlock
        {
            Text = "DEVICES",
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Foreground = SolidColorBrush.Parse("#555566"),
            VerticalAlignment = VerticalAlignment.Center,
            LetterSpacing = 1.5
        };
        DockPanel.SetDock(deviceListTitle, Dock.Left);
        deviceListHeader.Children.Add(deviceListTitle);

        // ── Add Device Button ──
        var btn_AddDevice = new Button
        {
            Content = "+",
            Background = SolidColorBrush.Parse("#252540"),
            Foreground = TextWhite,
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 16,
            Padding = new Thickness(0)
        };
        DockPanel.SetDock(btn_AddDevice, Dock.Right);
        deviceListHeader.Children.Add(btn_AddDevice);

        // ── Device List Panel ──
        var deviceList = new DeviceListPanel();

        // ── Sidebar Bottom Buttons ──
        var btn_Settings = new Button
        {
            Content = "⚙  Settings",
            Background = SolidColorBrush.Parse("#1A1A30"),
            Foreground = SubtextGrey,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 36,
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(16, 4, 16, 4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            FontSize = 12
        };

        // ── Sidebar Assembly ──
        var sidebarTopPanel = new StackPanel
        {
            Spacing = 0,
            Children =
            {
                appTitle,
                appSubtitle,
                btn_Scan,
                progressBar,
                txt_Count,
                new Border { Height = 1, Background = SeparatorColor, Margin = new Thickness(16, 14, 16, 0) },
                deviceListHeader
            }
        };
        DockPanel.SetDock(sidebarTopPanel, Dock.Top);

        var sidebarBottomPanel = new StackPanel
        {
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Bottom,
            Children =
            {
                new Border { Height = 1, Background = SeparatorColor, Margin = new Thickness(16, 0) },
                btn_Settings
            }
        };
        DockPanel.SetDock(sidebarBottomPanel, Dock.Bottom);

        var sidebar = new DockPanel
        {
            Background = BgSidebar,
            LastChildFill = true,
            Children = { sidebarTopPanel, sidebarBottomPanel, deviceList }
        };

        // ═══════════════════════════════════════════
        // ██  MAIN AREA (Center - Radar Canvas)
        // ═══════════════════════════════════════════

        var radarCanvas = new RadarCanvas();

        // ── Status Bar (Bottom) ──
        var statusOnline = new TextBlock { Text = "● 0 Online", Foreground = OnlineGreen, FontSize = 11, Margin = new Thickness(0, 0, 16, 0) };
        var statusOffline = new TextBlock { Text = "● 0 Offline", Foreground = OfflineRed, FontSize = 11, Margin = new Thickness(0, 0, 16, 0) };
        var statusMonitor = new TextBlock { Text = "Monitor: Idle", Foreground = SubtextGrey, FontSize = 11, Margin = new Thickness(0, 0, 16, 0) };
        var statusLastScan = new TextBlock { Text = "", Foreground = SubtextGrey, FontSize = 11 };

        var statusBar = new Border
        {
            Background = SolidColorBrush.Parse("#111120"),
            Padding = new Thickness(16, 6),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
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

        // Container that holds whichever right panel is active
        var rightPanelContainer = new Grid();
        rightPanelContainer.Children.Add(settingsPanel);
        rightPanelContainer.Children.Add(detailPanel);

        // ═══════════════════════════════════════════
        // ██  ROOT LAYOUT (3-column Grid)
        // ═══════════════════════════════════════════

        var rootGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("260, *, Auto")
        };

        Grid.SetColumn(sidebar, 0);
        Grid.SetColumn(mainArea, 1);
        Grid.SetColumn(rightPanelContainer, 2);

        rootGrid.Children.Add(sidebar);
        rootGrid.Children.Add(mainArea);
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
                node = db.MergeWithHistory(node);
                
                // Check if this MAC already exists in our list (update it)
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
            btn_Scan.IsEnabled = false;
            btn_Scan.Content = "⏳  SCANNING...";
            activeNodes.Clear();
            radarCanvas.UpdateNodes(activeNodes);
            deviceList.UpdateDevices(activeNodes);
            txt_Count.Text = "0 devices found";
            progressBar.Value = 0;

            string localSubnet = SubnetScanner.GetLocalBaseIp();
            await scanner.ScanSubnetAsync(localSubnet);

            btn_Scan.Content = "⚡  SCAN NETWORK";
            btn_Scan.IsEnabled = true;
            statusLastScan.Text = $"Last scan: {DateTime.Now:HH:mm:ss}";

            // Feed results to the connectivity monitor
            monitor.UpdateTrackedDevices(activeNodes);

            // Start background monitoring if not already running
            statusMonitor.Text = $"Monitor: Active ({monitor.IntervalSeconds}s)";
            _ = Task.Run(() => monitor.StartMonitoringAsync(cts.Token));
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
                // Merge updated statuses back into activeNodes
                foreach (var updated in updatedNodes)
                {
                    var idx = activeNodes.FindIndex(n => n.MacAddress == updated.MacAddress);
                    if (idx >= 0) activeNodes[idx] = updated;
                }
                RefreshAll();
            });
        };

        // ── Device Selection (from list or radar) ──
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
            // Refresh the list and radar to show updated names
            monitor.AddDevice(node);
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

        // ── Add Device Button (manual device registration) ──
        btn_AddDevice.Click += (s, e) =>
        {
            settingsPanel.IsVisible = false;

            // Create a blank node for manual entry
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

        // ── Load previously registered devices on startup ──
        window.Opened += (s, e) =>
        {
            var knownDevices = db.GetAllKnownDevices();
            foreach (var device in knownDevices)
            {
                device.IsOnline = false; // Will be confirmed on first scan/monitor cycle
                if (!activeNodes.Any(n => n.MacAddress == device.MacAddress))
                {
                    activeNodes.Add(device);
                }
            }
            if (activeNodes.Count > 0)
            {
                RefreshAll();
            }
        };

        // ── Cleanup on window close ──
        window.Closing += (s, e) =>
        {
            cts.Cancel();
        };

        return window;
    }
}