using Avalonia;
using Avalonia.Media;
using Moq;
using Moq.Protected;
using NodeRadarPro.Core;
using NodeRadarPro.UI;
using NodeRadar_Pro;

namespace NodeRadarPro.Tests;

public class UptimeChartControlTests
{
    private static bool _appInitialized;

    public UptimeChartControlTests()
    {
        if (!_appInitialized)
        {
            try
            {
                AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .SetupWithoutStarting();
            }
            catch (Exception ex)
            {
                // Avalonia platform may already be initialized by preceding test fixtures
                _ = ex;
            }
            _appInitialized = true;
        }
    }

    [Fact]
    public void SetData_WithNull_InitializesEmptySnapshotsList()
    {
        var control = new UptimeChartControl();
        control.Measure(new Size(200, 100));
        control.Arrange(new Rect(0, 0, 200, 100));

        control.SetData(null!);

        var mockContext = new Mock<DrawingContext>();
        control.Render(mockContext.Object);

        // Verify background drawn
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Once(),
            ItExpr.IsAny<IBrush>(),
            ItExpr.IsAny<IPen>(),
            ItExpr.IsAny<RoundedRect>(),
            ItExpr.IsAny<BoxShadows>());

        // 3 grid lines
        mockContext.Protected().Verify(
            "DrawLineCore",
            Times.Exactly(3),
            ItExpr.IsAny<IPen>(),
            ItExpr.IsAny<Point>(),
            ItExpr.IsAny<Point>());

