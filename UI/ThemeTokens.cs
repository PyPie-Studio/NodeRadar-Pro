using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace NodeRadarPro.UI;

/// <summary>
/// Centralized design tokens for the "Kinetic Observatory" theme.
/// Material Design 3 palette extracted from the HTML mockups.
/// All UI files reference these instead of hardcoding colors.
/// </summary>
public static class ThemeTokens
{
    // ═══════════════════════════════════════════
    // ██  SURFACE HIERARCHY (dark → light)
    // ═══════════════════════════════════════════
    public static readonly IBrush Surface = new SolidColorBrush(Color.Parse("#0C1322"), 0.7);
    public static readonly IBrush SurfaceDim = SolidColorBrush.Parse("#0C1322");
    public static readonly IBrush SurfaceContainerLowest = SolidColorBrush.Parse("#070E1D");
    public static readonly IBrush SurfaceContainerLow = SolidColorBrush.Parse("#141B2B");
    public static readonly IBrush SurfaceContainer = SolidColorBrush.Parse("#191F2F");
    public static readonly IBrush SurfaceContainerHigh = SolidColorBrush.Parse("#232A3A");
    public static readonly IBrush SurfaceContainerHighest = SolidColorBrush.Parse("#2E3545");
    public static readonly IBrush SurfaceVariant = SolidColorBrush.Parse("#2E3545");
    public static readonly IBrush SurfaceBright = SolidColorBrush.Parse("#323949");

    // ═══════════════════════════════════════════
    // ██  PRIMARY (Purple)
    // ═══════════════════════════════════════════
    public static readonly IBrush Primary = SolidColorBrush.Parse("#DFB7FF");
    public static readonly IBrush PrimaryContainer = SolidColorBrush.Parse("#6B21A8");
    public static readonly IBrush OnPrimary = SolidColorBrush.Parse("#4A007F");
    public static readonly IBrush OnPrimaryContainer = SolidColorBrush.Parse("#D7A8FF");
    public static readonly IBrush InversePrimary = SolidColorBrush.Parse("#803ABD");

    // ═══════════════════════════════════════════
    // ██  SECONDARY (Lavender)
    // ═══════════════════════════════════════════
    public static readonly IBrush Secondary = SolidColorBrush.Parse("#D3BBFF");
    public static readonly IBrush SecondaryContainer = SolidColorBrush.Parse("#592DA2");
    public static readonly IBrush OnSecondaryContainer = SolidColorBrush.Parse("#C8AAFF");

    // ═══════════════════════════════════════════
    // ██  TERTIARY (Cyan — "Healthy" accent)
    // ═══════════════════════════════════════════
    public static readonly IBrush Tertiary = SolidColorBrush.Parse("#4CD7F6");
    public static readonly IBrush TertiaryContainer = SolidColorBrush.Parse("#005362");
    public static readonly IBrush OnTertiaryContainer = SolidColorBrush.Parse("#3CCCEA");

    // ═══════════════════════════════════════════
    // ██  ERROR (Soft red — "Offline" accent)
    // ═══════════════════════════════════════════
    public static readonly IBrush Error = SolidColorBrush.Parse("#FFB4AB");
    public static readonly IBrush ErrorContainer = SolidColorBrush.Parse("#93000A");

    // ═══════════════════════════════════════════
    // ██  TEXT / ON-SURFACE
    // ═══════════════════════════════════════════
    public static readonly IBrush OnSurface = SolidColorBrush.Parse("#DCE2F7");
    public static readonly IBrush OnSurfaceVariant = SolidColorBrush.Parse("#CFC2D4");
    public static readonly IBrush Outline = SolidColorBrush.Parse("#988D9E");
    public static readonly IBrush OutlineVariant = SolidColorBrush.Parse("#4C4452");

