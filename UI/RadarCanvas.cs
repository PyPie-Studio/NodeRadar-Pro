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
    private Vector _panOffset = new Vector(0, 0);
    private Point _lastPointerPoint;
    private bool _isDragging = false;

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
        _zoomLevel = Math.Clamp(_zoomLevel + (delta * 0.1), 0.5, 5.0);
        
        // Reset pan if zoomed out to 1.0 or less
        if (_zoomLevel <= 1.0) _panOffset = new Vector(0, 0);
        
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
                center.X + Math.Cos(nodeRad) * distance + _panOffset.X,
                center.Y + Math.Sin(nodeRad) * distance + _panOffset.Y
            );
        }
        return new Point(center.X + _panOffset.X, center.Y + _panOffset.Y);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _lastPointerPoint = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;

        if (props.IsLeftButtonPressed)
        {
            _isDragging = true;
            e.Pointer.Capture(this);
        }

        var bounds = Bounds;
        var center = new Point(bounds.Width / 2, bounds.Height / 2);
        double baseRadius = Math.Min(bounds.Width, bounds.Height) / 2.2;
        bool isRightClick = props.IsRightButtonPressed;

        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, baseRadius);
            
            // Verify node is within the visual radar boundary (clipped in Render)
            double distToCenter = Math.Sqrt(Math.Pow(nodePoint.X - center.X, 2) + Math.Pow(nodePoint.Y - center.Y, 2));
            if (distToCenter > baseRadius) continue;

            double dx = _lastPointerPoint.X - nodePoint.X;
            double dy = _lastPointerPoint.Y - nodePoint.Y;
            
            // Hit radius shrinks with node scale (Inverse Zoom)
            double nodeScale = 1.0 / Math.Sqrt(_zoomLevel);
            double hitRadius = 15 * nodeScale;

            if ((dx * dx) + (dy * dy) <= (hitRadius * hitRadius))
            {
                _isDragging = false; // Selection takes precedence over panning
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

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging && _zoomLevel > 1.0)
        {
            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _lastPointerPoint;
            _panOffset += delta;
            _lastPointerPoint = currentPoint;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
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
        double maxRadius = baseRadius; // Fixed size for the radar circle

        // Background circle — deep surface
        var bgBrush = new ImmutableSolidColorBrush(SurfaceBg, 0.7);
        context.DrawEllipse(bgBrush, null, center, maxRadius, maxRadius);

        // Inner shadow ring
        var innerShadowPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(15, 0, 0, 0)), 30);
        context.DrawEllipse(null, innerShadowPen, center, Math.Max(1, maxRadius - 15), Math.Max(1, maxRadius - 15));

        // Concentric rings — tertiary cyan at increasing opacity
        byte[] ringAlphas = { 15, 25, 38, 50 };
        for (int i = 1; i <= 4; i++)
        {
            double ringRadius = maxRadius * (i / 4.0);
            byte alpha = ringAlphas[i - 1];
            var ringPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(alpha, RingColor.R, RingColor.G, RingColor.B)), 1);
            context.DrawEllipse(null, ringPen, center, ringRadius, ringRadius);
        }

        // Coordinate Labels
        var labelBrush = new ImmutableSolidColorBrush(new Color(100, RingColor.R, RingColor.G, RingColor.B));
        var labelTypeface = new Typeface("Inter", FontStyle.Normal, FontWeight.Bold);
        string[] labels = { "N", "E", "S", "W" };
        for (int i = 0; i < 4; i++)
        {
            double ang = (i * 90 - 90) * (Math.PI / 180.0);
            var labelText = new FormattedText(labels[i], System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, labelTypeface, 16, labelBrush);
            double lx = center.X + Math.Cos(ang) * (maxRadius + 16) - (labelText.Width / 2);
            double ly = center.Y + Math.Sin(ang) * (maxRadius + 16) - (labelText.Height / 2);
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
        context.DrawEllipse(hubGlow, null, center, 12, 12);
        var hubBrush = new ImmutableSolidColorBrush(PrimaryPurple);
        context.DrawEllipse(hubBrush, null, center, 5, 5);
        var hubInner = new ImmutableSolidColorBrush(OnPrimary);
        context.DrawEllipse(hubInner, null, center, 2, 2);

        // ── PUSH CLIPPING FOR NODES ──
        // Only draw nodes if they are within the radar circle
        using var nodeClip = context.PushClip(new RoundedRect(new Rect(center.X - maxRadius, center.Y - maxRadius, maxRadius * 2, maxRadius * 2), maxRadius));

        // Network Nodes
        double pulseScale = 1.0 + (Math.Sin(_pulsePhase) * 0.25);
        
        // Visibility Scaling: Make elements slightly larger as we zoom in for better clarity
        // spacing out is already handled by the position calculation
        double nodeScale = Math.Max(1.0, 1.0 + (_zoomLevel - 1.0) * 0.15);
        double textScale = Math.Max(1.0, 1.0 + (_zoomLevel - 1.0) * 0.2);

        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, baseRadius);
            if (nodePoint == (center + _panOffset)) continue;

            bool isSelected = node.MacAddress == _selectedMac;
            double nodeBaseRadius = (node.IsRegistered ? 7 : 5) * nodeScale;

            if (node.IsOnline)
            {
                // Outer glow
                double glowRadius = nodeBaseRadius + (5 * nodeScale) * pulseScale;

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
                var ringPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(80, ringC.R, ringC.G, ringC.B)), 1.5 * nodeScale);
                context.DrawEllipse(null, ringPen, nodePoint, nodeBaseRadius + (4 * nodeScale), nodeBaseRadius + (4 * nodeScale));
            }

            // Selection highlight
            if (isSelected)
            {
                var selectPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.White), 1.5 * nodeScale);
                context.DrawEllipse(null, selectPen, nodePoint, nodeBaseRadius + (7 * nodeScale), nodeBaseRadius + (7 * nodeScale));
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
                typeface, 11 * textScale, textColor
            );

            double textYOffset = nodePoint.Y > center.Y ? -(24 * nodeScale) : (18 * nodeScale);
            context.DrawText(formattedText, new Point(nodePoint.X - (formattedText.Width / 2), nodePoint.Y + textYOffset));
        }

        // Pop the nodeClip
        // context automatically pops 'using' objects when they go out of scope.

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