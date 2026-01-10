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

using System;
using System.Globalization;

using Windows.Foundation;
using Windows.UI;

/// <summary>
/// Signal Box Page 2 - SpDrS60 Style (Classic German Relay Interlocking)
/// Based on the classic Siemens SpDrS60 relay interlocking displays from the 1960s-1990s.
/// Features: Black background, yellow/green/red track sections, red illuminated signal buttons,
/// classic DB typography, station name headers, mechanical switch position indicators.
/// </summary>
public sealed class SignalBoxPage2 : SignalBoxPageBase
{
    private static readonly SignalBoxColorScheme SpDrS60Colors = new()
    {
        Background = Color.FromArgb(255, 15, 15, 15),
        PanelBackground = Color.FromArgb(255, 25, 25, 25),
        MessagePanelBackground = Color.FromArgb(255, 10, 10, 10),
        Border = Color.FromArgb(255, 60, 60, 60),
        GridLine = Color.FromArgb(30, 100, 100, 100),
        Accent = Color.FromArgb(255, 255, 200, 0),
        ButtonBackground = Color.FromArgb(255, 40, 40, 40),
        ButtonHover = Color.FromArgb(255, 60, 60, 60),
        ButtonBorder = Color.FromArgb(80, 120, 120, 120),
        TrackFree = Color.FromArgb(255, 160, 160, 160),
        TrackOccupied = Color.FromArgb(255, 255, 0, 0),
        RouteSet = Color.FromArgb(255, 0, 255, 0),
        RouteClearing = Color.FromArgb(255, 255, 255, 0),
        Blocked = Color.FromArgb(255, 0, 180, 255),
        SignalRed = Color.FromArgb(255, 255, 0, 0),
        SignalGreen = Color.FromArgb(255, 0, 255, 0),
        SignalYellow = Color.FromArgb(255, 255, 255, 0)
    };

    private TextBlock? _clockText;

    public SignalBoxPage2(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
        StartClock();
    }

    protected override string StyleName => "SpDrS60 Stellwerk";

    protected override SignalBoxColorScheme Colors => SpDrS60Colors;

