using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NodeRadarPro.UI;

/// <summary>
/// Settings & Alerts page — matches Settings_Alerts_Form screenshot exactly.
/// Left: General Settings + Scan Parameters. Right: Notification Center.
/// </summary>
public class SettingsPage : Border
{
    private readonly LocalDatabase _db;
    private AppSettings _settings;

    // General
    private readonly ComboBox _interfaceSelector;
    private readonly CheckBox _promiscuousToggle;

    // Scan Parameters
    private readonly Slider _sweepFrequency;
    private readonly TextBlock _sweepFreqValue;
    private readonly Slider _responseTimeout;
    private readonly TextBlock _responseTimeoutValue;
    private readonly CheckBox _synScanToggle;
    private readonly CheckBox _dnsResolveToggle;

    // Old settings kept for persistence
    private readonly CheckBox _fadeOutToggle;
    private readonly NumericUpDown _fadeOutSeconds;
    private readonly NumericUpDown _monitorInterval;

    // Notification
    private readonly CheckBox _toastToggle;
    private readonly CheckBox _soundToggle;
    private readonly CheckBox _emailToggle;

    // Thresholds
    private readonly Slider _latencyThresholdSlider;
    private readonly TextBlock _latencyThresholdValue;
    private readonly Slider _packetLossThresholdSlider;
    private readonly TextBlock _packetLossThresholdValue;

    // SMTP
    private readonly TextBox _smtpHost;
    private readonly TextBox _smtpPort;
    private readonly TextBox _smtpUser;
    private readonly TextBox _smtpPass;
    private readonly StackPanel _smtpSettingsPanel;

    public event Action<AppSettings>? SettingsSaved;

    public SettingsPage(LocalDatabase db)
    {
        _db = db;
        _settings = db.LoadSettings();
        Background = ThemeTokens.Surface;

        // ═══════════════════════
        // PAGE HEADER
        // ═══════════════════════
        var topLabel = new TextBlock
        {
            Text = "SETTINGS CONFIGURATION",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeTokens.NavAccentBorder,
            FontFamily = new FontFamily("Inter"),
            LetterSpacing = 2.5,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var title = ThemeTokens.Headline("System Configurations", 36);
        title.Margin = new Thickness(0, 0, 0, 6);
        var subtitle = ThemeTokens.Body("Manage network interfaces, scan parameters, and alert routing protocols.", 14);

        var headerSection = new StackPanel { Margin = new Thickness(0, 0, 0, 28), Children = { topLabel, title, subtitle } };

        // ═══════════════════════
        // LEFT: General Settings
        // ═══════════════════════
        var generalIcon = new TextBlock { Text = "<>", FontSize = 16, Foreground = ThemeTokens.Primary, VerticalAlignment = VerticalAlignment.Center };
        var generalTitle = ThemeTokens.Headline("General Settings", 22);
        var generalHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 24), Children = { generalIcon, generalTitle } };

