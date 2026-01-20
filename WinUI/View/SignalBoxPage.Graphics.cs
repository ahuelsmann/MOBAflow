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

    private static Canvas CreateCurve90Graphic()
    {
        // 90 degree curve centered in cell
        // Connection points: Left (0,30) and Bottom (30,60)
        // This creates a quarter circle that connects to straight tracks
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Path
        {
            Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry),
                "M 0,30 Q 30,30 30,60"),
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

    private static Canvas CreateSwitchGraphic(SbSwitchViewModel element)
    {
        // Switch: Left (0,30) -> Right (60,30) + branch
        var canvas = new Canvas { Width = 60, Height = 60 };
        var isStraight = element.SwitchPosition == Domain.SwitchPosition.Straight;
        var isDiverging = element.SwitchPosition != Domain.SwitchPosition.Straight;

        // Main track through
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Branch: From center (30,30) to diverging
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 60, Y2 = element.SwitchPosition == Domain.SwitchPosition.DivergingLeft ? 0 : 60,
            Stroke = isDiverging ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        return canvas;
    }

    /// <summary>
    /// Creates signal graphic based on signal type and aspect.
    /// </summary>
    private Canvas CreateSignalGraphic(SbSignalViewModel element)
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
    }    private static Canvas CreateCrossingGraphic()
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
