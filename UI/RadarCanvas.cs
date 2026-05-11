using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using NodeRadarPro.Core;

namespace NodeRadarPro.UI;

/// <summary>
/// Custom radar canvas with Kinetic Observatory color scheme.
/// Tertiary cyan sweep, online nodes glow cyan, offline glow soft red.
/// Supports mouse wheel zoom.
/// </summary>
public class RadarCanvas : Control
{
    private double _radarAngle = 0;
    private List<NetworkNode> _activeNodes = new();
    private string? _selectedMac = null;
    private double _pulsePhase = 0;
    private double _zoomLevel = 1.0;

    private readonly Avalonia.Threading.DispatcherTimer _radarTimer;

    public event Action<NetworkNode>? NodeRightClicked;
    public event Action<NetworkNode>? NodeSelected;

    // Kinetic Observatory palette
    private static readonly Color CyanGlow = Color.Parse("#4CD7F6");
    private static readonly Color CyanContainer = Color.Parse("#005362");
    private static readonly Color WarningGlow = Color.Parse("#FFCE50");
    private static readonly Color ErrorGlow = Color.Parse("#FFB4AB");
    private static readonly Color PrimaryPurple = Color.Parse("#DFB7FF");
    private static readonly Color OnPrimary = Color.Parse("#4A007F");
    private static readonly Color SweepCyan = Color.Parse("#4CD7F6");
    private static readonly Color SurfaceBg = Color.Parse("#070E1D");
    private static readonly Color RingColor = Color.Parse("#4CD7F6");

    public RadarCanvas()
    {
        _radarTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _radarTimer.Tick += (s, e) =>
        {
            _radarAngle += 1.2;
            if (_radarAngle >= 360) _radarAngle = 0;
            _pulsePhase += 0.04;
            if (_pulsePhase > Math.PI * 2) _pulsePhase -= Math.PI * 2;
            InvalidateVisual();
        };
        _radarTimer.Start();
    }

    public void UpdateNodes(List<NetworkNode> nodes)
    {
        _activeNodes = nodes;
        InvalidateVisual();
    }

    public void SelectNode(string? macAddress)
    {
        _selectedMac = macAddress;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double delta = e.Delta.Y;
        _zoomLevel = Math.Clamp(_zoomLevel + (delta * 0.1), 0.5, 3.0);
        InvalidateVisual();
    }

