using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using NodeRadarPro.Core.Fingerprinting;
using NodeRadarPro.Core.Messaging;
using NodeRadarPro.Data;
using NodeRadarPro.UI.Controls.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NodeRadarPro.UI.Pages;

/// <summary>
/// Device Inventory orchestrator page, coordinating InventoryFilterBar, InventoryGridControl, and InventoryDetailsFlyout.
/// </summary>
public class InventoryPage : Border
{
    private readonly LocalDatabase _db;
    private readonly ConnectivityMonitor _monitor;
    private List<NetworkNode> _activeNodes;
    private const string ManualDevicePrefix = "MANUAL-";

    private readonly InventoryFilterBar _filterBar;
    private readonly InventoryGridControl _gridControl;
    private readonly InventoryDetailsFlyout _detailsFlyout;

    private bool _isDeleteConfirming;
    private DispatcherTimer? _deleteConfirmTimer;
    private bool _isBulkDeleteConfirming;
    private DispatcherTimer? _bulkDeleteConfirmTimer;

    public event Action<NetworkNode>? DeviceSaved;
    public event Action<NetworkNode>? DeviceDeleted;
    public event Action<IEnumerable<NetworkNode>>? DevicesDeleted;
    public event Action<NetworkNode>? DeviceStatusChanged;
    public event Action<NetworkNode>? DeviceSelected;
    public event Action<string>? ViewLogsRequested;

    internal TextBox SearchBox => _filterBar.SearchBox;
    internal TextBlock DeviceCountText => _filterBar.DeviceCountText;
    internal Button SelectModeButton => _filterBar.SelectModeButton;
    internal CheckBox SelectAllCheckBox => _filterBar.SelectAllCheckBox;
    internal StackPanel DeviceListContainer => _gridControl.DeviceListContainer;
    internal WrapPanel ChipPanel => _filterBar.ChipPanel;
    internal void SetActiveFilter(string filter) => _filterBar.SetActiveFilter(filter);

    public InventoryPage(LocalDatabase db, ConnectivityMonitor monitor, List<NetworkNode> activeNodes)
    {
        _db = db;
        _monitor = monitor;
        _activeNodes = activeNodes;
        Background = ThemeTokens.Surface;

        _filterBar = new InventoryFilterBar();
        _gridControl = new InventoryGridControl();
        _detailsFlyout = new InventoryDetailsFlyout();

        // Wire filter bar events
        _filterBar.FilterChanged += _ => RefreshDeviceList();
        _filterBar.SearchDebounced += _ => RefreshDeviceList();
        _filterBar.SelectModeToggled += ToggleSelectionMode;
        _filterBar.SelectAllToggled += ToggleSelectAll;
        _filterBar.AddDeviceClicked += OnAddDevice;

        // Wire grid control events
        _gridControl.DeviceCardClicked += ShowDevice;
        _gridControl.SelectionChanged += UpdateBulkView;

        // Wire details flyout events
        _detailsFlyout.SaveRequested += _ => SaveCurrentNode();
        _detailsFlyout.DeleteRequested += _ => OnDeleteClicked();
        _detailsFlyout.BulkDeleteRequested += OnBulkDeleteClicked;
        _detailsFlyout.ExitBulkRequested += ToggleSelectionMode;
        _detailsFlyout.PingRequested += OnPingClicked;
        _detailsFlyout.WakeRequested += OnWakeClicked;
        _detailsFlyout.PortScanRequested += OnPortScanClicked;
        _detailsFlyout.ViewLogsRequested += mac => ViewLogsRequested?.Invoke(mac);
        _detailsFlyout.UptimeHoursChanged += _ => RefreshUptimeChart();

        // Subscribe to node updates
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
                    RefreshDeviceList();
                }
                else
                {
                    UpdateExistingCards();
                }

