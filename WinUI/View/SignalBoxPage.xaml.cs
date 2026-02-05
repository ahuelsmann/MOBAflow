namespace Moba.WinUI.View;

using Common.Configuration;
using Domain;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Service;
using SharedUI.Interface;
using SharedUI.ViewModel;
using ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

/// <summary>
/// Modernes elektronisches Stellwerk (ESTW) - Gleisbildstellwerk.
/// Ermoeglicht die grafische Planung von Gleisanlagen mit Signalen und Weichen.
/// Uses SignalBoxPlanViewModel for MVVM-compliant data management.
/// Supports skin switching via ISkinProvider.
/// </summary>
public sealed partial class SignalBoxPage : Page
{
    private const int GridCellSize = 60;
    private const int GridColumns = 32;
    private const int GridRows = 18;

    private readonly Dictionary<Guid, FrameworkElement> _elementVisuals = [];
    private readonly List<Line> _gridLines = [];
    private readonly ISkinProvider _skinProvider;

    /// <summary>
    /// MainWindowViewModel for accessing the active project.
    /// </summary>
    public MainWindowViewModel ViewModel { get; }

    /// <summary>
    /// Skin selection ViewModel for this page.
    /// </summary>
    public SkinSelectorViewModel SkinViewModel { get; }

    /// <summary>
    /// SignalBoxPlan ViewModel - the source of truth for element data.
    /// </summary>
    private SignalBoxPlanViewModel? _planViewModel;

    private readonly List<Ellipse> _blinkingLeds = [];

    private bool _isGridVisible = true;
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    private DispatcherTimer? _blinkTimer;
    private bool _blinkState;

    public SignalBoxPage(
        MainWindowViewModel viewModel,
        ISkinProvider skinProvider,
        SkinSelectorViewModel skinViewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(skinProvider);
        ArgumentNullException.ThrowIfNull(skinViewModel);

        ViewModel = viewModel;
        _skinProvider = skinProvider;
        SkinViewModel = skinViewModel;

        InitializeComponent();

        // Subscribe to skin changes for dynamic updates
        _skinProvider.SkinChanged += OnSkinProviderChanged;
        _skinProvider.DarkModeChanged += OnDarkModeChanged;

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnSkinProviderChanged(object? sender, SkinChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplySkinColors);
    }

