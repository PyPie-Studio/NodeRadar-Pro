using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace NodeRadarPro.UI;

/// <summary>
/// Slide-in detail/registration panel for viewing and editing device properties.
/// All fields persist by MAC address — so DHCP IP changes don't lose saved info.
/// </summary>
public class DeviceDetailPanel : Border
{
    // ── Theme Colors ──
    private static readonly IBrush BgPanel = SolidColorBrush.Parse("#151525");
    private static readonly IBrush AccentPurple = SolidColorBrush.Parse("#8A2BE2");
    private static readonly IBrush TextWhite = Brushes.WhiteSmoke;
    private static readonly IBrush TextGrey = SolidColorBrush.Parse("#888899");
    private static readonly IBrush OnlineGreen = SolidColorBrush.Parse("#00FFcc");
    private static readonly IBrush OfflineRed = SolidColorBrush.Parse("#FF4444");
    private static readonly IBrush DangerRed = SolidColorBrush.Parse("#CC3333");
    private static readonly IBrush BorderColor = SolidColorBrush.Parse("#2A2A45");

    private NetworkNode? _currentNode;
    private readonly LocalDatabase _db;
    private readonly ConnectivityMonitor _monitor;

    // ── Controls ──
    private readonly TextBlock _headerText;
    private readonly TextBlock _statusBadge;
    private readonly TextBlock _deviceTypeText;
    private readonly TextBox _ipInput;
    private readonly TextBox _macInput;
    private readonly TextBlock _vendorText;
    private readonly TextBlock _hostnameText;
    private readonly TextBlock _uptimeText;
    private readonly TextBlock _latencyText;
    private readonly TextBox _nameInput;
    private readonly TextBox _deviceNameInput;
    private readonly TextBox _deviceModelInput;
    private readonly TextBox _locationInput;
    private readonly TextBox _notesInput;
    private readonly TextBlock _portResults;
    private readonly TextBlock _pingResult;
    private readonly Button _saveBtn;
    private readonly Button _portScanBtn;
    private readonly Button _pingBtn;
    private readonly Button _deleteBtn;
    private readonly Button _closeBtn;

    public event Action? CloseRequested;
    public event Action<NetworkNode>? DeviceSaved;
    public event Action<NetworkNode>? DeviceStatusChanged;
    public event Action<NetworkNode>? DeviceDeleted;

    public DeviceDetailPanel(LocalDatabase db, ConnectivityMonitor monitor)
    {
        _db = db;
        _monitor = monitor;

        MinWidth = 280;
        MaxWidth = 480;
        Width = 320;
        Background = BgPanel;
        BorderBrush = BorderColor;
        BorderThickness = new Thickness(0);
        IsVisible = false;

        // ── Build Controls ──
        _closeBtn = MakeButton("✕", SolidColorBrush.Parse("#333345"), 32, 32);
        _closeBtn.Click += (s, e) => { IsVisible = false; CloseRequested?.Invoke(); };
        _closeBtn.CornerRadius = new CornerRadius(16);
        _closeBtn.HorizontalAlignment = HorizontalAlignment.Right;

        _headerText = MakeLabel("Device Details", 18, FontWeight.Bold, TextWhite);
        _statusBadge = MakeLabel("● Online", 13, FontWeight.SemiBold, OnlineGreen);
        _deviceTypeText = MakeLabel("", 12, FontWeight.Normal, SolidColorBrush.Parse("#9B7FD4"));

        _ipInput = MakeInput("IP Address...");
        _macInput = MakeInput("MAC Address...");

        _vendorText = MakeLabel("Vendor: —", 12, FontWeight.Normal, TextGrey);
        _hostnameText = MakeLabel("Hostname: —", 12, FontWeight.Normal, TextGrey);
        _uptimeText = MakeLabel("Uptime: —", 12, FontWeight.Normal, TextGrey);
        _latencyText = MakeLabel("Latency: —", 12, FontWeight.Normal, TextGrey);

        _nameInput = MakeInput("Custom Name...");
        _deviceNameInput = MakeInput("Device Name (e.g. Main Router)...");
        _deviceModelInput = MakeInput("Model (e.g. MikroTik CCR2116)...");
        _locationInput = MakeInput("Location (e.g. Server Room 3F)...");
        _notesInput = MakeInput("Notes...");
        _notesInput.AcceptsReturn = true;
        _notesInput.Height = 70;
        _notesInput.TextWrapping = TextWrapping.Wrap;

        _saveBtn = MakeButton("💾  Save & Register", AccentPurple, double.NaN, 42);
        _saveBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _saveBtn.FontWeight = FontWeight.SemiBold;
        _saveBtn.Click += OnSaveClicked;

        _portScanBtn = MakeButton("🔍  Scan Ports", SolidColorBrush.Parse("#252540"), double.NaN, 38);
        _portScanBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _portScanBtn.Click += OnPortScanClicked;
        _portResults = MakeLabel("", 11, FontWeight.Normal, OnlineGreen);
        _portResults.TextWrapping = TextWrapping.Wrap;

        _pingBtn = MakeButton("📡  Ping Device", SolidColorBrush.Parse("#252540"), double.NaN, 38);
        _pingBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _pingBtn.Click += OnPingClicked;
        _pingResult = MakeLabel("", 12, FontWeight.Normal, TextGrey);

        _deleteBtn = MakeButton("🗑  Delete Device", DangerRed, double.NaN, 38);
        _deleteBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _deleteBtn.Click += OnDeleteClicked;

        // ── Assemble Layout ──
        var rootPanel = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(18, 14, 18, 18),
            Children =
            {
                _closeBtn,
                _headerText,
                _statusBadge,
                _deviceTypeText,
                MakeSeparator(),
                MakeSectionLabel("NETWORK INFO"),
                MakeFieldLabel("IP Address"),
                _ipInput,
                MakeFieldLabel("MAC Address"),
                _macInput,
                _vendorText, _hostnameText, _uptimeText, _latencyText,
                MakeSeparator(),
                MakeSectionLabel("REGISTRATION"),
                MakeFieldLabel("Custom Name"),
                _nameInput,
                MakeFieldLabel("Device Name"),
                _deviceNameInput,
                MakeFieldLabel("Device Model"),
                _deviceModelInput,
                MakeFieldLabel("Location"),
                _locationInput,
                MakeFieldLabel("Notes"),
                _notesInput,
                new Panel { Height = 4 },
                _saveBtn,
                MakeSeparator(),
                MakeSectionLabel("DIAGNOSTICS"),
                _pingBtn,
                _pingResult,
                _portScanBtn,
                _portResults,
                MakeSeparator(),
                _deleteBtn
            }
        };

