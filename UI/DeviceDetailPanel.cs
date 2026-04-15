using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using System;
using System.Collections.Generic;

namespace NodeRadarPro.UI;

/// <summary>
/// Slide-in detail/registration panel for viewing and editing device properties.
/// All fields persist by MAC address — so DHCP IP changes don't lose saved info.
/// </summary>
public class DeviceDetailPanel : Border
{
    // ── Theme Colors ──
    private static readonly IBrush BgPanel = SolidColorBrush.Parse("#151525");
    private static readonly IBrush BgInput = SolidColorBrush.Parse("#1E1E35");
    private static readonly IBrush AccentPurple = SolidColorBrush.Parse("#8A2BE2");
    private static readonly IBrush TextWhite = Brushes.WhiteSmoke;
    private static readonly IBrush TextGrey = SolidColorBrush.Parse("#888899");
    private static readonly IBrush OnlineGreen = SolidColorBrush.Parse("#00FFcc");
    private static readonly IBrush OfflineRed = SolidColorBrush.Parse("#FF4444");
    private static readonly IBrush BorderColor = SolidColorBrush.Parse("#2A2A45");

    private NetworkNode? _currentNode;
    private readonly LocalDatabase _db;
    private readonly ConnectivityMonitor _monitor;

    // ── Controls ──
    private readonly TextBlock _headerText;
    private readonly TextBlock _statusBadge;
    private readonly TextBlock _ipText;
    private readonly TextBlock _macText;
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
    private readonly Button _closeBtn;
    private readonly StackPanel _rootPanel;

    /// <summary>Fired when the close button is clicked.</summary>
    public event Action? CloseRequested;
    
    /// <summary>Fired when device data is saved (so the radar + list can refresh).</summary>
    public event Action<NetworkNode>? DeviceSaved;

    public DeviceDetailPanel(LocalDatabase db, ConnectivityMonitor monitor)
    {
        _db = db;
        _monitor = monitor;

        Width = 320;
        Background = BgPanel;
        BorderBrush = BorderColor;
        BorderThickness = new Thickness(1, 0, 0, 0);
        IsVisible = false;

        // ── Build Controls ──
        _closeBtn = MakeButton("✕", SolidColorBrush.Parse("#333345"), 32, 32);
        _closeBtn.Click += (s, e) => { IsVisible = false; CloseRequested?.Invoke(); };
        _closeBtn.CornerRadius = new CornerRadius(16);
        _closeBtn.HorizontalAlignment = HorizontalAlignment.Right;

        _headerText = MakeLabel("Device Details", 18, FontWeight.Bold, TextWhite);
        _statusBadge = MakeLabel("● Online", 13, FontWeight.SemiBold, OnlineGreen);

        _ipText = MakeLabel("IP: —", 12, FontWeight.Normal, TextGrey);
        _macText = MakeLabel("MAC: —", 12, FontWeight.Normal, TextGrey);
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

        _saveBtn = MakeButton("💾  Save & Register", AccentPurple, 200, 40);
        _saveBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _saveBtn.Click += OnSaveClicked;

        _portScanBtn = MakeButton("🔍  Scan Ports", SolidColorBrush.Parse("#333345"), 200, 36);
        _portScanBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _portScanBtn.Click += OnPortScanClicked;
        _portResults = MakeLabel("", 11, FontWeight.Normal, SolidColorBrush.Parse("#00FFcc"));
        _portResults.TextWrapping = TextWrapping.Wrap;

        _pingBtn = MakeButton("📡  Ping Device", SolidColorBrush.Parse("#333345"), 200, 36);
        _pingBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _pingBtn.Click += OnPingClicked;
        _pingResult = MakeLabel("", 12, FontWeight.Normal, TextGrey);

        // ── Assemble Layout ──
        _rootPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(16, 12, 16, 16),
            Children =
            {
                _closeBtn,
                _headerText,
                _statusBadge,
                MakeSeparator(),
                MakeSectionLabel("NETWORK INFO"),
                _ipText, _macText, _vendorText, _hostnameText, _uptimeText, _latencyText,
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
                new Panel { Height = 6 },
                _saveBtn,
                MakeSeparator(),
                MakeSectionLabel("DIAGNOSTICS"),
                _pingBtn,
                _pingResult,
                _portScanBtn,
                _portResults
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = _rootPanel,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        Child = scrollViewer;
    }

    /// <summary>
    /// Opens the detail panel for a specific device.
    /// </summary>
    public void ShowDevice(NetworkNode node)
    {
        _currentNode = node;
        IsVisible = true;

        _headerText.Text = node.DisplayName;
        _statusBadge.Text = node.IsOnline ? "● Online" : "● Offline";
        _statusBadge.Foreground = node.IsOnline ? OnlineGreen : OfflineRed;

        _ipText.Text = $"IP:  {node.IpAddress}";
        _macText.Text = $"MAC:  {node.MacAddress}";
        _vendorText.Text = $"Vendor:  {(string.IsNullOrEmpty(node.Vendor) ? "Unknown" : node.Vendor)}";
        _hostnameText.Text = $"Hostname:  {node.Hostname}";
        _uptimeText.Text = $"Uptime:  {node.UptimeDisplay}";
        _latencyText.Text = node.IsOnline ? $"Latency:  {node.PingLatencyMs}ms" : "Latency:  —";

        _nameInput.Text = node.CustomName;
        _deviceNameInput.Text = node.DeviceName;
        _deviceModelInput.Text = node.DeviceModel;
        _locationInput.Text = node.Location;
        _notesInput.Text = node.Notes;

        _portResults.Text = "";
        _pingResult.Text = "";
    }

    /// <summary>
    /// Refreshes the displayed status without resetting input fields.
    /// </summary>
    public void RefreshStatus(NetworkNode node)
    {
        if (_currentNode?.MacAddress != node.MacAddress) return;
        _currentNode = node;
        _statusBadge.Text = node.IsOnline ? "● Online" : "● Offline";
        _statusBadge.Foreground = node.IsOnline ? OnlineGreen : OfflineRed;
        _uptimeText.Text = $"Uptime:  {node.UptimeDisplay}";
        _latencyText.Text = node.IsOnline ? $"Latency:  {node.PingLatencyMs}ms" : "Latency:  —";
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

        _db.UpdateRegistration(
            _currentNode.MacAddress,
            _currentNode.CustomName,
            _currentNode.Notes,
            _currentNode.Location,
            _currentNode.DeviceName,
            _currentNode.DeviceModel,
            _currentNode.IconPath
        );

        _headerText.Text = _currentNode.DisplayName;
        
        // Flash save confirmation
        _saveBtn.Content = "✅  Saved!";
        var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        timer.Tick += (s, ev) => { _saveBtn.Content = "💾  Save & Register"; timer.Stop(); };
        timer.Start();

        DeviceSaved?.Invoke(_currentNode);
    }

    private async void OnPortScanClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentNode == null) return;
        _portScanBtn.IsEnabled = false;
        _portScanBtn.Content = "⏳  Scanning...";
        _portResults.Text = "";

