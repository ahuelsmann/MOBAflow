// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Microsoft.Maui.Graphics;

/// <summary>
/// Semicircle speed gauge with needle for locomotive control on MOBAsmart.
/// </summary>
public partial class SpeedGaugeView
{
    private readonly SpeedGaugeDrawable _drawable = new();
    private bool _themeColorsCached;
    private CancellationTokenSource? _invalidateThrottleCts;
    private double _lastDrawnValue = double.NaN;
    private double _lastDrawnMaximum = double.NaN;
    private int _lastDrawnDisplayKmh = int.MinValue;

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

    public SpeedGaugeView()
    {
        InitializeComponent();
        GaugeCanvas.Drawable = _drawable;
        Loaded += OnLoaded;
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
        if (!_themeColorsCached)
        {
            CacheThemeColors();
        }

        var valueChanged = !double.Equals(_lastDrawnValue, Value);
        var maximumChanged = !double.Equals(_lastDrawnMaximum, Maximum);
        var displayChanged = _lastDrawnDisplayKmh != DisplayKmh;

        if (!forceInvalidate && !valueChanged && !maximumChanged && !displayChanged)
        {
            return;
        }

        _drawable.Value = Value;
        _drawable.Maximum = Maximum;
        _drawable.DisplayKmh = DisplayKmh;
        GaugeCanvas.Invalidate();

        _lastDrawnValue = Value;
        _lastDrawnMaximum = Maximum;
        _lastDrawnDisplayKmh = DisplayKmh;
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
            var radius = Math.Min(dirtyRect.Width * 0.38f, dirtyRect.Height * 0.5f);
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

            DrawTickMarks(canvas, centerX, centerY, radius + 10f);

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

        private void DrawTickMarks(ICanvas canvas, float centerX, float centerY, float tickRadius)
        {
            canvas.StrokeSize = 2f;
            canvas.StrokeColor = TextSecondary.WithAlpha(0.7f);

            for (var i = 0; i <= 4; i++)
            {
                var tickNormalized = i / 4d;
                var angleDeg = 180f - (float)(tickNormalized * 180f);
                var (outerX, outerY) = PointAtAngle(centerX, centerY, tickRadius, angleDeg);
                var (innerX, innerY) = PointAtAngle(centerX, centerY, tickRadius - 8f, angleDeg);
                canvas.DrawLine(innerX, innerY, outerX, outerY);
            }
        }
    }
}