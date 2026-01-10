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
/// Signal Box Page 1 - ESTW Modern Style (Elektronisches Stellwerk)
/// A modern, colorful electronic interlocking display inspired by various German ESTW systems.
/// Features: Deep teal/blue background, bright LED-style signals, colorful track states,
/// modern UI with glow effects, comprehensive German railway signal support (Ks, H/V, Hl).
/// </summary>
public sealed class SignalBoxPage : SignalBoxPageBase
{
    private static readonly SignalBoxColorScheme EstwModernColors = new()
    {
        Background = Color.FromArgb(255, 20, 40, 60),
        PanelBackground = Color.FromArgb(255, 25, 45, 65),
        MessagePanelBackground = Color.FromArgb(255, 15, 35, 55),
        Border = Color.FromArgb(255, 50, 70, 90),
        GridLine = Color.FromArgb(30, 80, 120, 160),
        Accent = Color.FromArgb(255, 50, 255, 50),
        ButtonBackground = Color.FromArgb(255, 35, 55, 75),
        ButtonHover = Color.FromArgb(255, 50, 80, 110),
        ButtonBorder = Color.FromArgb(80, 100, 150, 200),
        TrackFree = Color.FromArgb(255, 100, 100, 100),
        TrackOccupied = Color.FromArgb(255, 255, 50, 50),
        RouteSet = Color.FromArgb(255, 50, 255, 50),
        RouteClearing = Color.FromArgb(255, 255, 255, 50),
        Blocked = Color.FromArgb(255, 100, 150, 255),
        SignalRed = Color.FromArgb(255, 255, 0, 0),
        SignalGreen = Color.FromArgb(255, 0, 255, 0),
        SignalYellow = Color.FromArgb(255, 255, 200, 0)
    };

    private TextBlock? _clockText;

    public SignalBoxPage(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
        StartClock();
    }

    protected override string StyleName => "ESTW Modern";

