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
/// Signal Box Page 6 - MOBAixl Network (Modern ESTW Multi-Panel Style)
/// Dark background with green route-set tracks, red occupied sections,
/// white station labels, multi-panel operations view.
/// Inspired by modern German railway control room displays with
/// multiple station areas visible simultaneously.
/// </summary>
public sealed class SignalBoxPage6 : SignalBoxPageBase2
{
    private static readonly SignalBoxColorScheme NetworkColors = new()
    {
        Background = Color.FromArgb(255, 10, 15, 20),
        PanelBackground = Color.FromArgb(255, 18, 22, 28),
        MessagePanelBackground = Color.FromArgb(255, 8, 10, 14),
        Border = Color.FromArgb(255, 35, 45, 55),
        GridLine = Color.FromArgb(12, 50, 70, 90),
        Accent = Color.FromArgb(255, 0, 255, 120),
        ButtonBackground = Color.FromArgb(255, 22, 28, 35),
        ButtonHover = Color.FromArgb(255, 35, 45, 55),
        ButtonBorder = Color.FromArgb(50, 60, 80, 100),
        TrackFree = Color.FromArgb(255, 80, 80, 80),
        TrackOccupied = Color.FromArgb(255, 255, 40, 40),
        RouteSet = Color.FromArgb(255, 0, 255, 0),
        RouteClearing = Color.FromArgb(255, 255, 255, 60),
        Blocked = Color.FromArgb(255, 60, 140, 255),
        SignalRed = Color.FromArgb(255, 255, 0, 0),
        SignalGreen = Color.FromArgb(255, 0, 255, 0),
        SignalYellow = Color.FromArgb(255, 255, 220, 0)
    };

    public SignalBoxPage6(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
    }

    protected override string StyleName => "MOBAixl Network";

    protected override string SubtitleText => "Betriebsfuehrungszentrale";

    protected override SignalBoxColorScheme Colors => NetworkColors;

    protected override string[] StationAreas => ["NHG", "TBD", "MST"];

