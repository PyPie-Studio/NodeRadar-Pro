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
/// A custom Avalonia control that physically draws a sweeping radar 
/// and maps discovered IPs as glowing dots on the screen.
/// Online = neon green, Offline = dim red, Registered = ring outline.
/// </summary>
public class RadarCanvas : Control
{
    private double _radarAngle = 0;
    private List<NetworkNode> _activeNodes = new();
    private string? _selectedMac = null;
    private double _pulsePhase = 0;
    
    // We use a timer to constantly invalidate the visual and redraw the sweeping line
    private readonly Avalonia.Threading.DispatcherTimer _radarTimer;

    // Fired when a user right-clicks a node on the canvas
    public event Action<NetworkNode>? NodeRightClicked;
    
    // Fired when a user left-clicks a node on the canvas
    public event Action<NetworkNode>? NodeSelected;

    public RadarCanvas()
    {
        // Smooth 60 FPS radar rotation
        _radarTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) 
        };
        _radarTimer.Tick += (s, e) =>
        {
            _radarAngle += 1.5;
            if (_radarAngle >= 360) _radarAngle = 0;
            _pulsePhase += 0.05;
            if (_pulsePhase > Math.PI * 2) _pulsePhase -= Math.PI * 2;
            InvalidateVisual(); // Trigger OnPaint
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

    /// <summary>
    /// Shared positioning logic used by both Render and hit-testing.
    /// Ensures clicks always match where dots are drawn.
    /// </summary>
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

        // Check if the user clicked near any drawn node
        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, maxRadius);
            
            double dx = clickPoint.X - nodePoint.X;
            double dy = clickPoint.Y - nodePoint.Y;
            if ((dx * dx) + (dy * dy) <= 400) // 20^2 pixel hit box
            {
                if (isRightClick)
                {
                    NodeRightClicked?.Invoke(node);
                }
                else
                {
                    _selectedMac = node.MacAddress;
                    NodeSelected?.Invoke(node);
                }
                return;
            }
        }

        // Clicked empty space — deselect
        if (!isRightClick)
        {
            _selectedMac = null;
        }
    }

    /// <summary>
    /// Equivalent to WinForms OnPaint. Everything here is GPU accelerated.
    /// </summary>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var center = new Point(bounds.Width / 2, bounds.Height / 2);
        double maxRadius = Math.Min(bounds.Width, bounds.Height) / 2.2;

        // ── Background Circle Fill ──
        var bgBrush = new ImmutableSolidColorBrush(Color.Parse("#0D0D1A"), 0.6);
        context.DrawEllipse(bgBrush, null, center, maxRadius, maxRadius);

        // ── Radar Grid Rings ──
        var gridPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#308A2BE2")), 1);
        
        for (int i = 1; i <= 4; i++)
        {
            double ringRadius = maxRadius * (i / 4.0);
            context.DrawEllipse(null, gridPen, center, ringRadius, ringRadius);
        }

        // ── Crosshairs ──
        context.DrawLine(gridPen, new Point(center.X, center.Y - maxRadius), new Point(center.X, center.Y + maxRadius));
        context.DrawLine(gridPen, new Point(center.X - maxRadius, center.Y), new Point(center.X + maxRadius, center.Y));

        // ── Radar Sweep Gradient (fading trail) ──
        double radians = _radarAngle * (Math.PI / 180.0);
        Point endPoint = new Point(
            center.X + Math.Cos(radians) * maxRadius,
            center.Y + Math.Sin(radians) * maxRadius
        );

        // Main sweep line
        var sweepPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#CC8A2BE2")), 2.5);
        context.DrawLine(sweepPen, center, endPoint);

        // Trailing sweep lines for a fading trail effect
        for (int t = 1; t <= 8; t++)
        {
            double trailAngle = (_radarAngle - (t * 4)) * (Math.PI / 180.0);
            Point trailEnd = new Point(
                center.X + Math.Cos(trailAngle) * maxRadius,
                center.Y + Math.Sin(trailAngle) * maxRadius
            );
            byte alpha = (byte)Math.Max(10, 120 - (t * 15));
            var trailPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(alpha, 138, 43, 226)), 1.5);
            context.DrawLine(trailPen, center, trailEnd);
        }

        // ── Center Dot ──
        var centerDotBrush = new ImmutableSolidColorBrush(Color.Parse("#8A2BE2"));
        context.DrawEllipse(centerDotBrush, null, center, 4, 4);

        // ── Draw Discovered Network Nodes ──
        double pulseScale = 1.0 + (Math.Sin(_pulsePhase) * 0.3); // 0.7 to 1.3 pulsing

        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, maxRadius);
            if (nodePoint == center) continue; // Skip unparseable IPs

            bool isSelected = node.MacAddress == _selectedMac;
            double baseRadius = node.IsRegistered ? 8 : 6;
            
            if (node.IsOnline)
            {
                // ── Online: Neon green with pulsing glow ──
                double glowRadius = baseRadius + 4 * pulseScale;
                var glowBrush = new ImmutableSolidColorBrush(new Color(60, 0, 255, 204));
                context.DrawEllipse(glowBrush, null, nodePoint, glowRadius, glowRadius);
                
                var onlineBrush = new ImmutableSolidColorBrush(Color.Parse("#00FFcc"));
                context.DrawEllipse(onlineBrush, null, nodePoint, baseRadius, baseRadius);
            }
            else
            {
                // ── Offline: Dim red, smaller ──
                double offlineRadius = baseRadius * 0.75;
                var offlineBrush = new ImmutableSolidColorBrush(new Color(180, 255, 68, 68));
                context.DrawEllipse(offlineBrush, null, nodePoint, offlineRadius, offlineRadius);
            }

            // ── Registered device ring ──
            if (node.IsRegistered)
            {
                var ringColor = node.IsOnline ? Color.Parse("#00FFcc") : Color.Parse("#FF4444");
                var ringPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(100, ringColor.R, ringColor.G, ringColor.B)), 1.5);
                context.DrawEllipse(null, ringPen, nodePoint, baseRadius + 4, baseRadius + 4);
            }
            
            // ── Selection highlight ──
            if (isSelected)
            {
                var selectPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#FFFFFF")), 2);
                context.DrawEllipse(null, selectPen, nodePoint, baseRadius + 7, baseRadius + 7);
            }

            // ── Label: Status dot + DisplayName ──
            var textColor = node.IsOnline 
                ? new ImmutableSolidColorBrush(Colors.WhiteSmoke) 
                : new ImmutableSolidColorBrush(new Color(140, 255, 255, 255));
            
            string label = node.DisplayName;
            var typeface = new Typeface("Inter", FontStyle.Normal, isSelected ? FontWeight.Bold : FontWeight.Normal);
            var formattedText = new FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                textColor
            );
            
            double textYOffset = nodePoint.Y > center.Y ? -22 : 14;
            context.DrawText(formattedText, new Point(nodePoint.X - (formattedText.Width / 2), nodePoint.Y + textYOffset));
        }
    }
}