        try
        {
            var openPorts = await PortScanner.ScanCommonPortsAsync(_currentNode.IpAddress);
            _portResults.Text = openPorts.Count > 0
                ? "Open: " + string.Join(", ", openPorts)
                : "No common ports open.";
        }
        catch
        {
            _portResults.Text = "Port scan failed.";
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
        _pingBtn.IsEnabled = false;
        _pingBtn.Content = "📡  Pinging...";
        _pingResult.Text = "";

        try
        {
            using var pinger = new System.Net.NetworkInformation.Ping();
            var reply = await pinger.SendPingAsync(_currentNode.IpAddress, 2000);

            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
            {
                _pingResult.Text = $"✅ Reply: {reply.RoundtripTime}ms";
                _pingResult.Foreground = OnlineGreen;
                _currentNode.IsOnline = true;
                _currentNode.PingLatencyMs = reply.RoundtripTime;
                _currentNode.LastSeen = DateTime.UtcNow;
            }
            else
            {
                _pingResult.Text = $"❌ No reply ({reply.Status})";
                _pingResult.Foreground = OfflineRed;
            }
        }
        catch (Exception ex)
        {
            _pingResult.Text = $"❌ Error: {ex.Message}";
            _pingResult.Foreground = OfflineRed;
        }
        finally
        {
            _pingBtn.IsEnabled = true;
            _pingBtn.Content = "📡  Ping Device";

            // Update status badge
            _statusBadge.Text = _currentNode.IsOnline ? "● Online" : "● Offline";
            _statusBadge.Foreground = _currentNode.IsOnline ? OnlineGreen : OfflineRed;
            _latencyText.Text = _currentNode.IsOnline ? $"Latency:  {_currentNode.PingLatencyMs}ms" : "Latency:  —";
        }
    }

    // ── UI Factory Helpers ──

    private static TextBlock MakeLabel(string text, double size, FontWeight weight, IBrush foreground)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = foreground
        };
    }

    private static TextBlock MakeSectionLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Foreground = SolidColorBrush.Parse("#555566"),
            Margin = new Thickness(0, 4, 0, 2),
            LetterSpacing = 1.5
        };
    }

    private static TextBlock MakeFieldLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 11,
            Foreground = SolidColorBrush.Parse("#999AAA"),
            Margin = new Thickness(0, 2, 0, 0)
        };
    }

    private static TextBox MakeInput(string placeholder)
    {
        return new TextBox
        {
            PlaceholderText = placeholder,
            Background = SolidColorBrush.Parse("#1E1E35"),
            Foreground = Brushes.WhiteSmoke,
            BorderBrush = SolidColorBrush.Parse("#2A2A45"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8),
            FontSize = 13
        };
    }

    private static Button MakeButton(string text, IBrush bg, double w, double h)
    {
        return new Button
        {
            Content = text,
            Background = bg,
            Foreground = Brushes.WhiteSmoke,
            Width = w,
            Height = h,
            CornerRadius = new CornerRadius(8),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 13
        };
    }

    private static Border MakeSeparator()
    {
        return new Border
        {
            Height = 1,
            Background = SolidColorBrush.Parse("#252540"),
            Margin = new Thickness(0, 6, 0, 6)
        };
    }
}
