// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Model;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

using DomainElement = Moba.Domain.SignalBoxPlanElement;
using DomainPlan = Moba.Domain.SignalBoxPlan;
using DomainSymbol = Moba.Domain.SignalBoxSymbol;

/// <summary>
/// Base class for the MOBAesb (Electronic Signal Box) page.
/// Uses Fluent Design System with standard WinUI ThemeResources.
/// Signal-specific colors (Red/Yellow/Green) are defined in <see cref="SignalColors"/>.
/// </summary>
public abstract class SignalBoxPageBase : Page
{
    protected const string DragDataNewElement = "NewElement:";
    protected const string DragDataMoveElement = "MoveElement:";

    protected const int GridCellSize = 60;
    protected const int GridColumns = 24;
    protected const int GridRows = 14;

    protected readonly MainWindowViewModel ViewModel;
    protected readonly IZ21 Z21;

    protected readonly List<SignalBoxElement> Elements = [];
    protected readonly Dictionary<Guid, FrameworkElement> ElementVisuals = [];

    protected Canvas TrackCanvas = null!;
    protected StackPanel Toolbox = null!;
    protected StackPanel PropertiesPanel = null!;
    protected ScrollViewer CanvasScrollViewer = null!;

    protected SignalBoxElement? SelectedElement;
    protected FrameworkElement? SelectedVisual;

    private bool _isGridVisible = true;
    private readonly List<Line> _gridLines = [];

    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    // Blinking animation support
    private DispatcherTimer? _blinkTimer;
    private bool _blinkState;
    private readonly List<Ellipse> _blinkingLeds = [];
    private readonly Dictionary<Guid, List<Ellipse>> _canvasBlinkingLeds = [];

    public bool IsGridVisible
    {
        get => _isGridVisible;
        set
        {
            _isGridVisible = value;
            foreach (var line in _gridLines)
                line.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    protected SignalBoxPageBase(MainWindowViewModel viewModel, IZ21 z21)
    {
        ViewModel = viewModel;
        Z21 = z21;

        ViewModel.SolutionSaving += OnSolutionSaving;
        ViewModel.SolutionLoaded += OnSolutionLoaded;

        // Initialize blink timer for signal animations
        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += OnBlinkTimerTick;

        Content = BuildLayout();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Abstract Members

    protected abstract string PageTitle { get; }

    protected abstract UIElement CreateToolboxIcon(SignalBoxElementType type);

    protected abstract FrameworkElement CreateElementVisual(SignalBoxElement element);

    protected abstract Border BuildHeader();

    #endregion

    #region Lifecycle Events

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Z21.Received += OnZ21FeedbackReceived;
        _blinkTimer?.Start();

        if (Elements.Count == 0)
        {
            var project = ViewModel.Solution?.Projects?.FirstOrDefault();
            if (project?.SignalBoxPlan != null && project.SignalBoxPlan.Elements.Count > 0)
            {
                LoadFromDomainPlan(project.SignalBoxPlan);
            }
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Z21.Received -= OnZ21FeedbackReceived;
        _blinkTimer?.Stop();
    }

    private void OnSolutionSaving(object? sender, EventArgs e)
    {
        SaveToDomainPlan();
    }

    private void OnSolutionLoaded(object? sender, EventArgs e)
    {
        // Clear all canvas blinking LEDs before loading new plan
        _canvasBlinkingLeds.Clear();

        var project = ViewModel.Solution?.Projects?.FirstOrDefault();
        if (project?.SignalBoxPlan != null)
        {
            LoadFromDomainPlan(project.SignalBoxPlan);
        }
        else
        {
            Elements.Clear();
            ElementVisuals.Clear();
            TrackCanvas?.Children.Clear();
            DrawGrid();
        }
    }

    private void OnZ21FeedbackReceived(FeedbackResult feedback)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateFeedbackPoint(feedback.InPort);
        });
    }

    /// <summary>
    /// Handles the blink timer tick for signal animation.
    /// Toggles visibility of registered blinking LEDs (both properties panel and canvas).
    /// </summary>
    private void OnBlinkTimerTick(object? sender, object e)
    {
        _blinkState = !_blinkState;
        var opacity = _blinkState ? 1.0 : 0.15;

        // Properties panel LEDs
        foreach (var led in _blinkingLeds)
        {
            led.Opacity = opacity;
        }

        // Canvas LEDs
        foreach (var leds in _canvasBlinkingLeds.Values)
        {
            foreach (var led in leds)
            {
                led.Opacity = opacity;
            }
        }

        // Debug output
        System.Diagnostics.Debug.WriteLine($"[BLINK] Timer tick: state={_blinkState}, opacity={opacity}, propsLeds={_blinkingLeds.Count}, canvasLeds={_canvasBlinkingLeds.Values.Sum(l => l.Count)}");
    }

    /// <summary>
    /// Registers an LED ellipse for blinking animation in the properties panel.
    /// </summary>
    protected void RegisterBlinkingLed(Ellipse led)
    {
        if (!_blinkingLeds.Contains(led))
        {
            _blinkingLeds.Add(led);
            System.Diagnostics.Debug.WriteLine($"[BLINK] Registered properties LED, total={_blinkingLeds.Count}");
        }
    }

    /// <summary>
    /// Registers an LED ellipse for blinking animation on the canvas for a specific element.
    /// </summary>
    protected void RegisterCanvasBlinkingLed(Guid elementId, Ellipse led)
    {
        if (!_canvasBlinkingLeds.TryGetValue(elementId, out var leds))
        {
            leds = [];
            _canvasBlinkingLeds[elementId] = leds;
        }

        if (!leds.Contains(led))
        {
            leds.Add(led);
            System.Diagnostics.Debug.WriteLine($"[BLINK] Registered canvas LED for element {elementId}, total for element={leds.Count}");
        }
    }

    /// <summary>
    /// Clears blinking LEDs for a specific canvas element.
    /// Called when refreshing a single element visual.
    /// </summary>
    protected void ClearCanvasBlinkingLeds(Guid elementId)
    {
        _canvasBlinkingLeds.Remove(elementId);
    }

    /// <summary>
    /// Clears all registered blinking LEDs (properties panel only).
    /// Called when refreshing the properties panel.
    /// </summary>
    protected void ClearBlinkingLeds()
    {
        _blinkingLeds.Clear();
    }

    #endregion

    #region Persistence

    private void SaveToDomainPlan()
    {
        var project = ViewModel.Solution?.Projects?.FirstOrDefault();
        if (project == null) return;

        var plan = new DomainPlan
        {
            Name = PageTitle,
            GridWidth = GridColumns,
            GridHeight = GridRows,
            CellSize = GridCellSize
        };

        foreach (var element in Elements)
        {
            plan.Elements.Add(new DomainElement
            {
                Id = element.Id,
                Symbol = MapToSymbol(element.Type),
                X = element.GridX,
                Y = element.GridY,
                Rotation = element.Rotation,
                Name = element.Name,
                Address = element.Address
            });
        }

        project.SignalBoxPlan = plan;
    }

