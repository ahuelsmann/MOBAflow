// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.Backend.Interface;
using Moba.SharedUI.ViewModel;

using System;
using System.Globalization;

using Windows.UI;

/// <summary>
/// Abstract base class for classic dark ESTW-style Signal Box pages (Pages 5-8).
/// Features: Pure black background, thin colored track lines, minimal LED indicators,
/// German railway operations terminology, classic interlocking aesthetics.
/// Inspired by real ESTW-ZN (Zentrales Netz) and El S control room displays.
/// </summary>
public abstract class SignalBoxPageBase2 : SignalBoxPageBase
{
    private TextBlock? _clockText;
    private TextBlock? _dateText;

    protected SignalBoxPageBase2(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
        StartClock();
    }

    /// <summary>Gets the subtitle text for this style (e.g., "Zentralnetz" or "Betriebsfuehrung").</summary>
    protected abstract string SubtitleText { get; }

    /// <summary>Gets the station area identifiers shown in the header (e.g., ["91", "92"]).</summary>
    protected virtual string[] StationAreas => ["91", "92"];

    private void StartClock()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) =>
        {
            if (_clockText != null)
                _clockText.Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            if (_dateText != null)
                _dateText.Text = DateTime.Now.ToString("ddd dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
        };
        timer.Start();
    }

    protected override Border BuildHeader()
    {
        var grid = new Grid { Padding = new Thickness(12, 8, 12, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left: Date/Time and System Status
        var leftPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };

        var dateTimePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _dateText = new TextBlock
        {
            Text = DateTime.Now.ToString("ddd dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE")),
            FontSize = 11,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        dateTimePanel.Children.Add(_dateText);

        _clockText = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            FontSize = 16,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        dateTimePanel.Children.Add(_clockText);
        leftPanel.Children.Add(dateTimePanel);

        // Status indicator bar (ZN, ZU, etc.)
        var statusBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        statusBar.Children.Add(CreateClassicIndicator("ZN", Colors.SignalGreen, false));
        statusBar.Children.Add(CreateClassicIndicator("ZU", Colors.SignalRed, true));
        leftPanel.Children.Add(statusBar);

        // Signal Mode indicators
        var signalModes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        signalModes.Children.Add(new TextBlock { Text = "S", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) });

        var modePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        modePanel.Children.Add(CreateModeBox("GN", Colors.SignalGreen));
        modePanel.Children.Add(CreateModeBox("WS", Colors.TrackFree));
        modePanel.Children.Add(CreateModeBox("RT", Colors.SignalRed));
        modePanel.Children.Add(CreateModeBox("GE", Colors.SignalYellow));
        modePanel.Children.Add(CreateModeBox("BL", Colors.Blocked));
        signalModes.Children.Add(modePanel);
        leftPanel.Children.Add(signalModes);

        // LI indicator (Leitungszustand)
        leftPanel.Children.Add(CreateClassicBox("LI", Colors.SignalGreen));

        Grid.SetColumn(leftPanel, 0);
        grid.Children.Add(leftPanel);

        // Center: Routing/Track indicators
        var centerPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Orientation = Orientation.Horizontal, Spacing = 24 };
        centerPanel.Children.Add(CreateTrackIndicatorGroup());
        Grid.SetColumn(centerPanel, 1);
        grid.Children.Add(centerPanel);

        // Right: Station areas and function keys
        var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, HorizontalAlignment = HorizontalAlignment.Right };
        rightPanel.Children.Add(CreateStationAreaPanel());
        rightPanel.Children.Add(CreateFunctionKeyPanel());
        Grid.SetColumn(rightPanel, 2);
        grid.Children.Add(rightPanel);

        return new Border
        {
            Background = new SolidColorBrush(Colors.PanelBackground),
            BorderBrush = new SolidColorBrush(Colors.Border),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid
        };
    }

    private Border CreateClassicIndicator(string label, Color activeColor, bool isActive)
    {
        return new Border
        {
            Background = new SolidColorBrush(isActive ? activeColor : Color.FromArgb(60, activeColor.R, activeColor.G, activeColor.B)),
            Padding = new Thickness(6, 2, 6, 2),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(isActive ? Microsoft.UI.Colors.Black : Color.FromArgb(120, 255, 255, 255))
            }
        };
    }

    private static Border CreateModeBox(string label, Color color)
    {
        return new Border
        {
            Width = 20,
            Height = 14,
            Background = new SolidColorBrush(color),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 7,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private Border CreateClassicBox(string label, Color color)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)),
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(8, 4, 8, 4),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(color)
            }
        };
    }

    private StackPanel CreateTrackIndicatorGroup()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

        // T1-T5 indicators
        var trackGroup = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        for (int i = 1; i <= 5; i++)
        {
            var isActive = i <= 3;
            trackGroup.Children.Add(new Border
            {
                Width = 24,
                Height = 18,
                Background = new SolidColorBrush(isActive ? Colors.SignalYellow : Color.FromArgb(40, 255, 200, 0)),
                Child = new TextBlock
                {
                    Text = string.Format(CultureInfo.InvariantCulture, "T{0}", i),
                    FontSize = 9,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(isActive ? Microsoft.UI.Colors.Black : Color.FromArgb(80, 255, 255, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }
        panel.Children.Add(trackGroup);

        // EU, SF, SB, FPL indicators
        panel.Children.Add(CreateClassicLabel("EU", false));
        panel.Children.Add(CreateClassicLabel("SF", false));
        panel.Children.Add(CreateClassicLabel("SB", false));
        panel.Children.Add(CreateClassicLabel("FPL", false));

        return panel;
    }

    private static TextBlock CreateClassicLabel(string text, bool isActive)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(isActive ? Color.FromArgb(255, 255, 200, 0) : Color.FromArgb(100, 255, 255, 255))
        };
    }

    private StackPanel CreateStationAreaPanel()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

        foreach (var area in StationAreas)
        {
            panel.Children.Add(new Border
            {
                Width = 28,
                Height = 20,
                Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
                BorderBrush = new SolidColorBrush(Colors.SignalYellow),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = area,
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.SignalYellow),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }

        return panel;
    }

    private StackPanel CreateFunctionKeyPanel()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

        // Bottom status: EIN, UQ, ZN, ST
        var bottomRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        bottomRow.Children.Add(CreateFunctionKey("EIN", false));
        bottomRow.Children.Add(CreateFunctionKey("UQ", false));
        bottomRow.Children.Add(CreateFunctionKey("ZN", true));
        bottomRow.Children.Add(CreateFunctionKey("ST", false));
        panel.Children.Add(bottomRow);

        return panel;
    }

    private Border CreateFunctionKey(string label, bool isActive)
    {
        return new Border
        {
            MinWidth = 28,
            Height = 18,
            Background = new SolidColorBrush(isActive ? Colors.SignalGreen : Color.FromArgb(40, 100, 100, 100)),
            Padding = new Thickness(4, 0, 4, 0),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(isActive ? Microsoft.UI.Colors.Black : Color.FromArgb(120, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    protected override Border BuildStatusBar()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(12, 4, 12, 4), Spacing = 16 };

        // Connection status
        var connPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        connPanel.Children.Add(new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Z21.IsConnected ? Colors.SignalGreen : Colors.SignalRed)
        });
        connPanel.Children.Add(new TextBlock
        {
            Text = Z21.IsConnected ? "Z21 VERBUNDEN" : "Z21 GETRENNT",
            FontSize = 10,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(connPanel);

        panel.Children.Add(new Border { Width = 1, Height = 12, Background = new SolidColorBrush(Colors.Border) });

        // Mode
        panel.Children.Add(new TextBlock
        {
            Text = SubtitleText,
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.Accent),
            VerticalAlignment = VerticalAlignment.Center
        });

        panel.Children.Add(new Border { Width = 1, Height = 12, Background = new SolidColorBrush(Colors.Border) });

        // Status message
        StatusTextBlock = new TextBlock
        {
            Text = "Bereit",
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Colors.SignalGreen),
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(StatusTextBlock);

        return new Border
        {
            Background = new SolidColorBrush(Colors.PanelBackground),
            BorderBrush = new SolidColorBrush(Colors.Border),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = panel
        };
    }

    /// <summary>Creates a thin line track segment for classic ESTW displays.</summary>
    protected static Line CreateThinTrack(double x1, double y1, double x2, double y2, Color color, double thickness = 3)
    {
        return new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = thickness,
            StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Flat,
            StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Flat
        };
    }

    /// <summary>Creates a classic LED indicator (small filled circle).</summary>
    protected static Ellipse CreateClassicLed(double x, double y, Color color, double size = 8)
    {
        var led = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color)
        };
        Canvas.SetLeft(led, x);
        Canvas.SetTop(led, y);
        return led;
    }

    /// <summary>Creates a classic signal button (rectangular with LED).</summary>
    protected static Grid CreateClassicSignalButton(Color ledColor, bool isOn)
    {
        var grid = new Grid();

        // Button housing
        grid.Children.Add(new Rectangle
        {
            Width = 16,
            Height = 24,
            Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)),
            StrokeThickness = 1
        });

        // LED
        grid.Children.Add(new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(isOn ? ledColor : Color.FromArgb(40, ledColor.R, ledColor.G, ledColor.B)),
            Margin = new Thickness(3, 4, 3, 0),
            VerticalAlignment = VerticalAlignment.Top
        });

        return grid;
    }

    /// <summary>Creates a classic track number label.</summary>
    protected static TextBlock CreateTrackNumber(string number, Color color)
    {
        return new TextBlock
        {
            Text = number,
            FontSize = 9,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(color)
        };
    }

    /// <summary>Creates a station name header box.</summary>
    protected Border CreateStationHeader(string stationName, string areaNumber)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new TextBlock
        {
            Text = stationName,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            VerticalAlignment = VerticalAlignment.Center
        });

        var areaBox = new Border
        {
            Background = new SolidColorBrush(Colors.SignalYellow),
            Padding = new Thickness(6, 2, 6, 2),
            Child = new TextBlock
            {
                Text = areaNumber,
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black)
            }
        };
        Grid.SetColumn(areaBox, 1);
        grid.Children.Add(areaBox);

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 0, 8),
            Child = grid
        };
    }
}
