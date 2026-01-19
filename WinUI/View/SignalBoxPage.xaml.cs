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

using System;
using System.Collections.Generic;
using System.Linq;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

/// <summary>
/// Modernes elektronisches Stellwerk (ESTW) - Gleisbildstellwerk.
/// Ermöglicht die grafische Planung von Gleisanlagen mit Signalen und Weichen.
/// Uses SignalBoxPlanViewModel for MVVM-compliant data management.
/// Supports skin switching via ISkinProvider.
/// </summary>
public sealed partial class SignalBoxPage : Page, IPersistablePage
{
    private const int GridCellSize = 60;
    private const int GridColumns = 32;
    private const int GridRows = 18;

    private readonly Dictionary<Guid, FrameworkElement> _elementVisuals = [];
    private readonly List<Line> _gridLines = [];
    private readonly ISkinProvider _skinProvider;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;

    /// <summary>
    /// MainWindowViewModel for accessing the active project.
    /// </summary>
    public MainWindowViewModel ViewModel { get; }

    /// <summary>
    /// SignalBoxPlan ViewModel - the source of truth for element data.
    /// </summary>
    private SignalBoxPlanViewModel? _planViewModel;

    /// <inheritdoc />
    public bool HasUnsavedChanges { get; private set; }

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
        AppSettings settings,
        ISettingsService? settingsService = null)
    {
        ViewModel = viewModel;
        _skinProvider = skinProvider;
        _settings = settings;
        _settingsService = settingsService;

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
    private SignalBoxPlanElementViewModel? SelectedElement => _planViewModel?.SelectedElement;

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

    #region Grid Drawing

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

    #endregion

    #region Skin System

    // Skin selection handlers - same pattern as TrainControlPage
    private async void OnSkinSystemClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.System);
    private async void OnSkinBlueClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Blue);
    private async void OnSkinGreenClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Green);
    private async void OnSkinVioletClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Violet);
    private async void OnSkinOrangeClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Orange);
    private async void OnSkinDarkOrangeClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.DarkOrange);
    private async void OnSkinRedClicked(object sender, RoutedEventArgs e) => await SetSkinAsync(AppSkin.Red);

    private async Task SetSkinAsync(AppSkin skin)
    {
        _skinProvider.SetSkin(skin);
        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
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

        // Canvas background - use skin-aware colors for signal box
        // Signal boxes typically work better with darker backgrounds for contrast
        var (canvasBg, gridColor) = (_skinProvider.CurrentSkin, isDark) switch
        {
            // Blue skin
            (AppSkin.Blue, true) => (Color.FromArgb(255, 10, 30, 50), Color.FromArgb(100, 0, 120, 212)),
            (AppSkin.Blue, false) => (Color.FromArgb(255, 220, 235, 250), Color.FromArgb(120, 0, 120, 212)),
            
            // Green skin
            (AppSkin.Green, true) => (Color.FromArgb(255, 10, 40, 20), Color.FromArgb(100, 16, 124, 16)),
            (AppSkin.Green, false) => (Color.FromArgb(255, 220, 245, 220), Color.FromArgb(120, 16, 124, 16)),
            
            // Violet skin (always dark-ish)
            (AppSkin.Violet, _) => (Color.FromArgb(255, 25, 25, 30), Color.FromArgb(80, 106, 90, 205)),
            
            // Orange skin
            (AppSkin.Orange, true) => (Color.FromArgb(255, 25, 15, 5), Color.FromArgb(100, 255, 140, 0)),
            (AppSkin.Orange, false) => (Color.FromArgb(255, 255, 245, 230), Color.FromArgb(120, 255, 140, 0)),
            
            // Dark Orange skin
            (AppSkin.DarkOrange, true) => (Color.FromArgb(255, 20, 12, 5), Color.FromArgb(100, 255, 102, 0)),
            (AppSkin.DarkOrange, false) => (Color.FromArgb(255, 255, 240, 225), Color.FromArgb(120, 255, 102, 0)),
            
            // Red skin
            (AppSkin.Red, true) => (Color.FromArgb(255, 30, 10, 10), Color.FromArgb(100, 204, 0, 0)),
            (AppSkin.Red, false) => (Color.FromArgb(255, 255, 235, 235), Color.FromArgb(120, 204, 0, 0)),
            
            // System skin - use theme resources
            _ => ((Color?)null, (Color?)null)
        };

        if (canvasBg.HasValue)
        {
            TrackCanvas.Background = new SolidColorBrush(canvasBg.Value);
            foreach (var line in _gridLines)
            {
                line.Stroke = new SolidColorBrush(gridColor!.Value);
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

    #endregion

    #region Toolbox Drag & Drop

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
        if (sender is Border { Tag: string typeTag })
        {
            e.Data.SetText($"NEW:{typeTag}");
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }
    }

    #endregion

    #region Canvas Interaction

    private void OnCanvasDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Move;
    }

    private async void OnCanvasDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text) || _planViewModel == null)
            return;

        var text = await e.DataView.GetTextAsync();
        var pos = e.GetPosition(TrackCanvas);
        int gridX = Math.Clamp((int)(pos.X / GridCellSize), 0, GridColumns - 1);
        int gridY = Math.Clamp((int)(pos.Y / GridCellSize), 0, GridRows - 1);

        // Check if an element already exists at this position
        var existingElement = _planViewModel.HitTest(gridX, gridY);

        if (text.StartsWith("NEW:"))
        {
            var typeTag = text[4..];

            if (Enum.TryParse<SignalBoxSymbol>(typeTag, out var symbol))
            {
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

                CreateElement(symbol, gridX, gridY);
            }
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

    #endregion

    #region Element Management

    private void CreateElement(SignalBoxSymbol symbol, int gridX, int gridY)
    {
        if (_planViewModel == null) return;

        var elementVm = _planViewModel.AddElement(symbol, gridX, gridY);
        CreateElementVisual(elementVm);
        SelectElement(elementVm);
        UpdateStatistics();
        MarkDirty();
    }

    private void MoveElement(SignalBoxPlanElementViewModel element, int newGridX, int newGridY)
    {
        element.X = newGridX;
        element.Y = newGridY;
        RefreshElementVisual(element);
        MarkDirty();
    }

    private void CreateElementVisual(SignalBoxPlanElementViewModel element)
    {
        var visual = BuildElementVisual(element);
        Canvas.SetLeft(visual, element.X * GridCellSize);
        Canvas.SetTop(visual, element.Y * GridCellSize);
        TrackCanvas.Children.Add(visual);
        _elementVisuals[element.Id] = visual;
    }

    private FrameworkElement BuildElementVisual(SignalBoxPlanElementViewModel element)
    {
        var container = new Canvas
        {
            Width = GridCellSize,
            Height = GridCellSize,
            Tag = element.Id,
            CanDrag = SelectedElement?.Id == element.Id, // Nur selektierte Elemente können gedraggt werden
            Background = new SolidColorBrush(Colors.Transparent),
            RenderTransform = new RotateTransform
            {
                Angle = element.Rotation,
                CenterX = GridCellSize / 2.0,
                CenterY = GridCellSize / 2.0
            }
        };

        // Highlight-Rahmen für selektiertes Element
        if (SelectedElement?.Id == element.Id)
        {
            var highlight = new Rectangle
            {
                Width = GridCellSize - 2,
                Height = GridCellSize - 2,
                Stroke = new SolidColorBrush(Colors.DodgerBlue),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(30, 30, 144, 255)),
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(highlight, 1);
            Canvas.SetTop(highlight, 1);
            container.Children.Add(highlight);
        }

        // Element-spezifische Grafik erstellen
        var graphic = element.Symbol switch
        {
            SignalBoxSymbol.TrackStraight => CreateStraightTrackGraphic(),
            SignalBoxSymbol.TrackCurve45 => CreateCurve45Graphic(),
            SignalBoxSymbol.TrackCurve90 => CreateCurve90Graphic(),
            SignalBoxSymbol.TrackEnd => CreateBufferStopGraphic(),
            SignalBoxSymbol.SwitchSimpleLeft => CreateSwitchGraphic(element, isLeft: true),
            SignalBoxSymbol.SwitchSimpleRight => CreateSwitchGraphic(element, isLeft: false),
            SignalBoxSymbol.SwitchThreeWay => CreateThreeWaySwitchGraphic(element),
            SignalBoxSymbol.SwitchDoubleSlip => CreateDoubleSwitchGraphic(element),
            SignalBoxSymbol.TrackCrossing => CreateCrossingGraphic(),
            SignalBoxSymbol.SignalKsMain => CreateKsMainSignalGraphic(element),
            SignalBoxSymbol.SignalKsDistant => CreateKsDistantSignalGraphic(element),
            SignalBoxSymbol.SignalSh or SignalBoxSymbol.SignalDwarf => CreateShuntSignalGraphic(element),
            SignalBoxSymbol.SignalBlock => CreateBlockSignalGraphic(element),
            SignalBoxSymbol.SignalKsCombined => CreateKsSignalScreenGraphic(element),
            SignalBoxSymbol.Platform => CreatePlatformGraphic(),
            SignalBoxSymbol.Detector or SignalBoxSymbol.AxleCounter => CreateFeedbackGraphic(),
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
            if (SignalBoxPlanViewModel.IsSwitchSymbol(element.Symbol))
            {
                ToggleSwitchPosition(element);
            }
            else if (SignalBoxPlanViewModel.IsSignalSymbol(element.Symbol))
            {
                ToggleSignalAspect(element);
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

    private void ToggleSwitchPosition(SignalBoxPlanElementViewModel element)
    {
        element.SwitchPosition = element.Symbol switch
        {
            SignalBoxSymbol.SwitchThreeWay => element.SwitchPosition switch
            {
                Domain.SwitchPosition.Straight => Domain.SwitchPosition.DivergingLeft,
                Domain.SwitchPosition.DivergingLeft => Domain.SwitchPosition.DivergingRight,
                _ => Domain.SwitchPosition.Straight
            },
            SignalBoxSymbol.SwitchDoubleSlip => element.SwitchPosition switch
            {
                Domain.SwitchPosition.Straight => Domain.SwitchPosition.DivergingLeft,
                _ => Domain.SwitchPosition.Straight
            },
            SignalBoxSymbol.SwitchSimpleLeft => element.SwitchPosition switch
            {
                Domain.SwitchPosition.Straight => Domain.SwitchPosition.DivergingLeft,
                _ => Domain.SwitchPosition.Straight
            },
            SignalBoxSymbol.SwitchSimpleRight => element.SwitchPosition switch
            {
                Domain.SwitchPosition.Straight => Domain.SwitchPosition.DivergingRight,
                _ => Domain.SwitchPosition.Straight
            },
            _ => Domain.SwitchPosition.Straight
        };
        RefreshElementVisual(element);
        UpdatePropertiesPanel();
    }

    private void ToggleSignalAspect(SignalBoxPlanElementViewModel element)
    {
        element.SignalAspect = element.SignalAspect switch
        {
            Domain.SignalAspect.Hp0 => Domain.SignalAspect.Ks1,
            Domain.SignalAspect.Ks1 => Domain.SignalAspect.Ks2,
            Domain.SignalAspect.Ks2 => Domain.SignalAspect.Hp0,
            _ => Domain.SignalAspect.Hp0
        };
        RefreshElementVisual(element);
        UpdatePropertiesPanel();
    }

    private void RefreshElementVisual(SignalBoxPlanElementViewModel element)
    {
        if (_elementVisuals.TryGetValue(element.Id, out var oldVisual))
        {
            TrackCanvas.Children.Remove(oldVisual);
            _elementVisuals.Remove(element.Id);
        }
        CreateElementVisual(element);
    }

    private void SelectElement(SignalBoxPlanElementViewModel? element)
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
        MarkDirty();
    }

    #endregion

    // Properties Panel methods moved to SignalBoxPage.Properties.cs

    #region IPersistablePage Implementation

    /// <inheritdoc />
    public void SyncToModel()
    {
        // ViewModel already syncs to Model automatically via property setters
        // Just ensure the plan exists
        var project = ViewModel.SelectedProject?.Model;
        if (project == null)
            return;

        project.SignalBoxPlan ??= new SignalBoxPlan
        {
            Name = "Stellwerk",
            GridWidth = GridColumns,
            GridHeight = GridRows,
            CellSize = GridCellSize
        };

        HasUnsavedChanges = false;
    }

    /// <inheritdoc />
    public void LoadFromModel()
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

        HasUnsavedChanges = false;
        UpdateStatistics();
    }

    /// <summary>
    /// Mark page as having unsaved changes.
    /// </summary>
    private void MarkDirty()
    {
        HasUnsavedChanges = true;
    }

    #endregion
}
