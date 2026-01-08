// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

/// <summary>
/// A modern speedometer control for displaying locomotive speed.
/// Features an arc-based gauge with animated needle and digital display.
/// </summary>
public sealed partial class SpeedometerControl : UserControl
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

    public SpeedometerControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateNeedle();
        UpdateSpeedArc();
        UpdateDisplayText();
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

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.UpdateNeedle();
            control.UpdateSpeedArc();
        }
    }

    private static void OnDisplayValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.UpdateDisplayText();
        }
    }

    private void UpdateNeedle()
    {
        if (NeedleRotation is null) return;

        // Calculate angle: -90 deg (left, speed=0) to +90 deg (right, speed=max)
        var range = (double)(MaxValue - MinValue);
        if (range <= 0) return;

        var normalizedValue = Math.Clamp((Value - MinValue) / range, 0, 1);

        // Angle goes from -90 deg (left) to +90 deg (right)
        var angle = -90 + (normalizedValue * 180);

        NeedleRotation.Angle = angle;
    }

    private void UpdateSpeedArc()
    {
        if (SpeedArc is null) return;

        // Calculate the arc path based on current speed
        var range = (double)(MaxValue - MinValue);
        if (range <= 0) return;

        var normalizedValue = Math.Clamp((Value - MinValue) / range, 0, 1);

        if (normalizedValue <= 0.001)
        {
            SpeedArc.Data = null;
            return;
        }

        // Arc parameters - match XAML canvas coordinates (center 130,130, radius 100)
        const double centerX = 130;
        const double centerY = 130;
        const double radius = 100;
        const double startAngle = 180; // Left side (0 speed)

        var sweepAngle = normalizedValue * 180; // Up to 180 deg for full speed
        var endAngle = startAngle - sweepAngle;

        // Calculate start and end points
        var startRad = startAngle * Math.PI / 180;
        var endRad = endAngle * Math.PI / 180;

        var startX = centerX + radius * Math.Cos(startRad);
        var startY = centerY - radius * Math.Sin(startRad);
        var endX = centerX + radius * Math.Cos(endRad);
        var endY = centerY - radius * Math.Sin(endRad);

        // Large arc flag (1 if > 180 deg, but we never exceed 180 deg)
        var largeArc = sweepAngle > 180 ? 1 : 0;

        // Sweep flag: 1 = clockwise (arc curves outward from center)
        const int sweepFlag = 1;

        // Create path data
        var pathData = string.Format(
            CultureInfo.InvariantCulture,
            "M {0:F1},{1:F1} A {2},{2} 0 {3} {4} {5:F1},{6:F1}",
            startX, startY, radius, largeArc, sweepFlag, endX, endY);

        try
        {
            SpeedArc.Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                typeof(Geometry), pathData);
        }
        catch
        {
            // Fallback if path parsing fails
            SpeedArc.Data = null;
        }

        // Update arc color based on speed (green to yellow to red)
        UpdateArcColor(normalizedValue);
    }

    private void UpdateArcColor(double normalizedValue)
    {
        if (SpeedArc is null) return;

        Windows.UI.Color color;

        if (normalizedValue < 0.5)
        {
            // Green to Yellow (0-50%)
            var t = normalizedValue * 2;
            color = Windows.UI.Color.FromArgb(255,
                (byte)(76 + t * 179),   // 76 to 255 (green to yellow R)
                (byte)(175 - t * 75),   // 175 to 100 (green to yellow G)
                80);                     // Blue stays low
        }
        else
        {
            // Yellow to Red (50-100%)
            var t = (normalizedValue - 0.5) * 2;
            color = Windows.UI.Color.FromArgb(255,
                255,                     // Red stays max
                (byte)(100 - t * 100),  // 100 to 0 (yellow to red G)
                (byte)(80 - t * 80));   // Blue goes to 0
        }

        SpeedArc.Stroke = new SolidColorBrush(color);
    }

    private void UpdateDisplayText()
    {
        if (SpeedText is null) return;
        SpeedText.Text = DisplayValue.ToString();
    }
}