        Child = new ScrollViewer
        {
            Content = rootPanel,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    public void ShowDevice(NetworkNode node)
    {
        _currentNode = node;
        IsVisible = true;

        _headerText.Text = node.DisplayName;
        _statusBadge.Text = node.IsOnline ? "● Online" : "● Offline";
        _statusBadge.Foreground = node.IsOnline ? OnlineGreen : OfflineRed;
        _deviceTypeText.Text = node.SubtitleText;

        _ipInput.Text = node.IpAddress;
        _macInput.Text = node.MacAddress;

        _vendorText.Text = $"Vendor:  {(string.IsNullOrEmpty(node.Vendor) || node.Vendor == "Unknown Vendor" ? "—" : node.Vendor)}";
        _hostnameText.Text = $"Hostname:  {(node.Hostname == "Unknown Device" ? "—" : node.Hostname)}";
        _uptimeText.Text = $"Uptime:  {node.UptimeDisplay}";
        _latencyText.Text = node.IsOnline && node.PingLatencyMs >= 0 ? $"Latency:  {node.PingLatencyMs}ms" : "Latency:  —";

        _nameInput.Text = node.CustomName;
        _deviceNameInput.Text = node.DeviceName;
        _deviceModelInput.Text = node.DeviceModel;
        _locationInput.Text = node.Location;
        _notesInput.Text = node.Notes;

        _portResults.Text = "";
        _pingResult.Text = "";
    }

    public void RefreshStatus(NetworkNode node)
    {
        if (_currentNode?.MacAddress != node.MacAddress) return;
        _currentNode = node;
        _statusBadge.Text = node.IsOnline ? "● Online" : "● Offline";
        _statusBadge.Foreground = node.IsOnline ? OnlineGreen : OfflineRed;
        _uptimeText.Text = $"Uptime:  {node.UptimeDisplay}";
        _latencyText.Text = node.IsOnline && node.PingLatencyMs >= 0 ? $"Latency:  {node.PingLatencyMs}ms" : "Latency:  —";
    }

    private void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;

        string newIp = (_ipInput.Text ?? "").Trim();
        string newMac = (_macInput.Text ?? "").Trim();

        if (!string.IsNullOrEmpty(newIp))
            _currentNode.IpAddress = newIp;
        if (!string.IsNullOrEmpty(newMac))
            _currentNode.MacAddress = newMac;

        _currentNode.CustomName = _nameInput.Text ?? "";
        _currentNode.DeviceName = _deviceNameInput.Text ?? "";
        _currentNode.DeviceModel = _deviceModelInput.Text ?? "";
        _currentNode.Location = _locationInput.Text ?? "";
        _currentNode.Notes = _notesInput.Text ?? "";
        _currentNode.IsRegistered = true;

        _db.UpdateRegistration(
            _currentNode.MacAddress,
            _currentNode.CustomName,
            _currentNode.Notes,
            _currentNode.Location,
            _currentNode.DeviceName,
            _currentNode.DeviceModel,
            _currentNode.IconPath,
            _currentNode.IpAddress
        );

        _headerText.Text = _currentNode.DisplayName;

        // Auto-resolve network info if IP was set on a manual entry
        if (newIp != "0.0.0.0" && !string.IsNullOrEmpty(newIp) && 
            (newMac.StartsWith("MANUAL") || newMac == "Unknown"))
        {
            TryAutoResolveAsync(newIp);
        }

        // Flash save confirmation
        _saveBtn.Content = "✅  Saved!";
        var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        timer.Tick += (s, ev) => { _saveBtn.Content = "💾  Save & Register"; timer.Stop(); };
        timer.Start();

        DeviceSaved?.Invoke(_currentNode);
    }

