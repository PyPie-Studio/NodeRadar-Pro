using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace NodeRadarPro.UI.Controls.Inventory;

/// <summary>
/// Composite flyout details panel for Inventory: telemetry stats, uptime chart, device registration form, and action buttons.
/// </summary>
public class InventoryDetailsFlyout : Border
{
    private readonly Border _detailArea;
    private readonly Border _emptyDetail;
    private readonly Border _bulkArea;
    private readonly Button _bulkDeleteBtn;
    private readonly TextBlock _bulkTitle;
    private NetworkNode? _currentNode;
    private readonly UptimeChartControl _uptimeChart;
    private readonly Grid _timeAxis;
    private int _uptimeHours = 24;
    private const string ManualDevicePrefix = "MANUAL-";

    // Detail controls
    private readonly TextBlock _detailName;
    private readonly TextBlock _detailSubtitle;
    private readonly Border _detailIpPill;
    private readonly TextBox _detailIpInput;
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

    private readonly CheckBox _connLostToggle;
    private readonly CheckBox _highLatencyToggle;

    public event Action<NetworkNode>? SaveRequested;
    public event Action<NetworkNode>? DeleteRequested;
    public event Action? BulkDeleteRequested;
    public event Action? ExitBulkRequested;
    public event Action<NetworkNode>? PingRequested;
    public event Action<NetworkNode>? WakeRequested;
    public event Action<NetworkNode>? PortScanRequested;
    public event Action<string>? ViewLogsRequested;
    public event Action<int>? UptimeHoursChanged;

    internal NetworkNode? CurrentNode => _currentNode;
    internal UptimeChartControl UptimeChart => _uptimeChart;
    internal int UptimeHours => _uptimeHours;

    public InventoryDetailsFlyout()
    {
        // ═══════════════════════
        // SELECTION BAR (bulk view)
        // ═══════════════════════
        _bulkTitle = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.OnSurface };
        _bulkDeleteBtn = ThemeTokens.DangerButton("Delete");
        _bulkDeleteBtn.Padding = new Thickness(12, 6);
        _bulkDeleteBtn.Click += (s, e) => BulkDeleteRequested?.Invoke();

        var exitBtn = ThemeTokens.SecondaryButton("Exit");
        exitBtn.Padding = new Thickness(12, 6);
        exitBtn.Click += (s, e) => ExitBulkRequested?.Invoke();

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
        Grid.SetColumn(selectionContent.Children[1], 2);

