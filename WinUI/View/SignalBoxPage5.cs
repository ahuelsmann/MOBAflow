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
/// Signal Box Page 5 - MOBAixl Classic (ESTW-ZN Inspired)
/// Pure black background with bright yellow tracks, red occupied sections,
/// green route set sections. Classic German railway interlocking aesthetics
/// with thin track lines and simple geometric signal indicators.
/// Inspired by ESTW-ZN (Zentrales Netz) control room displays.
/// </summary>
public sealed class SignalBoxPage5 : SignalBoxPageBase2
{
    private static readonly SignalBoxColorScheme ClassicColors = new()
    {
        Background = Color.FromArgb(255, 0, 0, 0),
        PanelBackground = Color.FromArgb(255, 15, 15, 15),
        MessagePanelBackground = Color.FromArgb(255, 5, 5, 5),
        Border = Color.FromArgb(255, 40, 40, 40),
        GridLine = Color.FromArgb(15, 60, 60, 60),
        Accent = Color.FromArgb(255, 255, 220, 0),
        ButtonBackground = Color.FromArgb(255, 25, 25, 25),
        ButtonHover = Color.FromArgb(255, 45, 45, 45),
        ButtonBorder = Color.FromArgb(60, 80, 80, 80),
        TrackFree = Color.FromArgb(255, 255, 220, 0),
        TrackOccupied = Color.FromArgb(255, 255, 0, 0),
        RouteSet = Color.FromArgb(255, 0, 255, 0),
        RouteClearing = Color.FromArgb(255, 255, 255, 0),
        Blocked = Color.FromArgb(255, 0, 150, 255),
        SignalRed = Color.FromArgb(255, 255, 0, 0),
        SignalGreen = Color.FromArgb(255, 0, 255, 0),
        SignalYellow = Color.FromArgb(255, 255, 255, 0)
    };

    public SignalBoxPage5(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
    }

    protected override string StyleName => "MOBAixl Classic";

    protected override string SubtitleText => "Zentralnetz-Stellwerk";

    protected override SignalBoxColorScheme Colors => ClassicColors;

    protected override string[] StationAreas => ["91", "92"];

    protected override UIElement CreateToolboxIcon(SignalBoxElementType type)
    {
        var canvas = new Canvas { Width = 48, Height = 40 };

        switch (type)
        {
            case SignalBoxElementType.TrackStraight:
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.TrackFree, 3));
                break;

