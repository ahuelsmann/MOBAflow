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
/// Signal Box Page 7 - MOBAixl Operations (High-Density Operations Style)
/// Dense track layouts with multiple color states for different track conditions.
/// Features: Compact kilometer markers, detailed switch positions, dense labeling,
/// multi-color track states (green=free, red=occupied, yellow=route clearing, blue=blocked).
/// Inspired by high-density railway control room operations displays.
/// </summary>
public sealed class SignalBoxPage7 : SignalBoxPageBase2
{
    private static readonly SignalBoxColorScheme OperationsColors = new()
    {
        Background = Color.FromArgb(255, 5, 8, 12),
        PanelBackground = Color.FromArgb(255, 12, 16, 22),
        MessagePanelBackground = Color.FromArgb(255, 4, 6, 10),
        Border = Color.FromArgb(255, 30, 40, 50),
        GridLine = Color.FromArgb(8, 40, 60, 80),
        Accent = Color.FromArgb(255, 255, 180, 0),
        ButtonBackground = Color.FromArgb(255, 18, 24, 32),
        ButtonHover = Color.FromArgb(255, 28, 38, 50),
        ButtonBorder = Color.FromArgb(40, 50, 70, 90),
        TrackFree = Color.FromArgb(255, 0, 200, 80),
        TrackOccupied = Color.FromArgb(255, 255, 30, 30),
        RouteSet = Color.FromArgb(255, 0, 255, 0),
        RouteClearing = Color.FromArgb(255, 255, 200, 0),
        Blocked = Color.FromArgb(255, 80, 160, 255),
        SignalRed = Color.FromArgb(255, 255, 0, 0),
        SignalGreen = Color.FromArgb(255, 0, 255, 0),
        SignalYellow = Color.FromArgb(255, 255, 200, 0)
    };

    public SignalBoxPage7(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
    }

    protected override string StyleName => "MOBAixl Operations";

    protected override string SubtitleText => "Leitstelle Betrieb";

    protected override SignalBoxColorScheme Colors => OperationsColors;

    protected override string[] StationAreas => ["KEI", "WEI", "HAN"];

    protected override UIElement CreateToolboxIcon(SignalBoxElementType type)
    {
        var canvas = new Canvas { Width = 48, Height = 40 };

        switch (type)
        {
            case SignalBoxElementType.TrackStraight:
                // Green track with km markers
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.TrackFree, 2));
                // Km marker dots
                canvas.Children.Add(CreateClassicLed(14, 18, Microsoft.UI.Colors.White, 3));
                canvas.Children.Add(CreateClassicLed(34, 18, Microsoft.UI.Colors.White, 3));
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
                    StrokeThickness = 2
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
                    StrokeThickness = 2
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(CreateThinTrack(6, 20, 30, 20, Colors.TrackFree, 2));
                // Red bar end stop
                canvas.Children.Add(new Rectangle { Width = 4, Height = 14, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(canvas.Children[^1], 34);
                Canvas.SetTop(canvas.Children[^1], 13);
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(CreateThinTrack(6, 22, 42, 22, Colors.RouteSet, 2));
                canvas.Children.Add(CreateThinTrack(22, 22, 36, 12, Colors.RouteClearing, 2));
                // Switch position number
                canvas.Children.Add(CreateKmMarker("W1", 16, 26));
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(CreateThinTrack(6, 18, 42, 18, Colors.RouteSet, 2));
                canvas.Children.Add(CreateThinTrack(22, 18, 36, 28, Colors.RouteClearing, 2));
                canvas.Children.Add(CreateKmMarker("W2", 16, 22));
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(CreateThinTrack(6, 10, 42, 30, Colors.RouteSet, 2));
                canvas.Children.Add(CreateThinTrack(6, 30, 42, 10, Colors.TrackFree, 2));
                canvas.Children.Add(CreateKmMarker("DKW", 17, 32));
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.TrackFree, 2));
                canvas.Children.Add(CreateThinTrack(24, 4, 24, 36, Colors.TrackFree, 2));
                break;

