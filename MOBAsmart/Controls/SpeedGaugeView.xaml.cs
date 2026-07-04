// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Moba.Common.Display;
using Microsoft.Maui.Graphics;

/// <summary>
/// Fluent-styled semicircle speed gauge with km/h primary scale and tertiary DCC steps.
/// </summary>
public partial class SpeedGaugeView
{
    private readonly SpeedGaugeDrawable _drawable = new();
    private bool _themeColorsCached;
    private AppTheme _lastCachedTheme = AppTheme.Unspecified;
    private CancellationTokenSource? _invalidateThrottleCts;
    private IDispatcherTimer? _animationTimer;
    private double _lastDrawnValue = double.NaN;
    private double _lastDrawnMaximum = double.NaN;
    private int _lastDrawnDisplayKmh = int.MinValue;
    private int _lastDrawnSpeedSteps = int.MinValue;
    private int _lastDrawnVmaxKmh = int.MinValue;
    private int _lastDrawnGaugeMaxKmh = int.MinValue;
    private double _animatedNormalized;
    private double _animationFrom;
    private double _animationTarget;
    private DateTime _animationStartUtc;
    private bool _isAnimating;
    private const int InvalidateThrottleMs = 16;
    private const int AnimationDurationMs = 280;
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
    public static readonly BindableProperty GaugeMaxKmhProperty = BindableProperty.Create(
        nameof(GaugeMaxKmh),
        typeof(int),
        typeof(SpeedGaugeView),
        400,
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

    public int SpeedSteps
    {
        get => (int)GetValue(SpeedStepsProperty);
        set => SetValue(SpeedStepsProperty, value);
    }

    public int VmaxKmh
    {
        get => (int)GetValue(VmaxKmhProperty);
        set => SetValue(VmaxKmhProperty, value);
    }

    public int GaugeMaxKmh
    {
        get => (int)GetValue(GaugeMaxKmhProperty);
        set => SetValue(GaugeMaxKmhProperty, value);
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
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        _invalidateThrottleCts?.Cancel();
        _invalidateThrottleCts?.Dispose();
        _invalidateThrottleCts = null;
        StopAnimation();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        CacheThemeColors();
        RefreshDrawable(forceInvalidate: true);
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        _themeColorsCached = false;
        _lastCachedTheme = AppTheme.Unspecified;
        Dispatcher.DispatchAsync(() =>
        {
            CacheThemeColors();
            RefreshDrawable(forceInvalidate: true);
        });
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

        if (!_themeColorsCached || _lastCachedTheme != GetEffectiveAppTheme())
        {
            CacheThemeColors();
        }

        var valueChanged = !double.Equals(_lastDrawnValue, Value);
        var maximumChanged = !double.Equals(_lastDrawnMaximum, Maximum);
        var displayChanged = _lastDrawnDisplayKmh != DisplayKmh;
        var stepsChanged = _lastDrawnSpeedSteps != SpeedSteps;
        var vmaxChanged = _lastDrawnVmaxKmh != VmaxKmh;
        var gaugeMaxChanged = _lastDrawnGaugeMaxKmh != GaugeMaxKmh;
        if (!forceInvalidate && !valueChanged && !maximumChanged && !displayChanged
            && !stepsChanged && !vmaxChanged && !gaugeMaxChanged && !_isAnimating)
        {
            return;
        }

        var gaugeMax = GaugeMaxKmh > 0 ? GaugeMaxKmh : 400;
        var targetNormalized = gaugeMax > 0
            ? Math.Clamp((double)DisplayKmh / gaugeMax, 0, 1)
            : 0;
        if (forceInvalidate || displayChanged || gaugeMaxChanged)
        {
            if (!_isAnimating && _animatedNormalized == 0 && targetNormalized == 0)
            {
                _animatedNormalized = 0;
            }

            else if (Math.Abs(targetNormalized - _animationTarget) > 0.0001 || forceInvalidate)
            {
                StartAnimation(targetNormalized);
            }

        }

        _drawable.Value = Value;
        _drawable.Maximum = Maximum;
        _drawable.DisplayKmh = DisplayKmh;
        _drawable.SpeedSteps = SpeedSteps;
        _drawable.VmaxKmh = VmaxKmh > 0 ? VmaxKmh : 200;
        _drawable.GaugeMaxKmh = gaugeMax;
        _drawable.AnimatedNormalized = _animatedNormalized;
        UpdateAccessibilityDescription();
        GaugeCanvas.Invalidate();
        _lastDrawnValue = Value;
        _lastDrawnMaximum = Maximum;
        _lastDrawnDisplayKmh = DisplayKmh;
        _lastDrawnSpeedSteps = SpeedSteps;
        _lastDrawnVmaxKmh = VmaxKmh;
        _lastDrawnGaugeMaxKmh = GaugeMaxKmh;
    }

    private void UpdateAccessibilityDescription()
    {
        var step = (int)Math.Round(Value);
        SemanticProperties.SetDescription(
            this,
            $"Current speed {DisplayKmh} kilometers per hour, DCC step {step} of {(int)Maximum}");
    }

    private void StartAnimation(double target)
    {
        _animationFrom = _animatedNormalized;
        _animationTarget = target;
        _animationStartUtc = DateTime.UtcNow;
        if (Math.Abs(_animationFrom - _animationTarget) < 0.0001)
        {
            _animatedNormalized = _animationTarget;
            _drawable.AnimatedNormalized = _animatedNormalized;
            return;
        }

        if (_animationTimer is null)
        {
            _animationTimer = Dispatcher.CreateTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(16);
            _animationTimer.Tick += OnAnimationTick;
        }

        _isAnimating = true;
        if (!_animationTimer.IsRunning)
        {
            _animationTimer.Start();
        }

    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var elapsed = (DateTime.UtcNow - _animationStartUtc).TotalMilliseconds;
        var t = Math.Clamp(elapsed / AnimationDurationMs, 0, 1);
        var eased = 1 - Math.Pow(1 - t, 3);
        _animatedNormalized = _animationFrom + ((_animationTarget - _animationFrom) * eased);
        _drawable.AnimatedNormalized = _animatedNormalized;
        GaugeCanvas.Invalidate();
        if (t >= 1)
        {
            _animatedNormalized = _animationTarget;
            _drawable.AnimatedNormalized = _animatedNormalized;
            _isAnimating = false;
            StopAnimation();
            GaugeCanvas.Invalidate();
        }

    }

    private void StopAnimation()
    {
        if (_animationTimer is not null)
        {
            _animationTimer.Stop();
        }

        _isAnimating = false;
    }

    private void CacheThemeColors()
    {
        _drawable.TrackColor = ResolveColor("BorderColor", Colors.Gray);
        _drawable.AccentColor = ResolveColor("RailwayAccent", Colors.DodgerBlue);
        _drawable.DangerColor = ResolveColor("RailwayDanger", Colors.Red);
        _drawable.TextPrimary = ResolveColor("TextPrimary", Colors.White);
        _drawable.TextSecondary = ResolveColor("TextSecondary", Colors.LightGray);
        _drawable.SurfaceColor = ResolveColor("Surface", Colors.DarkGray);
        _drawable.SurfaceVariantColor = ResolveColor("SurfaceVariant", Colors.Gray);
        var surface = ToColorComponents(_drawable.SurfaceColor);
        var textPrimary = ToColorComponents(_drawable.TextPrimary);
        _drawable.IsLightBackground = GaugeThemeAppearance.IsLightBackground(
            GetGaugeBackgroundMode(),
            surface.R,
            surface.G,
            surface.B,
            textPrimary.R,
            textPrimary.G,
            textPrimary.B);
        _lastCachedTheme = GetEffectiveAppTheme();
        _themeColorsCached = true;
    }

    private static AppTheme GetEffectiveAppTheme()
    {
        if (Application.Current is not Application app)
        {
            return AppTheme.Unspecified;
        }

        return app.RequestedTheme switch
        {
            AppTheme.Light => AppTheme.Light,
            AppTheme.Dark => AppTheme.Dark,
            _ => app.PlatformAppTheme
        };
    }

    private static GaugeBackgroundMode GetGaugeBackgroundMode() =>
        GetEffectiveAppTheme() switch
        {
            AppTheme.Light => GaugeBackgroundMode.Light,
            AppTheme.Dark => GaugeBackgroundMode.Dark,
            _ => GaugeBackgroundMode.Auto
        };
    private static (byte R, byte G, byte B) ToColorComponents(Color color) =>
        ((byte)Math.Clamp(color.Red * 255, 0, 255),
         (byte)Math.Clamp(color.Green * 255, 0, 255),
         (byte)Math.Clamp(color.Blue * 255, 0, 255));
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
        public int GaugeMaxKmh { get; set; } = 400;
        public double AnimatedNormalized { get; set; }
        public Color TrackColor { get; set; } = Colors.Gray;
        public Color AccentColor { get; set; } = Colors.DodgerBlue;
        public Color DangerColor { get; set; } = Colors.Red;
        public Color TextPrimary { get; set; } = Colors.White;
        public Color TextSecondary { get; set; } = Colors.LightGray;
        public Color SurfaceColor { get; set; } = Colors.DarkGray;
        public Color SurfaceVariantColor { get; set; } = Colors.Gray;
        public bool IsLightBackground { get; set; }
        private float ResolveContentAlpha(bool isIdle) =>
            isIdle && !IsLightBackground ? 0.82f : 1f;
        private float ResolveAlpha(float baseAlpha, float contentAlpha) =>
            Math.Min(1f, baseAlpha * contentAlpha);
        private Color ResolveTrackColor(float contentAlpha) =>
            IsLightBackground
                ? TextSecondary.WithAlpha(ResolveAlpha(0.55f, contentAlpha))
                : SurfaceVariantColor.WithAlpha(ResolveAlpha(0.50f, contentAlpha));
        private Color ResolvePrimaryMarkerColor(float contentAlpha) =>
            TextPrimary.WithAlpha(ResolveAlpha(IsLightBackground ? 0.97f : 0.95f, contentAlpha));
        private Color ResolveSecondaryMarkerColor(float contentAlpha) =>
            TextSecondary.WithAlpha(ResolveAlpha(IsLightBackground ? 0.82f : 0.75f, contentAlpha));
        private Color ResolveInnerStepColor(float contentAlpha) =>
            AccentColor.WithAlpha(ResolveAlpha(IsLightBackground ? 0.88f : 0.80f, contentAlpha));
        private Color ResolveIdleNeedleColor(float contentAlpha) =>
            (IsLightBackground ? TextPrimary : TextSecondary)
                .WithAlpha(ResolveAlpha(IsLightBackground ? 0.78f : 0.68f, contentAlpha));
        private Color ResolveCenterSpeedColor(float contentAlpha) =>
            IsLightBackground
                ? TextPrimary
                : TextPrimary.WithAlpha(ResolveAlpha(1f, contentAlpha));
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var scale = dirtyRect.Width / 390f;
            var centerX = dirtyRect.Width / 2f;
            var centerY = dirtyRect.Height * 0.64f;
            var radius = Math.Min(dirtyRect.Width * 0.31f, dirtyRect.Height * 0.44f);
            var stroke = 15f * scale;
            const float startAngleDeg = 180f;
            const float endAngleDeg = 0f;
            var gaugeMax = GaugeMaxKmh > 0 ? GaugeMaxKmh : 400;
            var normalized = Math.Clamp(AnimatedNormalized, 0, 1);
            var isIdle = DisplayKmh == 0 && Value <= 0.001;
            var contentAlpha = ResolveContentAlpha(isIdle);
            var vmaxRatio = gaugeMax > 0 ? Math.Clamp((double)VmaxKmh / gaugeMax, 0, 1) : 0.5;
            var isOverVmax = DisplayKmh > VmaxKmh && VmaxKmh > 0;
            canvas.StrokeSize = stroke;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = ResolveTrackColor(contentAlpha);
            canvas.DrawPath(CreateArcPath(centerX, centerY, radius, startAngleDeg, endAngleDeg));
            if (normalized > 0.001)
            {
                var speedEndAngle = startAngleDeg - (float)(normalized * 180f);
                var arcColor = GetArcColor(normalized, vmaxRatio, contentAlpha);
                canvas.StrokeSize = stroke + (6f * scale);
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeColor = arcColor.WithAlpha(ResolveAlpha(IsLightBackground ? 0.48f : 0.38f, contentAlpha));
                canvas.DrawPath(CreateArcPath(centerX, centerY, radius, startAngleDeg, speedEndAngle));
                canvas.StrokeSize = stroke;
                canvas.StrokeColor = arcColor;
                canvas.DrawPath(CreateArcPath(centerX, centerY, radius, startAngleDeg, speedEndAngle));
            }

            DrawVmaxMarker(canvas, centerX, centerY, radius, scale, gaugeMax, vmaxRatio);
            DrawKmhMarkers(canvas, centerX, centerY, radius, scale, gaugeMax, contentAlpha, dirtyRect.Width);
            DrawSpeedStepMarkers(canvas, centerX, centerY, radius, scale, contentAlpha);
            var needleAngleDeg = startAngleDeg - (float)(normalized * 180f);
            var needleLength = radius - (18f * scale);
            var needleColor = isIdle
                ? ResolveIdleNeedleColor(contentAlpha)
                : isOverVmax || normalized > 0.88
                    ? DangerColor.WithAlpha(ResolveAlpha(1f, contentAlpha))
                    : AccentColor.WithAlpha(ResolveAlpha(1f, contentAlpha));
            DrawNeedleShadow(canvas, centerX, centerY, needleAngleDeg, needleLength, scale, IsLightBackground);
            DrawNeedle(canvas, centerX, centerY, needleAngleDeg, needleLength, scale, needleColor);
            DrawHub(canvas, centerX, centerY, scale, contentAlpha, isIdle, isOverVmax || normalized > 0.88);
            DrawCenterBackdrop(canvas, centerX, centerY, scale, contentAlpha);
            DrawCenterDisplay(canvas, centerX, centerY, scale, contentAlpha);
        }

