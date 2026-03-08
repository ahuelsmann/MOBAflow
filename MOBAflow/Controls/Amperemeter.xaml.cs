namespace Moba.WinUI.Controls;

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
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateNeedle();
        UpdateCurrentArc();
        UpdateDisplayText();
        ApplyAccentColor();
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

            // Update mA markers when MaxValue changes
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
            control.ApplyAccentColor();
        }
    }

    private void ApplyAccentColor()
    {
        if (AccentColor is not { } color)
            return;

        var brush = new SolidColorBrush(color);

        // Apply to needle
        if (Needle is { } needle)
        {
            needle.Fill = brush;
        }

        // Apply to center circle stroke
        if (CenterCircle is { } circle)
        {
            circle.Stroke = brush;
        }
    }

    private void UpdateNeedle()
    {
        if (NeedleRotation is null) return;

        // Calculate angle: -90 deg (left, current=0) to +90 deg (right, current=max)
        var range = (double)(MaxValue - MinValue);
        if (range <= 0) return;

        var normalizedValue = Math.Clamp((Value - MinValue) / range, 0, 1);

        // Angle goes from -90 deg (left) to +90 deg (right)
        var angle = -90 + (normalizedValue * 180);

        NeedleRotation.Angle = angle;
    }

    private void UpdateCurrentArc()
    {
        if (CurrentArc is null) return;

        var range = (double)(MaxValue - MinValue);
        if (range <= 0) return;

        var normalizedValue = Math.Clamp((Value - MinValue) / range, 0, 1);

        if (normalizedValue <= 0.001)
        {
            CurrentArc.Data = null;
            CurrentArc.Visibility = Visibility.Collapsed;
            return;
        }

        CurrentArc.Visibility = Visibility.Visible;

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

        CurrentArc.StrokeThickness = lineThickness;
        CurrentArc.Data = geometryGroup;

        UpdateArcColor(normalizedValue);
    }

    private void UpdateArcColor(double normalizedValue)
    {
        if (CurrentArc is null) return;

        Color color;

        // Color coding for current load:
        // 0-50%: Green (low load)
        // 50-80%: Yellow (medium load)
        // 80-100%: Red (high load/overload warning)

        if (normalizedValue < 0.5)
        {
            // Green to Yellow (0-50%)
            var t = normalizedValue * 2;
            color = Color.FromArgb(255,
                (byte)(76 + (t * 179)),   // 76 to 255 (green to yellow R)
                (byte)(175 - (t * 75)),   // 175 to 100 (green to yellow G)
                80);                      // Blue stays low
        }
        else if (normalizedValue < 0.8)
        {
            // Yellow to Orange (50-80%)
            var t = (normalizedValue - 0.5) / 0.3;
            color = Color.FromArgb(255,
                255,                      // Red stays max
                (byte)(100 - (t * 40)),   // 100 to 60 (yellow to orange)
                (byte)(80 - (t * 80)));   // Blue goes to 0
        }
        else
        {
            // Orange to Red (80-100%)
            var t = (normalizedValue - 0.8) / 0.2;
            color = Color.FromArgb(255,
                255,                      // Red stays max
                (byte)(60 - (t * 60)),    // 60 to 0 (orange to red)
                0);                       // Blue stays 0
        }

        CurrentArc.Stroke = new SolidColorBrush(color);
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
    private int CalculateOptimalMilliampereStep(int maxCurrent)
    {
        // Adaptive step sizing based on max current
        return maxCurrent switch
        {
            <= 500 => 50,      // 0, 50, 100, 150... 500 (10 markers)
            <= 1000 => 100,    // 0, 100, 200, 300... 1000 (10 markers)
            <= 2000 => 200,    // 0, 200, 400, 600... 2000 (10 markers)
            <= 3000 => 250,    // 0, 250, 500, 750... 3000 (12 markers)
            <= 5000 => 500,    // 0, 500, 1000, 1500... 5000 (10 markers)
            _ => 1000          // 0, 1000, 2000, 3000... (flexible)
        };
    }

    /// <summary>
    /// Renders mA markers dynamically based on MaxValue with adaptive step sizing.
    /// </summary>
    private void RenderMilliampereMarkers()
    {
        if (MilliampereMarkersCanvas is null)
            return;

        // Clear existing markers
        MilliampereMarkersCanvas.Children.Clear();

        // Arc parameters
        const double centerX = 130;
        const double centerY = 130;
        const double arcOuterRadius = 108;
        const double markerLength = 8;

        // Calculate adaptive step size
        var mAStep = CalculateOptimalMilliampereStep(MaxValue);

        // Generate mA values
        var mAValues = new List<int>();
        for (int mA = 0; mA <= MaxValue; mA += mAStep)
        {
            mAValues.Add(mA);
        }

        // Ensure we include MaxValue if it's not a multiple of step
        if (mAValues.Count == 0 || mAValues[^1] != MaxValue)
        {
            mAValues.Add(MaxValue);
        }

        var markerBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
        var maxBrush = new SolidColorBrush(Color.FromArgb(255, 232, 17, 35)); // Red for MAX

        foreach (var mA in mAValues)
        {
            var isMax = mA == MaxValue;

            // Normalize position
            var percentage = MaxValue > 0 ? (double)mA / MaxValue : 0;

            // Angle: 180° (left, 0 mA) to 0° (right, max)
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
                Stroke = isMax ? maxBrush : markerBrush,
                StrokeThickness = isMax ? 3.0 : 2.5
            };
            MilliampereMarkersCanvas.Children.Add(line);

            // Label position
            const double labelDistance = 125;
            var labelX = centerX + (labelDistance * radialX);
            var labelY = centerY + (labelDistance * radialY);

            var label = new TextBlock
            {
                Text = mA.ToString(),
                FontSize = isMax ? 11 : 10,
                FontWeight = isMax ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                Foreground = isMax ? maxBrush : markerBrush,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(label, labelX - 10);
            Canvas.SetTop(label, labelY - 8);

            MilliampereMarkersCanvas.Children.Add(label);
        }
    }
}