    private void OnDarkModeChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplySkinColors);
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        InitializeCanvas();
        InitializeBlinkTimer();
        InitializeAspectButtons();
        LoadFromModel();
        UpdateStatistics();
        UpdatePropertiesPanel();
        ApplySkinColors();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _blinkTimer?.Stop();
        _skinProvider.SkinChanged -= OnSkinProviderChanged;
        _skinProvider.DarkModeChanged -= OnDarkModeChanged;
    }

    /// <summary>
    /// Gets or creates the SignalBoxPlanViewModel for the current project.
    /// </summary>
    private SignalBoxPlanViewModel? GetOrCreatePlanViewModel()
    {
        var project = ViewModel.SelectedProject?.Model;
        if (project == null)
            return null;

        project.SignalBoxPlan ??= new SignalBoxPlan
        {
            Name = "Stellwerk",
            GridWidth = GridColumns,
            GridHeight = GridRows,
            CellSize = GridCellSize
        };

        _planViewModel ??= new SignalBoxPlanViewModel(project.SignalBoxPlan);
        return _planViewModel;
    }

    /// <summary>
    /// Shortcut to access the currently selected element from the ViewModel.
    /// </summary>
    private SbElement? SelectedElement => _planViewModel?.SelectedElement;

    private void InitializeCanvas()
    {
        TrackCanvas.Width = GridColumns * GridCellSize;
        TrackCanvas.Height = GridRows * GridCellSize;
        DrawGrid();
    }

    private void InitializeBlinkTimer()
    {
        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += (s, e) =>
        {
            _blinkState = !_blinkState;
            foreach (var led in _blinkingLeds)
            {
                led.Opacity = _blinkState ? 1.0 : 0.2;
            }
        };
        _blinkTimer.Start();
    }

    private void InitializeAspectButtons()
    {
        // Set fixed aspects for the signal aspect selection buttons
        AspectHp0Signal.Aspect = "Hp0";
        AspectKs1Signal.Aspect = "Ks1";
        AspectKs2Signal.Aspect = "Ks2";
        AspectKs1BlinkSignal.Aspect = "Ks1Blink";
        AspectKennlichtSignal.Aspect = "Kennlicht";
        AspectDunkelSignal.Aspect = "Dunkel";
        AspectRa12Signal.Aspect = "Ra12";
        AspectZs1Signal.Aspect = "Zs1";
        AspectZs7Signal.Aspect = "Zs7";
    }

    private void DrawGrid()
    {
        foreach (var line in _gridLines)
        {
            TrackCanvas.Children.Remove(line);
        }
        _gridLines.Clear();

        var brush = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];

        // Vertikale Linien
        for (int x = 0; x <= GridColumns; x++)
        {
            var line = new Line
            {
                X1 = x * GridCellSize,
                Y1 = 0,
                X2 = x * GridCellSize,
                Y2 = GridRows * GridCellSize,
                Stroke = brush,
                StrokeThickness = x % 5 == 0 ? 1.0 : 0.5,
                Opacity = x % 5 == 0 ? 0.5 : 0.25,
                Visibility = _isGridVisible ? Visibility.Visible : Visibility.Collapsed
            };
            _gridLines.Add(line);
            TrackCanvas.Children.Add(line);
        }

        // Horizontale Linien
        for (int y = 0; y <= GridRows; y++)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y * GridCellSize,
                X2 = GridColumns * GridCellSize,
                Y2 = y * GridCellSize,
                Stroke = brush,
                StrokeThickness = y % 5 == 0 ? 1.0 : 0.5,
                Opacity = y % 5 == 0 ? 0.5 : 0.25,
                Visibility = _isGridVisible ? Visibility.Visible : Visibility.Collapsed
            };
            _gridLines.Add(line);
            TrackCanvas.Children.Add(line);
        }
    }

    private void OnGridToggled(object sender, RoutedEventArgs e)
    {
        _isGridVisible = GridToggleButton.IsChecked ?? false;
        foreach (var line in _gridLines)
        {
            line.Visibility = _isGridVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Applies skin colors to canvas and grid based on current ISkinProvider settings.
    /// Properly handles both skin selection AND dark/light mode.
    /// </summary>
    private void ApplySkinColors()
    {
        var palette = SkinColors.GetPalette(_skinProvider.CurrentSkin, _skinProvider.IsDarkMode);
        var isSystemSkin = _skinProvider.CurrentSkin == AppSkin.System;
        var isDark = _skinProvider.IsDarkMode;

        // Set page RequestedTheme based on palette (controls Light/Dark for standard WinUI controls)
        RequestedTheme = palette.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        // Canvas background - use skin palette colors for signal box
        var gridBrush = new SolidColorBrush(palette.Accent)
        {
            Opacity = isDark ? 0.35 : 0.25
        };

        if (!isSystemSkin)
        {
            TrackCanvas.Background = palette.PanelBackgroundBrush;
            foreach (var line in _gridLines)
            {
                line.Stroke = gridBrush;
            }
        }
        else
        {
            // System skin: use WinUI theme resources
            TrackCanvas.Background = (Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"];
            var defaultBrush = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
            foreach (var line in _gridLines)
            {
                line.Stroke = defaultBrush;
            }
        }

        // Header styling
        if (FindName("HeaderBorder") is Border headerBorder)
        {
            if (isSystemSkin)
            {
                headerBorder.ClearValue(Border.BackgroundProperty);
            }
            else
            {
                headerBorder.Background = palette.HeaderBackgroundBrush;
            }
        }

        if (FindName("TitleText") is TextBlock titleText)
        {
            if (isSystemSkin)
            {
                titleText.ClearValue(TextBlock.ForegroundProperty);
            }
            else
            {
                titleText.Foreground = palette.HeaderForegroundBrush;
            }
        }

        // Apply panel backgrounds using palette colors
        if (FindName("ToolboxPanel") is Border toolboxPanel)
        {
            if (isSystemSkin)
            {
                toolboxPanel.ClearValue(Border.BackgroundProperty);
            }
            else
            {
                toolboxPanel.Background = palette.PanelBackgroundBrush;
            }
        }

        if (FindName("PropertiesPanel") is Border propertiesPanel)
        {
            if (isSystemSkin)
            {
                propertiesPanel.ClearValue(Border.BackgroundProperty);
            }
            else
            {
                propertiesPanel.Background = palette.PanelBackgroundBrush;
            }
        }

        // Refresh all track elements with new skin colors
        RefreshAllElements();
    }

    private void RefreshAllElements()
    {
        if (_planViewModel == null) return;
        foreach (var element in _planViewModel.Elements)
        {
            RefreshElementVisual(element);
        }
    }

    private void OnToolPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["SubtleFillColorTertiaryBrush"];
        }
    }

    private void OnToolPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        }
    }

    private void OnToolDragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if (sender is Border border && border.Tag is string typeTag && !string.IsNullOrEmpty(typeTag))
        {
            e.Data.SetText($"NEW:{typeTag}");
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }
        else
        {
            // Cancel drag if no valid tag found - prevents stale drag data issues
            e.Cancel = true;
        }
    }

    private void OnCanvasDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Move;
    }

    private async void OnCanvasDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text) || _planViewModel == null)
            return;

        var text = await e.DataView.GetTextAsync();

        // Validate that we have valid drag data
        if (string.IsNullOrEmpty(text))
            return;

        var pos = e.GetPosition(TrackCanvas);
        int gridX = Math.Clamp((int)(pos.X / GridCellSize), 0, GridColumns - 1);
        int gridY = Math.Clamp((int)(pos.Y / GridCellSize), 0, GridRows - 1);

        // Check if an element already exists at this position
        var existingElement = _planViewModel.HitTest(gridX, gridY);

        if (text.StartsWith("NEW:"))
        {
            var typeTag = text[4..];

            // If an element exists, remove it first
            if (existingElement != null)
            {
                if (_elementVisuals.TryGetValue(existingElement.Id, out var visual))
                {
                    TrackCanvas.Children.Remove(visual);
                    _elementVisuals.Remove(existingElement.Id);
                }
                _planViewModel.RemoveElement(existingElement);
            }

            CreateElementByType(typeTag, gridX, gridY);
        }
        else if (text.StartsWith("MOVE:") && Guid.TryParse(text[5..], out var elementId))
        {
            var element = _planViewModel.FindById(elementId);
            if (element != null && (existingElement == null || existingElement.Id == elementId))
            {
                MoveElement(element, gridX, gridY);
            }
        }
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Wenn das Event bereits behandelt wurde (z.B. von einem Element), ignorieren
        if (e.Handled) return;

        var point = e.GetCurrentPoint(TrackCanvas);

        if (point.Properties.IsRightButtonPressed)
        {
            // Panning starten
            _isPanning = true;
            _panStartPoint = point.Position;
            _panStartHorizontalOffset = CanvasScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = CanvasScrollViewer.VerticalOffset;
            TrackCanvas.CapturePointer(e.Pointer);
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            // Klick auf leeren Canvas-Bereich - deselektieren
            _planViewModel?.ClearSelection();
            UpdatePropertiesPanel();
        }
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPanning) return;

        var point = e.GetCurrentPoint(TrackCanvas).Position;
        var deltaX = point.X - _panStartPoint.X;
        var deltaY = point.Y - _panStartPoint.Y;

        CanvasScrollViewer.ChangeView(
            _panStartHorizontalOffset - deltaX,
            _panStartVerticalOffset - deltaY,
            null,
            true);
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            TrackCanvas.ReleasePointerCapture(e.Pointer);
        }
    }

    /// <summary>
    /// Creates an element based on the type tag from drag-drop.
    /// </summary>
    private void CreateElementByType(string typeTag, int gridX, int gridY)
    {
        if (_planViewModel == null) return;

        SbElement? element = typeTag switch
        {
            "TrackStraight" => _planViewModel.AddTrackStraight(gridX, gridY),
            "TrackCurve" => _planViewModel.AddTrackCurve(gridX, gridY),
            "Switch" => _planViewModel.AddSwitch(gridX, gridY),
            "Signal" => _planViewModel.AddSignal(gridX, gridY),
            "Detector" => _planViewModel.AddDetector(gridX, gridY),
            _ => null
        };

        if (element == null) return;

        CreateElementVisual(element);
        SelectElement(element);
        UpdateStatistics();
    }

    private void MoveElement(SbElement element, int newGridX, int newGridY)
    {
        element.X = newGridX;
        element.Y = newGridY;
        RefreshElementVisual(element);
    }

    private void CreateElementVisual(SbElement element)
    {
        var visual = BuildElementVisual(element);
        Canvas.SetLeft(visual, element.X * GridCellSize);
        Canvas.SetTop(visual, element.Y * GridCellSize);
        TrackCanvas.Children.Add(visual);
        _elementVisuals[element.Id] = visual;
    }

    private FrameworkElement BuildElementVisual(SbElement element)
    {
        var container = new Canvas
        {
            Width = GridCellSize,
            Height = GridCellSize,
            Tag = element.Id,
            CanDrag = SelectedElement?.Id == element.Id, // Nur selektierte Elemente koennen gedraggt werden
            Background = new SolidColorBrush(Colors.Transparent),
            RenderTransform = new RotateTransform
            {
                Angle = element.Rotation,
                CenterX = GridCellSize / 2.0,
                CenterY = GridCellSize / 2.0
            }
        };

        // Highlight-Rahmen fuer selektiertes Element
        if (SelectedElement?.Id == element.Id)
        {
            var accentStroke = (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            var accentFillSource = (SolidColorBrush)Application.Current.Resources["AccentFillColorSecondaryBrush"];
            var accentFill = new SolidColorBrush(accentFillSource.Color)
            {
                Opacity = 0.2
            };

            var highlight = new Rectangle
            {
                Width = GridCellSize - 2,
                Height = GridCellSize - 2,
                Stroke = accentStroke,
                StrokeThickness = 2,
                Fill = accentFill,
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(highlight, 1);
            Canvas.SetTop(highlight, 1);
            container.Children.Add(highlight);
        }

        // Element-spezifische Grafik erstellen (type-based dispatch)
        var graphic = element switch
        {
            SbTrackStraight => CreateStraightTrackGraphic(),
            SbTrackCurve => CreateCurve90Graphic(),
            SbSwitch sw => CreateSwitchGraphic(sw),
            SbSignal sig => CreateSignalGraphic(sig),
            SbDetector => CreateFeedbackGraphic(),
            _ => CreatePlaceholderGraphic()
        };

        container.Children.Add(graphic);

        // Event-Handler fuer Klick
        container.PointerPressed += (s, e) =>
        {
            e.Handled = true;
            var point = e.GetCurrentPoint(container);
            if (point.Properties.IsLeftButtonPressed)
            {
                SelectElement(element);
            }
        };

        // Doppelklick fuer Weichen/Signale schalten
        container.DoubleTapped += (s, e) =>
        {
            e.Handled = true;
            if (element is SbSwitch sw)
            {
                ToggleSwitchPosition(sw);
            }
            else if (element is SbSignal sig)
            {
                ToggleSignalAspect(sig);
            }
        };

        // Drag-Daten setzen (CanDrag ist bereits nur fuer selektierte Elemente true)
        container.DragStarting += (s, e) =>
        {
            e.Data.SetText($"MOVE:{element.Id}");
            e.Data.RequestedOperation = DataPackageOperation.Move;
        };

        return container;
    }

    private void ToggleSwitchPosition(SbSwitch element)
    {
        element.SwitchPosition = element.SwitchPosition switch
        {
            SwitchPosition.Straight => SwitchPosition.DivergingLeft,
            SwitchPosition.DivergingLeft => SwitchPosition.DivergingRight,
            _ => SwitchPosition.Straight
        };
        RefreshElementVisual(element);
        UpdatePropertiesPanel();
    }

    private void ToggleSignalAspect(SbSignal element)
    {
        element.SignalAspect = element.SignalAspect switch
        {
            SignalAspect.Hp0 => SignalAspect.Ks1,
            SignalAspect.Ks1 => SignalAspect.Ks2,
            SignalAspect.Ks2 => SignalAspect.Hp0,
            _ => SignalAspect.Hp0
        };
        RefreshElementVisual(element);
        UpdatePropertiesPanel();
    }

    private void RefreshElementVisual(SbElement element)
    {
        if (_elementVisuals.TryGetValue(element.Id, out var oldVisual))
        {
            TrackCanvas.Children.Remove(oldVisual);
            _elementVisuals.Remove(element.Id);
        }
        CreateElementVisual(element);
    }

    private void SelectElement(SbElement? element)
    {
        if (SelectedElement?.Id == element?.Id) return;

        var previousSelection = SelectedElement;
        _planViewModel?.Select(element);

        DispatcherQueue.TryEnqueue(() =>
        {
            if (previousSelection != null && _planViewModel?.Elements.Contains(previousSelection) == true)
            {
                RefreshElementVisual(previousSelection);
            }
            if (element != null && _planViewModel?.Elements.Contains(element) == true)
            {
                RefreshElementVisual(element);
            }
        });

        _blinkingLeds.Clear();
        UpdatePropertiesPanel();
    }

    private void DeleteSelectedElement()
    {
        if (SelectedElement == null || _planViewModel == null) return;

        var elementToDelete = SelectedElement;

        _planViewModel.ClearSelection();
        _blinkingLeds.Clear();
        UpdatePropertiesPanel();

        // Dann aus Canvas und Listen entfernen
        if (_elementVisuals.TryGetValue(elementToDelete.Id, out var visual))
        {
            TrackCanvas.Children.Remove(visual);
            _elementVisuals.Remove(elementToDelete.Id);
        }

        _planViewModel.RemoveElement(elementToDelete);
        UpdateStatistics();
    }

    /// <summary>
    /// Loads the signal box plan from the domain model and recreates all visuals.
    /// </summary>
    private void LoadFromModel()
    {
        // Clear existing visuals
        foreach (var visual in _elementVisuals.Values)
        {
            TrackCanvas.Children.Remove(visual);
        }
        _elementVisuals.Clear();

        // Get or create ViewModel and refresh from model
        var planVm = GetOrCreatePlanViewModel();
        if (planVm == null)
            return;

        planVm.Refresh();

        // Recreate visuals for all elements
        foreach (var element in planVm.Elements)
        {
            CreateElementVisual(element);
        }

        UpdateStatistics();
    }

    // Properties Panel methods moved to SignalBoxPage.Properties.cs
}