        private void DrawVmaxMarker(
            ICanvas canvas,
            float centerX,
            float centerY,
            float radius,
            float scale,
            int gaugeMax,
            double vmaxRatio)
        {
            if (VmaxKmh <= 0 || vmaxRatio <= 0.001 || vmaxRatio >= 0.999)
            {
                return;
            }

            var angleDeg = 180f - (float)(vmaxRatio * 180f);
            var markerLength = 14f * scale;
            var arcRadius = radius + (6f * scale);
            var (startX, startY) = PointAtAngle(centerX, centerY, arcRadius - (2f * scale), angleDeg);
            var (endX, endY) = PointAtAngle(centerX, centerY, arcRadius + markerLength, angleDeg);
            canvas.StrokeSize = 2.5f * scale;
            canvas.StrokeColor = ResolveSecondaryMarkerColor(1f).WithAlpha(ResolveAlpha(0.75f, 1f));
            canvas.DrawLine(startX, startY, endX, endY);
        }

        private void DrawKmhMarkers(
            ICanvas canvas,
            float centerX,
            float centerY,
            float radius,
            float scale,
            int gaugeMax,
            float contentAlpha,
            float canvasWidth)
        {
            var kmhStep = CalculateOptimalKmhStep(gaugeMax);
            var kmhValues = new List<int>();
            for (var kmh = 0; kmh <= gaugeMax; kmh += kmhStep)
            {
                kmhValues.Add(kmh);
            }

            if (kmhValues.Count == 0 || kmhValues[^1] != gaugeMax)
            {
                kmhValues.Add(gaugeMax);
            }

            var arcOuterRadius = radius + (8f * scale);
            var labelDistance = radius + (44f * scale);
            var markerBrush = ResolvePrimaryMarkerColor(contentAlpha);
            foreach (var kmh in kmhValues)
            {
                var isGaugeMax = kmh == gaugeMax;
                var isMajor = IsMajorKmhTick(kmh, gaugeMax, kmhStep);
                var percentage = gaugeMax > 0 ? (double)kmh / gaugeMax : 0;
                var angleDeg = 180f - (float)(percentage * 180f);
                var tickLength = (isMajor ? 11f : 6f) * scale;
                var (startX, startY) = PointAtAngle(centerX, centerY, arcOuterRadius, angleDeg);
                var (endX, endY) = PointAtAngle(centerX, centerY, arcOuterRadius - tickLength, angleDeg);
                canvas.StrokeSize = (isMajor ? 2.8f : 1.8f) * scale;
                canvas.StrokeColor = isGaugeMax
                    ? markerBrush.WithAlpha(IsLightBackground ? 1f : 0.98f)
                    : markerBrush.WithAlpha(isMajor
                        ? (IsLightBackground ? 0.98f : 0.95f)
                        : (IsLightBackground ? 0.82f : 0.78f));
                canvas.DrawLine(startX, startY, endX, endY);
                if (!isMajor && kmh % kmhStep != 0)
                {
                    continue;
                }

                var labelText = kmh.ToString();
                var labelWidth = labelText.Length switch
                {
                    >= 3 => 60f * scale,
                    2 => 36f * scale,
                    _ => 26f * scale
                };
                var labelHeight = 22f * scale;
                var (labelX, labelY) = PointAtAngle(centerX, centerY, labelDistance, angleDeg);
                var boxX = CalculateKmhLabelLeft(labelX, labelWidth, canvasWidth);
                canvas.FontColor = markerBrush;
                canvas.FontSize = (isMajor ? 14f : 11f) * scale;
                canvas.Font = isMajor
                    ? Microsoft.Maui.Graphics.Font.DefaultBold
                    : Microsoft.Maui.Graphics.Font.DefaultBold;
                canvas.DrawString(
                    labelText,
                    boxX,
                    labelY - (labelHeight / 2f),
                    labelWidth,
                    labelHeight,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center);
            }

        }