    private Point GetNodePosition(NetworkNode node, Point center, double maxRadius)
    {
        string[] ipParts = node.IpAddress.Split('.');
        if (ipParts.Length == 4 && int.TryParse(ipParts[3], out int lastOctet))
        {
            double nodeAngle = (lastOctet * 37.3) % 360;
            double nodeRad = nodeAngle * (Math.PI / 180.0);
            double distanceFactor = (lastOctet % 2 == 0) ? 0.4 : 0.8;
            double distance = ((maxRadius * 0.1) + ((lastOctet / 254.0) * (maxRadius * distanceFactor))) * _zoomLevel;

            return new Point(
                center.X + Math.Cos(nodeRad) * distance,
                center.Y + Math.Sin(nodeRad) * distance
            );
        }
        return center;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var clickPoint = e.GetPosition(this);
        var bounds = Bounds;
        var center = new Point(bounds.Width / 2, bounds.Height / 2);
        double baseRadius = Math.Min(bounds.Width, bounds.Height) / 2.2;
        bool isRightClick = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;

        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, baseRadius);
            double dx = clickPoint.X - nodePoint.X;
            double dy = clickPoint.Y - nodePoint.Y;
            // Scale hit detection radius with zoom
            double hitRadius = 20 * _zoomLevel;
            if ((dx * dx) + (dy * dy) <= (hitRadius * hitRadius))
            {
                if (isRightClick)
                    NodeRightClicked?.Invoke(node);
                else
                {
                    _selectedMac = node.MacAddress;
                    NodeSelected?.Invoke(node);
                }
                return;
            }
        }

        if (!isRightClick) _selectedMac = null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        // ── FIX: Apply clipping to prevent zoom overlap ──
        using var clip = context.PushClip(new Rect(0, 0, bounds.Width, bounds.Height));

        var topLevel = TopLevel.GetTopLevel(this);
        double scaling = topLevel?.RenderScaling ?? 1.0;

        var center = new Point(bounds.Width / 2, bounds.Height / 2);
        double baseRadius = Math.Min(bounds.Width, bounds.Height) / 2.2;
        double maxRadius = baseRadius * _zoomLevel;

        // Background circle — deep surface
        var bgBrush = new ImmutableSolidColorBrush(SurfaceBg, 0.7);
        context.DrawEllipse(bgBrush, null, center, maxRadius, maxRadius);

        // Inner shadow ring
        var innerShadowPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(15, 0, 0, 0)), 30 * _zoomLevel);
        context.DrawEllipse(null, innerShadowPen, center, Math.Max(1, maxRadius - (15 * _zoomLevel)), Math.Max(1, maxRadius - (15 * _zoomLevel)));

        // Concentric rings — tertiary cyan at increasing opacity
        byte[] ringAlphas = { 15, 25, 38, 50 };
        for (int i = 1; i <= 4; i++)
        {
            double ringRadius = maxRadius * (i / 4.0);
            byte alpha = ringAlphas[i - 1];
            var ringPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(alpha, RingColor.R, RingColor.G, RingColor.B)), 1);
            context.DrawEllipse(null, ringPen, center, ringRadius, ringRadius);
        }

        // Coordinate Labels — INCREASED SIZE
        var labelBrush = new ImmutableSolidColorBrush(new Color(100, RingColor.R, RingColor.G, RingColor.B));
        var labelTypeface = new Typeface("Inter", FontStyle.Normal, FontWeight.Bold);
        string[] labels = { "N", "E", "S", "W" };
        for (int i = 0; i < 4; i++)
        {
            double ang = (i * 90 - 90) * (Math.PI / 180.0);
            var labelText = new FormattedText(labels[i], System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, labelTypeface, 16 * _zoomLevel, labelBrush);
            double lx = center.X + Math.Cos(ang) * (maxRadius + (16 * _zoomLevel)) - (labelText.Width / 2);
            double ly = center.Y + Math.Sin(ang) * (maxRadius + (16 * _zoomLevel)) - (labelText.Height / 2);
            context.DrawText(labelText, new Point(lx, ly));
        }

        // Crosshairs — faint
        var crossPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(20, RingColor.R, RingColor.G, RingColor.B)), 0.5);
        context.DrawLine(crossPen, new Point(center.X, center.Y - maxRadius), new Point(center.X, center.Y + maxRadius));
        context.DrawLine(crossPen, new Point(center.X - maxRadius, center.Y), new Point(center.X + maxRadius, center.Y));

        // Scanning Wave — an expanding, fading ring
        double waveProgress = (_radarAngle / 360.0); // 0.0 to 1.0
        double waveRadius = maxRadius * waveProgress;
        byte waveAlpha = (byte)(40 * (1.0 - waveProgress));
        var wavePen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(waveAlpha, SweepCyan.R, SweepCyan.G, SweepCyan.B)), 2);
        context.DrawEllipse(null, wavePen, center, waveRadius, waveRadius);

        // Sweep line — tertiary cyan
        double radians = _radarAngle * (Math.PI / 180.0);
        Point endPoint = new Point(
            center.X + Math.Cos(radians) * maxRadius,
            center.Y + Math.Sin(radians) * maxRadius
        );

        var sweepPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(200, SweepCyan.R, SweepCyan.G, SweepCyan.B)), 2);
        context.DrawLine(sweepPen, center, endPoint);

        // Trailing sweep
        for (int t = 1; t <= 10; t++)
        {
            double trailAngle = (_radarAngle - (t * 3.5)) * (Math.PI / 180.0);
            Point trailEnd = new Point(
                center.X + Math.Cos(trailAngle) * maxRadius,
                center.Y + Math.Sin(trailAngle) * maxRadius
            );
            byte alpha = (byte)Math.Max(5, 100 - (t * 10));
            var trailPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(alpha, SweepCyan.R, SweepCyan.G, SweepCyan.B)), 1.2);
            context.DrawLine(trailPen, center, trailEnd);
        }

        // Center hub — primary purple with inner dot
        var hubGlow = new ImmutableSolidColorBrush(new Color(60, PrimaryPurple.R, PrimaryPurple.G, PrimaryPurple.B));
        context.DrawEllipse(hubGlow, null, center, 12 * _zoomLevel, 12 * _zoomLevel);
        var hubBrush = new ImmutableSolidColorBrush(PrimaryPurple);
        context.DrawEllipse(hubBrush, null, center, 5 * _zoomLevel, 5 * _zoomLevel);
        var hubInner = new ImmutableSolidColorBrush(OnPrimary);
        context.DrawEllipse(hubInner, null, center, 2 * _zoomLevel, 2 * _zoomLevel);

        // Network Nodes
        double pulseScale = 1.0 + (Math.Sin(_pulsePhase) * 0.25);

        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, baseRadius);
            if (nodePoint == center) continue;

            bool isSelected = node.MacAddress == _selectedMac;
            double nodeBaseRadius = (node.IsRegistered ? 7 : 5) * _zoomLevel;

            if (node.IsOnline)
            {
                // Outer glow
                double glowRadius = nodeBaseRadius + (5 * _zoomLevel) * pulseScale;

                var glowColor = node.ThreatLevel switch
                {
                    ThreatLevel.Critical => ErrorGlow,
                    ThreatLevel.Warning => WarningGlow,
                    _ => CyanGlow
                };

                var glowBrush = new ImmutableSolidColorBrush(new Color(40, glowColor.R, glowColor.G, glowColor.B));
                context.DrawEllipse(glowBrush, null, nodePoint, glowRadius, glowRadius);

                // Core dot
                var coreBrush = new ImmutableSolidColorBrush(glowColor);
                context.DrawEllipse(coreBrush, null, nodePoint, nodeBaseRadius, nodeBaseRadius);
            }
            else
            {
                double offRadius = nodeBaseRadius * 0.7;
                var offBrush = new ImmutableSolidColorBrush(new Color(150, ErrorGlow.R, ErrorGlow.G, ErrorGlow.B));
                context.DrawEllipse(offBrush, null, nodePoint, offRadius, offRadius);
            }

            // Registered ring
            if (node.IsRegistered)
            {
                var ringC = node.IsOnline ? CyanGlow : ErrorGlow;
                var ringPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(80, ringC.R, ringC.G, ringC.B)), 1.5 * _zoomLevel);
                context.DrawEllipse(null, ringPen, nodePoint, nodeBaseRadius + (4 * _zoomLevel), nodeBaseRadius + (4 * _zoomLevel));
            }

            // Selection highlight
            if (isSelected)
            {
                var selectPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.White), 1.5 * _zoomLevel);
                context.DrawEllipse(null, selectPen, nodePoint, nodeBaseRadius + (7 * _zoomLevel), nodeBaseRadius + (7 * _zoomLevel));
            }

            // Label
            var textColor = node.IsOnline
                ? new ImmutableSolidColorBrush(Color.Parse("#DCE2F7"))
                : new ImmutableSolidColorBrush(new Color(100, 220, 226, 247));

            var typeface = new Typeface("Inter", FontStyle.Normal, isSelected ? FontWeight.Bold : FontWeight.Normal);
            var formattedText = new FormattedText(
                node.DisplayName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface, 11 * _zoomLevel, textColor
            );

            double textYOffset = nodePoint.Y > center.Y ? -(20 * _zoomLevel) : (14 * _zoomLevel);
            context.DrawText(formattedText, new Point(nodePoint.X - (formattedText.Width / 2), nodePoint.Y + textYOffset));
        }

        // Zoom Level Indicator (Subtle overlay)
        if (_zoomLevel != 1.0)
        {
            var zoomText = new FormattedText(
                $"Zoom: {_zoomLevel:F1}x",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter"), 12, new ImmutableSolidColorBrush(RingColor, 0.4)
            );
            context.DrawText(zoomText, new Point(bounds.Width - zoomText.Width - 10, bounds.Height - zoomText.Height - 10));
        }
    }
}