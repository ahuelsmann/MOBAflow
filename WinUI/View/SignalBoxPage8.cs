// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.Backend.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Model;

using System.Globalization;

using Windows.Foundation;
using Windows.UI;

/// <summary>
/// Signal Box Page 8 - MOBAixl Minimal (Ultra-Clean Design)
/// Maximum visual clarity with minimal visual elements.
/// Features: Very thin lines, monochrome palette with accent colors,
/// subtle indicators, clean geometric shapes, no decorative elements.
/// Designed for maximum focus on track occupancy and signal states.
/// </summary>
public sealed class SignalBoxPage8 : SignalBoxPageBase2
{
    private static readonly SignalBoxColorScheme MinimalColors = new()
    {
        Background = Color.FromArgb(255, 8, 8, 10),
        PanelBackground = Color.FromArgb(255, 14, 14, 18),
        MessagePanelBackground = Color.FromArgb(255, 6, 6, 8),
        Border = Color.FromArgb(255, 28, 28, 35),
        GridLine = Color.FromArgb(6, 30, 30, 40),
        Accent = Color.FromArgb(255, 100, 200, 255),
        ButtonBackground = Color.FromArgb(255, 16, 16, 22),
        ButtonHover = Color.FromArgb(255, 24, 24, 32),
        ButtonBorder = Color.FromArgb(30, 40, 40, 60),
        TrackFree = Color.FromArgb(255, 60, 60, 70),
        TrackOccupied = Color.FromArgb(255, 255, 60, 60),
        RouteSet = Color.FromArgb(255, 60, 255, 120),
        RouteClearing = Color.FromArgb(255, 255, 220, 80),
        Blocked = Color.FromArgb(255, 100, 160, 255),
        SignalRed = Color.FromArgb(255, 255, 80, 80),
        SignalGreen = Color.FromArgb(255, 80, 255, 120),
        SignalYellow = Color.FromArgb(255, 255, 220, 80)
    };

    public SignalBoxPage8(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
    }

    protected override string StyleName => "MOBAixl Minimal";

    protected override string SubtitleText => "Schematische Ansicht";

    protected override SignalBoxColorScheme Colors => MinimalColors;

    protected override string[] StationAreas => ["A", "B"];