        private static float CalculateKmhLabelLeft(float labelX, float labelWidth, float canvasWidth)
        {
            const float canvasEdgeInset = 4f;
            var centered = labelX - (labelWidth / 2f);
            var maxLeft = canvasWidth - canvasEdgeInset - labelWidth;
            return Math.Clamp(centered, canvasEdgeInset, maxLeft);
        }

        private void DrawSpeedStepMarkers(
            ICanvas canvas,
            float centerX,
            float centerY,
            float radius,
            float scale,
            float contentAlpha)
        {
            var arcInnerRadius = radius - (10f * scale);
            var labelDistance = radius - (28f * scale);
            var tertiaryColor = ResolveInnerStepColor(contentAlpha);
            var (maxStep, stepsToDisplay) = SpeedSteps switch
            {
                14 => (13, new[] { 0, 3, 7, 10, 13 }),
                28 => (27, new[] { 0, 7, 14, 21, 27 }),
                _ => (126, new[] { 0, 32, 63, 95, 126 })
            };
            canvas.StrokeSize = 2f * scale;
            canvas.StrokeColor = tertiaryColor;
            canvas.FontColor = tertiaryColor;
            canvas.FontSize = (IsLightBackground ? 11f : 10.5f) * scale;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            foreach (var step in stepsToDisplay)
            {
                if (step == 0)
                {
                    continue;
                }

                var stepNormalized = maxStep > 0 ? (double)step / maxStep : 0;
                var angleDeg = 180f - (float)(stepNormalized * 180f);
                var tickLength = 5f * scale;
                var (startX, startY) = PointAtAngle(centerX, centerY, arcInnerRadius, angleDeg);
                var (endX, endY) = PointAtAngle(centerX, centerY, arcInnerRadius + tickLength, angleDeg);
                canvas.DrawLine(startX, startY, endX, endY);
                var (labelX, labelY) = PointAtAngle(centerX, centerY, labelDistance, angleDeg);
                var labelWidth = 28f * scale;
                var labelHeight = 16f * scale;
                canvas.DrawString(
                    step.ToString(),
                    labelX - (labelWidth / 2f),
                    labelY - (labelHeight / 2f),
                    labelWidth,
                    labelHeight,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center);
            }

        }

