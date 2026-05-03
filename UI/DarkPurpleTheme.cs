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

using Avalonia.Platform;

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
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://NodeRadar Pro/Resources/NodeRadar Pro Icon.png"))),
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
        var _nodesLock = new object();
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
        var scannerPage = new ScannerPage(db, scanner, activeNodes, _nodesLock);
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
            if (name == "logs") 
            {
                 // Issue 9: Clear filters when navigating directly to logs page
                 logsPage.ClearAllFilters(); 
                 logsPage.RefreshLogs();
            }
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

        void SyncGlobalStats()
        {
            Dispatcher.UIThread.Post(() =>
            {
                int online;
                long avgLat;
                lock (_nodesLock)
                {
                    online = activeNodes.Count(n => n.IsOnline);
                    var onlineWithLatency = activeNodes.Where(n => n.IsOnline && n.PingLatencyMs >= 0).ToList();
                    avgLat = onlineWithLatency.Count > 0 ? (long)onlineWithLatency.Average(n => n.PingLatencyMs) : -1;
                }
                
                int alertCount = 0;
                try { alertCount = db.GetUnresolvedAlertCount(); } catch { }

                // Update Top Nav
                topNav.UpdateStatus(online, avgLat, alertCount);
                
                // Refresh visible pages
                if (dashboardPage.IsVisible) dashboardPage.RefreshData();
                if (inventoryPage.IsVisible) inventoryPage.RefreshData();
                if (alertsPage.IsVisible) alertsPage.RefreshAlerts();
            });
        }

        // ── Scanner page data changes → refresh dashboard ──
        scannerPage.DataChanged += () =>
        {
            int count = 0;
            lock (_nodesLock) { count = activeNodes.Count(n => n.IsOnline); }
            try { db.Log(LogLevel.Info, "Scanner", $"Scan discovered {count} devices"); } catch { }
            SyncGlobalStats();
        };

        alertsPage.AlertsChanged += () =>
        {
            SyncGlobalStats();
        };

        // ── Inventory page events ──
        inventoryPage.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            try { db.Log(LogLevel.Info, "Inventory", $"Device '{node.DisplayName}' saved", node.MacAddress); } catch { }
            SyncGlobalStats();
        };

        inventoryPage.DeviceDeleted += (node) =>
        {
            try { db.Log(LogLevel.Info, "Inventory", $"Device '{node.DisplayName}' deleted", node.MacAddress); } catch { }
            SyncGlobalStats();
        };

        inventoryPage.DeviceStatusChanged += (node) =>
        {
            lock (_nodesLock)
            {
                var idx = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
                if (idx >= 0) activeNodes[idx] = node;
            }
            SyncGlobalStats();
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
            logsPage.ClearDeviceFilter(); // Reset first to ensure clean state
            logsPage.FilterByDevice(mac);
            sideNav.SetActive("logs");
            ShowPage("logs");
        };

        inventoryPage.DeviceSelected += (node) =>
        {
             // Fix Issue 4: Fetch history when device is selected in inventory
             var history = db.GetUptimeHistory(node.MacAddress, 24);
             inventoryPage.UpdateUptimeChart(history);
        };

        // ── Scanner device save → register device (I9) ──
        scannerPage.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            try { db.Log(LogLevel.Info, "Scanner", $"Device '{node.DisplayName}' saved from scan results", node.MacAddress); } catch { }
            SyncGlobalStats();
        };

        // ── Settings saved → push to services ──
        settingsPage.SettingsSaved += (newSettings) =>
        {
            settings = newSettings;
            ApplySettings(newSettings, monitor, scanner);
            try { db.Log(LogLevel.Info, "Settings", "Settings updated"); } catch { }
            SyncGlobalStats();
        };

        // ── Monitor alert events → update alerts page ──
        monitor.AlertTriggered += (alert) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (alertsPage.IsVisible) alertsPage.RefreshAlerts();
                SyncGlobalStats();
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
                lock (_nodesLock)
                {
                    var idx = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
                    if (idx >= 0) activeNodes[idx] = node;
                }
                SyncGlobalStats();
            });
        };

        monitor.DeviceCameOnline += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IntrusionAlerter.AlertDeviceReconnected(node);
                lock (_nodesLock)
                {
                    var idx = activeNodes.FindIndex(n => n.MacAddress == node.MacAddress);
                    if (idx >= 0) activeNodes[idx] = node;
                }
                SyncGlobalStats();
            });
        };

        monitor.StatusUpdated += (updatedNodes) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                lock (_nodesLock)
                {
                    foreach (var updated in updatedNodes)
                    {
                        var idx = activeNodes.FindIndex(n => n.MacAddress == updated.MacAddress);
                        if (idx >= 0) activeNodes[idx] = updated;
                    }
                }
                SyncGlobalStats();
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

            // Task 2: Database Integrity Check (Non-Blocking + Shared Read)
            _ = Task.Run(() => {
                try {
                    string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string dbPath = System.IO.Path.Combine(myDocuments, "PyPie Studio", "NodeRadar Pro", "noderadar.db");
                    // Using FileShare.ReadWrite allows us to hash while LiteDB has the file open
                    using var stream = new System.IO.FileStream(dbPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                    var hashBytes = sha256.ComputeHash(stream);
                    string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    if (string.IsNullOrEmpty(settings.LastKnownGoodHash))
                    {
                        settings.LastKnownGoodHash = hash;
                        db.SaveSettings(settings);
                        db.Log(LogLevel.Info, "Security", $"Initial Database Hash stored: {hash}");
                    }
                    else if (settings.LastKnownGoodHash != hash)
                    {
                        db.Log(LogLevel.Error, "Security", "[SECURITY] Database integrity mismatch detected!");
                    }
                    else
                    {
                        db.Log(LogLevel.Info, "Security", "Database Integrity verified.");
                    }
                } catch (Exception ex) {
                    db.Log(LogLevel.Warning, "Security", $"Integrity check deferred: {ex.Message}");
                }
            });

            // Apply settings to services (Non-UI)
            ApplySettings(settings, monitor, scanner);

            // Process devices
            lock (_nodesLock)
            {
                foreach (var device in savedDevices)
                {
                    device.IsOnline = false;
                    if (!activeNodes.Any(n => n.MacAddress == device.MacAddress))
                        activeNodes.Add(device);
                }
            }

            // UI Status Updates
            Dispatcher.UIThread.Post(() =>
            {
                int online;
                lock (_nodesLock) { online = activeNodes.Count(n => n.IsOnline); }
                topNav.UpdateStatus(online, -1, alertCount);
                dashboardPage.RefreshData();
                SyncGlobalStats();
            });

            // Start services
            int activeCount = 0;
            lock (_nodesLock) { activeCount = activeNodes.Count; }

            if (activeCount > 0)
            {
                lock (_nodesLock) { monitor.UpdateTrackedDevices(activeNodes); }
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
            cts.Dispose();
            scannerPage.Cleanup();
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

        // S3: Pass preferred interface to scanner
        scanner.PreferredInterfaceName = settings.SelectedInterfaceName;

        scanner.TimeoutMs = settings.ResponseTimeoutMs;
        scanner.EnableDnsResolve = settings.EnableDnsResolve;
        scanner.EnableInlinePortScan = settings.EnableInlinePortScan;
        scanner.FastScanMode = settings.EnableFastScan;
        scanner.EnableOsDetection = settings.EnableOsDetection;
    }
}