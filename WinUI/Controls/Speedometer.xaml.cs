namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Globalization;
using Windows.Foundation;
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

        var range = (double)(MaxValue - MinValue);
        if (range <= 0) return;

        var normalizedValue = Math.Clamp((Value - MinValue) / range, 0, 1);

        if (normalizedValue <= 0.001)
        {
            SpeedArc.Data = null;
            SpeedArc.Visibility = Visibility.Collapsed;
            return;
        }

        SpeedArc.Visibility = Visibility.Visible;

        const double centerX = 130;
        const double centerY = 130;
        const double outerRadius = 100;
        const double innerRadius = 92;
        const double lineSpacingDeg = 2;
        const double lineThickness = 2;
        const double startAngle = 180;

        var sweepAngle = normalizedValue * 180;
        var endAngle = startAngle - sweepAngle;

        var geometryGroup = new GeometryGroup();
        for (var angleDeg = startAngle; angleDeg >= endAngle; angleDeg -= lineSpacingDeg)
        {
            var angleRad = angleDeg * Math.PI / 180;
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            var startX = centerX + (innerRadius * cos);
            var startY = centerY - (innerRadius * sin);
            var endX = centerX + (outerRadius * cos);
            var endY = centerY - (outerRadius * sin);

            geometryGroup.Children.Add(new LineGeometry
            {
                StartPoint = new Point(startX, startY),
                EndPoint = new Point(endX, endY)
            });
        }

        SpeedArc.StrokeThickness = lineThickness;
        SpeedArc.Data = geometryGroup;

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
    /// Calculates the optimal km/h step for marker display.
    /// Goal: Display 8-10 markers on the gauge (never overloaded).
    /// </summary>
    private int CalculateOptimalKmhStep(int vmax)
    {
        // Adaptive step sizing based on max speed
        return vmax switch
        {
            <= 50 => 5,      // 0, 5, 10, 15... 50 (10 markers)
            <= 100 => 10,    // 0, 10, 20, 30... 100 (10 markers)
            <= 200 => 20,    // 0, 20, 40, 60... 200 (10 markers)
            <= 300 => 30,    // 0, 30, 60, 90... 300 (10 markers)
            _ => 50          // 0, 50, 100, 150... 330+ (7 markers)
        };
    }

    /// <summary>
    /// Renders DCC speed step markers dynamically based on SpeedSteps configuration.
    /// Markers are displayed as radial strokes starting from the inner edge of the arc.
    /// Uses AccentColor from current skin palette for visual consistency.
    /// Strokes: 8px long (starting at inner edge 92, extending to 100).
    /// </summary>
    private void RenderSpeedStepMarkers()
    {
        if (SpeedStepMarkersCanvas is null)
            return;

        // Clear existing markers
        SpeedStepMarkersCanvas.Children.Clear();

        // Arc parameters
        const double centerX = 130;
        const double centerY = 130;
        const double arcInnerRadius = 92;    // Inner edge of arc (100 - 8)
        const double markerLength = 8;       // 8px long strokes

        // Determine actual max step value and strategic positions
        var (maxStep, stepsToDisplay) = SpeedSteps switch
        {
            14 => (13, new[] { 0, 3, 7, 10, 13 }),
            28 => (27, new[] { 0, 7, 14, 21, 27 }),
            128 or _ => (126, new[] { 0, 32, 63, 95, 126 })
        };

        // Use AccentColor from accent brush (matching speedometer needle)
        var accentBrush = AccentColor.HasValue
            ? new SolidColorBrush(AccentColor.Value)
            : (Brush)Application.Current.Resources["AccentFillColorTertiaryBrush"];

        foreach (var step in stepsToDisplay)
        {
            // Calculate normalized position
            var normalized = maxStep > 0 ? (double)step / maxStep : 0;

            // Angle calculation: 180° (left, 0) to 0° (right, max)
            var angleDeg = 180 - (normalized * 180);
            var angleRad = angleDeg * Math.PI / 180;

            // Radial direction (from center outward)
            var radialX = Math.Cos(angleRad);
            var radialY = -Math.Sin(angleRad);

            // Start point at inner edge of arc (92), end point 8px outward (100)
            var startX = centerX + (arcInnerRadius * radialX);
            var startY = centerY + (arcInnerRadius * radialY);

            var endX = centerX + ((arcInnerRadius + markerLength) * radialX);
            var endY = centerY + ((arcInnerRadius + markerLength) * radialY);

            // Create radial stroke - single sharp line with accent color
            var line = new Line
            {
                X1 = startX,
                Y1 = startY,
                X2 = endX,
                Y2 = endY,
                Stroke = accentBrush,
                StrokeThickness = 2.5
            };
            SpeedStepMarkersCanvas.Children.Add(line);

            // Label position (offset radially inward for speed steps)
            const double labelDistance = 75;
            var labelX = centerX + (labelDistance * radialX);
            var labelY = centerY + (labelDistance * radialY);

            // Create step number label
            var label = new TextBlock
            {
                Text = step.ToString(),
                FontSize = 9,
                Foreground = accentBrush,
                Opacity = 0.85,
                TextAlignment = TextAlignment.Center
            };

            // Center the label
            Canvas.SetLeft(label, labelX - 8);
            Canvas.SetTop(label, labelY - 5);

            SpeedStepMarkersCanvas.Children.Add(label);
        }
    }

    /// <summary>
    /// Renders km/h markers dynamically based on VmaxKmh with adaptive step-sizing.
    /// Markers are displayed as radial strokes starting from the outer edge of the arc.
    /// Uses AccentLight color from current skin palette for visual consistency.
    /// Strokes: 6px long (starting at outer edge 108, extending to 100).
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
        const double arcOuterRadius = 108;   // Outer edge of arc (100 + 8)
        const double markerLength = 8;       // 8px long strokes

        // Calculate adaptive step size (goal: 8-10 markers)
        var kmhStep = CalculateOptimalKmhStep(VmaxKmh);

        // Generate km/h values: 0, step, 2*step, ... up to Vmax
        var kmhValues = new List<int>();
        for (int kmh = 0; kmh <= VmaxKmh; kmh += kmhStep)
        {
            kmhValues.Add(kmh);
        }
        // Ensure we include Vmax if it's not a multiple of step
        if (kmhValues.Count == 0 || kmhValues[^1] != VmaxKmh)
        {
            kmhValues.Add(VmaxKmh);
        }

        // Use AccentLight from current skin for markers (bright, visible color)
        var markerBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)); // Bright white with transparency
        var maxBrush = new SolidColorBrush(Color.FromArgb(255, 232, 17, 35)); // Red for MAX (constant)

        foreach (var kmh in kmhValues)
        {
            var isMax = kmh == VmaxKmh;
            
            // Normalize position: 0 km/h = left (180°), Vmax = right (0°)
            var percentage = VmaxKmh > 0 ? (double)kmh / VmaxKmh : 0;

            // Angle goes from 180 deg (left, 0 km/h) to 0 deg (right, Vmax)
            var angleDeg = 180 - (percentage * 180);
            var angleRad = angleDeg * Math.PI / 180;

            // Radial direction (from center outward)
            var radialX = Math.Cos(angleRad);
            var radialY = -Math.Sin(angleRad);

            // Start point at outer edge of arc (108), end point 8px inward (100)
            var startX = centerX + (arcOuterRadius * radialX);
            var startY = centerY + (arcOuterRadius * radialY);

            var endX = centerX + ((arcOuterRadius - markerLength) * radialX);
            var endY = centerY + ((arcOuterRadius - markerLength) * radialY);

            // Create radial stroke - single sharp line with luminous color
            var line = new Line
            {
                X1 = startX,
                Y1 = startY,
                X2 = endX,
                Y2 = endY,
                Stroke = isMax ? maxBrush : markerBrush,
                StrokeThickness = isMax ? 3.0 : 2.5
            };
            KmhMarkersCanvas.Children.Add(line);

            // Label position (offset radially outward, beyond the stroke)
            const double labelDistance = 125;
            var labelX = centerX + (labelDistance * radialX);
            var labelY = centerY + (labelDistance * radialY);

            // Create km/h number label
            var label = new TextBlock
            {
                Text = kmh.ToString(),
                FontSize = isMax ? 11 : 10,
                FontWeight = isMax ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                Foreground = isMax ? maxBrush : markerBrush,
                TextAlignment = TextAlignment.Center
            };

            // Center the label
            Canvas.SetLeft(label, labelX - 10);
            Canvas.SetTop(label, labelY - 8);

            KmhMarkersCanvas.Children.Add(label);
        }
    }
}
