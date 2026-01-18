namespace Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using System;
using System.Collections.Generic;
using System.Linq;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

/// <summary>
/// Modernes elektronisches Stellwerk (ESTW) - Gleisbildstellwerk.
/// Ermöglicht die grafische Planung von Gleisanlagen mit Signalen und Weichen.
/// </summary>
public sealed partial class SignalBoxPage : Page
{
    private const int GridCellSize = 60;
    private const int GridColumns = 32;
    private const int GridRows = 18;

    private readonly List<TrackElement> _elements = [];
    private readonly Dictionary<Guid, FrameworkElement> _elementVisuals = [];
    private readonly List<Line> _gridLines = [];
    private readonly List<Ellipse> _blinkingLeds = [];

    private TrackElement? _selectedElement;
    private bool _isGridVisible = true;
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    private DispatcherTimer? _blinkTimer;
    private bool _blinkState;

    private string _currentSkin = "System";

    public SignalBoxPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        InitializeCanvas();
        InitializeBlinkTimer();
        InitializeAspectButtons();
        UpdateStatistics();
        UpdatePropertiesPanel();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _blinkTimer?.Stop();
    }

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

    private void OnSkinSystemClicked(object sender, RoutedEventArgs e) => ApplySkin("System");
    private void OnSkinBlueClicked(object sender, RoutedEventArgs e) => ApplySkin("Blue");
    private void OnSkinGreenClicked(object sender, RoutedEventArgs e) => ApplySkin("Green");
    private void OnSkinDarkClicked(object sender, RoutedEventArgs e) => ApplySkin("Dark");
    private void OnSkinClassicClicked(object sender, RoutedEventArgs e) => ApplySkin("Classic");

    private void ApplySkin(string skinName)
    {
        _currentSkin = skinName;

        var (canvasBg, gridColor) = skinName switch
        {
            "Blue" => (Color.FromArgb(255, 10, 30, 50), Color.FromArgb(100, 0, 120, 212)),
            "Green" => (Color.FromArgb(255, 10, 40, 20), Color.FromArgb(100, 16, 124, 16)),
            "Dark" => (Color.FromArgb(255, 15, 15, 15), Color.FromArgb(80, 60, 60, 60)),
            "Classic" => (Color.FromArgb(255, 30, 30, 35), Color.FromArgb(100, 80, 80, 90)),
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
            TrackCanvas.Background = (Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"];
            var defaultBrush = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
            foreach (var line in _gridLines)
            {
                line.Stroke = defaultBrush;
            }
        }

        // Alle Elemente neu zeichnen mit neuem Skin
        RefreshAllElements();
    }

    private void RefreshAllElements()
    {
        foreach (var element in _elements)
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
        if (!e.DataView.Contains(StandardDataFormats.Text))
            return;

        var text = await e.DataView.GetTextAsync();
        var pos = e.GetPosition(TrackCanvas);
        int gridX = Math.Clamp((int)(pos.X / GridCellSize), 0, GridColumns - 1);
        int gridY = Math.Clamp((int)(pos.Y / GridCellSize), 0, GridRows - 1);

        // Prüfen ob bereits ein Element an dieser Position existiert
        var existingElement = _elements.FirstOrDefault(el => el.GridX == gridX && el.GridY == gridY);

        if (text.StartsWith("NEW:"))
        {
            var typeTag = text[4..];
            
            if (Enum.TryParse<TrackElementType>(typeTag, out var elementType))
            {
                // Wenn bereits ein Element existiert, dieses zuerst entfernen
                if (existingElement != null)
                {
                    if (_elementVisuals.TryGetValue(existingElement.Id, out var visual))
                    {
                        TrackCanvas.Children.Remove(visual);
                        _elementVisuals.Remove(existingElement.Id);
                    }
                    _elements.Remove(existingElement);
                    
                    if (_selectedElement?.Id == existingElement.Id)
                    {
                        _selectedElement = null;
                    }
                }
                
                CreateElement(elementType, gridX, gridY);
            }
        }
        else if (text.StartsWith("MOVE:") && Guid.TryParse(text[5..], out var elementId))
        {
            var element = _elements.FirstOrDefault(el => el.Id == elementId);
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
            SelectElement(null);
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

    private void CreateElement(TrackElementType type, int gridX, int gridY)
    {
        var element = new TrackElement
        {
            Id = Guid.NewGuid(),
            Type = type,
            GridX = gridX,
            GridY = gridY,
            Rotation = 0,
            Address = GetNextAddress(type),
            SignalAspect = IsSignalType(type) ? SignalAspect.Hp0 : SignalAspect.Hp0,
            SwitchPosition = IsSwitchType(type) ? SwitchPosition.Straight : SwitchPosition.Straight
        };

        _elements.Add(element);
        CreateElementVisual(element);
        SelectElement(element);
        UpdateStatistics();
    }

    private int GetNextAddress(TrackElementType type)
    {
        var relevantElements = _elements.Where(e =>
            (IsSignalType(type) && IsSignalType(e.Type)) ||
            (IsSwitchType(type) && IsSwitchType(e.Type)));

        return relevantElements.Any() ? relevantElements.Max(e => e.Address) + 1 : 1;
    }

    private void MoveElement(TrackElement element, int newGridX, int newGridY)
    {
        element.GridX = newGridX;
        element.GridY = newGridY;
        RefreshElementVisual(element);
    }

    private void CreateElementVisual(TrackElement element)
    {
        var visual = BuildElementVisual(element);
        Canvas.SetLeft(visual, element.GridX * GridCellSize);
        Canvas.SetTop(visual, element.GridY * GridCellSize);
        TrackCanvas.Children.Add(visual);
        _elementVisuals[element.Id] = visual;
    }

    private FrameworkElement BuildElementVisual(TrackElement element)
    {
        var container = new Canvas
        {
            Width = GridCellSize,
            Height = GridCellSize,
            Tag = element.Id,
            CanDrag = _selectedElement?.Id == element.Id, // Nur selektierte Elemente können gedraggt werden
            Background = new SolidColorBrush(Colors.Transparent),
            RenderTransform = new RotateTransform
            {
                Angle = element.Rotation,
                CenterX = GridCellSize / 2.0,
                CenterY = GridCellSize / 2.0
            }
        };

        // Highlight-Rahmen für selektiertes Element
        if (_selectedElement?.Id == element.Id)
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
        var graphic = element.Type switch
        {
            TrackElementType.StraightTrack => CreateStraightTrackGraphic(),
            TrackElementType.Curve45 => CreateCurve45Graphic(),
            TrackElementType.Curve90 => CreateCurve90Graphic(),
            TrackElementType.BufferStop => CreateBufferStopGraphic(),
            TrackElementType.SwitchLeft => CreateSwitchGraphic(element, isLeft: true),
            TrackElementType.SwitchRight => CreateSwitchGraphic(element, isLeft: false),
            TrackElementType.SwitchThreeWay => CreateThreeWaySwitchGraphic(element),
            TrackElementType.SwitchDouble => CreateDoubleSwitchGraphic(element),
            TrackElementType.Crossing => CreateCrossingGraphic(),
            TrackElementType.SignalKsMain => CreateKsMainSignalGraphic(element),
            TrackElementType.SignalKsDistant => CreateKsDistantSignalGraphic(element),
            TrackElementType.SignalShunt => CreateShuntSignalGraphic(element),
            TrackElementType.SignalBlock => CreateBlockSignalGraphic(element),
            TrackElementType.SignalKsScreen => CreateKsSignalScreenGraphic(element),
            TrackElementType.Platform => CreatePlatformGraphic(),
            TrackElementType.Feedback => CreateFeedbackGraphic(),
            _ => CreatePlaceholderGraphic()
        };

        container.Children.Add(graphic);

            // Event-Handler für Klick
            container.PointerPressed += (s, e) =>
            {
                e.Handled = true;
                var point = e.GetCurrentPoint(container);
                if (point.Properties.IsLeftButtonPressed)
                {
                    SelectElement(element);
                }
            };

            // Doppelklick für Weichen/Signale schalten
            container.DoubleTapped += (s, e) =>
            {
                e.Handled = true;
                if (IsSwitchType(element.Type))
                {
                    ToggleSwitchPosition(element);
                        }
                        else if (IsSignalType(element.Type))
                        {
                            ToggleSignalAspect(element);
                        }
                    };

                    // Drag-Daten setzen (CanDrag ist bereits nur für selektierte Elemente true)
                    container.DragStarting += (s, e) =>
                    {
                        e.Data.SetText($"MOVE:{element.Id}");
                        e.Data.RequestedOperation = DataPackageOperation.Move;
                    };

                    return container;
        }

        private void ToggleSwitchPosition(TrackElement element)
        {
            element.SwitchPosition = element.Type switch
                {
                    TrackElementType.SwitchThreeWay => element.SwitchPosition switch
                    {
                        SwitchPosition.Straight => SwitchPosition.DivergingLeft,
                        SwitchPosition.DivergingLeft => SwitchPosition.DivergingRight,
                        _ => SwitchPosition.Straight
                    },
                    TrackElementType.SwitchDouble => element.SwitchPosition switch
                    {
                        SwitchPosition.Straight => SwitchPosition.DivergingLeft,
                        _ => SwitchPosition.Straight
                    },
                    TrackElementType.SwitchLeft => element.SwitchPosition switch
                    {
                        SwitchPosition.Straight => SwitchPosition.DivergingLeft,
                        _ => SwitchPosition.Straight
                    },
                    TrackElementType.SwitchRight => element.SwitchPosition switch
                    {
                        SwitchPosition.Straight => SwitchPosition.DivergingRight,
                        _ => SwitchPosition.Straight
                    },
                    _ => SwitchPosition.Straight
                };
                RefreshElementVisual(element);
                UpdatePropertiesPanel();
            }

            private void ToggleSignalAspect(TrackElement element)
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

            private void RefreshElementVisual(TrackElement element)
            {
                if (_elementVisuals.TryGetValue(element.Id, out var oldVisual))
                {
                    TrackCanvas.Children.Remove(oldVisual);
                    _elementVisuals.Remove(element.Id);
                }
                CreateElementVisual(element);
            }

            private void SelectElement(TrackElement? element)
            {
                if (_selectedElement?.Id == element?.Id) return;
        
                var previousSelection = _selectedElement;
                _selectedElement = element;

                DispatcherQueue.TryEnqueue(() =>
                {
                    if (previousSelection != null && _elements.Contains(previousSelection))
                    {
                        RefreshElementVisual(previousSelection);
                    }
                    if (element != null && _elements.Contains(element))
                    {
                        RefreshElementVisual(element);
                    }
                });

                _blinkingLeds.Clear();
                UpdatePropertiesPanel();
            }

            private void DeleteSelectedElement()
            {
                if (_selectedElement == null) return;

                var elementToDelete = _selectedElement;

                _selectedElement = null;
                _blinkingLeds.Clear();
                UpdatePropertiesPanel();

        // Dann aus Canvas und Listen entfernen
        if (_elementVisuals.TryGetValue(elementToDelete.Id, out var visual))
        {
            TrackCanvas.Children.Remove(visual);
            _elementVisuals.Remove(elementToDelete.Id);
        }

        _elements.Remove(elementToDelete);
        UpdateStatistics();
    }

    #endregion

    #region Element Graphics - Simplified Track Display

    // Verbindungspunkte für alle Elemente (60x60 Zelle):
    // Links:  (0, 30)   Rechts: (60, 30)
    // Oben:   (30, 0)   Unten:  (30, 60)
    
    private static readonly SolidColorBrush TrackBrush = new(Color.FromArgb(255, 200, 200, 200));
    private static readonly SolidColorBrush TrackActiveBrush = new(Color.FromArgb(255, 0, 200, 0));
    private static readonly SolidColorBrush BufferStopBrush = new(Color.FromArgb(255, 200, 60, 60));
    private const double TrackThickness = 4;

    private static Canvas CreateStraightTrackGraphic()
    {
        // Horizontal: Links (0,30) → Rechts (60,30)
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });
        return canvas;
    }

    private static Canvas CreateCurve45Graphic()
    {
        // 45° Kurve (flach): Links (0,30) → Oben (30,0)
        // Flachere Biegung als 90°-Kurve
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Path
        {
            Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), 
                "M 0,30 C 20,30 30,15 30,0"),
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });
        return canvas;
    }

    private static Canvas CreateCurve90Graphic()
    {
        // 90° Kurve (eng): Links (0,30) → Oben (30,0)
        // Enge Biegung wie ein Viertelkreis
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Path
        {
            Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), 
                "M 0,30 C 0,13 13,0 30,0"),
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });
        return canvas;
    }

    private static Canvas CreateBufferStopGraphic()
    {
        // Prellbock: Links (0,30) → Mitte, dann Stopp
        var canvas = new Canvas { Width = 60, Height = 60 };
        
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 45, Y2 = 30,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });
        
        canvas.Children.Add(new Line
        {
            X1 = 45, Y1 = 18, X2 = 45, Y2 = 42,
            Stroke = BufferStopBrush,
            StrokeThickness = 5
        });
        
        return canvas;
    }

    private static Canvas CreateSwitchGraphic(TrackElement element, bool isLeft)
    {
        // Weiche: Links (0,30) → Rechts (60,30) + Abzweig nach Oben/Unten
        var canvas = new Canvas { Width = 60, Height = 60 };
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isDiverging = isLeft 
            ? element.SwitchPosition == SwitchPosition.DivergingLeft 
            : element.SwitchPosition == SwitchPosition.DivergingRight;

        // Hauptgleis durchgehend
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Abzweig: Von Mitte (30,30) nach Rechts-Oben (60,0) oder Rechts-Unten (60,60)
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 60, Y2 = isLeft ? 0 : 60,
            Stroke = isDiverging ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        return canvas;
    }

    private static Canvas CreateThreeWaySwitchGraphic(TrackElement element)
    {
        // Dreiwege-Weiche: Gerade + Abzweig nach oben + Abzweig nach unten
        var canvas = new Canvas { Width = 60, Height = 60 };
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isLeft = element.SwitchPosition == SwitchPosition.DivergingLeft;
        var isRight = element.SwitchPosition == SwitchPosition.DivergingRight;

        // Hauptgleis durchgehend: (0,30) → (60,30)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Abzweig nach oben: (30,30) → (60,0)
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 60, Y2 = 0,
            Stroke = isLeft ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Abzweig nach unten: (30,30) → (60,60)
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 30,
            X2 = 60, Y2 = 60,
            Stroke = isRight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        return canvas;
    }

    private static Canvas CreateDoubleSwitchGraphic(TrackElement element)
    {
        // DKW (Doppelkreuzungsweiche): 4 Fahrwege möglich
        // Straight = Beide geradeaus, DivergingLeft = Kreuzung aktiv
        var canvas = new Canvas { Width = 60, Height = 60 };
        var isStraight = element.SwitchPosition == SwitchPosition.Straight;
        var isCrossing = element.SwitchPosition != SwitchPosition.Straight;

        // Oberes Gleis: (0,15) → (60,15)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 15, X2 = 60, Y2 = 15,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Unteres Gleis: (0,45) → (60,45)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 45, X2 = 60, Y2 = 45,
            Stroke = isStraight ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Kreuzverbindung oben-links nach unten-rechts: (0,15) → (60,45)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 15, X2 = 60, Y2 = 45,
            Stroke = isCrossing ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness - 1
        });
        
        // Kreuzverbindung unten-links nach oben-rechts: (0,45) → (60,15)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 45, X2 = 60, Y2 = 15,
            Stroke = isCrossing ? TrackActiveBrush : TrackBrush,
            StrokeThickness = TrackThickness - 1
        });

        return canvas;
    }

    private static Canvas CreateCrossingGraphic()
    {
        // Kreuzung: Horizontal + Vertikal
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Horizontal: (0,30) → (60,30)
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Vertikal: (30,0) → (30,60)
        canvas.Children.Add(new Line
        {
            X1 = 30, Y1 = 0, X2 = 30, Y2 = 60,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        return canvas;
    }

    private Canvas CreateKsMainSignalGraphic(TrackElement element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Use KsSignalScreen for realistic display
        var signalScreen = new Controls.KsSignalScreen
        {
            Width = 50,
            Height = 50,
            Aspect = element.SignalAspect.ToString()
        };
        Canvas.SetLeft(signalScreen, 5);
        Canvas.SetTop(signalScreen, 5);
        canvas.Children.Add(signalScreen);

        return canvas;
    }

    private Canvas CreateKsDistantSignalGraphic(TrackElement element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Use KsSignalScreen for realistic display
        var signalScreen = new Controls.KsSignalScreen
        {
            Width = 50,
            Height = 50,
            Aspect = element.SignalAspect.ToString()
        };
        Canvas.SetLeft(signalScreen, 5);
        Canvas.SetTop(signalScreen, 5);
        canvas.Children.Add(signalScreen);

        return canvas;
    }

    private static Canvas CreateShuntSignalGraphic(TrackElement element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Signalmast
        canvas.Children.Add(new Rectangle { Width = 4, Height = 30, Fill = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44)) });
        Canvas.SetLeft(canvas.Children[^1], 28);
        Canvas.SetTop(canvas.Children[^1], 25);

        // Signalgehäuse
        canvas.Children.Add(new Rectangle { Width = 22, Height = 16, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), RadiusX = 2, RadiusY = 2 });
        Canvas.SetLeft(canvas.Children[^1], 19);
        Canvas.SetTop(canvas.Children[^1], 12);

        // Zwei weiße LEDs (Ra12)
        canvas.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = new SolidColorBrush(Colors.White) });
        Canvas.SetLeft(canvas.Children[^1], 22);
        Canvas.SetTop(canvas.Children[^1], 17);

        canvas.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = new SolidColorBrush(Colors.White) });
        Canvas.SetLeft(canvas.Children[^1], 31);
        Canvas.SetTop(canvas.Children[^1], 17);

        return canvas;
    }

    private static Canvas CreateBlockSignalGraphic(TrackElement element)
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Signalmast
        canvas.Children.Add(new Rectangle { Width = 4, Height = 30, Fill = new SolidColorBrush(Color.FromArgb(255, 44, 44, 44)) });
        Canvas.SetLeft(canvas.Children[^1], 28);
        Canvas.SetTop(canvas.Children[^1], 25);

        // Rundes Gehäuse
        canvas.Children.Add(new Ellipse { Width = 22, Height = 22, Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)), Stroke = new SolidColorBrush(Color.FromArgb(255, 96, 96, 96)), StrokeThickness = 1 });
        Canvas.SetLeft(canvas.Children[^1], 19);
        Canvas.SetTop(canvas.Children[^1], 8);

        // Rote LED
        canvas.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) });
        Canvas.SetLeft(canvas.Children[^1], 25);
        Canvas.SetTop(canvas.Children[^1], 14);

        return canvas;
    }

    /// <summary>
    /// Creates a KsSignalScreen graphic using the actual KsSignalScreen UserControl.
    /// The control uses Viewbox internally, so it scales to any size.
    /// </summary>
    private static UIElement CreateKsSignalScreenGraphic(TrackElement element)
    {
        // Use the actual KsSignalScreen UserControl - it has a Viewbox so it scales automatically
        var signalScreen = new Controls.KsSignalScreen
        {
            Width = 55,
            Height = 55,
            Aspect = element.SignalAspect.ToString()
        };
        
        return signalScreen;
    }

    private static Canvas CreatePlatformGraphic()
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Gleis
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 40, X2 = 60, Y2 = 40,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Bahnsteig (einfaches Rechteck)
        canvas.Children.Add(new Rectangle 
        { 
            Width = 50, 
            Height = 12, 
            Fill = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
            StrokeThickness = 1
        });
        Canvas.SetLeft(canvas.Children[^1], 5);
        Canvas.SetTop(canvas.Children[^1], 15);

        return canvas;
    }

    private static Canvas CreateFeedbackGraphic()
    {
        var canvas = new Canvas { Width = 60, Height = 60 };

        // Gleis
        canvas.Children.Add(new Line
        {
            X1 = 0, Y1 = 30, X2 = 60, Y2 = 30,
            Stroke = TrackBrush,
            StrokeThickness = TrackThickness
        });

        // Rückmelder-Markierung (kleiner Kreis)
        var marker = new Ellipse 
        { 
            Width = 10, 
            Height = 10, 
            Fill = new SolidColorBrush(Color.FromArgb(255, 255, 200, 0)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 200, 150, 0)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(marker, 25);
        Canvas.SetTop(marker, 25);
        canvas.Children.Add(marker);

        return canvas;
    }

    private static Canvas CreatePlaceholderGraphic()
    {
        var canvas = new Canvas { Width = 60, Height = 60 };
        canvas.Children.Add(new Rectangle
        {
            Width = 56,
            Height = 56,
            Fill = new SolidColorBrush(Color.FromArgb(30, 100, 100, 100)),
            Stroke = new SolidColorBrush(Color.FromArgb(60, 150, 150, 150)),
            StrokeThickness = 1
        });
        Canvas.SetLeft(canvas.Children[^1], 2);
        Canvas.SetTop(canvas.Children[^1], 2);
        return canvas;
    }

    #endregion

    #region Properties Panel

    private void UpdatePropertiesPanel()
    {
        if (_selectedElement == null)
        {
            NoSelectionInfo.Visibility = Visibility.Visible;
            ElementPropertiesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        NoSelectionInfo.Visibility = Visibility.Collapsed;
        ElementPropertiesPanel.Visibility = Visibility.Visible;

        // Element-Info aktualisieren
        ElementTypeText.Text = GetElementTypeName(_selectedElement.Type);
        ElementPositionText.Text = $"({_selectedElement.GridX}, {_selectedElement.GridY})";
        ElementIdText.Text = _selectedElement.Id.ToString()[..8];

        // Adresse
        ElementAddressBox.Value = _selectedElement.Address;

        // Signal-Panel Sichtbarkeit
        SignalAspectPanel.Visibility = IsSignalType(_selectedElement.Type) ? Visibility.Visible : Visibility.Collapsed;

        // Update SignalPreview with current aspect
            if (IsSignalType(_selectedElement.Type))
            {
                SignalPreview.Aspect = _selectedElement.SignalAspect.ToString();
            }

            // Weichen-Panel Sichtbarkeit
            SwitchPositionPanel.Visibility = IsSwitchType(_selectedElement.Type) ? Visibility.Visible : Visibility.Collapsed;

            // Aspekt-Buttons aktualisieren
            UpdateAspectButtons();
        
            // Weichen-Buttons aktualisieren
            if (IsSwitchType(_selectedElement.Type))
            {
                UpdateSwitchButtons();
            }
        }

        private void UpdateAspectButtons()
    {
        if (_selectedElement == null) return;

        var accentBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var normalBrush = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];

        AspectHp0Button.Background = _selectedElement.SignalAspect == SignalAspect.Hp0 ? accentBrush : normalBrush;
        AspectKs1Button.Background = _selectedElement.SignalAspect == SignalAspect.Ks1 ? accentBrush : normalBrush;
        AspectKs2Button.Background = _selectedElement.SignalAspect == SignalAspect.Ks2 ? accentBrush : normalBrush;
        AspectKs1BlinkButton.Background = _selectedElement.SignalAspect == SignalAspect.Ks1Blink ? accentBrush : normalBrush;
        AspectKennlichtButton.Background = _selectedElement.SignalAspect == SignalAspect.Kennlicht ? accentBrush : normalBrush;
        AspectDunkelButton.Background = _selectedElement.SignalAspect == SignalAspect.Dunkel ? accentBrush : normalBrush;
        AspectRa12Button.Background = _selectedElement.SignalAspect == SignalAspect.Ra12 ? accentBrush : normalBrush;
        AspectZs1Button.Background = _selectedElement.SignalAspect == SignalAspect.Zs1 ? accentBrush : normalBrush;
        AspectZs7Button.Background = _selectedElement.SignalAspect == SignalAspect.Zs7 ? accentBrush : normalBrush;

        // Update SignalType ComboBox
        SignalTypeComboBox.SelectedIndex = _selectedElement.Type switch
        {
            TrackElementType.SignalKsMain => 0,
            TrackElementType.SignalKsDistant => 1,
            TrackElementType.SignalKsScreen => 2,
            TrackElementType.SignalShunt => 3,
            TrackElementType.SignalBlock => 4,
            _ => 0
        };
    }

    private static string GetElementTypeName(TrackElementType type) => type switch
    {
        TrackElementType.StraightTrack => "Gerades Gleis",
        TrackElementType.Curve45 => "Kurve 45°",
        TrackElementType.Curve90 => "Kurve 90°",
        TrackElementType.BufferStop => "Prellbock",
        TrackElementType.SwitchLeft => "Weiche Links",
        TrackElementType.SwitchRight => "Weiche Rechts",
        TrackElementType.SwitchThreeWay => "Dreiwege-Weiche",
        TrackElementType.SwitchDouble => "Doppelkreuzungsweiche",
        TrackElementType.Crossing => "Kreuzung",
        TrackElementType.SignalKsMain => "Ks-Hauptsignal",
        TrackElementType.SignalKsDistant => "Ks-Vorsignal",
        TrackElementType.SignalShunt => "Rangiersignal",
        TrackElementType.SignalBlock => "Gleissperrsignal",
        TrackElementType.SignalKsScreen => "Ks-Signalschirm",
        TrackElementType.Platform => "Bahnsteig",
        TrackElementType.Feedback => "Rückmelder",
        _ => "Unbekannt"
    };

    private static bool IsSignalType(TrackElementType type) =>
        type is TrackElementType.SignalKsMain or TrackElementType.SignalKsDistant or TrackElementType.SignalShunt or TrackElementType.SignalBlock or TrackElementType.SignalKsScreen;

    private static bool IsSwitchType(TrackElementType type) =>
    type is TrackElementType.SwitchLeft or TrackElementType.SwitchRight or TrackElementType.SwitchThreeWay or TrackElementType.SwitchDouble;

    private static bool IsTrackType(TrackElementType type) =>
        type is TrackElementType.StraightTrack or TrackElementType.Curve45 or TrackElementType.Curve90 or TrackElementType.BufferStop or TrackElementType.Crossing;

    private void UpdateStatistics()
    {
        TrackCountText.Text = _elements.Count(e => IsTrackType(e.Type)).ToString();
        SwitchCountText.Text = _elements.Count(e => IsSwitchType(e.Type)).ToString();
        SignalCountText.Text = _elements.Count(e => IsSignalType(e.Type)).ToString();
    }

    #endregion

    #region Property Panel Event Handlers

    private void OnRotateClicked(object sender, RoutedEventArgs e)
    {
        if (_selectedElement == null || sender is not Button { Tag: string rotationStr }) return;

        if (int.TryParse(rotationStr, out var rotation))
        {
            _selectedElement.Rotation = rotation;
            RefreshElementVisual(_selectedElement);
        }
    }

    private void OnAspectClicked(object sender, PointerRoutedEventArgs e)
    {
        if (_selectedElement == null || sender is not Border { Tag: string aspectStr }) return;

        if (Enum.TryParse<SignalAspect>(aspectStr, out var aspect))
        {
            _blinkingLeds.Clear();
            _selectedElement.SignalAspect = aspect;

            // Update preview signal
            SignalPreview.Aspect = aspectStr;

            // Update canvas element
            RefreshElementVisual(_selectedElement);
            UpdateAspectButtons();
        }
    }

    private void OnSwitchPositionClicked(object sender, RoutedEventArgs e)
    {
        if (_selectedElement == null || sender is not Button { Tag: string positionStr }) return;

        _selectedElement.SwitchPosition = positionStr switch
        {
            "Straight" => SwitchPosition.Straight,
            "DivergingLeft" => SwitchPosition.DivergingLeft,
            "DivergingRight" => SwitchPosition.DivergingRight,
            _ => SwitchPosition.Straight
        };
        
        RefreshElementVisual(_selectedElement);
        UpdateSwitchButtons();
    }

    private void UpdateSwitchButtons()
    {
        if (_selectedElement == null || !IsSwitchType(_selectedElement.Type)) return;

        var isThreeWay = _selectedElement.Type == TrackElementType.SwitchThreeWay;
        var isLeftSwitch = _selectedElement.Type == TrackElementType.SwitchLeft;
        
        // Dritte Spalte nur bei Dreiwege-Weiche oder Links/Rechts-Weiche zeigen
        ThirdSwitchColumn.Width = isThreeWay ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        SwitchRightButton.Visibility = isThreeWay || !isLeftSwitch ? Visibility.Visible : Visibility.Collapsed;
        SwitchLeftButton.Visibility = isThreeWay || isLeftSwitch ? Visibility.Visible : Visibility.Collapsed;

        // Aktiven Button hervorheben
        var accentStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
        var defaultStyle = (Style)Application.Current.Resources["DefaultButtonStyle"];

        SwitchStraightButton.Style = _selectedElement.SwitchPosition == SwitchPosition.Straight ? accentStyle : defaultStyle;
        SwitchLeftButton.Style = _selectedElement.SwitchPosition == SwitchPosition.DivergingLeft ? accentStyle : defaultStyle;
        SwitchRightButton.Style = _selectedElement.SwitchPosition == SwitchPosition.DivergingRight ? accentStyle : defaultStyle;
    }

    private void OnAddressChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_selectedElement == null || double.IsNaN(args.NewValue)) return;
        _selectedElement.Address = (int)args.NewValue;
    }

    private void OnDeleteElementClicked(object sender, RoutedEventArgs e)
    {
        DeleteSelectedElement();
    }

    private void OnSignalTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedElement == null || SignalTypeComboBox.SelectedItem is not ComboBoxItem { Tag: string typeTag })
            return;

        var newType = typeTag switch
        {
            "KsMain" => TrackElementType.SignalKsMain,
            "KsDistant" => TrackElementType.SignalKsDistant,
            "KsMulti" => TrackElementType.SignalKsScreen,
            "Shunt" => TrackElementType.SignalShunt,
            "Block" => TrackElementType.SignalBlock,
            _ => _selectedElement.Type
        };

        if (newType != _selectedElement.Type)
        {
            _selectedElement.Type = newType;
            ElementTypeText.Text = GetElementTypeName(newType);
            RefreshElementVisual(_selectedElement);
            UpdateStatistics();
        }
    }

    #endregion
}