        private static void DrawNeedleShadow(
            ICanvas canvas,
            float centerX,
            float centerY,
            float angleDeg,
            float length,
            float scale,
            bool isLightBackground)
        {
            DrawNeedleShape(
                canvas,
                centerX + (1.5f * scale),
                centerY + (2f * scale),
                angleDeg,
                length,
                scale,
                Colors.Black.WithAlpha(isLightBackground ? 0.22f : 0.30f));
        }

        private static void DrawNeedle(
            ICanvas canvas,
            float centerX,
            float centerY,
            float angleDeg,
            float length,
            float scale,
            Color color)
        {
            DrawNeedleShape(canvas, centerX, centerY, angleDeg, length, scale, color);
        }

        private static void DrawNeedleShape(
            ICanvas canvas,
            float centerX,
            float centerY,
            float angleDeg,
            float length,
            float scale,
            Color color)
        {
            var angleRad = angleDeg * Math.PI / 180d;
            var perpRad = angleRad + (Math.PI / 2d);
            var baseOffset = 10f * scale;
            var baseHalf = 4.5f * scale;
            var tipX = centerX + (length * (float)Math.Cos(angleRad));
            var tipY = centerY - (length * (float)Math.Sin(angleRad));
            var baseCx = centerX + (baseOffset * (float)Math.Cos(angleRad));
            var baseCy = centerY - (baseOffset * (float)Math.Sin(angleRad));
            var leftX = baseCx + (baseHalf * (float)Math.Cos(perpRad));
            var leftY = baseCy - (baseHalf * (float)Math.Sin(perpRad));
            var rightX = baseCx - (baseHalf * (float)Math.Cos(perpRad));
            var rightY = baseCy + (baseHalf * (float)Math.Sin(perpRad));
            var path = new PathF();
            path.MoveTo(leftX, leftY);
            path.LineTo(tipX, tipY);
            path.LineTo(rightX, rightY);
            path.Close();
            canvas.FillColor = color;
            canvas.FillPath(path);
        }

