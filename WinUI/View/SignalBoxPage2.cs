// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Common.Configuration;

using Controls;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.Backend.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Model;
using Moba.WinUI.Service;

using SharedUI.Interface;

using System;
using System.Globalization;

using Windows.UI;

using AppTheme = Moba.WinUI.Service.ApplicationTheme;

/// <summary>
/// MOBAesb 2 - Theme-enabled Electronic Signal Box page.
/// Uses Skin-System for manufacturer-inspired color themes.
/// Signal colors (Red/Yellow/Green) remain constant for realistic representation.
/// </summary>
public sealed class SignalBoxPage2 : SignalBoxPageBase
{
    private TextBlock? _clockText;
    private TextBlock? _titleText;
    private Border? _headerBorder;
    private readonly IThemeProvider _themeProvider;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;

    public SignalBoxPage2(
        MainWindowViewModel viewModel,
        IZ21 z21,
        IThemeProvider themeProvider,
        AppSettings settings,
        ISettingsService? settingsService = null)
        : base(viewModel, z21)
    {
        _themeProvider = themeProvider;
        _settings = settings;
        _settingsService = settingsService;

        // Subscribe to theme changes
        _themeProvider.ThemeChanged += OnThemeChanged;
        _themeProvider.DarkModeChanged += OnDarkModeChanged;

        StartClock();
        ApplyThemeColors();
    }

