// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.Backend.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Model;

using System;
using System.Globalization;

using Windows.UI;

/// <summary>
/// MOBAesb - Electronic Signal Box page.
/// Uses Fluent Design System with standard WinUI Light/Dark theme support.
/// Signal colors (Red/Yellow/Green) remain constant for realistic representation.
/// </summary>
public sealed class SignalBoxPage : SignalBoxPageBase
{
    private TextBlock? _clockText;

    public SignalBoxPage(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
        StartClock();
    }

    protected override string PageTitle => "MOBAesb";

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
        var grid = new Grid { Padding = new Thickness(16, 8, 16, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left: Title and Clock
        var leftPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, VerticalAlignment = VerticalAlignment.Center };

        leftPanel.Children.Add(new TextBlock
        {
            Text = "MOBAesb",
            Style = (Style)Application.Current.Resources["TitleTextBlockStyle"]
        });

        _clockText = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center
        };
        leftPanel.Children.Add(_clockText);

        // Connection status
        var connectionPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        connectionPanel.Children.Add(new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(Z21.IsConnected ? SignalColors.SignalGreen : SignalColors.SignalRed)
        });
        connectionPanel.Children.Add(new TextBlock
        {
            Text = Z21.IsConnected ? "Z21 verbunden" : "Z21 getrennt",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            VerticalAlignment = VerticalAlignment.Center
        });
        leftPanel.Children.Add(connectionPanel);


        Grid.SetColumn(leftPanel, 0);
        grid.Children.Add(leftPanel);

        // Right: Grid toggle
        var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };

        var gridToggle = new Button
        {
            Content = new FontIcon { Glyph = "\uF5EF", FontSize = 14 }
        };
        ToolTipService.SetToolTip(gridToggle, "Raster ein/ausblenden");
        gridToggle.Click += (s, e) => IsGridVisible = !IsGridVisible;
        rightPanel.Children.Add(gridToggle);

        Grid.SetColumn(rightPanel, 2);
        grid.Children.Add(rightPanel);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid
        };
    }

    protected override UIElement CreateToolboxIcon(SignalBoxElementType type)
    {
        var canvas = new Canvas { Width = 32, Height = 24 };

        switch (type)
        {
            case SignalBoxElementType.TrackStraight:
                canvas.Children.Add(CreateTrackLine(2, 12, 30, 12, SignalColors.TrackFree, 3));
                break;

            case SignalBoxElementType.TrackCurve45:
                canvas.Children.Add(CreateTrackLine(2, 12, 26, 20, SignalColors.TrackFree, 3));
                break;

            case SignalBoxElementType.TrackCurve90:
                canvas.Children.Add(CreateTrackLine(2, 12, 16, 12, SignalColors.TrackFree, 3));
                canvas.Children.Add(CreateTrackLine(16, 12, 16, 22, SignalColors.TrackFree, 3));
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(CreateTrackLine(2, 12, 22, 12, SignalColors.TrackFree, 3));
                canvas.Children.Add(CreateTrackLine(26, 6, 26, 18, SignalColors.SignalRed, 4));
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(CreateTrackLine(2, 14, 30, 14, SignalColors.TrackFree, 3));
                canvas.Children.Add(CreateTrackLine(16, 14, 26, 6, SignalColors.RouteSet, 2));
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(CreateTrackLine(2, 10, 30, 10, SignalColors.TrackFree, 3));
                canvas.Children.Add(CreateTrackLine(16, 10, 26, 18, SignalColors.RouteSet, 2));
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(CreateTrackLine(2, 4, 30, 20, SignalColors.TrackFree, 3));
                canvas.Children.Add(CreateTrackLine(2, 20, 30, 4, SignalColors.TrackFree, 3));
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(CreateTrackLine(2, 12, 30, 12, SignalColors.TrackFree, 3));
                canvas.Children.Add(CreateTrackLine(16, 2, 16, 22, SignalColors.TrackFree, 3));
                break;

            case SignalBoxElementType.SignalMain:
            case SignalBoxElementType.SignalHvHp:
                canvas.Children.Add(new Rectangle { Width = 8, Height = 16, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 12);
                Canvas.SetTop(canvas.Children[^1], 4);
                canvas.Children.Add(CreateSignalLed(14, 6, SignalColors.SignalRed, 5));
                canvas.Children.Add(CreateSignalLed(14, 13, SignalColors.LedOff, 5));
                break;

            case SignalBoxElementType.SignalDistant:
            case SignalBoxElementType.SignalHvVr:
                canvas.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(SignalColors.SignalYellow) });
                Canvas.SetLeft(canvas.Children[^1], 10);
                Canvas.SetTop(canvas.Children[^1], 6);
                break;

            case SignalBoxElementType.SignalCombined:
            case SignalBoxElementType.SignalKsCombined:
            case SignalBoxElementType.SignalKsMain:
                // Use PathIcon for Ks-Combined signal
                return CreateKsSignalPathIcon(SignalBoxElementType.SignalKsCombined);

            case SignalBoxElementType.SignalKsDistant:
                // Use PathIcon for Ks-Distant signal
                return CreateKsSignalPathIcon(SignalBoxElementType.SignalKsDistant);
                break;

            case SignalBoxElementType.SignalSvMain:
            case SignalBoxElementType.SignalSvDistant:
                canvas.Children.Add(new Rectangle { Width = 18, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 7);
                Canvas.SetTop(canvas.Children[^1], 7);
                canvas.Children.Add(CreateSignalLed(10, 9, SignalColors.SignalRed, 5));
                canvas.Children.Add(CreateSignalLed(18, 9, SignalColors.LedOff, 5));
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 14, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 9);
                Canvas.SetTop(canvas.Children[^1], 8);
                canvas.Children.Add(CreateSignalLed(11, 10, SignalColors.SignalWhite, 4));
                canvas.Children.Add(CreateSignalLed(18, 10, SignalColors.SignalWhite, 4));
                break;

            case SignalBoxElementType.SignalSpeed:
                canvas.Children.Add(new Rectangle { Width = 12, Height = 12, Fill = new SolidColorBrush(SignalColors.SignalYellow) });
                Canvas.SetLeft(canvas.Children[^1], 10);
                Canvas.SetTop(canvas.Children[^1], 6);
                canvas.Children.Add(new TextBlock { Text = "8", FontSize = 8, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) });
                Canvas.SetLeft(canvas.Children[^1], 13);
                Canvas.SetTop(canvas.Children[^1], 6);
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(CreateTrackLine(2, 8, 30, 8, SignalColors.TrackFree, 3));
                canvas.Children.Add(new Rectangle { Width = 24, Height = 4, Fill = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)) });
                Canvas.SetLeft(canvas.Children[^1], 4);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(CreateTrackLine(2, 12, 30, 12, SignalColors.TrackOccupied, 3));
                canvas.Children.Add(new Rectangle { Width = 8, Height = 8, Fill = new SolidColorBrush(SignalColors.TrackOccupied) });
                Canvas.SetLeft(canvas.Children[^1], 12);
                Canvas.SetTop(canvas.Children[^1], 4);
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(new TextBlock
                {
                    Text = "Abc",
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                });
                Canvas.SetLeft(canvas.Children[^1], 8);
                Canvas.SetTop(canvas.Children[^1], 6);
                break;

            default:
                canvas.Children.Add(new Rectangle
                {
                    Width = 28, Height = 20,
                    Stroke = new SolidColorBrush(SignalColors.TrackFree),
                    StrokeThickness = 2
                });
                Canvas.SetLeft(canvas.Children[^1], 2);
                Canvas.SetTop(canvas.Children[^1], 2);
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
            Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
        };

        var trackColor = GetTrackStateColor(element.State);

        switch (element.Type)
        {
            case SignalBoxElementType.TrackStraight:
                container.Children.Add(CreateTrackLine(4, 30, 56, 30, trackColor));
                break;

            case SignalBoxElementType.TrackCurve45:
                container.Children.Add(CreateTrackLine(4, 30, 56, 4, trackColor));
                break;

            case SignalBoxElementType.TrackCurve90:
                container.Children.Add(CreateTrackLine(4, 30, 30, 30, trackColor));
                container.Children.Add(CreateTrackLine(30, 30, 30, 56, trackColor));
                break;

            case SignalBoxElementType.TrackEndStop:
                container.Children.Add(CreateTrackLine(4, 30, 40, 30, trackColor));
                container.Children.Add(CreateTrackLine(46, 18, 46, 42, SignalColors.SignalRed, 5));
                break;

            case SignalBoxElementType.SwitchLeft:
            case SignalBoxElementType.SwitchRight:
                container.Children.Add(CreateSwitchVisual(element, trackColor));
                break;

            case SignalBoxElementType.SwitchDouble:
                container.Children.Add(CreateTrackLine(4, 8, 56, 52, trackColor));
                container.Children.Add(CreateTrackLine(4, 52, 56, 8, trackColor));
                container.Children.Add(new Rectangle { Width = 8, Height = 8, Fill = new SolidColorBrush(SignalColors.SignalWhite) });
                Canvas.SetLeft(container.Children[^1], 26);
                Canvas.SetTop(container.Children[^1], 26);
                break;

            case SignalBoxElementType.SwitchCrossing:
                container.Children.Add(CreateTrackLine(4, 30, 56, 30, trackColor));
                container.Children.Add(CreateTrackLine(30, 4, 30, 56, trackColor));
                break;

            case SignalBoxElementType.SignalMain:
            case SignalBoxElementType.SignalHvHp:
                container.Children.Add(CreateHvMainSignalVisual(element));
                break;

            case SignalBoxElementType.SignalDistant:
            case SignalBoxElementType.SignalHvVr:
                container.Children.Add(CreateHvDistantSignalVisual(element));
                break;

            case SignalBoxElementType.SignalCombined:
            case SignalBoxElementType.SignalKsCombined:
            case SignalBoxElementType.SignalKsMain:
                container.Children.Add(CreateKsSignalVisual(element));
                break;

            case SignalBoxElementType.SignalKsDistant:
                container.Children.Add(CreateKsDistantSignalVisual(element));
                break;

            case SignalBoxElementType.SignalSvMain:
            case SignalBoxElementType.SignalSvDistant:
                container.Children.Add(CreateSvSignalVisual(element));
                break;

            case SignalBoxElementType.SignalShunting:
                container.Children.Add(CreateShuntingSignalVisual(element));
                break;

            case SignalBoxElementType.FeedbackPoint:
                container.Children.Add(CreateFeedbackVisual(element, trackColor));
                break;

            case SignalBoxElementType.Platform:
                container.Children.Add(CreateTrackLine(4, 24, 56, 24, trackColor));
                container.Children.Add(new Rectangle { Width = 52, Height = 6, Fill = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)) });
                Canvas.SetLeft(container.Children[^1], 4);
                Canvas.SetTop(container.Children[^1], 38);
                break;

            case SignalBoxElementType.Label:
                var txt = new TextBlock
                {
                    Text = element.Name.Length > 0 ? element.Name : "Text",
                    Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
                };
                Canvas.SetLeft(txt, 6);
                Canvas.SetTop(txt, 20);
                container.Children.Add(txt);
                break;

            default:
                container.Children.Add(new Rectangle
                {
                    Width = GridCellSize - 4,
                    Height = GridCellSize - 4,
                    Stroke = new SolidColorBrush(trackColor),
                    StrokeThickness = 2
                });
                break;
        }

        container.RenderTransform = new RotateTransform { Angle = element.Rotation, CenterX = GridCellSize / 2, CenterY = GridCellSize / 2 };
        SetupElementInteraction(container, element);

        return container;
    }

    private Grid CreateSwitchVisual(SignalBoxElement element, Color trackColor)
    {
        var grid = new Grid();
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.Type == SignalBoxElementType.SwitchLeft;

        var mainColor = isStraight ? trackColor : Color.FromArgb(100, trackColor.R, trackColor.G, trackColor.B);
        grid.Children.Add(CreateTrackLine(4, 30, 56, 30, mainColor));

        var divergeColor = !isStraight ? trackColor : Color.FromArgb(60, 100, 100, 100);
        grid.Children.Add(CreateTrackLine(30, 30, 52, isLeft ? 10 : 50, divergeColor, 3));

        var indicatorColor = isStraight ? SignalColors.SignalGreen : SignalColors.SignalYellow;
        grid.Children.Add(new Rectangle { Width = 8, Height = 8, Fill = new SolidColorBrush(indicatorColor) });
        Canvas.SetLeft(grid.Children[^1], 26);
        Canvas.SetTop(grid.Children[^1], 26);

        return grid;
    }

    private static Grid CreateHvMainSignalVisual(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 3, Height = 42, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
        Canvas.SetLeft(grid.Children[^1], 14);
        Canvas.SetTop(grid.Children[^1], 9);

        grid.Children.Add(new Rectangle { Width = 20, Height = 34, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 13);

        var redOn = element.SignalAspect == SignalAspect.Hp0;
        grid.Children.Add(CreateSignalLed(28, 17, redOn ? SignalColors.SignalRed : SignalColors.LedOff, 12));

        var greenOn = element.SignalAspect is SignalAspect.Hp1 or SignalAspect.Hp2;
        grid.Children.Add(CreateSignalLed(28, 33, greenOn ? SignalColors.SignalGreen : SignalColors.LedOff, 12));

        return grid;
    }

    private static Grid CreateHvDistantSignalVisual(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 3, Height = 38, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
        Canvas.SetLeft(grid.Children[^1], 14);
        Canvas.SetTop(grid.Children[^1], 11);

        var isExpectStop = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
        var discColor = isExpectStop ? SignalColors.SignalYellow : SignalColors.SignalGreen;
        grid.Children.Add(new Ellipse
        {
            Width = 22, Height = 22,
            Fill = new SolidColorBrush(discColor),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 50, 60, 80)),
            StrokeThickness = 2
        });
        Canvas.SetLeft(grid.Children[^1], 25);
        Canvas.SetTop(grid.Children[^1], 19);

        return grid;
    }

    private static Grid CreateKsSignalVisual(SignalBoxElement element)
    {
        var grid = new Grid();

        // Ks-Kombinationssignal mit Dreieck-Layout (wie auf Wikipedia)
        // Signalmast (dunkelgrauer vertikaler Balken)
        grid.Children.Add(new Rectangle { Width = 3, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
        Canvas.SetLeft(grid.Children[^1], 28);
        Canvas.SetTop(grid.Children[^1], 5);

        // Schwarzer Signalschirm (Dreieck-Anordnung)
        grid.Children.Add(new Rectangle { Width = 24, Height = 48, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) });
        Canvas.SetLeft(grid.Children[^1], 18);
        Canvas.SetTop(grid.Children[^1], 4);

        // Rote Lampe oben (Hp0 - Halt)
        var redOn = element.SignalAspect == SignalAspect.Hp0;
        grid.Children.Add(CreateSignalLed(26, 10, redOn ? SignalColors.SignalRed : SignalColors.LedOff, 10));

        // Grüne Lampe unten links (Ks1 - Fahrt)
        var greenOn = element.SignalAspect is SignalAspect.Ks1 or SignalAspect.Ks1Blink;
        grid.Children.Add(CreateSignalLed(20, 38, greenOn ? SignalColors.SignalGreen : SignalColors.LedOff, 10));

        // Gelbe Lampe unten rechts (Ks2 - Halt erwarten)
        var yellowOn = element.SignalAspect == SignalAspect.Ks2;
        grid.Children.Add(CreateSignalLed(32, 38, yellowOn ? SignalColors.SignalYellow : SignalColors.LedOff, 10));

        return grid;
    }

    private static Grid CreateKsDistantSignalVisual(SignalBoxElement element)
    {
        var grid = new Grid();

        // Signalmast
        grid.Children.Add(new Rectangle { Width = 3, Height = 38, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
        Canvas.SetLeft(grid.Children[^1], 28);
        Canvas.SetTop(grid.Children[^1], 11);

        // Schwarzer Signalschirm (horizontal für Vorsignal)
        grid.Children.Add(new Rectangle { Width = 32, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) });
        Canvas.SetLeft(grid.Children[^1], 14);
        Canvas.SetTop(grid.Children[^1], 21);

        // Grüne Lampe links (Ks1 erwarten - Fahrt erwarten)
        var greenOn = element.SignalAspect == SignalAspect.Ks1;
        grid.Children.Add(CreateSignalLed(18, 26, greenOn ? SignalColors.SignalGreen : SignalColors.LedOff, 10));

        // Gelbe Lampe rechts (Ks2 - Halt erwarten)
        var yellowOn = element.SignalAspect == SignalAspect.Ks2;
        grid.Children.Add(CreateSignalLed(36, 26, yellowOn ? SignalColors.SignalYellow : SignalColors.LedOff, 10));

        return grid;
    }

    private static Grid CreateSvSignalVisual(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 30, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) });
        Canvas.SetLeft(grid.Children[^1], 15);
        Canvas.SetTop(grid.Children[^1], 19);

        var redOn = element.SignalAspect == SignalAspect.Hp0 || element.SignalAspect == SignalAspect.Vr0;
        grid.Children.Add(CreateSignalLed(19, 24, redOn ? SignalColors.SignalRed : SignalColors.LedOff, 10));

        var greenOn = element.SignalAspect == SignalAspect.Hp1 || element.SignalAspect == SignalAspect.Vr1;
        grid.Children.Add(CreateSignalLed(33, 24, greenOn ? SignalColors.SignalGreen : SignalColors.LedOff, 10));

        return grid;
    }

    private static Grid CreateShuntingSignalVisual(SignalBoxElement element)
    {
        var grid = new Grid();

        grid.Children.Add(new Rectangle { Width = 34, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) });
        Canvas.SetLeft(grid.Children[^1], 13);
        Canvas.SetTop(grid.Children[^1], 21);

        var isOn = element.SignalAspect == SignalAspect.Sh1;
        grid.Children.Add(CreateSignalLed(18, 25, isOn ? SignalColors.SignalWhite : SignalColors.LedOff, 10));
        grid.Children.Add(CreateSignalLed(34, 25, isOn ? SignalColors.SignalWhite : SignalColors.LedOff, 10));

        return grid;
    }

    private Grid CreateFeedbackVisual(SignalBoxElement element, Color trackColor)
    {
        var grid = new Grid();

        grid.Children.Add(CreateTrackLine(4, 30, 56, 30, trackColor));

        var ledColor = element.State == SignalBoxElementState.Occupied ? SignalColors.SignalRed : SignalColors.LedOff;
        grid.Children.Add(new Rectangle { Width = 12, Height = 12, Fill = new SolidColorBrush(ledColor) });
        Canvas.SetLeft(grid.Children[^1], 24);
        Canvas.SetTop(grid.Children[^1], 17);

        if (element.Address > 0)
        {
            var addr = new TextBlock
            {
                Text = element.Address.ToString(CultureInfo.InvariantCulture),
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"]
            };
            Canvas.SetLeft(addr, 26);
            Canvas.SetTop(addr, 44);
            grid.Children.Add(addr);
        }

        return grid;
    }
}
