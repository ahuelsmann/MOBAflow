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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

using DomainElement = Moba.Domain.SignalBoxPlanElement;
using DomainPlan = Moba.Domain.SignalBoxPlan;
using DomainSymbol = Moba.Domain.SignalBoxSymbol;

/// <summary>
/// Abstract base class for all Signal Box page implementations.
/// Provides shared functionality for track diagram editing, drag/drop, element interaction.
/// Subclasses implement visual styling (colors, icon designs) for different ESTW styles.
/// </summary>
public abstract class SignalBoxPageBase : Page
{
    // Drag operation identifiers
    protected const string DragDataNewElement = "NewElement:";
    protected const string DragDataMoveElement = "MoveElement:";

    // Grid settings
    protected const int GridCellSize = 60;
    protected const int GridColumns = 24;
    protected const int GridRows = 14;

    // Core dependencies
    protected readonly MainWindowViewModel ViewModel;
    protected readonly IZ21 Z21;

    // Data collections
    protected readonly List<SignalBoxElement> Elements = [];
    protected readonly Dictionary<Guid, FrameworkElement> ElementVisuals = [];

    // UI elements
    protected Canvas TrackCanvas = null!;
    protected StackPanel Toolbox = null!;
    protected StackPanel PropertiesPanel = null!;
    protected ScrollViewer CanvasScrollViewer = null!;

    // Selection state
    protected SignalBoxElement? SelectedElement;
    protected FrameworkElement? SelectedVisual;
    protected TextBlock? StatusTextBlock;

    // Grid visibility
    private bool _isGridVisible = true;
    private readonly List<Line> _gridLines = [];

    /// <summary>Gets or sets whether the grid lines are visible.</summary>
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

    // Pan state for right-mouse-button panning
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

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

    #region Abstract Methods - Subclass Implementation Required

    /// <summary>Gets the display name for this signal box style.</summary>
    protected abstract string StyleName { get; }

    /// <summary>Gets the color scheme for this style.</summary>
    protected abstract SignalBoxColorScheme Colors { get; }

    /// <summary>Creates the toolbox icon for the given element type.</summary>
    protected abstract UIElement CreateToolboxIcon(SignalBoxElementType type);

    /// <summary>Creates the track diagram visual for an element.</summary>
    protected abstract FrameworkElement CreateElementVisual(SignalBoxElement element);

    /// <summary>Builds the style-specific header/toolbar.</summary>
    protected abstract Border BuildHeader();

    /// <summary>Whether to show the status bar at the bottom. Override to return false to hide it.</summary>
    protected virtual bool ShowStatusBar => true;

