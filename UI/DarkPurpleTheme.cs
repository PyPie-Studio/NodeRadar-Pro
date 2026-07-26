using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
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

        // 1. Update Check
        if (settings.CheckUpdatesOnStartup)
        {
            _ = Task.Run(() => CheckForUpdatesOnStartupAsync(mainWindow));
        }

        // 2. Auto-Backup Timer
        InitializeAutoBackupTimer(settings);
    }

    private static async Task CheckForUpdatesOnStartupAsync(Window mainWindow)
    {
        try
        {
            var (hasUpdate, version, downloadUrl) = await UpdateService.CheckForUpdatesAsync(ThemeTokens.AppVersion);
            if (hasUpdate)
            {
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    ShowUpdatePrompt(mainWindow, version, downloadUrl);
                });
            }
        }
        catch { }
    }

    private static void ShowUpdatePrompt(Window mainWindow, string version, string downloadUrl)
    {
        if (mainWindow.Content is not Grid rootGrid) return;
        
        var overlay = new Grid
        {
            Background = new SolidColorBrush(Colors.Black, 0.5),
            ZIndex = 1000
        };

        var btnLater = ThemeTokens.SecondaryButton("Later");
        btnLater.Click += (s, e) => rootGrid.Children.Remove(overlay);

        var progressText = ThemeTokens.Body("");
        progressText.IsVisible = false;
        progressText.HorizontalAlignment = HorizontalAlignment.Center;
        progressText.Margin = new Thickness(0, 5);

        var btnUpdate = ThemeTokens.PrimaryButton("Download & Install");
        btnUpdate.Click += async (s, e) => {
            btnUpdate.IsEnabled = false;
            btnLater.IsEnabled = false;
            progressText.IsVisible = true;
            progressText.Text = "Starting download...";

            try {
                await Task.Run(async () => {
                    await UpdateService.DownloadAndInstallAsync(downloadUrl, progress => {
                        Dispatcher.UIThread.Post(() => {
                            progressText.Text = $"Downloading: {progress:F0}%";
                        });
                    });
                });
            } catch (Exception ex) {
                Dispatcher.UIThread.Post(() => {
                    progressText.Text = $"Error: {ex.Message}";
                    btnUpdate.IsEnabled = true;
                    btnLater.IsEnabled = true;
                });
            }
        };

        var prompt = ThemeTokens.GlassCard(new StackPanel
        {
            Spacing = 15,
            Width = 400,
            Children = 
            {
                ThemeTokens.Headline("Update Available", 20),
                ThemeTokens.Body($"A new version ({version}) of NodeRadar Pro is available. Would you like to download and install it now?"),
                progressText,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { btnLater, btnUpdate }
                }
            }
        });

        prompt.HorizontalAlignment = HorizontalAlignment.Center;
        prompt.VerticalAlignment = VerticalAlignment.Center;
        overlay.Children.Add(prompt);
        
        if (rootGrid.ColumnDefinitions.Count > 0)
            Grid.SetColumnSpan(overlay, rootGrid.ColumnDefinitions.Count);
        if (rootGrid.RowDefinitions.Count > 0)
            Grid.SetRowSpan(overlay, rootGrid.RowDefinitions.Count);
            
        rootGrid.Children.Add(overlay);
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
                Task.Run(() => LocalDatabase.Instance.BackupDatabase());
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
        catch { }

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
            SyncGlobalStats();
        };

        // ── Inventory page events ──
        inventoryPage.DeviceSaved += (node) =>
        {
            monitor.AddDevice(node);
            activeNodesMap.AddOrUpdate(node.MacAddress, node, (k, v) => node);
            try { db.Log(LogLevel.Info, "Inventory", $"Device '{node.DisplayName}' saved", node.MacAddress); } catch { }
            SyncGlobalStats();
        };

        inventoryPage.DeviceDeleted += (node) =>
        {
            activeNodesMap.TryRemove(node.MacAddress, out _);
            try { db.Log(LogLevel.Info, "Inventory", $"Device '{node.DisplayName}' deleted", node.MacAddress); } catch { }
            SyncGlobalStats();
        };

        inventoryPage.DevicesDeleted += (nodes) =>
        {
            foreach (var node in nodes)
            {
                activeNodesMap.TryRemove(node.MacAddress, out _);
            }
            try { db.Log(LogLevel.Info, "Inventory", $"Bulk deleted {nodes.Count()} devices"); } catch { }
            SyncGlobalStats();
        };

        inventoryPage.DeviceStatusChanged += (node) =>
        {
            activeNodesMap.AddOrUpdate(node.MacAddress, node, (k, v) => node);
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
            // Fix: Persist to database immediately so it survives app restart
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
            db.Checkpoint();

            monitor.AddDevice(node);
            activeNodesMap.AddOrUpdate(node.MacAddress, node, (k, v) => node);
            try { db.Log(LogLevel.Info, "Scanner", $"Device '{node.DisplayName}' permanently registered", node.MacAddress); } catch { }
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
                activeNodesMap.AddOrUpdate(node.MacAddress, node, (k, v) => node);
                SyncGlobalStats();
            });
        };

        monitor.DeviceCameOnline += (node) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IntrusionAlerter.AlertDeviceReconnected(node);
                activeNodesMap.AddOrUpdate(node.MacAddress, node, (k, v) => node);
                SyncGlobalStats();
            });
        };

        monitor.StatusUpdated += (updatedNodes) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var updated in updatedNodes)
                {
                    activeNodesMap.AddOrUpdate(updated.MacAddress, updated, (k, v) => updated);
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
            settings = await Task.Run(() => db.LoadSettings());

            // Task 2: System Integrity Shield (Verify Binaries)
            _ = Task.Run(() => {
                try {
                    // We verify the core logic DLL instead of the dynamic database
                    string appPath = AppDomain.CurrentDomain.BaseDirectory;
                    string dllPath = System.IO.Path.Combine(appPath, "NodeRadar Pro.dll");
                    
                    if (System.IO.File.Exists(dllPath))
                    {
                        using var stream = new System.IO.FileStream(dllPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                        using var sha256 = System.Security.Cryptography.SHA256.Create();
                        var hashBytes = sha256.ComputeHash(stream);
                        string currentHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                        // On first run or update, lock in the hash
                        if (string.IsNullOrEmpty(settings.LastKnownGoodHash) || settings.LastKnownGoodHash == "INITIAL")
                        {
                            settings.LastKnownGoodHash = currentHash;
                            db.SaveSettings(settings);
                            db.Log(LogLevel.Info, "Security", "System integrity signature locked.");
                        }
                        else if (settings.LastKnownGoodHash != currentHash)
                        {
                            db.Log(LogLevel.Warning, "Security", "CORE INTEGRITY MISMATCH: Application binary may have been tampered with!");
                        }
                        else
                        {
                            db.Log(LogLevel.Info, "Security", "System integrity verified (SHA256).");
                        }
                    }
                } catch (Exception ex) {
                    db.Log(LogLevel.Warning, "Security", $"Integrity check deferred: {ex.Message}");
                }
            });

            // Apply settings to services (Non-UI)
            ApplySettings(settings, monitor, scanner);

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