    private void StartClock()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) =>
        {
            if (_clockText != null)
                _clockText.Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        };
        timer.Start();
    }

    protected override Border BuildHeader()
    {
        var grid = new Grid { Padding = new Thickness(16, 12, 16, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var stationPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        stationPanel.Children.Add(new TextBlock
        {
            Text = "Bf",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Normal,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200))
        });
        stationPanel.Children.Add(new TextBlock
        {
            Text = "MUSTERSTADT",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)),
            CharacterSpacing = 80
        });
        Grid.SetColumn(stationPanel, 0);
        grid.Children.Add(stationPanel);

        var titlePanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        titlePanel.Children.Add(new TextBlock
        {
            Text = "SpDrS60",
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Spurplandrucktastenstellwerk",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Grid.SetColumn(titlePanel, 1);
        grid.Children.Add(titlePanel);

        var rightPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Orientation = Orientation.Horizontal, Spacing = 24 };

        var ledPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        ledPanel.Children.Add(CreateStatusLed("BUe", Colors.SignalGreen));
        ledPanel.Children.Add(CreateStatusLed("Gsp", Colors.SignalRed));
        ledPanel.Children.Add(CreateStatusLed("Fst", Colors.SignalGreen));
        rightPanel.Children.Add(ledPanel);

        _clockText = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            FontSize = 28,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)),
            VerticalAlignment = VerticalAlignment.Center
        };
        rightPanel.Children.Add(_clockText);

        Grid.SetColumn(rightPanel, 2);
        grid.Children.Add(rightPanel);

        return new Border
        {
            Background = new SolidColorBrush(Colors.PanelBackground),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)),
            BorderThickness = new Thickness(0, 0, 0, 3),
            Child = grid
        };
    }

    private static StackPanel CreateStatusLed(string label, Color color)
    {
        var panel = new StackPanel { Spacing = 2 };
        panel.Children.Add(new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 8,
            Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        return panel;
    }

    protected override Border BuildStatusBar()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(12, 6, 12, 6), Spacing = 24 };

        var connLed = new Ellipse { Width = 14, Height = 14, Fill = new SolidColorBrush(Z21.IsConnected ? Colors.SignalGreen : Colors.SignalRed) };
        panel.Children.Add(connLed);
        panel.Children.Add(new TextBlock
        {
            Text = Z21.IsConnected ? "Z21 VERBUNDEN" : "Z21 GETRENNT",
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        });

        panel.Children.Add(new Border { Width = 1, Height = 20, Background = new SolidColorBrush(Colors.Border), Margin = new Thickness(8, 0, 8, 0) });

        StatusTextBlock = new TextBlock
        {
            Text = "Bereit - Elemente aus Toolbox ziehen",
            Foreground = new SolidColorBrush(Colors.Accent),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(StatusTextBlock);

        return new Border
        {
            Background = new SolidColorBrush(Colors.PanelBackground),
            BorderBrush = new SolidColorBrush(Colors.Border),
            BorderThickness = new Thickness(0, 2, 0, 0),
            Child = panel
        };
    }

    protected override UIElement CreateToolboxIcon(SignalBoxElementType type)
    {
        var canvas = new Canvas { Width = 48, Height = 40 };

        switch (type)
        {
            case SignalBoxElementType.TrackStraight:
                canvas.Children.Add(new Line
                {
                    X1 = 6, Y1 = 20, X2 = 42, Y2 = 20,
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 5,
                    StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
                    StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round
                });
                break;

            case SignalBoxElementType.TrackCurve45:
                canvas.Children.Add(new Path
                {
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(6, 20),
                            Segments = { new ArcSegment { Point = new Point(36, 34), Size = new Size(24, 24), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 4
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
                    StrokeThickness = 4
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 20, X2 = 30, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 32, Y1 = 12, X2 = 38, Y2 = 28, Stroke = new SolidColorBrush(Colors.SignalRed), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 38, Y1 = 12, X2 = 32, Y2 = 28, Stroke = new SolidColorBrush(Colors.SignalRed), StrokeThickness = 3 });
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 22, X2 = 42, Y2 = 22, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 20, Y1 = 22, X2 = 38, Y2 = 8, Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 200, 255)), StrokeThickness = 3 });
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.SignalGreen) });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 19);
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 18, X2 = 42, Y2 = 18, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 20, Y1 = 18, X2 = 38, Y2 = 32, Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 200, 255)), StrokeThickness = 3 });
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.SignalGreen) });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 15);
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 10, X2 = 42, Y2 = 30, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 6, Y1 = 30, X2 = 42, Y2 = 10, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 20);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 20, X2 = 42, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 24, Y1 = 4, X2 = 24, Y2 = 36, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                break;

            case SignalBoxElementType.SignalMain:
                canvas.Children.Add(new Rectangle { Width = 18, Height = 28, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), RadiusX = 3, RadiusY = 3 });
                Canvas.SetLeft(canvas.Children[^1], 15);
                Canvas.SetTop(canvas.Children[^1], 6);
                canvas.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(canvas.Children[^1], 18);
                Canvas.SetTop(canvas.Children[^1], 10);
                canvas.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromArgb(40, 0, 255, 0)) });
                Canvas.SetLeft(canvas.Children[^1], 18);
                Canvas.SetTop(canvas.Children[^1], 20);
                break;

            case SignalBoxElementType.SignalDistant:
                canvas.Children.Add(new Ellipse { Width = 20, Height = 20, Fill = new SolidColorBrush(Colors.Accent), Stroke = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)), StrokeThickness = 2 });
                Canvas.SetLeft(canvas.Children[^1], 14);
                Canvas.SetTop(canvas.Children[^1], 10);
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(new Rectangle { Width = 16, Height = 32, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), RadiusX = 3, RadiusY = 3 });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 4);
                canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.SignalGreen) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 8);
                canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 0)) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 22);
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 24, Height = 14, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 12);
                Canvas.SetTop(canvas.Children[^1], 13);
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 15);
                Canvas.SetTop(canvas.Children[^1], 16);
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 25);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.SignalSpeed:
                var triangle = new Path
                {
                    Fill = new SolidColorBrush(Colors.Accent),
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(24, 6),
                            Segments = { new LineSegment { Point = new Point(36, 34) }, new LineSegment { Point = new Point(12, 34) } },
                            IsClosed = true
                        }}
                    }
                };
                canvas.Children.Add(triangle);
                canvas.Children.Add(new TextBlock { Text = "8", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(new Rectangle { Width = 36, Height = 6, Fill = new SolidColorBrush(Microsoft.UI.Colors.White), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 6);
                Canvas.SetTop(canvas.Children[^1], 26);
                canvas.Children.Add(new Line { X1 = 6, Y1 = 14, X2 = 42, Y2 = 14, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 20, X2 = 42, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 5 });
                canvas.Children.Add(new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Microsoft.UI.Colors.White, Offset = 0 },
                            new GradientStop { Color = Colors.SignalRed, Offset = 0.5 },
                            new GradientStop { Color = Color.FromArgb(100, 255, 0, 0), Offset = 1 }
                        }
                    }
                });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 13);
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(new Rectangle { Width = 32, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(200, 40, 40, 40)), RadiusX = 3, RadiusY = 3 });
                Canvas.SetLeft(canvas.Children[^1], 8);
                Canvas.SetTop(canvas.Children[^1], 11);
                canvas.Children.Add(new TextBlock { Text = "Abc", FontSize = 11, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 14);
                Canvas.SetTop(canvas.Children[^1], 12);
                break;

            default:
                canvas.Children.Add(new Rectangle
                {
                    Width = 40,
                    Height = 32,
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 2,
                    RadiusX = 4,
                    RadiusY = 4
                });
                Canvas.SetLeft(canvas.Children[^1], 4);
                Canvas.SetTop(canvas.Children[^1], 4);
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
                container.Children.Add(new Line
                {
                    X1 = 4, Y1 = 30, X2 = 56, Y2 = 30,
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 6,
                    StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
                    StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round
                });
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
                    StrokeThickness = 6
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                container.Children.Add(new Line { X1 = 4, Y1 = 30, X2 = 40, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 6 });
                container.Children.Add(new Line { X1 = 42, Y1 = 20, X2 = 52, Y2 = 40, Stroke = new SolidColorBrush(Colors.SignalRed), StrokeThickness = 4 });
                container.Children.Add(new Line { X1 = 52, Y1 = 20, X2 = 42, Y2 = 40, Stroke = new SolidColorBrush(Colors.SignalRed), StrokeThickness = 4 });
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateSpDrS60Switch(element));
                break;

            case SignalBoxElementType.SwitchDouble:
                container.Children.Add(new Line { X1 = 4, Y1 = 8, X2 = 56, Y2 = 52, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 5 });
                container.Children.Add(new Line { X1 = 4, Y1 = 52, X2 = 56, Y2 = 8, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 5 });
                container.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(container.Children[^1], 24);
                Canvas.SetTop(container.Children[^1], 24);
                break;

            case SignalBoxElementType.SwitchCrossing:
                container.Children.Add(new Line { X1 = 4, Y1 = 30, X2 = 56, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 6 });
                container.Children.Add(new Line { X1 = 30, Y1 = 4, X2 = 30, Y2 = 56, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 6 });
                break;

            case SignalBoxElementType.SignalMain:
                container.Children.Add(CreateSpDrS60MainSignal(element));
                break;

            case SignalBoxElementType.SignalDistant:
                container.Children.Add(CreateSpDrS60DistantSignal(element));
                break;

            case SignalBoxElementType.SignalCombined:
                container.Children.Add(CreateSpDrS60CombinedSignal(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateSpDrS60ShuntingSignal(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateSpDrS60Feedback(element));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(new Line { X1 = 4, Y1 = 24, X2 = 56, Y2 = 24, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 5 });
                container.Children.Add(new Rectangle { Width = 52, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.White), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(container.Children[^1], 4);
                Canvas.SetTop(container.Children[^1], 38);
                break;

            case SignalBoxElementType.Label:
                var labelBg = new Rectangle { Width = 50, Height = 24, Fill = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)), RadiusX = 4, RadiusY = 4 };
                Canvas.SetLeft(labelBg, 5);
                Canvas.SetTop(labelBg, 18);
                container.Children.Add(labelBg);
                var labelText = new TextBlock { Text = element.Name.Length > 0 ? element.Name : "Text", FontSize = 12, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) };
                Canvas.SetLeft(labelText, 10);
                Canvas.SetTop(labelText, 20);
                container.Children.Add(labelText);
                break;

            default:
                container.Children.Add(new Rectangle
                {
                    Width = GridCellSize - 4,
                    Height = GridCellSize - 4,
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 2,
                    RadiusX = 4,
                    RadiusY = 4
                });
                break;
        }

        container.RenderTransform = new RotateTransform { Angle = element.Rotation, CenterX = GridCellSize / 2, CenterY = GridCellSize / 2 };
        SetupElementInteraction(container, element);

        return container;
    }

    private Grid CreateSpDrS60Switch(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = GetStateColor(element.State, Colors);
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        grid.Children.Add(new Line
        {
            X1 = 4, Y1 = 30, X2 = 56, Y2 = 30,
            Stroke = new SolidColorBrush(isStraight ? trackColor : Color.FromArgb(100, trackColor.R, trackColor.G, trackColor.B)),
            StrokeThickness = 6
        });

        var divergeColor = !isStraight ? trackColor : Color.FromArgb(80, 160, 160, 160);
        grid.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 52, Y2 = isLeft ? 10 : 50,
            Stroke = new SolidColorBrush(divergeColor),
            StrokeThickness = 5
        });

        var motorColor = isStraight ? Color.FromArgb(255, 0, 200, 255) : Color.FromArgb(255, 255, 200, 0);
        grid.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(motorColor) });
        Canvas.SetLeft(grid.Children[^1], 25);
        Canvas.SetTop(grid.Children[^1], 25);

        return grid;
    }

    private Grid CreateSpDrS60MainSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 4, Height = 40, Fill = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)) });
        Canvas.SetLeft(grid.Children[^1], 12);
        Canvas.SetTop(grid.Children[^1], 10);

        grid.Children.Add(new Rectangle { Width = 24, Height = 36, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), RadiusX = 4, RadiusY = 4 });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 12);

        var redOn = element.SignalAspect == SignalAspect.Hp0;
        grid.Children.Add(CreateLed(30, 16, redOn ? Colors.SignalRed : Color.FromArgb(30, 255, 0, 0), 14));

        var greenOn = element.SignalAspect == SignalAspect.Hp1;
        grid.Children.Add(CreateLed(30, 32, greenOn ? Colors.SignalGreen : Color.FromArgb(30, 0, 255, 0), 14));

        return grid;
    }

    private Grid CreateSpDrS60DistantSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 3, Height = 36, Fill = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)) });
        Canvas.SetLeft(grid.Children[^1], 14);
        Canvas.SetTop(grid.Children[^1], 12);

        var isYellow = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
        grid.Children.Add(new Ellipse
        {
            Width = 24,
            Height = 24,
            Fill = new SolidColorBrush(isYellow ? Colors.SignalYellow : Color.FromArgb(40, 255, 255, 0)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            StrokeThickness = 2
        });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 18);

        return grid;
    }

    private Grid CreateSpDrS60CombinedSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 20, Height = 44, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), RadiusX = 4, RadiusY = 4 });
        Canvas.SetLeft(grid.Children[^1], 20);
        Canvas.SetTop(grid.Children[^1], 8);

        var topColor = element.SignalAspect switch
        {
            SignalAspect.Hp0 => Colors.SignalRed,
            SignalAspect.Ks1 => Colors.SignalGreen,
            _ => Color.FromArgb(30, 128, 128, 128)
        };
        grid.Children.Add(CreateLed(25, 14, topColor, 12));

        var bottomColor = element.SignalAspect == SignalAspect.Ks2 ? Colors.SignalYellow : Color.FromArgb(30, 255, 255, 0);
        grid.Children.Add(CreateLed(25, 36, bottomColor, 12));

        return grid;
    }

    private Grid CreateSpDrS60ShuntingSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 36, Height = 20, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), RadiusX = 3, RadiusY = 3 });
        Canvas.SetLeft(grid.Children[^1], 12);
        Canvas.SetTop(grid.Children[^1], 20);

        var isOn = element.SignalAspect == SignalAspect.Sh1;
        grid.Children.Add(CreateLed(18, 24, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(30, 255, 255, 255), 10));
        grid.Children.Add(CreateLed(32, 24, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(30, 255, 255, 255), 10));

        return grid;
    }

    private Grid CreateSpDrS60Feedback(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = element.State == SignalBoxElementState.Occupied ? Colors.TrackOccupied : Colors.TrackFree;

        grid.Children.Add(new Line { X1 = 4, Y1 = 30, X2 = 56, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 6 });

        grid.Children.Add(CreateLed(24, 18, element.State == SignalBoxElementState.Occupied ? Colors.SignalRed : Color.FromArgb(60, 255, 0, 0), 14));

        var addr = new TextBlock
        {
            Text = element.Address > 0 ? element.Address.ToString(CultureInfo.InvariantCulture) : "?",
            FontSize = 10,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        Canvas.SetLeft(addr, 26);
        Canvas.SetTop(addr, 42);
        grid.Children.Add(addr);

        return grid;
    }
}
