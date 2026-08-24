using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Moq;
using Moq.Protected;
using NodeRadarPro.Core;
using NodeRadarPro.UI;
using NodeRadar_Pro;
using Xunit;

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
            catch
            {
                // Platform already initialized or fallback
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

        // 1 background + 4 snapshot bars = 5 DrawRectangleCore calls
        mockContext.Protected().Verify(
            "DrawRectangleCore",
            Times.Exactly(5),
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
