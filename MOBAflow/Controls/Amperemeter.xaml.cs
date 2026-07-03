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
/// Amperemeter control for displaying Z21 main track current consumption.
/// Features an arc-based gauge with animated needle and digital display.
/// Default range: 0-3000 mA (typical Z21 main track current).
/// Supports dynamic theme colors via AccentColor property.
/// </summary>
internal sealed partial class AmperemeterControl
{
    /// <summary>
    /// Minimum current value (typically 0 mA).
    /// </summary>
    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register(nameof(MinValue), typeof(int), typeof(AmperemeterControl),
            new PropertyMetadata(0, OnValueChanged));

    /// <summary>
    /// Maximum current value (typically 3000 mA for Z21 main track).
    /// </summary>
    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(nameof(MaxValue), typeof(int), typeof(AmperemeterControl),
            new PropertyMetadata(3000, OnValueChanged));

    /// <summary>
    /// Current value in milliamperes (mA).
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(AmperemeterControl),
            new PropertyMetadata(0, OnValueChanged));

    /// <summary>
    /// Accent color for needle and center circle. When set, overrides ThemeResource.
    /// </summary>
    public static readonly DependencyProperty AccentColorProperty =
        DependencyProperty.Register(nameof(AccentColor), typeof(Color?), typeof(AmperemeterControl),
            new PropertyMetadata(null, OnAccentColorChanged));

    public AmperemeterControl()
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
        RenderMilliampereMarkers();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged += OnActualThemeChanged;
        UpdateNeedle();
        UpdateCurrentArc();
        UpdateDisplayText();
        UpdateGaugeColors();
        RenderMilliampereMarkers();
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

    /// <summary>
    /// Gets or sets the accent color for the needle. Null uses default ThemeResource.
    /// </summary>
    public Color? AccentColor
    {
        get => (Color?)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AmperemeterControl control)
        {
            control.UpdateNeedle();
            control.UpdateCurrentArc();
            control.UpdateDisplayText();
            control.UpdateGaugeColors();

            if (e.Property == MaxValueProperty)
            {
                control.RenderMilliampereMarkers();
            }
        }
    }

    private static void OnAccentColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AmperemeterControl control)
        {
            control.UpdateGaugeColors();
        }
    }

    private void UpdateGaugeColors()
    {
        var range = (double)(MaxValue - MinValue);
        var normalized = range > 0 ? Math.Clamp((Value - MinValue) / range, 0, 1) : 0;
        var isIdle = Value <= MinValue;
        var isDanger = normalized > GaugeVisualRules.DangerNormalizedThreshold;

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

        var range = (double)(MaxValue - MinValue);
        if (range <= 0) return;

        var normalizedValue = Math.Clamp((Value - MinValue) / range, 0, 1);
        var angle = -90 + (normalizedValue * 180);

        NeedleRotation.Angle = angle;
    }

    private void UpdateCurrentArc()
    {
        if (CurrentArc is null) return;

        var range = (double)(MaxValue - MinValue);
        if (range <= 0) return;

        var normalizedValue = Math.Clamp((Value - MinValue) / range, 0, 1);
        var arcGeometry = GaugeArcGeometryBuilder.CreateSweepArc(normalizedValue);

        if (arcGeometry is null)
        {
            CurrentArc.Data = null;
            CurrentArc.Visibility = Visibility.Collapsed;
            return;
        }

        CurrentArc.Visibility = Visibility.Visible;
        CurrentArc.StrokeThickness = GaugeArcGeometryBuilder.ArcStrokeThickness;
        CurrentArc.Data = arcGeometry;

        UpdateArcColor(normalizedValue);
    }

    private void UpdateArcColor(double normalizedValue)
    {
        if (CurrentArc is null) return;

        CurrentArc.Stroke = new SolidColorBrush(
            GaugeVisualRules.ResolveCurrentArcColor(this, normalizedValue));
    }

    private void UpdateDisplayText()
    {
        if (CurrentText is null) return;
        CurrentText.Text = Value.ToString();
    }

    /// <summary>
    /// Calculates the optimal mA step for marker display.
    /// Goal: Display 8-10 markers on the gauge (never overloaded).
    /// </summary>
    private static int CalculateOptimalMilliampereStep(int maxCurrent) => maxCurrent switch
    {
        <= 500 => 50,
        <= 1000 => 100,
        <= 2000 => 200,
        <= 3000 => 250,
        <= 5000 => 500,
        _ => 1000
    };

    /// <summary>
    /// Renders mA markers dynamically based on MaxValue with adaptive step sizing.
    /// </summary>
    private void RenderMilliampereMarkers()
    {
        if (MilliampereMarkersCanvas is null)
            return;

        MilliampereMarkersCanvas.Children.Clear();

        const double centerX = GaugeVisualRules.GaugeCenterX;
        const double centerY = GaugeVisualRules.GaugeCenterY;
        const double arcOuterRadius = GaugeVisualRules.OuterArcRadius;
        const double markerLength = 8;

        var mAStep = CalculateOptimalMilliampereStep(MaxValue);

        var mAValues = new List<int>();
        for (int mA = 0; mA <= MaxValue; mA += mAStep)
        {
            mAValues.Add(mA);
        }

        if (mAValues.Count == 0 || mAValues[^1] != MaxValue)
        {
            mAValues.Add(MaxValue);
        }

        var markerBrush = GaugeVisualRules.CreatePrimaryMarkerBrush(this);

        foreach (var mA in mAValues)
        {
            var isMax = mA == MaxValue;
            var percentage = MaxValue > 0 ? (double)mA / MaxValue : 0;

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
                StrokeThickness = isMax ? 2.8 : 1.8
            };
            MilliampereMarkersCanvas.Children.Add(line);

            var labelText = mA.ToString();
            var labelWidth = GaugeVisualRules.CalculateMarkerLabelWidth(labelText);

            var labelHeight = GaugeVisualRules.OuterMarkerLabelHeight;
            var label = new TextBlock
            {
                Text = labelText,
                Width = labelWidth,
                Height = labelHeight,
                FontSize = isMax
                    ? GaugeVisualRules.OuterMajorMarkerFontSize
                    : GaugeVisualRules.OuterMinorMarkerFontSize,
                FontWeight = isMax ? FontWeights.Bold : FontWeights.Normal,
                Foreground = markerBrush,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            var (_, _, left, top) = GaugeVisualRules.CalculateOuterScaleLabelPosition(
                angleDeg, labelWidth, labelHeight);
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top);

            MilliampereMarkersCanvas.Children.Add(label);
        }
    }
}