    /// <summary>Builds the style-specific status bar at the bottom. Only called if ShowStatusBar is true.</summary>
    protected virtual Border? BuildStatusBar() => null;

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
                LogMessage("INFO", string.Format(CultureInfo.InvariantCulture, "Loaded {0} elements", Elements.Count));
            }
            else
            {
                LogMessage("INFO", string.Format(CultureInfo.InvariantCulture, "{0} initialized", StyleName));
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
            LogMessage("FEEDBACK", string.Format(CultureInfo.InvariantCulture, "InPort {0}: OCCUPIED", feedback.InPort));
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
            Name = StyleName,
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
        LogMessage("SAVE", string.Format(CultureInfo.InvariantCulture, "Saved {0} elements", plan.Elements.Count));
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
        var rootGrid = new Grid { Background = new SolidColorBrush(Colors.Background) };
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = BuildHeader();
        Grid.SetRow(header, 0);
        Grid.SetColumnSpan(header, 5);
        rootGrid.Children.Add(header);

        var toolboxPanel = BuildToolbox();
        Grid.SetRow(toolboxPanel, 1);
        Grid.SetColumn(toolboxPanel, 0);
        rootGrid.Children.Add(toolboxPanel);

        var divider1 = new Border { Width = 2, Background = new SolidColorBrush(Colors.Border) };
        Grid.SetRow(divider1, 1);
        Grid.SetColumn(divider1, 1);
        rootGrid.Children.Add(divider1);

        var canvasPanel = BuildCanvasPanel();
        Grid.SetRow(canvasPanel, 1);
        Grid.SetColumn(canvasPanel, 2);
        rootGrid.Children.Add(canvasPanel);

        var divider2 = new Border { Width = 2, Background = new SolidColorBrush(Colors.Border) };
        Grid.SetRow(divider2, 1);
        Grid.SetColumn(divider2, 3);
        rootGrid.Children.Add(divider2);

            var propertiesPanel = BuildPropertiesPanel();
            Grid.SetRow(propertiesPanel, 1);
            Grid.SetColumn(propertiesPanel, 4);
            rootGrid.Children.Add(propertiesPanel);

            if (ShowStatusBar)
            {
                var statusBar = BuildStatusBar();
                if (statusBar != null)
                {
                    Grid.SetRow(statusBar, 2);
                    Grid.SetColumnSpan(statusBar, 5);
                    rootGrid.Children.Add(statusBar);
                }
            }

            return rootGrid;
        }

    protected virtual Border BuildToolbox()
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
        Toolbox.Children.Add(CreateToolboxCategory("GLEISE", "\uE909"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Gerade", SignalBoxElementType.TrackStraight),
            ("Kurve 45", SignalBoxElementType.TrackCurve45),
            ("Kurve 90", SignalBoxElementType.TrackCurve90),
            ("Prellbock", SignalBoxElementType.TrackEndStop)
        ]));

        // Switch Elements
        Toolbox.Children.Add(CreateToolboxCategory("WEICHEN", "\uE8AB"));
        Toolbox.Children.Add(CreateIconGrid([
            ("EW links", SignalBoxElementType.SwitchLeft),
            ("EW rechts", SignalBoxElementType.SwitchRight),
            ("DKW", SignalBoxElementType.SwitchDouble),
            ("Kreuzung", SignalBoxElementType.SwitchCrossing)
        ]));

        // Main Signals
        Toolbox.Children.Add(CreateToolboxCategory("SIGNALE", "\uE8B8"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Hauptsig", SignalBoxElementType.SignalMain),
            ("Vorsignal", SignalBoxElementType.SignalDistant),
            ("Ks-Signal", SignalBoxElementType.SignalCombined),
            ("Rangiersig", SignalBoxElementType.SignalShunting)
        ]));

        // Additional
        Toolbox.Children.Add(CreateToolboxCategory("ZUSATZ", "\uE946"));
        Toolbox.Children.Add(CreateIconGrid([
            ("Zs3", SignalBoxElementType.SignalSpeed),
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

    private Border CreateToolboxCategory(string name, string glyph)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.Accent)
        });
        panel.Children.Add(new TextBlock
        {
            Text = name,
            FontSize = 11,
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
            CornerRadius = new CornerRadius(8),
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
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            LogMessage("DRAG", string.Format(CultureInfo.InvariantCulture, "New: {0}", type));
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
            Background = new SolidColorBrush(Colors.Background),
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

        return new Border { Background = new SolidColorBrush(Colors.Background), Child = CanvasScrollViewer };
    }

    protected virtual void DrawGrid()
    {
        _gridLines.Clear();
        for (int x = 0; x <= GridColumns; x++)
        {
            var line = new Line
            {
                X1 = x * GridCellSize,
                Y1 = 0,
                X2 = x * GridCellSize,
                Y2 = GridRows * GridCellSize,
                Stroke = new SolidColorBrush(Colors.GridLine),
                StrokeThickness = 1,
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
                Stroke = new SolidColorBrush(Colors.GridLine),
                StrokeThickness = 1,
                Visibility = _isGridVisible ? Visibility.Visible : Visibility.Collapsed
            };
            _gridLines.Add(line);
            TrackCanvas.Children.Add(line);
        }
    }

    private Border BuildPropertiesPanel()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        PropertiesPanel = new StackPanel { Spacing = 12, Padding = new Thickness(12) };

        var headerPanel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 16) };
        headerPanel.Children.Add(new TextBlock
        {
            Text = "EIGENSCHAFTEN",
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
        PropertiesPanel.Children.Add(headerPanel);

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = "Kein Element ausgewaehlt",
            FontSize = 12,
            Foreground = new SolidColorBrush(Colors.TextMuted),
            TextWrapping = TextWrapping.Wrap
        });

        scroll.Content = PropertiesPanel;

        return new Border
        {
            Background = new SolidColorBrush(Colors.PanelBackground),
            Child = scroll
        };
    }

    #endregion

    #region Canvas Interaction

    private void OnCanvasDragOver(object sender, DragEventArgs e)
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

                    LogMessage("MOVE", string.Format(CultureInfo.InvariantCulture, "{0} to ({1}, {2})", element.Type, gridX, gridY));
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

            LogMessage("PLACED", string.Format(CultureInfo.InvariantCulture, "{0} at ({1}, {2})", type, gridX, gridY));
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
            LogMessage("ROTATE", string.Format(CultureInfo.InvariantCulture, "{0} to {1} deg", element.Type, element.Rotation));
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
                LogMessage("SWITCH", string.Format(CultureInfo.InvariantCulture, "{0} -> {1}", element.Type, element.SwitchPosition));
            }
                    else if (IsSignalType(element.Type))
                    {
                        CycleSignalAspect(element);
                        RefreshElementVisual(element);
                        LogMessage("SIGNAL", string.Format(CultureInfo.InvariantCulture, "{0} -> {1}", element.Type, element.SignalAspect));
                    }

                    SelectElement(element);
                    e.Handled = true;
                };
            }

            private static bool IsSignalType(SignalBoxElementType type)
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

                    private void CycleSignalAspect(SignalBoxElement element)
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
        while (PropertiesPanel.Children.Count > 1)
            PropertiesPanel.Children.RemoveAt(1);

        if (SelectedElement == null)
        {
            PropertiesPanel.Children.Add(new TextBlock
            {
                Text = "Kein Element ausgewaehlt",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.TextMuted),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = GetElementTypeName(SelectedElement.Type),
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.TextPrimary),
            Margin = new Thickness(0, 0, 0, 8)
        });

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = string.Format(CultureInfo.InvariantCulture, "Position: ({0}, {1})", SelectedElement.GridX, SelectedElement.GridY),
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.TextSecondary)
        });

        PropertiesPanel.Children.Add(new TextBlock
        {
            Text = string.Format(CultureInfo.InvariantCulture, "Rotation: {0} Grad", SelectedElement.Rotation),
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.TextSecondary),
            Margin = new Thickness(0, 0, 0, 12)
        });

        if (IsSignalType(SelectedElement.Type))
        {
            PropertiesPanel.Children.Add(CreateSignalAspectSelector());
        }

        if (SelectedElement.Type is SignalBoxElementType.SwitchLeft or SignalBoxElementType.SwitchRight or SignalBoxElementType.SwitchDouble)
        {
            PropertiesPanel.Children.Add(CreateSwitchPositionDisplay());
        }

        if (SelectedElement.Type == SignalBoxElementType.FeedbackPoint)
        {
            PropertiesPanel.Children.Add(CreateFeedbackAddressInput());
        }

        var deleteBtn = new Button
        {
            Content = "Loeschen",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 20, 0, 0),
            Background = new SolidColorBrush(Color.FromArgb(255, 120, 40, 40))
        };
        deleteBtn.Click += (s, e) => DeleteSelectedElement();
        PropertiesPanel.Children.Add(deleteBtn);
    }

    private StackPanel CreateSignalAspectSelector()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "SIGNALBILD",
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.Accent),
            CharacterSpacing = 50
        });

        var aspects = SelectedElement!.Type switch
        {
            SignalBoxElementType.SignalMain => new[] {
                ("Hp0 - Halt", SignalAspect.Hp0, Colors.SignalRed),
                ("Hp1 - Fahrt", SignalAspect.Hp1, Colors.SignalGreen),
                ("Hp2 - Langsam", SignalAspect.Hp2, Colors.SignalYellow)
            },
            SignalBoxElementType.SignalDistant => new[] {
                ("Vr0 - Halt erwarten", SignalAspect.Vr0, Colors.SignalYellow),
                ("Vr1 - Fahrt erwarten", SignalAspect.Vr1, Colors.SignalGreen),
                ("Vr2 - Langsam", SignalAspect.Vr2, Colors.SignalYellow)
            },
            SignalBoxElementType.SignalCombined => new[] {
                ("Hp0 - Halt", SignalAspect.Hp0, Colors.SignalRed),
                ("Ks1 - Fahrt", SignalAspect.Ks1, Colors.SignalGreen),
                ("Ks2 - Halt erwarten", SignalAspect.Ks2, Colors.SignalYellow)
            },
            _ => Array.Empty<(string, SignalAspect, Color)>()
        };

        foreach (var (name, aspect, color) in aspects)
        {
            var isSelected = SelectedElement.SignalAspect == aspect;
            var btn = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(isSelected ? Colors.ButtonHover : Colors.ButtonBackground),
                BorderBrush = new SolidColorBrush(isSelected ? color : Colors.ButtonBorder),
                BorderThickness = new Thickness(isSelected ? 2 : 1)
            };

            var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            content.Children.Add(new Ellipse { Width = 12, Height = 12, Fill = new SolidColorBrush(color) });
            content.Children.Add(new TextBlock { Text = name, FontSize = 11, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) });
            btn.Content = content;

            var capturedAspect = aspect;
            btn.Click += (s, e) =>
            {
                SelectedElement!.SignalAspect = capturedAspect;
                RefreshElementVisual(SelectedElement);
                UpdatePropertiesPanel();
                LogMessage("SIGNAL", string.Format(CultureInfo.InvariantCulture, "Aspect: {0}", capturedAspect));
            };

            panel.Children.Add(btn);
        }

        return panel;
    }

    private StackPanel CreateSwitchPositionDisplay()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "WEICHENSTELLUNG",
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.Accent),
            CharacterSpacing = 50
        });

        var position = SelectedElement!.SwitchPosition == SwitchPosition.Straight ? "Grundstellung" : "Abzweig";
        var color = SelectedElement.SwitchPosition == SwitchPosition.Straight ? Colors.SignalGreen : Colors.SignalYellow;

        panel.Children.Add(new TextBlock
        {
            Text = position,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(color)
        });

        var toggleBtn = new Button
        {
            Content = "Umstellen",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 0)
        };
        toggleBtn.Click += (s, e) =>
        {
            SelectedElement!.SwitchPosition = SelectedElement.SwitchPosition == SwitchPosition.Straight
                ? SwitchPosition.Diverging
                : SwitchPosition.Straight;
            RefreshElementVisual(SelectedElement);
            UpdatePropertiesPanel();
            LogMessage("SWITCH", string.Format(CultureInfo.InvariantCulture, "Position: {0}", SelectedElement.SwitchPosition));
        };
        panel.Children.Add(toggleBtn);

        return panel;
    }

    private StackPanel CreateFeedbackAddressInput()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "RUECKMELDER-ADRESSE",
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.Accent),
            CharacterSpacing = 50
        });

        var input = new NumberBox
        {
            Value = SelectedElement!.Address,
            Minimum = 1,
            Maximum = 2048,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            Header = "InPort"
        };
        input.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(e.NewValue))
            {
                SelectedElement!.Address = (int)e.NewValue;
                RefreshElementVisual(SelectedElement);
                LogMessage("FEEDBACK", string.Format(CultureInfo.InvariantCulture, "Address: {0}", SelectedElement.Address));
            }
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
        LogMessage("DELETE", string.Format(CultureInfo.InvariantCulture, "Removed {0}", SelectedElement.Type));
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
        SignalBoxElementType.SignalMain => "Hauptsignal",
        SignalBoxElementType.SignalDistant => "Vorsignal",
        SignalBoxElementType.SignalCombined => "Ks-Signal",
        SignalBoxElementType.SignalShunting => "Rangiersignal",
        SignalBoxElementType.SignalSpeed => "Zs3",
        SignalBoxElementType.Platform => "Bahnsteig",
        SignalBoxElementType.FeedbackPoint => "Rueckmelder",
        SignalBoxElementType.Label => "Beschriftung",
        _ => type.ToString()
    };

    #endregion

    #region Logging

    protected static void LogMessage(string category, string message)
    {
        System.Diagnostics.Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "[{0:HH:mm:ss}] [{1}] {2}", DateTime.Now, category, message));
    }

    #endregion

    #region Helper Methods

    protected static Color GetStateColor(SignalBoxElementState state, SignalBoxColorScheme colors) => state switch
    {
        SignalBoxElementState.Free => colors.TrackFree,
        SignalBoxElementState.Occupied => colors.TrackOccupied,
        SignalBoxElementState.RouteSet => colors.RouteSet,
        SignalBoxElementState.RouteClearing => colors.RouteClearing,
        SignalBoxElementState.Blocked => colors.Blocked,
        _ => colors.TrackFree
    };

    protected static Ellipse CreateLed(double x, double y, Color color, double size = 10)
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
                    new GradientStop { Color = color, Offset = 0.3 },
                    new GradientStop { Color = Color.FromArgb(200, color.R, color.G, color.B), Offset = 1 }
                }
            }
        };
        Canvas.SetLeft(led, x);
        Canvas.SetTop(led, y);
        return led;
    }

    #endregion
}

