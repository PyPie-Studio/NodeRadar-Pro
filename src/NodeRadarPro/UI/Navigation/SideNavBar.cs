using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

using Avalonia.Platform;

namespace NodeRadarPro.UI.Navigation;

/// <summary>
/// Persistent left sidebar navigation — 256px wide.
/// Fires PageChanged when user clicks a nav item.
/// </summary>
public class SideNavBar : Border
{
    private readonly Dictionary<string, Border> _navItems = new();
    private string _activePage = "dashboard";

    public event Action<string>? PageChanged;

    public SideNavBar()
    {
        Width = ThemeTokens.SidebarWidth;
        Background = ThemeTokens.NavBg;
        BoxShadow = new BoxShadows(new BoxShadow
        {
            OffsetX = 10,
            OffsetY = 0,
            Blur = 30,
            Color = ThemeTokens.ShadowSidebar
        });

        var logoImage = new Image
        {
            Width = 32,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        try
        {
            using var logoStream = AssetLoader.Open(new Uri("avares://NodeRadarPro/Resources/NodeRadar Pro Icon.png"));
            logoImage.Source = new Bitmap(logoStream);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, "UI", $"Failed to load navbar logo: {ex.Message}");
        }
        RenderOptions.SetBitmapInterpolationMode(logoImage, BitmapInterpolationMode.HighQuality);

        // ── Header: Icon + Title ──
        var iconBg = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(10),
            Background = ThemeTokens.SurfaceContainerHigh,
            Child = logoImage
        };

