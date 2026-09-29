using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using NodeRadarPro.UI;

namespace NodeRadarPro.Tests.UI;

public class StatCardAlignmentTests
{
    private static bool _appInitialized;

    public StatCardAlignmentTests()
    {
        if (!_appInitialized)
        {
            try
            {
                AppBuilder.Configure<Avalonia.Application>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .SetupWithoutStarting();
            }
            catch (Exception ex)
            {
                _ = ex;
            }
            _appInitialized = true;
        }
    }

    [Fact]
    public void AlertsPage_MakeStatCard_SvgIconIsLeftAligned()
    {
        var method = typeof(AlertsPage).GetMethod("MakeStatCard", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var valTb = new TextBlock { Text = "42" };
        var card = method.Invoke(null, [ThemeTokens.SvgAlertTriangle, "Active Alerts", valTb, ThemeTokens.Error]) as Border;
        Assert.NotNull(card);

        var stack = card.Child as StackPanel;
        Assert.NotNull(stack);
        Assert.NotEmpty(stack.Children);

        var icon = stack.Children[0] as Avalonia.Controls.Shapes.Path;
        Assert.NotNull(icon);
        Assert.Equal(HorizontalAlignment.Left, icon.HorizontalAlignment);
    }

    [Fact]
    public void SystemLogsPage_MakeLogStatCard_SvgIconIsLeftAligned()
    {
        var method = typeof(SystemLogsPage).GetMethod("MakeLogStatCard", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var valTb = new TextBlock { Text = "100" };
        var card = method.Invoke(null, [ThemeTokens.SvgInfo, "Info", valTb, ThemeTokens.Tertiary]) as Border;
        Assert.NotNull(card);

        var stack = card.Child as StackPanel;
        Assert.NotNull(stack);
        Assert.NotEmpty(stack.Children);

        var icon = stack.Children[0] as Avalonia.Controls.Shapes.Path;
        Assert.NotNull(icon);
        Assert.Equal(HorizontalAlignment.Left, icon.HorizontalAlignment);
    }
}
