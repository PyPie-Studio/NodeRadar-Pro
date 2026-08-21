using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace NodeRadarPro.UI;

/// <summary>
/// Fixed 56px top navigation bar with branding, network health status, and action icons.
/// </summary>
public class TopNavBar : Border
{
    private readonly TextBlock _healthText;
    private readonly TextBlock _latencyText;

    public TopNavBar()
    {
        Height = ThemeTokens.TopBarHeight;
        Background = new SolidColorBrush(Color.Parse("#0C1322"), 0.9);
        BoxShadow = new BoxShadows(new BoxShadow
        {
            OffsetY = 1,
            Blur = 4,
            Color = Color.FromArgb(25, 128, 0, 128)
        });

        // Left: Title
        var title = new TextBlock
        {
            Text = "NODE RADAR PRO",
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = SolidColorBrush.Parse("#A855F7"),
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Inter"),
            LetterSpacing = 3
        };

        // Center: Status
        var healthDot = ThemeTokens.StatusDot(true, 7);
        healthDot.VerticalAlignment = VerticalAlignment.Center;
        healthDot.Margin = new Thickness(0, 0, 6, 0);

        _healthText = new TextBlock
        {
            Text = "Network Health: Stable",
            FontSize = 12,
            Foreground = ThemeTokens.Tertiary,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Inter")
        };
        ThemeTokens.SetToolTip(_healthText, "Overall network stability based on active device heartbeat and alert count.");

        var separator = new Border
        {
            Width = 4,
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = ThemeTokens.SlateText,
            Margin = new Thickness(14, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        _latencyText = new TextBlock
        {
            Text = "Latency: —",
            FontSize = 12,
            Foreground = ThemeTokens.SlateText,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Inter")
        };
        ThemeTokens.SetToolTip(_latencyText, "Average round-trip time (RTT) across all monitored network nodes.");

        var statusGroup = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { healthDot, _healthText, separator, _latencyText }
        };

        // Right: Version badge
        var versionBadge = new TextBlock
        {
            Text = "v1.0.0",
            FontSize = 11,
            Foreground = ThemeTokens.SlateText,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Inter"),
            Opacity = 0.7
        };
        ThemeTokens.SetToolTip(versionBadge, "Production Build v1.0.0 - PyPie Studio");
        var rightGroup = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { versionBadge }
        };

        // Assembly
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        Grid.SetColumn(title, 0);
        Grid.SetColumn(statusGroup, 1);
        Grid.SetColumn(rightGroup, 2);

        statusGroup.HorizontalAlignment = HorizontalAlignment.Center;

        grid.Margin = new Thickness(24, 0);
        grid.Children.Add(title);
        grid.Children.Add(statusGroup);
        grid.Children.Add(rightGroup);

        Child = grid;
    }

    public void UpdateStatus(int onlineCount, long avgLatency, int alertCount = 0)
    {
        if (alertCount > 0)
        {
            _healthText.Text = $"Network Health: {alertCount} Alert(s)";
            _healthText.Foreground = ThemeTokens.Error;
        }
        else if (onlineCount > 0)
        {
            _healthText.Text = "Network Health: Stable";
            _healthText.Foreground = ThemeTokens.Tertiary;
        }
        else
        {
            _healthText.Text = "Network Health: No Devices";
            _healthText.Foreground = ThemeTokens.SlateText;
        }
        _latencyText.Text = avgLatency >= 0 ? $"Latency: {avgLatency}ms" : "Latency: —";
    }


}
