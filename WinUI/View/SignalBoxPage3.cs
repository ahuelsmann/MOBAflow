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
/// Signal Box Page 3 - ESTW L90 Style (Modern Siemens Electronic Interlocking)
/// Based on Siemens ESTW L90 displays used since the 1990s.
/// Features: Deep blue background, thick bright lines, modern LED-style signals,
/// clean geometric design, high contrast elements.
/// </summary>
public sealed class SignalBoxPage3 : SignalBoxPageBase
{
    private static readonly SignalBoxColorScheme EstwL90Colors = new()
    {
        Background = Color.FromArgb(255, 10, 30, 60),
        PanelBackground = Color.FromArgb(255, 15, 40, 70),
        MessagePanelBackground = Color.FromArgb(255, 8, 25, 50),
        Border = Color.FromArgb(255, 30, 80, 140),
        GridLine = Color.FromArgb(25, 80, 150, 220),
        Accent = Color.FromArgb(255, 0, 200, 255),
        ButtonBackground = Color.FromArgb(255, 20, 50, 90),
        ButtonHover = Color.FromArgb(255, 30, 70, 120),
        ButtonBorder = Color.FromArgb(80, 80, 150, 220),
        TrackFree = Color.FromArgb(255, 120, 120, 120),
        TrackOccupied = Color.FromArgb(255, 255, 40, 40),
        RouteSet = Color.FromArgb(255, 40, 255, 40),
        RouteClearing = Color.FromArgb(255, 255, 255, 40),
        Blocked = Color.FromArgb(255, 80, 140, 255),
        SignalRed = Color.FromArgb(255, 255, 20, 20),
        SignalGreen = Color.FromArgb(255, 20, 255, 20),
        SignalYellow = Color.FromArgb(255, 255, 220, 20)
    };

    private TextBlock? _clockText;

    public SignalBoxPage3(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
        StartClock();
    }

    protected override string StyleName => "ESTW L90";

    protected override SignalBoxColorScheme Colors => EstwL90Colors;

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
        var grid = new Grid { Padding = new Thickness(20, 14, 20, 14) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };

