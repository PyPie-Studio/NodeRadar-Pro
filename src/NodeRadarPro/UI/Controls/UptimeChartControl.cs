using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NodeRadarPro.Core;

namespace NodeRadarPro.UI;

/// <summary>
/// Custom uptime bar chart that renders colored bars from UptimeSnapshot data.
/// Green = online, Red = offline. Bar height represents latency proportionally.
/// </summary>
public class UptimeChartControl : Control
{
    private List<UptimeSnapshot> _snapshots = new();

    private static readonly IBrush OnlineBrush = new SolidColorBrush(Color.Parse("#4CD7F6"), 0.8);
    private static readonly IBrush OfflineBrush = new SolidColorBrush(Color.Parse("#FFB4AB"), 0.7);
    private static readonly IBrush EmptyBrush = new SolidColorBrush(Color.Parse("#0C1322"), 0.3);
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.Parse("#1E293B"), 0.4), 1);
    private static readonly FormattedText _noDataText = new(
        "📈  Waiting for monitoring data...",
        System.Globalization.CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        new Typeface("Inter"),
        14,
        new SolidColorBrush(Color.Parse("#4C4452"), 0.6));

    public void SetData(List<UptimeSnapshot> snapshots)
    {
        _snapshots = snapshots ?? new();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;

        if (w < 10 || h < 10) return;

        // Background
        context.DrawRectangle(EmptyBrush, null, new Rect(0, 0, w, h), 6, 6);

        // Grid lines (3 horizontal)
        for (int i = 1; i <= 3; i++)
        {
            double y = Math.Round(h * i / 4);
            context.DrawLine(GridPen, new Point(0, y), new Point(w, y));
        }

        if (_snapshots.Count == 0)
        {
            context.DrawText(_noDataText, new Point(Math.Round((w - _noDataText.Width) / 2), Math.Round((h - _noDataText.Height) / 2)));
            return;
        }

        // Calculate bar dimensions with pixel snapping to prevent jitter
        int barCount = Math.Max(1, Math.Min(_snapshots.Count, (int)(w / 4))); // Minimum 4px per bar
        double barWidth = Math.Floor(w / barCount);
        double gap = Math.Max(1, Math.Floor(barWidth * 0.1));
        double startX = Math.Floor((w - (barCount * barWidth)) / 2);

        // Find max latency for scaling using index loop
        double maxLatency = 20.0;
        int startIndex = Math.Max(0, _snapshots.Count - barCount);
        for (int i = startIndex; i < _snapshots.Count; i++)
        {
            var lat = (double)_snapshots[i].LatencyMs;
            if (lat > maxLatency) maxLatency = lat;
        }

        // Draw bars (most recent on right)
        for (int i = 0; i < barCount && (startIndex + i) < _snapshots.Count; i++)
        {
            var snapshot = _snapshots[startIndex + i];
            double x = startX + i * barWidth;

            IBrush brush = snapshot.IsOnline ? OnlineBrush : OfflineBrush;

            double barHeight;
            if (snapshot.IsOnline && snapshot.LatencyMs > 0)
            {
                // Scale bar height 20%-100% based on latency (inverted: low latency = tall bar = good)
                double latencyRatio = 1.0 - Math.Min(1.0, snapshot.LatencyMs / maxLatency);
                barHeight = (0.2 + latencyRatio * 0.8) * h;
            }
            else if (snapshot.IsOnline)
            {
                barHeight = h * 0.9; // Online but no latency data
            }
            else
            {
                barHeight = h * 0.15; // Offline = short red bar
            }

            // Snap bar dimensions to pixels
            double snappedHeight = Math.Round(barHeight);
            double barX = x + Math.Floor(gap / 2);
            double barW = barWidth - gap;
            double barY = h - snappedHeight;

            context.DrawRectangle(brush, null, new Rect(barX, barY, barW, snappedHeight), 1, 1);
        }
    }
}
