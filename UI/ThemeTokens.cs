using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

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
        Background = new SolidColorBrush(Color.Parse("#2E3545"), 0.6),
        CornerRadius = new CornerRadius(CardRadius),
        Padding = new Thickness(padding),
        BorderBrush = new SolidColorBrush(Color.Parse("#4C4452"), 0.15),
        BorderThickness = new Thickness(1),
        BoxShadow = new BoxShadows(new BoxShadow
        {
            OffsetX = 0, OffsetY = 20, Blur = 40,
            Color = Color.FromArgb(102, 0, 0, 0) // rgba(0,0,0,0.4)
        }),
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