    protected override UIElement CreateToolboxIcon(SignalBoxElementType type)
    {
        var canvas = new Canvas { Width = 48, Height = 40 };

        switch (type)
        {
            case SignalBoxElementType.TrackStraight:
                // Ultra-thin line
                canvas.Children.Add(CreateThinTrack(8, 20, 40, 20, Colors.TrackFree, 1.5));
                break;

            case SignalBoxElementType.TrackCurve45:
                canvas.Children.Add(new Path
                {
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(8, 20),
                            Segments = { new ArcSegment { Point = new Point(32, 30), Size = new Size(20, 20), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 1.5
                });
                break;

            case SignalBoxElementType.TrackCurve90:
                canvas.Children.Add(new Path
                {
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(8, 20),
                            Segments = { new ArcSegment { Point = new Point(24, 36), Size = new Size(16, 16), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 1.5
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(CreateThinTrack(8, 20, 32, 20, Colors.TrackFree, 1.5));
                // Minimal end marker - just a vertical line
                canvas.Children.Add(CreateThinTrack(36, 14, 36, 26, Colors.SignalRed, 2));
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(CreateThinTrack(8, 22, 40, 22, Colors.TrackFree, 1.5));
                canvas.Children.Add(CreateThinTrack(24, 22, 36, 12, Color.FromArgb(80, 60, 60, 70), 1));
                // Minimal switch point - small dot
                canvas.Children.Add(new Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 22);
                Canvas.SetTop(canvas.Children[^1], 20);
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(CreateThinTrack(8, 18, 40, 18, Colors.TrackFree, 1.5));
                canvas.Children.Add(CreateThinTrack(24, 18, 36, 28, Color.FromArgb(80, 60, 60, 70), 1));
                canvas.Children.Add(new Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 22);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(CreateThinTrack(8, 12, 40, 28, Colors.TrackFree, 1.5));
                canvas.Children.Add(CreateThinTrack(8, 28, 40, 12, Color.FromArgb(80, 60, 60, 70), 1));
                canvas.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 21.5);
                Canvas.SetTop(canvas.Children[^1], 17.5);
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(CreateThinTrack(8, 20, 40, 20, Colors.TrackFree, 1.5));
                canvas.Children.Add(CreateThinTrack(24, 6, 24, 34, Colors.TrackFree, 1.5));
                break;

            case SignalBoxElementType.SignalMain:
                // Minimal signal - just a colored circle
                canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 15);
                break;

            case SignalBoxElementType.SignalDistant:
                // Hollow circle for distant signal
                canvas.Children.Add(new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Stroke = new SolidColorBrush(Colors.SignalYellow),
                    StrokeThickness = 2
                });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 15);
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.SignalGreen) });
                Canvas.SetLeft(canvas.Children[^1], 20);
                Canvas.SetTop(canvas.Children[^1], 12);
                canvas.Children.Add(new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 220, 80)),
                    StrokeThickness = 1.5
                });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 23);
                break;

            case SignalBoxElementType.SignalShunting:
                // Two small dots
                canvas.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 17);
                canvas.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 26);
                Canvas.SetTop(canvas.Children[^1], 17);
                break;

            case SignalBoxElementType.SignalSpeed:
                // Minimal speed - just a number
                canvas.Children.Add(new TextBlock
                {
                    Text = "8",
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Light,
                    Foreground = new SolidColorBrush(Colors.SignalYellow)
                });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 12);
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(CreateThinTrack(8, 16, 40, 16, Colors.TrackFree, 1.5));
                // Thin platform edge
                canvas.Children.Add(CreateThinTrack(10, 24, 38, 24, Microsoft.UI.Colors.White, 1));
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(CreateThinTrack(8, 20, 40, 20, Colors.TrackFree, 1.5));
                // Minimal feedback indicator
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 12);
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(new TextBlock
                {
                    Text = "01",
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.Light,
                    Foreground = new SolidColorBrush(Colors.Accent)
                });
                Canvas.SetLeft(canvas.Children[^1], 18);
                Canvas.SetTop(canvas.Children[^1], 14);
                break;

            default:
                canvas.Children.Add(new Rectangle
                {
                    Width = 32, Height = 24,
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 1
                });
                Canvas.SetLeft(canvas.Children[^1], 8);
                Canvas.SetTop(canvas.Children[^1], 8);
                break;
        }

        return canvas;
    }

    protected override FrameworkElement CreateElementVisual(SignalBoxElement element)
    {
        var container = new Grid
        {
            Width = GridCellSize,
            Height = GridCellSize,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };

        var trackColor = GetStateColor(element.State, Colors);

        switch (element.Type)
        {
            case SignalBoxElementType.TrackStraight:
                container.Children.Add(CreateThinTrack(6, 30, 54, 30, trackColor, 2));
                break;

            case SignalBoxElementType.TrackCurve45:
            case SignalBoxElementType.TrackCurve90:
                container.Children.Add(new Path
                {
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(6, 30),
                            Segments = { new ArcSegment { Point = new Point(30, 54), Size = new Size(24, 24), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 2
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                container.Children.Add(CreateThinTrack(6, 30, 46, 30, trackColor, 2));
                container.Children.Add(CreateThinTrack(50, 20, 50, 40, Colors.SignalRed, 3));
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateMinimalSwitch(element));
                break;

            case SignalBoxElementType.SwitchDouble:
                container.Children.Add(CreateThinTrack(6, 10, 54, 50, trackColor, 2));
                container.Children.Add(CreateThinTrack(6, 50, 54, 10, Color.FromArgb(60, trackColor.R, trackColor.G, trackColor.B), 1.5));
                container.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(container.Children[^1], 27);
                Canvas.SetTop(container.Children[^1], 27);
                break;

            case SignalBoxElementType.SwitchCrossing:
                container.Children.Add(CreateThinTrack(6, 30, 54, 30, trackColor, 2));
                container.Children.Add(CreateThinTrack(30, 6, 30, 54, trackColor, 2));
                break;

            case SignalBoxElementType.SignalMain:
                container.Children.Add(CreateMinimalMainSignal(element));
                break;

            case SignalBoxElementType.SignalDistant:
                container.Children.Add(CreateMinimalDistantSignal(element));
                break;

            case SignalBoxElementType.SignalCombined:
                container.Children.Add(CreateMinimalCombinedSignal(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateMinimalShuntingSignal(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateMinimalFeedback(element));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(CreateThinTrack(6, 26, 54, 26, trackColor, 2));
                container.Children.Add(CreateThinTrack(10, 38, 50, 38, Microsoft.UI.Colors.White, 1.5));
                break;

            case SignalBoxElementType.Label:
                var txt = new TextBlock
                {
                    Text = element.Name.Length > 0 ? element.Name : "01",
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.Light,
                    Foreground = new SolidColorBrush(Colors.Accent)
                };
                Canvas.SetLeft(txt, 18);
                Canvas.SetTop(txt, 24);
                container.Children.Add(txt);
                break;

            default:
                container.Children.Add(new Rectangle
                {
                    Width = GridCellSize - 8,
                    Height = GridCellSize - 8,
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 1
                });
                Canvas.SetLeft(container.Children[^1], 4);
                Canvas.SetTop(container.Children[^1], 4);
                break;
        }

        container.RenderTransform = new RotateTransform { Angle = element.Rotation, CenterX = GridCellSize / 2, CenterY = GridCellSize / 2 };
        SetupElementInteraction(container, element);

        return container;
    }

    private Grid CreateMinimalSwitch(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = GetStateColor(element.State, Colors);
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        // Main track
        var mainColor = isStraight ? trackColor : Color.FromArgb(50, trackColor.R, trackColor.G, trackColor.B);
        grid.Children.Add(CreateThinTrack(6, 30, 54, 30, mainColor, 2));

        // Diverging track
        var divergeColor = !isStraight ? trackColor : Color.FromArgb(40, 60, 60, 70);
        grid.Children.Add(CreateThinTrack(30, 30, 50, isLeft ? 14 : 46, divergeColor, 1.5));

        // Minimal switch indicator
        var indicatorColor = isStraight ? Colors.SignalGreen : Colors.SignalYellow;
        grid.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(indicatorColor) });
        Canvas.SetLeft(grid.Children[^1], 27);
        Canvas.SetTop(grid.Children[^1], 27);

        return grid;
    }

    private Grid CreateMinimalMainSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var isGo = element.SignalAspect == SignalAspect.Hp1;
        var signalColor = isGo ? Colors.SignalGreen : Colors.SignalRed;

        grid.Children.Add(new Ellipse { Width = 16, Height = 16, Fill = new SolidColorBrush(signalColor) });
        Canvas.SetLeft(grid.Children[^1], 22);
        Canvas.SetTop(grid.Children[^1], 22);

        return grid;
    }

    private Grid CreateMinimalDistantSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var isExpectStop = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
        var ringColor = isExpectStop ? Colors.SignalYellow : Color.FromArgb(60, 255, 220, 80);

        grid.Children.Add(new Ellipse
        {
            Width = 16,
            Height = 16,
            Stroke = new SolidColorBrush(ringColor),
            StrokeThickness = 3
        });
        Canvas.SetLeft(grid.Children[^1], 22);
        Canvas.SetTop(grid.Children[^1], 22);

        return grid;
    }

    private Grid CreateMinimalCombinedSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var topColor = element.SignalAspect switch
        {
            SignalAspect.Hp0 => Colors.SignalRed,
            SignalAspect.Ks1 => Colors.SignalGreen,
            _ => Color.FromArgb(50, 128, 128, 128)
        };

        grid.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(topColor) });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 16);

        var bottomColor = element.SignalAspect == SignalAspect.Ks2 ? Colors.SignalYellow : Color.FromArgb(40, 255, 220, 80);
        grid.Children.Add(new Ellipse
        {
            Width = 8,
            Height = 8,
            Stroke = new SolidColorBrush(bottomColor),
            StrokeThickness = 2
        });
        Canvas.SetLeft(grid.Children[^1], 26);
        Canvas.SetTop(grid.Children[^1], 36);

        return grid;
    }

    private Grid CreateMinimalShuntingSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var isOn = element.SignalAspect == SignalAspect.Sh1;
        var dotColor = isOn ? Microsoft.UI.Colors.White : Color.FromArgb(40, 255, 255, 255);

        grid.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(dotColor) });
        Canvas.SetLeft(grid.Children[^1], 20);
        Canvas.SetTop(grid.Children[^1], 26);

        grid.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(dotColor) });
        Canvas.SetLeft(grid.Children[^1], 32);
        Canvas.SetTop(grid.Children[^1], 26);

        return grid;
    }

    private Grid CreateMinimalFeedback(SignalBoxElement element)
    {
        var grid = new Grid();
        var isOccupied = element.State == SignalBoxElementState.Occupied;
        var trackColor = isOccupied ? Colors.TrackOccupied : Colors.TrackFree;

        grid.Children.Add(CreateThinTrack(6, 30, 54, 30, trackColor, 2));

        // Minimal feedback dot
        var ledColor = isOccupied ? Colors.SignalRed : Color.FromArgb(40, 255, 60, 60);
        grid.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(ledColor) });
        Canvas.SetLeft(grid.Children[^1], 25);
        Canvas.SetTop(grid.Children[^1], 18);

        // Subtle address label
        var addr = new TextBlock
        {
            Text = element.Address > 0 ? element.Address.ToString(CultureInfo.InvariantCulture) : "?",
            FontSize = 8,
            FontWeight = Microsoft.UI.Text.FontWeights.Light,
            Foreground = new SolidColorBrush(Colors.Accent)
        };
        Canvas.SetLeft(addr, 27);
        Canvas.SetTop(addr, 44);
        grid.Children.Add(addr);

        return grid;
    }
}
