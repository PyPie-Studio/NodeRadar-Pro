using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Messaging;
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
    private static DispatcherTimer? _autoBackupTimer;

    public static void OnStartup(Window mainWindow)
    {
        var db = LocalDatabase.Instance;
        var settings = db.LoadSettings();

        // Auto-Backup Timer
        InitializeAutoBackupTimer(settings);
    }


    public static void InitializeAutoBackupTimer(AppSettings settings)
    {
        _autoBackupTimer?.Stop();

        if (settings.EnableAutoBackup)
        {
            _autoBackupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(settings.AutoBackupIntervalHours)
            };
            _autoBackupTimer.Tick += (s, e) =>
            {
                Task.Run(() =>
                {
                    LocalDatabase.Instance.PruneOldData();
                    LocalDatabase.Instance.Backup.BackupDatabase();
                });
            };
            _autoBackupTimer.Start();
        }
    }

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
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://NodeRadar Pro/Resources/NodeRadar Pro Icon.ico"))),
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
        var activeNodesMap = new ConcurrentDictionary<string, NetworkNode>();

        // Load registered devices from database (Issue: Inventory was empty on restart)
        try
        {
            var registered = db.GetRegisteredDevices();
            foreach (var node in registered)
            {
                node.IsOnline = false; // Initially offline until monitor probes them
                activeNodesMap.TryAdd(node.MacAddress, node);
                monitor.AddDevice(node);
            }
        }
        catch (Exception ex)
        {
            db.Log(LogLevel.Error, "Database", $"Failed to load registered devices: {ex.Message}");
        }

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
        var dashboardPage = new DashboardPage(activeNodesMap.Values.ToList());
        var scannerPage = new ScannerPage(db, scanner, activeNodesMap);
        var traceroutePage = new TraceroutePage();
        var inventoryPage = new InventoryPage(db, monitor, activeNodesMap.Values.ToList());
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
        pageHost.Children.Add(traceroutePage);
        pageHost.Children.Add(inventoryPage);
        pageHost.Children.Add(settingsPage);
        pageHost.Children.Add(portScansPage);
        pageHost.Children.Add(alertsPage);
        pageHost.Children.Add(logsPage);
        pageHost.Children.Add(supportPage);

        // Hide all except dashboard
        scannerPage.IsVisible = false;
        traceroutePage.IsVisible = false;
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
            traceroutePage.IsVisible = name == "traceroute";
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
            // Pre-calculate stats off the UI thread
            var values = activeNodesMap.Values;
            var snapshot = new System.Collections.Generic.List<NetworkNode>(values.Count);

            int online = 0;
            long totalLatency = 0;
            int latencyCount = 0;

            foreach (var node in values)
            {
                snapshot.Add(node);
                if (node.IsOnline)
                {
                    online++;
                    if (node.PingLatencyMs >= 0)
                    {
                        totalLatency += node.PingLatencyMs;
                        latencyCount++;
                    }
                }
            }

            long avgLat = latencyCount > 0 ? totalLatency / latencyCount : -1;

            int alertCount = 0;
            try { alertCount = db.GetUnresolvedAlertCount(); } catch { }

            Dispatcher.UIThread.Post(() =>
            {
                // Update Top Nav
                topNav.UpdateStatus(online, avgLat, alertCount);

                // Broadcast updates
                EventAggregator.Instance.Publish(new GlobalStatsUpdatedMessage(online, avgLat, alertCount));
                EventAggregator.Instance.Publish(new NodesUpdatedMessage(snapshot));
            });
        }

        // ── Scanner page data changes → refresh dashboard ──
        scannerPage.DataChanged += () =>
        {
            int count = activeNodesMap.Values.Count(n => n.IsOnline);
            try { db.Log(LogLevel.Info, "Scanner", $"Scan discovered {count} devices"); } catch { }
            SyncGlobalStats();
        };

        alertsPage.AlertsChanged += () =>
        {
            db.Checkpoint();
            SyncGlobalStats();
        };

        // ── Inventory page events ──
        inventoryPage.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            activeNodesMap.UpdateNode(node);
            try { db.Log(LogLevel.Info, "Inventory", $"Device '{node.DisplayName}' saved", node.MacAddress); } catch { }
            db.Checkpoint();
            SyncGlobalStats();
        };

        inventoryPage.DeviceDeleted += (node) =>
        {
            activeNodesMap.TryRemove(node.MacAddress, out _);
            try { db.Log(LogLevel.Info, "Inventory", $"Device '{node.DisplayName}' deleted", node.MacAddress); } catch { }
            db.Checkpoint();
            SyncGlobalStats();
        };

        inventoryPage.DevicesDeleted += (nodes) =>
        {
            foreach (var node in nodes)
            {
                activeNodesMap.TryRemove(node.MacAddress, out _);
            }
            try { db.Log(LogLevel.Info, "Inventory", $"Bulk deleted {nodes.Count()} devices"); } catch { }
            db.Checkpoint();
            SyncGlobalStats();
        };

        inventoryPage.DeviceStatusChanged += (node) =>
        {
            activeNodesMap.UpdateNode(node);
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
            db.UpdateRegistration(
                node.MacAddress,
                node.CustomName,
                node.Notes,
                node.Location,
                node.DeviceName,
                node.DeviceModel,
                node.IconPath,
                node.IpAddress,
                node.VulnerabilityScore,
                node.ThreatLevel,
                node.ExactModel
            );

            monitor.AddDevice(node);
            activeNodesMap.UpdateNode(node);
            try { db.Log(LogLevel.Info, "Scanner", $"Device '{node.DisplayName}' permanently registered", node.MacAddress); } catch { }
            db.Checkpoint();
            SyncGlobalStats();
        };

        // ── Settings saved → push to services ──
        settingsPage.SettingsSaved += (newSettings) =>
        {
            settings = newSettings;
            ApplySettings(newSettings, monitor, scanner, detector);
            try { db.Log(LogLevel.Info, "Settings", "Settings updated"); } catch { }
            db.Checkpoint();
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
                activeNodesMap.UpdateNode(node);
                SyncGlobalStats();
            });
        };

        monitor.DeviceCameOnline += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IntrusionAlerter.AlertDeviceReconnected(node);
                activeNodesMap.UpdateNode(node);
                SyncGlobalStats();
            });
        };

        monitor.StatusUpdated += (updatedNodes) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var updated in updatedNodes)
                {
                    activeNodesMap.UpdateNode(updated);
                }
                SyncGlobalStats();
            });
        };

        // ═══════════════════════════════════════════
        // ██  STARTUP
        // ═══════════════════════════════════════════
        window.Opened += async (s, e) =>
        {
            // Background data load
            settings = db.LoadSettings();

            // Task 2: System Integrity Shield (Verify Binaries)
            _ = Task.Run(() => VerifySystemIntegrity(settings, db));

            // Apply settings to services (Non-UI)
            ApplySettings(settings, monitor, scanner, detector);

            // UI Status Updates
            Dispatcher.UIThread.Post(() =>
            {
                int online = activeNodesMap.Values.Count(n => n.IsOnline);
                int alertCountActual = 0;
                try { alertCountActual = db.GetUnresolvedAlertCount(); } catch { }

                topNav.UpdateStatus(online, -1, alertCountActual);
                dashboardPage.RefreshData();
                inventoryPage.RefreshData();
                SyncGlobalStats();
            });

            // Start services
            if (activeNodesMap.Count > 0)
            {
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
    /// Pushes AppSettings values to the scanner, monitor, and detector services.
    /// </summary>
    private static void ApplySettings(AppSettings settings, ConnectivityMonitor monitor, SubnetScanner scanner, IntrusionDetector detector)
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

        detector.Configure(settings);
    }

    /// <summary>
    /// Verifies the system integrity by checking the binary file hash against the last known good hash.
    /// </summary>
    private static void VerifySystemIntegrity(AppSettings settings, LocalDatabase db)
    {
        try
        {
            // We verify the core logic DLL/executable instead of the dynamic database
            string mainFile = Environment.ProcessPath ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NodeRadar Pro.dll");

            if (!System.IO.File.Exists(mainFile))
            {
                return;
            }

            string? currentHash = SecurityService.ComputeFileHash(mainFile);
            if (string.IsNullOrEmpty(currentHash))
            {
                return;
            }

            if (string.IsNullOrEmpty(settings.LastKnownGoodHash) || settings.LastKnownGoodHash == "INITIAL")
            {
                settings.LastKnownGoodHash = currentHash;
                db.SaveSettings(settings);
                db.Log(LogLevel.Info, "Security", "System integrity signature locked.");
            }
            else if (!SecurityService.VerifyHash(settings.LastKnownGoodHash, currentHash))
            {
                db.Log(LogLevel.Warning, "Security", "CORE INTEGRITY MISMATCH: Application binary may have been tampered with!");
            }
            else
            {
                db.Log(LogLevel.Info, "Security", "System integrity verified (SHA256).");
            }
        }
        catch (Exception ex)
        {
            db.Log(LogLevel.Warning, "Security", $"Integrity check deferred: {ex.Message}");
        }
    }
}