        private void DrawHub(ICanvas canvas, float centerX, float centerY, float scale, float contentAlpha, bool isIdle, bool isDanger)
        {
            var outerRadius = 18f * scale;
            var innerRadius = 13f * scale;
            var activeRingColor = isDanger ? DangerColor : AccentColor;
            canvas.FillColor = IsLightBackground
                ? TextSecondary.WithAlpha(ResolveAlpha(0.18f, contentAlpha))
                : SurfaceColor.WithAlpha(ResolveAlpha(0.95f, contentAlpha));
            canvas.FillCircle(centerX, centerY, outerRadius);
            canvas.StrokeColor = isIdle
                ? ResolveIdleNeedleColor(contentAlpha).WithAlpha(ResolveAlpha(IsLightBackground ? 0.78f : 0.65f, contentAlpha))
                : activeRingColor.WithAlpha(ResolveAlpha(0.95f, contentAlpha));
            canvas.StrokeSize = 2.5f * scale;
            canvas.DrawCircle(centerX, centerY, outerRadius);
            canvas.FillColor = IsLightBackground
                ? TextPrimary.WithAlpha(ResolveAlpha(0.14f, contentAlpha))
                : SurfaceVariantColor.WithAlpha(ResolveAlpha(0.8f, contentAlpha));
            canvas.FillCircle(centerX, centerY, innerRadius);
            canvas.StrokeColor = (isIdle
                    ? (IsLightBackground ? TextPrimary : ResolveSecondaryMarkerColor(contentAlpha))
                    : activeRingColor)
                .WithAlpha(ResolveAlpha(IsLightBackground ? 0.55f : 0.70f, contentAlpha));
            canvas.StrokeSize = 1f * scale;
            canvas.DrawCircle(centerX, centerY, innerRadius);
        }

