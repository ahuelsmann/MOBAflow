// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Microsoft.Maui.Graphics;

/// <summary>
/// Semicircle speed gauge with needle for locomotive control on MOBAsmart.
/// Mirrors WinUI <c>SpeedometerControl</c>: km/h in the center, outer km/h ring, inner DCC step ring.
/// </summary>
public partial class SpeedGaugeView
{
    private readonly SpeedGaugeDrawable _drawable = new();
    private bool _themeColorsCached;
    private CancellationTokenSource? _invalidateThrottleCts;
    private double _lastDrawnValue = double.NaN;
    private double _lastDrawnMaximum = double.NaN;
    private int _lastDrawnDisplayKmh = int.MinValue;
    private int _lastDrawnSpeedSteps = int.MinValue;
    private int _lastDrawnVmaxKmh = int.MinValue;

    private const int InvalidateThrottleMs = 16;

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value),
        typeof(double),
        typeof(SpeedGaugeView),
        0d,
        propertyChanged: OnGaugePropertyChanged);

    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(
        nameof(Maximum),
        typeof(double),
        typeof(SpeedGaugeView),
        126d,
        propertyChanged: OnGaugePropertyChanged);

    public static readonly BindableProperty DisplayKmhProperty = BindableProperty.Create(
        nameof(DisplayKmh),
        typeof(int),
        typeof(SpeedGaugeView),
        0,
        propertyChanged: OnGaugePropertyChanged);

    public static readonly BindableProperty SpeedStepsProperty = BindableProperty.Create(
        nameof(SpeedSteps),
        typeof(int),
        typeof(SpeedGaugeView),
        128,
        propertyChanged: OnGaugePropertyChanged);

    public static readonly BindableProperty VmaxKmhProperty = BindableProperty.Create(
        nameof(VmaxKmh),
        typeof(int),
        typeof(SpeedGaugeView),
        200,
        propertyChanged: OnGaugePropertyChanged);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public int DisplayKmh
    {
        get => (int)GetValue(DisplayKmhProperty);
        set => SetValue(DisplayKmhProperty, value);
    }

    /// <summary>DCC speed steps mode: 14, 28, or 128.</summary>
    public int SpeedSteps
    {
        get => (int)GetValue(SpeedStepsProperty);
        set => SetValue(SpeedStepsProperty, value);
    }

    /// <summary>Maximum speed in km/h for outer ring markers.</summary>
    public int VmaxKmh
    {
        get => (int)GetValue(VmaxKmhProperty);
        set => SetValue(VmaxKmhProperty, value);
    }

    public SpeedGaugeView()
    {
        InitializeComponent();
        GaugeCanvas.Drawable = _drawable;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _invalidateThrottleCts?.Cancel();
        _invalidateThrottleCts?.Dispose();
        _invalidateThrottleCts = null;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        CacheThemeColors();
        RefreshDrawable(forceInvalidate: true);
    }

    private static void OnGaugePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SpeedGaugeView view)
        {
            view.ScheduleRefreshDrawable();
        }
    }

    private void ScheduleRefreshDrawable()
    {
        _invalidateThrottleCts?.Cancel();
        _invalidateThrottleCts?.Dispose();
        _invalidateThrottleCts = new CancellationTokenSource();
        var token = _invalidateThrottleCts.Token;
        _ = ThrottledRefreshAsync(token);
    }

    private async Task ThrottledRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(InvalidateThrottleMs, cancellationToken).ConfigureAwait(true);
            RefreshDrawable(forceInvalidate: false);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer gauge update.
        }
    }

    private void RefreshDrawable(bool forceInvalidate)
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => RefreshDrawable(forceInvalidate));
            return;
        }

        if (!_themeColorsCached)
        {
            CacheThemeColors();
        }

        var valueChanged = !double.Equals(_lastDrawnValue, Value);
        var maximumChanged = !double.Equals(_lastDrawnMaximum, Maximum);
        var displayChanged = _lastDrawnDisplayKmh != DisplayKmh;
        var stepsChanged = _lastDrawnSpeedSteps != SpeedSteps;
        var vmaxChanged = _lastDrawnVmaxKmh != VmaxKmh;

        if (!forceInvalidate && !valueChanged && !maximumChanged && !displayChanged && !stepsChanged && !vmaxChanged)
        {
            return;
        }

        _drawable.Value = Value;
        _drawable.Maximum = Maximum;
        _drawable.DisplayKmh = DisplayKmh;
        _drawable.SpeedSteps = SpeedSteps;
        _drawable.VmaxKmh = VmaxKmh > 0 ? VmaxKmh : 200;
        GaugeCanvas.Invalidate();

        _lastDrawnValue = Value;
        _lastDrawnMaximum = Maximum;
        _lastDrawnDisplayKmh = DisplayKmh;
        _lastDrawnSpeedSteps = SpeedSteps;
        _lastDrawnVmaxKmh = VmaxKmh;
    }

    private void CacheThemeColors()
    {
        _drawable.TrackColor = ResolveColor("BorderColor", Colors.Gray);
        _drawable.AccentColor = ResolveColor("RailwayAccent", Colors.DodgerBlue);
        _drawable.NeedleColor = ResolveColor("RailwayDanger", Colors.Red);
        _drawable.TextPrimary = ResolveColor("TextPrimary", Colors.White);
        _drawable.TextSecondary = ResolveColor("TextSecondary", Colors.LightGray);
        _drawable.SurfaceColor = ResolveColor("Surface", Colors.DarkGray);
        _themeColorsCached = true;
    }

    private static Color ResolveColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var resource) == true
            && resource is Color color)
        {
            return color;
        }

        return fallback;
    }

    private sealed class SpeedGaugeDrawable : IDrawable
    {
        public double Value { get; set; }
        public double Maximum { get; set; } = 126;
        public int DisplayKmh { get; set; }
        public int SpeedSteps { get; set; } = 128;
        public int VmaxKmh { get; set; } = 200;
        public Color TrackColor { get; set; } = Colors.Gray;
        public Color AccentColor { get; set; } = Colors.DodgerBlue;
        public Color NeedleColor { get; set; } = Colors.Red;
        public Color TextPrimary { get; set; } = Colors.White;
        public Color TextSecondary { get; set; } = Colors.LightGray;
        public Color SurfaceColor { get; set; } = Colors.DarkGray;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centerX = dirtyRect.Width / 2f;
            var centerY = dirtyRect.Height * 0.65f;
            var radius = Math.Min(dirtyRect.Width * 0.34f, dirtyRect.Height * 0.46f);
            const float stroke = 14f;
            const float startAngleDeg = 180f;
            const float endAngleDeg = 0f;

            var normalized = Maximum > 0 ? Math.Clamp(Value / Maximum, 0, 1) : 0;

            canvas.StrokeSize = stroke;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = TrackColor.WithAlpha(0.55f);
            canvas.DrawPath(CreateArcPath(centerX, centerY, radius, startAngleDeg, endAngleDeg));

            if (normalized > 0.001)
            {
                var speedEndAngle = startAngleDeg - (float)(normalized * 180f);
                canvas.StrokeSize = stroke;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeColor = InterpolateSpeedColor(normalized);
                canvas.DrawPath(CreateArcPath(centerX, centerY, radius, startAngleDeg, speedEndAngle));
            }

            DrawKmhMarkers(canvas, centerX, centerY, radius);
            DrawSpeedStepMarkers(canvas, centerX, centerY, radius);

            var needleAngleDeg = startAngleDeg - (float)(normalized * 180f);
            var needleRad = needleAngleDeg * Math.PI / 180d;
            var needleLength = radius - 18f;
            var needleEndX = centerX + (float)(needleLength * Math.Cos(needleRad));
            var needleEndY = centerY - (float)(needleLength * Math.Sin(needleRad));

            canvas.StrokeSize = 4f;
            canvas.StrokeColor = NeedleColor;
            canvas.DrawLine(centerX, centerY, needleEndX, needleEndY);

            canvas.FillColor = SurfaceColor;
            canvas.FillCircle(centerX, centerY, 14f);
            canvas.StrokeColor = NeedleColor;
            canvas.StrokeSize = 3f;
            canvas.DrawCircle(centerX, centerY, 14f);

            canvas.FontColor = TextPrimary;
            canvas.FontSize = 28;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(
                DisplayKmh.ToString(),
                centerX - 40,
                centerY + 18,
                80,
                36,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);

            canvas.FontColor = TextSecondary;
            canvas.FontSize = 11;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.DrawString(
                "km/h",
                centerX - 30,
                centerY + 48,
                60,
                20,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        private void DrawSpeedStepMarkers(ICanvas canvas, float centerX, float centerY, float radius)
        {
            const float markerLength = 8f;
            var arcInnerRadius = radius - 8f;
            var labelDistance = radius - 25f;

            var (maxStep, stepsToDisplay) = SpeedSteps switch
            {
                14 => (13, new[] { 0, 3, 7, 10, 13 }),
                28 => (27, new[] { 0, 7, 14, 21, 27 }),
                _ => (126, new[] { 0, 32, 63, 95, 126 })
            };

            canvas.StrokeSize = 2.5f;
            canvas.StrokeColor = AccentColor.WithAlpha(0.9f);
            canvas.FontColor = AccentColor.WithAlpha(0.85f);
            canvas.FontSize = 9;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;

            foreach (var step in stepsToDisplay)
            {
                var normalized = maxStep > 0 ? (double)step / maxStep : 0;
                var angleDeg = 180f - (float)(normalized * 180f);

                var (startX, startY) = PointAtAngle(centerX, centerY, arcInnerRadius, angleDeg);
                var (endX, endY) = PointAtAngle(centerX, centerY, arcInnerRadius + markerLength, angleDeg);
                canvas.DrawLine(startX, startY, endX, endY);

                var (labelX, labelY) = PointAtAngle(centerX, centerY, labelDistance, angleDeg);
                canvas.DrawString(
                    step.ToString(),
                    labelX - 8,
                    labelY - 5,
                    16,
                    12,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center);
            }
        }

        private void DrawKmhMarkers(ICanvas canvas, float centerX, float centerY, float radius)
        {
            const float markerLength = 8f;
            var arcOuterRadius = radius + 8f;
            var labelDistance = radius + 25f;
            var kmhStep = CalculateOptimalKmhStep(VmaxKmh);

            var kmhValues = new List<int>();
            for (var kmh = 0; kmh <= VmaxKmh; kmh += kmhStep)
            {
                kmhValues.Add(kmh);
            }

            if (kmhValues.Count == 0 || kmhValues[^1] != VmaxKmh)
            {
                kmhValues.Add(VmaxKmh);
            }

            var markerBrush = TextPrimary.WithAlpha(0.78f);
            var maxBrush = NeedleColor;

            foreach (var kmh in kmhValues)
            {
                var isMax = kmh == VmaxKmh;
                var percentage = VmaxKmh > 0 ? (double)kmh / VmaxKmh : 0;
                var angleDeg = 180f - (float)(percentage * 180f);

                var (startX, startY) = PointAtAngle(centerX, centerY, arcOuterRadius, angleDeg);
                var (endX, endY) = PointAtAngle(centerX, centerY, arcOuterRadius - markerLength, angleDeg);

                canvas.StrokeSize = isMax ? 3f : 2.5f;
                canvas.StrokeColor = isMax ? maxBrush : markerBrush;
                canvas.DrawLine(startX, startY, endX, endY);

                var (labelX, labelY) = PointAtAngle(centerX, centerY, labelDistance, angleDeg);
                canvas.FontColor = isMax ? maxBrush : markerBrush;
                canvas.FontSize = isMax ? 11 : 10;
                canvas.Font = isMax
                    ? Microsoft.Maui.Graphics.Font.DefaultBold
                    : Microsoft.Maui.Graphics.Font.Default;
                canvas.DrawString(
                    kmh.ToString(),
                    labelX - 10,
                    labelY - 8,
                    20,
                    14,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center);
            }
        }

        private static int CalculateOptimalKmhStep(int vmax) => vmax switch
        {
            <= 50 => 5,
            <= 100 => 10,
            <= 200 => 20,
            <= 300 => 30,
            _ => 50
        };

        private static PathF CreateArcPath(
            float centerX,
            float centerY,
            float radius,
            float startAngleDeg,
            float endAngleDeg)
        {
            var path = new PathF();
            var (startX, startY) = PointAtAngle(centerX, centerY, radius, startAngleDeg);
            path.MoveTo(startX, startY);
            path.AddArc(
                centerX - radius,
                centerY - radius,
                centerX + radius,
                centerY + radius,
                startAngleDeg,
                endAngleDeg,
                clockwise: true);
            return path;
        }

        private static (float x, float y) PointAtAngle(float centerX, float centerY, float radius, float angleDeg)
        {
            var angleRad = angleDeg * Math.PI / 180d;
            return (
                centerX + (radius * (float)Math.Cos(angleRad)),
                centerY - (radius * (float)Math.Sin(angleRad)));
        }

        private static Color InterpolateSpeedColor(double normalized)
        {
            if (normalized < 0.5)
            {
                var t = normalized * 2;
                return Color.FromRgb(
                    (float)(0.3 + (t * 0.7)),
                    (float)(0.69 - (t * 0.29)),
                    0.31f);
            }

            var u = (normalized - 0.5) * 2;
            return Color.FromRgb(
                1f,
                (float)(0.4 * (1 - u)),
                (float)(0.31 * (1 - u)));
        }
    }
}