#region Data Models

public enum TrackElementType
{
    StraightTrack,
    Curve45,
    Curve90,
    BufferStop,
    SwitchLeft,
    SwitchRight,
    SwitchThreeWay,
    SwitchDouble,
    Crossing,
    SignalKsMain,
    SignalKsDistant,
    SignalShunt,
    SignalBlock,
    SignalKsScreen,
    Platform,
    Feedback
}

public enum SignalAspect
{
    Hp0,        // Halt (Rot)
    Ks1,        // Fahrt (Grün)
    Ks2,        // Halt erwarten (Gelb)
    Ks1Blink,   // Fahrt mit Geschwindigkeitsbeschränkung (Grün blinkend)
    Kennlicht,  // Signal betrieblich abgeschaltet (Weiß oben)
    Dunkel,     // Signal aus (alle dunkel)
    Ra12,       // Sh1/Ra12 Rangierfahrt erlaubt (2x Weiß diagonal)
    Zs1,        // Ersatzsignal (Weiß blinkend)
    Zs7         // Vorsichtsignal (3x Gelb dreieckig)
}

public enum SwitchPosition
{
    Straight,       // Gerade
    DivergingLeft,  // Abzweig Links (für Dreiwege-Weiche)
    DivergingRight  // Abzweig Rechts
}

public class TrackElement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TrackElementType Type { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int Rotation { get; set; }
    public int Address { get; set; } = 1;
    public SignalAspect SignalAspect { get; set; } = SignalAspect.Hp0;
    public SwitchPosition SwitchPosition { get; set; } = SwitchPosition.Straight;
}

#endregion
