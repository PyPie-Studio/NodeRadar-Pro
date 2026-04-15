using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodeRadarPro.Core;
using NodeRadarPro.Data;
using System;

namespace NodeRadarPro.UI;

/// <summary>
/// Settings panel for configuring offline device behavior and monitor interval.
/// </summary>
public class SettingsPanel : Border
{
    private static readonly IBrush BgPanel = SolidColorBrush.Parse("#151525");
    private static readonly IBrush AccentPurple = SolidColorBrush.Parse("#8A2BE2");
    private static readonly IBrush TextWhite = Brushes.WhiteSmoke;
    private static readonly IBrush TextGrey = SolidColorBrush.Parse("#888899");
    private static readonly IBrush BorderColor = SolidColorBrush.Parse("#2A2A45");

    private readonly LocalDatabase _db;
    private AppSettings _settings;

    private readonly CheckBox _fadeOutToggle;
    private readonly NumericUpDown _fadeOutSeconds;
    private readonly NumericUpDown _monitorInterval;
    private readonly Button _saveBtn;
    private readonly Button _closeBtn;

    /// <summary>Fired when settings are saved.</summary>
    public event Action<AppSettings>? SettingsSaved;
    public event Action? CloseRequested;

    public SettingsPanel(LocalDatabase db)
    {
        _db = db;
        _settings = db.LoadSettings();

        Width = 320;
        Background = BgPanel;
        BorderBrush = BorderColor;
        BorderThickness = new Thickness(1, 0, 0, 0);
        IsVisible = false;

        _closeBtn = new Button
        {
            Content = "✕",
            Background = SolidColorBrush.Parse("#333345"),
            Foreground = TextWhite,
            Width = 32, Height = 32,
            CornerRadius = new CornerRadius(16),
            HorizontalAlignment = HorizontalAlignment.Right,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        _closeBtn.Click += (s, e) => { IsVisible = false; CloseRequested?.Invoke(); };

        _fadeOutToggle = new CheckBox
        {
            Content = "Enable offline device fade-out",
            IsChecked = _settings.EnableOfflineFadeOut,
            Foreground = TextWhite,
            FontSize = 13,
            Margin = new Thickness(0, 4)
        };

        _fadeOutSeconds = new NumericUpDown
        {
            Value = _settings.FadeOutSeconds,
            Minimum = 30,
            Maximum = 3600,
            Increment = 30,
            Foreground = TextWhite,
            FontSize = 13,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _monitorInterval = new NumericUpDown
        {
            Value = _settings.MonitorIntervalSeconds,
            Minimum = 10,
            Maximum = 600,
            Increment = 10,
            Foreground = TextWhite,
            FontSize = 13,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _fadeOutToggle.IsCheckedChanged += (s, e) =>
        {
            _fadeOutSeconds.IsEnabled = _fadeOutToggle.IsChecked == true;
        };
        _fadeOutSeconds.IsEnabled = _settings.EnableOfflineFadeOut;

        _saveBtn = new Button
        {
            Content = "💾  Save Settings",
            Background = AccentPurple,
            Foreground = TextWhite,
            Height = 40,
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            FontSize = 13
        };
        _saveBtn.Click += OnSaveClicked;

        var panel = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(16, 12, 16, 16),
            Children =
            {
                _closeBtn,
                MakeHeader("Settings"),
                MakeSeparator(),
                MakeSectionLabel("CONNECTIVITY MONITOR"),
                MakeFieldLabel("Ping interval (seconds)"),
                _monitorInterval,
                MakeSeparator(),
                MakeSectionLabel("OFFLINE DEVICES"),
                _fadeOutToggle,
                MakeFieldLabel("Fade-out time (seconds)"),
                _fadeOutSeconds,
                new TextBlock
                {
                    Text = "When enabled, offline devices will be hidden from the radar after the specified time. When disabled, they remain visible indefinitely (dimmed red).",
                    Foreground = TextGrey,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4)
                },
                MakeSeparator(),
                _saveBtn
            }
        };

        Child = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    public void Show()
    {
        _settings = _db.LoadSettings();
        _fadeOutToggle.IsChecked = _settings.EnableOfflineFadeOut;
        _fadeOutSeconds.Value = _settings.FadeOutSeconds;
        _monitorInterval.Value = _settings.MonitorIntervalSeconds;
        _fadeOutSeconds.IsEnabled = _settings.EnableOfflineFadeOut;
        IsVisible = true;
    }

    private void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _settings.EnableOfflineFadeOut = _fadeOutToggle.IsChecked == true;
        _settings.FadeOutSeconds = (int)(_fadeOutSeconds.Value ?? 300);
        _settings.MonitorIntervalSeconds = (int)(_monitorInterval.Value ?? 60);

        _db.SaveSettings(_settings);

        _saveBtn.Content = "✅  Saved!";
        var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        timer.Tick += (s, ev) => { _saveBtn.Content = "💾  Save Settings"; timer.Stop(); };
        timer.Start();

        SettingsSaved?.Invoke(_settings);
    }

    // ── Helpers ──

    private static TextBlock MakeHeader(string text) => new()
    {
        Text = text, FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Brushes.WhiteSmoke
    };

    private static TextBlock MakeSectionLabel(string text) => new()
    {
        Text = text, FontSize = 10, FontWeight = FontWeight.Bold,
        Foreground = SolidColorBrush.Parse("#555566"),
        Margin = new Thickness(0, 4, 0, 2), LetterSpacing = 1.5
    };

    private static TextBlock MakeFieldLabel(string text) => new()
    {
        Text = text, FontSize = 11, Foreground = SolidColorBrush.Parse("#999AAA"),
        Margin = new Thickness(0, 2, 0, 0)
    };

    private static Border MakeSeparator() => new()
    {
        Height = 1, Background = SolidColorBrush.Parse("#252540"),
        Margin = new Thickness(0, 6, 0, 6)
    };
}