    // ═══════════════════════════════════════════
    // ██  SIDEBAR-SPECIFIC
    // ═══════════════════════════════════════════
    public static readonly IBrush NavBg = SolidColorBrush.Parse("#141B2B");
    public static readonly IBrush NavItemActive = SolidColorBrush.Parse("#232A3A");
    public static readonly IBrush NavItemHover = SolidColorBrush.Parse("#1E2536");
    public static readonly IBrush NavTextInactive = SolidColorBrush.Parse("#8892A8");
    public static readonly IBrush NavTextActive = SolidColorBrush.Parse("#B794F6");
    public static readonly IBrush NavAccentBorder = SolidColorBrush.Parse("#A855F7");
    public static readonly IBrush PurpleLight = SolidColorBrush.Parse("#E9D5FF");
    public static readonly IBrush SlateText = SolidColorBrush.Parse("#94A3B8");

    // ═══════════════════════════════════════════
    // ██  GHOST BORDER (10% opacity)
    // ═══════════════════════════════════════════
    public static readonly IBrush GhostBorder = new SolidColorBrush(Color.Parse("#4C4452"), 0.10);
    public static readonly IBrush GhostBorder20 = new SolidColorBrush(Color.Parse("#4C4452"), 0.20);
    public static readonly IBrush GhostBorder30 = new SolidColorBrush(Color.Parse("#4C4452"), 0.30);

    // ═══════════════════════════════════════════
    // ██  STANDARD DIMENSIONS
    // ═══════════════════════════════════════════
    public const double SidebarWidth = 256;
    public const double TopBarHeight = 56;
    public const double CardRadius = 12;
    public const double ButtonRadius = 6;
    public const double InputRadius = 8;

    public const string AppVersion = "1.0.0";