                if (_detailsFlyout.CurrentNode != null)
                {
                    var updated = _activeNodes.FirstOrDefault(n => n.MacAddress == _detailsFlyout.CurrentNode.MacAddress);
                    if (updated != null)
                    {
                        _detailsFlyout.UpdateStatusDot(updated.IsOnline);
                    }
                }
            });
        });

        // Assemble Left Card
        var listDock = new DockPanel();
        DockPanel.SetDock(_filterBar, Dock.Top);
        listDock.Children.Add(_filterBar);
        listDock.Children.Add(_gridControl);

        var leftCard = ThemeTokens.Card(listDock, ThemeTokens.SurfaceContainerLow, 0);
        leftCard.ClipToBounds = true;

        // Assemble Root Grid
        var rootGrid = new Grid
        {
            Margin = new Thickness(24),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 340 },
                new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) }
            }
        };

        Grid.SetColumn(leftCard, 0);
        Grid.SetColumn(_detailsFlyout, 1);
        leftCard.Margin = new Thickness(0, 0, 12, 0);
        _detailsFlyout.Margin = new Thickness(12, 0, 0, 0);
        rootGrid.Children.Add(leftCard);
        rootGrid.Children.Add(_detailsFlyout);

        Child = rootGrid;
    }

    public void RefreshData()
    {
        RefreshDeviceList();
    }

    private void ToggleSelectionMode()
    {
        bool nextMode = !_gridControl.IsSelectionMode;
        _gridControl.SetSelectionMode(nextMode);
        _filterBar.SetSelectionModeUI(nextMode);

        if (nextMode)
        {
            _detailsFlyout.ShowBulk(0);
        }
        else
        {
            _detailsFlyout.ShowEmpty();
        }

        RefreshDeviceList();
    }

    private void ToggleSelectAll(bool selected)
    {
        var visibleNodes = GetFilteredNodes();
        _gridControl.SelectAll(visibleNodes, selected);
        UpdateBulkView();
        RefreshDeviceList();
    }

    private void UpdateBulkView()
    {
        int count = _gridControl.SelectedMacs.Count;
        _detailsFlyout.ShowBulk(count);
    }

    private void OnBulkDeleteClicked()
    {
        if (_gridControl.SelectedMacs.Count == 0) return;

        if (!_isBulkDeleteConfirming)
        {
            _isBulkDeleteConfirming = true;
            _detailsFlyout.SetConfirmBulkDeleteButton();

            _bulkDeleteConfirmTimer?.Stop();
            _bulkDeleteConfirmTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _bulkDeleteConfirmTimer.Tick += (s, e) =>
            {
                _isBulkDeleteConfirming = false;
                _detailsFlyout.ResetBulkDeleteButton();
                _bulkDeleteConfirmTimer?.Stop();
            };
            _bulkDeleteConfirmTimer.Start();
        }
        else
        {
            _bulkDeleteConfirmTimer?.Stop();
            _isBulkDeleteConfirming = false;
            _detailsFlyout.ResetBulkDeleteButton();

            var macsToDelete = _gridControl.SelectedMacs.ToList();
            var deletedNodes = new List<NetworkNode>();

            foreach (var mac in macsToDelete)
            {
                var node = _activeNodes.FirstOrDefault(n => n.MacAddress == mac);
                if (node != null)
                {
                    deletedNodes.Add(node);
                    _db.DeleteDevice(mac);
                    _monitor.RemoveDevice(mac);
                }
            }

            _gridControl.SelectedMacs.Clear();
            ToggleSelectionMode();
            RefreshDeviceList();

            if (deletedNodes.Count > 0)
            {
                DevicesDeleted?.Invoke(deletedNodes);
            }
        }
    }

    public void ShowDevice(NetworkNode node)
    {
        _gridControl.SetSelectedMac(node.MacAddress);
        var history = _db.GetUptimeHistory(node.MacAddress, _detailsFlyout.UptimeHours);
        _detailsFlyout.ShowDevice(node, node.IsOnline, history);
        RefreshDeviceList();
        DeviceSelected?.Invoke(node);
    }

    public void UpdateUptimeChart(List<UptimeSnapshot> history)
    {
        _detailsFlyout.UpdateUptimeChart(history);
    }

    private void RefreshUptimeChart()
    {
        if (_detailsFlyout.CurrentNode == null) return;
        var mac = _detailsFlyout.CurrentNode.MacAddress;
        var hours = _detailsFlyout.UptimeHours;

        Task.Run(() =>
        {
            var history = _db.GetUptimeHistory(mac, hours);
            Dispatcher.UIThread.Post(() =>
            {
                if (_detailsFlyout.CurrentNode?.MacAddress == mac)
                {
                    _detailsFlyout.UpdateUptimeChart(history);
                }
            });
        });
    }

    private List<NetworkNode> GetFilteredNodes()
    {
        var registered = _db.GetAllDevices().ToDictionary(d => d.MacAddress, StringComparer.OrdinalIgnoreCase);

        var allNodes = new Dictionary<string, NetworkNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var reg in registered.Values)
        {
            allNodes[reg.MacAddress] = reg;
        }

        foreach (var live in _activeNodes)
        {
            if (allNodes.TryGetValue(live.MacAddress, out var existing))
            {
                existing.IsOnline = live.IsOnline;
                existing.PingLatencyMs = live.PingLatencyMs;
                if (!string.IsNullOrEmpty(live.IpAddress)) existing.IpAddress = live.IpAddress;
                if (!string.IsNullOrEmpty(live.Hostname) && string.IsNullOrEmpty(existing.Hostname)) existing.Hostname = live.Hostname;
                if (!string.IsNullOrEmpty(live.Vendor) && string.IsNullOrEmpty(existing.Vendor)) existing.Vendor = live.Vendor;
                if (!string.IsNullOrEmpty(live.DeviceType) && existing.DeviceType == "Generic Device") existing.DeviceType = live.DeviceType;
                if (!string.IsNullOrEmpty(live.ExactModel)) existing.ExactModel = live.ExactModel;
                if (!string.IsNullOrEmpty(live.OsGuess)) existing.OsGuess = live.OsGuess;
            }
            else
            {
                allNodes[live.MacAddress] = live;
            }
        }

        var list = allNodes.Values.AsEnumerable();

        string filter = _filterBar.ActiveFilter;
        if (filter == "online") list = list.Where(n => n.IsOnline);
        else if (filter == "offline") list = list.Where(n => !n.IsOnline && n.IsRegistered);
        else if (filter == "critical") list = list.Where(n => n.ThreatLevel == ThreatLevel.Critical || n.ThreatLevel == ThreatLevel.Warning);
        else if (filter == "servers") list = list.Where(n => (n.DeviceType ?? "").Contains("server", StringComparison.OrdinalIgnoreCase) || (n.DeviceType ?? "").Contains("nas", StringComparison.OrdinalIgnoreCase));
        else if (filter == "iot") list = list.Where(n => (n.DeviceType ?? "").Contains("iot", StringComparison.OrdinalIgnoreCase) || (n.DeviceType ?? "").Contains("smart", StringComparison.OrdinalIgnoreCase) || (n.DeviceType ?? "").Contains("tv", StringComparison.OrdinalIgnoreCase));

        string search = _filterBar.SearchBox.Text?.Trim() ?? "";
        if (!string.IsNullOrEmpty(search))
        {
            list = list.Where(n =>
                (n.DisplayName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (n.IpAddress ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (n.MacAddress ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (n.Vendor ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (n.DeviceModel ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (n.ExactModel ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
            );
        }

        return list.OrderByDescending(n => n.IsOnline).ThenBy(n => n.DisplayName).ToList();
    }

    private void RefreshDeviceList()
    {
        var filtered = GetFilteredNodes();
        _filterBar.SetDeviceCount(filtered.Count);
        _gridControl.RenderCards(filtered);
    }

    private void UpdateExistingCards()
    {
        var filtered = GetFilteredNodes();
        _gridControl.UpdateExistingCards(filtered);
    }

    private void OnAddDevice()
    {
        var random = new Random();
        var bytes = new byte[3];
        random.NextBytes(bytes);
        string fakeMac = $"{ManualDevicePrefix}{bytes[0]:X2}:{bytes[1]:X2}:{bytes[2]:X2}";

        var manualNode = new NetworkNode
        {
            MacAddress = fakeMac,
            IpAddress = "192.168.1.50",
            Hostname = "Manual Device",
            CustomName = "New Static Device",
            DeviceType = "Generic Device",
            Vendor = "Static / Manual",
            IsRegistered = true,
            IsOnline = false,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        _db.UpdateRegistration(manualNode);
        _activeNodes.Add(manualNode);
        RefreshDeviceList();
        ShowDevice(manualNode);
    }

    private void SaveCurrentNode()
    {
        if (_detailsFlyout.CurrentNode == null) return;
        var node = _detailsFlyout.CurrentNode;

        var formData = _detailsFlyout.GetFormData();
        node.CustomName = formData.Name;
        node.DeviceModel = formData.DeviceModel;
        node.Location = formData.Location;
        node.Notes = formData.Notes;

        if (node.MacAddress.StartsWith(ManualDevicePrefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(formData.Ip))
        {
            if (IPAddress.TryParse(formData.Ip, out _))
            {
                node.IpAddress = formData.Ip;
            }
        }

        node.AlertOnConnectionLost = formData.AlertConn;
        node.AlertOnHighLatency = formData.AlertLatency;
        node.IsRegistered = true;

        _db.UpdateRegistration(node);

        RefreshDeviceList();
        ShowDevice(node);
        DeviceSaved?.Invoke(node);
    }

    private void OnDeleteClicked()
    {
        if (_detailsFlyout.CurrentNode == null) return;

        if (!_isDeleteConfirming)
        {
            _isDeleteConfirming = true;
            _detailsFlyout.SetConfirmDeleteButton();

            _deleteConfirmTimer?.Stop();
            _deleteConfirmTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _deleteConfirmTimer.Tick += (s, e) =>
            {
                _isDeleteConfirming = false;
                _detailsFlyout.ResetDeleteButton();
                _deleteConfirmTimer?.Stop();
            };
            _deleteConfirmTimer.Start();
        }
        else
        {
            _deleteConfirmTimer?.Stop();
            _isDeleteConfirming = false;
            _detailsFlyout.ResetDeleteButton();

            var node = _detailsFlyout.CurrentNode;
            _db.DeleteDevice(node.MacAddress);
            _monitor.RemoveDevice(node.MacAddress);
            _activeNodes.RemoveAll(n => n.MacAddress == node.MacAddress);

            _detailsFlyout.ShowEmpty();
            RefreshDeviceList();
            DeviceDeleted?.Invoke(node);
        }
    }

    private async void OnPingClicked(NetworkNode node)
    {
        _detailsFlyout.SetPingResult("Pinging...", ThemeTokens.OnSurfaceVariant);

        var (success, _, latency) = await SubnetScanner.QuickProbeAsync(node.IpAddress);
        bool statusChanged = node.IsOnline != success;
        node.IsOnline = success;
        node.PingLatencyMs = latency;
        node.LastSeen = DateTime.UtcNow;

        if (success)
        {
            _detailsFlyout.SetPingResult($"Online — {latency}ms", ThemeTokens.Tertiary);
            _detailsFlyout.UpdateStatusDot(true);

            if (node.Vendor == "Unknown Vendor" || string.IsNullOrEmpty(node.Vendor))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var res = await DeepFingerprintEngine.Instance.FingerprintNodeAsync(node, CancellationToken.None);
                        if (!string.IsNullOrEmpty(res.Vendor) && res.Vendor != "Unknown Vendor")
                        {
                            node.Vendor = res.Vendor;
                            _db.UpdateRegistration(node);
                            Dispatcher.UIThread.Post(() => RefreshDeviceList());
                        }
                    }
                    catch { }
                });
            }
        }
        else
        {
            _detailsFlyout.SetPingResult("Offline — No response", ThemeTokens.Error);
            _detailsFlyout.UpdateStatusDot(false);
        }

        UpdateExistingCards();
        if (statusChanged)
        {
            DeviceStatusChanged?.Invoke(node);
        }
    }

    private async void OnWakeClicked(NetworkNode node)
    {
        bool success = await WakeOnLan.WakeAsync(node.MacAddress);
        if (success)
        {
            _detailsFlyout.SetPingResult("Magic Packet transmitted via UDP 9.", ThemeTokens.Tertiary);
        }
        else
        {
            _detailsFlyout.SetPingResult("Wake failed: Invalid MAC format.", ThemeTokens.Error);
        }
    }

    private async void OnPortScanClicked(NetworkNode node)
    {
        _detailsFlyout.SetPortResult("Scanning standard ports...");
        var openPorts = await PortScanner.ScanCommonPortsAsync(node.IpAddress);

        if (openPorts.Count > 0)
        {
            _detailsFlyout.SetPortResult($"Open: {string.Join(", ", openPorts)}");

        }
        else
        {
            _detailsFlyout.SetPortResult("No common ports open.");
        }
    }
}
