namespace Moba.WinUI.View;

using Controls;

using Domain;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

/// <summary>
/// Partial class containing all element graphics creation methods.
/// </summary>
sealed partial class SignalBoxPage
{
    #region Element Graphics - Simplified Track Display

    // Connection points for all elements (60x60 cell):
    // Left:  (0, 30)   Right: (60, 30)
    // Top:   (30, 0)   Bottom: (30, 60)

    // NOTE: Colors are now obtained from resources instead of hardcoded
    // See: ThemeResources in App.xaml or theme-specific ResourceDictionaries
    private static SolidColorBrush GetTrackBrush() => (SolidColorBrush)Application.Current.Resources["SignalBoxTrackColor"];
    private static SolidColorBrush GetTrackActiveBrush() => (SolidColorBrush)Application.Current.Resources["SignalBoxTrackActiveColor"];
    private const double TrackThickness = 4;

    private static Canvas CreateStraightTrackGraphic()
    {
        // Horizontal: Left (0,30) -> Right (60,30)
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Line
        {
            X1 = 0,
            Y1 = 30,
            X2 = 60,
            Y2 = 30,
            Stroke = GetTrackBrush(),
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
            Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry),
                "M 0,30 Q 30,30 30,60"),
            Stroke = GetTrackBrush(),
            StrokeThickness = TrackThickness
        });
        return canvas;
    }

    private static Canvas CreateSwitchGraphic(SbSwitch element)
    {
        // Switch: Left (0,30) -> Right (60,30) + branch
        var canvas = new Canvas { Width = 60, Height = 60 };
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isDiverging = element.SwitchPosition != SwitchPosition.Straight;

        // Main track through
        canvas.Children.Add(new Line
        {
            X1 = 0,
            Y1 = 30,
            X2 = 60,
            Y2 = 30,
            Stroke = isStraight ? GetTrackActiveBrush() : GetTrackBrush(),
            StrokeThickness = TrackThickness
        });

        // Branch: From center (30,30) to diverging
        canvas.Children.Add(new Line
        {
            X1 = 30,
            Y1 = 30,
            X2 = 60,
            Y2 = element.SwitchPosition == SwitchPosition.DivergingLeft ? 0 : 60,
            Stroke = isDiverging ? GetTrackActiveBrush() : GetTrackBrush(),
            StrokeThickness = TrackThickness
        });

        return canvas;
    }

    /// <summary>
    /// Creates signal graphic based on signal type and aspect.
    /// </summary>
    private Canvas CreateSignalGraphic(SbSignal element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        var signalScreen = new KsSignalScreen
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

    private static Canvas CreateFeedbackGraphic()
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Track
        canvas.Children.Add(new Line
        {
            X1 = 0,
            Y1 = 30,
            X2 = 60,
            Y2 = 30,
            Stroke = GetTrackBrush(),
            StrokeThickness = TrackThickness
        });

        // Feedback marker (small circle)
        var markerFill = (SolidColorBrush)Application.Current.Resources["AccentFillColorSecondaryBrush"];
        var markerStroke = (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"];

        var marker = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = markerFill,
            Stroke = markerStroke,
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
        var fillBrush = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        var strokeBrush = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];

        canvas.Children.Add(new Rectangle
        {
            Width = 56,
            Height = 56,
            Fill = fillBrush,
            Stroke = strokeBrush,
            StrokeThickness = 1,
            Opacity = 0.6
        });
        Canvas.SetLeft(canvas.Children[^1], 2);
        Canvas.SetTop(canvas.Children[^1], 2);
        return canvas;
    }

    #endregion
}