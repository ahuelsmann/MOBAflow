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
/// Consolidated Signal Box Page with Light/Dark theme support.
/// Features: Classic "verspielte" header with date/time/status indicators,
/// straight track lines without rounded corners (ILTIS/Classic style),
/// no footer/status bar, grid toggle functionality.
/// Signals grouped by system: H/V, Hl, Ks, Sv.
/// </summary>
public sealed class SignalBoxPage : SignalBoxPageBase
{
    private SignalBoxTheme _currentTheme = SignalBoxTheme.Dark;
    private SignalBoxColorScheme _colors;
    private TextBlock? _clockText;
    private TextBlock? _dateText;
    private Grid? _rootGrid;

    public SignalBoxPage(MainWindowViewModel viewModel, IZ21 z21)
        : base(viewModel, z21)
    {
        _colors = SignalBoxColorScheme.Dark;
        StartClock();
    }

    protected override string StyleName => "MOBAflow Stellwerk";

    protected override SignalBoxColorScheme Colors => _colors;

    protected override bool ShowStatusBar => false;

    /// <summary>Gets or sets the current theme. Changing this refreshes the UI.</summary>
    public SignalBoxTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                _colors = SignalBoxColorScheme.GetTheme(value);
                RefreshTheme();
            }
        }
    }

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

    private void RefreshTheme()
    {
        if (_rootGrid != null)
            _rootGrid.Background = new SolidColorBrush(Colors.Background);

        foreach (var element in Elements)
            RefreshElementVisual(element);
    }

    protected override Border BuildHeader()
    {
        var grid = new Grid { Padding = new Thickness(12, 8, 12, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left: Date/Time and Status Indicators
        var leftPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };

        var dateTimePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _dateText = new TextBlock
        {
            Text = DateTime.Now.ToString("ddd dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE")),
            FontSize = 11,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Colors.TextPrimary)
        };
        dateTimePanel.Children.Add(_dateText);

        _clockText = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            FontSize = 16,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.TextPrimary)
        };
        dateTimePanel.Children.Add(_clockText);
        leftPanel.Children.Add(dateTimePanel);

        // Status indicators (ZN, ZU style)
        var statusBar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        statusBar.Children.Add(CreateIndicator("ZN", Colors.SignalGreen, false));
        statusBar.Children.Add(CreateIndicator("ZU", Colors.SignalRed, true));
        leftPanel.Children.Add(statusBar);

        // Signal mode indicators (S GN WS RT GE BL)
        var signalModes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        signalModes.Children.Add(new TextBlock { Text = "S", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Colors.TextPrimary) });

        var modePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        modePanel.Children.Add(CreateModeBox("GN", Colors.SignalGreen));
        modePanel.Children.Add(CreateModeBox("WS", Colors.TrackFree));
        modePanel.Children.Add(CreateModeBox("RT", Colors.SignalRed));
        modePanel.Children.Add(CreateModeBox("GE", Colors.SignalYellow));
        modePanel.Children.Add(CreateModeBox("BL", Colors.Blocked));
        signalModes.Children.Add(modePanel);
        leftPanel.Children.Add(signalModes);

        // Connection indicator
        leftPanel.Children.Add(CreateConnectionBox());

        Grid.SetColumn(leftPanel, 0);
        grid.Children.Add(leftPanel);

        // Center: Track indicators and theme toggle
        var centerPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Orientation = Orientation.Horizontal, Spacing = 24 };
        centerPanel.Children.Add(CreateTrackIndicators());
        centerPanel.Children.Add(CreateThemeToggle());
        centerPanel.Children.Add(CreateGridToggle());
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

    private Border CreateIndicator(string label, Color activeColor, bool isActive)
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

    private Border CreateConnectionBox()
    {
        var color = Z21.IsConnected ? Colors.SignalGreen : Colors.SignalRed;
        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)),
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(8, 4, 8, 4),
            Child = new TextBlock
            {
                Text = Z21.IsConnected ? "Z21" : "---",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(color)
            }
        };
    }

    private StackPanel CreateTrackIndicators()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

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

        return panel;
    }

    private Border CreateThemeToggle()
    {
        var btn = new Button
        {
            Content = _currentTheme == SignalBoxTheme.Dark ? "\u2600" : "\u263D",
            FontSize = 16,
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Colors.ButtonBackground),
            BorderBrush = new SolidColorBrush(Colors.ButtonBorder)
        };
        ToolTipService.SetToolTip(btn, _currentTheme == SignalBoxTheme.Dark ? "Zu hellem Theme wechseln" : "Zu dunklem Theme wechseln");
        btn.Click += (s, e) =>
        {
            CurrentTheme = _currentTheme == SignalBoxTheme.Dark ? SignalBoxTheme.Light : SignalBoxTheme.Dark;
            Content = BuildLayoutWithTheme();
        };
        return new Border { Child = btn };
    }

    private Border CreateGridToggle()
    {
        var btn = new Button
        {
            Content = "\u25A6",
            FontSize = 14,
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Colors.ButtonBackground),
            BorderBrush = new SolidColorBrush(Colors.ButtonBorder)
        };
        ToolTipService.SetToolTip(btn, "Raster ein/ausblenden");
        btn.Click += (s, e) => IsGridVisible = !IsGridVisible;
        return new Border { Child = btn };
    }

    private StackPanel CreateStationAreaPanel()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        string[] areas = ["91", "92"];

        foreach (var area in areas)
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
        panel.Children.Add(CreateFunctionKey("EIN", false));
        panel.Children.Add(CreateFunctionKey("UQ", false));
        panel.Children.Add(CreateFunctionKey("ZN", true));
        panel.Children.Add(CreateFunctionKey("ST", false));
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

    private Grid BuildLayoutWithTheme()
    {
        var layout = (Grid)Content;
        return layout;
    }

    protected override Border BuildToolbox()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Toolbox = new StackPanel { Spacing = 12, Padding = new Thickness(12) };

        // Header
        var headerPanel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 16) };
        headerPanel.Children.Add(new TextBlock
        {
            Text = "TOOLBOX",
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.TextSecondary),
            CharacterSpacing = 100
        });
        headerPanel.Children.Add(new Border
        {
            Height = 2,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops =
                {
                    new GradientStop { Color = Colors.Accent, Offset = 0 },
                    new GradientStop { Color = Microsoft.UI.Colors.Transparent, Offset = 1 }
                }
            }
        });
        Toolbox.Children.Add(headerPanel);

        // Track Elements
        Toolbox.Children.Add(CreateToolboxCategory("GLEISE"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Gerade", SignalBoxElementType.TrackStraight),
            ("Kurve 45", SignalBoxElementType.TrackCurve45),
            ("Kurve 90", SignalBoxElementType.TrackCurve90),
            ("Prellbock", SignalBoxElementType.TrackEndStop)
        ]));

        // Switch Elements
        Toolbox.Children.Add(CreateToolboxCategory("WEICHEN"));
        Toolbox.Children.Add(CreateIconGrid([
            ("EW links", SignalBoxElementType.SwitchLeft),
            ("EW rechts", SignalBoxElementType.SwitchRight),
            ("DKW", SignalBoxElementType.SwitchDouble),
            ("Kreuzung", SignalBoxElementType.SwitchCrossing)
        ]));

        // H/V-System Signals
        Toolbox.Children.Add(CreateToolboxCategory("H/V-SYSTEM"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Hp-Signal", SignalBoxElementType.SignalHvHp),
            ("Vr-Signal", SignalBoxElementType.SignalHvVr)
        ]));

        // Ks-System Signals
        Toolbox.Children.Add(CreateToolboxCategory("Ks-SYSTEM"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Ks-Haupt", SignalBoxElementType.SignalKsMain),
            ("Ks-Vor", SignalBoxElementType.SignalKsDistant),
            ("Ks-Kombi", SignalBoxElementType.SignalKsCombined)
        ]));

        // Sv-System Signals (S-Bahn)
        Toolbox.Children.Add(CreateToolboxCategory("Sv-SYSTEM"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Sv-Haupt", SignalBoxElementType.SignalSvMain),
            ("Sv-Vor", SignalBoxElementType.SignalSvDistant)
        ]));

        // General Signals
        Toolbox.Children.Add(CreateToolboxCategory("RANGIERSIG"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Rangier", SignalBoxElementType.SignalShunting),
            ("Zs 3", SignalBoxElementType.SignalSpeed)
        ]));

        // Additional Elements
        Toolbox.Children.Add(CreateToolboxCategory("ZUSATZ"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Bahnsteig", SignalBoxElementType.Platform),
            ("Melder", SignalBoxElementType.FeedbackPoint),
            ("Label", SignalBoxElementType.Label)
        ]));

        scroll.Content = Toolbox;

        return new Border
        {
            Background = new SolidColorBrush(Colors.PanelBackground),
            Child = scroll
        };
    }

    private Border CreateToolboxCategory(string name)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = name,
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.Accent),
            CharacterSpacing = 50
        });

        return new Border { Padding = new Thickness(0, 8, 0, 4), Child = panel };
    }

    private Grid CreateIconGrid((string tooltip, SignalBoxElementType type)[] items)
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        int row = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (i % 2 == 0)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var (tooltip, type) = items[i];
            var button = CreateToolboxIconButton(tooltip, type);
            Grid.SetRow(button, row);
            Grid.SetColumn(button, i % 2);
            grid.Children.Add(button);

            if (i % 2 == 1) row++;
        }

        return grid;
    }

    private Border CreateToolboxIconButton(string tooltip, SignalBoxElementType type)
    {
        var button = new Border
        {
            Width = 72,
            Height = 56,
            Background = new SolidColorBrush(Colors.ButtonBackground),
            BorderBrush = new SolidColorBrush(Colors.ButtonBorder),
            BorderThickness = new Thickness(1),
            Tag = type,
            CanDrag = true,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var content = new Grid();
        content.Children.Add(CreateToolboxIcon(type));

        var label = new TextBlock
        {
            Text = tooltip,
            FontSize = 9,
            Foreground = new SolidColorBrush(Colors.TextMuted),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 4)
        };
        content.Children.Add(label);
        button.Child = content;

        ToolTipService.SetToolTip(button, tooltip);

        button.PointerEntered += (s, e) =>
        {
            button.Background = new SolidColorBrush(Colors.ButtonHover);
            button.BorderBrush = new SolidColorBrush(Colors.Accent);
        };
        button.PointerExited += (s, e) =>
        {
            button.Background = new SolidColorBrush(Colors.ButtonBackground);
            button.BorderBrush = new SolidColorBrush(Colors.ButtonBorder);
        };

        button.DragStarting += (s, e) =>
        {
            e.Data.SetText(string.Format(CultureInfo.InvariantCulture, "{0}{1}", DragDataNewElement, (int)type));
            e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            LogMessage("DRAG", string.Format(CultureInfo.InvariantCulture, "New: {0}", type));
        };

        return button;
    }

    protected override UIElement CreateToolboxIcon(SignalBoxElementType type)
    {
        var canvas = new Canvas { Width = 48, Height = 40 };

        switch (type)
        {
            case SignalBoxElementType.TrackStraight:
                canvas.Children.Add(CreateStraightLine(6, 20, 42, 20, Colors.TrackFree, 3));
                break;

            case SignalBoxElementType.TrackCurve45:
                canvas.Children.Add(CreateStraightLine(6, 20, 34, 32, Colors.TrackFree, 3));
                break;

            case SignalBoxElementType.TrackCurve90:
                canvas.Children.Add(CreateStraightLine(6, 20, 24, 20, Colors.TrackFree, 3));
                canvas.Children.Add(CreateStraightLine(24, 20, 24, 38, Colors.TrackFree, 3));
                break;

            case SignalBoxElementType.TrackEndStop:
                canvas.Children.Add(CreateStraightLine(6, 20, 32, 20, Colors.TrackFree, 3));
                canvas.Children.Add(CreateStraightLine(36, 12, 36, 28, Colors.SignalRed, 4));
                break;

            case SignalBoxElementType.SwitchLeft:
                canvas.Children.Add(CreateStraightLine(6, 22, 42, 22, Colors.TrackFree, 3));
                canvas.Children.Add(CreateStraightLine(24, 22, 38, 10, Colors.RouteSet, 2));
                canvas.Children.Add(new Rectangle { Width = 4, Height = 4, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 22);
                Canvas.SetTop(canvas.Children[^1], 20);
                break;

            case SignalBoxElementType.SwitchRight:
                canvas.Children.Add(CreateStraightLine(6, 18, 42, 18, Colors.TrackFree, 3));
                canvas.Children.Add(CreateStraightLine(24, 18, 38, 30, Colors.RouteSet, 2));
                canvas.Children.Add(new Rectangle { Width = 4, Height = 4, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 22);
                Canvas.SetTop(canvas.Children[^1], 16);
                break;

            case SignalBoxElementType.SwitchDouble:
                canvas.Children.Add(CreateStraightLine(6, 10, 42, 30, Colors.TrackFree, 3));
                canvas.Children.Add(CreateStraightLine(6, 30, 42, 10, Colors.TrackFree, 3));
                canvas.Children.Add(new Rectangle { Width = 6, Height = 6, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 17);
                break;

            case SignalBoxElementType.SwitchCrossing:
                canvas.Children.Add(CreateStraightLine(6, 20, 42, 20, Colors.TrackFree, 3));
                canvas.Children.Add(CreateStraightLine(24, 4, 24, 36, Colors.TrackFree, 3));
                break;

            case SignalBoxElementType.SignalMain:
                canvas.Children.Add(new Rectangle { Width = 10, Height = 18, Fill = new SolidColorBrush(Colors.SignalRed) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 8);
                break;

            case SignalBoxElementType.SignalDistant:
                canvas.Children.Add(new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = new SolidColorBrush(Colors.SignalYellow)
                });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 13);
                break;

            case SignalBoxElementType.SignalCombined:
                canvas.Children.Add(new Rectangle { Width = 10, Height = 22, Fill = new SolidColorBrush(Colors.SignalGreen) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 6);
                break;

            case SignalBoxElementType.SignalShunting:
                canvas.Children.Add(new Rectangle { Width = 18, Height = 8, Fill = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)) });
                Canvas.SetLeft(canvas.Children[^1], 15);
                Canvas.SetTop(canvas.Children[^1], 16);
                canvas.Children.Add(CreateClassicLed(18, 17, Microsoft.UI.Colors.White, 4));
                canvas.Children.Add(CreateClassicLed(27, 17, Microsoft.UI.Colors.White, 4));
                break;

            case SignalBoxElementType.SignalSpeed:
                canvas.Children.Add(new Rectangle { Width = 16, Height = 16, Fill = new SolidColorBrush(Colors.SignalYellow) });
                Canvas.SetLeft(canvas.Children[^1], 16);
                Canvas.SetTop(canvas.Children[^1], 10);
                canvas.Children.Add(new TextBlock { Text = "8", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });
                Canvas.SetLeft(canvas.Children[^1], 21);
                Canvas.SetTop(canvas.Children[^1], 11);
                break;

            // H/V-System Signals
            case SignalBoxElementType.SignalHvHp:
                canvas.Children.Add(new Rectangle { Width = 3, Height = 24, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
                Canvas.SetLeft(canvas.Children[^1], 12);
                Canvas.SetTop(canvas.Children[^1], 8);
                canvas.Children.Add(new Rectangle { Width = 14, Height = 20, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(canvas.Children[^1], 22);
                Canvas.SetTop(canvas.Children[^1], 8);
                canvas.Children.Add(CreateClassicLed(25, 10, Colors.SignalRed, 6));
                canvas.Children.Add(CreateClassicLed(25, 20, Color.FromArgb(30, 0, 255, 0), 6));
                break;

            case SignalBoxElementType.SignalHvVr:
                canvas.Children.Add(new Rectangle { Width = 3, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
                Canvas.SetLeft(canvas.Children[^1], 12);
                Canvas.SetTop(canvas.Children[^1], 9);
                canvas.Children.Add(new Ellipse { Width = 16, Height = 16, Fill = new SolidColorBrush(Colors.SignalYellow), Stroke = new SolidColorBrush(Color.FromArgb(255, 50, 60, 80)), StrokeThickness = 1 });
                Canvas.SetLeft(canvas.Children[^1], 22);
                Canvas.SetTop(canvas.Children[^1], 12);
                break;

            // Ks-System Signals
            case SignalBoxElementType.SignalKsMain:
                canvas.Children.Add(new Rectangle { Width = 14, Height = 20, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 10);
                canvas.Children.Add(CreateClassicLed(20, 12, Colors.SignalRed, 8));
                canvas.Children.Add(CreateClassicLed(20, 22, Color.FromArgb(30, 0, 255, 0), 6));
                break;

            case SignalBoxElementType.SignalKsDistant:
                canvas.Children.Add(new Rectangle { Width = 14, Height = 16, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 12);
                canvas.Children.Add(CreateClassicLed(20, 16, Colors.SignalYellow, 8));
                break;

            case SignalBoxElementType.SignalKsCombined:
                canvas.Children.Add(new Rectangle { Width = 14, Height = 26, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(canvas.Children[^1], 17);
                Canvas.SetTop(canvas.Children[^1], 7);
                canvas.Children.Add(CreateClassicLed(20, 9, Colors.SignalGreen, 8));
                canvas.Children.Add(CreateClassicLed(20, 21, Color.FromArgb(30, 255, 255, 0), 6));
                break;

            // Sv-System Signals (S-Bahn)
            case SignalBoxElementType.SignalSvMain:
                canvas.Children.Add(new Rectangle { Width = 18, Height = 14, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(canvas.Children[^1], 15);
                Canvas.SetTop(canvas.Children[^1], 13);
                canvas.Children.Add(CreateClassicLed(18, 16, Colors.SignalRed, 5));
                canvas.Children.Add(CreateClassicLed(26, 16, Color.FromArgb(30, 0, 255, 0), 5));
                break;

            case SignalBoxElementType.SignalSvDistant:
                canvas.Children.Add(new Rectangle { Width = 18, Height = 14, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(canvas.Children[^1], 15);
                Canvas.SetTop(canvas.Children[^1], 13);
                canvas.Children.Add(CreateClassicLed(18, 16, Colors.SignalYellow, 5));
                canvas.Children.Add(CreateClassicLed(26, 16, Colors.SignalYellow, 5));
                break;

            case SignalBoxElementType.Platform:
                canvas.Children.Add(CreateStraightLine(6, 14, 42, 14, Colors.TrackFree, 3));
                canvas.Children.Add(new Rectangle { Width = 28, Height = 3, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                Canvas.SetLeft(canvas.Children[^1], 10);
                Canvas.SetTop(canvas.Children[^1], 24);
                break;

            case SignalBoxElementType.FeedbackPoint:
                canvas.Children.Add(CreateStraightLine(6, 20, 42, 20, Colors.TrackOccupied, 3));
                canvas.Children.Add(new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(Colors.TrackOccupied) });
                Canvas.SetLeft(canvas.Children[^1], 19);
                Canvas.SetTop(canvas.Children[^1], 8);
                break;

            case SignalBoxElementType.Label:
                canvas.Children.Add(new Rectangle { Width = 32, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(180, 25, 40, 60)), Stroke = new SolidColorBrush(Colors.Accent), StrokeThickness = 1 });
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
                    StrokeThickness = 2
                });
                Canvas.SetLeft(canvas.Children[^1], 4);
                Canvas.SetTop(canvas.Children[^1], 4);
                break;
        }

        return canvas;
    }

    private static Line CreateStraightLine(double x1, double y1, double x2, double y2, Color color, double thickness)
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

    private static Ellipse CreateClassicLed(double x, double y, Color color, double size)
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
                    container.Children.Add(CreateStraightLine(4, 30, 56, 30, trackColor, 4));
                    break;

                case SignalBoxElementType.TrackCurve45:
                    container.Children.Add(CreateStraightLine(4, 30, 56, 4, trackColor, 4));
                    break;

                case SignalBoxElementType.TrackCurve90:
                    container.Children.Add(CreateStraightLine(4, 30, 30, 30, trackColor, 4));
                    container.Children.Add(CreateStraightLine(30, 30, 30, 56, trackColor, 4));
                    break;

                case SignalBoxElementType.TrackEndStop:
                    container.Children.Add(CreateStraightLine(4, 30, 40, 30, trackColor, 4));
                    container.Children.Add(CreateStraightLine(46, 18, 46, 42, Colors.SignalRed, 5));
                    break;

                case SignalBoxElementType.SwitchLeft:
                case SignalBoxElementType.SwitchRight:
                    container.Children.Add(CreateClassicSwitch(element));
                    break;

                case SignalBoxElementType.SwitchDouble:
                    container.Children.Add(CreateStraightLine(4, 8, 56, 52, trackColor, 4));
                    container.Children.Add(CreateStraightLine(4, 52, 56, 8, trackColor, 4));
                    container.Children.Add(new Rectangle { Width = 8, Height = 8, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                    Canvas.SetLeft(container.Children[^1], 26);
                    Canvas.SetTop(container.Children[^1], 26);
                    break;

                case SignalBoxElementType.SwitchCrossing:
                    container.Children.Add(CreateStraightLine(4, 30, 56, 30, trackColor, 4));
                    container.Children.Add(CreateStraightLine(30, 4, 30, 56, trackColor, 4));
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

                // H/V-System Signals
                case SignalBoxElementType.SignalHvHp:
                    container.Children.Add(CreateHvHpSignal(element));
                    break;

                case SignalBoxElementType.SignalHvVr:
                    container.Children.Add(CreateHvVrSignal(element));
                    break;

                // Ks-System Signals
                case SignalBoxElementType.SignalKsMain:
                    container.Children.Add(CreateKsMainSignal(element));
                    break;

                case SignalBoxElementType.SignalKsDistant:
                    container.Children.Add(CreateKsDistantSignal(element));
                    break;

                case SignalBoxElementType.SignalKsCombined:
                    container.Children.Add(CreateClassicCombinedSignal(element));
                    break;

                // Sv-System Signals
                case SignalBoxElementType.SignalSvMain:
                    container.Children.Add(CreateSvMainSignal(element));
                    break;

                case SignalBoxElementType.SignalSvDistant:
                    container.Children.Add(CreateSvDistantSignal(element));
                    break;

                case SignalBoxElementType.FeedbackPoint:
                    container.Children.Add(CreateClassicFeedback(element));
                    break;

                case SignalBoxElementType.Platform:
                    container.Children.Add(CreateStraightLine(4, 24, 56, 24, trackColor, 4));
                    container.Children.Add(new Rectangle { Width = 52, Height = 6, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) });
                    Canvas.SetLeft(container.Children[^1], 4);
                    Canvas.SetTop(container.Children[^1], 38);
                    break;

                case SignalBoxElementType.Label:
                    var bg = new Rectangle
                    {
                        Width = 50, Height = 24,
                        Fill = new SolidColorBrush(Color.FromArgb(180, 20, 35, 55)),
                        Stroke = new SolidColorBrush(Colors.Accent),
                        StrokeThickness = 1
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
                        StrokeThickness = 2
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

            var mainColor = isStraight ? trackColor : Color.FromArgb(100, trackColor.R, trackColor.G, trackColor.B);
            grid.Children.Add(CreateStraightLine(4, 30, 56, 30, mainColor, 4));

            var divergeColor = !isStraight ? trackColor : Color.FromArgb(60, 100, 100, 100);
            grid.Children.Add(CreateStraightLine(30, 30, 52, isLeft ? 10 : 50, divergeColor, 3));

            var indicatorColor = isStraight ? Colors.SignalGreen : Colors.SignalYellow;
            grid.Children.Add(new Rectangle
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(indicatorColor)
            });
            Canvas.SetLeft(grid.Children[^1], 26);
            Canvas.SetTop(grid.Children[^1], 26);

            return grid;
        }

        private Grid CreateClassicMainSignal(SignalBoxElement element)
        {
            var grid = new Grid();

            grid.Children.Add(new Rectangle { Width = 3, Height = 42, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
            Canvas.SetLeft(grid.Children[^1], 14);
            Canvas.SetTop(grid.Children[^1], 9);

            grid.Children.Add(new Rectangle { Width = 20, Height = 34, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
            Canvas.SetLeft(grid.Children[^1], 24);
            Canvas.SetTop(grid.Children[^1], 13);

            var redOn = element.SignalAspect == SignalAspect.Hp0;
            grid.Children.Add(CreateClassicLed(28, 17, redOn ? Colors.SignalRed : Color.FromArgb(30, 255, 0, 0), 12));

            var greenOn = element.SignalAspect == SignalAspect.Hp1;
            grid.Children.Add(CreateClassicLed(28, 33, greenOn ? Colors.SignalGreen : Color.FromArgb(30, 0, 255, 0), 12));

            return grid;
        }

        private Grid CreateClassicDistantSignal(SignalBoxElement element)
        {
            var grid = new Grid();

            grid.Children.Add(new Rectangle { Width = 3, Height = 38, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
            Canvas.SetLeft(grid.Children[^1], 14);
            Canvas.SetTop(grid.Children[^1], 11);

            var isExpectStop = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
            var discColor = isExpectStop ? Colors.SignalYellow : Color.FromArgb(40, 255, 200, 0);
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

        private Grid CreateClassicCombinedSignal(SignalBoxElement element)
        {
            var grid = new Grid();

            grid.Children.Add(new Rectangle { Width = 18, Height = 44, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
            Canvas.SetLeft(grid.Children[^1], 21);
            Canvas.SetTop(grid.Children[^1], 8);

            var topColor = element.SignalAspect switch
            {
                SignalAspect.Hp0 => Colors.SignalRed,
                SignalAspect.Ks1 => Colors.SignalGreen,
                _ => Color.FromArgb(30, 128, 128, 128)
            };
            grid.Children.Add(CreateClassicLed(25, 14, topColor, 12));

            var bottomColor = element.SignalAspect == SignalAspect.Ks2 ? Colors.SignalYellow : Color.FromArgb(30, 255, 255, 0);
            grid.Children.Add(CreateClassicLed(25, 36, bottomColor, 12));

            return grid;
        }

        private Grid CreateClassicShuntingSignal(SignalBoxElement element)
        {
            var grid = new Grid();

            grid.Children.Add(new Rectangle { Width = 34, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
            Canvas.SetLeft(grid.Children[^1], 13);
            Canvas.SetTop(grid.Children[^1], 21);

            var isOn = element.SignalAspect == SignalAspect.Sh1;
            grid.Children.Add(CreateClassicLed(18, 25, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(30, 255, 255, 255), 10));
            grid.Children.Add(CreateClassicLed(34, 25, isOn ? Microsoft.UI.Colors.White : Color.FromArgb(30, 255, 255, 255), 10));

            return grid;
        }

            private Grid CreateClassicFeedback(SignalBoxElement element)
            {
                var grid = new Grid();
                var trackColor = element.State == SignalBoxElementState.Occupied ? Colors.TrackOccupied : Colors.TrackFree;

                grid.Children.Add(CreateStraightLine(4, 30, 56, 30, trackColor, 4));

                var ledColor = element.State == SignalBoxElementState.Occupied ? Colors.SignalRed : Color.FromArgb(60, 255, 0, 0);
                grid.Children.Add(new Rectangle { Width = 12, Height = 12, Fill = new SolidColorBrush(ledColor) });
                Canvas.SetLeft(grid.Children[^1], 24);
                Canvas.SetTop(grid.Children[^1], 17);

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

            private Grid CreateHvHpSignal(SignalBoxElement element)
            {
                var grid = new Grid();

                grid.Children.Add(new Rectangle { Width = 3, Height = 42, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
                Canvas.SetLeft(grid.Children[^1], 14);
                Canvas.SetTop(grid.Children[^1], 9);

                grid.Children.Add(new Rectangle { Width = 20, Height = 36, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(grid.Children[^1], 24);
                Canvas.SetTop(grid.Children[^1], 11);

                var redOn = element.SignalAspect == SignalAspect.Hp0;
                grid.Children.Add(CreateClassicLed(28, 15, redOn ? Colors.SignalRed : Color.FromArgb(30, 255, 0, 0), 12));

                var greenOn = element.SignalAspect == SignalAspect.Hp1;
                grid.Children.Add(CreateClassicLed(28, 31, greenOn ? Colors.SignalGreen : Color.FromArgb(30, 0, 255, 0), 12));

                return grid;
            }

            private Grid CreateHvVrSignal(SignalBoxElement element)
            {
                var grid = new Grid();

                grid.Children.Add(new Rectangle { Width = 3, Height = 38, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
                Canvas.SetLeft(grid.Children[^1], 14);
                Canvas.SetTop(grid.Children[^1], 11);

                var isExpectStop = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
                var discColor = isExpectStop ? Colors.SignalYellow : Colors.SignalGreen;
                grid.Children.Add(new Ellipse
                {
                    Width = 24, Height = 24,
                    Fill = new SolidColorBrush(discColor),
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 50, 60, 80)),
                    StrokeThickness = 2
                });
                Canvas.SetLeft(grid.Children[^1], 24);
                Canvas.SetTop(grid.Children[^1], 18);

                return grid;
            }

            private Grid CreateKsMainSignal(SignalBoxElement element)
            {
                var grid = new Grid();

                grid.Children.Add(new Rectangle { Width = 20, Height = 40, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(grid.Children[^1], 20);
                Canvas.SetTop(grid.Children[^1], 10);

                var redOn = element.SignalAspect == SignalAspect.Hp0;
                grid.Children.Add(CreateClassicLed(24, 14, redOn ? Colors.SignalRed : Color.FromArgb(30, 255, 0, 0), 12));

                var greenOn = element.SignalAspect is SignalAspect.Ks1 or SignalAspect.Ks1Blink;
                grid.Children.Add(CreateClassicLed(24, 34, greenOn ? Colors.SignalGreen : Color.FromArgb(30, 0, 255, 0), 12));

                return grid;
            }

            private Grid CreateKsDistantSignal(SignalBoxElement element)
            {
                var grid = new Grid();

                grid.Children.Add(new Rectangle { Width = 20, Height = 28, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(grid.Children[^1], 20);
                Canvas.SetTop(grid.Children[^1], 16);

                var yellowOn = element.SignalAspect == SignalAspect.Ks2;
                grid.Children.Add(CreateClassicLed(24, 24, yellowOn ? Colors.SignalYellow : Color.FromArgb(30, 255, 255, 0), 12));

                return grid;
            }

            private Grid CreateSvMainSignal(SignalBoxElement element)
            {
                var grid = new Grid();

                grid.Children.Add(new Rectangle { Width = 30, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(grid.Children[^1], 15);
                Canvas.SetTop(grid.Children[^1], 19);

                var redOn = element.SignalAspect == SignalAspect.Hp0;
                grid.Children.Add(CreateClassicLed(19, 24, redOn ? Colors.SignalRed : Color.FromArgb(30, 255, 0, 0), 10));

                var greenOn = element.SignalAspect == SignalAspect.Hp1;
                grid.Children.Add(CreateClassicLed(33, 24, greenOn ? Colors.SignalGreen : Color.FromArgb(30, 0, 255, 0), 10));

                return grid;
            }

            private Grid CreateSvDistantSignal(SignalBoxElement element)
            {
                var grid = new Grid();

                grid.Children.Add(new Rectangle { Width = 30, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(255, 25, 35, 50)) });
                Canvas.SetLeft(grid.Children[^1], 15);
                Canvas.SetTop(grid.Children[^1], 19);

                var leftOn = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
                grid.Children.Add(CreateClassicLed(19, 24, leftOn ? Colors.SignalYellow : Color.FromArgb(30, 255, 255, 0), 10));

                var rightOn = element.SignalAspect is SignalAspect.Vr0 or SignalAspect.Vr2;
                grid.Children.Add(CreateClassicLed(33, 24, rightOn ? Colors.SignalYellow : Color.FromArgb(30, 255, 255, 0), 10));

                return grid;
            }
        }