        private void DrawCenterBackdrop(ICanvas canvas, float centerX, float centerY, float scale, float contentAlpha)
        {
            var backdropWidth = 116f * scale;
            var backdropHeight = 72f * scale;
            var backdropX = centerX - (backdropWidth / 2f);
            var backdropY = centerY + (8f * scale);
            canvas.FillColor = IsLightBackground
                ? SurfaceColor.WithAlpha(ResolveAlpha(0.35f, contentAlpha))
                : SurfaceColor.WithAlpha(ResolveAlpha(0.28f, contentAlpha));
            canvas.FillRoundedRectangle(backdropX, backdropY, backdropWidth, backdropHeight, 8f * scale);
        }

        private void DrawCenterDisplay(ICanvas canvas, float centerX, float centerY, float scale, float contentAlpha)
        {
            var speedText = DisplayKmh.ToString();
            var stepText = $"Step {(int)Math.Round(Value)}";
            canvas.FontColor = ResolveCenterSpeedColor(contentAlpha);
            canvas.FontSize = 40 * scale;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(
                speedText,
                centerX - (54f * scale),
                centerY + (14f * scale),
                108f * scale,
                46f * scale,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
            canvas.FontColor = TextSecondary.WithAlpha(IsLightBackground ? 0.88f : ResolveAlpha(0.82f, contentAlpha));
            canvas.FontSize = 11f * scale;
            canvas.Font = IsLightBackground
                ? Microsoft.Maui.Graphics.Font.DefaultBold
                : Microsoft.Maui.Graphics.Font.Default;
            canvas.DrawString(
                "km/h",
                centerX - (40f * scale),
                centerY + (52f * scale),
                80f * scale,
                18f * scale,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
            canvas.FontColor = ResolveInnerStepColor(contentAlpha).WithAlpha(ResolveAlpha(0.85f, contentAlpha));
            canvas.FontSize = (IsLightBackground ? 10f : 9.5f) * scale;
            canvas.Font = IsLightBackground
                ? Microsoft.Maui.Graphics.Font.DefaultBold
                : Microsoft.Maui.Graphics.Font.Default;
            canvas.DrawString(
                stepText,
                centerX - (44f * scale),
                centerY + (68f * scale),
                88f * scale,
                16f * scale,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        private Color GetArcColor(double normalized, double vmaxRatio, float contentAlpha)
        {
            if (normalized > 0.88 || (vmaxRatio > 0 && normalized > vmaxRatio + 0.05))
            {
                return DangerColor.WithAlpha(contentAlpha);
            }

            if (normalized > 0.7)
            {
                var t = (normalized - 0.7) / 0.18;
                t = Math.Clamp(t, 0, 1);
                return LerpColor(AccentColor, DangerColor.WithAlpha(0.85f), t).WithAlpha(contentAlpha);
            }

            return AccentColor.WithAlpha(ResolveAlpha(0.95f, contentAlpha));
        }

        private static bool IsMajorKmhTick(int kmh, int gaugeMax, int step)
        {
            if (kmh == 0 || kmh == gaugeMax)
            {
                return true;
            }

            return step >= 50 ? kmh % 100 == 0 : step > 0 && kmh % (step * 2) == 0;
        }

        private static int CalculateOptimalKmhStep(int vmax) => vmax switch
        {
            <= 50 => 5,
            <= 100 => 10,
            <= 200 => 20,
            <= 300 => 30,
            <= 400 => 50,
            _ => 50
        };
        private static Color LerpColor(Color from, Color to, double t)
        {
            return Color.FromRgba(
                from.Red + ((to.Red - from.Red) * t),
                from.Green + ((to.Green - from.Green) * t),
                from.Blue + ((to.Blue - from.Blue) * t),
                from.Alpha + ((to.Alpha - from.Alpha) * t));
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

    }

}