    private async void TryAutoResolveAsync(string ip)
    {
        try
        {
            var (isOnline, mac, latency) = await SubnetScanner.QuickProbeAsync(ip);

            if (isOnline)
            {
                _currentNode!.IsOnline = true;
                _currentNode.PingLatencyMs = latency;
                _currentNode.LastSeen = DateTime.UtcNow;
            }

            if (mac != "Unknown")
            {
                _currentNode!.MacAddress = mac;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => _macInput.Text = mac);

                _currentNode.Vendor = VendorLookup.GetVendor(mac);
                _currentNode.DeviceType = VendorLookup.GuessDeviceType(_currentNode.Vendor, _currentNode.Hostname);
            }

            // Try hostname
            if (_currentNode!.Hostname == "Unknown Device" || _currentNode.Hostname == "Manual Entry")
            {
                try
                {
                    var hostEntry = await System.Net.Dns.GetHostEntryAsync(ip);
                    if (hostEntry.HostName != ip)
                        _currentNode.Hostname = hostEntry.HostName;
                }
                catch { }
            }

            // Update UI on main thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _vendorText.Text = $"Vendor:  {(_currentNode.Vendor == "Unknown Vendor" ? "—" : _currentNode.Vendor)}";
                _hostnameText.Text = $"Hostname:  {(_currentNode.Hostname == "Unknown Device" ? "—" : _currentNode.Hostname)}";
                _deviceTypeText.Text = _currentNode.SubtitleText;
                _statusBadge.Text = _currentNode.IsOnline ? "● Online" : "● Offline";
                _statusBadge.Foreground = _currentNode.IsOnline ? OnlineGreen : OfflineRed;
                _latencyText.Text = _currentNode.IsOnline && _currentNode.PingLatencyMs >= 0
                    ? $"Latency:  {_currentNode.PingLatencyMs}ms" : "Latency:  —";
                _headerText.Text = _currentNode.DisplayName;
            });

            DeviceStatusChanged?.Invoke(_currentNode);
        }
        catch { }
    }

    private async void OnPortScanClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;

        string ip = (_ipInput.Text ?? "").Trim();
        if (string.IsNullOrEmpty(ip) || ip == "0.0.0.0")
        {
            _portResults.Text = "⚠ Enter a valid IP address first.";
            _portResults.Foreground = OfflineRed;
            return;
        }

        _portScanBtn.IsEnabled = false;
        _portScanBtn.Content = "⏳  Scanning...";
        _portResults.Text = "";
        _portResults.Foreground = OnlineGreen;

        try
        {
            var openPorts = await PortScanner.ScanCommonPortsAsync(ip);
            _portResults.Text = openPorts.Count > 0
                ? "Open: " + string.Join(", ", openPorts)
                : "No common ports open.";
        }
        catch
        {
            _portResults.Text = "Port scan failed.";
            _portResults.Foreground = OfflineRed;
        }
        finally
        {
            _portScanBtn.IsEnabled = true;
            _portScanBtn.Content = "🔍  Scan Ports";
        }
    }

    private async void OnPingClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;

        string ip = (_ipInput.Text ?? "").Trim();
        if (string.IsNullOrEmpty(ip) || ip == "0.0.0.0")
        {
            _pingResult.Text = "⚠ Enter a valid IP address first.";
            _pingResult.Foreground = OfflineRed;
            return;
        }

        _currentNode.IpAddress = ip;

        _pingBtn.IsEnabled = false;
        _pingBtn.Content = "📡  Pinging...";
        _pingResult.Text = "";

        try
        {
            // Use the comprehensive probe (ARP + ICMP + TCP)
            var (isOnline, mac, latency) = await SubnetScanner.QuickProbeAsync(ip);

            if (isOnline)
            {
                string method = latency >= 0 ? $"{latency}ms" : "ARP/TCP";
                _pingResult.Text = $"✅ Device reachable ({method})";
                _pingResult.Foreground = OnlineGreen;
                _currentNode.IsOnline = true;
                _currentNode.PingLatencyMs = latency;
                _currentNode.LastSeen = DateTime.UtcNow;

                // Update MAC if resolved
                if (mac != "Unknown" && (_currentNode.MacAddress.StartsWith("MANUAL") || _currentNode.MacAddress == "Unknown"))
                {
                    _currentNode.MacAddress = mac;
                    _macInput.Text = mac;
                    _currentNode.Vendor = VendorLookup.GetVendor(mac);
                    _currentNode.DeviceType = VendorLookup.GuessDeviceType(_currentNode.Vendor, _currentNode.Hostname);
                    _vendorText.Text = $"Vendor:  {_currentNode.Vendor}";
                    _deviceTypeText.Text = _currentNode.SubtitleText;
                }
            }
            else
            {
                _pingResult.Text = "❌ Device not reachable";
                _pingResult.Foreground = OfflineRed;
                _currentNode.IsOnline = false;
            }
        }
        catch (Exception ex)
        {
            _pingResult.Text = $"❌ Error: {ex.Message}";
            _pingResult.Foreground = OfflineRed;
            _currentNode.IsOnline = false;
        }
        finally
        {
            _pingBtn.IsEnabled = true;
            _pingBtn.Content = "📡  Ping Device";

            _statusBadge.Text = _currentNode.IsOnline ? "● Online" : "● Offline";
            _statusBadge.Foreground = _currentNode.IsOnline ? OnlineGreen : OfflineRed;
            _latencyText.Text = _currentNode.IsOnline && _currentNode.PingLatencyMs >= 0
                ? $"Latency:  {_currentNode.PingLatencyMs}ms" : "Latency:  —";

            DeviceStatusChanged?.Invoke(_currentNode);
        }
    }

    private async void OnDeleteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;

        // Show a simple confirmation dialog style embedded inside the panel
        _deleteBtn.Content = "Confirm Delete?";
        _deleteBtn.Background = SolidColorBrush.Parse("#8B0000"); // Darker red to signify danger

        // If they click again, do the actual delete
        _deleteBtn.Click -= OnDeleteClicked;
        _deleteBtn.Click += DoActualDelete;

        // Reset back to normal after 3 seconds if not clicked
        var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, ev) =>
        {
            if (_deleteBtn.Content?.ToString() == "Confirm Delete?")
            {
                _deleteBtn.Content = "🗑  Delete Device";
                _deleteBtn.Background = DangerRed;
                _deleteBtn.Click -= DoActualDelete;
                _deleteBtn.Click += OnDeleteClicked;
            }
            timer.Stop();
        };
        timer.Start();
    }

    private void DoActualDelete(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;

        _db.DeleteDevice(_currentNode.MacAddress);
        _monitor.RemoveDevice(_currentNode.MacAddress);

        var deletedNode = _currentNode;
        _currentNode = null;

        IsVisible = false;
        CloseRequested?.Invoke();
        DeviceDeleted?.Invoke(deletedNode);

        // Reset the button for the next device
        _deleteBtn.Content = "🗑  Delete Device";
        _deleteBtn.Background = DangerRed;
        _deleteBtn.Click -= DoActualDelete;
        _deleteBtn.Click += OnDeleteClicked;
    }

    // ── UI Factory Helpers ──

    private static TextBlock MakeLabel(string text, double size, FontWeight weight, IBrush foreground) => new()
    {
        Text = text, FontSize = size, FontWeight = weight, Foreground = foreground
    };

    private static TextBlock MakeSectionLabel(string text) => new()
    {
        Text = text, FontSize = 10, FontWeight = FontWeight.Bold,
        Foreground = SolidColorBrush.Parse("#555566"),
        Margin = new Thickness(0, 6, 0, 2), LetterSpacing = 1.5
    };

    private static TextBlock MakeFieldLabel(string text) => new()
    {
        Text = text, FontSize = 11,
        Foreground = SolidColorBrush.Parse("#999AAA"),
        Margin = new Thickness(0, 2, 0, 0)
    };

    private static TextBox MakeInput(string placeholder) => new()
    {
        PlaceholderText = placeholder,
        Background = SolidColorBrush.Parse("#1A1A30"),
        Foreground = Brushes.WhiteSmoke,
        BorderBrush = SolidColorBrush.Parse("#2A2A45"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(10, 8),
        FontSize = 13
    };

    private static Button MakeButton(string text, IBrush bg, double w, double h) => new()
    {
        Content = text,
        Background = bg,
        Foreground = Brushes.WhiteSmoke,
        Width = double.IsNaN(w) ? double.NaN : w,
        Height = h,
        CornerRadius = new CornerRadius(8),
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        FontSize = 13,
        Padding = new Thickness(12, 0)
    };

    private static Border MakeSeparator() => new()
    {
        Height = 1,
        Background = SolidColorBrush.Parse("#252540"),
        Margin = new Thickness(0, 8, 0, 8)
    };
}
