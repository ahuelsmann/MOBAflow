// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

/// <summary>
/// ESU CabControl-style round throttle control for DCC speed steps (0-28).
/// Features a circular dial with yellow center showing current speed,
/// arc indicator, tick marks, and DCC address/protocol display.
/// </summary>
public sealed partial class RoundThrottleControl : UserControl
{
    private const double CenterX = 120;
    private const double CenterY = 120;
    private const double ArcRadius = 80;
    private const double StartAngle = 135; // Bottom-left
    private const double EndAngle = 405;   // Bottom-right (270 degree sweep)
    private const double SweepAngle = 270;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(RoundThrottleControl),
            new PropertyMetadata(0, OnValueChanged));

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(nameof(MaxValue), typeof(int), typeof(RoundThrottleControl),
            new PropertyMetadata(28, OnValueChanged));

    public static readonly DependencyProperty AddressProperty =
        DependencyProperty.Register(nameof(Address), typeof(int), typeof(RoundThrottleControl),
            new PropertyMetadata(3, OnAddressChanged));

    public static readonly DependencyProperty ProtocolProperty =
        DependencyProperty.Register(nameof(Protocol), typeof(string), typeof(RoundThrottleControl),
            new PropertyMetadata("DCC 28", OnProtocolChanged));

    public static readonly DependencyProperty IsForwardProperty =
        DependencyProperty.Register(nameof(IsForward), typeof(bool), typeof(RoundThrottleControl),
            new PropertyMetadata(true, OnDirectionChanged));

    public static readonly DependencyProperty AccentColorProperty =
        DependencyProperty.Register(nameof(AccentColor), typeof(Color?), typeof(RoundThrottleControl),
            new PropertyMetadata(null, OnAccentColorChanged));

    public RoundThrottleControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int MaxValue
    {
        get => (int)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public int Address
    {
        get => (int)GetValue(AddressProperty);
        set => SetValue(AddressProperty, value);
    }

    public string Protocol
    {
        get => (string)GetValue(ProtocolProperty);
        set => SetValue(ProtocolProperty, value);
    }

    public bool IsForward
    {
        get => (bool)GetValue(IsForwardProperty);
        set => SetValue(IsForwardProperty, value);
    }

    public Color? AccentColor
    {
        get => (Color?)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DrawTickMarks();
        UpdateSpeedDisplay();
        UpdateSpeedArc();
        UpdateAddressDisplay();
        UpdateProtocolDisplay();
        UpdateDirectionArrow();
        ApplyAccentColor();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundThrottleControl control && control.IsLoaded)
        {
            control.UpdateSpeedDisplay();
            control.UpdateSpeedArc();
        }
    }

    private static void OnAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundThrottleControl control && control.IsLoaded)
        {
            control.UpdateAddressDisplay();
        }
    }

    private static void OnProtocolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundThrottleControl control && control.IsLoaded)
        {
            control.UpdateProtocolDisplay();
        }
    }

    private static void OnDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundThrottleControl control && control.IsLoaded)
        {
            control.UpdateDirectionArrow();
        }
    }

    private static void OnAccentColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundThrottleControl control && control.IsLoaded)
        {
            control.ApplyAccentColor();
        }
    }

    private void DrawTickMarks()
    {
        TickCanvas.Children.Clear();

        // Draw tick marks for each speed step
        for (int i = 0; i <= MaxValue; i++)
        {
            double angle = StartAngle + (SweepAngle * i / MaxValue);
            double radians = angle * Math.PI / 180;

            double innerRadius = (i % 7 == 0) ? 62 : 68; // Longer ticks at 0, 7, 14, 21, 28
            double outerRadius = 75;

            double x1 = CenterX + innerRadius * Math.Cos(radians);
            double y1 = CenterY + innerRadius * Math.Sin(radians);
            double x2 = CenterX + outerRadius * Math.Cos(radians);
            double y2 = CenterY + outerRadius * Math.Sin(radians);

            var tick = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                StrokeThickness = (i % 7 == 0) ? 2 : 1
            };

            TickCanvas.Children.Add(tick);

            // Add number labels at major ticks
            if (i % 7 == 0)
            {
                double labelRadius = 55;
                double labelX = CenterX + labelRadius * Math.Cos(radians) - 8;
                double labelY = CenterY + labelRadius * Math.Sin(radians) - 8;

                var label = new TextBlock
                {
                    Text = i.ToString(CultureInfo.InvariantCulture),
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    Width = 20,
                    TextAlignment = TextAlignment.Center
                };

                Canvas.SetLeft(label, labelX);
                Canvas.SetTop(label, labelY);
                TickCanvas.Children.Add(label);
            }
        }
    }

    private void UpdateSpeedDisplay()
    {
        SpeedText.Text = Value.ToString(CultureInfo.InvariantCulture);
    }

    private void UpdateSpeedArc()
    {
        if (MaxValue <= 0) return;

        double percentage = (double)Value / MaxValue;
        double sweepAngleCurrent = SweepAngle * percentage;

        if (sweepAngleCurrent < 1)
        {
            SpeedArc.Data = null;
            return;
        }

        double startRad = StartAngle * Math.PI / 180;
        double endRad = (StartAngle + sweepAngleCurrent) * Math.PI / 180;

        double x1 = CenterX + ArcRadius * Math.Cos(startRad);
        double y1 = CenterY + ArcRadius * Math.Sin(startRad);
        double x2 = CenterX + ArcRadius * Math.Cos(endRad);
        double y2 = CenterY + ArcRadius * Math.Sin(endRad);

        int largeArc = sweepAngleCurrent > 180 ? 1 : 0;

        string pathData = string.Format(
            CultureInfo.InvariantCulture,
            "M {0:F1},{1:F1} A {2},{2} 0 {3} 1 {4:F1},{5:F1}",
            x1, y1, ArcRadius, largeArc, x2, y2);

        SpeedArc.Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
            typeof(Geometry), pathData);
    }

    private void UpdateAddressDisplay()
    {
        AddressText.Text = string.Format(CultureInfo.InvariantCulture, "[{0:D4}]", Address);
    }

    private void UpdateProtocolDisplay()
    {
        ProtocolText.Text = Protocol;
    }

    private void UpdateDirectionArrow()
    {
        // Rotate arrow: up for forward, down for reverse
        if (DirectionArrow.RenderTransform is not RotateTransform)
        {
            DirectionArrow.RenderTransform = new RotateTransform();
            DirectionArrow.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        }

        ((RotateTransform)DirectionArrow.RenderTransform).Angle = IsForward ? 0 : 180;
    }

    private void ApplyAccentColor()
    {
        if (AccentColor.HasValue)
        {
            var brush = new SolidColorBrush(AccentColor.Value);
            CenterCircle.Fill = brush;
            DirectionArrow.Fill = brush;
        }
        else
        {
            // Default ESU yellow for center circle
            CenterCircle.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0));
            DirectionArrow.Fill = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        }
    }
}
