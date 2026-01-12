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
    }

    private void OnSolutionSaving(object? sender, EventArgs e)
    {
        SaveToDomainPlan();
    }

    private void OnSolutionLoaded(object? sender, EventArgs e)
    {
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
                Rotation = 0
            };
            Elements.Add(element);

            var visual = CreateElementVisual(element);
            Canvas.SetLeft(visual, gridX * GridCellSize);
            Canvas.SetTop(visual, gridY * GridCellSize);
            TrackCanvas.Children.Add(visual);
            ElementVisuals[element.Id] = visual;
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

            if (element.Type is SignalBoxElementType.SwitchLeft or SignalBoxElementType.SwitchRight or SignalBoxElementType.SwitchDouble)
            {
                element.SwitchPosition = element.SwitchPosition == SwitchPosition.Straight
                    ? SwitchPosition.Diverging
                    : SwitchPosition.Straight;
                RefreshElementVisual(element);
            }
            else if (IsSignalType(element.Type))
            {
                CycleSignalAspect(element);
                RefreshElementVisual(element);
            }

            SelectElement(element);
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

    private static void CycleSignalAspect(SignalBoxElement element)
    {
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
            SignalBoxElementType.SignalCombined or SignalBoxElementType.SignalKsCombined => element.SignalAspect switch
            {
                SignalAspect.Hp0 => SignalAspect.Ks1,
                SignalAspect.Ks1 => SignalAspect.Ks2,
                _ => SignalAspect.Hp0
            },
            SignalBoxElementType.SignalKsMain => element.SignalAspect switch
            {
                SignalAspect.Hp0 => SignalAspect.Ks1,
                SignalAspect.Ks1 => SignalAspect.Ks1Blink,
                _ => SignalAspect.Hp0
            },
            SignalBoxElementType.SignalKsDistant => element.SignalAspect switch
            {
                SignalAspect.Ks2 => SignalAspect.Ks1,
                _ => SignalAspect.Ks2
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
        };
        panel.Children.Add(input);

        return panel;
    }

    private void DeleteSelectedElement()
    {
        if (SelectedElement == null) return;

        if (ElementVisuals.TryGetValue(SelectedElement.Id, out var visual))
        {
            TrackCanvas.Children.Remove(visual);
            ElementVisuals.Remove(SelectedElement.Id);
        }

        Elements.Remove(SelectedElement);
        SelectElement(null);
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