        // Network Interface dropdown
        var interfaceLabel = new TextBlock { Text = "Primary Network Interface", FontSize = 14, FontWeight = FontWeight.Medium, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 0, 0, 4) };
        var interfaceDesc = new TextBlock { Text = "Select the interface for primary radar sweeps.", FontSize = 12, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 0, 0, 10) };

        _interfaceSelector = new ComboBox
        {
            FontSize = 13,
            Foreground = ThemeTokens.OnSurface,
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(14, 10),
            FontFamily = new FontFamily("Inter")
        };
        ThemeTokens.SetToolTip(_interfaceSelector, "Choose the physical or virtual network adapter to use for scanning and monitoring.");
        // Populate with available interfaces
        PopulateNetworkInterfaces();

        // Promiscuous Mode toggle
        _promiscuousToggle = new CheckBox { IsChecked = false };
        ThemeTokens.SetToolTip(_promiscuousToggle, "Attempt to listen to all packets on the network segment, not just those addressed to this host.");
        var promiscRow = MakeToggleCard("Promiscuous Mode", "Capture all traffic on the segment.", _promiscuousToggle);

        // Hidden persistence controls
        _monitorInterval = new NumericUpDown { Value = _settings.MonitorIntervalSeconds, Minimum = 10, Maximum = 600, IsVisible = false };
        _fadeOutToggle = new CheckBox { IsChecked = _settings.EnableOfflineFadeOut, IsVisible = false };
        _fadeOutSeconds = new NumericUpDown { Value = _settings.FadeOutSeconds, IsVisible = false };

        var generalContent = new StackPanel { Children = { generalHeader, interfaceLabel, interfaceDesc, _interfaceSelector, new Panel { Height = 20 }, promiscRow, _monitorInterval, _fadeOutToggle, _fadeOutSeconds } };
        var generalCard = ThemeTokens.Card(generalContent, ThemeTokens.SurfaceContainerLow, 32);

        // ═══════════════════════
        // LEFT: Scan Parameters
        // ═══════════════════════
        var scanIcon = new TextBlock { Text = "◎", FontSize = 16, Foreground = ThemeTokens.Primary, VerticalAlignment = VerticalAlignment.Center };
        var scanTitle = ThemeTokens.Headline("Scan Parameters", 22);
        var scanHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 20), Children = { scanIcon, scanTitle } };

        // Sweep Frequency slider
        _sweepFrequency = new Slider { Minimum = 5, Maximum = 120, Value = 30 };
        ThemeTokens.SetToolTip(_sweepFrequency, "Set how often the background monitor probes each registered device.");
        _sweepFreqValue = new TextBlock { Text = "30s", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Tertiary, FontFamily = new FontFamily("Inter") };
        _sweepFrequency.ValueChanged += (s, e) => _sweepFreqValue.Text = $"{(int)_sweepFrequency.Value}s";

        // Response Timeout slider
        _responseTimeout = new Slider { Minimum = 100, Maximum = 5000, Value = 1500 };
        ThemeTokens.SetToolTip(_responseTimeout, "Maximum time to wait for a device to respond before marking it as potentially offline.");
        _responseTimeoutValue = new TextBlock { Text = "1500ms", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Primary, FontFamily = new FontFamily("Inter") };
        _responseTimeout.ValueChanged += (s, e) => _responseTimeoutValue.Text = $"{(int)_responseTimeout.Value}ms";

        var sliderGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(1, GridUnitType.Star)) }
        };

        var sweepCard = MakeSliderCard("Sweep Frequency", "Interval between active node discoveries.", _sweepFrequency, _sweepFreqValue);
        var timeoutCard = MakeSliderCard("Response Timeout", "Maximum wait time for ICMP/TCP replies.", _responseTimeout, _responseTimeoutValue);

        Grid.SetColumn(sweepCard, 0);
        Grid.SetColumn(timeoutCard, 1);
        sweepCard.Margin = new Thickness(0, 0, 6, 0);
        timeoutCard.Margin = new Thickness(6, 0, 0, 0);
        sliderGrid.Children.Add(sweepCard);
        sliderGrid.Children.Add(timeoutCard);

        // SYN Scan + DNS toggles
        _synScanToggle = new CheckBox { IsChecked = false };
        ThemeTokens.SetToolTip(_synScanToggle, "Enable aggressive TCP SYN scanning to identify open ports behind stealthy firewalls.");
        _dnsResolveToggle = new CheckBox { IsChecked = true };
        ThemeTokens.SetToolTip(_dnsResolveToggle, "Perform reverse DNS lookups to resolve IP addresses to human-readable hostnames.");
        var synRow = MakeToggleCard("Aggressive Port Scanning (SYN)", "", _synScanToggle, "⊕");
        var dnsRow = MakeToggleCard("Resolve Hostnames (DNS)", "", _dnsResolveToggle, "◉");

        var scanContent = new StackPanel { Spacing = 12, Children = { scanHeader, sliderGrid, synRow, dnsRow } };
        var scanCard = ThemeTokens.Card(scanContent, ThemeTokens.SurfaceContainerLow, 32);
        scanCard.Margin = new Thickness(0, 20, 0, 0);

        var leftCol = new StackPanel { Children = { generalCard, scanCard } };

        // ═══════════════════════
        // RIGHT: Notification Center
        // ═══════════════════════
        var notifIcon = new TextBlock { Text = "🔔", FontSize = 18, VerticalAlignment = VerticalAlignment.Center };
        var notifTitle = ThemeTokens.Headline("Notification Center", 22);
        var notifHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 20), Children = { notifIcon, notifTitle } };

        var separator1 = new Border { Height = 1, Background = ThemeTokens.GhostBorder, Margin = new Thickness(0, 0, 0, 12) };

        var routingLabel = ThemeTokens.SectionLabel("ALERT ROUTING");

        _toastToggle = new CheckBox { IsChecked = true };
        ThemeTokens.SetToolTip(_toastToggle, "Show visual desktop notifications when devices go offline or reconnect.");
        _soundToggle = new CheckBox { IsChecked = true };
        ThemeTokens.SetToolTip(_soundToggle, "Play an audible chime for important network events.");
        _emailToggle = new CheckBox { IsChecked = false };
        ThemeTokens.SetToolTip(_emailToggle, "Dispatch automated email alerts for critical outages and security intrusions.");

        var toastRow = MakeCheckboxCard("UI Popups (Toasts)", "Display transient alerts in dashboard.", _toastToggle);
        var soundRow = MakeCheckboxCard("Audible Alarms", "Play sounds for critical events.", _soundToggle);
        var emailRow = MakeCheckboxCard("Email Dispatch", "Send summaries to admin@node.local", _emailToggle);

        // SMTP Fields
        _smtpHost = ThemeTokens.Input("SMTP Host (e.g., smtp.gmail.com)");
        ThemeTokens.SetToolTip(_smtpHost, "The hostname of your outgoing mail server.");
        _smtpPort = ThemeTokens.Input("Port (e.g., 587)");
        ThemeTokens.SetToolTip(_smtpPort, "The TCP port for SMTP (Common: 587 for TLS, 465 for SSL).");
        _smtpUser = ThemeTokens.Input("Username");
        ThemeTokens.SetToolTip(_smtpUser, "Your SMTP authentication username (usually your email address).");
        _smtpPass = ThemeTokens.Input("Password");
        ThemeTokens.SetToolTip(_smtpPass, "Your SMTP authentication password. For Gmail, use an 'App Password'.");
        _smtpPass.PasswordChar = '●';

        _smtpSettingsPanel = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(10, 5, 0, 10),
            IsVisible = _emailToggle.IsChecked == true,
            Children =
            {
                ThemeTokens.Label("SMTP Configuration"),
                _smtpHost,
                _smtpPort,
                _smtpUser,
                _smtpPass
            }
        };

        _emailToggle.IsCheckedChanged += (s, e) => _smtpSettingsPanel.IsVisible = _emailToggle.IsChecked == true;

        var updatesLabel = ThemeTokens.SectionLabel("SOFTWARE UPDATES");
        var updateBtn = ThemeTokens.SecondaryButton("Check for Updates");
        ThemeTokens.SetToolTip(updateBtn, "Visit the PyPie Studio GitHub repository to check for the latest NodeRadar Pro stable release.");
        updateBtn.Margin = new Thickness(0, 4, 0, 12);
        updateBtn.Click += OnCheckForUpdatesClicked;

        var separator2 = new Border { Height = 1, Background = ThemeTokens.GhostBorder, Margin = new Thickness(0, 12, 0, 8) };

        var thresholdLabel = ThemeTokens.SectionLabel("CRITICAL THRESHOLDS");

        _latencyThresholdSlider = new Slider { Minimum = 50, Maximum = 1000, Value = _settings.LatencyThresholdMs };
        _latencyThresholdValue = new TextBlock { Text = $"{_settings.LatencyThresholdMs}ms", FontSize = 13, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Error, VerticalAlignment = VerticalAlignment.Center };
        _latencyThresholdSlider.ValueChanged += (s, e) => _latencyThresholdValue.Text = $"{(int)_latencyThresholdSlider.Value}ms";
        var latencyCard = MakeThresholdSliderCard("Latency Warning", "> Threshold", _latencyThresholdSlider, _latencyThresholdValue);

        _packetLossThresholdSlider = new Slider { Minimum = 1, Maximum = 50, Value = _settings.PacketLossThresholdPct };
        _packetLossThresholdValue = new TextBlock { Text = $"{_settings.PacketLossThresholdPct:F1}%", FontSize = 13, FontWeight = FontWeight.Bold, Foreground = ThemeTokens.Error, VerticalAlignment = VerticalAlignment.Center };
        _packetLossThresholdSlider.ValueChanged += (s, e) => _packetLossThresholdValue.Text = $"{_packetLossThresholdSlider.Value:F1}%";
        var packetCard = MakeThresholdSliderCard("Packet Loss Alert", "> Threshold", _packetLossThresholdSlider, _packetLossThresholdValue);

        // Buttons
        var resetBtn = ThemeTokens.SecondaryButton("Reset\nDefaults");
        ThemeTokens.SetToolTip(resetBtn, "Wipe all custom configurations and restore system factory settings.");
        resetBtn.Height = 50;
        resetBtn.FontSize = 12;
        resetBtn.Click += OnResetClicked;

        var saveBtn = ThemeTokens.PrimaryButton("Save\nConfiguration");
        ThemeTokens.SetToolTip(saveBtn, "Apply and persist all changed settings to the local database.");
        saveBtn.Height = 50;
        saveBtn.FontSize = 12;
        saveBtn.Click += OnSaveClicked;

        var btnGrid = new Grid { Margin = new Thickness(0, 20, 0, 0) };
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        Grid.SetColumn(resetBtn, 0);
        Grid.SetColumn(saveBtn, 1);
        resetBtn.Margin = new Thickness(0, 0, 4, 0);
        saveBtn.Margin = new Thickness(4, 0, 0, 0);
        btnGrid.Children.Add(resetBtn);
        btnGrid.Children.Add(saveBtn);

        var notifContent = new StackPanel
        {
            Children = { notifHeader, separator1, routingLabel, toastRow, soundRow, emailRow, _smtpSettingsPanel, updatesLabel, updateBtn, separator2, thresholdLabel, latencyCard, packetCard, btnGrid }
        };
        var notifCard = ThemeTokens.GlassCard(notifContent, 28);

        // ═══════════════════════
        // ROOT
        // ═══════════════════════
        var rootGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(2, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star))
            }
        };

        Grid.SetColumn(leftCol, 0);
        Grid.SetColumn(notifCard, 1);
        leftCol.Margin = new Thickness(0, 0, 14, 0);
        notifCard.Margin = new Thickness(14, 0, 0, 0);
        rootGrid.Children.Add(leftCol);
        rootGrid.Children.Add(notifCard);

        var rootContainer = new StackPanel { Children = { headerSection, rootGrid } };

        Child = new ScrollViewer
        {
            Content = rootContainer,
            Margin = new Thickness(32, 28),
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    public void Refresh()
    {
        _settings = _db.LoadSettings();
        _monitorInterval.Value = _settings.MonitorIntervalSeconds;
        _fadeOutToggle.IsChecked = _settings.EnableOfflineFadeOut;
        _fadeOutSeconds.Value = _settings.FadeOutSeconds;
        _sweepFrequency.Value = _settings.SweepFrequencySeconds;
        _sweepFreqValue.Text = $"{_settings.SweepFrequencySeconds}s";
        _responseTimeout.Value = _settings.ResponseTimeoutMs;
        _responseTimeoutValue.Text = $"{_settings.ResponseTimeoutMs}ms";
        _latencyThresholdSlider.Value = _settings.LatencyThresholdMs;
        _latencyThresholdValue.Text = $"{_settings.LatencyThresholdMs}ms";
        _packetLossThresholdSlider.Value = _settings.PacketLossThresholdPct;
        _packetLossThresholdValue.Text = $"{_settings.PacketLossThresholdPct:F1}%";
        _synScanToggle.IsChecked = _settings.EnableSynScan;
        _dnsResolveToggle.IsChecked = _settings.EnableDnsResolve;
        _promiscuousToggle.IsChecked = _settings.EnablePromiscuous;
        _toastToggle.IsChecked = _settings.EnableToastAlerts;
        _soundToggle.IsChecked = _settings.EnableSoundAlerts;
        _emailToggle.IsChecked = _settings.EnableEmailAlerts;

        _smtpHost.Text = _settings.SmtpHost;
        _smtpPort.Text = _settings.SmtpPort.ToString();
        _smtpUser.Text = _settings.SmtpUser;
        _smtpPass.Text = _settings.SmtpPassword;
        _smtpSettingsPanel.IsVisible = _settings.EnableEmailAlerts;

        // Select matching interface
        if (!string.IsNullOrEmpty(_settings.SelectedInterfaceName) && _interfaceSelector.ItemsSource is IList<string> items)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i].Contains(_settings.SelectedInterfaceName, StringComparison.OrdinalIgnoreCase))
                { _interfaceSelector.SelectedIndex = i; break; }
        }
    }

    private void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _settings.MonitorIntervalSeconds = (int)(_monitorInterval.Value ?? 60);
        _settings.EnableOfflineFadeOut = _fadeOutToggle.IsChecked == true;
        _settings.FadeOutSeconds = (int)(_fadeOutSeconds.Value ?? 300);
        _settings.SweepFrequencySeconds = (int)_sweepFrequency.Value;
        _settings.ResponseTimeoutMs = (int)_responseTimeout.Value;
        _settings.EnableSynScan = _synScanToggle.IsChecked == true;
        _settings.EnableDnsResolve = _dnsResolveToggle.IsChecked == true;
        _settings.EnablePromiscuous = _promiscuousToggle.IsChecked == true;
        _settings.EnableToastAlerts = _toastToggle.IsChecked == true;
        _settings.EnableSoundAlerts = _soundToggle.IsChecked == true;
        _settings.EnableEmailAlerts = _emailToggle.IsChecked == true;

        _settings.SmtpHost = _smtpHost.Text ?? "";
        if (int.TryParse(_smtpPort.Text, out var port)) _settings.SmtpPort = port;
        _settings.SmtpUser = _smtpUser.Text ?? "";
        _settings.SmtpPassword = _smtpPass.Text ?? "";

        // Save selected interface name
        if (_interfaceSelector.SelectedItem is string iface)
        {
            var parts = iface.Split(" - ");
            _settings.SelectedInterfaceName = parts.Length > 0 ? parts[0].Trim() : "";
        }

        _db.SaveSettings(_settings);
        SettingsSaved?.Invoke(_settings);
    }

    private void OnResetClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _settings = new AppSettings();
        _db.SaveSettings(_settings);
        Refresh();
        SettingsSaved?.Invoke(_settings);
    }

    private async void OnCheckForUpdatesClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var originalText = btn.Content;
            try
            {
                btn.IsEnabled = false;
                btn.Content = "Opening Browser...";

                _db.Log(LogLevel.Info, "Update", "Directing user to GitHub Releases...");
                
                // Open GitHub Releases page in default browser
                string url = "https://github.com/PyPie-Studio/NodeRadar-Pro/releases";
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    btn.Content = "Check GitHub";
                }
                catch (Exception ex)
                {
                    _db.Log(LogLevel.Error, "Update", $"Failed to open browser: {ex.Message}");
                    btn.Content = "Error";
                }

                await Task.Delay(2000);
                btn.Content = originalText;
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }
    }

    private void PopulateNetworkInterfaces()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            var items = new List<string>();
            foreach (var ni in interfaces)
            {
                string status = ni.OperationalStatus == OperationalStatus.Up ? "Active" : "Inactive";
                items.Add($"{ni.Name} - {ni.Description} ({status})");
            }
            if (items.Count == 0) items.Add("No interfaces detected");
            _interfaceSelector.ItemsSource = items;
            _interfaceSelector.SelectedIndex = 0;
        }
        catch
        {
            _interfaceSelector.ItemsSource = new[] { "Default Interface" };
            _interfaceSelector.SelectedIndex = 0;
        }
    }

    // ── UI FACTORY HELPERS ──

    private static Border MakeThresholdSliderCard(string title, string description, Slider slider, TextBlock valueLabel)
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var titleTb = new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") };
        Grid.SetColumn(titleTb, 0);
        Grid.SetColumn(valueLabel, 1);
        header.Children.Add(titleTb);
        header.Children.Add(valueLabel);

        var desc = new TextBlock { Text = description, FontSize = 11, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 2, 0, 8) };

        return new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            Child = new StackPanel { Children = { header, desc, slider } }
        };
    }

    private static Border MakeToggleCard(string title, string description, CheckBox toggle, string? icon = null)
    {
        toggle.Content = null;
        var left = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { }
        };

        if (icon != null)
        {
            left.Children.Add(new Border
            {
                Width = 28, Height = 28,
                CornerRadius = new CornerRadius(6),
                Background = ThemeTokens.SurfaceContainerLowest,
                Child = new TextBlock { Text = icon, FontSize = 12, Foreground = ThemeTokens.Primary, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            });
        }

        var textPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.Medium, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") }
            }
        };
        if (!string.IsNullOrEmpty(description))
            textPanel.Children.Add(new TextBlock { Text = description, FontSize = 11, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 2, 0, 0) });
        left.Children.Add(textPanel);

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Grid.SetColumn(left, 0);
        Grid.SetColumn(toggle, 1);
        toggle.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(left);
        row.Children.Add(toggle);

        return new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 14),
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            Child = row
        };
    }

    private static Border MakeSliderCard(string title, string description, Slider slider, TextBlock valueLabel)
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var titleTb = new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.Medium, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") };
        Grid.SetColumn(titleTb, 0);
        Grid.SetColumn(valueLabel, 1);
        header.Children.Add(titleTb);
        header.Children.Add(valueLabel);

        var desc = new TextBlock { Text = description, FontSize = 11, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 4, 0, 10) };

        return new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 14),
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            Child = new StackPanel { Children = { header, desc, slider } }
        };
    }

    private static Border MakeCheckboxCard(string title, string description, CheckBox toggle)
    {
        var textPanel = new StackPanel
        {
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") },
                new TextBlock { Text = description, FontSize = 11, Foreground = ThemeTokens.OnSurfaceVariant, FontFamily = new FontFamily("Inter"), Margin = new Thickness(0, 2, 0, 0) }
            }
        };

        return new Border
        {
            Background = ThemeTokens.SurfaceContainerLowest,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12),
            Margin = new Thickness(0, 0, 0, 6),
            BorderBrush = ThemeTokens.GhostBorder,
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { toggle, textPanel }
            }
        };
    }

    private static Border MakeThresholdBar(string text, string value, double pct, IBrush color)
    {
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var labelTb = new TextBlock { Text = text, FontSize = 14, Foreground = ThemeTokens.OnSurface, FontFamily = new FontFamily("Inter") };
        var valueTb = new TextBlock { Text = value, FontSize = 13, Foreground = color, FontFamily = new FontFamily("Inter"), FontWeight = FontWeight.Medium };
        Grid.SetColumn(labelTb, 0);
        Grid.SetColumn(valueTb, 1);
        header.Children.Add(labelTb);
        header.Children.Add(valueTb);

        var barBg = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = ThemeTokens.SurfaceContainerLowest };
        var barFill = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = color, HorizontalAlignment = HorizontalAlignment.Left };

        var barContainer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        barContainer.Children.Add(barBg);
        barContainer.Children.Add(barFill);
        barContainer.LayoutUpdated += (s, e) =>
        {
            if (barContainer.Bounds.Width > 0) barFill.Width = barContainer.Bounds.Width * pct;
        };

        return new Border { Margin = new Thickness(0, 6, 0, 6), Child = new StackPanel { Children = { header, barContainer } } };
    }
}
