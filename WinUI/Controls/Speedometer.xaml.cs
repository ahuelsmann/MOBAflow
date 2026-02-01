namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Globalization;
using Windows.UI;

/// <summary>
/// A modern speedometer control for displaying locomotive speed.
/// Features an arc-based gauge with animated needle and digital display.
/// Supports dynamic theme colors via AccentColor property.
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
        ApplyAccentColor();
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

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.UpdateNeedle();
            control.UpdateSpeedArc();
            
            // Update km/h markers when MaxValue changes (Vmax)
            if (e.Property == MaxValueProperty)
            {
                control.RenderKmhMarkers();
            }
        }
    }

    private static void OnDisplayValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.UpdateDisplayText();
        }
    }

    private static void OnAccentColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpeedometerControl control)
        {
            control.ApplyAccentColor();
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
        if (d is SpeedometerControl control)
        {
            control.RenderKmhMarkers();
        }
    }

    private void ApplyAccentColor()
    {
        if (AccentColor is not Color color)
            return;

        var brush = new SolidColorBrush(color);

        // Apply to needle
        if (Needle is Path needle)
        {
            needle.Fill = brush;
        }

        // Apply to center circle stroke
        if (CenterCircle is Ellipse circle)
        {
            circle.Stroke = brush;
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

        var startX = centerX + (radius * Math.Cos(startRad));
        var startY = centerY - (radius * Math.Sin(startRad));
        var endX = centerX + (radius * Math.Cos(endRad));
        var endY = centerY - (radius * Math.Sin(endRad));

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
            SpeedArc.Data = (Geometry)XamlBindingHelper.ConvertValue(
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

        Color color;

        if (normalizedValue < 0.5)
        {
            // Green to Yellow (0-50%)
            var t = normalizedValue * 2;
            color = Color.FromArgb(255,
                (byte)(76 + (t * 179)),   // 76 to 255 (green to yellow R)
                (byte)(175 - (t * 75)),   // 175 to 100 (green to yellow G)
                80);                     // Blue stays low
        }
        else
        {
            // Yellow to Red (50-100%)
            var t = (normalizedValue - 0.5) * 2;
            color = Color.FromArgb(255,
                255,                     // Red stays max
                (byte)(100 - (t * 100)),  // 100 to 0 (yellow to red G)
                (byte)(80 - (t * 80)));   // Blue goes to 0
        }

        SpeedArc.Stroke = new SolidColorBrush(color);
    }

    private void UpdateDisplayText()
    {
        if (SpeedText is null) return;
        SpeedText.Text = DisplayValue.ToString();
    }

    /// <summary>
    /// Renders DCC speed step markers dynamically based on SpeedSteps configuration.
    /// Markers are positioned on inner ring for clear separation from km/h markers.
    /// </summary>
    private void RenderSpeedStepMarkers()
    {
        if (SpeedStepMarkersCanvas is null)
            return;

        // Clear existing markers
        SpeedStepMarkersCanvas.Children.Clear();

        // Arc parameters (inner ring)
        const double centerX = 130;
        const double centerY = 130;
        const double innerRadius = 96;   // Further inside (was 106)
        const double markerLength = 5;   // Shorter tick marks
        const double labelRadius = innerRadius + 12; // Position for step numbers

        // Determine actual max step value
        var maxStep = SpeedSteps switch
        {
            14 => 13,
            28 => 27,
            128 => 126,
            _ => 126
        };

        // Strategic positions to display
        var stepsToDisplay = SpeedSteps switch
        {
            14 => new[] { 0, 3, 7, 10, 13 },
            28 => new[] { 0, 7, 14, 21, 27 },
            128 => new[] { 0, 32, 63, 95, 126 },
            _ => new[] { 0, 32, 63, 95, 126 }
        };

        // Use accent color for speed step markers (different from km/h)
        var accentBrush = AccentColor.HasValue
            ? new SolidColorBrush(AccentColor.Value)
            : (Brush)Application.Current.Resources["AccentFillColorTertiaryBrush"];

        foreach (var step in stepsToDisplay)
        {
            // Calculate normalized position
            var normalized = maxStep > 0 ? (double)step / maxStep : 0;

            // Angle calculation
            var angleDeg = 180 - (normalized * 180);
            var angleRad = angleDeg * Math.PI / 180;

            // Inner point (start of tick mark)
            var innerX = centerX + (innerRadius * Math.Cos(angleRad));
            var innerY = centerY - (innerRadius * Math.Sin(angleRad));

            // Outer point (end of tick mark)
            var outerX = centerX + ((innerRadius + markerLength) * Math.Cos(angleRad));
            var outerY = centerY - ((innerRadius + markerLength) * Math.Sin(angleRad));

            // Create tick mark line
            var line = new Line
            {
                X1 = innerX,
                Y1 = innerY,
                X2 = outerX,
                Y2 = outerY,
                Stroke = accentBrush,
                StrokeThickness = 1.2,
                Opacity = 0.7  // Slightly transparent to differentiate
            };

            SpeedStepMarkersCanvas.Children.Add(line);

            // Label position
            var labelX = centerX + (labelRadius * Math.Cos(angleRad));
            var labelY = centerY - (labelRadius * Math.Sin(angleRad));

            // Create step number label
            var label = new TextBlock
            {
                Text = step.ToString(),
                FontSize = 8,  // Smaller font
                Foreground = accentBrush,
                Opacity = 0.8,
                TextAlignment = TextAlignment.Center
            };

            // Center the label
            Canvas.SetLeft(label, labelX - 8);
            Canvas.SetTop(label, labelY - 5);

            SpeedStepMarkersCanvas.Children.Add(label);
        }
    }

    /// <summary>
    /// Renders km/h markers dynamically based on VmaxKmh.
    /// Displays 5 markers at 0%, 25%, 50%, 75%, 100% of Vmax.
    /// Positioned on outer ring for clear separation from speed step markers.
    /// </summary>
    private void RenderKmhMarkers()
    {
        if (KmhMarkersCanvas is null)
            return;

        // Clear existing markers
        KmhMarkersCanvas.Children.Clear();

        // Arc parameters
        const double centerX = 130;
        const double centerY = 130;
        const double outerRadius = 114;   // Outside the stroke (100 + strokeThickness)
        const double markerLength = 8;    // Longer tick marks than speed steps
        const double labelRadius = outerRadius + 16; // Position for km/h numbers

        // Calculate km/h values to display (0%, 25%, 50%, 75%, 100% of Vmax)
        var percentages = new[] { 0, 0.25, 0.5, 0.75, 1.0 };
        var primaryBrush = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        var maxBrush = new SolidColorBrush(Color.FromArgb(255, 232, 17, 35)); // Red for MAX

        foreach (var percentage in percentages)
        {
            var kmh = (int)Math.Round(VmaxKmh * percentage);
            var isMax = percentage >= 0.99; // Is this the MAX marker?

            // Angle goes from 180 deg (left, 0%) to 0 deg (right, 100%)
            var angleDeg = 180 - (percentage * 180);
            var angleRad = angleDeg * Math.PI / 180;

            // Inner point (start of tick mark)
            var innerX = centerX + (outerRadius * Math.Cos(angleRad));
            var innerY = centerY - (outerRadius * Math.Sin(angleRad));

            // Outer point (end of tick mark)
            var outerX = centerX + ((outerRadius + markerLength) * Math.Cos(angleRad));
            var outerY = centerY - ((outerRadius + markerLength) * Math.Sin(angleRad));

            // Create tick mark line
            var line = new Line
            {
                X1 = innerX,
                Y1 = innerY,
                X2 = outerX,
                Y2 = outerY,
                Stroke = isMax ? maxBrush : primaryBrush,
                StrokeThickness = isMax ? 2.5 : 2
            };

            KmhMarkersCanvas.Children.Add(line);

            // Label position (further out)
            var labelX = centerX + (labelRadius * Math.Cos(angleRad));
            var labelY = centerY - (labelRadius * Math.Sin(angleRad));

            // Create km/h number label
            var label = new TextBlock
            {
                Text = kmh.ToString(),
                FontSize = isMax ? 11 : 12,
                FontWeight = isMax ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                Foreground = isMax ? maxBrush : primaryBrush,
                TextAlignment = TextAlignment.Center
            };

            // Adjust label position to center it
            Canvas.SetLeft(label, labelX - 12);
            Canvas.SetTop(label, labelY - 7);

            KmhMarkersCanvas.Children.Add(label);
        }
    }
}
