// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

/// <summary>
/// A modern speedometer control for displaying locomotive speed.
/// Features an arc-based gauge with animated needle and digital display.
/// Supports dynamic theme colors via AccentColor property.
/// </summary>
internal sealed partial class SpeedometerControl
{

    /// <summary>
    /// Minimum speed value (typically 0).
    /// </summary>
    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register(nameof(MinValue), typeof(int), typeof(SpeedometerControl),
            new PropertyMetadata(0, OnValueChanged));

    /// <summary>
    /// Maximum speed value (typically 126 for DCC 128 speed steps).
    /// </summary>
    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(nameof(MaxValue), typeof(int), typeof(SpeedometerControl),
            new PropertyMetadata(126, OnValueChanged));

    /// <summary>
    /// Current speed value.
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(SpeedometerControl),
            new PropertyMetadata(0, OnValueChanged));

    /// <summary>
    /// Display value (e.g., km/h instead of speed steps).
    /// </summary>
    public static readonly DependencyProperty DisplayValueProperty =
        DependencyProperty.Register(nameof(DisplayValue), typeof(int), typeof(SpeedometerControl),
            new PropertyMetadata(0, OnDisplayValueChanged));

    /// <summary>
    /// Accent color for needle and center circle. When set, overrides ThemeResource.
    /// </summary>
    public static readonly DependencyProperty AccentColorProperty =
        DependencyProperty.Register(nameof(AccentColor), typeof(Color?), typeof(SpeedometerControl),
            new PropertyMetadata(null, OnAccentColorChanged));

    /// <summary>
    /// DCC speed steps configuration (14, 28, or 128).
    /// Controls how many speed step markers are displayed.
    /// </summary>
    public static readonly DependencyProperty SpeedStepsProperty =
        DependencyProperty.Register(nameof(SpeedSteps), typeof(int), typeof(SpeedometerControl),
            new PropertyMetadata(128, OnSpeedStepsChanged));

    /// <summary>
    /// Maximum speed in km/h (Vmax) for displaying km/h markers.
    /// This is separate from MaxValue which represents DCC speed steps.
    /// </summary>
    public static readonly DependencyProperty VmaxKmhProperty =
        DependencyProperty.Register(nameof(VmaxKmh), typeof(int), typeof(SpeedometerControl),
            new PropertyMetadata(200, OnVmaxKmhChanged));

    /// <summary>
    /// Full-scale maximum on the km/h ring (needle, arc, and outer markers).
    /// </summary>
    public static readonly DependencyProperty GaugeMaxKmhProperty =
        DependencyProperty.Register(nameof(GaugeMaxKmh), typeof(int), typeof(SpeedometerControl),
            new PropertyMetadata(400, OnGaugeMaxKmhChanged));
    public SpeedometerControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged -= OnActualThemeChanged;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateGaugeColors();
        RenderKmhMarkers();
        RenderSpeedStepMarkers();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged += OnActualThemeChanged;
        UpdateNeedle();
        UpdateSpeedArc();
        UpdateDisplayText();
        UpdateGaugeColors();
        RenderKmhMarkers();
        RenderSpeedStepMarkers();
    }

    public int MinValue
    {
        get => (int)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public int MaxValue
    {
        get => (int)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int DisplayValue
    {
        get => (int)GetValue(DisplayValueProperty);
        set => SetValue(DisplayValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the accent color for the needle. Null uses default ThemeResource.
    /// </summary>
    public Color? AccentColor
    {
        get => (Color?)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of DCC speed steps (14, 28, or 128).
    /// </summary>
    public int SpeedSteps
    {
        get => (int)GetValue(SpeedStepsProperty);
        set => SetValue(SpeedStepsProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum speed in km/h (Vmax).
    /// Used for displaying km/h markers on the outer ring.
    /// </summary>
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

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SpeedometerControl control)
        {
            return;
        }

        if (e.Property == MaxValueProperty)
        {
            control.RenderSpeedStepMarkers();
        }

        control.UpdateGaugeColors();
    }

    private static void OnDisplayValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.UpdateDisplayText();
            control.UpdateNeedle();
            control.UpdateSpeedArc();
            control.UpdateGaugeColors();
        }
    }

    private static void OnAccentColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.UpdateGaugeColors();
        }
    }

    private static void OnSpeedStepsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.RenderSpeedStepMarkers();
        }
    }

    private static void OnVmaxKmhChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        _ = e;
        if (d is SpeedometerControl control)
        {
            control.RenderKmhMarkers();
            control.UpdateGaugeColors();
        }
    }

    private static void OnGaugeMaxKmhChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.RenderKmhMarkers();
            control.UpdateNeedle();
            control.UpdateSpeedArc();
            control.UpdateGaugeColors();
        }
    }

    private void UpdateGaugeColors()
    {
        var gaugeMax = GaugeMaxKmh > 0 ? GaugeMaxKmh : 400;
        var normalized = gaugeMax > 0 ? Math.Clamp((double)DisplayValue / gaugeMax, 0, 1) : 0;
        var isIdle = DisplayValue == 0 && Value <= 0;
        var isOverVmax = DisplayValue > VmaxKmh && VmaxKmh > 0;
        var isDanger = isOverVmax || normalized > GaugeVisualRules.DangerNormalizedThreshold;
        var brushes = GaugeVisualRules.ResolveNeedleBrushes(this, isIdle, isDanger, AccentColor);
        if (Needle is { } needle)
        {
            needle.Fill = brushes.Needle;
        }

        if (CenterCircle is { } circle)
        {
            circle.Stroke = brushes.HubRing;
        }
    }

    private void UpdateNeedle()
    {
        if (NeedleRotation is null) return;
        var gaugeMax = GaugeMaxKmh > 0 ? GaugeMaxKmh : 400;
        if (gaugeMax <= 0) return;
        var normalizedValue = Math.Clamp((double)DisplayValue / gaugeMax, 0, 1);
        // Angle goes from -90 deg (left) to +90 deg (right)
        var angle = -90 + (normalizedValue * 180);
        NeedleRotation.Angle = angle;
    }

    private void UpdateSpeedArc()
    {
        if (SpeedArc is null) return;
        var gaugeMax = GaugeMaxKmh > 0 ? GaugeMaxKmh : 400;
        if (gaugeMax <= 0) return;
        var normalizedValue = Math.Clamp((double)DisplayValue / gaugeMax, 0, 1);
        var arcGeometry = GaugeArcGeometryBuilder.CreateSweepArc(normalizedValue);
        if (arcGeometry is null)
        {
            SpeedArc.Data = null;
            SpeedArc.Visibility = Visibility.Collapsed;
            if (SpeedArcGlow is { } glowHidden)
            {
                glowHidden.Data = null;
                glowHidden.Visibility = Visibility.Collapsed;
            }

            return;
        }

        // WinUI Path.Data can only parent a geometry once; glow needs its own instance.
        var glowGeometry = GaugeArcGeometryBuilder.CreateSweepArc(normalizedValue);
        SpeedArc.Visibility = Visibility.Visible;
        SpeedArc.StrokeThickness = GaugeArcGeometryBuilder.ArcStrokeThickness;
        SpeedArc.Data = arcGeometry;
        if (SpeedArcGlow is { } glow)
        {
            glow.Visibility = Visibility.Visible;
            glow.StrokeThickness = GaugeArcGeometryBuilder.ArcStrokeThickness + 6;
            glow.Data = glowGeometry;
        }
        UpdateArcColor(normalizedValue);
    }

    private void UpdateArcColor(double normalizedValue)
    {
        if (SpeedArc is null) return;
        var gaugeMax = GaugeMaxKmh > 0 ? GaugeMaxKmh : 400;
        var vmaxRatio = gaugeMax > 0 ? Math.Clamp((double)VmaxKmh / gaugeMax, 0, 1) : 0.5;
        var arcColor = GaugeVisualRules.ResolveSpeedArcColor(this, normalizedValue, vmaxRatio);
        SpeedArc.Stroke = new SolidColorBrush(arcColor);
        if (SpeedArcGlow is { } glow)
        {
            glow.Stroke = new SolidColorBrush(GaugeVisualRules.WithAlpha(arcColor, GaugeVisualRules.ArcGlowAlpha));
        }
    }

    private void UpdateDisplayText()
    {
        if (SpeedText is null) return;
        SpeedText.Text = DisplayValue.ToString();
    }

    /// <summary>
    /// Calculates the optimal km/h step for marker display.
    /// Goal: Display 8-10 markers on the gauge (never overloaded).
    /// </summary>
    private static int CalculateOptimalKmhStep(int vmax) => vmax switch
    {
        <= 50 => 5,
        <= 100 => 10,
        <= 200 => 20,
        <= 300 => 30,
        <= 400 => 50,
        _ => 50
    };

    /// <summary>
    /// Renders DCC speed step markers dynamically based on SpeedSteps configuration.
    /// </summary>
    private void RenderSpeedStepMarkers()
    {
        if (SpeedStepMarkersCanvas is null)
            return;
        SpeedStepMarkersCanvas.Children.Clear();
        const double centerX = GaugeVisualRules.GaugeCenterX;
        const double centerY = GaugeVisualRules.GaugeCenterY;
        const double arcInnerRadius = 92;
        const double markerLength = 8;
        var (maxStep, stepsToDisplay) = SpeedSteps switch
        {
            14 => (13, new[] { 0, 3, 7, 10, 13 }),
            28 => (27, new[] { 0, 7, 14, 21, 27 }),
            _ => (126, [0, 32, 63, 95, 126])
        };
        var tertiaryBrush = GaugeVisualRules.CreateAccentSecondaryMarkerBrush(this);
        foreach (var step in stepsToDisplay)
        {
            if (step == 0)
            {
                continue;
            }

            var normalized = maxStep > 0 ? (double)step / maxStep : 0;
            var angleDeg = 180 - (normalized * 180);
            var angleRad = angleDeg * Math.PI / 180;
            var radialX = Math.Cos(angleRad);
            var radialY = -Math.Sin(angleRad);
            var startX = centerX + (arcInnerRadius * radialX);
            var startY = centerY + (arcInnerRadius * radialY);
            var endX = centerX + ((arcInnerRadius + markerLength) * radialX);
            var endY = centerY + ((arcInnerRadius + markerLength) * radialY);
            var line = new Line
            {
                X1 = startX,
                Y1 = startY,
                X2 = endX,
                Y2 = endY,
                Stroke = tertiaryBrush,
                StrokeThickness = 2
            };
            SpeedStepMarkersCanvas.Children.Add(line);
            // Max step shares the 3 o'clock radial with the outer gauge-max label; tick only avoids overlap.
            if (step == maxStep)
            {
                continue;
            }

            var labelDistance = GaugeVisualRules.SecondaryMarkerLabelDistance;
            var labelX = centerX + (labelDistance * radialX);
            var labelY = centerY + (labelDistance * radialY);
            const double stepLabelWidth = 28;
            var labelHeight = GaugeVisualRules.SecondaryMarkerLabelHeight;
            var label = new TextBlock
            {
                Text = step.ToString(),
                Width = stepLabelWidth,
                Height = labelHeight,
                FontSize = GaugeVisualRules.SecondaryMarkerFontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = tertiaryBrush,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(label, labelX - (stepLabelWidth / 2));
            Canvas.SetTop(label, GaugeVisualRules.CalculateMarkerLabelTop(labelY, labelHeight));
            SpeedStepMarkersCanvas.Children.Add(label);
        }
    }

    /// <summary>
    /// Renders km/h markers dynamically based on GaugeMaxKmh with adaptive step-sizing.
    /// </summary>
    private void RenderKmhMarkers()
    {
        if (KmhMarkersCanvas is null)
            return;
        KmhMarkersCanvas.Children.Clear();
        const double centerX = GaugeVisualRules.GaugeCenterX;
        const double centerY = GaugeVisualRules.GaugeCenterY;
        const double arcOuterRadius = GaugeVisualRules.OuterArcRadius;
        const double markerLength = 8;
        var gaugeMax = GaugeMaxKmh > 0 ? GaugeMaxKmh : 400;
        var kmhStep = CalculateOptimalKmhStep(gaugeMax);
        var kmhValues = new List<int>();
        for (int kmh = 0; kmh <= gaugeMax; kmh += kmhStep)
        {
            kmhValues.Add(kmh);
        }

        if (kmhValues.Count == 0 || kmhValues[^1] != gaugeMax)
        {
            kmhValues.Add(gaugeMax);
        }

        var markerBrush = GaugeVisualRules.CreatePrimaryMarkerBrush(this);
        foreach (var kmh in kmhValues)
        {
            var isMajor = IsMajorKmhTick(kmh, gaugeMax, kmhStep);
            var percentage = gaugeMax > 0 ? (double)kmh / gaugeMax : 0;
            var angleDeg = 180 - (percentage * 180);
            var angleRad = angleDeg * Math.PI / 180;
            var radialX = Math.Cos(angleRad);
            var radialY = -Math.Sin(angleRad);
            var startX = centerX + (arcOuterRadius * radialX);
            var startY = centerY + (arcOuterRadius * radialY);
            var endX = centerX + ((arcOuterRadius - markerLength) * radialX);
            var endY = centerY + ((arcOuterRadius - markerLength) * radialY);
            var line = new Line
            {
                X1 = startX,
                Y1 = startY,
                X2 = endX,
                Y2 = endY,
                Stroke = markerBrush,
                StrokeThickness = isMajor ? 2.8 : 1.8
            };
            KmhMarkersCanvas.Children.Add(line);
            if (!isMajor && kmh % kmhStep != 0)
            {
                continue;
            }

            var labelText = kmh.ToString();
            var labelWidth = GaugeVisualRules.CalculateMarkerLabelWidth(labelText);
            var labelHeight = GaugeVisualRules.OuterMarkerLabelHeight;
            var label = new TextBlock
            {
                Text = labelText,
                Width = labelWidth,
                Height = labelHeight,
                FontSize = isMajor
                    ? GaugeVisualRules.OuterMajorMarkerFontSize
                    : GaugeVisualRules.OuterMinorMarkerFontSize,
                FontWeight = isMajor ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = markerBrush,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            var (_, _, left, top) = GaugeVisualRules.CalculateOuterScaleLabelPosition(
                angleDeg, labelWidth, labelHeight);
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top);
            KmhMarkersCanvas.Children.Add(label);
        }
    }

    private static bool IsMajorKmhTick(int kmh, int gaugeMax, int step)
    {
        if (kmh == 0 || kmh == gaugeMax)
        {
            return true;
        }

        return step >= 50 ? kmh % 100 == 0 : step > 0 && kmh % (step * 2) == 0;
    }
}