        _bulkArea = new Border
        {
            Background = ThemeTokens.SurfaceContainerHigh,
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            IsVisible = false,
            Child = selectionContent
        };

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
                    new TextBlock { Text = "⊞", FontSize = 48, Foreground = ThemeTokens.GhostBorder30, HorizontalAlignment = HorizontalAlignment.Center },
                    ThemeTokens.Headline("Select a Device", 22),
                    ThemeTokens.Body("Click on a device from the list to view full details.", 13)
                }
            }
        };

        // Glass header
        _deviceIconBox = new Border
        {
            Width = 60,
            Height = 60,
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
            Child = new TextBlock { FontSize = 10, FontWeight = FontWeight.Bold, FontFamily = ThemeTokens.DefaultFont }
        };

        _detailName = new TextBlock
        {
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeTokens.OnSurface,
            FontFamily = ThemeTokens.DefaultFont,
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
        _detailIpInput = ThemeTokens.Input("192.168.1.1");
        _detailIpInput.Width = 140;
        _detailIpInput.Height = 28;
        _detailIpInput.FontSize = 13;
        _detailIpInput.Padding = new Thickness(8, 4);
        _detailIpInput.VerticalAlignment = VerticalAlignment.Center;
        _detailIpInput.IsVisible = false;
        _detailIpInput.KeyDown += (s, e) => { if (e.Key == Key.Enter && _currentNode != null) SaveRequested?.Invoke(_currentNode); };
        ThemeTokens.SetToolTip(_detailIpInput, "Editable IP address for manually added device.");

        _detailMacPill = MakeInfoPill("MAC:", "—");
        var pillRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { _detailIpPill, _detailIpInput, _detailMacPill }
        };

        _portScanBtn = new Button
        {
            Content = new StackPanel
            {
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = "Scan", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.Tertiary, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ThemeTokens.DefaultFont },
                    new TextBlock { Text = "Ports", FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.Tertiary, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = ThemeTokens.DefaultFont }
                }
            },
            Background = Brushes.Transparent,
            Width = 80,
            Height = 60,
            CornerRadius = new CornerRadius(8),
            BorderBrush = ThemeTokens.TertiaryContainerSubtle,
            BorderThickness = new Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = ThemeTokens.HandCursor
        };
        _portScanBtn.Click += (s, e) => { if (_currentNode != null) PortScanRequested?.Invoke(_currentNode); };

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

        // Uptime History
        var uptimeIcon = ThemeTokens.VectorIcon(ThemeTokens.SvgChart, 18, ThemeTokens.NavTextInactive);
        var uptimeTitle = ThemeTokens.Headline("Uptime History", 18);

        var tab24h = MakeTimeTab("24h", true);
        var tab7d = MakeTimeTab("7d", false);
        var tab30d = MakeTimeTab("30d", false);
        var tabRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0, Children = { tab24h, tab7d, tab30d } };

        tab24h.PointerPressed += (s, e) => { _uptimeHours = 24; RefreshTimeAxis(); RefreshTimeTabs(tab24h, tab7d, tab30d); UptimeHoursChanged?.Invoke(24); };
        tab7d.PointerPressed += (s, e) => { _uptimeHours = 168; RefreshTimeAxis(); RefreshTimeTabs(tab7d, tab24h, tab30d); UptimeHoursChanged?.Invoke(168); };
        tab30d.PointerPressed += (s, e) => { _uptimeHours = 720; RefreshTimeAxis(); RefreshTimeTabs(tab30d, tab24h, tab7d); UptimeHoursChanged?.Invoke(720); };

        var uptimeHeader = new Grid();
        uptimeHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        uptimeHeader.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        uptimeHeader.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var uptimeTitleGroup = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { uptimeIcon, uptimeTitle } };
        Grid.SetColumn(uptimeTitleGroup, 0);
        Grid.SetColumn(tabRow, 2);
        uptimeHeader.Children.Add(uptimeTitleGroup);
        uptimeHeader.Children.Add(tabRow);

        _uptimeChart = new UptimeChartControl
        {
            Height = 120,
            Margin = new Thickness(0, 16, 0, 8)
        };

        _timeAxis = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        for (int i = 0; i < 5; i++)
            _timeAxis.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

        RefreshTimeAxis();

        var uptimeContent = new StackPanel { Children = { uptimeHeader, _uptimeChart, _timeAxis } };
        var uptimeCard = ThemeTokens.Card(uptimeContent, ThemeTokens.SurfaceContainerHigh, 24);

        // Bento: Alert Config + Telemetry
        var alertIcon = ThemeTokens.VectorIcon(ThemeTokens.SvgShield, 18, ThemeTokens.NavTextInactive);
        var alertTitle = ThemeTokens.Headline("Alert Configuration", 16);
        var alertTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 16), Children = { alertIcon, alertTitle } };

        _connLostToggle = new CheckBox { IsChecked = true };
        _highLatencyToggle = new CheckBox { IsChecked = false };

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

        _latencyStatText = new TextBlock { Text = "—", FontSize = 22, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Tertiary, FontFamily = ThemeTokens.DefaultFont };
        _packetLossText = new TextBlock { Text = "0.0", FontSize = 22, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.OnSurface, FontFamily = ThemeTokens.DefaultFont };
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
        var viewLogsLink = new TextBlock { Text = "View Logs →", FontSize = 12, Foreground = ThemeTokens.Tertiary, FontFamily = ThemeTokens.DefaultFont, HorizontalAlignment = HorizontalAlignment.Right, Cursor = ThemeTokens.HandCursor };
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

        // Registration form
        _nameInput = ThemeTokens.Input("Custom Name...");
        ThemeTokens.SetToolTip(_nameInput, "Assign a unique nickname to this device for easier identification.");

        _deviceNameInput = ThemeTokens.Input("Device Name...");
        _deviceNameInput.IsReadOnly = true;
        _deviceNameInput.IsHitTestVisible = false;
        _deviceNameInput.Opacity = 0.7;
        ThemeTokens.SetToolTip(_deviceNameInput, "The official hostname reported by the device (Read-only).");

        _deviceModelInput = ThemeTokens.Input("Device Model...");
        ThemeTokens.SetToolTip(_deviceModelInput, "The hardware model or version identified during scanning.");

        _exactModelInput = ThemeTokens.Input("Deep Intelligence Model...");
        _exactModelInput.IsReadOnly = true;
        _exactModelInput.IsHitTestVisible = false;
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

        _saveBtn = ThemeTokens.PrimaryButton(ThemeTokens.SvgSave, "Save & Register");
        ThemeTokens.SetToolTip(_saveBtn, "Commit these changes and permanently register this device in the database.");
        _saveBtn.Click += (s, e) => { if (_currentNode != null) SaveRequested?.Invoke(_currentNode); };

        _pingBtn = ThemeTokens.SecondaryButton(ThemeTokens.SvgRadar, "Ping Device");
        ThemeTokens.SetToolTip(_pingBtn, "Send a live ICMP ping to check device responsiveness.");
        _pingBtn.Click += (s, e) => { if (_currentNode != null) PingRequested?.Invoke(_currentNode); };

        _wakeBtn = ThemeTokens.SecondaryButton(ThemeTokens.SvgBolt, "Wake Device");
        ThemeTokens.SetToolTip(_wakeBtn, "Send a Wake-on-LAN Magic Packet to power on this device remotely.");
        _wakeBtn.Click += (s, e) => { if (_currentNode != null) WakeRequested?.Invoke(_currentNode); };

        _deleteBtn = ThemeTokens.DangerButton(ThemeTokens.SvgTrash, "Delete Device");
        ThemeTokens.SetToolTip(_deleteBtn, "Remove this device permanently from the database.");
        _deleteBtn.Click += (s, e) => { if (_currentNode != null) DeleteRequested?.Invoke(_currentNode); };

        var webBtn = ThemeTokens.TertiaryButton(ThemeTokens.SvgGlobe, "Open Web UI");
        ThemeTokens.SetToolTip(webBtn, "Open this device's IP in your default web browser.");
        webBtn.Click += (s, e) =>
        {
            if (_currentNode != null)
            {
                AppUtils.OpenSafeUrl($"http://{_currentNode.IpAddress}");
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
                webBtn,
                _pingBtn,
                _wakeBtn,
                _pingResult,
                _portResult,
                new Panel { Height = 6 },
                _deleteBtn
            }
        };

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

        var container = new Grid();
        container.Children.Add(_emptyDetail);
        container.Children.Add(_detailArea);
        container.Children.Add(_bulkArea);
        Child = container;
    }

    public void ShowEmpty()
    {
        _currentNode = null;
        _emptyDetail.IsVisible = true;
        _detailArea.IsVisible = false;
        _bulkArea.IsVisible = false;
    }

    public void ShowBulk(int selectedCount)
    {
        _currentNode = null;
        _emptyDetail.IsVisible = false;
        _detailArea.IsVisible = false;
        _bulkArea.IsVisible = true;
        _bulkTitle.Text = $"{selectedCount} Device(s) Selected";
    }

    public void ShowDevice(NetworkNode node, bool isOnline, List<UptimeSnapshot> history)
    {
        _currentNode = node;
        _emptyDetail.IsVisible = false;
        _bulkArea.IsVisible = false;
        _detailArea.IsVisible = true;

        _detailName.Text = node.DisplayName;
        _detailSubtitle.Text = InventoryGridControl.GetRichSubtitle(node);

        bool isManual = node.MacAddress.StartsWith(ManualDevicePrefix, StringComparison.OrdinalIgnoreCase);
        if (isManual)
        {
            _detailIpPill.IsVisible = false;
            _detailIpInput.IsVisible = true;
            _detailIpInput.Text = node.IpAddress;
            UpdatePill(_detailMacPill, "Manual Device");
        }
        else
        {
            _detailIpPill.IsVisible = true;
            _detailIpInput.IsVisible = false;
            UpdatePill(_detailIpPill, node.IpAddress);
            UpdatePill(_detailMacPill, node.MacAddress);
        }

        UpdateStatusDot(isOnline);

        if (node.ThreatLevel != ThreatLevel.Safe)
        {
            _threatBadge.IsVisible = true;
            _threatBadge.Background = node.ThreatLevel == ThreatLevel.Critical ? ThemeTokens.ErrorContainer : ThemeTokens.TertiaryContainerSubtle;
            if (_threatBadge.Child is TextBlock tb)
            {
                tb.Text = node.ThreatLevel.ToString().ToUpperInvariant();
                tb.Foreground = node.ThreatLevel == ThreatLevel.Critical ? ThemeTokens.Error : ThemeTokens.Tertiary;
            }
        }
        else
        {
            _threatBadge.IsVisible = false;
        }

        _deviceIconBox.Child = ThemeTokens.VectorIcon(InventoryGridControl.GetDeviceSvg(node), 28, isOnline ? ThemeTokens.Tertiary : ThemeTokens.OnSurfaceVariant);

        _nameInput.Text = node.CustomName ?? "";
        _deviceNameInput.Text = node.Hostname ?? "";
        _deviceModelInput.Text = node.DeviceModel ?? "";
        _exactModelInput.Text = node.ExactModel ?? "";
        _locationInput.Text = node.Location ?? "";
        _notesInput.Text = node.Notes ?? "";

        _connLostToggle.IsChecked = node.AlertOnConnectionLost;
        _highLatencyToggle.IsChecked = node.AlertOnHighLatency;

        _pingResult.Text = "";
        _portResult.Text = "";

        _latencyStatText.Text = node.PingLatencyMs > 0 ? node.PingLatencyMs.ToString() : "—";
        _packetLossText.Text = "0.0";
        _lastScanText.Text = node.LastSeen != default ? GetTimeAgo(node.LastSeen) : "—";

        _uptimeChart.SetData(history);
        RefreshTimeAxis();
    }

    public void UpdateStatusDot(bool isOnline)
    {
        _statusDot.Background = isOnline ? ThemeTokens.Tertiary : ThemeTokens.Error;
    }

    public void SetPingResult(string result, IBrush foreground)
    {
        _pingResult.Text = result;
        _pingResult.Foreground = foreground;
    }

    public void SetPortResult(string result)
    {
        _portResult.Text = result;
    }

    public (string Name, string DeviceModel, string Location, string Notes, string Ip, bool AlertConn, bool AlertLatency) GetFormData()
    {
        return (
            _nameInput.Text?.Trim() ?? "",
            _deviceModelInput.Text?.Trim() ?? "",
            _locationInput.Text?.Trim() ?? "",
            _notesInput.Text?.Trim() ?? "",
            _detailIpInput.Text?.Trim() ?? "",
            _connLostToggle.IsChecked == true,
            _highLatencyToggle.IsChecked == true
        );
    }

    public void ResetDeleteButton()
    {
        _deleteBtn.Content = ThemeTokens.ButtonContent(ThemeTokens.SvgTrash, "Delete Device");
    }

    public void SetConfirmDeleteButton()
    {
        _deleteBtn.Content = ThemeTokens.ButtonContent(ThemeTokens.SvgTrash, "Click Again to Confirm");
    }

    public void ResetBulkDeleteButton()
    {
        _bulkDeleteBtn.Content = "Delete";
    }

    public void SetConfirmBulkDeleteButton()
    {
        _bulkDeleteBtn.Content = "Click Again to Confirm";
    }

    public void UpdateUptimeChart(List<UptimeSnapshot> history)
    {
        _uptimeChart.SetData(history);
    }

    private void RefreshTimeAxis()
    {
        _timeAxis.Children.Clear();
        var now = DateTime.Now;
        var labels = new string[5];

        if (_uptimeHours == 24)
        {
            for (int i = 0; i < 5; i++)
            {
                var t = now.AddHours(-24 + (i * 6));
                labels[i] = t.ToString("hh:mm tt");
            }
        }
        else if (_uptimeHours == 168)
        {
            for (int i = 0; i < 5; i++)
            {
                var t = now.AddDays(-7 + (i * 1.75));
                labels[i] = t.ToString("ddd hh tt");
            }
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                var t = now.AddDays(-30 + (i * 7.5));
                labels[i] = t.ToString("MMM dd");
            }
        }

        for (int i = 0; i < 5; i++)
        {
            var align = i == 0 ? HorizontalAlignment.Left : (i == 4 ? HorizontalAlignment.Right : HorizontalAlignment.Center);
            var tb = ThemeTokens.Label(labels[i], 11, ThemeTokens.OnSurfaceVariant);
            tb.HorizontalAlignment = align;
            Grid.SetColumn(tb, i);
            _timeAxis.Children.Add(tb);
        }
    }

    private static void RefreshTimeTabs(Border active, Border inactive1, Border inactive2)
    {
        active.Background = ThemeTokens.SurfaceContainerHighest;
        active.BorderBrush = ThemeTokens.GhostBorder20;
        if (active.Child is TextBlock atb) atb.Foreground = ThemeTokens.OnSurface;

        inactive1.Background = Brushes.Transparent;
        inactive1.BorderBrush = Brushes.Transparent;
        if (inactive1.Child is TextBlock itb1) itb1.Foreground = ThemeTokens.OnSurfaceVariant;

        inactive2.Background = Brushes.Transparent;
        inactive2.BorderBrush = Brushes.Transparent;
        if (inactive2.Child is TextBlock itb2) itb2.Foreground = ThemeTokens.OnSurfaceVariant;
    }

    private static Border MakeInfoPill(string label, string value)
    {
        var valTb = new TextBlock { Text = value, FontSize = 12, Foreground = ThemeTokens.Primary, FontFamily = ThemeTokens.DefaultFont, FontWeight = FontWeight.SemiBold, Tag = "value" };
        ThemeTokens.AddCopyAction(valTb);

        return new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 6),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    ThemeTokens.Label(label, 11),
                    valTb
                }
            }
        };
    }

    private static void UpdatePill(Border pill, string value)
    {
        if (pill.Child is StackPanel sp)
        {
            foreach (var child in sp.Children)
            {
                if (child is TextBlock tb && Equals(tb.Tag, "value"))
                {
                    tb.Text = value;
                    break;
                }
            }
        }
    }

    private static Border MakeAlertToggleRow(string title, string description, CheckBox toggle)
    {
        var t = ThemeTokens.Body(title, 14);
        t.FontWeight = FontWeight.Medium;
        var d = ThemeTokens.Body(description, 12);
        d.Foreground = ThemeTokens.OnSurfaceVariant;
        var textCol = new StackPanel { Spacing = 2, Children = { t, d } };

        toggle.VerticalAlignment = VerticalAlignment.Center;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(textCol, 0);
        Grid.SetColumn(toggle, 1);
        grid.Children.Add(textCol);
        grid.Children.Add(toggle);

        return new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10),
            Child = grid
        };
    }

    private static Border MakeTimeTab(string text, bool active) => new()
    {
        Background = active ? ThemeTokens.SurfaceContainerHighest : Brushes.Transparent,
        CornerRadius = new CornerRadius(6),
        BorderBrush = active ? ThemeTokens.GhostBorder20 : Brushes.Transparent,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(12, 4),
        Margin = new Thickness(2, 0),
        Cursor = ThemeTokens.HandCursor,
        Child = new TextBlock { Text = text, FontSize = 12, Foreground = active ? ThemeTokens.OnSurface : ThemeTokens.OnSurfaceVariant, FontFamily = ThemeTokens.DefaultFont, FontWeight = FontWeight.Medium }
    };

    private static TextBlock MakeFieldLabel(string text) => new()
    {
        Text = text,
        FontSize = 12,
        Foreground = ThemeTokens.OnSurfaceVariant,
        FontFamily = ThemeTokens.DefaultFont,
        FontWeight = FontWeight.Medium,
        Margin = new Thickness(0, 4, 0, 2)
    };

    private static string GetTimeAgo(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return $"{(int)diff.TotalDays}d ago";
    }
}