    // ── SVG Vector Paths (Modern Technical Icons) ──
    public const string SvgRouter = "M4,17V9H20V17H4M12,10A1,1 0 0,0 11,11A1,1 0 0,0 12,12A1,1 0 0,0 13,11A1,1 0 0,0 12,10M15,10A1,1 0 0,0 14,11A1,1 0 0,0 15,12A1,1 0 0,0 16,11A1,1 0 0,0 15,10M18,10A1,1 0 0,0 17,11A1,1 0 0,0 18,12A1,1 0 0,0 19,11A1,1 0 0,0 18,10M7,12V14H10V12H7Z";
    public const string SvgServer = "M4,4H20A1,1 0 0,1 21,5V9A1,1 0 0,1 20,10H4A1,1 0 0,1 3,9V5A1,1 0 0,1 4,4M4,14H20A1,1 0 0,1 21,15V19A1,1 0 0,1 20,20H4A1,1 0 0,1 3,19V15A1,1 0 0,1 4,14M9,7A1,1 0 1,0 8,8A1,1 0 0,0 9,7M9,17A1,1 0 1,0 8,18A1,1 0 0,0 9,17M12,7A1,1 0 1,0 11,8A1,1 0 0,0 12,7M12,17A1,1 0 1,0 11,18A1,1 0 0,0 12,17Z";
    public const string SvgDesktop = "M21,14H3V4H21M21,2H3C1.89,2 1,2.89 1,4V16A2,2 0 0,0 3,18H10L9,19V20H15V19L14,18H21A2,2 0 0,0 23,16V4C23,2.89 22.1,2 21,2Z";
    public const string SvgPhone = "M17,19H7V5H17M17,1H7C5.89,1 5,1.89 5,3V21C5,22.1 5.89,23 7,23H17C18.1,23 19,22.1 19,21V3C19,1.89 18.1,1 17,1Z";
    public const string SvgPrinter = "M18,3H6V7H18M19,12A1,1 0 0,1 18,11A1,1 0 0,1 19,10A1,1 0 0,1 20,11A1,1 0 0,1 19,12M16,19H8V14H16M19,8H5A3,3 0 0,0 2,11V17H6V21H18V17H22V11A3,3 0 0,0 19,8Z";
    public const string SvgTv = "M21,3H3C1.89,3 1,3.89 1,5V17A2,2 0 0,0 3,19H8V21H16V19H21A2,2 0 0,0 23,17V5C23,3.89 22.1,3 21,3M21,17H3V5H21V17Z";
    public const string SvgIot = "M12,2A7,7 0 0,0 5,9C5,11.38 6.19,13.47 8,14.74V17A1,1 0 0,0 9,18H15A1,1 0 0,0 16,17V14.74C17.81,13.47 19,11.38 19,9A7,7 0 0,0 12,2M9,21A1,1 0 0,0 10,22H14A1,1 0 0,0 15,21V20H9V21Z";
    public const string SvgShield = "M12,1L3,5V11C3,16.55 6.84,21.74 12,23C17.16,21.74 21,16.55 21,11V5L12,1Z";
    public const string SvgChart = "M2,13H8V21H2V13M9,3H15V21H9V3M16,8H22V21H16V8Z";
    public const string SvgCamera = "M4,4H20A2,2 0 0,1 22,6V18A2,2 0 0,1 20,20H4A2,2 0 0,1 2,18V6A2,2 0 0,1 4,4M4,6V18H20V6H4M12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9Z";
    public const string SvgSpeaker = "M12,2A3,3 0 0,1 15,5V11A3,3 0 0,1 12,14A3,3 0 0,1 9,11V5A3,3 0 0,1 12,2M19,11C19,14.53 16.39,17.44 13,17.93V21H11V17.93C7.61,17.44 5,14.53 5,11H7C7,13.76 9.24,16 12,16C14.76,16 17,13.76 17,11H19Z";
    public const string SvgGamepad = "M21.58,16.09L21.57,16.25C21.43,17.33 20.36,18.06 19.34,17.8C18.68,17.64 18.17,17.12 18,16.47L17.5,14H6.5L6,16.47C5.83,17.12 5.32,17.64 4.66,17.8C3.64,18.06 2.57,17.33 2.43,16.25L2.42,16.09L2.83,8C3,6.33 4.41,5 6.09,5H17.91C19.59,5 21,6.33 21.17,8L21.58,16.09M10,8.5V10H8.5V11.5H7V10H5.5V8.5H7V7H8.5V8.5H10M16,11A1,1 0 0,0 17,10A1,1 0 0,0 16,9A1,1 0 0,0 15,10A1,1 0 0,0 16,11M18,8.5A1,1 0 0,0 19,7.5A1,1 0 0,0 18,6.5A1,1 0 0,0 17,7.5A1,1 0 0,0 18,8.5Z";
    public const string SvgNas = "M19,15V17H5V15H19M19,11V13H5V11H19M19,7V9H5V7H19M21,5V19A2,2 0 0,1 19,21H5A2,2 0 0,1 3,19V5A2,2 0 0,1 5,3H19A2,2 0 0,1 21,5Z";
    public const string SvgFirewall = "M12,1L3,5V11C3,16.55 6.84,21.74 12,23C17.16,21.74 21,16.55 21,11V5L12,1M12,11.99H19C18.47,16.11 15.72,19.78 12,20.92V11.99H5V6.3L12,3.19V11.99Z";
    public const string SvgSwitch = "M4,3H20A2,2 0 0,1 22,5V15A2,2 0 0,1 20,17H4A2,2 0 0,1 2,15V5A2,2 0 0,1 4,3M4,5V15H20V5H4M6,7H8V9H6V7M10,7H12V9H10V7M14,7H16V9H14V7M18,7H20V9H18V7M6,11H8V13H6V11M10,11H12V13H10V11M14,11H16V13H14V11M18,11H20V13H18V11Z";
    public const string SvgAccessPoint = "M12,3C14.8,3 17.3,4.1 19.1,6L20.5,4.6C18.3,2.4 15.3,1 12,1C8.7,1 5.7,2.4 3.5,4.6L4.9,6C6.7,4.1 9.2,3 12,3M12,7C13.7,7 15.3,7.7 16.4,8.8L17.8,7.4C16.3,5.9 14.3,5 12,5C9.7,5 7.7,5.9 6.2,7.4L7.6,8.8C8.7,7.7 10.3,7 12,7M12,11C12.8,11 13.5,11.3 14,11.8L15.4,10.4C14.5,9.5 13.3,9 12,9C10.7,9 9.5,9.5 8.6,10.4L10,11.8C10.5,11.3 11.2,11 12,11M12,13C10.9,13 10,13.9 10,15C10,16.1 10.9,17 12,17C13.1,17 14,16.1 14,15C14,13.9 13.1,13 12,13M11,18V22H13V18H11Z";
    public const string SvgTablet = "M19,18H5V6H19M21,4H3C1.89,4 1,4.89 1,6V18A2,2 0 0,0 3,20H21A2,2 0 0,0 23,18V6C23,4.89 22.1,4 21,4Z";
    public const string SvgDvr = "M21,3H3C1.89,3 1,3.89 1,5V19A2,2 0 0,0 3,21H21A2,2 0 0,0 23,19V5C23,3.89 22.1,3 21,3M21,19H3V5H21V19M8,8H16V16H8V8Z";
    public const string SvgSearch = "M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z";
    public const string SvgTrash = "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z";
    public const string SvgSave = "M15,9H5V5H15M12,19A3,3 0 0,1 9,16A3,3 0 0,1 12,13A3,3 0 0,1 15,16A3,3 0 0,1 12,19M17,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3Z";
    public const string SvgCheck = "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z";
    public const string SvgBolt = "M11,15H6L13,1V9H18L11,23V15Z";
    public const string SvgStop = "M18,18H6V6H18V18Z";
    public const string SvgPlay = "M8,5.14V19.14L19,12.14L8,5.14Z";

