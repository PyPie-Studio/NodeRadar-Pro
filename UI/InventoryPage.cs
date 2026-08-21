using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using NodeRadarPro.Core.Messaging;
using NodeRadarPro.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Diagnostics;

namespace NodeRadarPro.UI;

/// <summary>
/// Device Inventory page — matches Device_Inventory_Form screenshot exactly.
/// Left: search + filter chips + device card list. Right: glass header + uptime + alert config + telemetry.
/// </summary>
public class InventoryPage : Border
{
    private readonly LocalDatabase _db;
    private readonly ConnectivityMonitor _monitor;
    private List<NetworkNode> _activeNodes;

    // Selection mode state
    private bool _isSelectionMode;
    private readonly HashSet<string> _selectedMacs = new();
    private readonly Button _selectModeBtn;
    private readonly CheckBox _selectAllCheckbox;

    // Left panel
    private readonly StackPanel _deviceListContainer;
    private readonly TextBox _searchBox;
    private readonly TextBlock _deviceCountText;
    private string? _selectedMac;
    private string _activeFilter = "all";
    private readonly StackPanel _chipPanel;

    private readonly Border _detailArea;
    private readonly Border _emptyDetail;
    private readonly Border _bulkArea;
    private readonly Button _bulkDeleteBtn;
    private readonly TextBlock _bulkTitle;
    private NetworkNode? _currentNode;
    private readonly UptimeChartControl _uptimeChart;
    private readonly Grid _timeAxis;
    private int _uptimeHours = 24;

    // Detail controls
    private readonly TextBlock _detailName;
    private readonly TextBlock _detailSubtitle;
    private readonly Border _detailIpPill;
    private readonly Border _detailMacPill;
    private readonly Border _statusDot;
    private readonly Border _threatBadge;

    private readonly TextBlock _latencyStatText;
    private readonly TextBlock _packetLossText;
    private readonly TextBlock _lastScanText;
    private readonly TextBox _nameInput;
    private readonly TextBox _deviceNameInput;
    private readonly TextBox _deviceModelInput;
    private readonly TextBox _exactModelInput;
    private readonly TextBox _locationInput;
    private readonly TextBox _notesInput;
    private readonly Button _saveBtn;
    private readonly Button _deleteBtn;
    private readonly Button _pingBtn;
    private readonly Button _wakeBtn;
    private readonly Button _portScanBtn;
    private readonly TextBlock _pingResult;
    private readonly TextBlock _portResult;
    private readonly Border _deviceIconBox;

    // Alert toggles
    private readonly CheckBox _connLostToggle;
    private readonly CheckBox _highLatencyToggle;

    public event Action<NetworkNode>? DeviceSaved;
    public event Action<NetworkNode>? DeviceDeleted;
    public event Action<IEnumerable<NetworkNode>>? DevicesDeleted;
    public event Action<NetworkNode>? DeviceStatusChanged;
    public event Action<NetworkNode>? DeviceSelected;
    public event Action<string>? ViewLogsRequested;