    protected override SignalBoxColorScheme Colors => EstwModernColors;

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
        var grid = new Grid { Padding = new Thickness(16, 10, 16, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Logo and Title
        var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

        // Logo
        var logoBox = new Border
        {
            Width = 36,
            Height = 36,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop { Color = Colors.Accent, Offset = 0 },
                    new GradientStop { Color = Color.FromArgb(255, 30, 180, 30), Offset = 1 }
                }
            },
            CornerRadius = new CornerRadius(6)
        };
        var logoText = new TextBlock
        {
            Text = "M",
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        logoBox.Child = logoText;
        titlePanel.Children.Add(logoBox);

        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        textStack.Children.Add(new TextBlock
        {
            Text = "MOBAflow ESTW",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        });
        textStack.Children.Add(new TextBlock
        {
            Text = "Elektronisches Stellwerk - Modern",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
        });
        titlePanel.Children.Add(textStack);

        Grid.SetColumn(titlePanel, 0);
        grid.Children.Add(titlePanel);

        // Center: Station info
        var stationPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        stationPanel.Children.Add(new TextBlock
        {
            Text = "MUSTERSTADT HBF",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.Accent),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        stationPanel.Children.Add(new TextBlock
        {
            Text = "Stellbereich Mitte",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        Grid.SetColumn(stationPanel, 1);
        grid.Children.Add(stationPanel);

        // Right: Status indicators and clock
        var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, HorizontalAlignment = HorizontalAlignment.Right };

        // Status LEDs
        var statusPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
        statusPanel.Children.Add(CreateHeaderLed("SYS", Colors.SignalGreen));
        statusPanel.Children.Add(CreateHeaderLed("BUE", Colors.SignalGreen));
        statusPanel.Children.Add(CreateHeaderLed("GSP", Colors.SignalRed));
        statusPanel.Children.Add(CreateHeaderLed("FST", Colors.SignalGreen));
        rightPanel.Children.Add(statusPanel);

        // Divider
        rightPanel.Children.Add(new Border { Width = 1, Height = 28, Background = new SolidColorBrush(Colors.Border), Margin = new Thickness(4, 0, 4, 0) });

        // Clock
        var clockPanel = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 15, 30, 45)),
            BorderBrush = new SolidColorBrush(Colors.Accent),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 4, 10, 4)
        };
        _clockText = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            FontSize = 20,
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
                    new GradientStop { Color = Color.FromArgb(255, 30, 50, 70), Offset = 0 },
                    new GradientStop { Color = Colors.PanelBackground, Offset = 1 }
                }
            },
            BorderBrush = new SolidColorBrush(Colors.Border),
            BorderThickness = new Thickness(0, 0, 0, 2),
            Child = grid
        };
    }

    private static StackPanel CreateHeaderLed(string label, Color color)
    {
        var panel = new StackPanel { Spacing = 2 };
        panel.Children.Add(new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Microsoft.UI.Colors.White, Offset = 0 },
                    new GradientStop { Color = color, Offset = 0.4 },
                    new GradientStop { Color = Color.FromArgb(180, color.R, color.G, color.B), Offset = 1 }
                }
            },
            HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 8,
            Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        return panel;
    }

    protected override Border BuildStatusBar()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(12, 6, 12, 6), Spacing = 20 };

        // Connection indicator
        var connPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        connPanel.Children.Add(new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Microsoft.UI.Colors.White, Offset = 0 },
                    new GradientStop { Color = Z21.IsConnected ? Colors.SignalGreen : Colors.SignalRed, Offset = 0.5 },
                    new GradientStop { Color = Color.FromArgb(100, Z21.IsConnected ? (byte)0 : (byte)255, Z21.IsConnected ? (byte)255 : (byte)0, 0), Offset = 1 }
                }
            }
        });
        connPanel.Children.Add(new TextBlock
        {
            Text = Z21.IsConnected ? "Z21 ONLINE" : "Z21 OFFLINE",
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(connPanel);

        panel.Children.Add(new Border { Width = 1, Height = 16, Background = new SolidColorBrush(Colors.Border) });

        // Mode indicator
        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 0, 60, 0)),
            BorderBrush = new SolidColorBrush(Colors.SignalGreen),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8, 2, 8, 2),
            Child = new TextBlock
            {
                Text = "BETRIEB",
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.SignalGreen)
            }
        });

        panel.Children.Add(new Border { Width = 1, Height = 16, Background = new SolidColorBrush(Colors.Border) });

        // Status message
        StatusTextBlock = new TextBlock
        {
            Text = "Bereit - Elemente aus Toolbox ziehen",
            Foreground = new SolidColorBrush(Colors.Accent),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
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
                            Segments = { new ArcSegment { Point = new Point(36, 34), Size = new Size(26, 26), SweepDirection = SweepDirection.Clockwise } }
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
                            Segments = { new ArcSegment { Point = new Point(24, 36), Size = new Size(18, 18), SweepDirection = SweepDirection.Clockwise } }
                        }}
                    },
                    Stroke = new SolidColorBrush(Colors.TrackFree),
                    StrokeThickness = 4
                });
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 20, X2 = 28, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 30, Y1 = 10, X2 = 38, Y2 = 30, Stroke = new SolidColorBrush(Colors.SignalRed), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 38, Y1 = 10, X2 = 30, Y2 = 30, Stroke = new SolidColorBrush(Colors.SignalRed), StrokeThickness = 3 });
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 22, X2 = 42, Y2 = 22, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 20, Y1 = 22, X2 = 38, Y2 = 8, Stroke = new SolidColorBrush(Colors.RouteSet), StrokeThickness = 3 });
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 18);
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 18, X2 = 42, Y2 = 18, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 20, Y1 = 18, X2 = 38, Y2 = 32, Stroke = new SolidColorBrush(Colors.RouteSet), StrokeThickness = 3 });
                canvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 14);
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 10, X2 = 42, Y2 = 30, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Line { X1 = 6, Y1 = 30, X2 = 42, Y2 = 10, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 3 });
                canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.Accent) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 15);
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(new Line { X1 = 6, Y1 = 20, X2 = 42, Y2 = 20, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                canvas.Children.Add(new Line { X1 = 24, Y1 = 4, X2 = 24, Y2 = 36, Stroke = new SolidColorBrush(Colors.TrackFree), StrokeThickness = 4 });
                break;

            case SignalBoxElementType.SignalMain:
                // Modern Ks-style main signal
                canvas.Children.Add(new Rectangle { Width = 16, Height = 30, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)), RadiusX = 3, RadiusY = 3 });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 5);
                canvas.Children.Add(CreateGlowingLed(20, 9, Colors.SignalRed, 12));
                canvas.Children.Add(CreateGlowingLed(20, 23, Color.FromArgb(40, 0, 255, 0), 12));
                break;

            case SignalBoxElementType.SignalDistant:
                canvas.Children.Add(new Ellipse
                {
                    Width = 22, Height = 22,
                    Fill = new SolidColorBrush(Colors.SignalYellow),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 40, 50, 70)),
                    StrokeThickness = 2
                });
                Canvas.SetLeft(canvas.Children[^1], 13);
                Canvas.SetTop(canvas.Children[^1], 9);
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(new Rectangle { Width = 14, Height = 34, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)), RadiusX = 3, RadiusY = 3 });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 3);
                canvas.Children.Add(CreateGlowingLed(21, 7, Colors.SignalGreen, 10));
                canvas.Children.Add(CreateGlowingLed(21, 24, Color.FromArgb(40, 255, 255, 0), 10));
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 26, Height = 14, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[^1], 11);
                Canvas.SetTop(canvas.Children[^1], 13);
                canvas.Children.Add(CreateGlowingLed(15, 15, Microsoft.UI.Colors.White, 8));
                canvas.Children.Add(CreateGlowingLed(27, 15, Microsoft.UI.Colors.White, 8));
                break;

            case SignalBoxElementType.SignalSpeed:
                var triangle = new Path
                {
                    Fill = new SolidColorBrush(Colors.SignalYellow),
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
                canvas.Children.Add(CreateGlowingLed(17, 13, Colors.SignalRed, 14));
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(new Rectangle { Width = 32, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(180, 25, 40, 60)), RadiusX = 3, RadiusY = 3, Stroke = new SolidColorBrush(Colors.Accent), StrokeThickness = 1 });
                Canvas.SetLeft(canvas.Children[^1], 8);
                Canvas.SetTop(canvas.Children[^1], 11);
                canvas.Children.Add(new TextBlock { Text = "Abc", FontSize = 11, Foreground = new SolidColorBrush(Colors.Accent) });
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

    private static Ellipse CreateGlowingLed(double x, double y, Color color, double size)
    {
        var led = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Microsoft.UI.Colors.White, Offset = 0 },
                    new GradientStop { Color = color, Offset = 0.4 },
                    new GradientStop { Color = Color.FromArgb(150, color.R, color.G, color.B), Offset = 1 }
                }
            }
        };
        Canvas.SetLeft(led, x);
        Canvas.SetTop(led, y);
        return led;
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
                container.Children.Add(new Line { X1 = 42, Y1 = 18, X2 = 54, Y2 = 42, Stroke = new SolidColorBrush(Colors.SignalRed), StrokeThickness = 4 });
                container.Children.Add(new Line { X1 = 54, Y1 = 18, X2 = 42, Y2 = 42, Stroke = new SolidColorBrush(Colors.SignalRed), StrokeThickness = 4 });
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateModernSwitch(element));
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
                container.Children.Add(CreateModernMainSignal(element));
                break;

            case SignalBoxElementType.SignalDistant:
                container.Children.Add(CreateModernDistantSignal(element));
                break;

            case SignalBoxElementType.SignalCombined:
                container.Children.Add(CreateModernCombinedSignal(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateModernShuntingSignal(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateModernFeedback(element));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(new Line { X1 = 4, Y1 = 24, X2 = 56, Y2 = 24, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 5 });
                container.Children.Add(new Rectangle { Width = 52, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.White), RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(container.Children[^1], 4);
                Canvas.SetTop(container.Children[^1], 38);
                break;

            case SignalBoxElementType.Label:
                var bg = new Rectangle
                {
                    Width = 50, Height = 24,
                    Fill = new SolidColorBrush(Color.FromArgb(180, 20, 35, 55)),
                    Stroke = new SolidColorBrush(Colors.Accent),
                    StrokeThickness = 1,
                    RadiusX = 4, RadiusY = 4
                };
                Canvas.SetLeft(bg, 5);
                Canvas.SetTop(bg, 18);
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

    private Grid CreateModernSwitch(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = GetStateColor(element.State, Colors);
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        // Main track
        grid.Children.Add(new Line
        {
            X1 = 4, Y1 = 30, X2 = 56, Y2 = 30,
            Stroke = new SolidColorBrush(isStraight ? trackColor : Color.FromArgb(100, trackColor.R, trackColor.G, trackColor.B)),
            StrokeThickness = 6
        });

        // Diverging track
        var divergeColor = !isStraight ? trackColor : Color.FromArgb(60, 100, 100, 100);
        grid.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 52, Y2 = isLeft ? 10 : 50,
            Stroke = new SolidColorBrush(divergeColor),
            StrokeThickness = 5
        });

        // Switch indicator with glow effect
        var indicatorColor = isStraight ? Colors.SignalGreen : Colors.SignalYellow;
        grid.Children.Add(new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Microsoft.UI.Colors.White, Offset = 0 },
                    new GradientStop { Color = indicatorColor, Offset = 0.4 },
                    new GradientStop { Color = Color.FromArgb(150, indicatorColor.R, indicatorColor.G, indicatorColor.B), Offset = 1 }
                }
            }
        });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 24);

        return grid;
    }

    private Grid CreateModernMainSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        // Mast
        grid.Children.Add(new Rectangle { Width = 4, Height = 42, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
        Canvas.SetLeft(grid.Children[^1], 12);
        Canvas.SetTop(grid.Children[^1], 9);

        // Signal head
        grid.Children.Add(new Rectangle { Width = 26, Height = 38, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)), RadiusX = 4, RadiusY = 4 });
        Canvas.SetLeft(grid.Children[^1], 22);
        Canvas.SetTop(grid.Children[^1], 11);

        // Red LED (top)
        var redOn = element.SignalAspect == SignalAspect.Hp0;
        grid.Children.Add(CreateLed(28, 15, redOn ? Colors.SignalRed : Color.FromArgb(30, 255, 0, 0), 16));

        // Green LED (bottom)
        var greenOn = element.SignalAspect == SignalAspect.Hp1;
        grid.Children.Add(CreateLed(28, 33, greenOn ? Colors.SignalGreen : Color.FromArgb(30, 0, 255, 0), 16));

        return grid;
    }

    private Grid CreateModernDistantSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        // Mast
        grid.Children.Add(new Rectangle { Width = 3, Height = 38, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
        Canvas.SetLeft(grid.Children[^1], 14);
        Canvas.SetTop(grid.Children[^1], 11);

        // Vorsignal disc
        var isExpectStop = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
        grid.Children.Add(new Ellipse
        {
            Width = 26, Height = 26,
            Fill = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Microsoft.UI.Colors.White, Offset = 0 },
                    new GradientStop { Color = isExpectStop ? Colors.SignalYellow : Color.FromArgb(40, 255, 200, 0), Offset = 0.4 },
                    new GradientStop { Color = isExpectStop ? Color.FromArgb(180, 255, 200, 0) : Color.FromArgb(20, 255, 200, 0), Offset = 1 }
                }
            },
            Stroke = new SolidColorBrush(Color.FromArgb(255, 50, 60, 80)),
            StrokeThickness = 2
        });
        Canvas.SetLeft(grid.Children[^1], 23);
        Canvas.SetTop(grid.Children[^1], 17);

        return grid;
    }

    private Grid CreateModernCombinedSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        // Tall signal head
        grid.Children.Add(new Rectangle { Width = 22, Height = 48, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)), RadiusX = 4, RadiusY = 4 });
        Canvas.SetLeft(grid.Children[^1], 19);
        Canvas.SetTop(grid.Children[^1], 6);

        // Top LED
        var topColor = element.SignalAspect switch
        {
            SignalAspect.Hp0 => Colors.SignalRed,
            SignalAspect.Ks1 => Colors.SignalGreen,
            _ => Color.FromArgb(30, 128, 128, 128)
        };
        grid.Children.Add(CreateLed(24, 12, topColor, 14));

        // Bottom LED
        var bottomColor = element.SignalAspect == SignalAspect.Ks2 ? Colors.SignalYellow : Color.FromArgb(30, 255, 255, 0);
        grid.Children.Add(CreateLed(24, 36, bottomColor, 14));

        return grid;
    }

    private Grid CreateModernShuntingSignal(SignalBoxElement element)
    {
        var grid = new Grid();

        // Compact housing
        grid.Children.Add(new Rectangle { Width = 38, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)), RadiusX = 3, RadiusY = 3 });
        Canvas.SetLeft(grid.Children[^1], 11);
        Canvas.SetTop(grid.Children[^1], 19);

        // Two white LEDs (Sh1)
        var isOn = element.SignalAspect == SignalAspect.Sh1;
        grid.Children.Add(CreateLed(17, 24, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(30, 255, 255, 255), 12));
        grid.Children.Add(CreateLed(33, 24, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(30, 255, 255, 255), 12));

        return grid;
    }

    private Grid CreateModernFeedback(SignalBoxElement element)
    {
        var grid = new Grid();
        var trackColor = element.State == SignalBoxElementState.Occupied ? Colors.TrackOccupied : Colors.TrackFree;

        // Track
        grid.Children.Add(new Line { X1 = 4, Y1 = 30, X2 = 56, Y2 = 30, Stroke = new SolidColorBrush(trackColor), StrokeThickness = 6 });

        // Feedback LED with glow
        var ledColor = element.State == SignalBoxElementState.Occupied ? Colors.SignalRed : Color.FromArgb(60, 255, 0, 0);
        grid.Children.Add(CreateLed(23, 17, ledColor, 16));

        // Address label
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