    private void LoadFromDomainPlan(DomainPlan plan)
    {
        Elements.Clear();
        ElementVisuals.Clear();
        TrackCanvas.Children.Clear();
        DrawGrid();

        foreach (var domainElement in plan.Elements)
        {
            var element = new SignalBoxElement
            {
                Id = domainElement.Id,
                Type = MapFromSymbol(domainElement.Symbol),
                GridX = domainElement.X,
                GridY = domainElement.Y,
                Rotation = domainElement.Rotation,
                Name = domainElement.Name,
                Address = domainElement.Address
            };
            Elements.Add(element);

            var visual = CreateElementVisual(element);
            Canvas.SetLeft(visual, element.GridX * GridCellSize);
            Canvas.SetTop(visual, element.GridY * GridCellSize);
            TrackCanvas.Children.Add(visual);
            ElementVisuals[element.Id] = visual;
        }
    }

    private static DomainSymbol MapToSymbol(SignalBoxElementType type) => type switch
    {
        SignalBoxElementType.TrackStraight => DomainSymbol.TrackStraight,
        SignalBoxElementType.TrackCurve45 => DomainSymbol.TrackCurve45,
        SignalBoxElementType.TrackCurve90 => DomainSymbol.TrackCurve90,
        SignalBoxElementType.TrackEndStop => DomainSymbol.TrackEnd,
        SignalBoxElementType.SwitchLeft => DomainSymbol.SwitchSimpleLeft,
        SignalBoxElementType.SwitchRight => DomainSymbol.SwitchSimpleRight,
        SignalBoxElementType.SwitchDouble => DomainSymbol.SwitchDoubleSlip,
        SignalBoxElementType.SwitchCrossing => DomainSymbol.SwitchDiamond,
        SignalBoxElementType.SignalMain => DomainSymbol.SignalKsMain,
        SignalBoxElementType.SignalDistant => DomainSymbol.SignalKsDistant,
        SignalBoxElementType.SignalCombined => DomainSymbol.SignalKsCombined,
        SignalBoxElementType.SignalHvHp => DomainSymbol.SignalHvMain,
        SignalBoxElementType.SignalHvVr => DomainSymbol.SignalHvDistant,
        SignalBoxElementType.SignalKsMain => DomainSymbol.SignalKsMain,
        SignalBoxElementType.SignalKsDistant => DomainSymbol.SignalKsDistant,
        SignalBoxElementType.SignalKsCombined => DomainSymbol.SignalKsCombined,
        SignalBoxElementType.SignalSvMain => DomainSymbol.SignalKsMain,
        SignalBoxElementType.SignalSvDistant => DomainSymbol.SignalKsDistant,
        SignalBoxElementType.SignalShunting => DomainSymbol.SignalSh,
        SignalBoxElementType.SignalSpeed => DomainSymbol.SignalZs3,
        SignalBoxElementType.Platform => DomainSymbol.Platform,
        SignalBoxElementType.FeedbackPoint => DomainSymbol.Detector,
        SignalBoxElementType.Label => DomainSymbol.Label,
        _ => DomainSymbol.TrackStraight
    };

    private static SignalBoxElementType MapFromSymbol(DomainSymbol symbol) => symbol switch
    {
        DomainSymbol.TrackStraight => SignalBoxElementType.TrackStraight,
        DomainSymbol.TrackCurve45 => SignalBoxElementType.TrackCurve45,
        DomainSymbol.TrackCurve90 => SignalBoxElementType.TrackCurve90,
        DomainSymbol.TrackEnd => SignalBoxElementType.TrackEndStop,
        DomainSymbol.SwitchSimpleLeft => SignalBoxElementType.SwitchLeft,
        DomainSymbol.SwitchSimpleRight => SignalBoxElementType.SwitchRight,
        DomainSymbol.SwitchDoubleSlip => SignalBoxElementType.SwitchDouble,
        DomainSymbol.SwitchDiamond => SignalBoxElementType.SwitchCrossing,
        DomainSymbol.SignalKsMain => SignalBoxElementType.SignalKsMain,
        DomainSymbol.SignalKsDistant => SignalBoxElementType.SignalKsDistant,
        DomainSymbol.SignalKsCombined => SignalBoxElementType.SignalKsCombined,
        DomainSymbol.SignalHvMain => SignalBoxElementType.SignalHvHp,
        DomainSymbol.SignalHvDistant => SignalBoxElementType.SignalHvVr,
        DomainSymbol.SignalHvCombined => SignalBoxElementType.SignalCombined,
        DomainSymbol.SignalHlMain or DomainSymbol.SignalHlDistant => SignalBoxElementType.SignalMain,
        DomainSymbol.SignalSh or DomainSymbol.SignalDwarf => SignalBoxElementType.SignalShunting,
        DomainSymbol.SignalZs3 => SignalBoxElementType.SignalSpeed,
        DomainSymbol.Platform or DomainSymbol.PlatformEdge => SignalBoxElementType.Platform,
        DomainSymbol.Detector or DomainSymbol.AxleCounter => SignalBoxElementType.FeedbackPoint,
        DomainSymbol.Label or DomainSymbol.TrackNumber => SignalBoxElementType.Label,
        _ => SignalBoxElementType.TrackStraight
    };

    #endregion

    #region Layout Building

    private Grid BuildLayout()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = BuildHeader();
        Grid.SetRow(header, 0);
        Grid.SetColumnSpan(header, 3);
        rootGrid.Children.Add(header);

        var toolboxPanel = BuildToolbox();
        Grid.SetRow(toolboxPanel, 1);
        Grid.SetColumn(toolboxPanel, 0);
        rootGrid.Children.Add(toolboxPanel);

        var canvasPanel = BuildCanvasPanel();
        Grid.SetRow(canvasPanel, 1);
        Grid.SetColumn(canvasPanel, 1);
        rootGrid.Children.Add(canvasPanel);

        var propertiesPanel = BuildPropertiesPanel();
        Grid.SetRow(propertiesPanel, 1);
        Grid.SetColumn(propertiesPanel, 2);
        rootGrid.Children.Add(propertiesPanel);

