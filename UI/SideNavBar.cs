using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

using Avalonia.Platform;

namespace NodeRadarPro.UI;

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
            OffsetX = 10, OffsetY = 0, Blur = 30,
            Color = Color.FromArgb(77, 0, 0, 0)
        });

        var logoImage = new Image
        {
            Source = new Bitmap(AssetLoader.Open(new Uri("avares://NodeRadar Pro/Resources/NodeRadar Pro Icon.png"))),
            Width = 32, Height = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        RenderOptions.SetBitmapInterpolationMode(logoImage, BitmapInterpolationMode.HighQuality);

        // ── Header: Icon + Title ──
        var iconBg = new Border
        {
            Width = 40, Height = 40,
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
                    FontFamily = new FontFamily("Inter")
                },
                new TextBlock
                {
                    Text = "by PyPie Studio",
                    FontSize = 10,
                    Foreground = ThemeTokens.SlateText,
                    FontFamily = new FontFamily("Inter"),
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
        mainNav.Children.Add(MakeNavItem("radar", "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8Z", "Network Radar", "Live active sonar view. Discovers devices and visualizes network topology."));
        mainNav.Children.Add(MakeNavItem("traceroute", "M22,12L18,8V11H10V5H13L9,1L5,5H8V13H18V16L22,12Z", "Visual Traceroute", "Map the hop-by-hop digital pathway to any network destination."));
        mainNav.Children.Add(MakeNavItem("inventory", "M17,1H7C5.89,1 5,1.89 5,3V21C5,22.1 5.89,23 7,23H17C18.1,23 19,22.1 19,21V3C19,1.89 18.1,1 17,1M12,19A2,2 0 1,1 14,17A2,2 0 0,1 12,19M15,8H9V7H15V8Z", "Device Inventory", "Detailed database of all known and discovered network assets."));
        mainNav.Children.Add(MakeNavItem("portscans", "M12,2L4.5,20.29L5.21,21L12,18L18.79,21L19.5,20.29L12,2Z", "Port Scans", "Deep-dive port enumeration and service discovery for specific nodes."));
        mainNav.Children.Add(MakeNavItem("alerts", ThemeTokens.SvgShield, "Alerts", "Security and connectivity event log with priority-based alerts."));
        mainNav.Children.Add(MakeNavItem("logs", "M3,5H21V7H3V5M3,9H21V11H3V9M3,13H21V15H3V13M3,17H21V19H3V17Z", "System Logs", "Raw system execution logs and debugging diagnostic data."));

        // ── Footer Nav ──
        var footerNav = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(12, 0, 12, 16)
        };
        footerNav.Children.Add(MakeNavItem("settings", "M12,15.5A2.5,2.5 0 0,1 9.5,13A2.5,2.5 0 0,1 12,10.5A2.5,2.5 0 0,1 14.5,13A2.5,2.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.35 19.43,11.03L21.54,9.37C21.73,9.2 21.78,8.92 21.66,8.7L19.66,5.24C19.54,5.02 19.27,4.93 19.05,5.03L16.56,5.87C16.04,5.49 15.48,5.17 14.87,4.93L14.49,2.27C14.45,2.04 14.25,1.87 14,1.87H10C9.75,1.87 9.55,2.04 9.51,2.27L9.13,4.93C8.52,5.17 7.96,5.49 7.44,5.87L4.95,5.03C4.73,4.93 4.46,5.02 4.34,5.24L2.34,8.7C2.22,8.92 2.27,9.2 2.46,9.37L4.57,11.03C4.53,11.35 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.8 2.22,15.08 2.34,15.3L4.34,18.76C4.46,18.98 4.73,19.07 4.95,18.97L7.44,18.13C7.96,18.51 8.52,18.83 9.13,19.07L9.51,21.73C9.55,21.96 9.75,22.13 10,22.13H14C14.25,22.13 14.45,21.96 14.49,21.73L14.87,19.07C15.48,18.83 16.04,18.51 16.56,18.13L19.05,18.97C19.27,19.07 19.54,18.98 19.66,18.76L21.66,15.3C21.78,15.08 21.73,14.8 21.54,14.63L19.43,12.97Z", "Settings", "Configure scanning parameters, alert thresholds, and security preferences."));
        footerNav.Children.Add(MakeNavItem("support", "M11,18H13V16H11V18M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20C6.47,20 2,15.5 2,12C2,8.47 6.47,4 12,4C17.53,4 22,8.47 22,12C22,15.5 17.53,20 12,20M12,6A4,4 0 0,0 8,10H10A2,2 0 0,1 12,8A2,2 0 0,1 14,10C14,12 11,11.75 11,15H13C13,12.75 16,12.5 16,10A4,4 0 0,0 12,6Z", "Support", "Technical documentation, version information, and developer support."));

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
        var iconText = new TextBlock
        {
            Text = icon,
            FontSize = 16,
            Foreground = ThemeTokens.NavTextInactive,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 24,
            TextAlignment = TextAlignment.Center,
            Tag = "icon"
        };

        var labelText = new TextBlock
        {
            Text = label,
            FontSize = 13,
            Foreground = ThemeTokens.NavTextInactive,
            FontWeight = FontWeight.Medium,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Inter"),
            LetterSpacing = 0.3,
            Tag = "label"
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children = { iconText, labelText }
        };

        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10),
            Background = Brushes.Transparent,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Child = content
        };

        ThemeTokens.SetToolTip(border, tooltip);

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
}
