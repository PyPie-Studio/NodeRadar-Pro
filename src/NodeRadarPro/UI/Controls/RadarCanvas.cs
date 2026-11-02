using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace NodeRadarPro.UI.Controls;

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
    public bool IsTimerRunning => _radarTimer.IsEnabled;
    public void PauseAnimation() => _radarTimer.Stop();
    public void ResumeAnimation() => _radarTimer.Start();

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

    // Cached brushes and pens for zero-allocation 60 FPS rendering
    private static readonly IBrush _bgBrush = new ImmutableSolidColorBrush(SurfaceBg, 0.7);
    private static readonly IPen _innerShadowPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(15, 0, 0, 0)), 30);
    private static readonly IPen[] _ringPens = new IPen[]
    {
        new ImmutablePen(new ImmutableSolidColorBrush(new Color(15, RingColor.R, RingColor.G, RingColor.B)), 1),
        new ImmutablePen(new ImmutableSolidColorBrush(new Color(25, RingColor.R, RingColor.G, RingColor.B)), 1),
        new ImmutablePen(new ImmutableSolidColorBrush(new Color(38, RingColor.R, RingColor.G, RingColor.B)), 1),
        new ImmutablePen(new ImmutableSolidColorBrush(new Color(50, RingColor.R, RingColor.G, RingColor.B)), 1)
    };
    private static readonly IPen _crossPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(20, RingColor.R, RingColor.G, RingColor.B)), 0.5);
    private static readonly IPen _sweepPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(200, SweepCyan.R, SweepCyan.G, SweepCyan.B)), 2);
    private static readonly IPen[] _trailPens = InitTrailPens();
    private static readonly IPen[] _wavePens = InitWavePens();

    private readonly struct NodeTextKey : IEquatable<NodeTextKey>
    {
        public readonly string MacAddress;
        public readonly string DisplayName;
        public readonly bool IsOnline;
        public readonly bool IsSelected;
        public readonly int TextScaleInt;

        public NodeTextKey(string macAddress, string displayName, bool isOnline, bool isSelected, int textScaleInt)
        {
            MacAddress = macAddress;
            DisplayName = displayName;
            IsOnline = isOnline;
            IsSelected = isSelected;
            TextScaleInt = textScaleInt;
        }

        public bool Equals(NodeTextKey other) =>
            IsOnline == other.IsOnline &&
            IsSelected == other.IsSelected &&
            TextScaleInt == other.TextScaleInt &&
            string.Equals(MacAddress, other.MacAddress, StringComparison.Ordinal) &&
            string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is NodeTextKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(MacAddress, StringComparer.Ordinal);
            hash.Add(DisplayName, StringComparer.Ordinal);
            hash.Add(IsOnline);
            hash.Add(IsSelected);
            hash.Add(TextScaleInt);
            return hash.ToHashCode();
        }
    }

    private readonly Dictionary<NodeTextKey, FormattedText> _textCache = new();
    private FormattedText? _cachedZoomText;
    private double _cachedZoomLevel = -1;

    private static readonly IBrush _hubGlow = new ImmutableSolidColorBrush(new Color(60, PrimaryPurple.R, PrimaryPurple.G, PrimaryPurple.B));
    private static readonly IBrush _hubBrush = new ImmutableSolidColorBrush(PrimaryPurple);
    private static readonly IBrush _hubInner = new ImmutableSolidColorBrush(OnPrimary);

    private static readonly IBrush _cyanGlowBrush = new ImmutableSolidColorBrush(new Color(40, CyanGlow.R, CyanGlow.G, CyanGlow.B));
    private static readonly IBrush _warningGlowBrush = new ImmutableSolidColorBrush(new Color(40, WarningGlow.R, WarningGlow.G, WarningGlow.B));
    private static readonly IBrush _errorGlowBrush = new ImmutableSolidColorBrush(new Color(40, ErrorGlow.R, ErrorGlow.G, ErrorGlow.B));
    private static readonly IBrush _cyanCoreBrush = new ImmutableSolidColorBrush(CyanGlow);
    private static readonly IBrush _warningCoreBrush = new ImmutableSolidColorBrush(WarningGlow);
    private static readonly IBrush _errorCoreBrush = new ImmutableSolidColorBrush(ErrorGlow);
    private static readonly IBrush _offBrush = new ImmutableSolidColorBrush(new Color(150, ErrorGlow.R, ErrorGlow.G, ErrorGlow.B));

    private static readonly IPen _registeredOnlinePen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(80, CyanGlow.R, CyanGlow.G, CyanGlow.B)), 1.5);
    private static readonly IPen _registeredOfflinePen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(80, ErrorGlow.R, ErrorGlow.G, ErrorGlow.B)), 1.5);
    private static readonly IPen _selectPen = new ImmutablePen(new ImmutableSolidColorBrush(Colors.White), 1.5);

    private static readonly IBrush _onlineTextColor = new ImmutableSolidColorBrush(Color.Parse("#DCE2F7"));
    private static readonly IBrush _offlineTextColor = new ImmutableSolidColorBrush(new Color(100, 220, 226, 247));
    private static readonly IBrush _zoomTextColor = new ImmutableSolidColorBrush(RingColor, 0.4);

    private static readonly Typeface _typefaceNormal = new(ThemeTokens.DefaultFont, FontStyle.Normal, FontWeight.Normal);
    private static readonly Typeface _typefaceBold = new(ThemeTokens.DefaultFont, FontStyle.Normal, FontWeight.Bold);

    private static readonly FormattedText[] _coordinateLabels = InitCoordinateLabels();

    private static IPen[] InitTrailPens()
    {
        var pens = new IPen[10];
        for (int t = 1; t <= 10; t++)
        {
            byte alpha = (byte)Math.Max(5, 100 - (t * 10));
            pens[t - 1] = new ImmutablePen(new ImmutableSolidColorBrush(new Color(alpha, SweepCyan.R, SweepCyan.G, SweepCyan.B)), 1.2);
        }
        return pens;
    }

    private static IPen[] InitWavePens()
    {
        var pens = new IPen[41];
        for (int a = 0; a <= 40; a++)
        {
            pens[a] = new ImmutablePen(new ImmutableSolidColorBrush(new Color((byte)a, SweepCyan.R, SweepCyan.G, SweepCyan.B)), 2);
        }
        return pens;
    }

    private static FormattedText[] InitCoordinateLabels()
    {
        var labelBrush = new ImmutableSolidColorBrush(new Color(100, RingColor.R, RingColor.G, RingColor.B));
        var labelTypeface = new Typeface(ThemeTokens.DefaultFont, FontStyle.Normal, FontWeight.Bold);
        string[] labels = { "N", "E", "S", "W" };
        var list = new FormattedText[4];
        for (int i = 0; i < 4; i++)
        {
            list[i] = new FormattedText(labels[i], System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, labelTypeface, 16, labelBrush);
        }
        return list;
    }

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
        ClipToBounds = true;
    }

    public void UpdateNodes(List<NetworkNode> nodes)
    {
        _activeNodes = nodes;
        _textCache.Clear();
        InvalidateVisual();
    }

    public void SelectNode(string? macAddress)
    {
        _selectedMac = macAddress;
        _textCache.Clear();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width;
        double h = double.IsInfinity(availableSize.Height) ? 360 : availableSize.Height;
        return new Size(Math.Max(100, w), Math.Max(100, h));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (IsVisible && !_radarTimer.IsEnabled)
        {
            _radarTimer.Start();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_radarTimer.IsEnabled)
        {
            _radarTimer.Stop();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty)
        {
            if (change.GetNewValue<bool>())
            {
                _radarTimer.Start();
            }
            else
            {
                _radarTimer.Stop();
            }
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double delta = e.Delta.Y;
        _zoomLevel = Math.Clamp(_zoomLevel + (delta * 0.1), 0.5, 5.0);

        // Reset pan if zoomed out to 1.0 or less
        if (_zoomLevel <= 1.0) _panOffset = new Vector(0, 0);

        _textCache.Clear();
        InvalidateVisual();
    }

    private static int ParseLastOctet(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return 0;
        int lastDot = ip.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < ip.Length - 1 && int.TryParse(ip.AsSpan(lastDot + 1), out int octet))
        {
            return octet;
        }
        return 0;
    }

    private Point GetNodePosition(NetworkNode node, Point center, double maxRadius)
    {
        int lastOctet = ParseLastOctet(node.IpAddress);
        if (lastOctet > 0)
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

        using var clip = context.PushClip(new Rect(0, 0, bounds.Width, bounds.Height));

        var center = new Point(bounds.Width / 2, bounds.Height / 2);
        double maxRadius = Math.Min(bounds.Width, bounds.Height) / 2.2;

        RenderBackgroundAndGrid(context, center, maxRadius);
        RenderRadarSweep(context, center, maxRadius);
        RenderNetworkNodes(context, center, maxRadius);
        RenderZoomOverlay(context, bounds);
    }

    private static void RenderBackgroundAndGrid(DrawingContext context, Point center, double maxRadius)
    {
        // Background circle — deep surface
        context.DrawEllipse(_bgBrush, null, center, maxRadius, maxRadius);

        // Inner shadow ring
        context.DrawEllipse(null, _innerShadowPen, center, Math.Max(1, maxRadius - 15), Math.Max(1, maxRadius - 15));

        // Concentric rings — tertiary cyan at increasing opacity
        for (int i = 1; i <= 4; i++)
        {
            double ringRadius = maxRadius * (i / 4.0);
            context.DrawEllipse(null, _ringPens[i - 1], center, ringRadius, ringRadius);
        }

        // Coordinate Labels
        for (int i = 0; i < 4; i++)
        {
            double ang = (i * 90 - 90) * (Math.PI / 180.0);
            var labelText = _coordinateLabels[i];
            double lx = center.X + Math.Cos(ang) * (maxRadius + 16) - (labelText.Width / 2);
            double ly = center.Y + Math.Sin(ang) * (maxRadius + 16) - (labelText.Height / 2);
            context.DrawText(labelText, new Point(lx, ly));
        }

        // Crosshairs — faint
        context.DrawLine(_crossPen, new Point(center.X, center.Y - maxRadius), new Point(center.X, center.Y + maxRadius));
        context.DrawLine(_crossPen, new Point(center.X - maxRadius, center.Y), new Point(center.X + maxRadius, center.Y));
    }

    private void RenderRadarSweep(DrawingContext context, Point center, double maxRadius)
    {
        // Scanning Wave — an expanding, fading ring
        double waveProgress = (_radarAngle / 360.0); // 0.0 to 1.0
        double waveRadius = maxRadius * waveProgress;
        int waveAlpha = Math.Clamp((int)(40 * (1.0 - waveProgress)), 0, 40);
        context.DrawEllipse(null, _wavePens[waveAlpha], center, waveRadius, waveRadius);

        // Sweep line — tertiary cyan
        double radians = _radarAngle * (Math.PI / 180.0);
        Point endPoint = new Point(
            center.X + Math.Cos(radians) * maxRadius,
            center.Y + Math.Sin(radians) * maxRadius
        );

        context.DrawLine(_sweepPen, center, endPoint);

        // Trailing sweep
        for (int t = 1; t <= 10; t++)
        {
            double trailAngle = (_radarAngle - (t * 3.5)) * (Math.PI / 180.0);
            Point trailEnd = new Point(
                center.X + Math.Cos(trailAngle) * maxRadius,
                center.Y + Math.Sin(trailAngle) * maxRadius
            );
            context.DrawLine(_trailPens[t - 1], center, trailEnd);
        }

        // Center hub — primary purple with inner dot
        context.DrawEllipse(_hubGlow, null, center, 12, 12);
        context.DrawEllipse(_hubBrush, null, center, 5, 5);
        context.DrawEllipse(_hubInner, null, center, 2, 2);
    }

    private void RenderNetworkNodes(DrawingContext context, Point center, double baseRadius)
    {
        // ── PUSH CLIPPING FOR NODES ──
        // Only draw nodes if they are within the radar circle
        using var nodeClip = context.PushClip(new RoundedRect(new Rect(center.X - baseRadius, center.Y - baseRadius, baseRadius * 2, baseRadius * 2), baseRadius));

        // Network Nodes
        double pulseScale = 1.0 + (Math.Sin(_pulsePhase) * 0.25);

        // Visibility Scaling: Make elements slightly larger as we zoom in for better clarity
        double nodeScale = Math.Max(1.0, 1.0 + (_zoomLevel - 1.0) * 0.15);
        double textScale = Math.Max(1.0, 1.0 + (_zoomLevel - 1.0) * 0.2);

        foreach (var node in _activeNodes)
        {
            Point nodePoint = GetNodePosition(node, center, baseRadius);
            if (nodePoint == (center + _panOffset)) continue;

            DrawSingleNode(context, node, center, nodePoint, nodeScale, textScale, pulseScale);
        }
    }

    private void DrawSingleNode(DrawingContext context, NetworkNode node, Point center, Point nodePoint, double nodeScale, double textScale, double pulseScale)
    {
        bool isSelected = node.MacAddress == _selectedMac;
        double nodeBaseRadius = (node.IsRegistered ? 7 : 5) * nodeScale;

        if (node.IsOnline)
        {
            // Outer glow
            double glowRadius = nodeBaseRadius + (5 * nodeScale) * pulseScale;

            var glowBrush = node.ThreatLevel switch
            {
                ThreatLevel.Critical => _errorGlowBrush,
                ThreatLevel.Warning => _warningGlowBrush,
                _ => _cyanGlowBrush
            };

            context.DrawEllipse(glowBrush, null, nodePoint, glowRadius, glowRadius);

            // Core dot
            var coreBrush = node.ThreatLevel switch
            {
                ThreatLevel.Critical => _errorCoreBrush,
                ThreatLevel.Warning => _warningCoreBrush,
                _ => _cyanCoreBrush
            };
            context.DrawEllipse(coreBrush, null, nodePoint, nodeBaseRadius, nodeBaseRadius);
        }
        else
        {
            double offRadius = nodeBaseRadius * 0.7;
            context.DrawEllipse(_offBrush, null, nodePoint, offRadius, offRadius);
        }

        // Registered ring
        if (node.IsRegistered)
        {
            var ringPen = node.IsOnline ? _registeredOnlinePen : _registeredOfflinePen;
            context.DrawEllipse(null, ringPen, nodePoint, nodeBaseRadius + (4 * nodeScale), nodeBaseRadius + (4 * nodeScale));
        }

        // Selection highlight
        if (isSelected)
        {
            context.DrawEllipse(null, _selectPen, nodePoint, nodeBaseRadius + (7 * nodeScale), nodeBaseRadius + (7 * nodeScale));
        }

        // Label
        var textColor = node.IsOnline ? _onlineTextColor : _offlineTextColor;
        var typeface = isSelected ? _typefaceBold : _typefaceNormal;

        var cacheKey = new NodeTextKey(node.MacAddress, node.DisplayName, node.IsOnline, isSelected, (int)(textScale * 100));
        if (!_textCache.TryGetValue(cacheKey, out var formattedText))
        {
            formattedText = new FormattedText(
                node.DisplayName,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface, 11 * textScale, textColor
            );
            _textCache[cacheKey] = formattedText;
        }

        double textYOffset = nodePoint.Y > center.Y ? -(24 * nodeScale) : (18 * nodeScale);
        context.DrawText(formattedText, new Point(nodePoint.X - (formattedText.Width / 2), nodePoint.Y + textYOffset));
    }

    private void RenderZoomOverlay(DrawingContext context, Rect bounds)
    {
        // Zoom Level Indicator (Subtle overlay)
        if (Math.Abs(_zoomLevel - 1.0) > 0.001)
        {
            if (_cachedZoomText == null || Math.Abs(_zoomLevel - _cachedZoomLevel) > 0.001)
            {
                _cachedZoomLevel = _zoomLevel;
                _cachedZoomText = new FormattedText(
                    $"Zoom: {_zoomLevel:F1}x",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _typefaceNormal, 12, _zoomTextColor
                );
            }
            context.DrawText(_cachedZoomText, new Point(bounds.Width - _cachedZoomText.Width - 10, bounds.Height - _cachedZoomText.Height - 10));
        }
    }
}
