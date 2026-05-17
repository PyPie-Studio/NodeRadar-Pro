using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using Avalonia.Input;
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
    public const string SvgShield = "M12,1L3,5V11C3,16.55 6.84,21.74 12,23C17.16,21.74 21,16.55 21,11V5L12,1Z";
    public const string SvgChart = "M2,13H8V21H2V13M9,3H15V21H9V3M16,8H22V21H16V8Z";

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

    public static void AddCopyAction(Control control, string? initialValue = null)
    {
        control.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        
        control.PointerPressed += async (s, e) =>
        {
            // Resolve live value from control if possible, otherwise use initialValue
            string? liveValue = initialValue;
            if (control is TextBlock tb) liveValue = tb.Text;
            else if (control is TextBox tbox) liveValue = tbox.Text;

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
        Background = PrimaryContainer,
        Foreground = OnSurface,
        FontSize = 15,
        FontWeight = FontWeight.SemiBold,
        FontFamily = new FontFamily("Inter"),
        Height = 44,
        CornerRadius = new CornerRadius(ButtonRadius),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        Padding = new Thickness(20, 0)
    };

    public static Button SecondaryButton(string text) => new()
    {
        Content = text,
        Background = Brushes.Transparent,
        Foreground = OnSurface,
        FontSize = 15,
        FontFamily = new FontFamily("Inter"),
        Height = 44,
        CornerRadius = new CornerRadius(ButtonRadius),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        BorderBrush = GhostBorder20,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(20, 0)
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
        Background = ErrorContainer,
        Foreground = Error,
        FontSize = 15,
        FontWeight = FontWeight.SemiBold,
        FontFamily = new FontFamily("Inter"),
        Height = 44,
        CornerRadius = new CornerRadius(ButtonRadius),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        Padding = new Thickness(20, 0)
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
