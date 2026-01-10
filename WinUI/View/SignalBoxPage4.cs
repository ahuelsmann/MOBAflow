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
/// Signal Box Page 4 - ILTIS Style (Swiss Railways SBB Electronic Interlocking)
/// Based on the Swiss ILTIS (Integrated Control and Information System) displays.
/// Features: Clean light gray background, thin precise lines, Swiss L-signal style,
/// modern minimalist design, high precision graphics, SBB corporate colors.
/// </summary>
public sealed class SignalBoxPage4 : SignalBoxPageBase
{
    private static readonly SignalBoxColorScheme IltisColors = new()
    {
        Background = Color.FromArgb(255, 235, 235, 235),
        PanelBackground = Color.FromArgb(255, 245, 245, 245),
        MessagePanelBackground = Color.FromArgb(255, 250, 250, 250),
        Border = Color.FromArgb(255, 180, 180, 180),
        GridLine = Color.FromArgb(40, 100, 100, 100),
        Accent = Color.FromArgb(255, 236, 0, 0),
        ButtonBackground = Color.FromArgb(255, 255, 255, 255),
        ButtonHover = Color.FromArgb(255, 245, 245, 245),
        ButtonBorder = Color.FromArgb(80, 150, 150, 150),
        TrackFree = Color.FromArgb(255, 80, 80, 80),
        TrackOccupied = Color.FromArgb(255, 220, 20, 20),
        RouteSet = Color.FromArgb(255, 20, 180, 20),
        RouteClearing = Color.FromArgb(255, 240, 200, 20),
        Blocked = Color.FromArgb(255, 60, 120, 200),
        SignalRed = Color.FromArgb(255, 220, 20, 20),
        SignalGreen = Color.FromArgb(255, 20, 180, 20),
        SignalYellow = Color.FromArgb(255, 240, 200, 20)
    };

    private TextBlock? _clockText;
    private TextBlock? _dateText;

    public SignalBoxPage4(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
        StartClock();
    }

    protected override string StyleName => "ILTIS Stellwerk";

    protected override SignalBoxColorScheme Colors => IltisColors;