        var titleBlock = new StackPanel
        {
            Spacing = 1,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "NodeRadar Pro",
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    Foreground = ThemeTokens.PurpleLight,
                    FontFamily = ThemeTokens.DefaultFont
                },
                new TextBlock
                {
                    Text = "by PyPie Studio",
                    FontSize = 10,
                    Foreground = ThemeTokens.SlateText,
                    FontFamily = ThemeTokens.DefaultFont,
                    LetterSpacing = 0.8
                }
            }
        };

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(16, 24, 16, 32),
            Children = { iconBg, titleBlock }
        };

        // ── Nav Items ──
        var mainNav = new StackPanel { Spacing = 4, Margin = new Thickness(12, 0) };
        mainNav.Children.Add(MakeNavItem("dashboard", ThemeTokens.SvgChart, "Dashboard", "Overview of network health, security alerts, and real-time connectivity stats."));
        mainNav.Children.Add(MakeNavItem("radar", ThemeTokens.SvgRadar, "Network Radar", "Live active sonar view. Discovers devices and visualizes network topology."));
        mainNav.Children.Add(MakeNavItem("traceroute", ThemeTokens.SvgTraceroute, "Visual Traceroute", "Map the hop-by-hop digital pathway to any network destination."));
        mainNav.Children.Add(MakeNavItem("inventory", ThemeTokens.SvgInventory, "Device Inventory", "Detailed database of all known and discovered network assets."));
        mainNav.Children.Add(MakeNavItem("portscans", ThemeTokens.SvgPortScans, "Port Scans", "Deep-dive port enumeration and service discovery for specific nodes."));
        mainNav.Children.Add(MakeNavItem("alerts", ThemeTokens.SvgShield, "Alerts", "Security and connectivity event log with priority-based alerts."));
        mainNav.Children.Add(MakeNavItem("logs", ThemeTokens.SvgLogs, "System Logs", "Raw system execution logs and debugging diagnostic data."));

        // ── Footer Nav ──
        var footerNav = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(12, 0, 12, 16)
        };
        footerNav.Children.Add(MakeNavItem("settings", ThemeTokens.SvgGear, "Settings", "Configure scanning parameters, alert thresholds, and security preferences."));
        footerNav.Children.Add(MakeNavItem("support", ThemeTokens.SvgSupport, "Support", "Technical documentation, version information, and developer support."));

        var footerSeparator = new Border
        {
            Height = 1,
            Background = ThemeTokens.GhostBorder,
            Margin = new Thickness(16, 0, 16, 8)
        };

        // ── Assembly ──
        var topSection = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);

        var bottomSection = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Children = { footerSeparator, footerNav }
        };
        DockPanel.SetDock(bottomSection, Dock.Bottom);

        topSection.Children.Add(header);
        topSection.Children.Add(bottomSection);
        topSection.Children.Add(new ScrollViewer
        {
            Content = mainNav,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden
        });

        Child = topSection;

        // Set initial active
        SetActive("dashboard");
    }

    public void SetActive(string pageName)
    {
        _activePage = pageName;
        foreach (var (name, border) in _navItems)
        {
            bool isActive = name == pageName;
            border.Background = isActive ? ThemeTokens.NavItemActive : Brushes.Transparent;
            border.BorderThickness = isActive ? new Thickness(3, 0, 0, 0) : new Thickness(0);
            border.BorderBrush = isActive ? ThemeTokens.NavAccentBorder : Brushes.Transparent;

            if (border.Child is StackPanel sp)
            {
                foreach (var c in sp.Children)
                {
                    if (c is TextBlock tb && tb.Tag?.ToString() == "label")
                    {
                        tb.Foreground = isActive ? ThemeTokens.NavTextActive : ThemeTokens.NavTextInactive;
                        tb.FontWeight = isActive ? FontWeight.Bold : FontWeight.Medium;
                    }
                    else if (c is Avalonia.Controls.Shapes.Path pathIcon && pathIcon.Tag?.ToString() == "icon")
                    {
                        pathIcon.Fill = isActive ? ThemeTokens.NavTextActive : ThemeTokens.NavTextInactive;
                    }
                    else if (c is TextBlock icon && icon.Tag?.ToString() == "icon")
                    {
                        icon.Foreground = isActive ? ThemeTokens.NavTextActive : ThemeTokens.NavTextInactive;
                    }
                }
            }
        }
    }

    private Border MakeNavItem(string name, string icon, string label, string tooltip)
    {
        Control iconControl;
        bool isSvg = icon.StartsWith('M') || icon.StartsWith('m');

        if (isSvg)
        {
            iconControl = ThemeTokens.VectorIcon(icon, 18, ThemeTokens.NavTextInactive);
            iconControl.Width = 24;
            iconControl.Height = 24;
        }
        else
        {
            iconControl = new TextBlock
            {
                Text = icon,
                FontSize = 16,
                Foreground = ThemeTokens.NavTextInactive,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 24,
                TextAlignment = TextAlignment.Center
            };
        }

        iconControl.Tag = "icon";

        var labelText = new TextBlock
        {
            Text = label,
            FontSize = 13,
            Foreground = ThemeTokens.NavTextInactive,
            FontWeight = FontWeight.Medium,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = ThemeTokens.DefaultFont,
            LetterSpacing = 0.3,
            Tag = "label"
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children = { iconControl, labelText }
        };

        var border = new Border
        {
            Focusable = true,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10),
            Background = Brushes.Transparent,
            Cursor = ThemeTokens.HandCursor,
            Child = content
        };

        ThemeTokens.SetToolTip(border, tooltip);

        border.KeyDown += (s, e) =>
        {
            if (e.Key is Avalonia.Input.Key.Enter or Avalonia.Input.Key.Space)
            {
                SetActive(name);
                PageChanged?.Invoke(name);
                e.Handled = true;
            }
        };

        border.PointerEntered += (s, e) =>
        {
            if (name != _activePage)
                border.Background = ThemeTokens.NavItemHover;
        };
        border.PointerExited += (s, e) =>
        {
            if (name != _activePage)
                border.Background = Brushes.Transparent;
        };
        border.PointerPressed += (s, e) =>
        {
            SetActive(name);
            PageChanged?.Invoke(name);
        };

        _navItems[name] = border;
        return border;
    }

    internal string ActivePage => _activePage;
    internal IReadOnlyDictionary<string, Border> NavItems => _navItems;
    internal void TriggerItemClick(string name) { SetActive(name); PageChanged?.Invoke(name); }
}
