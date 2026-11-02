using Avalonia;
using Avalonia.Controls;

namespace NodeRadarPro.Tests.UI.Pages;

[Collection("AvaloniaUIStaticState")]
public class DashboardPageLayoutTests
{
    public DashboardPageLayoutTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void DashboardPage_MeasuredAndArranged_RadarCanvasHasNonZeroHeight()
    {
        var page = new DashboardPage(new List<NetworkNode>());
        page.Measure(new Size(1280, 800));
        page.Arrange(new Rect(0, 0, 1280, 800));

        var sv = page.Child as ScrollViewer;
        Assert.NotNull(sv);
        var border = sv.Content as Border;
        Assert.NotNull(border);
        var grid = border.Child as Grid;
        Assert.NotNull(grid);

        // If sv has no template applied in headless mode, measure/arrange border directly
        border.Measure(new Size(1280, 800));
        border.Arrange(new Rect(0, 0, 1280, 800));

        var radar = FindChild<RadarCanvas>(page);
        Assert.NotNull(radar);
        Assert.True(radar.Bounds.Height > 100, $"RadarCanvas height was {radar.Bounds.Height}, desired height was {radar.DesiredSize.Height}, grid height was {grid.Bounds.Height}");
    }

    [Fact]
    public void DashboardPage_MeasuredWithInfiniteHeight_RadarCanvasMaintainsMinHeight()
    {
        var page = new DashboardPage(new List<NetworkNode>());
        var sv = page.Child as ScrollViewer;
        Assert.NotNull(sv);
        var border = sv.Content as Border;
        Assert.NotNull(border);

        border.Measure(new Size(1280, double.PositiveInfinity));
        border.Arrange(new Rect(0, 0, 1280, border.DesiredSize.Height));

        var radar = FindChild<RadarCanvas>(page);
        Assert.NotNull(radar);
        Assert.True(radar.Bounds.Height >= 360, $"RadarCanvas height was {radar.Bounds.Height}, expected >= 360");
    }

    [Fact]
    public void RadarCanvas_DirectVisibilityChange_PausesAndResumesTimer()
    {
        var canvas = new RadarCanvas();
        Assert.True(canvas.IsTimerRunning);

        canvas.IsVisible = false;
        Assert.False(canvas.IsTimerRunning);

        canvas.IsVisible = true;
        Assert.True(canvas.IsTimerRunning);
    }

    [Fact]
    public void DashboardPage_IsVisibleFalse_PausesRadarAnimation()
    {
        var page = new DashboardPage(new List<NetworkNode>());
        var radar = FindChild<RadarCanvas>(page);
        Assert.NotNull(radar);
        Assert.True(radar.IsTimerRunning);

        page.IsVisible = false;
        Assert.False(radar.IsTimerRunning, "RadarCanvas timer should pause when DashboardPage is hidden");

        page.IsVisible = true;
        Assert.True(radar.IsTimerRunning, "RadarCanvas timer should resume when DashboardPage is shown");
    }

    private static T? FindChild<T>(Control parent) where T : Control
    {
        if (parent is T match) return match;

        if (parent is ContentControl contentControl && contentControl.Content is Control cc)
        {
            var result = FindChild<T>(cc);
            if (result != null) return result;
        }

        if (parent is Border border && border.Child is Control bc)
        {
            var result = FindChild<T>(bc);
            if (result != null) return result;
        }

        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                var result = FindChild<T>(child);
                if (result != null) return result;
            }
        }

        if (parent is ScrollViewer sv && sv.Content is Control sc)
        {
            var result = FindChild<T>(sc);
            if (result != null) return result;
        }

        return null;
    }
}
