namespace Moba.WinUI.View;

using Domain;

using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using SharedUI.ViewModel;

using Windows.UI;

/// <summary>
/// Partial class containing all element graphics creation methods.
/// </summary>
public sealed partial class SignalBoxPage
{
    #region Element Graphics - Simplified Track Display

    // Connection points for all elements (60x60 cell):
    // Left:  (0, 30)   Right: (60, 30)
    // Top:   (30, 0)   Bottom: (30, 60)

    private static readonly SolidColorBrush TrackBrush = new(Color.FromArgb(255, 200, 200, 200));
    private static readonly SolidColorBrush TrackActiveBrush = new(Color.FromArgb(255, 0, 200, 0));
    private static readonly SolidColorBrush BufferStopBrush = new(Color.FromArgb(255, 200, 60, 60));
    private const double TrackThickness = 4;

    private static Canvas CreateStraightTrackGraphic()
    {
        // Horizontal: Left (0,30) -> Right (60,30)
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });
        return canvas;
    }

    private static Canvas CreateCurve45Graphic()
    {
        // 45 degree curve: Left (0,30) -> Right-Top diagonal (60,0)
        // Flatter curve for gradual direction change
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Path
        {
            Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry),
                "M 0,30 Q 30,30 60,0"),
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });
        return canvas;
    }

    private static Canvas CreateCurve90Graphic()
    {
        // 90 degree curve: Left (0,30) -> Top (30,0)
        // Tighter curve for sharper direction change (quarter circle)
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Path
        {
            Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry),
                "M 0,30 Q 0,0 30,0"),
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });
        return canvas;
    }

    private static Canvas CreateBufferStopGraphic()
    {
        // Buffer stop: Left (0,30) -> Center, then stop
        var canvas = new Canvas { Width = 60, Height = 60 };

        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 45, Y2 = 30,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        canvas.Children.Add(new Line
        {
            X1 = 45, Y1 = 18, X2 = 45, Y2 = 42,
            Stroke = BufferStopBrush,
            StrokeThickness = 5
        });

        return canvas;
    }

    private static Canvas CreateSwitchGraphic(SignalBoxPlanElementViewModel element, bool isLeft)
    {
        // Switch: Left (0,30) -> Right (60,30) + branch to Top/Bottom
        var canvas = new Canvas { Width = 60, Height = 60 };
        var isStraight = element.SwitchPosition == Domain.SwitchPosition.Straight;
        var isDiverging = isLeft
            ? element.SwitchPosition == Domain.SwitchPosition.DivergingLeft
            : element.SwitchPosition == Domain.SwitchPosition.DivergingRight;

        // Main track through
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Branch: From center (30,30) to Right-Top (60,0) or Right-Bottom (60,60)
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 60, Y2 = isLeft ? 0 : 60,
            Stroke = isDiverging ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        return canvas;
    }

    private static Canvas CreateThreeWaySwitchGraphic(SignalBoxPlanElementViewModel element)
    {
        // Three-way switch: Straight + branch up + branch down
        var canvas = new Canvas { Width = 60, Height = 60 };
        var isStraight = element.SwitchPosition == Domain.SwitchPosition.Straight;
        var isLeft = element.SwitchPosition == Domain.SwitchPosition.DivergingLeft;
        var isRight = element.SwitchPosition == Domain.SwitchPosition.DivergingRight;

        // Main track through: (0,30) -> (60,30)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Branch up: (30,30) -> (60,0)
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 60, Y2 = 0,
            Stroke = isLeft ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Branch down: (30,30) -> (60,60)
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 60, Y2 = 60,
            Stroke = isRight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        return canvas;
    }

    private static Canvas CreateDoubleSwitchGraphic(SignalBoxPlanElementViewModel element)
    {
        // DKW (Double crossover switch): 4 possible routes
        var canvas = new Canvas { Width = 60, Height = 60 };
        var isStraight = element.SwitchPosition == Domain.SwitchPosition.Straight;
        var isCrossing = element.SwitchPosition != Domain.SwitchPosition.Straight;

        // Upper track: (0,15) -> (60,15)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 15, X2 = 60, Y2 = 15,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Lower track: (0,45) -> (60,45)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 45, X2 = 60, Y2 = 45,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Cross connection upper-left to lower-right: (0,15) -> (60,45)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 15, X2 = 60, Y2 = 45,
            Stroke = isCrossing ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness - 1
        });

        // Cross connection lower-left to upper-right: (0,45) -> (60,15)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 45, X2 = 60, Y2 = 15,
            Stroke = isCrossing ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness - 1
        });

        return canvas;
    }

    private static Canvas CreateCrossingGraphic()
    {
        // Crossing: Horizontal + Vertical
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Horizontal: (0,30) -> (60,30)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Vertical: (30,0) -> (30,60)
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 0, X2 = 30, Y2 = 60,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        return canvas;
    }

    private Canvas CreateKsMainSignalGraphic(SignalBoxPlanElementViewModel element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        var signalScreen = new Controls.KsSignalScreen
        {
            Width = 50,
            Height = 50,
            Aspect = element.SignalAspect.ToString()
        };
        Canvas.SetLeft(signalScreen, 5);
        Canvas.SetTop(signalScreen, 5);
        canvas.Children.Add(signalScreen);

        return canvas;
    }

    private Canvas CreateKsDistantSignalGraphic(SignalBoxPlanElementViewModel element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        var signalScreen = new Controls.KsSignalScreen
        {
            Width = 50,
            Height = 50,
            Aspect = element.SignalAspect.ToString()
        };
        Canvas.SetLeft(signalScreen, 5);
        Canvas.SetTop(signalScreen, 5);
        canvas.Children.Add(signalScreen);

        return canvas;
    }

    private static Canvas CreateShuntSignalGraphic(SignalBoxPlanElementViewModel element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Signal mast
        canvas.Children.Add(new Rectangle { Width = 4, Height = 30, Fill = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44)) });
        Canvas.SetLeft(canvas.Children[^1], 28);
        Canvas.SetTop(canvas.Children[^1], 25);

        // Signal housing
        canvas.Children.Add(new Rectangle { Width = 22, Height = 16, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), RadiusX = 2, RadiusY = 2 });
        Canvas.SetLeft(canvas.Children[^1], 19);
        Canvas.SetTop(canvas.Children[^1], 12);

        // Two white LEDs (Ra12)
        canvas.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = new SolidColorBrush(Colors.White) });
        Canvas.SetLeft(canvas.Children[^1], 22);
        Canvas.SetTop(canvas.Children[^1], 17);

        canvas.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = new SolidColorBrush(Colors.White) });
        Canvas.SetLeft(canvas.Children[^1], 31);
        Canvas.SetTop(canvas.Children[^1], 17);

        return canvas;
    }

    private static Canvas CreateBlockSignalGraphic(SignalBoxPlanElementViewModel element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Signal mast
        canvas.Children.Add(new Rectangle { Width = 4, Height = 30, Fill = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44)) });
        Canvas.SetLeft(canvas.Children[^1], 28);
        Canvas.SetTop(canvas.Children[^1], 25);

        // Round housing
        canvas.Children.Add(new Ellipse { Width = 22, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), Stroke = new SolidColorBrush(Color.FromArgb(255, 96, 96, 96)), StrokeThickness = 1 });
        Canvas.SetLeft(canvas.Children[^1], 19);
        Canvas.SetTop(canvas.Children[^1], 8);

        // Red LED
        canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) });
        Canvas.SetLeft(canvas.Children[^1], 25);
        Canvas.SetTop(canvas.Children[^1], 14);

        return canvas;
    }

    private static Microsoft.UI.Xaml.UIElement CreateKsSignalScreenGraphic(SignalBoxPlanElementViewModel element)
    {
        var signalScreen = new Controls.KsSignalScreen
        {
            Width = 55,
            Height = 55,
            Aspect = element.SignalAspect.ToString()
        };

        return signalScreen;
    }

    private static Canvas CreatePlatformGraphic()
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Track
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 40, X2 = 60, Y2 = 40,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Platform (simple rectangle)
        canvas.Children.Add(new Rectangle
        {
            Width = 50,
            Height = 12,
            Fill = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
            StrokeThickness = 1
        });
        Canvas.SetLeft(canvas.Children[^1], 5);
        Canvas.SetTop(canvas.Children[^1], 15);

        return canvas;
    }

    private static Canvas CreateFeedbackGraphic()
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Track
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Feedback marker (small circle)
        var marker = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(Color.FromArgb(255, 255, 200, 0)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 200, 150, 0)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(marker, 25);
        Canvas.SetTop(marker, 25);
        canvas.Children.Add(marker);

        return canvas;
    }

    private static Canvas CreatePlaceholderGraphic()
    {
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Rectangle
        {
            Width = 56,
            Height = 56,
            Fill = new SolidColorBrush(Color.FromArgb(30, 100, 100, 100)),
            Stroke = new SolidColorBrush(Color.FromArgb(60, 150, 150, 150)),
            StrokeThickness = 1
        });
        Canvas.SetLeft(canvas.Children[^1], 2);
        Canvas.SetTop(canvas.Children[^1], 2);
        return canvas;
    }

    #endregion
}