        return rootGrid;
    }

    protected virtual Border BuildToolbox()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Toolbox = new StackPanel { Spacing = 12, Padding = new Thickness(12) };

        Toolbox.Children.Add(CreateToolboxCategory("GLEISE"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Gerade", SignalBoxElementType.TrackStraight),
            ("Kurve 45", SignalBoxElementType.TrackCurve45),
            ("Kurve 90", SignalBoxElementType.TrackCurve90),
            ("Prellbock", SignalBoxElementType.TrackEndStop)
        ]));

        Toolbox.Children.Add(CreateToolboxCategory("WEICHEN"));
        Toolbox.Children.Add(CreateIconGrid([
            ("EW links", SignalBoxElementType.SwitchLeft),
            ("EW rechts", SignalBoxElementType.SwitchRight),
            ("DKW", SignalBoxElementType.SwitchDouble),
            ("Kreuzung", SignalBoxElementType.SwitchCrossing)
        ]));

        Toolbox.Children.Add(CreateToolboxCategory("H/V-SYSTEM"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Hp-Signal", SignalBoxElementType.SignalHvHp),
            ("Vr-Signal", SignalBoxElementType.SignalHvVr)
        ]));

        Toolbox.Children.Add(CreateToolboxCategory("Ks-SYSTEM"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Ks-Haupt", SignalBoxElementType.SignalKsMain),
            ("Ks-Vor", SignalBoxElementType.SignalKsDistant),
            ("Ks-Kombi", SignalBoxElementType.SignalKsCombined)
        ]));

        Toolbox.Children.Add(CreateToolboxCategory("Sv-SYSTEM"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Sv-Haupt", SignalBoxElementType.SignalSvMain),
            ("Sv-Vor", SignalBoxElementType.SignalSvDistant)
        ]));

        Toolbox.Children.Add(CreateToolboxCategory("RANGIERSIGNALE"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Rangier", SignalBoxElementType.SignalShunting),
            ("Zs 3", SignalBoxElementType.SignalSpeed)
        ]));

        Toolbox.Children.Add(CreateToolboxCategory("ZUSATZ"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Bahnsteig", SignalBoxElementType.Platform),
            ("Melder", SignalBoxElementType.FeedbackPoint),
            ("Label", SignalBoxElementType.Label)
        ]));

        scroll.Content = Toolbox;

        return new Border
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = scroll
        };
    }

    private static TextBlock CreateToolboxCategory(string name)
    {
        return new TextBlock
        {
            Text = name,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Margin = new Thickness(0, 8, 0, 4)
        };
    }

    private Grid CreateIconGrid((string tooltip, SignalBoxElementType type)[] items)
    {
        var grid = new Grid { ColumnSpacing = 6, RowSpacing = 6 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        int row = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (i % 2 == 0)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var (tooltip, type) = items[i];
            var button = CreateToolboxButton(tooltip, type);
            Grid.SetRow(button, row);
            Grid.SetColumn(button, i % 2);
            grid.Children.Add(button);

            if (i % 2 == 1) row++;
        }

        return grid;
    }

    private Border CreateToolboxButton(string tooltip, SignalBoxElementType type)
    {
        var button = new Border
        {
            Height = 52,
            CornerRadius = new CornerRadius(4),
            Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            Tag = type,
            CanDrag = true
        };

        var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(CreateToolboxIcon(type));
        content.Children.Add(new TextBlock
        {
            Text = tooltip,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        });
        button.Child = content;

        ToolTipService.SetToolTip(button, tooltip);

        button.PointerEntered += (s, e) =>
        {
            button.Background = (Brush)Application.Current.Resources["SubtleFillColorTertiaryBrush"];
        };
        button.PointerExited += (s, e) =>
        {
            button.Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        };

        button.DragStarting += (s, e) =>
        {
            e.Data.SetText(string.Format(CultureInfo.InvariantCulture, "{0}{1}", DragDataNewElement, (int)type));
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        };

        return button;
    }

    private Border BuildCanvasPanel()
    {
        CanvasScrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            ZoomMode = ZoomMode.Enabled,
            MinZoomFactor = 0.3f,
            MaxZoomFactor = 3.0f
        };

        TrackCanvas = new Canvas
        {
            Width = GridColumns * GridCellSize,
            Height = GridRows * GridCellSize,
            Background = (Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"],
            AllowDrop = true
        };

        TrackCanvas.DragOver += OnCanvasDragOver;
        TrackCanvas.Drop += OnCanvasDrop;
        TrackCanvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        TrackCanvas.PointerPressed += OnCanvasPointerPressed;
        TrackCanvas.PointerMoved += OnCanvasPointerMoved;
        TrackCanvas.PointerReleased += OnCanvasPointerReleased;

        DrawGrid();
        CanvasScrollViewer.Content = TrackCanvas;

        return new Border { Child = CanvasScrollViewer };
    }

    protected virtual void DrawGrid()
    {
        _gridLines.Clear();
        var gridBrush = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];

        for (int x = 0; x <= GridColumns; x++)
        {
            var line = new Line
            {
                X1 = x * GridCellSize,
                Y1 = 0,
                X2 = x * GridCellSize,
                Y2 = GridRows * GridCellSize,
                Stroke = gridBrush,
                StrokeThickness = 0.5,
                Opacity = 0.3,
                Visibility = _isGridVisible ? Visibility.Visible : Visibility.Collapsed
            };
            _gridLines.Add(line);
            TrackCanvas.Children.Add(line);
        }
        for (int y = 0; y <= GridRows; y++)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y * GridCellSize,
                X2 = GridColumns * GridCellSize,
                Y2 = y * GridCellSize,
                Stroke = gridBrush,
                StrokeThickness = 0.5,
                Opacity = 0.3,
                Visibility = _isGridVisible ? Visibility.Visible : Visibility.Collapsed
            };
            _gridLines.Add(line);
            TrackCanvas.Children.Add(line);
        }
    }

    private Border BuildPropertiesPanel()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        PropertiesPanel = new StackPanel { Spacing = 12, Padding = new Thickness(16) };

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = "EIGENSCHAFTEN",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = "Kein Element ausgewaehlt",
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
            TextWrapping = TextWrapping.Wrap
        });

        scroll.Content = PropertiesPanel;

        return new Border
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = scroll
        };
    }

    #endregion

    #region Canvas Interaction

    private static void OnCanvasDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Move;
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsGlyphVisible = true;
    }

    private async void OnCanvasDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text))
            return;

        var text = await e.DataView.GetTextAsync();
        var pos = e.GetPosition(TrackCanvas);
        int gridX = Math.Clamp((int)(pos.X / GridCellSize), 0, GridColumns - 1);
        int gridY = Math.Clamp((int)(pos.Y / GridCellSize), 0, GridRows - 1);

        if (text.StartsWith(DragDataMoveElement, StringComparison.Ordinal))
        {
            var idString = text[DragDataMoveElement.Length..];
            if (Guid.TryParse(idString, out var elementId))
            {
                var element = Elements.Find(el => el.Id == elementId);
                if (element != null)
                {
                    element.GridX = gridX;
                    element.GridY = gridY;

                    if (ElementVisuals.TryGetValue(element.Id, out var visual))
                    {
                        Canvas.SetLeft(visual, gridX * GridCellSize);
                        Canvas.SetTop(visual, gridY * GridCellSize);
                    }

                    // Trigger auto-save after element position change
                    _ = ViewModel.SaveSolutionInternalAsync();
                }
            }
            return;
        }

        if (text.StartsWith(DragDataNewElement, StringComparison.Ordinal))
        {
            var typeString = text[DragDataNewElement.Length..];
            if (!int.TryParse(typeString, out var typeInt))
                return;

            var type = (SignalBoxElementType)typeInt;
            var element = new SignalBoxElement
            {
                Type = type,
                GridX = gridX,
                GridY = gridY,
                Rotation = 0,
                KsSignalType = GetDefaultKsSignalType(type)
            };
            Elements.Add(element);

            var visual = CreateElementVisual(element);
            Canvas.SetLeft(visual, gridX * GridCellSize);
            Canvas.SetTop(visual, gridY * GridCellSize);
            TrackCanvas.Children.Add(visual);
            ElementVisuals[element.Id] = visual;

            // Trigger auto-save after adding new element
            _ = ViewModel.SaveSolutionInternalAsync();
        }
    }

    private void OnCanvasPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(TrackCanvas).Position;
        int gridX = (int)(pos.X / GridCellSize);
        int gridY = (int)(pos.Y / GridCellSize);

        var element = Elements.Find(el => el.GridX == gridX && el.GridY == gridY);
        if (element != null)
        {
            var delta = e.GetCurrentPoint(TrackCanvas).Properties.MouseWheelDelta;
            element.Rotation = (element.Rotation + (delta > 0 ? 45 : -45) + 360) % 360;

            if (ElementVisuals.TryGetValue(element.Id, out var visual))
            {
                visual.RenderTransform = new RotateTransform
                {
                    Angle = element.Rotation,
                    CenterX = GridCellSize / 2,
                    CenterY = GridCellSize / 2
                };
            }

            // Trigger auto-save after element rotation
            _ = ViewModel.SaveSolutionInternalAsync();

            e.Handled = true;
        }
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(TrackCanvas);

        if (point.Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _panStartPoint = point.Position;
            _panStartHorizontalOffset = CanvasScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = CanvasScrollViewer.VerticalOffset;
            TrackCanvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPanning) return;

        var point = e.GetCurrentPoint(TrackCanvas);
        var deltaX = point.Position.X - _panStartPoint.X;
        var deltaY = point.Position.Y - _panStartPoint.Y;

        CanvasScrollViewer.ChangeView(
            _panStartHorizontalOffset - deltaX,
            _panStartVerticalOffset - deltaY,
            null,
            true);

        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            TrackCanvas.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    #endregion

    #region Element Interaction

    protected void SetupElementInteraction(FrameworkElement visual, SignalBoxElement element)
    {
        visual.CanDrag = true;

        visual.DragStarting += (s, e) =>
        {
            e.Data.SetText(string.Format(CultureInfo.InvariantCulture, "{0}{1}", DragDataMoveElement, element.Id));
            e.Data.RequestedOperation = DataPackageOperation.Move;
        };

        visual.PointerEntered += (s, e) =>
        {
            if (SelectedElement?.Id != element.Id)
                visual.Opacity = 0.8;
        };

        visual.PointerExited += (s, e) =>
        {
            if (SelectedElement?.Id != element.Id)
                visual.Opacity = 1.0;
        };

        visual.PointerPressed += (s, e) =>
        {
            if (!e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
                return;

            bool elementChanged = false;

            if (element.Type is SignalBoxElementType.SwitchLeft or SignalBoxElementType.SwitchRight or SignalBoxElementType.SwitchDouble)
            {
                element.SwitchPosition = element.SwitchPosition == SwitchPosition.Straight
                    ? SwitchPosition.Diverging
                    : SwitchPosition.Straight;
                RefreshElementVisual(element);
                elementChanged = true;
            }
            else if (IsSignalType(element.Type))
            {
                CycleSignalAspect(element);
                RefreshElementVisual(element);
                elementChanged = true;
            }

            SelectElement(element);

            // Trigger auto-save after element state change
            if (elementChanged)
            {
                _ = ViewModel.SaveSolutionInternalAsync();
            }

            e.Handled = true;
        };
    }

    protected static bool IsSignalType(SignalBoxElementType type)
    {
        return type is SignalBoxElementType.SignalMain
            or SignalBoxElementType.SignalDistant
            or SignalBoxElementType.SignalCombined
            or SignalBoxElementType.SignalShunting
            or SignalBoxElementType.SignalHvHp
            or SignalBoxElementType.SignalHvVr
            or SignalBoxElementType.SignalKsMain
            or SignalBoxElementType.SignalKsDistant
            or SignalBoxElementType.SignalKsCombined
            or SignalBoxElementType.SignalSvMain
            or SignalBoxElementType.SignalSvDistant;
    }

    /// <summary>
    /// Creates a PathIcon for Ks signals based on type and aspect.
    /// Represents authentic Ks-Signalsystem with triangle lamp layout.
    /// </summary>
    protected static PathIcon CreateKsSignalPathIcon(SignalBoxElementType type, SignalAspect? aspect = null)
    {
        string pathData = type switch
        {
            // Ks-Hauptsignal: Vertical mast + Red (top) + Green (bottom)
            SignalBoxElementType.SignalKsMain => 
                "M7,1 L9,1 L9,15 L7,15 Z " + // Mast
                "M5,2 A2,2 0 1,1 9,2 A2,2 0 1,1 5,2 " + // Red top
                "M5,12 A2,2 0 1,1 9,12 A2,2 0 1,1 5,12", // Green bottom

            // Ks-Vorsignal: Horizontal lights (Green + Yellow)
            SignalBoxElementType.SignalKsDistant =>
                "M7,1 L9,1 L9,15 L7,15 Z " + // Mast
                "M2,7 A2,2 0 1,1 6,7 A2,2 0 1,1 2,7 " + // Green left
                "M10,7 A2,2 0 1,1 14,7 A2,2 0 1,1 10,7", // Yellow right

            // Ks-Kombinationssignal: Triangle (Red top, Green left, Yellow right)
            SignalBoxElementType.SignalKsCombined =>
                "M7,1 L9,1 L9,15 L7,15 Z " + // Mast
                "M6,2 A2,2 0 1,1 10,2 A2,2 0 1,1 6,2 " + // Red top
                "M2,11 A2,2 0 1,1 6,11 A2,2 0 1,1 2,11 " + // Green bottom-left
                "M10,11 A2,2 0 1,1 14,11 A2,2 0 1,1 10,11", // Yellow bottom-right

                    _ => "M7,1 L9,1 L9,15 L7,15 Z M5,7 A2,2 0 1,1 9,7 A2,2 0 1,1 5,7"
                };

                return new PathIcon
                {
                    Data = (Microsoft.UI.Xaml.Media.Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                        typeof(Microsoft.UI.Xaml.Media.Geometry), pathData),
                    Width = 16,
                    Height = 16
                };
            }

            /// <summary>
            /// Gets the default KsSignalType for a given element type.
            /// </summary>
            private static KsSignalType GetDefaultKsSignalType(SignalBoxElementType elementType) => elementType switch
            {
                SignalBoxElementType.SignalKsMain => KsSignalType.Hauptsignal,
                SignalBoxElementType.SignalKsDistant => KsSignalType.Vorsignal,
                SignalBoxElementType.SignalKsCombined => KsSignalType.Mehrabschnittssignal,
                _ => KsSignalType.Hauptsignal
            };

            private static void CycleSignalAspect(SignalBoxElement element)
            {
                // For Ks signals, cycle through available aspects based on KsSignalType
                if (IsKsSignalType(element.Type))
                {
                    var availableAspects = GetAvailableKsAspects(element.KsSignalType);
                    if (availableAspects.Count > 0)
                    {
                        var currentIndex = availableAspects.IndexOf(element.SignalAspect);
                        var nextIndex = (currentIndex + 1) % availableAspects.Count;
                        element.SignalAspect = availableAspects[nextIndex];
                        return;
                    }
                }

                // Non-Ks signals
                element.SignalAspect = element.Type switch
                {
                    SignalBoxElementType.SignalMain or SignalBoxElementType.SignalHvHp => element.SignalAspect switch
                    {
                        SignalAspect.Hp0 => SignalAspect.Hp1,
                        SignalAspect.Hp1 => SignalAspect.Hp2,
                        _ => SignalAspect.Hp0
                    },
                    SignalBoxElementType.SignalDistant or SignalBoxElementType.SignalHvVr => element.SignalAspect switch
                    {
                        SignalAspect.Vr0 => SignalAspect.Vr1,
                        SignalAspect.Vr1 => SignalAspect.Vr2,
                        _ => SignalAspect.Vr0
                    },
                    SignalBoxElementType.SignalCombined => element.SignalAspect switch
                    {
                        SignalAspect.Hp0 => SignalAspect.Ks1,
                        SignalAspect.Ks1 => SignalAspect.Ks2,
                        _ => SignalAspect.Hp0
                    },
                    SignalBoxElementType.SignalSvMain => element.SignalAspect switch
                    {
                        SignalAspect.Hp0 => SignalAspect.Hp1,
                        _ => SignalAspect.Hp0
                    },
                    SignalBoxElementType.SignalSvDistant => element.SignalAspect switch
                    {
                        SignalAspect.Vr0 => SignalAspect.Vr1,
                        _ => SignalAspect.Vr0
                    },
                    SignalBoxElementType.SignalShunting => element.SignalAspect switch
                    {
                        SignalAspect.Sh0 => SignalAspect.Sh1,
                        _ => SignalAspect.Sh0
                    },
                    _ => element.SignalAspect
                };
            }

            protected void SelectElement(SignalBoxElement? element)
    {
        if (SelectedVisual != null)
            SelectedVisual.Opacity = 1.0;

        SelectedElement = element;
        SelectedVisual = element != null && ElementVisuals.TryGetValue(element.Id, out var visual) ? visual : null;

        UpdatePropertiesPanel();
    }

    protected void RefreshElementVisual(SignalBoxElement element)
    {
        if (!ElementVisuals.TryGetValue(element.Id, out var visual))
            return;

        // Clear blinking LEDs for this element before recreating
        ClearCanvasBlinkingLeds(element.Id);

        TrackCanvas.Children.Remove(visual);
        var newVisual = CreateElementVisual(element);
        Canvas.SetLeft(newVisual, element.GridX * GridCellSize);
        Canvas.SetTop(newVisual, element.GridY * GridCellSize);
        TrackCanvas.Children.Add(newVisual);
        ElementVisuals[element.Id] = newVisual;

        if (SelectedElement?.Id == element.Id)
            SelectedVisual = newVisual;
    }

    private void UpdateFeedbackPoint(int address)
    {
        var element = Elements.Find(e => e.Type == SignalBoxElementType.FeedbackPoint && e.Address == address);
        if (element != null)
        {
            element.State = SignalBoxElementState.Occupied;
            RefreshElementVisual(element);
        }
    }

    #endregion

    #region Properties Panel

    protected virtual void UpdatePropertiesPanel()
    {
        // Clear blinking LEDs from properties panel (will be re-added if needed)
        ClearBlinkingLeds();

        PropertiesPanel.Children.Clear();

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = "EIGENSCHAFTEN",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Margin = new Thickness(0, 0, 0, 12)
        });

        if (SelectedElement == null)
        {
            PropertiesPanel.Children.Add(new TextBlock
            {
                Text = "Kein Element ausgewaehlt",
                Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = GetElementTypeName(SelectedElement.Type),
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            Margin = new Thickness(0, 0, 0, 16)
        });

        var infoCard = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var infoPanel = new StackPanel { Spacing = 4 };
        infoPanel.Children.Add(new TextBlock
        {
            Text = string.Format(CultureInfo.InvariantCulture, "Position: ({0}, {1})", SelectedElement.GridX, SelectedElement.GridY),
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
        });
        infoPanel.Children.Add(new TextBlock
        {
            Text = string.Format(CultureInfo.InvariantCulture, "Rotation: {0} Grad", SelectedElement.Rotation),
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
        });
        infoCard.Child = infoPanel;
        PropertiesPanel.Children.Add(infoCard);

        if (SelectedElement.Type is SignalBoxElementType.SwitchLeft or SignalBoxElementType.SwitchRight or SignalBoxElementType.SwitchDouble)
        {
            PropertiesPanel.Children.Add(CreateSwitchControls());
        }

        if (SelectedElement.Type == SignalBoxElementType.FeedbackPoint)
        {
            PropertiesPanel.Children.Add(CreateFeedbackControls());
        }

        if (SelectedElement.Type == SignalBoxElementType.Label)
        {
            PropertiesPanel.Children.Add(CreateLabelControls());
        }

        // Ks-Signal controls
        if (IsKsSignalType(SelectedElement.Type))
        {
            PropertiesPanel.Children.Add(CreateKsSignalControls());
        }

        var deleteBtn = new Button
        {
            Content = "Element loeschen",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 24, 0, 0)
        };
        deleteBtn.Click += (s, e) => DeleteSelectedElement();
        PropertiesPanel.Children.Add(deleteBtn);
    }

    private StackPanel CreateSwitchControls()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "Weichenstellung",
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
        });

        var toggleBtn = new Button
        {
            Content = SelectedElement!.SwitchPosition == SwitchPosition.Straight ? "Grundstellung" : "Abzweig",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        toggleBtn.Click += (s, e) =>
        {
            SelectedElement!.SwitchPosition = SelectedElement.SwitchPosition == SwitchPosition.Straight
                ? SwitchPosition.Diverging
                : SwitchPosition.Straight;
            toggleBtn.Content = SelectedElement.SwitchPosition == SwitchPosition.Straight ? "Grundstellung" : "Abzweig";
            RefreshElementVisual(SelectedElement);

            // Trigger auto-save after switch position change
            _ = ViewModel.SaveSolutionInternalAsync();
        };
        panel.Children.Add(toggleBtn);

        return panel;
    }

    private StackPanel CreateFeedbackControls()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "Rueckmelder-Adresse",
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
        });

        var input = new NumberBox
        {
            Value = SelectedElement!.Address,
            Minimum = 1,
            Maximum = 2048,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
        };
        input.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(e.NewValue))
            {
                SelectedElement!.Address = (int)e.NewValue;
                RefreshElementVisual(SelectedElement);

                // Trigger auto-save after feedback address change
                _ = ViewModel.SaveSolutionInternalAsync();
            }
        };
        panel.Children.Add(input);

        return panel;
    }

    private StackPanel CreateLabelControls()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "Beschriftung",
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
        });

        var input = new TextBox
        {
            Text = SelectedElement!.Name,
            PlaceholderText = "Text eingeben..."
        };
            input.TextChanged += (s, e) =>
            {
                SelectedElement!.Name = input.Text;
                RefreshElementVisual(SelectedElement);

                // Trigger auto-save after label text change
                _ = ViewModel.SaveSolutionInternalAsync();
            };
            panel.Children.Add(input);

            return panel;
        }

        /// <summary>
        /// Checks if the element type is a Ks signal type.
        /// </summary>
        protected static bool IsKsSignalType(SignalBoxElementType type)
        {
            return type is SignalBoxElementType.SignalKsMain
                or SignalBoxElementType.SignalKsDistant
                or SignalBoxElementType.SignalKsCombined;
        }

        /// <summary>
        /// Gets the available signal aspects for a Ks signal based on its type.
        /// </summary>
        protected static List<SignalAspect> GetAvailableKsAspects(KsSignalType ksType)
        {
            return ksType switch
            {
                KsSignalType.Vorsignal => [SignalAspect.Ks1, SignalAspect.Ks2],
                KsSignalType.VorsignalShortBraking => [SignalAspect.Ks1ShortBraking, SignalAspect.Ks2ShortBraking],
                KsSignalType.VorsignalRepeater => [SignalAspect.Ks1Repeater, SignalAspect.Ks2Repeater],
                KsSignalType.Hauptsignal => [SignalAspect.Hp0, SignalAspect.Ks1, SignalAspect.Ks1Blink],
                KsSignalType.Mehrabschnittssignal => [SignalAspect.Hp0, SignalAspect.Ks1, SignalAspect.Ks1Blink, SignalAspect.Ks2],
                _ => [SignalAspect.Hp0, SignalAspect.Ks1]
            };
        }

        /// <summary>
        /// Gets the display name for a Ks signal type.
        /// </summary>
        private static string GetKsSignalTypeName(KsSignalType type) => type switch
        {
            KsSignalType.Vorsignal => "Vorsignal",
            KsSignalType.VorsignalShortBraking => "Vorsignal (verk. Bremsweg)",
            KsSignalType.VorsignalRepeater => "Vorsignalwiederholer",
            KsSignalType.Hauptsignal => "Hauptsignal",
            KsSignalType.Mehrabschnittssignal => "Mehrabschnittssignal",
            _ => type.ToString()
        };

        /// <summary>
        /// Gets the display name for a signal aspect.
        /// </summary>
        private static string GetSignalAspectName(SignalAspect aspect) => aspect switch
        {
            SignalAspect.Hp0 => "Hp 0 - Halt",
            SignalAspect.Ks1 => "Ks 1 - Fahrt",
            SignalAspect.Ks1Blink => "Ks 1 - Fahrt (blinkend)",
            SignalAspect.Ks2 => "Ks 2 - Halt erwarten",
            SignalAspect.Ks1ShortBraking => "Ks 1 - Fahrt (verk. Bremswg)",
            SignalAspect.Ks2ShortBraking => "Ks 2 - Halt erw. (verk. Bremswg)",
            SignalAspect.Ks1Repeater => "Ks 1 - Fahrt (Wiederholer)",
            SignalAspect.Ks2Repeater => "Ks 2 - Halt erw. (Wiederholer)",
            _ => aspect.ToString()
        };

        /// <summary>
        /// Creates the controls for Ks signal configuration.
        /// </summary>
        private StackPanel CreateKsSignalControls()
        {
            var panel = new StackPanel { Spacing = 12 };

            // Ks Signal Type Selection
            panel.Children.Add(new TextBlock
            {
                Text = "Signaltyp",
                Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
            });

            var typeCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Add all Ks signal types
            foreach (KsSignalType type in Enum.GetValues<KsSignalType>())
            {
                typeCombo.Items.Add(new ComboBoxItem
                {
                    Content = GetKsSignalTypeName(type),
                    Tag = type
                });
            }

            // Select current type
            foreach (ComboBoxItem item in typeCombo.Items)
            {
                if ((KsSignalType)item.Tag! == SelectedElement!.KsSignalType)
                {
                    typeCombo.SelectedItem = item;
                    break;
                }
            }

            typeCombo.SelectionChanged += (s, e) =>
            {
                if (typeCombo.SelectedItem is ComboBoxItem selectedItem)
                {
                    SelectedElement!.KsSignalType = (KsSignalType)selectedItem.Tag!;

                    // Reset signal aspect to first available for new type
                    var availableAspects = GetAvailableKsAspects(SelectedElement.KsSignalType);
                    if (availableAspects.Count > 0 && !availableAspects.Contains(SelectedElement.SignalAspect))
                    {
                        SelectedElement.SignalAspect = availableAspects[0];
                    }

                    RefreshElementVisual(SelectedElement);
                    UpdatePropertiesPanel(); // Refresh to show new aspect options

                    _ = ViewModel.SaveSolutionInternalAsync();
                }
            };
            panel.Children.Add(typeCombo);

            // Signal Aspect Selection (Signalbild)
            panel.Children.Add(new TextBlock
            {
                Text = "Signalbild",
                Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
                Margin = new Thickness(0, 8, 0, 0)
            });

            var aspectsGrid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
            aspectsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            aspectsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var availableAspects = GetAvailableKsAspects(SelectedElement!.KsSignalType);
            int row = 0;

            for (int i = 0; i < availableAspects.Count; i++)
            {
                if (i % 2 == 0)
                {
                    aspectsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                var aspect = availableAspects[i];
                var aspectButton = CreateSignalAspectButton(aspect);

                Grid.SetRow(aspectButton, row);
                Grid.SetColumn(aspectButton, i % 2);
                aspectsGrid.Children.Add(aspectButton);

                if (i % 2 == 1)
                {
                    row++;
                }
            }

            panel.Children.Add(aspectsGrid);

            // DCC Address
            panel.Children.Add(new TextBlock
            {
                Text = "DCC-Adresse",
                Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
                Margin = new Thickness(0, 8, 0, 0)
            });

            var addressInput = new NumberBox
            {
                Value = SelectedElement!.Address,
                Minimum = 0,
                Maximum = 2048,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                PlaceholderText = "0 = nicht konfiguriert"
            };
            addressInput.ValueChanged += (s, e) =>
            {
                if (!double.IsNaN(e.NewValue))
                {
                    SelectedElement!.Address = (int)e.NewValue;
                    _ = ViewModel.SaveSolutionInternalAsync();
                }
            };
            panel.Children.Add(addressInput);

            return panel;
        }

        /// <summary>
        /// Creates a button for selecting a signal aspect with visual representation.
        /// </summary>
        private Border CreateSignalAspectButton(SignalAspect aspect)
        {
            var isSelected = SelectedElement?.SignalAspect == aspect;

            var button = new Border
            {
                Height = 80,
                CornerRadius = new CornerRadius(4),
                Background = isSelected
                    ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
                    : (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
                BorderBrush = isSelected
                    ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
                    : (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Padding = new Thickness(4),
                Tag = aspect
            };

            var content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 4
            };

            // Visual representation of the signal aspect
            content.Children.Add(CreateSignalAspectVisual(aspect));

            // Label
            content.Children.Add(new TextBlock
            {
                Text = GetShortAspectName(aspect),
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = isSelected
                    ? (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"]
                    : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
            });

            button.Child = content;
            ToolTipService.SetToolTip(button, GetSignalAspectName(aspect));

            button.PointerPressed += (s, e) =>
            {
                if (!e.GetCurrentPoint(button).Properties.IsLeftButtonPressed)
                    return;

                SelectedElement!.SignalAspect = aspect;
                RefreshElementVisual(SelectedElement);
                UpdatePropertiesPanel();
                _ = ViewModel.SaveSolutionInternalAsync();
            };

            button.PointerEntered += (s, e) =>
            {
                if (!isSelected)
                {
                    button.Background = (Brush)Application.Current.Resources["SubtleFillColorTertiaryBrush"];
                }
            };

            button.PointerExited += (s, e) =>
            {
                if (!isSelected)
                {
                    button.Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
                }
            };

            return button;
        }

        /// <summary>
        /// Gets a short display name for a signal aspect.
        /// </summary>
        private static string GetShortAspectName(SignalAspect aspect) => aspect switch
        {
            SignalAspect.Hp0 => "Hp 0",
            SignalAspect.Ks1 => "Ks 1",
            SignalAspect.Ks1Blink => "Ks 1 blink",
            SignalAspect.Ks2 => "Ks 2",
            SignalAspect.Ks1ShortBraking => "Ks 1 vB",
            SignalAspect.Ks2ShortBraking => "Ks 2 vB",
            SignalAspect.Ks1Repeater => "Ks 1 W",
            SignalAspect.Ks2Repeater => "Ks 2 W",
            _ => aspect.ToString()
        };

        /// <summary>
        /// Creates a visual representation of a Ks signal aspect for the properties panel.
        /// Supports blinking animation for Ks1Blink aspect.
        /// </summary>
        private Canvas CreateSignalAspectVisual(SignalAspect aspect)
        {
            var canvas = new Canvas { Width = 32, Height = 48 };

            // Signal housing (dark background)
            var housing = new Rectangle
            {
                Width = 20,
                Height = 44,
                Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)),
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(housing, 6);
            Canvas.SetTop(housing, 2);
            canvas.Children.Add(housing);

            switch (aspect)
            {
                case SignalAspect.Hp0:
                    // Red light at top
                    canvas.Children.Add(CreateAspectLed(11, 8, SignalColors.SignalRed));
                    canvas.Children.Add(CreateAspectLed(11, 22, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 36, SignalColors.LedOff));
                    break;

                case SignalAspect.Ks1:
                    // Green light
                    canvas.Children.Add(CreateAspectLed(11, 8, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 22, SignalColors.SignalGreen));
                    canvas.Children.Add(CreateAspectLed(11, 36, SignalColors.LedOff));
                    break;

                case SignalAspect.Ks1Blink:
                    // Green light with blinking animation
                    canvas.Children.Add(CreateAspectLed(11, 8, SignalColors.LedOff));
                    var blinkLed = CreateAspectLed(11, 22, SignalColors.SignalGreen);
                    canvas.Children.Add(blinkLed);
                    canvas.Children.Add(CreateAspectLed(11, 36, SignalColors.LedOff));
                    // Register for blinking
                    RegisterBlinkingLed(blinkLed);
                    break;

                case SignalAspect.Ks2:
                    // Yellow light
                    canvas.Children.Add(CreateAspectLed(11, 8, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 22, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 36, SignalColors.SignalYellow));
                    break;

                case SignalAspect.Ks1ShortBraking:
                    // Green + white additional light (top)
                    canvas.Children.Add(CreateAspectLed(11, 5, SignalColors.SignalWhite, 6)); // Small white top
                    canvas.Children.Add(CreateAspectLed(11, 22, SignalColors.SignalGreen));
                    canvas.Children.Add(CreateAspectLed(11, 36, SignalColors.LedOff));
                    break;

                case SignalAspect.Ks2ShortBraking:
                    // Yellow + white additional light (top)
                    canvas.Children.Add(CreateAspectLed(11, 5, SignalColors.SignalWhite, 6)); // Small white top
                    canvas.Children.Add(CreateAspectLed(11, 22, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 36, SignalColors.SignalYellow));
                    break;

                case SignalAspect.Ks1Repeater:
                    // Green + white additional light (bottom)
                    canvas.Children.Add(CreateAspectLed(11, 8, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 22, SignalColors.SignalGreen));
                    canvas.Children.Add(CreateAspectLed(11, 40, SignalColors.SignalWhite, 6)); // Small white bottom
                    break;

                case SignalAspect.Ks2Repeater:
                    // Yellow + white additional light (bottom)
                    canvas.Children.Add(CreateAspectLed(11, 8, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 22, SignalColors.SignalYellow));
                    canvas.Children.Add(CreateAspectLed(11, 40, SignalColors.SignalWhite, 6)); // Small white bottom
                    break;

                default:
                    // Default: all off
                    canvas.Children.Add(CreateAspectLed(11, 8, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 22, SignalColors.LedOff));
                    canvas.Children.Add(CreateAspectLed(11, 36, SignalColors.LedOff));
                    break;
            }

            return canvas;
        }

        /// <summary>
        /// Creates an LED ellipse for signal aspect visualization.
        /// </summary>
        private static Ellipse CreateAspectLed(double x, double y, Color color, double size = 10)
        {
            var led = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color)
            };
            Canvas.SetLeft(led, x - size / 2);
            Canvas.SetTop(led, y);
            return led;
        }

            private void DeleteSelectedElement()
        {
            if (SelectedElement == null) return;

            // Clear any blinking LEDs for this element
            ClearCanvasBlinkingLeds(SelectedElement.Id);

            if (ElementVisuals.TryGetValue(SelectedElement.Id, out var visual))
            {
                TrackCanvas.Children.Remove(visual);
                ElementVisuals.Remove(SelectedElement.Id);
            }

            Elements.Remove(SelectedElement);
            SelectElement(null);

            // Trigger auto-save after element deletion
            _ = ViewModel.SaveSolutionInternalAsync();
        }

    protected static string GetElementTypeName(SignalBoxElementType type) => type switch
    {
        SignalBoxElementType.TrackStraight => "Gerades Gleis",
        SignalBoxElementType.TrackCurve45 => "Bogen 45 Grad",
        SignalBoxElementType.TrackCurve90 => "Bogen 90 Grad",
        SignalBoxElementType.TrackEndStop => "Prellbock",
        SignalBoxElementType.SwitchLeft => "Weiche Links",
        SignalBoxElementType.SwitchRight => "Weiche Rechts",
        SignalBoxElementType.SwitchDouble => "DKW",
        SignalBoxElementType.SwitchCrossing => "Kreuzung",
        SignalBoxElementType.SignalMain or SignalBoxElementType.SignalHvHp => "Hauptsignal (H/V)",
        SignalBoxElementType.SignalDistant or SignalBoxElementType.SignalHvVr => "Vorsignal (H/V)",
        SignalBoxElementType.SignalKsMain => "Ks-Hauptsignal",
        SignalBoxElementType.SignalKsDistant => "Ks-Vorsignal",
        SignalBoxElementType.SignalCombined or SignalBoxElementType.SignalKsCombined => "Ks-Mehrabschnitt",
        SignalBoxElementType.SignalSvMain => "Sv-Hauptsignal",
        SignalBoxElementType.SignalSvDistant => "Sv-Vorsignal",
        SignalBoxElementType.SignalShunting => "Rangiersignal",
        SignalBoxElementType.SignalSpeed => "Zs 3",
        SignalBoxElementType.Platform => "Bahnsteig",
        SignalBoxElementType.FeedbackPoint => "Rueckmelder",
        SignalBoxElementType.Label => "Beschriftung",
        _ => type.ToString()
    };

    #endregion

    #region Helper Methods

    protected static Color GetTrackStateColor(SignalBoxElementState state) => state switch
    {
        SignalBoxElementState.Free => SignalColors.TrackFree,
        SignalBoxElementState.Occupied => SignalColors.TrackOccupied,
        SignalBoxElementState.RouteSet => SignalColors.RouteSet,
        SignalBoxElementState.RouteClearing => SignalColors.SignalYellow,
        SignalBoxElementState.Blocked => SignalColors.Blocked,
        _ => SignalColors.TrackFree
    };

    protected static Line CreateTrackLine(double x1, double y1, double x2, double y2, Color color, double thickness = 4)
    {
        return new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Flat,
            StrokeEndLineCap = PenLineCap.Flat
        };
    }

    /// <summary>
    /// Creates a track arc (quarter circle) similar to MOBAtps track rendering.
    /// </summary>
    /// <param name="centerX">Center X of the arc circle.</param>
    /// <param name="centerY">Center Y of the arc circle.</param>
    /// <param name="radius">Radius of the arc.</param>
    /// <param name="startAngleDeg">Start angle in degrees (0 = right, 90 = down).</param>
    /// <param name="sweepAngleDeg">Sweep angle in degrees (positive = clockwise).</param>
    /// <param name="color">Track color.</param>
    /// <param name="thickness">Line thickness.</param>
    protected static Microsoft.UI.Xaml.Shapes.Path CreateTrackArc(
        double centerX, double centerY, double radius,
        double startAngleDeg, double sweepAngleDeg,
        Color color, double thickness = 4)
    {
        double startRad = startAngleDeg * Math.PI / 180.0;
        double sweepRad = sweepAngleDeg * Math.PI / 180.0;

        var startPoint = new Point(
            centerX + radius * Math.Cos(startRad),
            centerY + radius * Math.Sin(startRad));

        var endPoint = new Point(
            centerX + radius * Math.Cos(startRad + sweepRad),
            centerY + radius * Math.Sin(startRad + sweepRad));

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false,
            IsFilled = false
        };

        var segment = new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            SweepDirection = sweepRad >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
            IsLargeArc = Math.Abs(sweepRad) > Math.PI
        };

        figure.Segments.Add(segment);

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        return new Microsoft.UI.Xaml.Shapes.Path
        {
            Data = geometry,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Flat,
            StrokeEndLineCap = PenLineCap.Flat
        };
    }

    protected static Ellipse CreateSignalLed(double x, double y, Color color, double size = 10)
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

    #endregion
}

/// <summary>
/// Signal-specific colors that remain constant regardless of app theme.
/// These represent real-world signal colors.
/// </summary>
public static class SignalColors
{
    public static Color SignalRed { get; } = Color.FromArgb(255, 255, 0, 0);
    public static Color SignalGreen { get; } = Color.FromArgb(255, 0, 200, 0);
    public static Color SignalYellow { get; } = Color.FromArgb(255, 255, 200, 0);
    public static Color SignalWhite { get; } = Color.FromArgb(255, 255, 255, 255);

    public static Color TrackFree { get; } = Color.FromArgb(255, 100, 100, 100);
    public static Color TrackOccupied { get; } = Color.FromArgb(255, 255, 60, 60);
    public static Color RouteSet { get; } = Color.FromArgb(255, 60, 200, 60);
    public static Color Blocked { get; } = Color.FromArgb(255, 100, 150, 220);

    public static Color LedOff { get; } = Color.FromArgb(40, 80, 80, 80);
}
