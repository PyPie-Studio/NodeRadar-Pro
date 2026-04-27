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
/// Application shell — builds the window, sidebar, top nav, and page host.
/// Manages page switching and wires all services together.
/// </summary>
public class DarkPurpleTheme
{
    public static Window BuildMainWindow()
    {
        var window = new Window
        {
            Title = "NodeRadar Pro — Network Monitor",
            Width = 1400,
            Height = 900,
            MinWidth = 900,
            MinHeight = 600,
            Background = Brushes.Transparent,
            TransparencyLevelHint = new[] 
            { 
                WindowTransparencyLevel.Mica, 
                WindowTransparencyLevel.AcrylicBlur, 
                WindowTransparencyLevel.Blur 
            },
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Icon = new WindowIcon("Resources/NodeRadar Pro Icon.png"),
        };

        // ═══════════════════════════════════════════
        // ██  CORE SERVICES
        // ═══════════════════════════════════════════
        var db = LocalDatabase.Instance;
        var scanner = new SubnetScanner();
        var monitor = new ConnectivityMonitor();
        monitor.SetDatabase(db);
        var detector = new IntrusionDetector();

        AppSettings settings = null!;
        var activeNodes = new List<NetworkNode>();
        var cts = new CancellationTokenSource();

        // Log startup
        try { db.Log(LogLevel.Info, "System", "NodeRadar Pro started"); } catch { }

        // ═══════════════════════════════════════════
        // ██  SHELL COMPONENTS
        // ═══════════════════════════════════════════
        var sideNav = new SideNavBar();
        var topNav = new TopNavBar();

        // ═══════════════════════════════════════════
        // ██  PAGES (Real implementations)
        // ═══════════════════════════════════════════
        var dashboardPage = new DashboardPage(activeNodes);
        var scannerPage = new ScannerPage(db, scanner, activeNodes);
        var inventoryPage = new InventoryPage(db, monitor, activeNodes);
        var settingsPage = new SettingsPage(db);
        var portScansPage = new PortScansPage();
        var alertsPage = new AlertsPage(db);
        var logsPage = new SystemLogsPage(db);
        var supportPage = new SupportPage();

        // ═══════════════════════════════════════════
        // ██  PAGE HOST
        // ═══════════════════════════════════════════
        var pageHost = new Grid();
        pageHost.Children.Add(dashboardPage);
        pageHost.Children.Add(scannerPage);
        pageHost.Children.Add(inventoryPage);
        pageHost.Children.Add(settingsPage);
        pageHost.Children.Add(portScansPage);
        pageHost.Children.Add(alertsPage);
        pageHost.Children.Add(logsPage);
        pageHost.Children.Add(supportPage);

        // Hide all except dashboard
        scannerPage.IsVisible = false;
        inventoryPage.IsVisible = false;
        settingsPage.IsVisible = false;
        portScansPage.IsVisible = false;
        alertsPage.IsVisible = false;
        logsPage.IsVisible = false;
        supportPage.IsVisible = false;

        void ShowPage(string name)
        {
            dashboardPage.IsVisible = name == "dashboard";
            scannerPage.IsVisible = name == "radar";
            inventoryPage.IsVisible = name == "inventory";
            settingsPage.IsVisible = name == "settings";
            portScansPage.IsVisible = name == "portscans";
            alertsPage.IsVisible = name == "alerts";
            logsPage.IsVisible = name == "logs";
            supportPage.IsVisible = name == "support";

            // Refresh active page data
            if (name == "dashboard") dashboardPage.RefreshData();
            if (name == "inventory") inventoryPage.RefreshData();
            if (name == "settings") settingsPage.Refresh();
            if (name == "alerts") alertsPage.RefreshAlerts();
            if (name == "logs") logsPage.RefreshLogs();
        }

        sideNav.PageChanged += ShowPage;

        // ═══════════════════════════════════════════
        // ██  LAYOUT: Sidebar + (TopNav / PageHost)
        // ═══════════════════════════════════════════
        var contentArea = new DockPanel { Background = ThemeTokens.Surface };
        DockPanel.SetDock(topNav, Dock.Top);
        contentArea.Children.Add(topNav);
        contentArea.Children.Add(pageHost);

        var rootGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(ThemeTokens.SidebarWidth, GridUnitType.Pixel)),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star))
            }
        };

        Grid.SetColumn(sideNav, 0);
        Grid.SetColumn(contentArea, 1);
        rootGrid.Children.Add(sideNav);
        rootGrid.Children.Add(contentArea);

        window.Content = rootGrid;

        // ═══════════════════════════════════════════
        // ██  EVENT WIRING
        // ═══════════════════════════════════════════

        IntrusionAlerter.Initialize(window);

        void RefreshAll()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (dashboardPage.IsVisible) dashboardPage.RefreshData();
                if (inventoryPage.IsVisible) inventoryPage.RefreshData();

                // Update top nav status
                int online = activeNodes.Count(n => n.IsOnline);
                var onlineWithLatency = activeNodes.Where(n => n.IsOnline && n.PingLatencyMs >= 0).ToList();
                long avgLat = onlineWithLatency.Count > 0 ? (long)onlineWithLatency.Average(n => n.PingLatencyMs) : -1;
                int alertCount = 0;
                try { alertCount = db.GetUnresolvedAlertCount(); } catch { }
                topNav.UpdateStatus(online, avgLat, alertCount);
            });
        }

        // ── Scanner page data changes → refresh dashboard ──
        scannerPage.DataChanged += () =>
        {
            try { db.Log(LogLevel.Info, "Scanner", $"Scan discovered {activeNodes.Count(n => n.IsOnline)} devices"); } catch { }
            RefreshAll();
        };

        // ── Inventory page events ──
        inventoryPage.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            try { db.Log(LogLevel.Info, "Inventory", $"Device '{node.DisplayName}' saved", node.MacAddress); } catch { }
            RefreshAll();
        };

        inventoryPage.DeviceDeleted += (node) =>
        {
            try { db.Log(LogLevel.Info, "Inventory", $"Device '{node.DisplayName}' deleted", node.MacAddress); } catch { }
            RefreshAll();
        };

        inventoryPage.DeviceStatusChanged += (node) =>
        {
            var idx = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
            if (idx >= 0) activeNodes[idx] = node;
            RefreshAll();
        };

        // ── Dashboard device selection → switch to inventory ──
        dashboardPage.DeviceSelected += (node) =>
        {
            sideNav.SetActive("inventory");
            ShowPage("inventory");
            inventoryPage.ShowDevice(node);
        };

        // ── Dashboard View Logs → navigate to alerts page (B6) ──
        dashboardPage.ViewLogsRequested += () =>
        {
            sideNav.SetActive("alerts");
            ShowPage("alerts");
        };

        // ── Inventory View Logs → navigate to logs page with device filter (B7/U12) ──
        inventoryPage.ViewLogsRequested += (mac) =>
        {
            logsPage.FilterByDevice(mac);
            sideNav.SetActive("logs");
            ShowPage("logs");
        };

        // ── Scanner device save → register device (I9) ──
        scannerPage.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            try { db.Log(LogLevel.Info, "Scanner", $"Device '{node.DisplayName}' saved from scan results", node.MacAddress); } catch { }
            RefreshAll();
        };

        // ── Settings saved → push to services ──
        settingsPage.SettingsSaved += (newSettings) =>
        {
            settings = newSettings;
            ApplySettings(newSettings, monitor, scanner);
            try { db.Log(LogLevel.Info, "Settings", "Settings updated"); } catch { }
            RefreshAll();
        };

        // ── Monitor alert events → update alerts page ──
        monitor.AlertTriggered += (alert) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (alertsPage.IsVisible) alertsPage.RefreshAlerts();
                RefreshAll();
            });
        };

        // ═══════════════════════════════════════════
        // ██  CONNECTIVITY MONITOR EVENTS
        // ═══════════════════════════════════════════

        monitor.DeviceWentOffline += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IntrusionAlerter.AlertDeviceOffline(node);
                var idx = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
                if (idx >= 0) activeNodes[idx] = node;
                RefreshAll();
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

        // ═══════════════════════════════════════════
        // ██  STARTUP — Load registered devices
        // ═══════════════════════════════════════════
        window.Opened += async (s, e) =>
        {
            // Background data load
            settings = await Task.Run(() => db.LoadSettings());
            var savedDevices = await Task.Run(() => db.GetRegisteredDevices());
            var alertCount = await Task.Run(() => db.GetUnresolvedAlertCount());

            // Task 2: Database Integrity Check
            try
            {
                string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string dbPath = System.IO.Path.Combine(myDocuments, "PyPie Studio", "NodeRadar Pro", "noderadar.db");
                string hash = SecurityService.ComputeFileHash(dbPath);
                if (hash != null)
                {
                    db.Log(LogLevel.Info, "Security", $"Database Integrity Hash: {hash}");
                }
            }
            catch (Exception ex)
            {
                db.Log(LogLevel.Error, "Security", $"Integrity check failed: {ex.Message}");
            }

            // Apply settings to services (Non-UI)
            ApplySettings(settings, monitor, scanner);

            // Process devices
            foreach (var device in savedDevices)
            {
                device.IsOnline = false;
                if (!activeNodes.Any(n => n.MacAddress == device.MacAddress))
                    activeNodes.Add(device);
            }

            // UI Status Updates
            Dispatcher.UIThread.Post(() =>
            {
                int online = activeNodes.Count(n => n.IsOnline);
                topNav.UpdateStatus(online, -1, alertCount);
                dashboardPage.RefreshData();
                RefreshAll();
            });

            // Start services
            if (activeNodes.Count > 0)
            {
                monitor.UpdateTrackedDevices(activeNodes);
                _ = Task.Run(() => monitor.StartMonitoringAsync(cts.Token));
            }

            // Start background intrusion detector (Task 3)
            _ = detector.StartAsync(cts.Token);
        };

        // ═══════════════════════════════════════════
        // ██  CLEANUP
        // ═══════════════════════════════════════════
        window.Closing += (s, e) =>
        {
            cts.Cancel();
            try { db.Log(LogLevel.Info, "System", "NodeRadar Pro shutting down"); } catch { }
        };

        return window;
    }

    /// <summary>
    /// Pushes AppSettings values to the scanner and monitor services.
    /// </summary>
    private static void ApplySettings(AppSettings settings, ConnectivityMonitor monitor, SubnetScanner scanner)
    {
        monitor.IntervalSeconds = settings.SweepFrequencySeconds > 0 ? settings.SweepFrequencySeconds : settings.MonitorIntervalSeconds;
        monitor.TimeoutMs = settings.ResponseTimeoutMs;
        monitor.LatencyThresholdMs = settings.LatencyThresholdMs;
        monitor.PacketLossThresholdPct = settings.PacketLossThresholdPct;

        // S1: Enhanced TCP probe mode
        monitor.EnableSynScan = settings.EnableSynScan;

        // S4/S5/S6: Push notification settings to services
        monitor.EnableToastAlerts = settings.EnableToastAlerts;
        monitor.EnableSoundAlerts = settings.EnableSoundAlerts;
        monitor.EnableEmailAlerts = settings.EnableEmailAlerts;
        IntrusionAlerter.Enabled = settings.EnableToastAlerts;
        IntrusionAlerter.Settings = settings;
        AudioService.Enabled = settings.EnableSoundAlerts;

        // S5/S6: Log stubs for unimplemented notification channels
        if (settings.EnableSoundAlerts)
        {
            // TODO: Implement sound alert playback using platform audio API
        }
        if (settings.EnableEmailAlerts && !string.IsNullOrEmpty(settings.SmtpHost))
        {
            // TODO: Implement SMTP email alert sending
        }

        // S3: Pass preferred interface to scanner
        scanner.PreferredInterfaceName = settings.SelectedInterfaceName;

        scanner.TimeoutMs = settings.ResponseTimeoutMs;
        scanner.EnableDnsResolve = settings.EnableDnsResolve;
        scanner.EnableInlinePortScan = settings.EnableInlinePortScan;
        scanner.FastScanMode = settings.EnableFastScan;
        scanner.EnableOsDetection = settings.EnableOsDetection;
    }
}