    private void StartClock()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) =>
        {
            if (_clockText != null)
                _clockText.Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            if (_dateText != null)
                _dateText.Text = DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        };
        timer.Start();
    }

    protected override Border BuildHeader()
    {
        var grid = new Grid { Padding = new Thickness(20, 12, 20, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var logoPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

        var logoBox = new Border
        {
            Width = 48,
            Height = 32,
            Background = new SolidColorBrush(Colors.Accent),
            CornerRadius = new CornerRadius(2)
        };
        var logoText = new TextBlock
        {
            Text = "SBB",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        logoBox.Child = logoText;
        logoPanel.Children.Add(logoBox);

        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new TextBlock
        {
            Text = "ILTIS",
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40))
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = "Integriertes Leit- und Informationssystem",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 80, 80, 80))
        });
        logoPanel.Children.Add(titleStack);

        Grid.SetColumn(logoPanel, 0);
        grid.Children.Add(logoPanel);

        var stationPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        stationPanel.Children.Add(new TextBlock
        {
            Text = "Musterstadt",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Normal,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        stationPanel.Children.Add(new TextBlock
        {
            Text = "Stellbereich A",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 80, 80, 80)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Grid.SetColumn(stationPanel, 1);
        grid.Children.Add(stationPanel);

        var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, HorizontalAlignment = HorizontalAlignment.Right };

        var statusPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        statusPanel.Children.Add(CreateStatusPill("Betrieb", Colors.SignalGreen));
        statusPanel.Children.Add(CreateStatusPill("Verbunden", Colors.SignalGreen));
        rightPanel.Children.Add(statusPanel);

        var timePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _dateText = new TextBlock
        {
            Text = DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 80, 80, 80)),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        timePanel.Children.Add(_dateText);
        _clockText = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            FontSize = 22,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = Microsoft.UI.Text.FontWeights.Light,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        timePanel.Children.Add(_clockText);
        rightPanel.Children.Add(timePanel);

        Grid.SetColumn(rightPanel, 2);
        grid.Children.Add(rightPanel);

        return new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            BorderBrush = new SolidColorBrush(Colors.Border),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid
        };
    }

    private static Border CreateStatusPill(string text, Color color)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 4, 10, 4)
        };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        panel.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(color) });
        panel.Children.Add(new TextBlock { Text = text, FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) });
        border.Child = panel;
        return border;
    }

    protected override Border BuildStatusBar()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(20, 8, 20, 8), Spacing = 20 };

        var connPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        connPanel.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Z21.IsConnected ? Colors.SignalGreen : Colors.SignalRed) });
        connPanel.Children.Add(new TextBlock
        {
            Text = Z21.IsConnected ? "Z21 verbunden" : "Z21 getrennt",
            Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            FontSize = 12
        });
        panel.Children.Add(connPanel);

        panel.Children.Add(new Border { Width = 1, Height = 14, Background = new SolidColorBrush(Colors.Border) });

        StatusTextBlock = new TextBlock
        {
            Text = "Bereit",
            Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
            FontSize = 12
        };
        panel.Children.Add(StatusTextBlock);

        return new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            BorderBrush = new SolidColorBrush(Colors.Border),
            BorderThickness = new Thickness(0, 1, 0, 0),
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
                    StrokeThickness = 3
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
                            StartPoint = new Point(4, 20),
                            Segments = { new ArcSegment { Point = new Point(24, 38), Size = new Size(20, 20), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 3
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 20, X2 = 32, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 34, Y1 = 12, X2 = 34, Y2 = 28, Stroke = new SolidColorBrush(Colors.Accent), StrokeThickness = 4 });
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 22, X2 = 44, Y2 = 22, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 22, Y1 = 22, X2 = 40, Y2 = 8, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 2 });
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.TrackFree), Stroke = new SolidColorBrush(Microsoft.UI.Colors.White), StrokeThickness = 1 });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 19);
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 18, X2 = 44, Y2 = 18, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 22, Y1 = 18, X2 = 40, Y2 = 32, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 2 });
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Colors.TrackFree), Stroke = new SolidColorBrush(Microsoft.UI.Colors.White), StrokeThickness = 1 });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 15);
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 10, X2 = 44, Y2 = 30, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 4, Y1 = 30, X2 = 44, Y2 = 10, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 20, X2 = 44, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 24, Y1 = 4, X2 = 24, Y2 = 36, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                break;

            case SignalBoxElementType.SignalMain:
                canvas.Children.Add(new Line { X1 = 24, Y1 = 6, X2 = 24, Y2 = 34, Stroke = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)), StrokeThickness = 2 });
                canvas.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(canvas.Children[^1], 28);
                Canvas.SetTop(canvas.Children[^1], 8);
                canvas.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromArgb(40, 0, 180, 0)) });
                Canvas.SetLeft(canvas.Children[^1], 28);
                Canvas.SetTop(canvas.Children[^1], 22);
                break;

            case SignalBoxElementType.SignalDistant:
                canvas.Children.Add(new Rectangle { Width = 14, Height = 14, Fill = new SolidColorBrush(Colors.SignalYellow), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 13);
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(new Line { X1 = 24, Y1 = 4, X2 = 24, Y2 = 36, Stroke = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)), StrokeThickness = 2 });
                canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.SignalGreen) });
                Canvas.SetLeft(canvas.Children[^1], 28);
                Canvas.SetTop(canvas.Children[^1], 15);
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 20, Height = 14, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 14);
                Canvas.SetTop(canvas.Children[^1], 13);
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 17);
                canvas.Children.Add(new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 25);
                Canvas.SetTop(canvas.Children[^1], 17);
                break;

            case SignalBoxElementType.SignalSpeed:
                canvas.Children.Add(new Rectangle { Width = 20, Height = 20, Fill = new SolidColorBrush(Colors.SignalYellow), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 14);
                Canvas.SetTop(canvas.Children[^1], 10);
                canvas.Children.Add(new TextBlock { Text = "6", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 12);
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(new Rectangle { Width = 38, Height = 4, Fill = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150)), RadiusX = 1, RadiusY = 1 });
                Canvas.SetLeft(canvas.Children[^1], 5);
                Canvas.SetTop(canvas.Children[^1], 28);
                canvas.Children.Add(new Line { X1 = 5, Y1 = 16, X2 = 43, Y2 = 16, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(new Line { X1 = 4, Y1 = 20, X2 = 44, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(Colors.Accent), Opacity = 0.6 });
                Canvas.SetLeft(canvas.Children[^1], 18);
                Canvas.SetTop(canvas.Children[^1], 14);
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(new TextBlock { Text = "Text", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)) });
                Canvas.SetLeft(canvas.Children[^1], 12);
                Canvas.SetTop(canvas.Children[^1], 14);
                break;

            default:
                canvas.Children.Add(new Rectangle
                {
                    Width = 40, Height = 32,
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 1, RadiusX = 2, RadiusY = 2
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
                    StrokeThickness = 4
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
                    StrokeThickness = 4
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                container.Children.Add(new Line { X1 = 2, Y1 = 30, X2 = 44, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 4 });
                container.Children.Add(new Line { X1 = 48, Y1 = 18, X2 = 48, Y2 = 42, Stroke = new SolidColorBrush(Colors.Accent), StrokeThickness = 5 });
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateIltisSwitch(element));
                break;

            case SignalBoxElementType.SwitchDouble:
                container.Children.Add(new Line { X1 = 2, Y1 = 8, X2 = 58, Y2 = 52, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 4 });
                container.Children.Add(new Line { X1 = 2, Y1 = 52, X2 = 58, Y2 = 8, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 4 });
                break;

            case SignalBoxElementType.SwitchCrossing:
                container.Children.Add(new Line { X1 = 2, Y1 = 30, X2 = 58, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 4 });
                container.Children.Add(new Line { X1 = 30, Y1 = 2, X2 = 30, Y2 = 58, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 4 });
                break;

            case SignalBoxElementType.SignalMain:
                container.Children.Add(CreateIltisMainSignal(element));
                break;

            case SignalBoxElementType.SignalDistant:
                container.Children.Add(CreateIltisDistantSignal(element));
                break;

            case SignalBoxElementType.SignalCombined:
                container.Children.Add(CreateIltisCombinedSignal(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateIltisShuntingSignal(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateIltisFeedback(element));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(new Line { X1 = 2, Y1 = 24, X2 = 58, Y2 = 24, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 4 });
                container.Children.Add(new Rectangle { Width = 54, Height = 6, Fill = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160)), RadiusX = 1, RadiusY = 1 });
                Canvas.SetLeft(container.Children[^1], 3);
                Canvas.SetTop(container.Children[^1], 40);
                break;

            case SignalBoxElementType.Label:
                var txt = new TextBlock
                {
                    Text = element.Name.Length > 0 ? element.Name : "Text",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60))
                };
                Canvas.SetLeft(txt, 8);
                Canvas.SetTop(txt, 22);
                container.Children.Add(txt);
                break;

            default:
                container.Children.Add(new Rectangle
                {
                    Width = GridCellSize - 4,
                    Height = GridCellSize - 4,
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 1, RadiusX = 2, RadiusY = 2
                });
                break;
        }

        container.RenderTransform = new RotateTransform { Angle = element.Rotation, CenterX = GridCellSize / 2, CenterY = GridCellSize / 2 };
        SetupElementInteraction(container, element);

        return container;
    }

    private Grid CreateIltisSwitch(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = GetStateColor(element.State, Colors);
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        grid.Children.Add(new Line
        {
            X1 = 2, Y1 = 30, X2 = 58, Y2 = 30,
            Stroke = new SolidColorBrush(isStraight ? trackColor : Color.FromArgb(100, trackColor.R, trackColor.G, trackColor.B)),
            StrokeThickness = 4
        });

        var divergeColor = !isStraight ? trackColor : Color.FromArgb(60, 80, 80, 80);
        grid.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 54, Y2 = isLeft ? 10 : 50,
            Stroke = new SolidColorBrush(divergeColor),
            StrokeThickness = 3
        });

        grid.Children.Add(new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(isStraight ? trackColor : Colors.RouteClearing),
            Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
            StrokeThickness = 2
        });
        Canvas.SetLeft(grid.Children[^1], 25);
        Canvas.SetTop(grid.Children[^1], 25);

        return grid;
    }

    private Grid CreateIltisMainSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Line { X1 = 20, Y1 = 8, X2 = 20, Y2 = 52, Stroke = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)), StrokeThickness = 2 });

        var redOn = element.SignalAspect == SignalAspect.Hp0;
        grid.Children.Add(new Ellipse { Width = 16, Height = 16, Fill = new SolidColorBrush(redOn ? Colors.SignalRed : Color.FromArgb(30, 220, 0, 0)) });
        Canvas.SetLeft(grid.Children[^1], 28);
        Canvas.SetTop(grid.Children[^1], 12);

        var greenOn = element.SignalAspect == SignalAspect.Hp1;
        grid.Children.Add(new Ellipse { Width = 16, Height = 16, Fill = new SolidColorBrush(greenOn ? Colors.SignalGreen : Color.FromArgb(30, 0, 180, 0)) });
        Canvas.SetLeft(grid.Children[^1], 28);
        Canvas.SetTop(grid.Children[^1], 32);

        return grid;
    }

    private Grid CreateIltisDistantSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Line { X1 = 18, Y1 = 10, X2 = 18, Y2 = 50, Stroke = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)), StrokeThickness = 2 });

        var isExpectStop = element.SignalAspect == SignalAspect.Vr0;
        var diamond = new Rectangle
        {
            Width = 20,
            Height = 20,
            Fill = new SolidColorBrush(isExpectStop ? Colors.SignalYellow : Color.FromArgb(30, 240, 200, 0)),
            RadiusX = 2,
            RadiusY = 2,
            RenderTransform = new RotateTransform { Angle = 45, CenterX = 10, CenterY = 10 }
        };
        Canvas.SetLeft(diamond, 28);
        Canvas.SetTop(diamond, 20);
        grid.Children.Add(diamond);

        return grid;
    }

    private Grid CreateIltisCombinedSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Line { X1 = 18, Y1 = 6, X2 = 18, Y2 = 54, Stroke = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)), StrokeThickness = 2 });

        var mainColor = element.SignalAspect switch
        {
            SignalAspect.Hp0 => Colors.SignalRed,
            SignalAspect.Ks1 => Colors.SignalGreen,
            SignalAspect.Ks2 => Colors.SignalYellow,
            _ => Color.FromArgb(30, 128, 128, 128)
        };
        grid.Children.Add(new Ellipse { Width = 18, Height = 18, Fill = new SolidColorBrush(mainColor) });
        Canvas.SetLeft(grid.Children[^1], 28);
        Canvas.SetTop(grid.Children[^1], 21);

        return grid;
    }

    private Grid CreateIltisShuntingSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle
        {
            Width = 36, Height = 18,
            Fill = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            RadiusX = 2, RadiusY = 2
        });
        Canvas.SetLeft(grid.Children[^1], 12);
        Canvas.SetTop(grid.Children[^1], 21);

        var isOn = element.SignalAspect == SignalAspect.Sh1;
        grid.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(isOn ? Microsoft.UI.Colors.White : Color.FromArgb(30, 255, 255, 255)) });
        Canvas.SetLeft(grid.Children[^1], 17);
        Canvas.SetTop(grid.Children[^1], 25);
        grid.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(isOn ? Microsoft.UI.Colors.White : Color.FromArgb(30, 255, 255, 255)) });
        Canvas.SetLeft(grid.Children[^1], 33);
        Canvas.SetTop(grid.Children[^1], 25);

        return grid;
    }

    private Grid CreateIltisFeedback(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = element.State == SignalBoxElementState.Occupied ? Colors.TrackOccupied : Colors.TrackFree;

        grid.Children.Add(new Line { X1 = 2, Y1 = 30, X2 = 58, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 4 });

        var indicatorColor = element.State == SignalBoxElementState.Occupied ? Colors.Accent : Color.FromArgb(60, 236, 0, 0);
        grid.Children.Add(new Ellipse { Width = 14, Height = 14, Fill = new SolidColorBrush(indicatorColor) });
        Canvas.SetLeft(grid.Children[^1], 23);
        Canvas.SetTop(grid.Children[^1], 16);

        var addr = new TextBlock
        {
            Text = element.Address > 0 ? element.Address.ToString(CultureInfo.InvariantCulture) : "?",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80))
        };
        Canvas.SetLeft(addr, 26);
        Canvas.SetTop(addr, 44);
        grid.Children.Add(addr);

        return grid;
    }
}