    public static string GetDeviceSvg(string? iconKey)
    {
        return iconKey?.ToLowerInvariant() switch
        {
            "router" or "gateway" => SvgRouter,
            "server" => SvgServer,
            "desktop" or "pc" => SvgDesktop,
            "phone" or "mobile" => SvgPhone,
            "printer" => SvgPrinter,
            "tv" => SvgTv,
            "camera" => SvgCamera,
            "speaker" => SvgSpeaker,
            "gamepad" or "console" => SvgGamepad,
            "nas" => SvgNas,
            "switch" => SvgSwitch,
            "accesspoint" or "ap" => SvgAccessPoint,
            "tablet" => SvgTablet,
            "dvr" or "nvr" => SvgDvr,
            "firewall" => SvgFirewall,
            _ => SvgIot
        };
    }

    public static Avalonia.Controls.Shapes.Path VectorIcon(string pathData, double size = 20, IBrush? color = null) => new()
    {
        Data = Geometry.Parse(pathData),
        Width = size,
        Height = size,
        Stretch = Stretch.Uniform,
        Fill = color ?? OnSurface
    };

    public static void SetToolTip(Control control, string tip)
    {
        ToolTip.SetTip(control, tip);
    }

    public static void AddCopyAction(Control control, string? initialValue = null, Func<string?>? valueProvider = null)
    {
        control.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);

