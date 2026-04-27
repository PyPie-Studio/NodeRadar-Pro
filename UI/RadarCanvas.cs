using System;
using System.Collections.Generic;
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
/// </summary>
public class RadarCanvas : Control
{
    private double _radarAngle = 0;
    private List<NetworkNode> _activeNodes = new();
    private string? _selectedMac = null;
    private double _pulsePhase = 0;

    private readonly Avalonia.Threading.DispatcherTimer _radarTimer;

    public event Action<NetworkNode>? NodeRightClicked;
    public event Action<NetworkNode>? NodeSelected;

    // Kinetic Observatory palette
    private static readonly Color CyanGlow = Color.Parse("#4CD7F6");
    private static readonly Color CyanContainer = Color.Parse("#005362");
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

    private static Point GetNodePosition(NetworkNode node, Point center, double maxRadius)
    {
        string[] ipParts = node.IpAddress.Split('.');
        if (ipParts.Length == 4 && int.TryParse(ipParts[3], out int lastOctet))
        {
            double nodeAngle = (lastOctet * 37.3) % 360;
            double nodeRad = nodeAngle * (Math.PI / 180.0);
            double distanceFactor = (lastOctet % 2 == 0) ? 0.4 : 0.8;
            double distance = (maxRadius * 0.1) + ((lastOctet / 254.0) * (maxRadius * distanceFactor));

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
        double maxRadius = Math.Min(bounds.Width, bounds.Height) / 2.2;
        bool isRightClick = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;

        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, maxRadius);
            double dx = clickPoint.X - nodePoint.X;
            double dy = clickPoint.Y - nodePoint.Y;
            if ((dx * dx) + (dy * dy) <= 400)
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

        // DPI Audit: Using logical pixels; Avalonia handles scaling automatically.
        // We capture scaling here for potential manual adjustments to small pens if needed.
        var topLevel = TopLevel.GetTopLevel(this);
        double scaling = topLevel?.RenderScaling ?? 1.0;

        var bounds = Bounds;
        var center = new Point(bounds.Width / 2, bounds.Height / 2);
        double maxRadius = Math.Min(bounds.Width, bounds.Height) / 2.2;

        // Background circle — deep surface
        var bgBrush = new ImmutableSolidColorBrush(SurfaceBg, 0.7);
        context.DrawEllipse(bgBrush, null, center, maxRadius, maxRadius);

        // Inner shadow ring
        var innerShadowPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(15, 0, 0, 0)), 30);
        context.DrawEllipse(null, innerShadowPen, center, maxRadius - 15, maxRadius - 15);

        // Concentric rings — tertiary cyan at increasing opacity
        byte[] ringAlphas = { 15, 25, 38, 50 };
        for (int i = 1; i <= 4; i++)
        {
            double ringRadius = maxRadius * (i / 4.0);
            byte alpha = ringAlphas[i - 1];
            var ringPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(alpha, RingColor.R, RingColor.G, RingColor.B)), 1);
            context.DrawEllipse(null, ringPen, center, ringRadius, ringRadius);
        }

        // Crosshairs — faint
        var crossPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(20, RingColor.R, RingColor.G, RingColor.B)), 0.5);
        context.DrawLine(crossPen, new Point(center.X, center.Y - maxRadius), new Point(center.X, center.Y + maxRadius));
        context.DrawLine(crossPen, new Point(center.X - maxRadius, center.Y), new Point(center.X + maxRadius, center.Y));

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

        // Network Nodes
        double pulseScale = 1.0 + (Math.Sin(_pulsePhase) * 0.25);

        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, maxRadius);
            if (nodePoint == center) continue;

            bool isSelected = node.MacAddress == _selectedMac;
            double baseRadius = node.IsRegistered ? 7 : 5;

            if (node.IsOnline)
            {
                // Outer glow
                double glowRadius = baseRadius + 5 * pulseScale;
                var glowBrush = new ImmutableSolidColorBrush(new Color(40, CyanGlow.R, CyanGlow.G, CyanGlow.B));
                context.DrawEllipse(glowBrush, null, nodePoint, glowRadius, glowRadius);

                // Core dot
                var coreBrush = new ImmutableSolidColorBrush(CyanGlow);
                context.DrawEllipse(coreBrush, null, nodePoint, baseRadius, baseRadius);
            }
            else
            {
                double offRadius = baseRadius * 0.7;
                var offBrush = new ImmutableSolidColorBrush(new Color(150, ErrorGlow.R, ErrorGlow.G, ErrorGlow.B));
                context.DrawEllipse(offBrush, null, nodePoint, offRadius, offRadius);
            }

            // Registered ring
            if (node.IsRegistered)
            {
                var ringC = node.IsOnline ? CyanGlow : ErrorGlow;
                var ringPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(80, ringC.R, ringC.G, ringC.B)), 1.5);
                context.DrawEllipse(null, ringPen, nodePoint, baseRadius + 4, baseRadius + 4);
            }

            // Selection highlight
            if (isSelected)
            {
                var selectPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.White), 1.5);
                context.DrawEllipse(null, selectPen, nodePoint, baseRadius + 7, baseRadius + 7);
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
                typeface, 11, textColor
            );

            double textYOffset = nodePoint.Y > center.Y ? -20 : 14;
            context.DrawText(formattedText, new Point(nodePoint.X - (formattedText.Width / 2), nodePoint.Y + textYOffset));
        }
    }
}