    protected override string PageTitle => "MOBAesb 2";

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyThemeColors);
    }

    private void OnDarkModeChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyThemeColors);
    }

    private void ApplyThemeColors()
    {
        var palette = ThemeColors.GetPalette(_themeProvider.CurrentTheme, _themeProvider.IsDarkMode);

        // Set page RequestedTheme for standard WinUI controls
        RequestedTheme = palette.IsDarkTheme
            ? ElementTheme.Dark
            : ElementTheme.Light;

        // Check if this is the "Original" theme (transparent header = no colored strip)
        var isOriginalTheme = _themeProvider.CurrentTheme == Service.ApplicationTheme.Original;

        if (_headerBorder != null)
        {
            if (isOriginalTheme)
            {
                // Original theme: No colored header strip - make background transparent
                _headerBorder.Background = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                _headerBorder.Background = palette.HeaderBackgroundBrush;
            }
        }

        if (_titleText != null)
        {
            if (isOriginalTheme)
            {
                // Original theme: Use standard text color based on light/dark mode
                _titleText.Foreground = _themeProvider.IsDarkMode
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Colors.Black);
            }
            else
            {
                _titleText.Foreground = palette.HeaderForegroundBrush;
            }
        }

        if (_clockText != null)
        {
            if (isOriginalTheme)
            {
                _clockText.Foreground = _themeProvider.IsDarkMode
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Colors.Black);
            }
            else
            {
                _clockText.Foreground = palette.HeaderForegroundBrush;
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
        };
        timer.Start();
    }

    protected override Border BuildHeader()
    {
        var palette = ThemeColors.GetPalette(_themeProvider.CurrentTheme, _themeProvider.IsDarkMode);

        var grid = new Grid { Padding = new Thickness(16, 12, 16, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left: Title and Clock
        var leftPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24, VerticalAlignment = VerticalAlignment.Center };

        _titleText = new TextBlock
        {
            Text = "MOBAesb 2",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = palette.HeaderForegroundBrush
        };
        leftPanel.Children.Add(_titleText);

        _clockText = new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            FontSize = 18,
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = palette.HeaderForegroundBrush
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
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = palette.HeaderForegroundBrush,
            Opacity = 0.8
        });
        leftPanel.Children.Add(connectionPanel);

        Grid.SetColumn(leftPanel, 0);
        grid.Children.Add(leftPanel);

        // Center: Skin Selector Buttons
        var skinCommandBar = new CommandBar
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed,
            Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))  // Transparent
        };

        // Classic Skin (Green)
        var classicButton = new AppBarButton
        {
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE91F", Foreground = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)) }
        };
        classicButton.Click += (s, e) => SetSkin(AppTheme.Classic);
        skinCommandBar.PrimaryCommands.Add(classicButton);

        // Modern Skin (Blue)
        var modernButton = new AppBarButton
        {
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE91F", Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)) }
        };
        modernButton.Click += (s, e) => SetSkin(AppTheme.Modern);
        skinCommandBar.PrimaryCommands.Add(modernButton);

        // Dark Skin (Violet)
        var darkButton = new AppBarButton
        {
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE91F", Foreground = new SolidColorBrush(Color.FromArgb(255, 106, 90, 205)) }
        };
        darkButton.Click += (s, e) => SetSkin(AppTheme.Dark);
        skinCommandBar.PrimaryCommands.Add(darkButton);

        // ESU CabControl Skin (Orange)
        var esuButton = new AppBarButton
        {
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE91F", Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)) }
        };
        esuButton.Click += (s, e) => SetSkin(AppTheme.EsuCabControl);
        skinCommandBar.PrimaryCommands.Add(esuButton);

        // Roco Z21 Skin (Orange)
        var rocoButton = new AppBarButton
        {
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE91F", Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 102, 0)) }
        };
        rocoButton.Click += (s, e) => SetSkin(AppTheme.RocoZ21);
        skinCommandBar.PrimaryCommands.Add(rocoButton);

        // Maerklin CS Skin (Red)
        var maerklinButton = new AppBarButton
        {
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE91F", Foreground = new SolidColorBrush(Color.FromArgb(255, 204, 0, 0)) }
        };
        maerklinButton.Click += (s, e) => SetSkin(AppTheme.MaerklinCS);
        skinCommandBar.PrimaryCommands.Add(maerklinButton);

        // Original Skin (White)
        var originalButton = new AppBarButton
        {
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE91F", Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)) }  // Gray
        };
        originalButton.Click += (s, e) => SetSkin(AppTheme.Original);
        skinCommandBar.PrimaryCommands.Add(originalButton);

        Grid.SetColumn(skinCommandBar, 1);
        grid.Children.Add(skinCommandBar);

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

        _headerBorder = new Border
        {
            Background = palette.HeaderBackgroundBrush,
            BorderThickness = new Thickness(0),
            Child = grid
        };

        return _headerBorder;
    }

    private async void SetSkin(AppTheme theme)
    {
        _themeProvider.SetTheme(theme);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
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
                // 45° arc from left to top-right (similar to MOBAtps)
                canvas.Children.Add(CreateTrackArc(2, -14, 26, 90, -45, SignalColors.TrackFree, 3));
                break;

            case SignalBoxElementType.TrackCurve90:
                // 90° arc from left to bottom (similar to MOBAtps)
                canvas.Children.Add(CreateTrackArc(30, 12, 26, 180, 90, SignalColors.TrackFree, 3));
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
                    Width = 28,
                    Height = 20,
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
        return CreateElementVisualAsModern(element);
    }

    private FrameworkElement CreateElementVisualAsModern(SignalBoxElement element)
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
                container.Children.Add(CreateTrackArc(4, -22, 52, 90, -45, trackColor));
                break;

            case SignalBoxElementType.TrackCurve90:
                container.Children.Add(CreateTrackArc(56, 30, 52, 180, 90, trackColor));
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
                container.Children.Add(CreateTrackLine(4, 30, 56, 30, trackColor));
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
            Width = 22,
            Height = 22,
            Fill = new SolidColorBrush(discColor),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 50, 60, 80)),
            StrokeThickness = 2
        });
        Canvas.SetLeft(grid.Children[^1], 25);
        Canvas.SetTop(grid.Children[^1], 19);

        return grid;
    }

    private Grid CreateKsSignalVisual(SignalBoxElement element)
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

        // Gruene Lampe unten links (Ks1 - Fahrt)
        var greenOn = element.SignalAspect is SignalAspect.Ks1 or SignalAspect.Ks1Blink;
        var greenLed = CreateSignalLed(20, 38, greenOn ? SignalColors.SignalGreen : SignalColors.LedOff, 10);
        grid.Children.Add(greenLed);

        // Register for blinking on canvas if Ks1Blink
        if (element.SignalAspect == SignalAspect.Ks1Blink)
        {
            RegisterCanvasBlinkingLed(element.Id, greenLed);
        }

        // Gelbe Lampe unten rechts (Ks2 - Halt erwarten)
        var yellowOn = element.SignalAspect == SignalAspect.Ks2;
        grid.Children.Add(CreateSignalLed(32, 38, yellowOn ? SignalColors.SignalYellow : SignalColors.LedOff, 10));

        return grid;
    }

    private Grid CreateKsDistantSignalVisual(SignalBoxElement element)
    {
        var grid = new Grid();

        // Signalmast
        grid.Children.Add(new Rectangle { Width = 3, Height = 38, Fill = new SolidColorBrush(Color.FromArgb(255, 60, 70, 90)) });
        Canvas.SetLeft(grid.Children[^1], 28);
        Canvas.SetTop(grid.Children[^1], 11);

        // Schwarzer Signalschirm (horizontal fuer Vorsignal)
        grid.Children.Add(new Rectangle { Width = 32, Height = 18, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) });
        Canvas.SetLeft(grid.Children[^1], 14);
        Canvas.SetTop(grid.Children[^1], 21);

        // Gruene Lampe links (Ks1 erwarten - Fahrt erwarten)
        var greenOn = element.SignalAspect is SignalAspect.Ks1 or SignalAspect.Ks1Blink;
        var greenLed = CreateSignalLed(18, 26, greenOn ? SignalColors.SignalGreen : SignalColors.LedOff, 10);
        grid.Children.Add(greenLed);

        // Register for blinking on canvas if Ks1Blink
        if (element.SignalAspect == SignalAspect.Ks1Blink)
        {
            RegisterCanvasBlinkingLed(element.Id, greenLed);
        }

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