        control.PointerPressed += async (s, e) =>
        {
            string? liveValue = valueProvider?.Invoke() ?? (control as TextBlock)?.Text ?? initialValue;

            if (string.IsNullOrEmpty(liveValue) || liveValue == "Unknown" || liveValue == "—") return;

            var topLevel = TopLevel.GetTopLevel(control);
            var clipboard = topLevel?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(liveValue);
                SetToolTip(control, "Copied!");
                await Task.Delay(1500);
                SetToolTip(control, $"Click to copy: {liveValue}");
            }
        };
    }

    // ═══════════════════════════════════════════
    // ██  FACTORY: Text
    // ═══════════════════════════════════════════

    public static TextBlock Headline(string text, double size = 24) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = FontWeight.Bold,
        Foreground = OnSurface,
        FontFamily = new FontFamily("Inter")
    };

    public static TextBlock Body(string text, double size = 16) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = FontWeight.Normal,
        Foreground = OnSurfaceVariant,
        FontFamily = new FontFamily("Inter")
    };

    public static TextBlock Label(string text, double size = 13, IBrush? color = null) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = FontWeight.Normal,
        Foreground = color ?? OnSurfaceVariant,
        FontFamily = new FontFamily("Inter"),
        LetterSpacing = 0.5
    };

    public static TextBlock SectionLabel(string text) => new()
    {
        Text = text,
        FontSize = 12,
        FontWeight = FontWeight.SemiBold,
        Foreground = new SolidColorBrush(Color.Parse("#CFC2D4"), 0.7),
        LetterSpacing = 1.5,
        Margin = new Thickness(0, 4, 0, 8),
        FontFamily = new FontFamily("Inter")
    };

    public static readonly IBrush HealthSafe = Tertiary;
    public static readonly IBrush HealthWarning = new SolidColorBrush(Color.Parse("#EAB308"));
    public static readonly IBrush HealthCritical = Error;

    // ═══════════════════════════════════════════
    // ██  FACTORY: Card
    // ═══════════════════════════════════════════

    public static Border Card(Control child, IBrush? background = null, double padding = 20) => new()
    {
        Background = background ?? SurfaceContainerHigh,
        CornerRadius = new CornerRadius(CardRadius),
        Padding = new Thickness(padding),
        BorderBrush = GhostBorder,
        BorderThickness = new Thickness(1),
        Child = child
    };

    public static Border GlassCard(Control child, double padding = 24) => new()
    {
        Background = new SolidColorBrush(Color.Parse("#FFFFFF"), 0.03),
        CornerRadius = new CornerRadius(16),
        Padding = new Thickness(padding),
        BorderBrush = new SolidColorBrush(Color.Parse("#FFFFFF"), 0.08),
        BorderThickness = new Thickness(1),
        ClipToBounds = true,
        Child = child
    };

    // ═══════════════════════════════════════════
    // ██  FACTORY: Input
    // ═══════════════════════════════════════════

    public static TextBox Input(string placeholder, double width = double.NaN) => new()
    {
        PlaceholderText = placeholder,
        Background = SurfaceContainerLowest,
        Foreground = OnSurface,
        BorderThickness = new Thickness(0),
        CornerRadius = new CornerRadius(InputRadius),
        Padding = new Thickness(12, 10),
        FontSize = 15,
        FontFamily = new FontFamily("Inter"),
        Width = double.IsNaN(width) ? double.NaN : width
    };

    // ═══════════════════════════════════════════
    // ██  FACTORY: Button
    // ═══════════════════════════════════════════

    public static Button PrimaryButton(string text) => new()
    {
        Content = text,
        Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.Parse("#7C3AED"), 0), new GradientStop(Color.Parse("#6B21A8"), 1) }
        },
        Foreground = Brushes.White,
        FontSize = 15,
        FontWeight = FontWeight.SemiBold,
        FontFamily = new FontFamily("Inter"),
        Height = 44,
        CornerRadius = new CornerRadius(8),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        Padding = new Thickness(24, 0),
        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
    };

    public static Button SecondaryButton(string text) => new()
    {
        Content = text,
        Background = Brushes.Transparent,
        Foreground = OnSurface,
        FontSize = 15,
        FontFamily = new FontFamily("Inter"),
        Height = 44,
        CornerRadius = new CornerRadius(8),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        BorderBrush = GhostBorder20,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(24, 0),
        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
    };

    public static Button TertiaryButton(string text) => new()
    {
        Content = text,
        Background = Brushes.Transparent,
        Foreground = Tertiary,
        FontSize = 14,
        FontFamily = new FontFamily("Inter"),
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        Padding = new Thickness(8, 4),
        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
    };

    public static Button DangerButton(string text) => new()
    {
        Content = text,
        Background = Brushes.Transparent,
        Foreground = Error,
        FontSize = 15,
        FontWeight = FontWeight.SemiBold,
        FontFamily = new FontFamily("Inter"),
        Height = 44,
        CornerRadius = new CornerRadius(8),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        Padding = new Thickness(24, 0),
        BorderBrush = new SolidColorBrush(Color.Parse("#FFB4AB"), 0.3),
        BorderThickness = new Thickness(1),
        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
    };

    // ═══════════════════════════════════════════
    // ██  FACTORY: Status Indicators
    // ═══════════════════════════════════════════

    public static Border StatusDot(bool isOnline, double size = 10)
    {
        var dot = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            Background = isOnline ? Tertiary : Error,
            Opacity = isOnline ? 1.0 : 0.6
        };

        if (isOnline)
        {
            dot.BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 8,
                Color = Color.Parse("#4CD7F6")
            });
        }

        return dot;
    }

    public static Border StatusBadge(string text, bool isOnline)
    {
        return new Border
        {
            Background = isOnline
                ? new SolidColorBrush(Color.Parse("#005362"), 0.3)
                : new SolidColorBrush(Color.Parse("#93000A"), 0.3),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = isOnline ? Tertiary : Error,
                FontFamily = new FontFamily("Inter")
            }
        };
    }
}