        var logoBox = new Border
        {
            Width = 40,
            Height = 40,
            Background = new SolidColorBrush(Colors.Accent),
            CornerRadius = new CornerRadius(4)
        };
        var logoText = new TextBlock
        {
            Text = "S",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.Background),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        logoBox.Child = logoText;
        titlePanel.Children.Add(logoBox);

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock
        {
            Text = "ESTW L90",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        });
        textStack.Children.Add(new TextBlock
        {
            Text = "Elektronisches Stellwerk - Level 90",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
        });
        titlePanel.Children.Add(textStack);

        Grid.SetColumn(titlePanel, 0);
        grid.Children.Add(titlePanel);

        var stationPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        stationPanel.Children.Add(new TextBlock
        {
            Text = "MUSTERSTADT HBF",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.Accent),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        stationPanel.Children.Add(new TextBlock
        {
            Text = "Stellbereich Nord",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Grid.SetColumn(stationPanel, 1);
        grid.Children.Add(stationPanel);

        var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20, HorizontalAlignment = HorizontalAlignment.Right };

        var statusGrid = new Grid();
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddStatusIndicator(statusGrid, "SYS", Colors.SignalGreen, 0, 0);
        AddStatusIndicator(statusGrid, "COM", Colors.SignalGreen, 1, 0);
        AddStatusIndicator(statusGrid, "BUe", Colors.SignalGreen, 0, 1);
        AddStatusIndicator(statusGrid, "GSP", Colors.SignalRed, 1, 1);

        rightPanel.Children.Add(statusGrid);

        var clockPanel = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 5, 20, 40)),
            BorderBrush = new SolidColorBrush(Colors.Accent),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 6, 12, 6)
        };
        _clockText = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            FontSize = 24,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.Accent)
        };
        clockPanel.Child = _clockText;
        rightPanel.Children.Add(clockPanel);

        Grid.SetColumn(rightPanel, 2);
        grid.Children.Add(rightPanel);

        return new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                {
                    new GradientStop { Color = Color.FromArgb(255, 20, 50, 90), Offset = 0 },
                    new GradientStop { Color = Colors.PanelBackground, Offset = 1 }
                }
            },
            BorderBrush = new SolidColorBrush(Colors.Accent),
            BorderThickness = new Thickness(0, 0, 0, 2),
            Child = grid
        };
    }

    private static void AddStatusIndicator(Grid grid, string label, Color color, int col, int row)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(4) };
        panel.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(color) });
        panel.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) });
        Grid.SetColumn(panel, col);
        Grid.SetRow(panel, row);
        grid.Children.Add(panel);
    }

    protected override Border BuildStatusBar()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(16, 8, 16, 8), Spacing = 20 };

        var connPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        connPanel.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Z21.IsConnected ? Colors.SignalGreen : Colors.SignalRed) });
        connPanel.Children.Add(new TextBlock
        {
            Text = Z21.IsConnected ? "Z21 Online" : "Z21 Offline",
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            FontSize = 12
        });
        panel.Children.Add(connPanel);

        panel.Children.Add(new Border { Width = 1, Height = 16, Background = new SolidColorBrush(Colors.Border) });

        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 0, 80, 0)),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8, 2, 8, 2),
            Child = new TextBlock { Text = "BETRIEB", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.SignalGreen) }
        });

        panel.Children.Add(new Border { Width = 1, Height = 16, Background = new SolidColorBrush(Colors.Border) });

        StatusTextBlock = new TextBlock
        {
            Text = "Bereit",
            Foreground = new SolidColorBrush(Colors.Accent),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12
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
                    X1 = 4, Y1 = 20, X2 = 44, Y2 = 20,
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 6,
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
                            StartPoint = new Point(4, 20),
                            Segments = { new ArcSegment { Point = new Point(36, 34), Size = new Size(28, 28), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 5
                });
                break;

            case SignalBoxElementType.TrackCurve90:
                canvas.Children.Add(new Path
                {
                    Data = new PathGeometry
                    {
                        Figures = { new PathFigure
                        {
                            StartPoint = new Point(4, 20),
                            Segments = { new ArcSegment { Point = new Point(24, 38), Size = new Size(20, 20), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 5
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 20, X2 = 30, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 5 });
                canvas.Children.Add(new Rectangle { Width = 8, Height = 20, Fill = new SolidColorBrush(Colors.SignalRed), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 32);
                Canvas.SetTop(canvas.Children[^1], 10);
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 22, X2 = 44, Y2 = 22, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 5 });
                canvas.Children.Add(new Line { X1 = 22, Y1 = 22, X2 = 40, Y2 = 6, Stroke = new SolidColorBrush(Colors.RouteSet), StrokeThickness = 4 });
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 18);
                Canvas.SetTop(canvas.Children[^1], 18);
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 18, X2 = 44, Y2 = 18, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 5 });
                canvas.Children.Add(new Line { X1 = 22, Y1 = 18, X2 = 40, Y2 = 34, Stroke = new SolidColorBrush(Colors.RouteSet), StrokeThickness = 4 });
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 18);
                Canvas.SetTop(canvas.Children[^1], 14);
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 8, X2 = 44, Y2 = 32, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 4, Y1 = 32, X2 = 44, Y2 = 8, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 15);
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 20, X2 = 44, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 5 });
                canvas.Children.Add(new Line { X1 = 24, Y1 = 4, X2 = 24, Y2 = 36, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 5 });
                break;

            case SignalBoxElementType.SignalMain:
                canvas.Children.Add(new Rectangle { Width = 16, Height = 30, Fill = new SolidColorBrush(Color.FromArgb(255, 10, 20, 40)), RadiusX = 3, RadiusY = 3 });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 5);
                canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 9);
                canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(40, 0, 255, 0)) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 21);
                break;

            case SignalBoxElementType.SignalDistant:
                canvas.Children.Add(new Ellipse { Width = 22, Height = 22, Fill = new SolidColorBrush(Colors.SignalYellow), Stroke = new SolidColorBrush(Color.FromArgb(255, 40, 60, 100)), StrokeThickness = 2 });
                Canvas.SetLeft(canvas.Children[^1], 13);
                Canvas.SetTop(canvas.Children[^1], 9);
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(new Rectangle { Width = 14, Height = 34, Fill = new SolidColorBrush(Color.FromArgb(255, 10, 20, 40)), RadiusX = 3, RadiusY = 3 });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 3);
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.SignalGreen) });
                Canvas.SetLeft(canvas.Children[^1], 20);
                Canvas.SetTop(canvas.Children[^1], 7);
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 0)) });
                Canvas.SetLeft(canvas.Children[^1], 20);
                Canvas.SetTop(canvas.Children[^1], 25);
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 28, Height = 16, Fill = new SolidColorBrush(Color.FromArgb(255, 10, 20, 40)), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 10);
                Canvas.SetTop(canvas.Children[^1], 12);
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 14);
                Canvas.SetTop(canvas.Children[^1], 16);
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 26);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.SignalSpeed:
                canvas.Children.Add(new Rectangle { Width = 24, Height = 24, Fill = new SolidColorBrush(Colors.SignalYellow), RadiusX = 4, RadiusY = 4 });
                Canvas.SetLeft(canvas.Children[^1], 12);
                Canvas.SetTop(canvas.Children[^1], 8);
                canvas.Children.Add(new TextBlock { Text = "8", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 12);
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(new Rectangle { Width = 38, Height = 6, Fill = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 5);
                Canvas.SetTop(canvas.Children[^1], 28);
                canvas.Children.Add(new Line { X1 = 5, Y1 = 14, X2 = 43, Y2 = 14, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 5 });
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 20, X2 = 44, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 6 });
                canvas.Children.Add(new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Microsoft.UI.Colors.White, Offset = 0 },
                            new GradientStop { Color = Colors.Accent, Offset = 0.5 },
                            new GradientStop { Color = Color.FromArgb(100, 0, 200, 255), Offset = 1 }
                        }
                    }
                });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 12);
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(new Rectangle { Width = 34, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(200, 10, 30, 60)), RadiusX = 3, RadiusY = 3, Stroke = new SolidColorBrush(Colors.Accent), StrokeThickness = 1 });
                Canvas.SetLeft(canvas.Children[^1], 7);
                Canvas.SetTop(canvas.Children[^1], 11);
                canvas.Children.Add(new TextBlock { Text = "Txt", FontSize = 11, Foreground = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 14);
                Canvas.SetTop(canvas.Children[^1], 12);
                break;

            default:
                canvas.Children.Add(new Rectangle
                {
                    Width = 40, Height = 32,
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 2, RadiusX = 4, RadiusY = 4
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
                    X1 = 2, Y1 = 30, X2 = 58, Y2 = 30,
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 7,
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
                            StartPoint = new Point(2, 30),
                            Segments = { new ArcSegment { Point = new Point(30, 58), Size = new Size(28, 28), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 7
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                container.Children.Add(new Line { X1 = 2, Y1 = 30, X2 = 42, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 7 });
                container.Children.Add(new Rectangle { Width = 10, Height = 26, Fill = new SolidColorBrush(Colors.SignalRed), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(container.Children[^1], 44);
                Canvas.SetTop(container.Children[^1], 17);
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateL90Switch(element));
                break;

            case SignalBoxElementType.SwitchDouble:
                container.Children.Add(new Line { X1 = 2, Y1 = 6, X2 = 58, Y2 = 54, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 6 });
                container.Children.Add(new Line { X1 = 2, Y1 = 54, X2 = 58, Y2 = 6, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 6 });
                container.Children.Add(new Ellipse { Width = 14, Height = 14, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(container.Children[^1], 23);
                Canvas.SetTop(container.Children[^1], 23);
                break;

            case SignalBoxElementType.SwitchCrossing:
                container.Children.Add(new Line { X1 = 2, Y1 = 30, X2 = 58, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 7 });
                container.Children.Add(new Line { X1 = 30, Y1 = 2, X2 = 30, Y2 = 58, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 7 });
                break;

            case SignalBoxElementType.SignalMain:
                container.Children.Add(CreateL90MainSignal(element));
                break;

            case SignalBoxElementType.SignalDistant:
                container.Children.Add(CreateL90DistantSignal(element));
                break;

            case SignalBoxElementType.SignalCombined:
                container.Children.Add(CreateL90CombinedSignal(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateL90ShuntingSignal(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateL90Feedback(element));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(new Line { X1 = 2, Y1 = 22, X2 = 58, Y2 = 22, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 6 });
                container.Children.Add(new Rectangle { Width = 54, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(container.Children[^1], 3);
                Canvas.SetTop(container.Children[^1], 38);
                break;

            case SignalBoxElementType.Label:
                var bg = new Rectangle
                {
                    Width = 52, Height = 26,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 10, 30, 60)),
                    Stroke = new SolidColorBrush(Colors.Accent),
                    StrokeThickness = 1,
                    RadiusX = 4, RadiusY = 4
                };
                Canvas.SetLeft(bg, 4);
                Canvas.SetTop(bg, 17);
                container.Children.Add(bg);
                var txt = new TextBlock
                {
                    Text = element.Name.Length > 0 ? element.Name : "Text",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Accent)
                };
                Canvas.SetLeft(txt, 10);
                Canvas.SetTop(txt, 20);
                container.Children.Add(txt);
                break;

            default:
                container.Children.Add(new Rectangle
                {
                    Width = GridCellSize - 4,
                    Height = GridCellSize - 4,
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 2, RadiusX = 4, RadiusY = 4
                });
                break;
        }

        container.RenderTransform = new RotateTransform { Angle = element.Rotation, CenterX = GridCellSize / 2, CenterY = GridCellSize / 2 };
        SetupElementInteraction(container, element);

        return container;
    }

    private Grid CreateL90Switch(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = GetStateColor(element.State, Colors);
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        grid.Children.Add(new Line
        {
            X1 = 2, Y1 = 30, X2 = 58, Y2 = 30,
            Stroke = new SolidColorBrush(isStraight ? trackColor : Color.FromArgb(100, trackColor.R, trackColor.G, trackColor.B)),
            StrokeThickness = 7
        });

        var divergeColor = !isStraight ? trackColor : Color.FromArgb(60, 120, 120, 120);
        grid.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 54, Y2 = isLeft ? 8 : 52,
            Stroke = new SolidColorBrush(divergeColor),
            StrokeThickness = 6
        });

        grid.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Colors.Accent) });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 24);

        return grid;
    }

    private Grid CreateL90MainSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 4, Height = 44, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 80, 120)) });
        Canvas.SetLeft(grid.Children[^1], 14);
        Canvas.SetTop(grid.Children[^1], 8);

        grid.Children.Add(new Rectangle { Width = 26, Height = 40, Fill = new SolidColorBrush(Color.FromArgb(255, 10, 25, 50)), RadiusX = 4, RadiusY = 4, Stroke = new SolidColorBrush(Color.FromArgb(255, 40, 80, 140)), StrokeThickness = 1 });
        Canvas.SetLeft(grid.Children[^1], 22);
        Canvas.SetTop(grid.Children[^1], 10);

        var redOn = element.SignalAspect == SignalAspect.Hp0;
        grid.Children.Add(CreateLed(28, 14, redOn ? Colors.SignalRed : Color.FromArgb(25, 255, 0, 0), 16));

        var greenOn = element.SignalAspect == SignalAspect.Hp1;
        grid.Children.Add(CreateLed(28, 32, greenOn ? Colors.SignalGreen : Color.FromArgb(25, 0, 255, 0), 16));

        return grid;
    }

    private Grid CreateL90DistantSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 3, Height = 38, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 80, 120)) });
        Canvas.SetLeft(grid.Children[^1], 16);
        Canvas.SetTop(grid.Children[^1], 10);

        var isExpectStop = element.SignalAspect == SignalAspect.Vr0;
        grid.Children.Add(new Ellipse
        {
            Width = 28, Height = 28,
            Fill = new SolidColorBrush(isExpectStop ? Colors.SignalYellow : Color.FromArgb(40, 255, 255, 0)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 40, 80, 140)),
            StrokeThickness = 2
        });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 16);

        return grid;
    }

    private Grid CreateL90CombinedSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 22, Height = 48, Fill = new SolidColorBrush(Color.FromArgb(255, 10, 25, 50)), RadiusX = 4, RadiusY = 4, Stroke = new SolidColorBrush(Color.FromArgb(255, 40, 80, 140)), StrokeThickness = 1 });
        Canvas.SetLeft(grid.Children[^1], 19);
        Canvas.SetTop(grid.Children[^1], 6);

        var topColor = element.SignalAspect switch
        {
            SignalAspect.Hp0 => Colors.SignalRed,
            SignalAspect.Ks1 => Colors.SignalGreen,
            _ => Color.FromArgb(25, 128, 128, 128)
        };
        grid.Children.Add(CreateLed(24, 12, topColor, 14));

        var bottomColor = element.SignalAspect == SignalAspect.Ks2 ? Colors.SignalYellow : Color.FromArgb(25, 255, 255, 0);
        grid.Children.Add(CreateLed(24, 36, bottomColor, 14));

        return grid;
    }

    private Grid CreateL90ShuntingSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 40, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(255, 10, 25, 50)), RadiusX = 3, RadiusY = 3, Stroke = new SolidColorBrush(Color.FromArgb(255, 40, 80, 140)), StrokeThickness = 1 });
        Canvas.SetLeft(grid.Children[^1], 10);
        Canvas.SetTop(grid.Children[^1], 19);

        var isOn = element.SignalAspect == SignalAspect.Sh1;
        grid.Children.Add(CreateLed(16, 24, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(25, 255, 255, 255), 12));
        grid.Children.Add(CreateLed(32, 24, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(25, 255, 255, 255), 12));

        return grid;
    }

    private Grid CreateL90Feedback(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = element.State == SignalBoxElementState.Occupied ? Colors.TrackOccupied : Colors.TrackFree;

        grid.Children.Add(new Line { X1 = 2, Y1 = 30, X2 = 58, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 7 });

        var ledColor = element.State == SignalBoxElementState.Occupied ? Colors.TrackOccupied : Colors.Accent;
        grid.Children.Add(CreateLed(22, 16, ledColor, 16));

        var addr = new TextBlock
        {
            Text = element.Address > 0 ? element.Address.ToString(CultureInfo.InvariantCulture) : "?",
            FontSize = 10,
            Foreground = new SolidColorBrush(Colors.Accent)
        };
        Canvas.SetLeft(addr, 26);
        Canvas.SetTop(addr, 44);
        grid.Children.Add(addr);

        return grid;
    }
}