            case SignalBoxElementType.SignalMain:
                // Signal with HP designation
                canvas.Children.Add(new Rectangle { Width = 10, Height = 18, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 8);
                canvas.Children.Add(CreateKmMarker("HP", 18, 28));
                break;

            case SignalBoxElementType.SignalDistant:
                canvas.Children.Add(new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = new SolidColorBrush(Colors.SignalYellow)
                });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 10);
                canvas.Children.Add(CreateKmMarker("VS", 18, 26));
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(new Rectangle { Width = 10, Height = 22, Fill = new SolidColorBrush(Colors.SignalGreen) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 6);
                canvas.Children.Add(CreateKmMarker("KS", 18, 30));
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 16, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 14);
                canvas.Children.Add(CreateClassicLed(19, 15, Microsoft.UI.Colors.White, 4));
                canvas.Children.Add(CreateClassicLed(27, 15, Microsoft.UI.Colors.White, 4));
                canvas.Children.Add(CreateKmMarker("SH", 18, 24));
                break;

            case SignalBoxElementType.SignalSpeed:
                canvas.Children.Add(new Rectangle { Width = 16, Height = 16, Fill = new SolidColorBrush(Colors.SignalYellow) });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 10);
                canvas.Children.Add(new TextBlock { Text = "8", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 11);
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(CreateThinTrack(6, 14, 42, 14, Colors.TrackFree, 2));
                canvas.Children.Add(new Rectangle { Width = 28, Height = 3, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 10);
                Canvas.SetTop(canvas.Children[^1], 24);
                canvas.Children.Add(CreateKmMarker("Gl1", 16, 30));
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.TrackOccupied, 2));
                canvas.Children.Add(new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.TrackOccupied) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 8);
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(CreateKmMarker("354.7", 12, 16));
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

    private static Border CreateKmMarker(string text, double x, double y)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            Padding = new Thickness(2, 0, 2, 0),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 7,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
            }
        };
        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        return border;
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
                container.Children.Add(CreateThinTrack(4, 30, 56, 30, trackColor, 3));
                // Km marker dots
                container.Children.Add(CreateClassicLed(18, 28, Microsoft.UI.Colors.White, 4));
                container.Children.Add(CreateClassicLed(42, 28, Microsoft.UI.Colors.White, 4));
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
                    StrokeThickness = 3
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                container.Children.Add(CreateThinTrack(4, 30, 44, 30, trackColor, 3));
                // Red bar end stop
                container.Children.Add(new Rectangle { Width = 6, Height = 20, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(container.Children[^1], 48);
                Canvas.SetTop(container.Children[^1], 20);
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateOperationsSwitch(element));
                break;

            case SignalBoxElementType.SwitchDouble:
                container.Children.Add(CreateThinTrack(4, 8, 56, 52, trackColor, 3));
                container.Children.Add(CreateThinTrack(4, 52, 56, 8, trackColor, 3));
                container.Children.Add(CreateOpsLabel("DKW"));
                Canvas.SetLeft(container.Children[^1], 20);
                Canvas.SetTop(container.Children[^1], 44);
                break;

            case SignalBoxElementType.SwitchCrossing:
                container.Children.Add(CreateThinTrack(4, 30, 56, 30, trackColor, 3));
                container.Children.Add(CreateThinTrack(30, 4, 30, 56, trackColor, 3));
                break;

            case SignalBoxElementType.SignalMain:
                container.Children.Add(CreateOperationsMainSignal(element));
                break;

            case SignalBoxElementType.SignalDistant:
                container.Children.Add(CreateOperationsDistantSignal(element));
                break;

            case SignalBoxElementType.SignalCombined:
                container.Children.Add(CreateOperationsCombinedSignal(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateOperationsShuntingSignal(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateOperationsFeedback(element));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(CreateThinTrack(4, 24, 56, 24, trackColor, 3));
                container.Children.Add(new Rectangle { Width = 42, Height = 4, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(container.Children[^1], 9);
                Canvas.SetTop(container.Children[^1], 36);
                container.Children.Add(CreateOpsLabel("Gl 1"));
                Canvas.SetLeft(container.Children[^1], 20);
                Canvas.SetTop(container.Children[^1], 44);
                break;

            case SignalBoxElementType.Label:
                var label = CreateOpsLabel(element.Name.Length > 0 ? element.Name : "354.7", 10);
                Canvas.SetLeft(label, 10);
                Canvas.SetTop(label, 24);
                container.Children.Add(label);
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

    private static Border CreateOpsLabel(string text, double fontSize = 8)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            Padding = new Thickness(2, 0, 2, 0),
            Child = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
            }
        };
    }

    private Grid CreateOperationsSwitch(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = GetStateColor(element.State, Colors);
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        // Main track
        var mainColor = isStraight ? trackColor : Colors.RouteClearing;
        grid.Children.Add(CreateThinTrack(4, 30, 56, 30, mainColor, 3));

        // Diverging track
        var divergeColor = !isStraight ? trackColor : Color.FromArgb(60, 100, 100, 100);
        grid.Children.Add(CreateThinTrack(28, 30, 50, isLeft ? 14 : 46, divergeColor, 2));

        // Switch number label
        var switchLabel = CreateOpsLabel(isLeft ? "W1" : "W2");
        Canvas.SetLeft(switchLabel, 22);
        Canvas.SetTop(switchLabel, 44);
        grid.Children.Add(switchLabel);

        return grid;
    }

    private Grid CreateOperationsMainSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var isGo = element.SignalAspect == SignalAspect.Hp1;
        var signalColor = isGo ? Colors.SignalGreen : Colors.SignalRed;

        // Vertical signal bar
        grid.Children.Add(new Rectangle { Width = 14, Height = 26, Fill = new SolidColorBrush(signalColor) });
        Canvas.SetLeft(grid.Children[^1], 23);
        Canvas.SetTop(grid.Children[^1], 12);

        // HP label
        var label = CreateOpsLabel("HP");
        Canvas.SetLeft(label, 24);
        Canvas.SetTop(label, 42);
        grid.Children.Add(label);

        return grid;
    }

    private Grid CreateOperationsDistantSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var isExpectStop = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
        grid.Children.Add(new Ellipse
        {
            Width = 20,
            Height = 20,
            Fill = new SolidColorBrush(isExpectStop ? Colors.SignalYellow : Color.FromArgb(50, 255, 200, 0))
        });
        Canvas.SetLeft(grid.Children[^1], 20);
        Canvas.SetTop(grid.Children[^1], 16);

        var label = CreateOpsLabel("VS");
        Canvas.SetLeft(label, 24);
        Canvas.SetTop(label, 40);
        grid.Children.Add(label);

        return grid;
    }

    private Grid CreateOperationsCombinedSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var topColor = element.SignalAspect switch
        {
            SignalAspect.Hp0 => Colors.SignalRed,
            SignalAspect.Ks1 => Colors.SignalGreen,
            _ => Color.FromArgb(60, 128, 128, 128)
        };

        grid.Children.Add(new Rectangle { Width = 14, Height = 30, Fill = new SolidColorBrush(topColor) });
        Canvas.SetLeft(grid.Children[^1], 23);
        Canvas.SetTop(grid.Children[^1], 10);

        var label = CreateOpsLabel("KS");
        Canvas.SetLeft(label, 24);
        Canvas.SetTop(label, 44);
        grid.Children.Add(label);

        return grid;
    }

    private Grid CreateOperationsShuntingSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle
        {
            Width = 26,
            Height = 12,
            Fill = new SolidColorBrush(Color.FromArgb(255, 30, 35, 40))
        });
        Canvas.SetLeft(grid.Children[^1], 17);
        Canvas.SetTop(grid.Children[^1], 24);

        var isOn = element.SignalAspect == SignalAspect.Sh1;
        grid.Children.Add(CreateClassicLed(22, 27, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(40, 255, 255, 255), 6));
        grid.Children.Add(CreateClassicLed(34, 27, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(40, 255, 255, 255), 6));

        var label = CreateOpsLabel("SH");
        Canvas.SetLeft(label, 24);
        Canvas.SetTop(label, 40);
        grid.Children.Add(label);

        return grid;
    }

    private Grid CreateOperationsFeedback(SignalBoxElement element)
    {
        var grid = new Grid();
        var isOccupied = element.State == SignalBoxElementState.Occupied;
        var trackColor = isOccupied ? Colors.TrackOccupied : Colors.TrackFree;

        grid.Children.Add(CreateThinTrack(4, 30, 56, 30, trackColor, 3));

        // Occupation marker
        var markerColor = isOccupied ? Colors.TrackOccupied : Color.FromArgb(50, 255, 0, 0);
        grid.Children.Add(new Rectangle { Width = 14, Height = 14, Fill = new SolidColorBrush(markerColor) });
        Canvas.SetLeft(grid.Children[^1], 23);
        Canvas.SetTop(grid.Children[^1], 14);

        var addr = CreateOpsLabel(element.Address > 0 ? element.Address.ToString(CultureInfo.InvariantCulture) : "?");
        Canvas.SetLeft(addr, 26);
        Canvas.SetTop(addr, 44);
        grid.Children.Add(addr);

        return grid;
    }
}
