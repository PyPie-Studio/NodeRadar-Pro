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
    private const string PageDashboard = "dashboard";
    private const string PageRadar = "radar";
    private const string PageTraceroute = "traceroute";
    private const string PageInventory = "inventory";
    private const string PageSettings = "settings";
    private const string PagePortScans = "portscans";
    private const string PageAlerts = "alerts";
    private const string PageLogs = "logs";
    private const string PageSupport = "support";

    private const string LogSecurity = "Security";
    private const string LogInventory = "Inventory";
    private const string LogScanner = "Scanner";

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
            Icon = LoadWindowIcon(),
        };

        // ═══════════════════════════════════════════
        // ██  CORE SERVICES
        // ═══════════════════════════════════════════
        var db = LocalDatabase.Instance;
        var scanner = new SubnetScanner();
        var monitor = new ConnectivityMonitor();
        monitor.SetDatabase(db);
        var detector = new IntrusionDetector();

        var activeNodesMap = new ConcurrentDictionary<string, NetworkNode>();

        // Load registered devices from database (Issue: Inventory was empty on restart)
        LoadRegisteredDevices(db, monitor, activeNodesMap);

        var cts = new CancellationTokenSource();

        // Log startup
        try { db.Log(LogLevel.Info, "System", "NodeRadar Pro started"); } catch { }

        var services = new ShellServices(db, scanner, monitor, detector, activeNodesMap, cts);

        // ═══════════════════════════════════════════
        // ██  SHELL COMPONENTS & PAGES
        // ═══════════════════════════════════════════
        var sideNav = new SideNavBar();
        var topNav = new TopNavBar();

        var pages = new ShellPages(
            new DashboardPage(activeNodesMap.Values.ToList()),
            new ScannerPage(db, scanner, activeNodesMap),
            new TraceroutePage(),
            new InventoryPage(db, monitor, activeNodesMap.Values.ToList()),
            new SettingsPage(db),
            new PortScansPage(),
            new AlertsPage(db),
            new SystemLogsPage(db),
            new SupportPage());

        var pageHost = CreatePageHost(pages);
        var showPage = SetupPageNavigation(sideNav, pages);

        // ═══════════════════════════════════════════
        // ██  LAYOUT: Sidebar + (TopNav / PageHost)
        // ═══════════════════════════════════════════
        var contentArea = new DockPanel { Background = ThemeTokens.Surface };
        DockPanel.SetDock(topNav, Dock.Top);
        contentArea.Children.Add(topNav);
        contentArea.Children.Add(pageHost);

        var rootGrid = new Grid
        {
            Background = ThemeTokens.SurfaceContainerLowest,
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
            SyncGlobalStatistics(activeNodesMap, db, topNav);
        }

        WirePageEvents(sideNav, pages, monitor, activeNodesMap, db, showPage, SyncGlobalStats);
        WireSettingsEvents(pages, services, SyncGlobalStats);
        WireMonitorEvents(monitor, activeNodesMap, pages.Alerts, SyncGlobalStats);
        WireWindowLifecycle(window, services, topNav, pages, SyncGlobalStats);

        return window;
    }

    private static WindowIcon? LoadWindowIcon()
    {
        try
        {
            using var iconStream = AssetLoader.Open(new Uri("avares://NodeRadarPro/Resources/NodeRadar Pro Icon.ico"));
            return new WindowIcon(iconStream);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, "UI", $"Failed to load window icon: {ex.Message}");
            return null;
        }
    }

    private static Grid CreatePageHost(ShellPages pages)
    {
        var pageHost = new Grid();
        pageHost.Children.Add(pages.Dashboard);
        pageHost.Children.Add(pages.Scanner);
        pageHost.Children.Add(pages.Traceroute);
        pageHost.Children.Add(pages.Inventory);
        pageHost.Children.Add(pages.Settings);
        pageHost.Children.Add(pages.PortScans);
        pageHost.Children.Add(pages.Alerts);
        pageHost.Children.Add(pages.Logs);
        pageHost.Children.Add(pages.Support);

        pages.Scanner.IsVisible = false;
        pages.Traceroute.IsVisible = false;
        pages.Inventory.IsVisible = false;
        pages.Settings.IsVisible = false;
        pages.PortScans.IsVisible = false;
        pages.Alerts.IsVisible = false;
        pages.Logs.IsVisible = false;
        pages.Support.IsVisible = false;

        return pageHost;
    }

    private static void SyncGlobalStatistics(
        ConcurrentDictionary<string, NetworkNode> activeNodesMap,
        LocalDatabase db,
        TopNavBar topNav)
    {
        Task.Run(() =>
        {
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
                topNav.UpdateStatus(online, avgLat, alertCount);
                EventAggregator.Instance.Publish(new GlobalStatsUpdatedMessage(online, avgLat, alertCount));
                EventAggregator.Instance.Publish(new NodesUpdatedMessage(snapshot));
            });
        });
    }

    private static void WireSettingsEvents(
        ShellPages pages,
        ShellServices services,
        Action syncGlobalStats)
    {
        pages.Settings.SettingsSaved += (newSettings) =>
        {
            ApplySettings(newSettings, services.Monitor, services.Scanner, services.Detector);
            Task.Run(() =>
            {
                try { services.Db.Log(LogLevel.Info, "Settings", "Settings updated"); } catch { }
                services.Db.Checkpoint();
                syncGlobalStats();
            });
        };
    }

    private static void WireWindowLifecycle(
        Window window,
        ShellServices services,
        TopNavBar topNav,
        ShellPages pages,
        Action syncGlobalStats)
    {
        window.Opened += (s, e) =>
        {
            Task.Run(() =>
            {
                var settings = services.Db.LoadSettings();
                VerifySystemIntegrity(settings, services.Db);
                ApplySettings(settings, services.Monitor, services.Scanner, services.Detector);

                int online = services.ActiveNodesMap.Values.Count(n => n.IsOnline);
                int alertCountActual = 0;
                try { alertCountActual = services.Db.GetUnresolvedAlertCount(); } catch { }

                Dispatcher.UIThread.Post(() =>
                {
                    topNav.UpdateStatus(online, -1, alertCountActual);
                    pages.Dashboard.RefreshData();
                    pages.Inventory.RefreshData();
                });

                syncGlobalStats();

                if (!services.ActiveNodesMap.IsEmpty)
                {
                    _ = Task.Run(() => services.Monitor.StartMonitoringAsync(services.Cts.Token), services.Cts.Token);
                }

                _ = Task.Run(() => services.Detector.StartAsync(services.Cts.Token), services.Cts.Token);
            });
        };

        window.Closing += (s, e) =>
        {
            services.Cts.Cancel();
            services.Cts.Dispose();
            pages.Scanner.Cleanup();
            try { services.Db.Log(LogLevel.Info, "System", "NodeRadar Pro shutting down"); } catch { }
        };
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

    private static void LoadRegisteredDevices(LocalDatabase db, ConnectivityMonitor monitor, ConcurrentDictionary<string, NetworkNode> activeNodesMap)
    {
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
    }

    private sealed record ShellPages(
        DashboardPage Dashboard,
        ScannerPage Scanner,
        TraceroutePage Traceroute,
        InventoryPage Inventory,
        SettingsPage Settings,
        PortScansPage PortScans,
        AlertsPage Alerts,
        SystemLogsPage Logs,
        SupportPage Support);

    private sealed record ShellServices(
        LocalDatabase Db,
        SubnetScanner Scanner,
        ConnectivityMonitor Monitor,
        IntrusionDetector Detector,
        ConcurrentDictionary<string, NetworkNode> ActiveNodesMap,
        CancellationTokenSource Cts);

    private static Action<string> SetupPageNavigation(
        SideNavBar sideNav,
        ShellPages pages)
    {
        void ShowPage(string name)
        {
            pages.Dashboard.IsVisible = name == PageDashboard;
            pages.Scanner.IsVisible = name == PageRadar;
            pages.Traceroute.IsVisible = name == PageTraceroute;
            pages.Inventory.IsVisible = name == PageInventory;
            pages.Settings.IsVisible = name == PageSettings;
            pages.PortScans.IsVisible = name == PagePortScans;
            pages.Alerts.IsVisible = name == PageAlerts;
            pages.Logs.IsVisible = name == PageLogs;
            pages.Support.IsVisible = name == PageSupport;

            // Refresh active page data
            if (name == PageDashboard) pages.Dashboard.RefreshData();
            if (name == PageInventory) pages.Inventory.RefreshData();
            if (name == PageSettings) pages.Settings.Refresh();
            if (name == PageAlerts) pages.Alerts.RefreshAlerts();
            if (name == PageLogs)
            {
                // Issue 9: Clear filters when navigating directly to logs page
                pages.Logs.ClearAllFilters();
                pages.Logs.RefreshLogs();
            }
        }

        sideNav.PageChanged += ShowPage;
        return ShowPage;
    }

    private static void WireScannerAndAlertEvents(
        ShellPages pages,
        ConnectivityMonitor monitor,
        ConcurrentDictionary<string, NetworkNode> activeNodesMap,
        LocalDatabase db,
        Action syncGlobalStats)
    {
        // ── Scanner page data changes → refresh dashboard ──
        pages.Scanner.DataChanged += () =>
        {
            Task.Run(() =>
            {
                int count = activeNodesMap.Values.Count(n => n.IsOnline);
                try { db.Log(LogLevel.Info, LogScanner, $"Scan discovered {count} devices"); } catch { }
                syncGlobalStats();
            });
        };

        pages.Alerts.AlertsChanged += () =>
        {
            Task.Run(() =>
            {
                db.Checkpoint();
                syncGlobalStats();
            });
        };

        // ── Scanner device save → register device (I9) ──
        pages.Scanner.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            activeNodesMap.UpdateNode(node);
            Task.Run(() =>
            {
                db.UpdateRegistration(node);
                try { db.Log(LogLevel.Info, LogScanner, $"Device '{node.DisplayName}' permanently registered", node.MacAddress); } catch { }
                db.Checkpoint();
                syncGlobalStats();
            });
        };
    }

    private static void WireInventoryEvents(
        ShellPages pages,
        ConnectivityMonitor monitor,
        ConcurrentDictionary<string, NetworkNode> activeNodesMap,
        LocalDatabase db,
        Action syncGlobalStats)
    {
        // ── Inventory page events ──
        pages.Inventory.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            activeNodesMap.UpdateNode(node);
            Task.Run(() =>
            {
                try { db.Log(LogLevel.Info, LogInventory, $"Device '{node.DisplayName}' saved", node.MacAddress); } catch { }
                db.Checkpoint();
                syncGlobalStats();
            });
        };

        pages.Inventory.DeviceDeleted += (node) =>
        {
            activeNodesMap.TryRemove(node.MacAddress, out _);
            Task.Run(() =>
            {
                try { db.Log(LogLevel.Info, LogInventory, $"Device '{node.DisplayName}' deleted", node.MacAddress); } catch { }
                db.Checkpoint();
                syncGlobalStats();
            });
        };

        pages.Inventory.DevicesDeleted += (nodes) =>
        {
            foreach (var node in nodes)
            {
                activeNodesMap.TryRemove(node.MacAddress, out _);
            }
            Task.Run(() =>
            {
                try { db.Log(LogLevel.Info, LogInventory, $"Bulk deleted {nodes.Count()} devices"); } catch { }
                db.Checkpoint();
                syncGlobalStats();
            });
        };

        pages.Inventory.DeviceStatusChanged += (node) =>
        {
            activeNodesMap.UpdateNode(node);
            syncGlobalStats();
        };

        pages.Inventory.DeviceSelected += (node) =>
        {
            // Fix Issue 4: Fetch history when device is selected in inventory
            Task.Run(() =>
            {
                var history = db.GetUptimeHistory(node.MacAddress, 24);
                Dispatcher.UIThread.Post(() => pages.Inventory.UpdateUptimeChart(history));
            });
        };
    }

    private static void WireNavigationEvents(
        ShellPages pages,
        SideNavBar sideNav,
        Action<string> showPage)
    {
        // ── Dashboard device selection → switch to inventory ──
        pages.Dashboard.DeviceSelected += (node) =>
        {
            sideNav.SetActive(PageInventory);
            showPage(PageInventory);
            pages.Inventory.ShowDevice(node);
        };

        // ── Dashboard View Logs → navigate to alerts page (B6) ──
        pages.Dashboard.ViewLogsRequested += () =>
        {
            sideNav.SetActive(PageAlerts);
            showPage(PageAlerts);
        };

        // ── Inventory View Logs → navigate to logs page with device filter (B7/U12) ──
        pages.Inventory.ViewLogsRequested += (mac) =>
        {
            pages.Logs.ClearDeviceFilter(); // Reset first to ensure clean state
            pages.Logs.FilterByDevice(mac);
            sideNav.SetActive(PageLogs);
            showPage(PageLogs);
        };
    }

    private static void WirePageEvents(
        SideNavBar sideNav,
        ShellPages pages,
        ConnectivityMonitor monitor,
        ConcurrentDictionary<string, NetworkNode> activeNodesMap,
        LocalDatabase db,
        Action<string> showPage,
        Action syncGlobalStats)
    {
        WireScannerAndAlertEvents(pages, monitor, activeNodesMap, db, syncGlobalStats);
        WireInventoryEvents(pages, monitor, activeNodesMap, db, syncGlobalStats);
        WireNavigationEvents(pages, sideNav, showPage);
    }

    private static void WireMonitorEvents(
        ConnectivityMonitor monitor,
        ConcurrentDictionary<string, NetworkNode> activeNodesMap,
        AlertsPage alertsPage,
        Action syncGlobalStats)
    {
        monitor.AlertTriggered += (alert) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (alertsPage.IsVisible) alertsPage.RefreshAlerts();
                syncGlobalStats();
            });
        };

        monitor.DeviceWentOffline += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IntrusionAlerter.AlertDeviceOffline(node);
                activeNodesMap.UpdateNode(node);
                syncGlobalStats();
            });
        };

        monitor.DeviceCameOnline += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IntrusionAlerter.AlertDeviceReconnected(node);
                activeNodesMap.UpdateNode(node);
                syncGlobalStats();
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
                syncGlobalStats();
            });
        };
    }

    /// <summary>
    /// Verifies the system integrity by checking the binary file hash against the last known good hash.
    /// </summary>
    private static void VerifySystemIntegrity(AppSettings settings, LocalDatabase db)
    {
        try
        {
            // We verify the core logic DLL/executable instead of the dynamic database
            string mainFile = Environment.ProcessPath ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NodeRadarPro.dll");

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
                db.Log(LogLevel.Info, LogSecurity, "System integrity signature locked.");
            }
            else if (!SecurityService.VerifyHash(settings.LastKnownGoodHash, currentHash))
            {
                db.Log(LogLevel.Warning, LogSecurity, "CORE INTEGRITY MISMATCH: Application binary may have been tampered with!");
            }
            else
            {
                db.Log(LogLevel.Info, LogSecurity, "System integrity verified (SHA256).");
            }
        }
        catch (Exception ex)
        {
            db.Log(LogLevel.Warning, LogSecurity, $"Integrity check deferred: {ex.Message}");
        }
    }
}