            case SignalBoxElementType.TrackCurve45:
                canvas.Children.Add(new Path
                {
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(6, 20),
                            Segments = { new ArcSegment { Point = new Point(34, 32), Size = new Size(22, 22), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 3
                });
                break;

            case SignalBoxElementType.TrackCurve90:
                canvas.Children.Add(new Path
                {
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(6, 20),
                            Segments = { new ArcSegment { Point = new Point(24, 38), Size = new Size(18, 18), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 3
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(CreateThinTrack(6, 20, 30, 20, Colors.TrackFree, 3));
                // End stop X marker
                canvas.Children.Add(CreateThinTrack(32, 12, 40, 28, Colors.SignalRed, 2));
                canvas.Children.Add(CreateThinTrack(40, 12, 32, 28, Colors.SignalRed, 2));
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(CreateThinTrack(6, 22, 42, 22, Colors.TrackFree, 3));
                canvas.Children.Add(CreateThinTrack(22, 22, 38, 10, Colors.RouteSet, 2));
                // Switch motor indicator
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 19);
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(CreateThinTrack(6, 18, 42, 18, Colors.TrackFree, 3));
                canvas.Children.Add(CreateThinTrack(22, 18, 38, 30, Colors.RouteSet, 2));
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 15);
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(CreateThinTrack(6, 10, 42, 30, Colors.TrackFree, 2));
                canvas.Children.Add(CreateThinTrack(6, 30, 42, 10, Colors.TrackFree, 2));
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 20);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.TrackFree, 3));
                canvas.Children.Add(CreateThinTrack(24, 4, 24, 36, Colors.TrackFree, 3));
                break;

            case SignalBoxElementType.SignalMain:
                // Classic signal button with red LED
                canvas.Children.Add(new Rectangle { Width = 12, Height = 20, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 18);
                Canvas.SetTop(canvas.Children[^1], 10);
                canvas.Children.Add(CreateClassicLed(21, 14, Colors.SignalRed, 6));
                break;

            case SignalBoxElementType.SignalDistant:
                // Yellow disc for distant signal
                canvas.Children.Add(new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = new SolidColorBrush(Colors.SignalYellow),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
                    StrokeThickness = 1
                });
                Canvas.SetLeft(canvas.Children[^1], 15);
                Canvas.SetTop(canvas.Children[^1], 11);
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(new Rectangle { Width = 12, Height = 26, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 18);
                Canvas.SetTop(canvas.Children[^1], 7);
                canvas.Children.Add(CreateClassicLed(21, 11, Colors.SignalGreen, 6));
                canvas.Children.Add(CreateClassicLed(21, 23, Color.FromArgb(40, 255, 255, 0), 6));
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 22, Height = 12, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 13);
                Canvas.SetTop(canvas.Children[^1], 14);
                canvas.Children.Add(CreateClassicLed(17, 17, Microsoft.UI.Colors.White, 5));
                canvas.Children.Add(CreateClassicLed(27, 17, Microsoft.UI.Colors.White, 5));
                break;

            case SignalBoxElementType.SignalSpeed:
                // Triangle with number
                var triangle = new Path
                {
                    Fill = new SolidColorBrush(Colors.SignalYellow),
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(24, 8),
                            Segments = { new LineSegment { Point = new Point(34, 32) }, new LineSegment { Point = new Point(14, 32) } },
                            IsClosed = true
                        }}
                    }
                };
                canvas.Children.Add(triangle);
                canvas.Children.Add(new TextBlock { Text = "8", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(CreateThinTrack(6, 14, 42, 14, Colors.TrackFree, 3));
                canvas.Children.Add(new Rectangle { Width = 32, Height = 4, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 8);
                Canvas.SetTop(canvas.Children[^1], 26);
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.TrackFree, 3));
                canvas.Children.Add(CreateClassicLed(20, 12, Colors.SignalRed, 10));
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(new Rectangle
                {
                    Width = 28, Height = 14,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                    Stroke = new SolidColorBrush(Colors.Accent),
                    StrokeThickness = 1
                });
                Canvas.SetLeft(canvas.Children[^1], 10);
                Canvas.SetTop(canvas.Children[^1], 13);
                canvas.Children.Add(CreateTrackNumber("01", Colors.Accent));
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 13);
                break;

            default:
                canvas.Children.Add(new Rectangle
                {
                    Width = 36, Height = 28,
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 1
                });
                Canvas.SetLeft(canvas.Children[^1], 6);
                Canvas.SetTop(canvas.Children[^1], 6);
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
                container.Children.Add(CreateThinTrack(4, 30, 56, 30, trackColor, 4));
                break;

            case SignalBoxElementType.TrackCurve45:
            case SignalBoxElementType.TrackCurve90:
                container.Children.Add(new Path
                {
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(4, 30),
                            Segments = { new ArcSegment { Point = new Point(30, 56), Size = new Size(26, 26), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 4
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                container.Children.Add(CreateThinTrack(4, 30, 40, 30, trackColor, 4));
                container.Children.Add(CreateThinTrack(44, 18, 54, 42, Colors.SignalRed, 3));
                container.Children.Add(CreateThinTrack(54, 18, 44, 42, Colors.SignalRed, 3));
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateClassicSwitch(element));
                break;

            case SignalBoxElementType.SwitchDouble:
                container.Children.Add(CreateThinTrack(4, 8, 56, 52, trackColor, 3));
                container.Children.Add(CreateThinTrack(4, 52, 56, 8, trackColor, 3));
                container.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(container.Children[^1], 25);
                Canvas.SetTop(container.Children[^1], 25);
                break;

            case SignalBoxElementType.SwitchCrossing:
                container.Children.Add(CreateThinTrack(4, 30, 56, 30, trackColor, 4));
                container.Children.Add(CreateThinTrack(30, 4, 30, 56, trackColor, 4));
                break;

            case SignalBoxElementType.SignalMain:
                container.Children.Add(CreateClassicMainSignal(element));
                break;

            case SignalBoxElementType.SignalDistant:
                container.Children.Add(CreateClassicDistantSignal(element));
                break;

            case SignalBoxElementType.SignalCombined:
                container.Children.Add(CreateClassicCombinedSignal(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateClassicShuntingSignal(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateClassicFeedback(element));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(CreateThinTrack(4, 24, 56, 24, trackColor, 4));
                container.Children.Add(new Rectangle { Width = 48, Height = 6, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(container.Children[^1], 6);
                Canvas.SetTop(container.Children[^1], 38);
                break;

            case SignalBoxElementType.Label:
                var bg = new Rectangle
                {
                    Width = 44, Height = 18,
                    Fill = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)),
                    Stroke = new SolidColorBrush(Colors.Accent),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(bg, 8);
                Canvas.SetTop(bg, 21);
                container.Children.Add(bg);
                var txt = CreateTrackNumber(element.Name.Length > 0 ? element.Name : "01", Colors.Accent);
                txt.FontSize = 11;
                Canvas.SetLeft(txt, 14);
                Canvas.SetTop(txt, 22);
                container.Children.Add(txt);
                break;

            default:
                container.Children.Add(new Rectangle
                {
                    Width = GridCellSize - 4,
                    Height = GridCellSize - 4,
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 1
                });
                break;
        }

        container.RenderTransform = new RotateTransform { Angle = element.Rotation, CenterX = GridCellSize / 2, CenterY = GridCellSize / 2 };
        SetupElementInteraction(container, element);

        return container;
    }

    private Grid CreateClassicSwitch(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = GetStateColor(element.State, Colors);
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        // Main track
        var mainColor = isStraight ? trackColor : Color.FromArgb(80, trackColor.R, trackColor.G, trackColor.B);
        grid.Children.Add(CreateThinTrack(4, 30, 56, 30, mainColor, 4));

        // Diverging track
        var divergeColor = !isStraight ? trackColor : Color.FromArgb(60, 120, 120, 120);
        grid.Children.Add(CreateThinTrack(30, 30, 52, isLeft ? 12 : 48, divergeColor, 3));

        // Switch motor indicator
        var indicatorColor = isStraight ? Colors.SignalGreen : Colors.SignalYellow;
        grid.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(indicatorColor) });
        Canvas.SetLeft(grid.Children[^1], 26);
        Canvas.SetTop(grid.Children[^1], 26);

        return grid;
    }

    private Grid CreateClassicMainSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        // Signal button housing
        grid.Children.Add(new Rectangle { Width = 20, Height = 32, Fill = new SolidColorBrush(Color.FromArgb(255, 35, 35, 35)) });
        Canvas.SetLeft(grid.Children[^1], 20);
        Canvas.SetTop(grid.Children[^1], 14);

        // Red LED (top)
        var redOn = element.SignalAspect == SignalAspect.Hp0;
        grid.Children.Add(CreateClassicLed(26, 18, redOn ? Colors.SignalRed : Color.FromArgb(40, 255, 0, 0), 10));

        // Green LED (bottom)
        var greenOn = element.SignalAspect == SignalAspect.Hp1;
        grid.Children.Add(CreateClassicLed(26, 32, greenOn ? Colors.SignalGreen : Color.FromArgb(40, 0, 255, 0), 10));

        return grid;
    }

    private Grid CreateClassicDistantSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var isExpectStop = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
        grid.Children.Add(new Ellipse
        {
            Width = 24,
            Height = 24,
            Fill = new SolidColorBrush(isExpectStop ? Colors.SignalYellow : Color.FromArgb(40, 255, 255, 0)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            StrokeThickness = 1
        });
        Canvas.SetLeft(grid.Children[^1], 18);
        Canvas.SetTop(grid.Children[^1], 18);

        return grid;
    }

    private Grid CreateClassicCombinedSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 18, Height = 40, Fill = new SolidColorBrush(Color.FromArgb(255, 35, 35, 35)) });
        Canvas.SetLeft(grid.Children[^1], 21);
        Canvas.SetTop(grid.Children[^1], 10);

        // Top LED
        var topColor = element.SignalAspect switch
        {
            SignalAspect.Hp0 => Colors.SignalRed,
            SignalAspect.Ks1 => Colors.SignalGreen,
            _ => Color.FromArgb(40, 128, 128, 128)
        };
        grid.Children.Add(CreateClassicLed(25, 15, topColor, 10));

        // Bottom LED
        var bottomColor = element.SignalAspect == SignalAspect.Ks2 ? Colors.SignalYellow : Color.FromArgb(40, 255, 255, 0);
        grid.Children.Add(CreateClassicLed(25, 35, bottomColor, 10));

        return grid;
    }

    private Grid CreateClassicShuntingSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 32, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(255, 35, 35, 35)) });
        Canvas.SetLeft(grid.Children[^1], 14);
        Canvas.SetTop(grid.Children[^1], 21);

        var isOn = element.SignalAspect == SignalAspect.Sh1;
        grid.Children.Add(CreateClassicLed(19, 25, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(40, 255, 255, 255), 8));
        grid.Children.Add(CreateClassicLed(33, 25, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(40, 255, 255, 255), 8));

        return grid;
    }

    private Grid CreateClassicFeedback(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = element.State == SignalBoxElementState.Occupied ? Colors.TrackOccupied : Colors.TrackFree;

        grid.Children.Add(CreateThinTrack(4, 30, 56, 30, trackColor, 4));

        var ledColor = element.State == SignalBoxElementState.Occupied ? Colors.SignalRed : Color.FromArgb(60, 255, 0, 0);
        grid.Children.Add(CreateClassicLed(23, 18, ledColor, 14));

        var addr = CreateTrackNumber(element.Address > 0 ? element.Address.ToString(CultureInfo.InvariantCulture) : "?", Colors.Accent);
        addr.FontSize = 9;
        Canvas.SetLeft(addr, 26);
        Canvas.SetTop(addr, 44);
        grid.Children.Add(addr);

        return grid;
    }
}