/// <summary>
/// Theme options for the Signal Box display.
/// </summary>
public enum SignalBoxTheme
{
    Light,
    Dark
}

/// <summary>
/// Color scheme for a signal box style.
/// </summary>
public record SignalBoxColorScheme
{
    public required Color Background { get; init; }
    public required Color PanelBackground { get; init; }
    public required Color MessagePanelBackground { get; init; }
    public required Color Border { get; init; }
    public required Color GridLine { get; init; }
    public required Color Accent { get; init; }
    public required Color ButtonBackground { get; init; }
    public required Color ButtonHover { get; init; }
    public required Color ButtonBorder { get; init; }
    public required Color TrackFree { get; init; }
    public required Color TrackOccupied { get; init; }
    public required Color RouteSet { get; init; }
    public required Color RouteClearing { get; init; }
    public required Color Blocked { get; init; }
    public required Color SignalRed { get; init; }
    public required Color SignalGreen { get; init; }
    public required Color SignalYellow { get; init; }

    public Color TextPrimary { get; init; } = Color.FromArgb(255, 255, 255, 255);
    public Color TextSecondary { get; init; } = Color.FromArgb(180, 255, 255, 255);
    public Color TextMuted { get; init; } = Color.FromArgb(120, 255, 255, 255);

    /// <summary>Light theme (ILTIS-inspired).</summary>
    public static SignalBoxColorScheme Light { get; } = new()
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
        SignalYellow = Color.FromArgb(255, 240, 200, 20),
        TextPrimary = Color.FromArgb(255, 40, 40, 40),
        TextSecondary = Color.FromArgb(255, 80, 80, 80),
        TextMuted = Color.FromArgb(180, 100, 100, 100)
    };

    /// <summary>Dark theme (Classic ESTW-inspired).</summary>
    public static SignalBoxColorScheme Dark { get; } = new()
    {
        Background = Color.FromArgb(255, 0, 0, 0),
        PanelBackground = Color.FromArgb(255, 15, 15, 15),
        MessagePanelBackground = Color.FromArgb(255, 5, 5, 5),
        Border = Color.FromArgb(255, 40, 40, 40),
        GridLine = Color.FromArgb(15, 60, 60, 60),
        Accent = Color.FromArgb(255, 255, 220, 0),
        ButtonBackground = Color.FromArgb(255, 25, 25, 25),
        ButtonHover = Color.FromArgb(255, 45, 45, 45),
        ButtonBorder = Color.FromArgb(60, 80, 80, 80),
        TrackFree = Color.FromArgb(255, 255, 220, 0),
        TrackOccupied = Color.FromArgb(255, 255, 0, 0),
        RouteSet = Color.FromArgb(255, 0, 255, 0),
        RouteClearing = Color.FromArgb(255, 255, 255, 0),
        Blocked = Color.FromArgb(255, 0, 150, 255),
        SignalRed = Color.FromArgb(255, 255, 0, 0),
        SignalGreen = Color.FromArgb(255, 0, 255, 0),
        SignalYellow = Color.FromArgb(255, 255, 255, 0),
        TextPrimary = Color.FromArgb(255, 255, 255, 255),
        TextSecondary = Color.FromArgb(180, 255, 255, 255),
        TextMuted = Color.FromArgb(120, 255, 255, 255)
    };

    /// <summary>Gets the color scheme for the specified theme.</summary>
    public static SignalBoxColorScheme GetTheme(SignalBoxTheme theme) => theme switch
    {
        SignalBoxTheme.Light => Light,
        SignalBoxTheme.Dark => Dark,
        _ => Dark
    };
}
