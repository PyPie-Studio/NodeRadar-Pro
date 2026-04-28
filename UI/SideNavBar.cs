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
        mainNav.Children.Add(MakeNavItem("dashboard", "▣", "Dashboard"));
        mainNav.Children.Add(MakeNavItem("radar", "◎", "Network Radar"));
        mainNav.Children.Add(MakeNavItem("inventory", "⊞", "Device Inventory"));
        mainNav.Children.Add(MakeNavItem("portscans", "⊟", "Port Scans"));
        mainNav.Children.Add(MakeNavItem("alerts", "⚠", "Alerts"));
        mainNav.Children.Add(MakeNavItem("logs", "☰", "System Logs"));

        // ── Footer Nav ──
        var footerNav = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(12, 0, 12, 16)
        };
        footerNav.Children.Add(MakeNavItem("settings", "⚙", "Settings"));
        footerNav.Children.Add(MakeNavItem("support", "?", "Support"));

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

    private Border MakeNavItem(string name, string icon, string label)
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