    public InventoryPage(LocalDatabase db, ConnectivityMonitor monitor, List<NetworkNode> activeNodes)
    {
        _db = db;
        _monitor = monitor;
        _activeNodes = activeNodes;
        Background = ThemeTokens.Surface;

        // Subscribe to updates
        EventAggregator.Instance.Subscribe<NodesUpdatedMessage>(msg =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var oldMacs = _activeNodes.Select(n => n.MacAddress).ToHashSet();
                var newMacs = msg.Nodes.Select(n => n.MacAddress).ToHashSet();
                
                bool membershipChanged = !oldMacs.SetEquals(newMacs);
                _activeNodes = msg.Nodes.ToList();
                
                if (membershipChanged)
                {
                    RefreshData();
                }
                else
                {
                    // Only update statuses/details of existing cards without full rebuild
                    UpdateExistingCards();
                }
            });
        });

        // ═══════════════════════
        // LEFT: Search + Filter + Device List
        // ═══════════════════════
        var searchIcon = new TextBlock
        {
            Text = "🔍",
            FontSize = 13,
            Foreground = ThemeTokens.OnSurfaceVariant,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        _searchBox = ThemeTokens.Input("Search devices, IPs, or tags...");
        _searchBox.Background = Brushes.Transparent;
        _searchBox.Padding = new Thickness(0, 10);
        _searchBox.TextChanged += (s, e) => RefreshDeviceList();
        ThemeTokens.SetToolTip(_searchBox, "Filter the device list by IP, MAC, Vendor, or Custom Name.");

        var searchWrap = new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(ThemeTokens.InputRadius),
            Padding = new Thickness(14, 0, 8, 0),
            Margin = new Thickness(12, 12, 12, 8),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { searchIcon, _searchBox }
            }
        };

        // Filter chips — built as field, rebuilt in RefreshDeviceList (B2)
        _chipPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(12, 0, 12, 8)
        };

        // Device list header
        _deviceCountText = ThemeTokens.Body("(0)", 14);
        _deviceCountText.Margin = new Thickness(6, 0, 0, 0);

        _selectModeBtn = ThemeTokens.TertiaryButton("Select");
        _selectModeBtn.Padding = new Thickness(8, 4);
        _selectModeBtn.Click += (s, e) => ToggleSelectionMode();

        _selectAllCheckbox = new CheckBox { IsVisible = false, Margin = new Thickness(4, 0, 0, 0) };
        _selectAllCheckbox.IsCheckedChanged += (s, e) => ToggleSelectAll(_selectAllCheckbox.IsChecked == true);

        var listTitleRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                ThemeTokens.Headline("Saved Devices", 18),
                _deviceCountText,
                _selectAllCheckbox
            }
        };

        var addBtn = new Button
        {
            Content = "+",
            Background = Brushes.Transparent,
            Foreground = ThemeTokens.OnSurfaceVariant,
            Width = 28, Height = 28,
            FontSize = 18,
            CornerRadius = new CornerRadius(6),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0)
        };
        addBtn.Click += OnAddDevice;
        ThemeTokens.SetToolTip(addBtn, "Manually register a new static device in the database.");

        var listHeader = new Grid { Margin = new Thickness(16, 8, 16, 8) };
        listHeader.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        listHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        listHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        Grid.SetColumn(listTitleRow, 0);
        Grid.SetColumn(_selectModeBtn, 1);
        Grid.SetColumn(addBtn, 2);

        listHeader.Children.Add(listTitleRow);
        listHeader.Children.Add(_selectModeBtn);
        listHeader.Children.Add(addBtn);

        _deviceListContainer = new StackPanel { Spacing = 4, Margin = new Thickness(8, 0) };

        var listScroll = new ScrollViewer
        {
            Content = _deviceListContainer,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        // ═══════════════════════
        // SELECTION BAR (contextual)
        // ═══════════════════════
        _bulkTitle = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.OnSurface };
        _bulkDeleteBtn = ThemeTokens.DangerButton("Delete");
        _bulkDeleteBtn.Padding = new Thickness(12, 6);
        _bulkDeleteBtn.Click += OnBulkDeleteClicked;

        var exitBtn = ThemeTokens.SecondaryButton("Exit");
        exitBtn.Padding = new Thickness(12, 6);
        exitBtn.Click += (s, e) => ToggleSelectionMode();

        var selectionContent = new Grid
        {
            Margin = new Thickness(16, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            Children =
            {
                _bulkTitle,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { _bulkDeleteBtn, exitBtn } }
            }
        };
        Grid.SetColumn(_bulkTitle, 0);
        Grid.SetColumn(selectionContent.Children[1], 2); // The stackpanel
        
        _bulkArea = new Border
        {
            Background = ThemeTokens.SurfaceContainerHigh,
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            IsVisible = false,
            Child = selectionContent
        };

        // Assemble left side
        var listDock = new DockPanel();
        DockPanel.SetDock(searchWrap, Dock.Top);
        DockPanel.SetDock(_chipPanel, Dock.Top);
        DockPanel.SetDock(listHeader, Dock.Top);
        DockPanel.SetDock(_bulkArea, Dock.Top);
        listDock.Children.Add(searchWrap);
        listDock.Children.Add(_chipPanel);
        listDock.Children.Add(listHeader);
        listDock.Children.Add(_bulkArea);
        listDock.Children.Add(listScroll);

        var leftCard = ThemeTokens.Card(listDock, ThemeTokens.SurfaceContainerLow, 0);

        // ═══════════════════════
        // RIGHT: Detail Panel
        // ═══════════════════════

        // Empty state
        _emptyDetail = new Border
        {
            Child = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "⊞", FontSize = 48, Foreground = new SolidColorBrush(Color.Parse("#4C4452"), 0.3), HorizontalAlignment = HorizontalAlignment.Center },
                    ThemeTokens.Headline("Select a Device", 22),
                    ThemeTokens.Body("Click on a device from the list to view full details.", 13)
                }
            }
        };

        // Glass header
        _deviceIconBox = new Border
        {
            Width = 60, Height = 60,
            CornerRadius = new CornerRadius(12),
            Background = ThemeTokens.SurfaceContainerLowest,
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 20, 0)
        };

        _statusDot = ThemeTokens.StatusDot(true, 10);
        _statusDot.VerticalAlignment = VerticalAlignment.Center;
        _statusDot.Margin = new Thickness(8, 0, 0, 0);

        _threatBadge = new Border
        {
            Padding = new Thickness(8, 3),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
            Child = new TextBlock { FontSize = 10, FontWeight = FontWeight.Bold, FontFamily = new FontFamily("Inter") }
        };

        _detailName = new TextBlock
        {
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeTokens.OnSurface,
            FontFamily = new FontFamily("Inter"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var nameStatusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { _detailName, _statusDot, _threatBadge }
        };

        _detailSubtitle = ThemeTokens.Body("", 14);
        _detailSubtitle.Margin = new Thickness(0, 4, 0, 10);

        _detailIpPill = MakeInfoPill("IP:", "—");
        _detailMacPill = MakeInfoPill("MAC:", "—");
        var pillRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { _detailIpPill, _detailMacPill }
        };

        // Scan Ports button (top-right of glass header, matching screenshot)
        _portScanBtn = new Button
        {
            Content = new StackPanel
            {
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = "Scan", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.Tertiary, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = new FontFamily("Inter") },
                    new TextBlock { Text = "Ports", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.Tertiary, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = new FontFamily("Inter") }
                }
            },
            Background = Brushes.Transparent,
            Width = 80, Height = 60,
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.Parse("#4CD7F6"), 0.3),
            BorderThickness = new Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        _portScanBtn.Click += OnPortScanClicked;

        var headerTextCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { nameStatusRow, _detailSubtitle, pillRow } };

        var headerLeftGroup = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { _deviceIconBox, headerTextCol }
        };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(headerLeftGroup, 0);
        Grid.SetColumn(_portScanBtn, 1);
        _portScanBtn.VerticalAlignment = VerticalAlignment.Top;
        headerGrid.Children.Add(headerLeftGroup);
        headerGrid.Children.Add(_portScanBtn);

        var glassHeader = ThemeTokens.GlassCard(headerGrid, 24);

        // ═══════════════════════
        // UPTIME HISTORY (real chart — I1)
        // ═══════════════════════
        var uptimeIcon = ThemeTokens.VectorIcon(ThemeTokens.SvgChart, 18, ThemeTokens.NavTextInactive);
        var uptimeTitle = ThemeTokens.Headline("Uptime History", 18);

        var tab24h = MakeTimeTab("24h", true);
        var tab7d = MakeTimeTab("7d", false);
        var tab30d = MakeTimeTab("30d", false);
        var tabRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0, Children = { tab24h, tab7d, tab30d } };

        // B13: Wire time tabs to change chart data range
        tab24h.PointerPressed += (s, e) => { _uptimeHours = 24; RefreshUptimeChart(); RefreshTimeTabs(tab24h, tab7d, tab30d); };
        tab7d.PointerPressed += (s, e) => { _uptimeHours = 168; RefreshUptimeChart(); RefreshTimeTabs(tab7d, tab24h, tab30d); };
        tab30d.PointerPressed += (s, e) => { _uptimeHours = 720; RefreshUptimeChart(); RefreshTimeTabs(tab30d, tab24h, tab7d); };

        var uptimeHeader = new Grid();
        uptimeHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        uptimeHeader.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        uptimeHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var uptimeTitleGroup = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { uptimeIcon, uptimeTitle } };
        Grid.SetColumn(uptimeTitleGroup, 0);
        Grid.SetColumn(tabRow, 2);
        uptimeHeader.Children.Add(uptimeTitleGroup);
        uptimeHeader.Children.Add(tabRow);

        // I1: Real uptime chart using custom UptimeChartControl
        _uptimeChart = new UptimeChartControl
        {
            Height = 120,
            Margin = new Thickness(0, 16, 0, 8)
        };

        // Time axis labels — now dynamic based on current time (B13)
        _timeAxis = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        for (int i = 0; i < 5; i++)
            _timeAxis.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        
        RefreshTimeAxis();

        var uptimeContent = new StackPanel { Children = { uptimeHeader, _uptimeChart, _timeAxis } };
        var uptimeCard = ThemeTokens.Card(uptimeContent, ThemeTokens.SurfaceContainerHigh, 24);

        // ═══════════════════════
        // BENTO: Alert Config + Telemetry
        // ═══════════════════════

        // Alert Configuration
        var alertIcon = ThemeTokens.VectorIcon(ThemeTokens.SvgShield, 18, ThemeTokens.NavTextInactive);
        var alertTitle = ThemeTokens.Headline("Alert Configuration", 16);
        var alertTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 16), Children = { alertIcon, alertTitle } };

        _connLostToggle = new CheckBox { IsChecked = true };
        _highLatencyToggle = new CheckBox { IsChecked = false };

        // Auto-save alert preferences when toggled
        _connLostToggle.IsCheckedChanged += (s, e) =>
        {
            if (_currentNode != null)
            {
                _currentNode.AlertOnConnectionLost = _connLostToggle.IsChecked == true;
                _db.UpdateDeviceAlertPrefs(_currentNode.MacAddress, _currentNode.AlertOnConnectionLost, _currentNode.AlertOnHighLatency);
            }
        };
        _highLatencyToggle.IsCheckedChanged += (s, e) =>
        {
            if (_currentNode != null)
            {
                _currentNode.AlertOnHighLatency = _highLatencyToggle.IsChecked == true;
                _db.UpdateDeviceAlertPrefs(_currentNode.MacAddress, _currentNode.AlertOnConnectionLost, _currentNode.AlertOnHighLatency);
            }
        };

        var alertContent = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                alertTitleRow,
                MakeAlertToggleRow("Connection Lost", "Alert when node drops off radar.", _connLostToggle),
                MakeAlertToggleRow("High Latency", "Spikes above 100ms threshold.", _highLatencyToggle)
            }
        };
        var alertCard = ThemeTokens.Card(alertContent, ThemeTokens.SurfaceContainerHigh, 20);

        // Telemetry Snapshot
        var telIcon = ThemeTokens.VectorIcon("M12,2A10,10 0 1,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 1,1 4,12A8,8 0 0,1 12,4M12,9A3,3 0 1,0 15,12A3,3 0 0,0 12,9Z", 18, ThemeTokens.Primary);
        var telTitle = ThemeTokens.Headline("Telemetry Snapshot", 16);
        var telTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 12), Children = { telIcon, telTitle } };

        _latencyStatText = new TextBlock { Text = "—", FontSize = 22, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter") };
        _packetLossText = new TextBlock { Text = "0.0", FontSize = 22, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") };
        _lastScanText = ThemeTokens.Body("—", 14);
        _lastScanText.FontWeight = FontWeight.Medium;

        var latLabel = ThemeTokens.Label("AVG\nLATENCY", 10);
        latLabel.LetterSpacing = 1;
        var pktLabel = ThemeTokens.Label("PACKET\nLOSS", 10);
        pktLabel.LetterSpacing = 1;

        var latBlock = new StackPanel { Spacing = 4, Children = { latLabel, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { _latencyStatText, new TextBlock { Text = "ms", FontSize = 12, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 3) } } } } };
        var pktBlock = new StackPanel { Spacing = 4, Children = { pktLabel, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { _packetLossText, new TextBlock { Text = "%", FontSize = 12, Foreground = ThemeTokens.OnSurfaceVariant, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 3) } } } } };

        var statGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) },
            Margin = new Thickness(0, 0, 0, 12)
        };
        var latBorder = new Border { Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 4, 0), Child = latBlock };
        var pktBorder = new Border { Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(4, 0, 0, 0), Child = pktBlock };
        Grid.SetColumn(latBorder, 0);
        Grid.SetColumn(pktBorder, 1);
        statGrid.Children.Add(latBorder);
        statGrid.Children.Add(pktBorder);

        var lastScanLabel = ThemeTokens.Label("LAST SCAN", 10);
        lastScanLabel.LetterSpacing = 1;
        var viewLogsLink = new TextBlock { Text = "View Logs →", FontSize = 12, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter"), HorizontalAlignment = HorizontalAlignment.Right, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
        viewLogsLink.PointerPressed += (s, e) => { if (_currentNode != null) ViewLogsRequested?.Invoke(_currentNode.MacAddress); };

        var lastScanRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        lastScanRow.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        lastScanRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var lastScanLeft = new StackPanel { Spacing = 2, Children = { lastScanLabel, _lastScanText } };
        Grid.SetColumn(lastScanLeft, 0);
        Grid.SetColumn(viewLogsLink, 1);
        viewLogsLink.VerticalAlignment = VerticalAlignment.Bottom;
        lastScanRow.Children.Add(lastScanLeft);
        lastScanRow.Children.Add(viewLogsLink);

        var telContent = new StackPanel { Children = { telTitleRow, statGrid, lastScanRow } };
        var telCard = ThemeTokens.Card(telContent, ThemeTokens.SurfaceContainerHigh, 20);

        var bentoGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) },
            Margin = new Thickness(0, 16, 0, 0)
        };
        Grid.SetColumn(alertCard, 0);
        Grid.SetColumn(telCard, 1);
        alertCard.Margin = new Thickness(0, 0, 8, 0);
        telCard.Margin = new Thickness(8, 0, 0, 0);
        bentoGrid.Children.Add(alertCard);
        bentoGrid.Children.Add(telCard);

        // ═══════════════════════
        // REGISTRATION FORM (below bento)
        // ═══════════════════════
        _nameInput = ThemeTokens.Input("Custom Name...");
        ThemeTokens.SetToolTip(_nameInput, "Assign a unique nickname to this device for easier identification.");
        
        _deviceNameInput = ThemeTokens.Input("Device Name...");
        _deviceNameInput.IsReadOnly = true;
        _deviceNameInput.IsHitTestVisible = false; // Prevent keyboard focus/cursor
        _deviceNameInput.Opacity = 0.7;
        ThemeTokens.SetToolTip(_deviceNameInput, "The official hostname reported by the device (Read-only).");

        _deviceModelInput = ThemeTokens.Input("Device Model...");
        ThemeTokens.SetToolTip(_deviceModelInput, "The hardware model or version identified during scanning.");

        _exactModelInput = ThemeTokens.Input("Deep Intelligence Model...");
        _exactModelInput.IsReadOnly = true;
        _exactModelInput.IsHitTestVisible = false; // Prevent keyboard focus/cursor
        _exactModelInput.Opacity = 0.9;
        _exactModelInput.Foreground = ThemeTokens.Tertiary;
        ThemeTokens.SetToolTip(_exactModelInput, "High-accuracy model identified via mDNS, SSDP, or HTTP banners.");

        _locationInput = ThemeTokens.Input("Location...");
        ThemeTokens.SetToolTip(_locationInput, "Specify the physical location of this device (e.g., Office, Server Room).");

        _notesInput = ThemeTokens.Input("Notes...");
        _notesInput.AcceptsReturn = true;
        _notesInput.Height = 60;
        _notesInput.TextWrapping = TextWrapping.Wrap;
        ThemeTokens.SetToolTip(_notesInput, "Additional technical details or administrative notes.");

        _saveBtn = ThemeTokens.PrimaryButton("💾  Save & Register");
        ThemeTokens.SetToolTip(_saveBtn, "Commit these changes and permanently register this device in the database.");
        _saveBtn.Click += OnSaveClicked;

        _pingBtn = ThemeTokens.SecondaryButton("◎  Ping Device");
        ThemeTokens.SetToolTip(_pingBtn, "Send a live ICMP ping to check device responsiveness.");
        _pingBtn.Click += OnPingClicked;

        _wakeBtn = ThemeTokens.SecondaryButton("⚡  Wake Device");
        ThemeTokens.SetToolTip(_wakeBtn, "Send a Wake-on-LAN Magic Packet to power on this device remotely.");
        _wakeBtn.Click += OnWakeClicked;

        _deleteBtn = ThemeTokens.DangerButton("🗑  Delete Device");
        ThemeTokens.SetToolTip(_deleteBtn, "Remove this device permanently from the database.");
        _deleteBtn.Click += OnDeleteClicked;

        var webBtn = ThemeTokens.TertiaryButton("🌐  Open Web UI");
        ThemeTokens.SetToolTip(webBtn, "Open this device's IP in your default web browser.");
        webBtn.Click += (s, e) => {
            if (_currentNode != null) {
                try {
                    if (Uri.TryCreate($"http://{_currentNode.IpAddress}", UriKind.Absolute, out Uri? uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                        (uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)) {
                        AppUtils.OpenSafeUrl(uri.AbsoluteUri);
                    }
                }
                catch { }
            }
        };

        _pingResult = ThemeTokens.Body("", 12);
        _portResult = ThemeTokens.Body("", 11);
        _portResult.TextWrapping = TextWrapping.Wrap;

        var regLabel = ThemeTokens.SectionLabel("REGISTRATION");
        var diagLabel = ThemeTokens.SectionLabel("DIAGNOSTICS");

        var formContent = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(0, 16, 0, 0),
            Children =
            {
                regLabel,
                MakeFieldLabel("Custom Name"), _nameInput,
                MakeFieldLabel("Device Name"), _deviceNameInput,
                MakeFieldLabel("Deep Intelligence Model"), _exactModelInput,
                MakeFieldLabel("Device Model"), _deviceModelInput,
                MakeFieldLabel("Location"), _locationInput,
                MakeFieldLabel("Notes"), _notesInput,
                new Panel { Height = 6 },
                _saveBtn,
                diagLabel,
                webBtn, // Added Web UI button
                _pingBtn,
                _wakeBtn,
                _pingResult,
                _portResult,
                new Panel { Height = 6 },
                _deleteBtn
            }
        };

        // Assemble right detail content
        var detailContent = new StackPanel
        {
            Spacing = 0,
            Children = { glassHeader, uptimeCard, bentoGrid, formContent }
        };
        uptimeCard.Margin = new Thickness(0, 16, 0, 0);

        _detailArea = new Border
        {
            Child = new ScrollViewer
            {
                Content = detailContent,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Margin = new Thickness(4)
            },
            IsVisible = false
        };

        var rightCol = new Grid();
        rightCol.Children.Add(_emptyDetail);
        rightCol.Children.Add(_detailArea);

        // ═══════════════════════
        // ROOT GRID
        // ═══════════════════════
        var rootGrid = new Grid
        {
            Margin = new Thickness(24),
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(2.2, GridUnitType.Star))
            }
        };

        Grid.SetColumn(leftCard, 0);
        Grid.SetColumn(rightCol, 1);
        leftCard.Margin = new Thickness(0, 0, 12, 0);
        rightCol.Margin = new Thickness(12, 0, 0, 0);
        rootGrid.Children.Add(leftCard);
        rootGrid.Children.Add(rightCol);

        Child = rootGrid;
    }

    // ══════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════

    public void RefreshData()
    {
        RefreshDeviceList();
        if (_currentNode != null)
        {
            var updated = _activeNodes.FirstOrDefault(n => n.MacAddress == _currentNode.MacAddress);
            if (updated != null) { _currentNode = updated; RefreshDetailView(); }
        }
    }

    private void ToggleSelectionMode()
    {
        _isSelectionMode = !_isSelectionMode;
        _selectedMacs.Clear();
        _selectModeBtn.Content = _isSelectionMode ? "Exit" : "Select";
        _selectAllCheckbox.IsVisible = _isSelectionMode;
        _selectAllCheckbox.IsChecked = false;
        _deviceCountText.IsVisible = !_isSelectionMode;
        
        _bulkArea.IsVisible = _isSelectionMode && _selectedMacs.Count > 0;
        
        if (!_isSelectionMode)
        {
            if (_currentNode != null) _detailArea.IsVisible = true;
            else _emptyDetail.IsVisible = true;
        }
        else
        {
            UpdateBulkView();
        }
        
        RefreshDeviceList();
    }

    private void ToggleSelectAll(bool selected)
    {
        if (selected)
        {
            foreach (var node in _activeNodes) _selectedMacs.Add(node.MacAddress);
        }
        else
        {
            _selectedMacs.Clear();
        }
        UpdateBulkView();
        RefreshDeviceList();
    }

    private void UpdateBulkView()
    {
        _bulkTitle.Text = $"{_selectedMacs.Count} selected";
        _bulkDeleteBtn.Content = "Delete";
        _bulkDeleteBtn.IsEnabled = _selectedMacs.Count > 0;
        
        // Contextually show/hide the bar if we are in selection mode
        _bulkArea.IsVisible = _isSelectionMode && _selectedMacs.Count > 0;
    }

    private void OnBulkDeleteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_selectedMacs.Count == 0) return;
        _bulkDeleteBtn.Content = "⚠ Confirm Bulk Delete?";
        _bulkDeleteBtn.Background = Brushes.DarkRed;
        _bulkDeleteBtn.Click -= OnBulkDeleteClicked;
        _bulkDeleteBtn.Click += DoActualBulkDelete;
        
        var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, ev) => { 
            _bulkDeleteBtn.Content = $"🗑 Delete {_selectedMacs.Count} Devices";
            _bulkDeleteBtn.Background = ThemeTokens.ErrorContainer;
            _bulkDeleteBtn.Click -= DoActualBulkDelete;
            _bulkDeleteBtn.Click += OnBulkDeleteClicked;
            timer.Stop(); 
        };
        timer.Start();
    }

    private void DoActualBulkDelete(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var macsToDelete = _selectedMacs.ToList();
        var nodesToDelete = _activeNodes.Where(n => macsToDelete.Contains(n.MacAddress)).ToList();
        
        _db.DeleteDevices(macsToDelete);
        foreach (var mac in macsToDelete) _monitor.RemoveDevice(mac);
        _activeNodes.RemoveAll(n => macsToDelete.Contains(n.MacAddress));
        
        DevicesDeleted?.Invoke(nodesToDelete);
        
        ToggleSelectionMode();
        RefreshDeviceList();
    }

    public void ShowDevice(NetworkNode node)
    {
        _currentNode = node;
        _selectedMac = node.MacAddress;
        _emptyDetail.IsVisible = false;
        _detailArea.IsVisible = true;

        _detailName.Text = node.DisplayName;
        _detailSubtitle.Text = GetRichSubtitle(node);

        UpdatePill(_detailIpPill, node.IpAddress);
        UpdatePill(_detailMacPill, node.MacAddress);
        UpdateStatusDot(node.IsOnline);

        // Update Status Badge
        _threatBadge.IsVisible = node.ThreatLevel != ThreatLevel.Safe || node.VulnerabilityScore > 0;
        if (_threatBadge.IsVisible)
        {
            var (bg, fg, text) = node.ThreatLevel switch
            {
                ThreatLevel.Critical => (ThemeTokens.ErrorContainer, ThemeTokens.Error, "CRITICAL RISK"),
                ThreatLevel.Warning => (new SolidColorBrush(Color.Parse("#FFCE50"), 0.2), new SolidColorBrush(Color.Parse("#FFCE50")), "SECURITY WARNING"),
                _ => (ThemeTokens.PrimaryContainer, ThemeTokens.Primary, "SAFE / AUDITED")
            };
            _threatBadge.Background = bg;
            if (_threatBadge.Child is TextBlock tb) { tb.Foreground = fg; tb.Text = text; }
        }

        _deviceIconBox.Child = ThemeTokens.VectorIcon(GetDeviceSvg(node), 32, node.IsOnline ? ThemeTokens.Tertiary : ThemeTokens.OnSurfaceVariant);

        _latencyStatText.Text = node.IsOnline && node.PingLatencyMs >= 0 ? $"{node.PingLatencyMs}" : "—";
        _packetLossText.Text = $"{node.PacketLossPct:F1}";
        _lastScanText.Text = node.LastSeen != default ? GetTimeAgo(node.LastSeen) : "—";

        _nameInput.Text = node.CustomName;
        _deviceNameInput.Text = node.DeviceName;
        _deviceModelInput.Text = node.DeviceModel;
        _exactModelInput.Text = node.ExactModel;
        _locationInput.Text = node.Location;
        _notesInput.Text = node.Notes;

        _wakeBtn.IsEnabled = !node.IsOnline;

        // Load per-device alert prefs
        _connLostToggle.IsChecked = node.AlertOnConnectionLost;
        _highLatencyToggle.IsChecked = node.AlertOnHighLatency;

        _pingResult.Text = "";
        _portResult.Text = "";
        ResetDeleteButton();
        RefreshUptimeChart();
        RefreshDeviceList();
        DeviceSelected?.Invoke(node);
    }

    // ══════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════

    public void UpdateUptimeChart(List<UptimeSnapshot> history)
    {
        Dispatcher.UIThread.Post(() => _uptimeChart.SetData(history));
    }

    private void RefreshDetailView()
    {
        if (_currentNode == null) return;
        UpdateStatusDot(_currentNode.IsOnline);
        _wakeBtn.IsEnabled = !_currentNode.IsOnline;

        _detailSubtitle.Text = GetRichSubtitle(_currentNode);
        _deviceIconBox.Child = ThemeTokens.VectorIcon(GetDeviceSvg(_currentNode), 32, _currentNode.IsOnline ? ThemeTokens.Tertiary : ThemeTokens.OnSurfaceVariant);

        _latencyStatText.Text = _currentNode.IsOnline && _currentNode.PingLatencyMs >= 0 ? $"{_currentNode.PingLatencyMs}" : "—";
        _packetLossText.Text = $"{_currentNode.PacketLossPct:F1}";
        _lastScanText.Text = _currentNode.LastSeen != default ? GetTimeAgo(_currentNode.LastSeen) : "—";
        RefreshUptimeChart();
    }

    private void RefreshTimeAxis()
    {
        _timeAxis.Children.Clear();
        var now = DateTime.Now;
        string[] labels = new string[5];
        
        for (int i = 0; i < 4; i++)
        {
            var time = now.AddHours(-(_uptimeHours / 4.0 * (4 - i)));
            labels[i] = time.ToString("HH:mm");
        }
        labels[4] = "Now";

        for (int i = 0; i < 5; i++)
        {
            var t = new TextBlock 
            { 
                Text = labels[i], 
                FontSize = 10, 
                Foreground = ThemeTokens.OnSurfaceVariant, 
                HorizontalAlignment = i == 4 ? HorizontalAlignment.Right : (i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Center) 
            };
            Grid.SetColumn(t, i);
            _timeAxis.Children.Add(t);
        }
    }

    private void RefreshUptimeChart()
    {
        if (_currentNode == null)
        {
            _uptimeChart.SetData(new List<UptimeSnapshot>());
            return;
        }
        try
        {
            var history = _db.GetUptimeHistory(_currentNode.MacAddress, _uptimeHours);
            _uptimeChart.SetData(history);
        }
        catch { _uptimeChart.SetData(new List<UptimeSnapshot>()); }
    }

    private static void RefreshTimeTabs(Border active, Border inactive1, Border inactive2)
    {
        active.Background = ThemeTokens.SurfaceContainerHigh;
        active.BorderBrush = ThemeTokens.GhostBorder;
        if (active.Child is TextBlock atb) atb.Foreground = ThemeTokens.OnSurface;

        inactive1.Background = Brushes.Transparent;
        inactive1.BorderBrush = Brushes.Transparent;
        if (inactive1.Child is TextBlock tb1) tb1.Foreground = ThemeTokens.OnSurfaceVariant;

        inactive2.Background = Brushes.Transparent;
        inactive2.BorderBrush = Brushes.Transparent;
        if (inactive2.Child is TextBlock tb2) tb2.Foreground = ThemeTokens.OnSurfaceVariant;
    }

    private void UpdateStatusDot(bool isOnline)
    {
        _statusDot.Background = isOnline ? ThemeTokens.Tertiary : ThemeTokens.Error;
        _statusDot.Opacity = isOnline ? 1.0 : 0.6;
        _statusDot.BoxShadow = isOnline ? new BoxShadows(new BoxShadow { Blur = 8, Color = Color.Parse("#4CD7F6") }) : default;
    }

    private void RefreshDeviceList()
    {
        _deviceListContainer.Children.Clear();

        // Rebuild filter chips with current active state (B2 fix)
        _chipPanel.Children.Clear();
        _chipPanel.Children.Add(MakeFilterChip("All", "all", _activeFilter == "all"));
        _chipPanel.Children.Add(MakeFilterChip("Online", "online", _activeFilter == "online"));
        _chipPanel.Children.Add(MakeFilterChip("Offline", "offline", _activeFilter == "offline"));
        _chipPanel.Children.Add(MakeFilterChip("Critical", "critical", _activeFilter == "critical"));

        string search = _searchBox.Text?.Trim().ToLower() ?? "";

        var filtered = _activeNodes.Where(n =>
        {
            if (_activeFilter == "online" && !n.IsOnline) return false;
            if (_activeFilter == "offline" && n.IsOnline) return false;
            if (_activeFilter == "critical" && (n.IsOnline || !n.IsRegistered)) return false;
            if (_activeFilter == "servers" && n.DeviceType != "Server" && n.DeviceType != "Computer") return false;
            if (_activeFilter == "iot" && n.DeviceType != "IoT") return false;

            if (!string.IsNullOrEmpty(search))
                return n.DisplayName.ToLower().Contains(search) || n.IpAddress.ToLower().Contains(search) || n.MacAddress.ToLower().Contains(search) || (n.Vendor ?? "").ToLower().Contains(search);
            return true;
        })
        .OrderByDescending(n => n.IsOnline)
        .ThenByDescending(n => n.IsRegistered)
        .ThenBy(n => n.DisplayName)
        .ToList();

        _deviceCountText.Text = $"({filtered.Count})";

        if (filtered.Count == 0)
        {
            _deviceListContainer.Children.Add(new TextBlock
            {
                Text = "No devices found.\nRun a scan to discover devices.",
                FontSize = 12,
                Foreground = ThemeTokens.OnSurfaceVariant,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12, 20),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });
            return;
        }

        foreach (var node in filtered) _deviceListContainer.Children.Add(BuildDeviceCard(node));
    }

    private void UpdateExistingCards()
    {
        foreach (var child in _deviceListContainer.Children)
        {
            if (child is Border card && card.Tag is string mac)
            {
                var node = _activeNodes.FirstOrDefault(n => n.MacAddress == mac);
                if (node != null)
                {
                    // Update only specific parts of the card
                    UpdateCardUI(card, node);
                }
            }
        }
        
        // Also update detail view if current node changed status
        if (_currentNode != null) RefreshDetailView();
    }

    private void UpdateCardUI(Border card, NetworkNode node)
    {
        if (card.Child is Grid grid)
        {
            // Update icon
            var leftGroup = grid.Children.OfType<StackPanel>().FirstOrDefault(c => Grid.GetColumn(c) == 0);
            if (leftGroup != null && leftGroup.Children.Count > 0)
            {
                var iconIndex = _isSelectionMode ? 1 : 0;
                if (leftGroup.Children.Count > iconIndex && leftGroup.Children[iconIndex] is Border iconBox)
                {
                    iconBox.Child = ThemeTokens.VectorIcon(GetDeviceSvg(node), 18, node.IsOnline ? ThemeTokens.Tertiary : ThemeTokens.OnSurfaceVariant);
                }
            }

            // Update status badge safely
            var existingBadge = grid.Children.OfType<Border>().FirstOrDefault(c => Grid.GetColumn(c) == 1);
            var newBadge = node.IsOnline ? ThemeTokens.StatusBadge("Online", true) : (node.IsRegistered ? ThemeTokens.StatusBadge("Offline", false) : null);

            if (existingBadge != null)
            {
                if (newBadge == null)
                {
                    grid.Children.Remove(existingBadge);
                }
                else if (existingBadge.Child is TextBlock etb && newBadge.Child is TextBlock ntb && etb.Text != ntb.Text)
                {
                    // Only replace if status actually changed
                    grid.Children.Remove(existingBadge);
                    newBadge.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(newBadge, 1);
                    grid.Children.Add(newBadge);
                }
            }
            else if (newBadge != null)
            {
                newBadge.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(newBadge, 1);
                grid.Children.Add(newBadge);
            }
        }
    }

    private Border BuildDeviceCard(NetworkNode node)
    {
        bool isSelected = node.MacAddress == _selectedMac;

        var icon = ThemeTokens.VectorIcon(GetDeviceSvg(node), 18, node.IsOnline ? ThemeTokens.Tertiary : ThemeTokens.OnSurfaceVariant);

        var iconBox = new Border
        {
            Width = 38, Height = 38,
            CornerRadius = new CornerRadius(8),
            Background = ThemeTokens.SurfaceContainerLowest,
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            Child = icon
        };

        var nameText = new TextBlock { Text = node.DisplayName, FontSize = 13, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.OnSurface, TextTrimming = TextTrimming.CharacterEllipsis, FontFamily = new FontFamily("Inter") };
        var subText = new TextBlock { Text = GetRichSubtitle(node), FontSize = 10, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 1, 0, 0) };
        var ipText = new TextBlock { Text = node.IpAddress, FontSize = 11, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 2, 0, 0) };
        var textCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { nameText, subText, ipText } };
        var leftGroup = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { iconBox, textCol } };

        if (_isSelectionMode)
        {
            var cardCheck = new CheckBox { IsChecked = _selectedMacs.Contains(node.MacAddress), Margin = new Thickness(0, 0, 8, 0), IsHitTestVisible = false };
            leftGroup.Children.Insert(0, cardCheck);
        }

        // Security Risk Dot
        if (node.ThreatLevel != ThreatLevel.Safe)
        {
            var riskDot = new Border
            {
                Width = 6, Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = node.ThreatLevel == ThreatLevel.Critical ? ThemeTokens.Error : new SolidColorBrush(Color.Parse("#FFCE50")),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 0, 0)
            };
            ThemeTokens.SetToolTip(riskDot, $"Security Risk: {node.ThreatLevel}");
            leftGroup.Children.Insert(1, riskDot);
        }

        Border? badge = null;
        if (node.IsOnline) badge = ThemeTokens.StatusBadge("Online", true);
        else if (node.IsRegistered && !node.IsOnline) badge = ThemeTokens.StatusBadge("Offline", false);

        var cardGrid = new Grid();
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        if (badge != null) cardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(leftGroup, 0);
        cardGrid.Children.Add(leftGroup);
        if (badge != null) { badge.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(badge, 1); cardGrid.Children.Add(badge); }

        var card = new Border
        {
            Tag = node.MacAddress,
            Padding = new Thickness(10, 10),
            CornerRadius = new CornerRadius(10),
            Background = isSelected ? ThemeTokens.SurfaceContainerHigh : Brushes.Transparent,
            BorderBrush = isSelected ? ThemeTokens.GhostBorder20 : Brushes.Transparent,
            BorderThickness = new Thickness(1),
            Child = cardGrid,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };

        if (isSelected) card.BoxShadow = new BoxShadows(new BoxShadow { OffsetY = 4, Blur = 20, Color = Color.FromArgb(50, 0, 0, 0) });

        card.PointerEntered += (s, e) => { if (node.MacAddress != _selectedMac) card.Background = ThemeTokens.SurfaceContainerHigh; };
        card.PointerExited += (s, e) => { if (node.MacAddress != _selectedMac) card.Background = Brushes.Transparent; };
        card.PointerPressed += (s, e) => {
            if (_isSelectionMode)
            {
                if (_selectedMacs.Contains(node.MacAddress)) _selectedMacs.Remove(node.MacAddress);
                else _selectedMacs.Add(node.MacAddress);
                UpdateBulkView();
                RefreshDeviceList();
            }
            else ShowDevice(node);
        };

        return card;
    }

    // ══════════════════════════════════
    // EVENT HANDLERS
    // ══════════════════════════════════

    private void OnAddDevice(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var manualNode = new NetworkNode
        {
            IpAddress = "0.0.0.0",
            MacAddress = $"MANUAL-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Hostname = "Manual Entry",
            IsOnline = false, IsRegistered = true,
            FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow
        };
        _activeNodes.Add(manualNode);
        ShowDevice(manualNode);
        DeviceSaved?.Invoke(manualNode);
    }

    private void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;
        _currentNode.CustomName = _nameInput.Text ?? "";
        _currentNode.DeviceName = _deviceNameInput.Text ?? "";
        _currentNode.DeviceModel = _deviceModelInput.Text ?? "";
        _currentNode.Location = _locationInput.Text ?? "";
        _currentNode.Notes = _notesInput.Text ?? "";
        _currentNode.IsRegistered = true;

        _db.UpdateRegistration(_currentNode.MacAddress, _currentNode.CustomName, _currentNode.Notes, _currentNode.Location, _currentNode.DeviceName, _currentNode.DeviceModel, _currentNode.IconPath, _currentNode.IpAddress, _currentNode.VulnerabilityScore, _currentNode.ThreatLevel, _currentNode.ExactModel);
        _detailName.Text = _currentNode.DisplayName;

        _saveBtn.Content = "✅  Saved!";
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        timer.Tick += (s, ev) => { _saveBtn.Content = "💾  Save & Register"; timer.Stop(); };
        timer.Start();
        DeviceSaved?.Invoke(_currentNode);
        RefreshDeviceList();
    }

    private void OnDeleteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;
        _deleteBtn.Content = "⚠  Confirm Delete?";
        _deleteBtn.Background = SolidColorBrush.Parse("#8B0000");
        _deleteBtn.Click -= OnDeleteClicked;
        _deleteBtn.Click += DoActualDelete;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, ev) => { ResetDeleteButton(); timer.Stop(); };
        timer.Start();
    }

    private void DoActualDelete(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;
        _db.DeleteDevice(_currentNode.MacAddress);
        _monitor.RemoveDevice(_currentNode.MacAddress);
        var deletedNode = _currentNode;
        _activeNodes.RemoveAll(n => n.MacAddress == deletedNode.MacAddress);
        _currentNode = null; _selectedMac = null;
        _detailArea.IsVisible = false; _emptyDetail.IsVisible = true;
        ResetDeleteButton(); RefreshDeviceList();
        DeviceDeleted?.Invoke(deletedNode);
    }

    private void ResetDeleteButton()
    {
        _deleteBtn.Content = "🗑  Delete Device";
        _deleteBtn.Background = ThemeTokens.ErrorContainer;
        _deleteBtn.Click -= DoActualDelete;
        _deleteBtn.Click -= OnDeleteClicked;
        _deleteBtn.Click += OnDeleteClicked;
    }

    private async void OnPingClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;
        string ip = _currentNode.IpAddress;
        if (string.IsNullOrEmpty(ip) || ip == "0.0.0.0") { _pingResult.Text = "⚠ Set a valid IP first."; _pingResult.Foreground = ThemeTokens.Error; return; }

        _pingBtn.IsEnabled = false; _pingBtn.Content = "◎  Pinging..."; _pingResult.Text = "";
        try
        {
            var (isOnline, mac, latency) = await SubnetScanner.QuickProbeAsync(ip);
            if (isOnline)
            {
                _pingResult.Text = $"✅ Reachable ({(latency >= 0 ? $"{latency}ms" : "ARP/TCP")})";
                _pingResult.Foreground = ThemeTokens.Tertiary;
                _currentNode.IsOnline = true; _currentNode.PingLatencyMs = latency; _currentNode.LastSeen = DateTime.UtcNow;
                if (mac != "Unknown" && (_currentNode.MacAddress.StartsWith("MANUAL") || _currentNode.MacAddress == "Unknown"))
                {
                    _currentNode.MacAddress = mac;
                    var fingerprint = await NodeRadarPro.Core.Fingerprinting.DeepFingerprintEngine.Instance.FingerprintNodeAsync(_currentNode, System.Threading.CancellationToken.None);
                    _currentNode.Vendor = fingerprint.Vendor;
                    _currentNode.DeviceType = fingerprint.TypeString;
                    _currentNode.OsGuess = fingerprint.Os;
                    _currentNode.IconPath = fingerprint.IconSvgKey;
                    if (!string.IsNullOrEmpty(fingerprint.Model))
                    {
                        if (string.IsNullOrEmpty(_currentNode.ExactModel)) _currentNode.ExactModel = fingerprint.Model;
                        else if (!_currentNode.ExactModel.Contains(fingerprint.Model)) _currentNode.ExactModel = $"{fingerprint.Model} | {_currentNode.ExactModel}";
                    }
                }
            }
            else { _pingResult.Text = "❌ Not reachable"; _pingResult.Foreground = ThemeTokens.Error; _currentNode.IsOnline = false; }
        }
        catch (System.Net.NetworkInformation.PingException ex) { _pingResult.Text = $"❌ {ex.Message}"; _pingResult.Foreground = ThemeTokens.Error; }
        catch (PlatformNotSupportedException ex) { _pingResult.Text = $"❌ {ex.Message}"; _pingResult.Foreground = ThemeTokens.Error; }
        finally
        {
            _pingBtn.IsEnabled = true; _pingBtn.Content = "◎  Ping Device";
            if (_currentNode != null) { UpdateStatusDot(_currentNode.IsOnline); RefreshDetailView(); DeviceStatusChanged?.Invoke(_currentNode); }
        }
    }

    private async void OnWakeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;
        _wakeBtn.IsEnabled = false;
        _wakeBtn.Content = "⚡  Sending...";
        
        bool success = await WakeOnLan.WakeAsync(_currentNode.MacAddress);
        
        if (success)
        {
            _wakeBtn.Content = "✅  Magic Packet Sent";
            _db.Log(LogLevel.Info, "WoL", $"Magic Packet broadcasted to {_currentNode.MacAddress} ({_currentNode.DisplayName})");
        }
        else
        {
            _wakeBtn.Content = "❌  Failed";
        }

        await Task.Delay(2000);
        _wakeBtn.Content = "⚡  Wake Device";
        _wakeBtn.IsEnabled = !_currentNode.IsOnline;
    }

    private async void OnPortScanClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;
        string ip = _currentNode.IpAddress;
        if (string.IsNullOrEmpty(ip) || ip == "0.0.0.0") { _portResult.Text = "⚠ Set a valid IP first."; _portResult.Foreground = ThemeTokens.Error; return; }
        _portScanBtn.IsEnabled = false; _portResult.Text = "";
        try
        {
            var openPorts = await PortScanner.ScanCommonPortsAsync(ip);
            _portResult.Text = openPorts.Count > 0 ? "Open: " + string.Join(", ", openPorts) : "No common ports open.";
            _portResult.Foreground = ThemeTokens.Tertiary;
        }
        catch { _portResult.Text = "Port scan failed."; _portResult.Foreground = ThemeTokens.Error; }
        finally { _portScanBtn.IsEnabled = true; }
    }

    // ══════════════════════════════════
    // UI FACTORY HELPERS
    // ══════════════════════════════════

    private Border MakeFilterChip(string text, string filterKey, bool active)
    {
        var chip = new Border
        {
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12, 5),
            Background = active ? ThemeTokens.PrimaryContainer : Brushes.Transparent,
            BorderBrush = active ? Brushes.Transparent : ThemeTokens.GhostBorder30,
            BorderThickness = new Thickness(1),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Child = new TextBlock { Text = text, FontSize = 12, Foreground = active ? ThemeTokens.OnPrimaryContainer : ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium }
        };
        chip.PointerPressed += (s, e) => { _activeFilter = filterKey; RefreshDeviceList(); };
        return chip;
    }

    private static Border MakeInfoPill(string label, string value)
    {
        var valTb = new TextBlock { Text = value, FontSize = 12, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.SemiBold, Tag = "value" };
        ThemeTokens.AddCopyAction(valTb); // Fix: Remove hardcoded value, let AddCopyAction resolve live text

        return new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 6),
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 6,
                Children =
                {
                    new TextBlock { Text = label, FontSize = 11, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter") },
                    valTb
                }
            }
        };
    }

    private static void UpdatePill(Border pill, string value)
    {
        if (pill.Child is StackPanel sp)
            foreach (var c in sp.Children)
                if (c is TextBlock tb && tb.Tag?.ToString() == "value") tb.Text = value;
    }

    private static Border MakeAlertToggleRow(string title, string description, CheckBox toggle)
    {
        var left = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeight.Medium, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") },
                new TextBlock { Text = description, FontSize = 11, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 2, 0, 0) }
            }
        };
        toggle.Content = null;
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(left, 0);
        Grid.SetColumn(toggle, 1);
        toggle.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(left);
        row.Children.Add(toggle);
        return new Border { Background = ThemeTokens.SurfaceContainerLowest, CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 12), Child = row };
    }

    private static Border MakeTimeTab(string text, bool active) => new()
    {
        Background = active ? ThemeTokens.SurfaceContainerHigh : Brushes.Transparent,
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(12, 5),
        BorderBrush = active ? ThemeTokens.GhostBorder : Brushes.Transparent,
        BorderThickness = new Thickness(1),
        Child = new TextBlock { Text = text, FontSize = 12, Foreground = active ? ThemeTokens.OnSurface : ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium }
    };

    private static TextBlock MakeFieldLabel(string text) => new()
    { Text = text, FontSize = 11, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 4, 0, 2) };

    private static string GetTimeAgo(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        if (diff.TotalSeconds < 10) return "Just now";
        if (diff.TotalMinutes < 1) return $"{(int)diff.TotalSeconds}s ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} mins ago";
        return $"{(int)diff.TotalHours}h ago";
    }

    private string GetDeviceSvg(NetworkNode node)
    {
        string icon = (node.IconPath ?? "").ToLower();
        if (icon == "phone" || icon == "mobile") return ThemeTokens.SvgPhone;
        if (icon == "tablet") return ThemeTokens.SvgTablet;
        if (icon == "server") return ThemeTokens.SvgServer;
        if (icon == "nas") return ThemeTokens.SvgNas;
        if (icon == "router" || icon == "network") return ThemeTokens.SvgRouter;
        if (icon == "switch") return ThemeTokens.SvgSwitch;
        if (icon == "firewall") return ThemeTokens.SvgFirewall;
        if (icon == "accesspoint") return ThemeTokens.SvgAccessPoint;
        if (icon == "printer") return ThemeTokens.SvgPrinter;
        if (icon == "tv") return ThemeTokens.SvgTv;
        if (icon == "iot" || icon == "light") return ThemeTokens.SvgIot;
        if (icon == "speaker") return ThemeTokens.SvgSpeaker;
        if (icon == "camera") return ThemeTokens.SvgCamera;
        if (icon == "dvr") return ThemeTokens.SvgDvr;
        if (icon == "gamepad") return ThemeTokens.SvgGamepad;
        if (icon == "pc" || icon == "laptop") return ThemeTokens.SvgDesktop;

        string type = (node.DeviceType ?? "").ToLower();
        if (type.Contains("router") || type.Contains("gateway") || type.Contains("network")) return ThemeTokens.SvgRouter;
        if (type.Contains("server") || type.Contains("nas")) return ThemeTokens.SvgServer;
        if (type.Contains("phone") || type.Contains("mobile") || type.Contains("iphone")) return ThemeTokens.SvgPhone;
        if (type.Contains("printer")) return ThemeTokens.SvgPrinter;
        if (type.Contains("tv")) return ThemeTokens.SvgTv;
        if (type.Contains("iot") || type.Contains("smart") || type.Contains("bulb") || type.Contains("light")) return ThemeTokens.SvgIot;

        string vendor = (node.Vendor ?? "").ToLower();
        if (vendor.Contains("apple") || vendor.Contains("samsung") || vendor.Contains("xiaomi") || vendor.Contains("google")) return ThemeTokens.SvgPhone;
        if (vendor.Contains("cisco") || vendor.Contains("tp-link") || vendor.Contains("ubiquiti") || vendor.Contains("netgear")) return ThemeTokens.SvgRouter;
        if (vendor.Contains("dell") || vendor.Contains("hp") || vendor.Contains("intel") || vendor.Contains("vmware")) return ThemeTokens.SvgServer;

        return ThemeTokens.SvgDesktop;
    }

    private string GetRichSubtitle(NetworkNode node)
    {
        var parts = new List<string>();
        
        // Prioritize OS Guess as the leading signal for Deep Intelligence
        if (!string.IsNullOrEmpty(node.OsGuess)) parts.Add(node.OsGuess);
        
        if (!string.IsNullOrEmpty(node.Vendor) && node.Vendor != "Unknown Vendor" && node.Vendor != "Unknown") 
            parts.Add(node.Vendor);
            
        if (!string.IsNullOrEmpty(node.DeviceType) && node.DeviceType != "Generic Device") 
            parts.Add(node.DeviceType);

        var uniqueParts = parts.Distinct().ToList();
        
        // Add ExactModel if it contains information not already present
        if (!string.IsNullOrEmpty(node.ExactModel))
        {
            bool alreadyCovered = uniqueParts.Any(p => node.ExactModel.Contains(p, StringComparison.OrdinalIgnoreCase));
            if (!alreadyCovered) uniqueParts.Add(node.ExactModel);
        }

        if (uniqueParts.Count == 0) return "Generic Network Device";
        return string.Join(" • ", uniqueParts);
    }
}