    protected override UIElement CreateToolboxIcon(SignalBoxElementType type)
    {
        var canvas = new Canvas { Width = 48, Height = 40 };

        switch (type)
        {
            case SignalBoxElementType.TrackStraight:
                // Thin green line for route-set style
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.RouteSet, 2));
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
                    Stroke = new SolidColorBrush(Colors.RouteSet),
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
                    Stroke = new SolidColorBrush(Colors.RouteSet),
                    StrokeThickness = 2
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(CreateThinTrack(6, 20, 32, 20, Colors.TrackFree, 2));
                // Small red triangle end marker
                var endMarker = new Path
                {
                    Fill = new SolidColorBrush(Colors.SignalRed),
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(34, 14),
                            Segments = { new LineSegment { Point = new Point(42, 20) }, new LineSegment { Point = new Point(34, 26) } },
                            IsClosed = true
                        }}
                    }
                };
                canvas.Children.Add(endMarker);
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(CreateThinTrack(6, 22, 42, 22, Colors.RouteSet, 2));
                canvas.Children.Add(CreateThinTrack(24, 22, 38, 10, Colors.TrackFree, 2));
                // Small switch point marker
                canvas.Children.Add(new Rectangle { Width = 4, Height = 4, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 22);
                Canvas.SetTop(canvas.Children[^1], 20);
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(CreateThinTrack(6, 18, 42, 18, Colors.RouteSet, 2));
                canvas.Children.Add(CreateThinTrack(24, 18, 38, 30, Colors.TrackFree, 2));
                canvas.Children.Add(new Rectangle { Width = 4, Height = 4, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 22);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(CreateThinTrack(6, 10, 42, 30, Colors.RouteSet, 2));
                canvas.Children.Add(CreateThinTrack(6, 30, 42, 10, Colors.TrackFree, 2));
                canvas.Children.Add(new Rectangle { Width = 6, Height = 6, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 17);
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.RouteSet, 2));
                canvas.Children.Add(CreateThinTrack(24, 4, 24, 36, Colors.RouteSet, 2));
                break;

            case SignalBoxElementType.SignalMain:
                // Compact signal with arrow indicator
                canvas.Children.Add(CreateSignalArrow(24, 12, Colors.SignalRed, true));
                break;

            case SignalBoxElementType.SignalDistant:
                canvas.Children.Add(new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = new SolidColorBrush(Colors.SignalYellow),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)),
                    StrokeThickness = 1
                });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 13);
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(CreateSignalArrow(24, 10, Colors.SignalGreen, true));
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Color.FromArgb(60, 255, 255, 0)) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 26);
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 18, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 15);
                Canvas.SetTop(canvas.Children[^1], 16);
                canvas.Children.Add(CreateClassicLed(18, 17, Microsoft.UI.Colors.White, 4));
                canvas.Children.Add(CreateClassicLed(27, 17, Microsoft.UI.Colors.White, 4));
                break;

            case SignalBoxElementType.SignalSpeed:
                // Compact speed indicator
                canvas.Children.Add(new Border
                {
                    Width = 18,
                    Height = 18,
                    Background = new SolidColorBrush(Colors.SignalYellow),
                    CornerRadius = new CornerRadius(2),
                    Child = new TextBlock
                    {
                        Text = "8",
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });
                Canvas.SetLeft(canvas.Children[^1], 15);
                Canvas.SetTop(canvas.Children[^1], 11);
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(CreateThinTrack(6, 14, 42, 14, Colors.RouteSet, 2));
                // Platform edge line
                canvas.Children.Add(new Rectangle { Width = 30, Height = 3, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 9);
                Canvas.SetTop(canvas.Children[^1], 24);
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(CreateThinTrack(6, 20, 42, 20, Colors.TrackOccupied, 2));
                // Occupation marker
                canvas.Children.Add(new Rectangle { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.TrackOccupied) });
                Canvas.SetLeft(canvas.Children[^1], 20);
                Canvas.SetTop(canvas.Children[^1], 11);
                break;

            case SignalBoxElementType.Label:
                // Station label style
                canvas.Children.Add(new Border
                {
                    Width = 32,
                    Height = 14,
                    Background = new SolidColorBrush(Color.FromArgb(200, 20, 25, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = "Bf",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });
                Canvas.SetLeft(canvas.Children[^1], 8);
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

    private static Path CreateSignalArrow(double x, double y, Color color, bool pointingRight)
    {
        var direction = pointingRight ? 1 : -1;
        var arrow = new Path
        {
            Fill = new SolidColorBrush(color),
            Data = new PathGeometry
            {
                Figures = { new PathFigure
                {
                    StartPoint = new Point(x - 6 * direction, y + 4),
                    Segments =
                    {
                        new LineSegment { Point = new Point(x + 6 * direction, y + 8) },
                        new LineSegment { Point = new Point(x + 6 * direction, y + 14) },
                        new LineSegment { Point = new Point(x - 6 * direction, y + 10) }
                    },
                    IsClosed = true
                }}
            }
        };
        return arrow;
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
                container.Children.Add(CreateThinTrack(4, 30, 42, 30, trackColor, 3));
                // End marker triangle
                var endMarker = new Path
                {
                    Fill = new SolidColorBrush(Colors.SignalRed),
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(44, 20),
                            Segments = { new LineSegment { Point = new Point(56, 30) }, new LineSegment { Point = new Point(44, 40) } },
                            IsClosed = true
                        }}
                    }
                };
                container.Children.Add(endMarker);
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateNetworkSwitch(element));
                break;

            case SignalBoxElementType.SwitchDouble:
                container.Children.Add(CreateThinTrack(4, 8, 56, 52, trackColor, 3));
                container.Children.Add(CreateThinTrack(4, 52, 56, 8, trackColor, 3));
                container.Children.Add(new Rectangle { Width = 8, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(container.Children[^1], 26);
                Canvas.SetTop(container.Children[^1], 26);
                break;

            case SignalBoxElementType.SwitchCrossing:
                container.Children.Add(CreateThinTrack(4, 30, 56, 30, trackColor, 3));
                container.Children.Add(CreateThinTrack(30, 4, 30, 56, trackColor, 3));
                break;

            case SignalBoxElementType.SignalMain:
                container.Children.Add(CreateNetworkMainSignal(element));
                break;

            case SignalBoxElementType.SignalDistant:
                container.Children.Add(CreateNetworkDistantSignal(element));
                break;

            case SignalBoxElementType.SignalCombined:
                container.Children.Add(CreateNetworkCombinedSignal(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateNetworkShuntingSignal(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateNetworkFeedback(element));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(CreateThinTrack(4, 24, 56, 24, trackColor, 3));
                container.Children.Add(new Rectangle { Width = 44, Height = 4, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(container.Children[^1], 8);
                Canvas.SetTop(container.Children[^1], 36);
                break;

            case SignalBoxElementType.Label:
                var labelBorder = new Border
                {
                    Width = 48,
                    Height = 18,
                    Background = new SolidColorBrush(Color.FromArgb(220, 15, 20, 25)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = element.Name.Length > 0 ? element.Name : "Bf",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Canvas.SetLeft(labelBorder, 6);
                Canvas.SetTop(labelBorder, 21);
                container.Children.Add(labelBorder);
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

    private Grid CreateNetworkSwitch(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = GetStateColor(element.State, Colors);
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        // Main track
        var mainColor = isStraight ? trackColor : Color.FromArgb(60, trackColor.R, trackColor.G, trackColor.B);
        grid.Children.Add(CreateThinTrack(4, 30, 56, 30, mainColor, 3));

        // Diverging track
        var divergeColor = !isStraight ? trackColor : Color.FromArgb(50, 80, 80, 80);
        grid.Children.Add(CreateThinTrack(30, 30, 52, isLeft ? 12 : 48, divergeColor, 2));

        // Switch point indicator (small white square)
        grid.Children.Add(new Rectangle
        {
            Width = 6,
            Height = 6,
            Fill = new SolidColorBrush(isStraight ? Colors.RouteSet : Colors.SignalYellow)
        });
        Canvas.SetLeft(grid.Children[^1], 27);
        Canvas.SetTop(grid.Children[^1], 27);

        return grid;
    }

    private Grid CreateNetworkMainSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var isGo = element.SignalAspect == SignalAspect.Hp1;
        var signalColor = isGo ? Colors.SignalGreen : Colors.SignalRed;

        // Arrow indicator pointing right
        var arrow = new Path
        {
            Fill = new SolidColorBrush(signalColor),
            Data = new PathGeometry
            {
                Figures = { new PathFigure
                {
                    StartPoint = new Point(18, 22),
                    Segments =
                    {
                        new LineSegment { Point = new Point(42, 30) },
                        new LineSegment { Point = new Point(18, 38) }
                    },
                    IsClosed = true
                }}
            }
        };
        grid.Children.Add(arrow);

        return grid;
    }

    private Grid CreateNetworkDistantSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var isExpectStop = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
        grid.Children.Add(new Ellipse
        {
            Width = 22,
            Height = 22,
            Fill = new SolidColorBrush(isExpectStop ? Colors.SignalYellow : Color.FromArgb(50, 255, 220, 0)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            StrokeThickness = 1
        });
        Canvas.SetLeft(grid.Children[^1], 19);
        Canvas.SetTop(grid.Children[^1], 19);

        return grid;
    }

    private Grid CreateNetworkCombinedSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        var topColor = element.SignalAspect switch
        {
            SignalAspect.Hp0 => Colors.SignalRed,
            SignalAspect.Ks1 => Colors.SignalGreen,
            _ => Color.FromArgb(50, 128, 128, 128)
        };

        // Arrow for main aspect
        var arrow = new Path
        {
            Fill = new SolidColorBrush(topColor),
            Data = new PathGeometry
            {
                Figures = { new PathFigure
                {
                    StartPoint = new Point(18, 18),
                    Segments =
                    {
                        new LineSegment { Point = new Point(40, 26) },
                        new LineSegment { Point = new Point(18, 34) }
                    },
                    IsClosed = true
                }}
            }
        };
        grid.Children.Add(arrow);

        // Bottom LED for distant aspect
        var bottomColor = element.SignalAspect == SignalAspect.Ks2 ? Colors.SignalYellow : Color.FromArgb(40, 255, 255, 0);
        grid.Children.Add(CreateClassicLed(26, 42, bottomColor, 10));

        return grid;
    }

    private Grid CreateNetworkShuntingSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle
        {
            Width = 28,
            Height = 14,
            Fill = new SolidColorBrush(Color.FromArgb(255, 30, 35, 40))
        });
        Canvas.SetLeft(grid.Children[^1], 16);
        Canvas.SetTop(grid.Children[^1], 23);

        var isOn = element.SignalAspect == SignalAspect.Sh1;
        grid.Children.Add(CreateClassicLed(21, 26, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(40, 255, 255, 255), 7));
        grid.Children.Add(CreateClassicLed(34, 26, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(40, 255, 255, 255), 7));

        return grid;
    }

    private Grid CreateNetworkFeedback(SignalBoxElement element)
    {
        var grid = new Grid();
        var isOccupied = element.State == SignalBoxElementState.Occupied;
        var trackColor = isOccupied ? Colors.TrackOccupied : Colors.TrackFree;

        grid.Children.Add(CreateThinTrack(4, 30, 56, 30, trackColor, 3));

        // Occupation marker (red square when occupied)
        var markerColor = isOccupied ? Colors.TrackOccupied : Color.FromArgb(40, 255, 0, 0);
        grid.Children.Add(new Rectangle { Width = 12, Height = 12, Fill = new SolidColorBrush(markerColor) });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 16);

        var addr = new TextBlock
        {
            Text = element.Address > 0 ? element.Address.ToString(CultureInfo.InvariantCulture) : "?",
            FontSize = 8,
            Foreground = new SolidColorBrush(Colors.Accent)
        };
        Canvas.SetLeft(addr, 26);
        Canvas.SetTop(addr, 44);
        grid.Children.Add(addr);

        return grid;
    }
}