        // Empty state text
        mockContext.Verify(
            c => c.DrawText(It.IsNotNull<FormattedText>(), It.IsAny<Point>()),
            Times.Once());
    }

    [Fact]
    public void Render_WhenBoundsTooSmall_ReturnsEarlyWithoutDrawing()
    {
        var control = new UptimeChartControl();
        control.Measure(new Size(5, 5));
        control.Arrange(new Rect(0, 0, 5, 5));

        var mockContext = new Mock<DrawingContext>();
        control.Render(mockContext.Object);

        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Never(),
            ItExpr.IsAny<IBrush>(),
            ItExpr.IsAny<IPen>(),
            ItExpr.IsAny<RoundedRect>(),
            ItExpr.IsAny<BoxShadows>());

        mockContext.Protected().Verify(
            "DrawLineCore",
            Times.Never(),
            ItExpr.IsAny<IPen>(),
            ItExpr.IsAny<Point>(),
            ItExpr.IsAny<Point>());

        mockContext.Verify(
            c => c.DrawText(It.IsAny<FormattedText>(), It.IsAny<Point>()),
            Times.Never());
    }

    [Fact]
    public void Render_WhenSnapshotsEmpty_DrawsBackgroundGridAndNoDataText()
    {
        var control = new UptimeChartControl();
        control.Measure(new Size(200, 100));
        control.Arrange(new Rect(0, 0, 200, 100));
        control.SetData(new List<UptimeSnapshot>());

        var mockContext = new Mock<DrawingContext>();
        control.Render(mockContext.Object);

        // 1 background rectangle
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Once(),
            ItExpr.IsAny<IBrush>(),
            ItExpr.IsAny<IPen>(),
            ItExpr.IsAny<RoundedRect>(),
            ItExpr.IsAny<BoxShadows>());

        // 3 grid lines
        mockContext.Protected().Verify(
            "DrawLineCore",
            Times.Exactly(3),
            ItExpr.IsAny<IPen>(),
            ItExpr.IsAny<Point>(),
            ItExpr.IsAny<Point>());

        // Empty state text
        mockContext.Verify(
            c => c.DrawText(It.IsNotNull<FormattedText>(), It.IsAny<Point>()),
            Times.Once());
    }

    [Fact]
    public void Render_WithOnlineAndOfflineSnapshots_RendersProportionalBars()
    {
        var control = new UptimeChartControl();
        control.Measure(new Size(200, 100));
        control.Arrange(new Rect(0, 0, 200, 100));

        var snapshots = new List<UptimeSnapshot>
        {
            new UptimeSnapshot { Timestamp = DateTime.Now.AddMinutes(-15), IsOnline = true, LatencyMs = 15 },
            new UptimeSnapshot { Timestamp = DateTime.Now.AddMinutes(-10), IsOnline = false, LatencyMs = 0 },
            new UptimeSnapshot { Timestamp = DateTime.Now.AddMinutes(-5), IsOnline = true, LatencyMs = 0 },
            new UptimeSnapshot { Timestamp = DateTime.Now, IsOnline = true, LatencyMs = 80 }
        };

        control.SetData(snapshots);

        var mockContext = new Mock<DrawingContext>();
        control.Render(mockContext.Object);

        // Verify background (EmptyBrush: #0C1322)
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Once(),
            ItExpr.Is<IBrush>(b => b != null && ((ISolidColorBrush)b).Color == Color.Parse("#0C1322")),
            ItExpr.IsNull<IPen>(),
            ItExpr.Is<RoundedRect>(r => r.Rect.Width == 200 && r.Rect.Height == 100),
            ItExpr.IsAny<BoxShadows>());

        // Verify offline bar (OfflineBrush: #FFB4AB, height 15)
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Once(),
            ItExpr.Is<IBrush>(b => b != null && ((ISolidColorBrush)b).Color == Color.Parse("#FFB4AB")),
            ItExpr.IsNull<IPen>(),
            ItExpr.Is<RoundedRect>(r => r.Rect.Height == 15),
            ItExpr.IsAny<BoxShadows>());

        // Verify online low-latency bar (OnlineBrush: #4CD7F6, 15ms -> height 85)
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Once(),
            ItExpr.Is<IBrush>(b => b != null && ((ISolidColorBrush)b).Color == Color.Parse("#4CD7F6")),
            ItExpr.IsNull<IPen>(),
            ItExpr.Is<RoundedRect>(r => r.Rect.Height == 85),
            ItExpr.IsAny<BoxShadows>());

        // Verify online high-latency bar (OnlineBrush: #4CD7F6, 80ms -> height 20)
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Once(),
            ItExpr.Is<IBrush>(b => b != null && ((ISolidColorBrush)b).Color == Color.Parse("#4CD7F6")),
            ItExpr.IsNull<IPen>(),
            ItExpr.Is<RoundedRect>(r => r.Rect.Height == 20),
            ItExpr.IsAny<BoxShadows>());

        // Verify online no-latency bar (OnlineBrush: #4CD7F6, 0ms -> height 90)
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Once(),
            ItExpr.Is<IBrush>(b => b != null && ((ISolidColorBrush)b).Color == Color.Parse("#4CD7F6")),
            ItExpr.IsNull<IPen>(),
            ItExpr.Is<RoundedRect>(r => r.Rect.Height == 90),
            ItExpr.IsAny<BoxShadows>());

        // 3 grid lines
        mockContext.Protected().Verify(
            "DrawLineCore",
            Times.Exactly(3),
            ItExpr.IsAny<IPen>(),
            ItExpr.IsAny<Point>(),
            ItExpr.IsAny<Point>());
    }

    [Fact]
    public void Render_WithMoreSnapshotsThanBarLimit_OnlyRendersRecentSnapshots()
    {
        var control = new UptimeChartControl();
        // Width 40 allows (40 / 4) = 10 bars max
        control.Measure(new Size(40, 100));
        control.Arrange(new Rect(0, 0, 40, 100));

        var snapshots = new List<UptimeSnapshot>();
        for (int i = 0; i < 25; i++)
        {
            snapshots.Add(new UptimeSnapshot { Timestamp = DateTime.Now.AddMinutes(-25 + i), IsOnline = i % 2 == 0, LatencyMs = 20 });
        }

        control.SetData(snapshots);

        var mockContext = new Mock<DrawingContext>();
        control.Render(mockContext.Object);

        // 1 background + 10 snapshot bars = 11 DrawRectangleCore calls
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Exactly(11),
            ItExpr.IsAny<IBrush>(),
            ItExpr.IsAny<IPen>(),
            ItExpr.IsAny<RoundedRect>(),
            ItExpr.IsAny<BoxShadows>());
    }
}
