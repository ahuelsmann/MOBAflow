// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.Service;
using Moba.TrackPlan.TrackSystem;
using Moba.WinUI.Rendering;

using System.Collections.ObjectModel;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

namespace Moba.WinUI.View;

public sealed partial class TrackPlanPage : Page
{
    private const double DisplayScale = 0.5;
    private const double SnapDistance = 50.0;
    private const double GridSize = 50.0;
    private const double PortRadius = 8.0;
    private const double SingleRotationHandleLength = 80.0;

    private readonly TrackPlanEditorViewModel _viewModel;
    private readonly ITrackCatalog _catalog = new PikoATrackCatalog();
    private readonly CanvasRenderer _renderer = new();
    private readonly IAttentionControlService _attentionControl = new DefaultAttentionControlService();
    private readonly IPortHoverAffordanceService _portHoverAffordance = new DefaultPortHoverAffordanceService();
    private readonly RulerService _rulerService = new();

    private bool _isPanning;
    private Point _panStart;
    private double _panScrollHorizontalStart;
    private double _panScrollVerticalStart;

    private DateTime _lastClickTime;
    private Point _lastClickPosition;
    private int _clickCount;
    private const double ClickTimeThreshold = 400;
    private const double ClickDistanceThreshold = 10;

    private bool _isRotatingGroup;
    private Point2D _rotationCenter;
    private double _rotationStartAngle;
    private Dictionary<Guid, double> _rotationStartRotations = new();
    private Dictionary<Guid, Point2D> _rotationStartPositions = new();
    private Point2D _dragStartWorldPos;

    private Point _movableRulerDragStart;

    private SolidColorBrush _trackBrush = null!;
    private SolidColorBrush _trackSelectedBrush = null!;
    private SolidColorBrush _trackHoverBrush = null!;
    private SolidColorBrush _portOpenBrush = null!;
    private SolidColorBrush _portConnectedBrush = null!;
    private SolidColorBrush _gridBrush = null!;
    private SolidColorBrush _feedbackBrush = null!;
    private SolidColorBrush _snapPreviewBrush = null!;

    public ObservableCollection<ConstraintViolation> Violations { get; } = [];

    public IReadOnlyList<TrackTemplate> StraightTemplates =>
        _catalog.Straights.ToList();

    public IReadOnlyList<TrackTemplate> CurveTemplates =>
        _catalog.Curves.ToList();

    public IReadOnlyList<TrackTemplate> SwitchTemplates =>
        _catalog.Switches.ToList();

    public TrackPlanPage(TrackPlanEditorViewModel viewModel)
    {
        System.Diagnostics.Debug.WriteLine($"[TrackPlanPage] Constructor: viewModel={viewModel is not null}, GraphCanvas={GraphCanvas}");
        
        _viewModel = viewModel;
        InitializeComponent();

        System.Diagnostics.Debug.WriteLine($"[TrackPlanPage] After InitializeComponent: GraphCanvas={GraphCanvas}");

        Loaded += OnLoaded;
        ActualThemeChanged += OnThemeChanged;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] Started");
        System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] GraphCanvas == null: {GraphCanvas == null}");
        System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] _viewModel == null: {_viewModel == null}");
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] Calling UpdateTheme()...");
            UpdateTheme();
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] UpdateTheme() completed");
            
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] Calling UpdateStatistics()...");
            UpdateStatistics();
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] UpdateStatistics() completed");
            
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] Calling RenderGraph()...");
            RenderGraph();
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] RenderGraph() completed");
            
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] Setting Focus...");
            Focus(FocusState.Programmatic);
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] Focus set");
        }
        catch (NullReferenceException nre)
        {
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] NullReferenceException: {nre.Message}");
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] StackTrace: {nre.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] GraphCanvas: {GraphCanvas?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] _viewModel: {_viewModel?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] _renderer: {_renderer?.GetType().Name ?? "null"}");
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] Exception ({ex.GetType().Name}): {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[TrackPlanPage.OnLoaded] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private void OnThemeChanged(FrameworkElement sender, object args)
    {
        UpdateTheme();
        RenderGraph();
    }

    private void UpdateTheme()
    {
        try
        {
            var resources = Application.Current.Resources;

            Color GetColorResource(string key, Color fallback)
            {
                try
                {
                    if (resources.TryGetValue(key, out var value) && value is Color color)
                        return color;
                }
                catch { }
                return fallback;
            }

            var accentColor = GetColorResource("SystemAccentColor", Color.FromArgb(255, 0, 120, 215));
            var textPrimaryColor = GetColorResource("TextFillColorPrimary", Colors.Black);
            var textSecondaryColor = GetColorResource("TextFillColorSecondary", Colors.Gray);

            _trackBrush = new SolidColorBrush(textPrimaryColor);
            _trackSelectedBrush = new SolidColorBrush(accentColor);

            var hoverColor = accentColor;
            if (hoverColor.A > 0)
                hoverColor = Color.FromArgb(hoverColor.A,
                    (byte)(hoverColor.R * 0.8),
                    (byte)(hoverColor.G * 0.8),
                    (byte)(hoverColor.B * 0.8));
            _trackHoverBrush = new SolidColorBrush(hoverColor);

            var attentionColor = GetColorResource("SystemFillColorCaution", Color.FromArgb(255, 255, 140, 0));
            _portOpenBrush = new SolidColorBrush(attentionColor);

            var successColor = GetColorResource("SystemFillColorPositive", Color.FromArgb(255, 34, 177, 76));
            _portConnectedBrush = new SolidColorBrush(successColor);

            var gridColor = textSecondaryColor;
            _gridBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * 0.25), gridColor.R, gridColor.G, gridColor.B));

            var errorColor = Color.FromArgb(255, 196, 43, 28);
            _feedbackBrush = new SolidColorBrush(errorColor);

            _snapPreviewBrush = new SolidColorBrush(accentColor);
            
            System.Diagnostics.Debug.WriteLine("[UpdateTheme] Theme colors updated successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateTheme] Exception: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateTheme] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Escape:
                _viewModel.ClearSelection();
                RenderGraph();
                UpdatePropertiesPanel();
                StatusText.Text = "Selection cleared";
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.Delete:
                DeleteSelected_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.A when IsCtrlPressed():
                _viewModel.SelectedTrackIds.Clear();
                foreach (var edge in _viewModel.Graph.Edges)
                    _viewModel.SelectedTrackIds.Add(edge.Id);
                RenderGraph();
                StatusText.Text = $"Selected all {_viewModel.SelectedTrackIds.Count} tracks";
                e.Handled = true;
                break;
        }
    }

    private static bool IsCtrlPressed() =>
        Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    private void UpdateStatistics()
    {
        if (NodeCountText == null || CanvasScrollViewer == null)
            return;

        NodeCountText.Text = _viewModel.Graph.Nodes.Count.ToString();
        EdgeCountText.Text = _viewModel.Graph.Edges.Count.ToString();

        var zoomFactor = CanvasScrollViewer.ZoomFactor;
        ZoomLevelText.Text = $"{zoomFactor:P0}";
        ZoomPercentText.Text = $"{zoomFactor:P0}";

        if (Math.Abs(ZoomSlider.Value - zoomFactor) > 0.01)
        {
            ZoomSlider.Value = zoomFactor;
        }
    }

    private void Element_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is FrameworkElement { DataContext: TrackTemplate t })
        {
            args.Data.SetText(t.Id);
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            StatusText.Text = $"Dragging {t.Id}…";
        }
        else args.Cancel = true;
    }

    private async void GraphCanvas_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text))
        {
            _viewModel.CancelGhostPlacement();
            return;
        }

        var deferral = e.GetDeferral();
        try
        {
            var templateId = await e.DataView.GetTextAsync();
            _viewModel.BeginGhostPlacement(templateId);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void GraphCanvas_DragLeave(object sender, DragEventArgs e)
    {
        _viewModel.CancelGhostPlacement();
        RenderGraph();
    }

    private async void GraphCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "";

        if (!e.DataView.Contains(StandardDataFormats.Text))
        {
            _viewModel.CancelGhostPlacement();
            RenderGraph();
            return;
        }

        var deferral = e.GetDeferral();
        try
        {
            var templateId = await e.DataView.GetTextAsync();
            if (_viewModel.GhostPlacement is null || _viewModel.GhostPlacement.TemplateId != templateId)
                _viewModel.BeginGhostPlacement(templateId);

            var pos = e.GetPosition(GraphCanvas);
            var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);

            _viewModel.UpdateGhostPlacement(
                world,
                gridSnap: GridToggle.IsChecked == true,
                gridSize: GridSize,
                snapDistance: SnapDistance,
                snapEnabled: SnapToggle.IsChecked == true);

            RenderGraph();
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async void GraphCanvas_Drop(object sender, DragEventArgs e)
    {

        if (!e.DataView.Contains(StandardDataFormats.Text))
        {
            _viewModel.CancelGhostPlacement();
            return;
        }

        var templateId = await e.DataView.GetTextAsync();
        var pos = e.GetPosition(GraphCanvas);

        var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
        var placement = _viewModel.GhostPlacement is not null
            ? _viewModel.CommitGhostPlacement(SnapToggle.IsChecked == true, SnapDistance)
            : null;

        var finalPosition = placement?.Position ?? world;
        var rotation = placement?.RotationDeg ?? 0.0;
        var preview = placement?.Preview;

        var status = _viewModel.DropTrack(
            templateId,
            finalPosition,
            gridSnap: GridToggle.IsChecked == true && placement is null,
            gridSize: GridSize,
            snapEnabled: SnapToggle.IsChecked == true,
            snapDistance: SnapDistance,
            out _,
            rotationDegOverride: rotation,
            snapPreview: preview);

        StatusText.Text = status;

        UpdateStatistics();
        RenderGraph();
        UpdatePropertiesPanel();
    }

    private void ShowContextMenu(Point pos)
    {
        var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
        var hit = _viewModel.HitTest(world, hitRadius: 40.0);

        var menu = new MenuFlyout();

        if (hit.HasValue)
        {
            if (_viewModel.SelectedTrackId != hit.Value)
            {
                _viewModel.SelectedTrackIds.Clear();
                _viewModel.SelectedTrackIds.Add(hit.Value);
            }

            var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == hit.Value);
            var template = edge != null ? _catalog.GetById(edge.TemplateId) : null;

            menu.Items.Add(CreateMenuItem("Delete", Symbol.Delete, () =>
            {
                DeleteSelected_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(CreateMenuItem("Rotate 15°", Symbol.Rotate, () =>
            {
                if (_viewModel.Rotations.TryGetValue(hit.Value, out var rot))
                {
                    _viewModel.Rotations[hit.Value] = (rot + 15) % 360;
                    RenderGraph();
                }
            }));

            menu.Items.Add(CreateMenuItem("Disconnect", Symbol.Cancel, () =>
            {
                DisconnectSelected_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(new MenuFlyoutSeparator());

            if (template != null)
            {
                foreach (var end in template.Ends)
                {
                    var portId = end.Id;
                    var hasIsolator = _viewModel.HasIsolator(hit.Value, portId);
                    var label = hasIsolator ? $"Remove Isolator ({portId})" : $"Add Isolator ({portId})";

                    menu.Items.Add(CreateMenuItem(label, Symbol.TwoPage, () =>
                    {
                        _viewModel.ToggleIsolator(hit.Value, portId);
                        RenderGraph();
                    }));
                }
            }

            menu.Items.Add(new MenuFlyoutSeparator());

            menu.Items.Add(CreateMenuItem("Create Section", Symbol.Highlight, () =>
            {
                CreateSection_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(CreateMenuItem("Select Connected", Symbol.SelectAll, () =>
            {
                _viewModel.SelectConnectedGroup(hit.Value);
                StatusText.Text = $"Selected {_viewModel.SelectedTrackIds.Count} connected tracks";
                RenderGraph();
                UpdatePropertiesPanel();
            }));
        }
        else if (_viewModel.SelectedTrackIds.Count > 0)
        {
            var count = _viewModel.SelectedTrackIds.Count;
            var countLabel = count == 1 ? "" : $" ({count})";

            menu.Items.Add(CreateMenuItem($"Delete{countLabel}", Symbol.Delete, () =>
            {
                DeleteSelected_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(CreateMenuItem($"Disconnect{countLabel}", Symbol.Cancel, () =>
            {
                DisconnectSelected_Click(this, new RoutedEventArgs());
            }));

            menu.Items.Add(new MenuFlyoutSeparator());

            menu.Items.Add(CreateMenuItem("Clear Selection", Symbol.Clear, () =>
            {
                _viewModel.ClearSelection();
                RenderGraph();
                UpdatePropertiesPanel();
            }));
        }
        else
        {
            menu.Items.Add(CreateMenuItem("Clear Selection", Symbol.Clear, () =>
            {
                _viewModel.ClearSelection();
                RenderGraph();
                UpdatePropertiesPanel();
            }));
        }

        menu.ShowAt(GraphCanvas, pos);
    }

    private static MenuFlyoutItem CreateMenuItem(string text, Symbol icon, Action action)
    {
        var item = new MenuFlyoutItem
        {
            Text = text,
            Icon = new SymbolIcon(icon)
        };
        item.Click += (s, e) => action();
        return item;
    }

    private int DetectClickCount(Point pos)
    {
        var now = DateTime.Now;
        var timeDiff = (now - _lastClickTime).TotalMilliseconds;
        var distDiff = Math.Sqrt(Math.Pow(pos.X - _lastClickPosition.X, 2) + Math.Pow(pos.Y - _lastClickPosition.Y, 2));

        if (timeDiff < ClickTimeThreshold && distDiff < ClickDistanceThreshold)
            _clickCount++;
        else
            _clickCount = 1;

        _lastClickTime = now;
        _lastClickPosition = pos;
        return _clickCount;
    }

    private (Point2D Center, Point2D HandlePosition)? GetRotationHandleInfo()
    {
        if (_viewModel.SelectedTrackIds.Count < 2)
            return null;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var trackId in _viewModel.SelectedTrackIds)
        {
            if (_viewModel.Positions.TryGetValue(trackId, out var pos))
            {
                minX = Math.Min(minX, pos.X - 60);
                minY = Math.Min(minY, pos.Y - 60);
                maxX = Math.Max(maxX, pos.X + 60);
                maxY = Math.Max(maxY, pos.Y + 60);
            }
        }

        if (minX == double.MaxValue)
            return null;

        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;
        var handleY = minY - 40;

        return (new Point2D(centerX, centerY), new Point2D(centerX, handleY));
    }

    private (Point2D Center, Point2D HandlePosition)? GetSingleRotationHandleInfo()
    {
        if (_viewModel.SelectedTrackIds.Count != 1)
            return null;

        var id = _viewModel.SelectedTrackId ?? _viewModel.SelectedTrackIds.First();
        if (!_viewModel.Positions.TryGetValue(id, out var pos))
            return null;

        var handle = new Point2D(pos.X, pos.Y - SingleRotationHandleLength);
        return (pos, handle);
    }

    private void GraphCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(GraphCanvas);
        var pos = point.Position;

        // Check if user clicked on the fixed horizontal ruler (top edge, extended hit area)
        if (_viewModel.ViewState.ShowFixedRulers && pos.Y < 10)
        {
            _viewModel.ViewState.IsDraggingMovableRuler = true;
            var (rulerWorldX, rulerWorldY) = _viewModel.ViewState.MovableRulerPosition ?? (0, 0);
            _movableRulerDragStart = new Point(rulerWorldX * DisplayScale, pos.Y);
            GraphCanvas.CapturePointer(e.Pointer);
            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
            StatusText.Text = "Moving horizontal ruler...";
            e.Handled = true;
            return;
        }

        // Check if user clicked on the fixed vertical ruler (left edge, extended hit area)
        if (_viewModel.ViewState.ShowFixedRulers && pos.X < 10)
        {
            _viewModel.ViewState.IsDraggingMovableRuler = true;
            var (rulerWorldX, rulerWorldY) = _viewModel.ViewState.MovableRulerPosition ?? (0, 0);
            _movableRulerDragStart = new Point(pos.X, rulerWorldY * DisplayScale);
            GraphCanvas.CapturePointer(e.Pointer);
            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
            StatusText.Text = "Moving vertical ruler...";
            e.Handled = true;
            return;
        }

        if (_viewModel.ViewState.ShowMovableRuler && _viewModel.ViewState.MovableRulerPosition is not null)
        {
            GraphCanvas_PointerPressed_MovableRuler(sender, e);
            if (e.Handled) return;
        }

        if (point.Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _panStart = pos;
            _panScrollHorizontalStart = CanvasScrollViewer.HorizontalOffset;
            _panScrollVerticalStart = CanvasScrollViewer.VerticalOffset;
            GraphCanvas.CapturePointer(e.Pointer);
            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeAll);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            var clickCount = DetectClickCount(pos);
            var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
            var isCtrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var isShiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            var handleInfo = GetRotationHandleInfo();
            if (handleInfo.HasValue)
            {
                var (center, handlePos) = handleInfo.Value;
                var distToHandle = Math.Sqrt(Math.Pow(world.X - handlePos.X, 2) + Math.Pow(world.Y - handlePos.Y, 2));

                if (distToHandle < 20)
                {
                    _isRotatingGroup = true;
                    _rotationCenter = center;
                    _rotationStartAngle = Math.Atan2(world.Y - center.Y, world.X - center.X);
                    _rotationStartRotations.Clear();
                    _rotationStartPositions.Clear();

                    foreach (var trackId in _viewModel.SelectedTrackIds)
                    {
                        if (_viewModel.Rotations.TryGetValue(trackId, out var rot))
                            _rotationStartRotations[trackId] = rot;
                        if (_viewModel.Positions.TryGetValue(trackId, out var pos2))
                            _rotationStartPositions[trackId] = pos2;
                    }

                    GraphCanvas.CapturePointer(e.Pointer);
                    StatusText.Text = "Rotating group...";
                    return;
                }
            }

            var singleHandleInfo = GetSingleRotationHandleInfo();
            if (singleHandleInfo.HasValue)
            {
                var (center, handlePos) = singleHandleInfo.Value;
                var distToHandle = Math.Sqrt(Math.Pow(world.X - handlePos.X, 2) + Math.Pow(world.Y - handlePos.Y, 2));

                if (distToHandle < 20)
                {
                    _isRotatingGroup = true;
                    _rotationCenter = center;
                    _rotationStartAngle = Math.Atan2(world.Y - center.Y, world.X - center.X);
                    _rotationStartRotations.Clear();
                    _rotationStartPositions.Clear();

                    foreach (var trackId in _viewModel.SelectedTrackIds)
                    {
                        if (_viewModel.Rotations.TryGetValue(trackId, out var rot))
                            _rotationStartRotations[trackId] = rot;
                        if (_viewModel.Positions.TryGetValue(trackId, out var pos2))
                            _rotationStartPositions[trackId] = pos2;
                    }

                    GraphCanvas.CapturePointer(e.Pointer);
                    StatusText.Text = "Rotating track...";
                    return;
                }
            }

            if (clickCount >= 3)
            {
                var hit = _viewModel.HitTest(world, hitRadius: 40.0);
                if (hit.HasValue)
                {
                    _viewModel.SelectConnectedGroup(hit.Value);
                    StatusText.Text = $"Selected {_viewModel.SelectedTrackIds.Count} connected tracks";
                    RenderGraph();
                    UpdatePropertiesPanel();
                    return;
                }
            }

            if (clickCount == 2)
            {
                var hit = _viewModel.HitTest(world, hitRadius: 40.0);
                if (hit.HasValue)
                {
                    var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == hit.Value);
                    var template = edge != null ? _catalog.GetById(edge.TemplateId) : null;

                    if (template?.Geometry.GeometryKind == TrackGeometryKind.Switch)
                    {
                        _viewModel.ToggleSwitchBranch(hit.Value);
                        StatusText.Text = "Toggled switch branch";
                        RenderGraph();
                        return;
                    }
                }
            }

            if (isShiftPressed && _viewModel.SelectedTrackId.HasValue)
            {
                var hit = _viewModel.HitTest(world, hitRadius: 40.0);
                if (hit.HasValue && hit.Value != _viewModel.SelectedTrackId.Value)
                {
                    _viewModel.SelectPathBetween(_viewModel.SelectedTrackId.Value, hit.Value);
                    StatusText.Text = $"Selected path with {_viewModel.SelectedTrackIds.Count} tracks";
                    RenderGraph();
                    UpdatePropertiesPanel();
                    return;
                }
            }

            _viewModel.PointerDown(world, true, isCtrlPressed);

            if (_viewModel.SelectedTrackIds.Count > 0 && _viewModel.GhostPlacement is null)
            {
                _dragStartWorldPos = world;
                _viewModel.BeginMultiGhostPlacement(_viewModel.SelectedTrackIds.ToList());

                ProtectedCursor = null;

                _ = _attentionControl.DimIrrelevantTracksAsync(
                    _viewModel.SelectedTrackIds.ToList(),
                    dimOpacity: 0.3f);
            }

            GraphCanvas.CapturePointer(e.Pointer);
            RenderGraph();
            UpdatePropertiesPanel();
        }
    }

    private void GraphCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel.ViewState.IsDraggingMovableRuler)
        {
            GraphCanvas_PointerMoved_FixedRulers(sender, e);
            if (e.Handled) return;
        }

        var p = e.GetCurrentPoint(GraphCanvas);
        var pos = p.Position;

        CoordinatesText.Text = $"X: {pos.X:F0}  Y: {pos.Y:F0}";

        if (_isPanning)
        {
            var deltaX = pos.X - _panStart.X;
            var deltaY = pos.Y - _panStart.Y;

            CanvasScrollViewer.ChangeView(
                _panScrollHorizontalStart - deltaX,
                _panScrollVerticalStart - deltaY,
                null,
                disableAnimation: true);

            e.Handled = true;
            return;
        }

        if (_isRotatingGroup)
        {
            var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
            var currentAngle = Math.Atan2(world.Y - _rotationCenter.Y, world.X - _rotationCenter.X);
            var deltaAngle = (currentAngle - _rotationStartAngle) * 180.0 / Math.PI;

            foreach (var trackId in _viewModel.SelectedTrackIds)
            {
                if (_rotationStartRotations.TryGetValue(trackId, out var startRot))
                    _viewModel.Rotations[trackId] = NormalizeDeg(startRot + deltaAngle);

                if (_rotationStartPositions.TryGetValue(trackId, out var startPos))
                {
                    var dx = startPos.X - _rotationCenter.X;
                    var dy = startPos.Y - _rotationCenter.Y;
                    var deltaRad = deltaAngle * Math.PI / 180.0;
                    var newX = _rotationCenter.X + dx * Math.Cos(deltaRad) - dy * Math.Sin(deltaRad);
                    var newY = _rotationCenter.Y + dx * Math.Sin(deltaRad) + dy * Math.Cos(deltaRad);
                    _viewModel.Positions[trackId] = new Point2D(newX, newY);
                }
            }

            StatusText.Text = $"Rotating: {deltaAngle:F1}°";
            RenderGraph();
            return;
        }

        var worldPos = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);

        var hoveredPorts = FindHoveredPorts(worldPos, PortRadius);
        var previousPorts = _portHoverAffordance.HighlightedPorts;

        foreach (var (trackId, portId) in previousPorts)
        {
            if (!hoveredPorts.Contains((trackId, portId)))
            {
                _ = _portHoverAffordance.UnhighlightPortAsync(trackId, portId);
            }
        }

        foreach (var (trackId, portId) in hoveredPorts)
        {
            if (!previousPorts.Contains((trackId, portId)))
            {
                _ = _portHoverAffordance.HighlightPortAsync(trackId, portId);
            }
        }

        var hoveredTracks = FindHoveredTracks(worldPos, hitRadius: 40.0);
        var previousTracks = _portHoverAffordance.HoveredTracks;

        foreach (var trackId in previousTracks)
        {
            if (!hoveredTracks.Contains(trackId))
            {
                _ = _portHoverAffordance.UnhighlightDraggableTrackAsync(trackId);
            }
        }

        foreach (var trackId in hoveredTracks)
        {
            if (!previousTracks.Contains(trackId))
            {
                _ = _portHoverAffordance.HighlightDraggableTrackAsync(trackId);
            }
        }

        if (_viewModel.GhostPlacement is null && _viewModel.MultiGhostPlacement is null)
        {
            _viewModel.PointerMove(worldPos, GridToggle.IsChecked == true, GridSize, SnapDistance);
        }
        else if (_viewModel.MultiGhostPlacement is not null)
        {
            _viewModel.UpdateMultiGhostPlacement(
                new Point2D(
                    worldPos.X - _dragStartWorldPos.X,
                    worldPos.Y - _dragStartWorldPos.Y),
                GridToggle.IsChecked == true,
                GridSize);

            if (_viewModel.MultiGhostPlacement is { } mg)
            {
                foreach (var trackId in mg.TrackIds)
                {
                    if (mg.InitialPositions.TryGetValue(trackId, out var initialPos))
                    {
                        _viewModel.Positions[trackId] = new Point2D(
                            initialPos.X + mg.CurrentOffset.X,
                            initialPos.Y + mg.CurrentOffset.Y);
                    }
                }

                _viewModel.FindAndSetSnapPreviewForMulti(mg.TrackIds, SnapDistance);
            }
        }
        else
        {
            _viewModel.PointerMove(worldPos, GridToggle.IsChecked == true, GridSize, SnapDistance);
        }

        RenderGraph();
    }

    private void GraphCanvas_PointerMoved_FixedRulers(object sender, PointerRoutedEventArgs e)
    {
        if (!_viewModel.ViewState.IsDraggingMovableRuler || !_viewModel.ViewState.ShowFixedRulers)
            return;

        var point = e.GetCurrentPoint(GraphCanvas);
        var pos = point.Position;

        var (currentX, currentY) = _viewModel.ViewState.MovableRulerPosition ?? (0, 0);
        
        // If dragging from horizontal ruler area (Y < 40), move horizontally
        if (_movableRulerDragStart.Y < 40)
        {
            var deltaX = (pos.X - _movableRulerDragStart.X) / DisplayScale;
            _viewModel.ViewState.MovableRulerPosition = (currentX + deltaX, 0);
        }
        // If dragging from vertical ruler area (X < 40), move vertically
        else if (_movableRulerDragStart.X < 40)
        {
            var deltaY = (pos.Y - _movableRulerDragStart.Y) / DisplayScale;
            _viewModel.ViewState.MovableRulerPosition = (0, currentY + deltaY);
        }

        _movableRulerDragStart = pos;
        RenderGraph();
        e.Handled = true;
    }

    private static double NormalizeDeg(double deg)
    {
        while (deg < 0) deg += 360;
        while (deg >= 360) deg -= 360;
        return deg;
    }

    private void GraphCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel.ViewState.IsDraggingMovableRuler)
        {
            GraphCanvas_PointerReleased_MovableRuler(sender, e);
            if (e.Handled) return;
        }

        var point = e.GetCurrentPoint(GraphCanvas);

        if (_isPanning)
        {
            _isPanning = false;
            GraphCanvas.ReleasePointerCaptures();
            ProtectedCursor = null;

            var deltaX = Math.Abs(point.Position.X - _panStart.X);
            var deltaY = Math.Abs(point.Position.Y - _panStart.Y);
            if (deltaX < 5 && deltaY < 5)
                ShowContextMenu(point.Position);

            e.Handled = true;
            return;
        }

        if (_isRotatingGroup)
        {
            _isRotatingGroup = false;
            _rotationStartRotations.Clear();
            _rotationStartPositions.Clear();
            GraphCanvas.ReleasePointerCaptures();
            StatusText.Text = "Rotation complete";
            RenderGraph();
            UpdateStatistics();
            return;
        }

        var pos = point.Position;
        var world = new Point2D(pos.X / DisplayScale, pos.Y / DisplayScale);
        var isCtrlPressed = false;

        try
        {
            var coreWindow = Windows.UI.Core.CoreWindow.GetForCurrentThread();
            if (coreWindow != null)
            {
                isCtrlPressed = (coreWindow.GetKeyState(Windows.System.VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
            }
        }
        catch
        {
            isCtrlPressed = false;
        }

        if (_viewModel.MultiGhostPlacement is not null)
        {
            _viewModel.CommitMultiGhostPlacement();

            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);

            _ = _attentionControl.RestoreAllTracksAsync();

            if (SnapToggle.IsChecked == true && _viewModel.CurrentSnapPreview is { } preview)
            {
                if (_viewModel.SelectedTrackIds.Count > 0)
                {
                    var primaryTrackId = _viewModel.SelectedTrackIds.First();
                    var delta = new Point2D(
                        preview.PreviewPosition.X - _viewModel.Positions[primaryTrackId].X,
                        preview.PreviewPosition.Y - _viewModel.Positions[primaryTrackId].Y);

                    foreach (var trackId in _viewModel.SelectedTrackIds)
                    {
                        _viewModel.Positions[trackId] = new Point2D(
                            _viewModel.Positions[trackId].X + delta.X,
                            _viewModel.Positions[trackId].Y + delta.Y);
                    }

                    if (_viewModel.ConnectionService.TryConnect(
                        preview.MovingEdgeId, preview.MovingPortId,
                        preview.TargetEdgeId, preview.TargetPortId))
                    {
                        var targetEdge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == preview.TargetEdgeId);
                        StatusText.Text = $"✓ Snapped {_viewModel.SelectedTrackIds.Count} tracks to {targetEdge?.TemplateId}";
                      }
                    else
                    {
                        var targetEdge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == preview.TargetEdgeId);
                        StatusText.Text = $"⚠ Snap failed: ports incompatible (from {preview.MovingPortId} to {preview.TargetPortId})";
                        System.Diagnostics.Debug.WriteLine(
                            $"[Snap] TryConnect failed: Edge {preview.MovingEdgeId} port {preview.MovingPortId} → " +
                            $"Edge {preview.TargetEdgeId} port {preview.TargetPortId}");
                    }
                }
            }
            else
            {
                StatusText.Text = $"Placed {_viewModel.SelectedTrackIds.Count} track(s)";
            }

            GraphCanvas.ReleasePointerCaptures();
            RenderGraph();
            UpdateStatistics();
            UpdatePropertiesPanel();
            return;
        }

        var status = _viewModel.PointerUp(
            world,
            snapEnabled: SnapToggle.IsChecked == true,
            snapDistance: SnapDistance,
            gridSnap: GridToggle.IsChecked == true,
            gridSize: GridSize,
            isCtrlPressed: isCtrlPressed);

        StatusText.Text = status;
        GraphCanvas.ReleasePointerCaptures();

        _ = _portHoverAffordance.ClearAllHighlightsAsync();

        RenderGraph();
        UpdateStatistics();
        UpdatePropertiesPanel();
    }

    private void GraphCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        UpdateStatistics();
    }

    private void UpdatePropertiesPanel()
    {
        CreateSectionButton.IsEnabled = _viewModel.SelectedTrackIds.Count > 0;
        ToggleIsolatorButton.IsEnabled = _viewModel.SelectedTrackId.HasValue;

        if (_viewModel.SelectedTrackId.HasValue)
        {
            var section = _viewModel.GetSectionForTrack(_viewModel.SelectedTrackId.Value);
            SectionInfoText.Text = section is not null
                ? $"Section: {section.Name}"
                : "No section assigned";
        }
        else
        {
            SectionInfoText.Text = _viewModel.SelectedTrackIds.Count > 0
                ? $"{_viewModel.SelectedTrackIds.Count} tracks selected"
                : "No section assigned";
        }

        if (_viewModel.SelectedTrackId is null)
        {
            PropertiesPanel.Visibility = Visibility.Collapsed;
            SelectionInfoText.Text = _viewModel.SelectedTrackIds.Count > 0
                ? $"{_viewModel.SelectedTrackIds.Count} tracks selected"
                : "No section assigned";
            return;
        }

        var id = _viewModel.SelectedTrackId.Value;
        var edge = _viewModel.Graph.Edges.First(e => e.Id == id);

        PropertiesPanel.Visibility = Visibility.Visible;
        SelectionInfoText.Text = $"Selected: {edge.TemplateId}";

        TrackIdTextBox.Text = id.ToString()[..8];
        TemplateIdTextBox.Text = edge.TemplateId;

        if (_viewModel.Positions.TryGetValue(id, out var pos))
        {
            PositionXBox.Value = pos.X;
            PositionYBox.Value = pos.Y;
        }

        if (_viewModel.Rotations.TryGetValue(id, out var rot))
            RotationBox.Value = rot;

        FeedbackPointBox.Value = edge.FeedbackPointNumber ?? double.NaN;
    }

    private void PositionBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_viewModel.SelectedTrackId is null || double.IsNaN(args.NewValue))
            return;

        var id = _viewModel.SelectedTrackId.Value;
        var pos = _viewModel.Positions[id];

        if (sender == PositionXBox)
            _viewModel.Positions[id] = new Point2D(args.NewValue, pos.Y);
        else
            _viewModel.Positions[id] = new Point2D(pos.X, args.NewValue);

        RenderGraph();
    }

    private void RotationBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_viewModel.SelectedTrackId is null || double.IsNaN(args.NewValue))
            return;

        _viewModel.Rotations[_viewModel.SelectedTrackId.Value] = args.NewValue;
        RenderGraph();
    }

    private void FeedbackPointBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_viewModel.SelectedTrackId is null)
            return;

        var edge = _viewModel.Graph.Edges.First(e => e.Id == _viewModel.SelectedTrackId);
        edge.FeedbackPointNumber = double.IsNaN(args.NewValue) ? null : (int)args.NewValue;

        RenderGraph();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrackIds.Count == 0)
            return;

        int count = _viewModel.SelectedTrackIds.Count;

        foreach (var id in _viewModel.SelectedTrackIds.ToList())
        {
            _viewModel.RemoveTrack(id);
        }

        _viewModel.ClearSelection();

        StatusText.Text = count == 1
            ? "Track deleted"
            : $"Deleted {count} tracks";

        UpdateStatistics();
        RenderGraph();
        UpdatePropertiesPanel();
    }

    private void DisconnectSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTrackIds.Count == 0)
            return;

        var service = _viewModel.ConnectionService;
        int totalDisconnected = 0;

        foreach (var id in _viewModel.SelectedTrackIds)
        {
            var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == id);
            if (edge is null) continue;

            foreach (var port in edge.Connections.Keys.ToList())
            {
                if (service.IsPortConnected(id, port))
                {
                    service.Disconnect(id, port);
                    totalDisconnected++;
                }
            }
        }

        StatusText.Text = totalDisconnected == 0
            ? "No connections to disconnect"
            : totalDisconnected == 1
                ? "Disconnected 1 port"
                : $"Disconnected {totalDisconnected} ports";

        RenderGraph();
    }

    private void CreateSection_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Sections feature coming soon";
    }

    private void ToggleIsolator_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Isolators feature coming soon";
    }

    private Section? _editingSection;

    private void SectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SectionEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void SectionNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void SectionFunctionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void SectionColorButton_Click(object sender, RoutedEventArgs e)
    {
    }

    private void DeleteSection_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Delete section - feature coming soon";
    }

    private void UpdateSectionsList()
    {
    }

    private void RenderGraph()
    {
        try
        {
            if (GraphCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine("[RenderGraph] ERROR: GraphCanvas is null");
                return;
            }

            GraphCanvas.Children.Clear();

            if (GridToggle?.IsChecked == true)
                RenderGrid();

            if (_renderer != null && _viewModel != null && _catalog != null)
            {
                _renderer.Render(
                    GraphCanvas,
                    _viewModel,
                    _catalog,
                    _trackBrush,
                    _trackSelectedBrush,
                    _trackHoverBrush);
            }

            if (_attentionControl.IsActive)
            {
                ApplyAttentionControlToRenderedTracks();
            }

            if (_viewModel.GhostPlacement is { } ghostPlacement)
            {
                var ghostTemplate = _catalog.GetById(ghostPlacement.TemplateId);
                if (ghostTemplate is not null)
                {
                    var ghostPosition = ghostPlacement.Position;
                    var ghostRotation = ghostPlacement.RotationDeg;

                    if (_viewModel.CurrentSnapPreview is { } preview)
                    {
                        ghostPosition = preview.PreviewPosition;
                        ghostRotation = preview.PreviewRotation;
                    }

                    _renderer.RenderGhostTrack(
                        GraphCanvas,
                        ghostTemplate,
                        ghostPosition,
                        ghostRotation,
                        _snapPreviewBrush);

                    var labelX = ghostPosition.X * DisplayScale;
                    var labelY = ghostPosition.Y * DisplayScale;

                    var gleiscode = new TextBlock
                    {
                        Text = ghostPlacement.TemplateId,
                        FontSize = 14,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = _trackSelectedBrush,
                        TextAlignment = TextAlignment.Center,
                        Opacity = 0.8
                    };

                    Canvas.SetLeft(gleiscode, labelX - 20);
                    Canvas.SetTop(gleiscode, labelY - 8);
                    GraphCanvas.Children.Add(gleiscode);
                }
            }

            if (_viewModel.MultiGhostPlacement is { } multiGhost)
            {
                foreach (var trackId in multiGhost.TrackIds)
                {
                    if (multiGhost.InitialPositions.TryGetValue(trackId, out var initialPos))
                    {
                        var ghostPos = new Point2D(
                            initialPos.X + multiGhost.CurrentOffset.X,
                            initialPos.Y + multiGhost.CurrentOffset.Y);

                        var ghostRot = _viewModel.Rotations.GetValueOrDefault(trackId, 0);

                        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == trackId);
                        if (edge is not null)
                        {
                            var trackTemplate = _catalog.GetById(edge.TemplateId);
                            if (trackTemplate is not null)
                            {
                                var ghostOpacity = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark ? 0.85 : 0.75;
                                _renderer.RenderGhostTrack(
                                    GraphCanvas,
                                    trackTemplate,
                                    ghostPos,
                                    ghostRot,
                                    _snapPreviewBrush,
                                    ghostOpacity);

                                var labelX = ghostPos.X * DisplayScale;
                                var labelY = ghostPos.Y * DisplayScale;

                                var gleiscode = new TextBlock
                                {
                                    Text = edge.TemplateId,
                                    FontSize = 12,
                                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                                    Foreground = new SolidColorBrush(Colors.White),
                                    TextAlignment = TextAlignment.Center,
                                    Opacity = 0.9
                                };

                                Canvas.SetLeft(gleiscode, labelX - 18);
                                Canvas.SetTop(gleiscode, labelY - 7);
                                GraphCanvas.Children.Add(gleiscode);
                            }
                        }
                    }
                }
            }

            var dragPreview = _viewModel.GetCurrentDragPreviewPose();
            if (dragPreview is { } dp)
            {
                var ghostOpacity = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark ? 0.85 : 0.75;
                _renderer.RenderGhostTrack(
                    GraphCanvas,
                    dp.Template,
                    dp.Position,
                    dp.RotationDeg,
                    _snapPreviewBrush,
                    ghostOpacity);
            }

            RenderSingleRotationHandle();
            RenderPorts();
            RenderFeedback();
            RenderIsolators();
            RenderSectionLabels();
            RenderRulers();
            RenderMovableRuler();
            RenderSelectionRectangle();
            RenderSelectionBoundingBox();

            bool isDarkTheme = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark;
            _renderer.RenderTypeIndicators(GraphCanvas, _viewModel, _catalog, isDarkTheme);

            _renderer.RenderPortHoverEffects(GraphCanvas, _portHoverAffordance.HighlightedPorts, _viewModel, _catalog);
            _renderer.RenderTrackHoverEffects(GraphCanvas, _portHoverAffordance.HoveredTracks, _viewModel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RenderGraph] Exception: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[RenderGraph] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private void ApplyAttentionControlToRenderedTracks()
    {
        foreach (var child in GraphCanvas.Children.OfType<FrameworkElement>())
        {
            if (child is not (Polyline or Polygon or Ellipse or Microsoft.UI.Xaml.Shapes.Path))
                continue;

            if (child.Opacity > 0.3)
            {
                child.Opacity = 0.3f;
            }
        }
    }

    private void RenderSingleRotationHandle()
    {
        if (_viewModel.SelectedTrackIds.Count != 1 || !_viewModel.SelectedTrackId.HasValue)
            return;

        var id = _viewModel.SelectedTrackId.Value;
        if (!_viewModel.Positions.TryGetValue(id, out var pos))
            return;

        var startX = pos.X * DisplayScale;
        var startY = pos.Y * DisplayScale;
        var endY = startY - SingleRotationHandleLength * DisplayScale;

        var line = new Line
        {
            X1 = startX,
            Y1 = startY,
            X2 = startX,
            Y2 = endY,
            Stroke = _trackSelectedBrush,
            StrokeThickness = 2
        };

        var circle = new Ellipse
        {
            Width = 18,
            Height = 18,
            Fill = _trackSelectedBrush,
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };

        Canvas.SetLeft(circle, startX - 9);
        Canvas.SetTop(circle, endY - 9);

        GraphCanvas.Children.Add(line);
        GraphCanvas.Children.Add(circle);
    }

    private void RenderGrid()
    {
        double w = GraphCanvas.Width;
        double h = GraphCanvas.Height;

        for (double x = 0; x <= w; x += GridSize)
        {
            GraphCanvas.Children.Add(new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = h,
                Stroke = _gridBrush,
                StrokeThickness = 1
            });
        }

        for (double y = 0; y <= h; y += GridSize)
        {
            GraphCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = w,
                Y2 = y,
                Stroke = _gridBrush,
                StrokeThickness = 1
            });
        }
    }

    private void RenderPorts()
    {
        var service = _viewModel.ConnectionService;
        bool enableHoverAnimation = _viewModel.ViewState.ShowPortHoverAnimation;

        foreach (var edge in _viewModel.Graph.Edges)
        {
            if (!_viewModel.Positions.TryGetValue(edge.Id, out var pos))
                continue;

            var rot = _viewModel.Rotations.GetValueOrDefault(edge.Id, 0);
            var template = _catalog.GetById(edge.TemplateId);
            if (template is null)
                continue;

            foreach (var end in template.Ends)
            {
                var offset = CalculatePortOffset(template, end.Id, rot);
                var x = (pos.X + offset.X) * DisplayScale;
                var y = (pos.Y + offset.Y) * DisplayScale;

                bool connected = service.IsPortConnected(edge.Id, end.Id);

                if (enableHoverAnimation)
                {
                    var shadow = new Ellipse
                    {
                        Width = PortRadius * 3,
                        Height = PortRadius * 3,
                        Fill = connected
                            ? new SolidColorBrush(Colors.Yellow) { Opacity = 0.0 }
                            : new SolidColorBrush(Colors.Orange) { Opacity = 0.0 },
                        Opacity = 0.0
                    };

                    Canvas.SetLeft(shadow, x - PortRadius * 1.5);
                    Canvas.SetTop(shadow, y - PortRadius * 1.5);
                    GraphCanvas.Children.Add(shadow);
                }

                var ellipse = new Ellipse
                {
                    Width = PortRadius * 2,
                    Height = PortRadius * 2,
                    Fill = connected ? _portConnectedBrush : _portOpenBrush,
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2,
                    RenderTransform = enableHoverAnimation ? new ScaleTransform { CenterX = PortRadius, CenterY = PortRadius } : null
                };

                Canvas.SetLeft(ellipse, x - PortRadius);
                Canvas.SetTop(ellipse, y - PortRadius);
                
                if (enableHoverAnimation)
                {
                    ellipse.Tag = (edge.Id, end.Id);
                    ellipse.PointerEntered += Port_PointerEntered;
                    ellipse.PointerExited += Port_PointerExited;
                }

                GraphCanvas.Children.Add(ellipse);
            }
        }
    }

    private void Port_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Ellipse ellipse)
            return;

        var scaleTransform = (ScaleTransform)ellipse.RenderTransform;
        var scaleStoryboard = new Storyboard();

        var scaleAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 1.3,
            Duration = new TimeSpan(0, 0, 0, 0, 150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(scaleAnimation, ellipse);
        Storyboard.SetTargetProperty(scaleAnimation, "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");
        scaleStoryboard.Children.Add(scaleAnimation);

        var scaleAnimationY = new DoubleAnimation
        {
            From = 1.0,
            To = 1.3,
            Duration = new TimeSpan(0, 0, 0, 0, 150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(scaleAnimationY, ellipse);
        Storyboard.SetTargetProperty(scaleAnimationY, "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");
        scaleStoryboard.Children.Add(scaleAnimationY);

        scaleStoryboard.Begin();

        var index = GraphCanvas.Children.IndexOf(ellipse);
        if (index > 0 && GraphCanvas.Children[index - 1] is Ellipse shadowEllipse)
        {
            var glowStoryboard = new Storyboard();
            var glowAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 0.4,
                Duration = new TimeSpan(0, 0, 0, 0, 150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(glowAnimation, shadowEllipse);
            Storyboard.SetTargetProperty(glowAnimation, "Opacity");
            glowStoryboard.Children.Add(glowAnimation);

            glowStoryboard.Begin();
        }

        e.Handled = true;
    }

    private void Port_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Ellipse ellipse)
            return;

        var scaleStoryboard = new Storyboard();

        var scaleAnimation = new DoubleAnimation
        {
            From = 1.3,
            To = 1.0,
            Duration = new TimeSpan(0, 0, 0, 0, 150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        Storyboard.SetTarget(scaleAnimation, ellipse);
        Storyboard.SetTargetProperty(scaleAnimation, "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");
        scaleStoryboard.Children.Add(scaleAnimation);

        var scaleAnimationY = new DoubleAnimation
        {
            From = 1.3,
            To = 1.0,
            Duration = new TimeSpan(0, 0, 0, 0, 150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        Storyboard.SetTarget(scaleAnimationY, ellipse);
        Storyboard.SetTargetProperty(scaleAnimationY, "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");
        scaleStoryboard.Children.Add(scaleAnimationY);

        scaleStoryboard.Begin();

        var index = GraphCanvas.Children.IndexOf(ellipse);
        if (index > 0 && GraphCanvas.Children[index - 1] is Ellipse shadowEllipse)
        {
            var glowStoryboard = new Storyboard();
            var glowAnimation = new DoubleAnimation
            {
                From = 0.4,
                To = 0.0,
                Duration = new TimeSpan(0, 0, 0, 0, 150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(glowAnimation, shadowEllipse);
            Storyboard.SetTargetProperty(glowAnimation, "Opacity");
            glowStoryboard.Children.Add(glowAnimation);

            glowStoryboard.Begin();
        }

        e.Handled = true;
    }

    private void RenderFeedback()
    {
        foreach (var edge in _viewModel.Graph.Edges)
        {
            if (!edge.FeedbackPointNumber.HasValue)
                continue;

            var pos = _viewModel.Positions[edge.Id];
            var px = pos.X * DisplayScale;
            var py = pos.Y * DisplayScale;

            var dot = new Ellipse { Width = 16, Height = 16, Fill = _feedbackBrush };

            Canvas.SetLeft(dot, px - 8);
            Canvas.SetTop(dot, py + 20);
            GraphCanvas.Children.Add(dot);

            var label = new TextBlock
            {
                Text = edge.FeedbackPointNumber.Value.ToString(),
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            Canvas.SetLeft(label, px - 4);
            Canvas.SetTop(label, py + 22);
            GraphCanvas.Children.Add(label);
        }
    }

    private void RenderIsolators()
    {
    }

    private void RenderSectionLabels()
    {
    }

    private void RenderSelectionBoundingBox()
    {
        if (_viewModel.SelectedTrackIds.Count < 2)
            return;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var trackId in _viewModel.SelectedTrackIds)
        {
            if (_viewModel.Positions.TryGetValue(trackId, out var pos))
            {
                var px = pos.X * DisplayScale;
                var py = pos.Y * DisplayScale;
                minX = Math.Min(minX, px - 30);
                minY = Math.Min(minY, py - 30);
                maxX = Math.Max(maxX, px + 30);
                maxY = Math.Max(maxY, py + 30);
            }
        }

        if (minX == double.MaxValue) return;

        var rect = new Rectangle
        {
            Width = maxX - minX,
            Height = maxY - minY,
            Stroke = new SolidColorBrush(Colors.LimeGreen),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 5, 3 },
            Fill = null
        };
        Canvas.SetLeft(rect, minX);
        Canvas.SetTop(rect, minY);
        GraphCanvas.Children.Add(rect);

        var handleX = (minX + maxX) / 2;
        var handleY = minY - 20;

        var handle = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(Colors.LimeGreen),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };

        Canvas.SetLeft(handle, handleX - 6);
        Canvas.SetTop(handle, handleY - 6);
        GraphCanvas.Children.Add(handle);

        var handleLine = new Line
        {
            X1 = handleX,
            Y1 = handleY,
            X2 = handleX,
            Y2 = minY,
            Stroke = new SolidColorBrush(Colors.LimeGreen),
            StrokeThickness = 1
        };
        GraphCanvas.Children.Add(handleLine);
    }

    private void RenderSelectionRectangle()
    {
        var rect = _viewModel.SelectionRectangle;
        if (rect is null)
            return;

        var (start, end) = rect.Value;

        var x1 = start.X * DisplayScale;
        var y1 = start.Y * DisplayScale;
        var x2 = end.X * DisplayScale;
        var y2 = end.Y * DisplayScale;

        var left = Math.Min(x1, x2);
        var top = Math.Min(y1, y2);
        var width = Math.Abs(x2 - x1);
        var height = Math.Abs(y2 - y1);

        var selectionRect = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = _trackSelectedBrush,
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(30, 0, 120, 215))
        };

        Canvas.SetLeft(selectionRect, left);
        Canvas.SetTop(selectionRect, top);
        GraphCanvas.Children.Add(selectionRect);
    }

    private IReadOnlySet<(System.Guid TrackId, string PortId)> FindHoveredPorts(Point2D worldPos, double searchRadius)
    {
        var hoveredPorts = new HashSet<(System.Guid, string)>();

        foreach (var edge in _viewModel.Graph.Edges)
        {
            var template = _catalog.GetById(edge.TemplateId);
            if (template is null || !_viewModel.Positions.TryGetValue(edge.Id, out var trackPos))
                continue;

            var rotation = _viewModel.Rotations.GetValueOrDefault(edge.Id, 0.0);

            foreach (var end in template.Ends)
            {
                var portOffset = CalculatePortOffset(template, end.Id, rotation);
                var portWorldPos = new Point2D(trackPos.X + portOffset.X, trackPos.Y + portOffset.Y);

                var dist = Math.Sqrt(
                    Math.Pow(worldPos.X - portWorldPos.X, 2) +
                    Math.Pow(worldPos.Y - portWorldPos.Y, 2));

                if (dist < searchRadius)
                {
                    hoveredPorts.Add((edge.Id, end.Id));
                }
            }
        }

        return hoveredPorts;
    }

    private IReadOnlySet<System.Guid> FindHoveredTracks(Point2D worldPos, double hitRadius)
    {
        var hoveredTracks = new HashSet<System.Guid>();

        foreach (var edge in _viewModel.Graph.Edges)
        {
            if (_viewModel.Positions.TryGetValue(edge.Id, out var trackPos))
            {
                var dist = Math.Sqrt(
                    Math.Pow(worldPos.X - trackPos.X, 2) +
                    Math.Pow(worldPos.Y - trackPos.Y, 2));

                if (dist < hitRadius)
                {
                    hoveredTracks.Add(edge.Id);
                }
            }
        }

        return hoveredTracks;
    }

    private static Point2D CalculatePortOffset(TrackTemplate template, string portId, double rotationDeg)
    {
        var spec = template.Geometry;
        double rotRad = rotationDeg * Math.PI / 180.0;

        static Point2D Rot(Point2D p, double r) =>
            new(p.X * Math.Cos(r) - p.Y * Math.Sin(r),
                p.X * Math.Sin(r) + p.Y * Math.Cos(r));

        if (spec.GeometryKind == TrackGeometryKind.Straight)
        {
            double length = spec.LengthMm!.Value;
            var local = portId == "A" ? new Point2D(0, 0) : new Point2D(length, 0);
            return Rot(local, rotRad);
        }

        if (spec.GeometryKind == TrackGeometryKind.Curve)
        {
            double radius = spec.RadiusMm!.Value;
            double sweepRad = spec.AngleDeg!.Value * Math.PI / 180.0;

            if (portId == "A")
                return Rot(new Point2D(0, 0), rotRad);

            var endLocal = new Point2D(
                radius * Math.Sin(sweepRad),
                radius - radius * Math.Cos(sweepRad));

            return Rot(endLocal, rotRad);
        }

        if (spec.GeometryKind == TrackGeometryKind.Switch)
        {
            double length = spec.LengthMm!.Value;
            double radius = spec.RadiusMm!.Value;
            double sweepRad = spec.AngleDeg!.Value * Math.PI / 180.0;
            double junction = spec.JunctionOffsetMm ?? (length / 2.0);

            if (portId == "A")
                return Rot(new Point2D(0, 0), rotRad);

            if (portId == "B")
                return Rot(new Point2D(length, 0), rotRad);

            if (portId == "C")
            {
                var j = new Point2D(junction, 0);
                var center = new Point2D(j.X, j.Y + radius);
                double startAngle = Math.Atan2(j.Y - center.Y, j.X - center.X);

                var endLocal = new Point2D(
                    center.X + radius * Math.Cos(startAngle + sweepRad),
                    center.Y + radius * Math.Sin(startAngle + sweepRad));

                var rel = new Point2D(endLocal.X - 0.0, endLocal.Y - 0.0);
                return Rot(rel, rotRad);
            }
        }

        return new Point2D(0, 0);
    }

    private void ZoomFit_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Graph.Nodes.Count == 0)
        {
            StatusText.Text = "No tracks to fit";
            return;
        }

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var pos in _viewModel.Positions.Values)
        {
            minX = Math.Min(minX, pos.X);
            minY = Math.Min(minY, pos.Y);
            maxX = Math.Max(maxX, pos.X);
            maxY = Math.Max(maxY, pos.Y);
        }

        double padding = 50;
        double centerX = (minX + maxX) / 2 * DisplayScale;
        double centerY = (minY + maxY) / 2 * DisplayScale;
        
        CanvasScrollViewer.ChangeView(centerX, centerY, null, disableAnimation: false);
        StatusText.Text = "Zoomed to fit all tracks";
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        CanvasScrollViewer.ChangeView(0, 0, 1.0f, disableAnimation: false);
        StatusText.Text = "Zoom reset";
    }

    private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (CanvasScrollViewer is null) return;
        
        CanvasScrollViewer.ChangeView(null, null, (float)e.NewValue, disableAnimation: true);
        
        if (ZoomPercentText is not null)
            ZoomPercentText.Text = $"{e.NewValue:P0}";
        if (ZoomLevelText is not null)
            ZoomLevelText.Text = $"{e.NewValue:P0}";
        if (StatusText is not null)
            StatusText.Text = $"Zoom: {e.NewValue:P0}";
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        if (CanvasScrollViewer is null || ZoomSlider is null) return;
        
        const double step = 0.1;
        var newZoom = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + step);
        ZoomSlider.Value = newZoom;
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        if (CanvasScrollViewer is null || ZoomSlider is null) return;
        
        const double step = 0.1;
        var newZoom = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - step);
        ZoomSlider.Value = newZoom;
    }

    private void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Validation not yet implemented";
    }

    private void RenderRulers()
    {
        if (_viewModel.ViewState.ShowFixedRulers == false)
            return;

        double currentZoom = ZoomSlider?.Value ?? 1.0;
        bool isDarkTheme = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark;
        
        double scrollOffsetX = CanvasScrollViewer.HorizontalOffset;
        double scrollOffsetY = CanvasScrollViewer.VerticalOffset;
        
        double viewportWidth = CanvasScrollViewer.ViewportWidth;
        double viewportHeight = CanvasScrollViewer.ViewportHeight;

        RenderHorizontalRuler(scrollOffsetX, scrollOffsetX + viewportWidth, viewportWidth, currentZoom, isDarkTheme);
        RenderVerticalRuler(scrollOffsetY, scrollOffsetY + viewportHeight, viewportHeight, currentZoom, isDarkTheme);
    }

    private void RenderHorizontalRuler(
        double viewportStartX,
        double viewportEndX,
        double viewportWidth,
        double zoomLevel,
        bool isDarkTheme)
    {
        const double RulerHeight = 40;  // Increased from 24 to 40 for better visibility
        
        var rulerGeometry = _rulerService.CreateHorizontalRuler(
            viewportStartX,
            viewportEndX,
            viewportWidth,
            zoomLevel,
            DisplayScale);

        var rulerBrush = isDarkTheme
            ? new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);

        var rulerBackground = new Rectangle
        {
            Width = GraphCanvas.Width,
            Height = RulerHeight,
            Fill = isDarkTheme
                ? new SolidColorBrush(Color.FromArgb(255, 40, 40, 40))
                : new SolidColorBrush(Color.FromArgb(255, 245, 245, 245))
        };
        Canvas.SetLeft(rulerBackground, 0);
        Canvas.SetTop(rulerBackground, -RulerHeight);
        GraphCanvas.Children.Add(rulerBackground);

        foreach (var tick in rulerGeometry.Ticks)
        {
            var tickLine = new Line
            {
                X1 = tick.Position,
                Y1 = -RulerHeight,
                X2 = tick.Position,
                Y2 = -RulerHeight + tick.Height,
                Stroke = rulerBrush,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(tickLine);

            if (tick.Label is not null)
            {
                var label = new TextBlock
                {
                    Text = tick.Label,
                    FontSize = 11,
                    Foreground = rulerBrush,
                    TextAlignment = TextAlignment.Center,
                    Opacity = 0.8
                };
                Canvas.SetLeft(label, tick.Position - 15);
                Canvas.SetTop(label, -RulerHeight + 20);
                GraphCanvas.Children.Add(label);
            }
        }
    }

    private void RenderVerticalRuler(
        double viewportStartY,
        double viewportEndY,
        double viewportHeight,
        double zoomLevel,
        bool isDarkTheme)
    {
        const double RulerWidth = 40;  // Increased from 24 to 40 for better visibility
        
        var rulerGeometry = _rulerService.CreateVerticalRuler(
            viewportStartY,
            viewportEndY,
            viewportHeight,
            zoomLevel,
            DisplayScale);

        var rulerBrush = isDarkTheme
            ? new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);

        var rulerBackground = new Rectangle
        {
            Width = RulerWidth,
            Height = GraphCanvas.Height,
            Fill = isDarkTheme
                ? new SolidColorBrush(Color.FromArgb(255, 40, 40, 40))
                : new SolidColorBrush(Color.FromArgb(255, 245, 245, 245))
        };
        Canvas.SetLeft(rulerBackground, -RulerWidth);
        Canvas.SetTop(rulerBackground, 0);
        GraphCanvas.Children.Add(rulerBackground);

        foreach (var tick in rulerGeometry.Ticks)
        {
            var tickLine = new Line
            {
                X1 = -RulerWidth,
                Y1 = tick.Position,
                X2 = -RulerWidth + tick.Height,
                Y2 = tick.Position,
                Stroke = rulerBrush,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(tickLine);
        }
    }

    private void RulerToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.ViewState.ShowFixedRulers = RulerToggle.IsChecked == true;
        RenderGraph();
    }

    private void RenderMovableRuler()
    {
        if (_viewModel.ViewState.ShowMovableRuler == false || _viewModel.ViewState.MovableRulerPosition is null)
            return;

        double currentZoom = ZoomSlider?.Value ?? 1.0;
        bool isDarkTheme = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark;
        var opacity = _viewModel.ViewState.MovableRulerOpacity;
        var rotation = _viewModel.ViewState.MovableRulerRotationDeg;
        var (rulerWorldX, rulerWorldY) = _viewModel.ViewState.MovableRulerPosition.Value;

        var rulerDisplayX = rulerWorldX * DisplayScale;
        var rulerDisplayY = rulerWorldY * DisplayScale;

        var rotRad = rotation * Math.PI / 180.0;
        var cosR = Math.Cos(rotRad);
        var sinR = Math.Sin(rotRad);

        var rulerBrush = isDarkTheme
            ? new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);
        rulerBrush.Opacity = opacity;

        RenderMovableHorizontalRuler(rulerDisplayX, rulerDisplayY, currentZoom, isDarkTheme, opacity, cosR, sinR);
        RenderMovableVerticalRuler(rulerDisplayX, rulerDisplayY, currentZoom, isDarkTheme, opacity, cosR, sinR);
    }

    private void RenderMovableHorizontalRuler(
        double originX,
        double originY,
        double zoomLevel,
        bool isDarkTheme,
        double opacity,
        double cosR,
        double sinR)
    {
        const double rulerLengthMm = 1000.0;
        const double rulerWidthMm = 50.0;

        var rulerGeometry = _rulerService.CreateHorizontalRuler(
            0,
            rulerLengthMm,
            rulerLengthMm * DisplayScale,
            zoomLevel,
            DisplayScale);

        var tickBrush = isDarkTheme
            ? new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);
        tickBrush.Opacity = opacity;

        var barLength = rulerLengthMm * DisplayScale;
        var barWidth = rulerWidthMm * DisplayScale;

        var barTopLeft = RotatePoint(0, 0, cosR, sinR, originX, originY);
        var barTopRight = RotatePoint(barLength, 0, cosR, sinR, originX, originY);
        var barBottomRight = RotatePoint(barLength, barWidth, cosR, sinR, originX, originY);
        var barBottomLeft = RotatePoint(0, barWidth, cosR, sinR, originX, originY);

        var barPoly = new Polygon
        {
            Points = new PointCollection
            {
                new Windows.Foundation.Point(barTopLeft.X, barTopLeft.Y),
                new Windows.Foundation.Point(barTopRight.X, barTopRight.Y),
                new Windows.Foundation.Point(barBottomRight.X, barBottomRight.Y),
                new Windows.Foundation.Point(barBottomLeft.X, barBottomLeft.Y)
            },
            Fill = isDarkTheme
                ? new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), 40, 40, 40))
                : new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), 245, 245, 245)),
            Opacity = opacity
        };
        GraphCanvas.Children.Add(barPoly);

        foreach (var tick in rulerGeometry.Ticks)
        {
            var tickStart = RotatePoint(tick.Position, 0, cosR, sinR, originX, originY);
            var tickEnd = RotatePoint(tick.Position, tick.Height, cosR, sinR, originX, originY);

            var tickLine = new Line
            {
                X1 = tickStart.X,
                Y1 = tickStart.Y,
                X2 = tickEnd.X,
                Y2 = tickEnd.Y,
                Stroke = tickBrush,
                StrokeThickness = 1,
                Opacity = opacity
            };
            GraphCanvas.Children.Add(tickLine);

            if (tick.Label is not null)
            {
                var label = new TextBlock
                {
                    Text = tick.Label,
                    FontSize = 11,
                    Foreground = tickBrush,
                    TextAlignment = TextAlignment.Center,
                    Opacity = opacity
                };
                Canvas.SetLeft(label, tick.Position - 15);
                Canvas.SetTop(label, -30);
                GraphCanvas.Children.Add(label);
            }
        }
    }

    private void RenderMovableVerticalRuler(
        double originX,
        double originY,
        double zoomLevel,
        bool isDarkTheme,
        double opacity,
        double cosR,
        double sinR)
    {
        const double rulerLengthMm = 1000.0;
        const double rulerWidthMm = 50.0;

        var rulerGeometry = _rulerService.CreateVerticalRuler(
            0,
            rulerLengthMm,
            rulerLengthMm * DisplayScale,
            zoomLevel,
            DisplayScale);

        var tickBrush = isDarkTheme
            ? new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);
        tickBrush.Opacity = opacity;

        var barLength = rulerLengthMm * DisplayScale;
        var barWidth = rulerWidthMm * DisplayScale;

        var offsetCosR = Math.Cos((Math.PI / 2.0) + Math.Atan2(sinR, cosR));
        var offsetSinR = Math.Sin((Math.PI / 2.0) + Math.Atan2(sinR, cosR));

        var barTopLeft = RotatePoint(0, 0, offsetCosR, offsetSinR, originX, originY);
        var barTopRight = RotatePoint(0, barWidth, offsetCosR, offsetSinR, originX, originY);
        var barBottomRight = RotatePoint(barLength, barWidth, offsetCosR, offsetSinR, originX, originY);
        var barBottomLeft = RotatePoint(barLength, 0, offsetCosR, offsetSinR, originX, originY);

        var barPoly = new Polygon
        {
            Points = new PointCollection
            {
                new Windows.Foundation.Point(barTopLeft.X, barTopLeft.Y),
                new Windows.Foundation.Point(barTopRight.X, barTopRight.Y),
                new Windows.Foundation.Point(barBottomRight.X, barBottomRight.Y),
                new Windows.Foundation.Point(barBottomLeft.X, barBottomLeft.Y)
            },
            Fill = isDarkTheme
                ? new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), 40, 40, 40))
                : new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), 245, 245, 245)),
            Opacity = opacity
        };
        GraphCanvas.Children.Add(barPoly);

        foreach (var tick in rulerGeometry.Ticks)
        {
            var tickStart = RotatePoint(0, tick.Position, offsetCosR, offsetSinR, originX, originY);
            var tickEnd = RotatePoint(tick.Height, tick.Position, offsetCosR, offsetSinR, originX, originY);

            var tickLine = new Line
            {
                X1 = tickStart.X,
                Y1 = tickStart.Y,
                X2 = tickEnd.X,
                Y2 = tickEnd.Y,
                Stroke = tickBrush,
                StrokeThickness = 1,
                Opacity = opacity
            };
            GraphCanvas.Children.Add(tickLine);
        }
    }

    private static (double X, double Y) RotatePoint(double x, double y, double cosR, double sinR, double originX, double originY)
    {
        var rotatedX = x * cosR - y * sinR;
        var rotatedY = x * sinR + y * cosR;
        return (originX + rotatedX, originY + rotatedY);
    }

    private void GraphCanvas_PointerPressed_MovableRuler(object sender, PointerRoutedEventArgs e)
    {
        if (!_viewModel.ViewState.ShowMovableRuler || _viewModel.ViewState.MovableRulerPosition is null)
            return;

        var point = e.GetCurrentPoint(GraphCanvas);
        var pos = point.Position;

        var (rulerX, rulerY) = _viewModel.ViewState.MovableRulerPosition.Value;
        var rulerDisplayX = rulerX * DisplayScale;
        var rulerDisplayY = rulerY * DisplayScale;

        var dist = Math.Sqrt(Math.Pow(pos.X - rulerDisplayX, 2) + Math.Pow(pos.Y - rulerDisplayY, 2));
        if (dist > 100)
            return;

        _viewModel.ViewState.IsDraggingMovableRuler = true;
        _movableRulerDragStart = new Point(rulerDisplayX, rulerDisplayY);
        GraphCanvas.CapturePointer(e.Pointer);
        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
        e.Handled = true;
    }

    private void GraphCanvas_PointerMoved_MovableRuler(object sender, PointerRoutedEventArgs e)
    {
        if (!_viewModel.ViewState.IsDraggingMovableRuler || _viewModel.ViewState.MovableRulerPosition is null)
            return;

        var point = e.GetCurrentPoint(GraphCanvas);
        var pos = point.Position;

        var deltaX = (pos.X - _movableRulerDragStart.X) / DisplayScale;
        var deltaY = (pos.Y - _movableRulerDragStart.Y) / DisplayScale;

        var (currentX, currentY) = _viewModel.ViewState.MovableRulerPosition.Value;
        _viewModel.ViewState.MovableRulerPosition = (currentX + deltaX, currentY + deltaY);

        _movableRulerDragStart = new Point(pos.X, pos.Y);
        RenderGraph();
        e.Handled = true;
    }

    private void GraphCanvas_PointerReleased_MovableRuler(object sender, PointerRoutedEventArgs e)
    {
        if (!_viewModel.ViewState.IsDraggingMovableRuler)
            return;

        _viewModel.ViewState.IsDraggingMovableRuler = false;
        GraphCanvas.ReleasePointerCaptures();
        ProtectedCursor = null;
        StatusText.Text = "Movable ruler repositioned";
        RenderGraph();
        e.Handled = true;
    }

    private void RotateMovableRulerLeft_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.ViewState.ShowMovableRuler)
            return;

        var newRotation = _viewModel.ViewState.MovableRulerRotationDeg - 15.0;
        _viewModel.ViewState.MovableRulerRotationDeg = NormalizeDeg(newRotation);
        StatusText.Text = $"Ruler rotated: {_viewModel.ViewState.MovableRulerRotationDeg:F0}°";
        RenderGraph();
    }

    private void RotateMovableRulerRight_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.ViewState.ShowMovableRuler)
            return;

        var newRotation = _viewModel.ViewState.MovableRulerRotationDeg + 15.0;
        _viewModel.ViewState.MovableRulerRotationDeg = NormalizeDeg(newRotation);
        StatusText.Text = $"Ruler rotated: {_viewModel.ViewState.MovableRulerRotationDeg:F0}°";
        RenderGraph();
    }

    private void ResetMovableRuler_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.ViewState.ShowMovableRuler)
            return;

        _viewModel.ViewState.MovableRulerPosition = (0, 0);
        _viewModel.ViewState.MovableRulerRotationDeg = 0;
        StatusText.Text = "Movable ruler reset";
        RenderGraph();
    }

    private void MovableRulerToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.ViewState.ShowMovableRuler = MovableRulerToggle?.IsChecked == true;
        
        if (_viewModel.ViewState.ShowMovableRuler && _viewModel.ViewState.MovableRulerPosition is null)
        {
            _viewModel.ViewState.MovableRulerPosition = (0, 0);
            _viewModel.ViewState.MovableRulerRotationDeg = 0;
            _viewModel.ViewState.MovableRulerOpacity = 0.8;
        }
        
        RenderGraph();
    }

    private void MovableRulerOpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_viewModel?.ViewState != null)
        {
            _viewModel.ViewState.MovableRulerOpacity = e.NewValue;
            RenderGraph();
        }
    }

    private void PortHoverAnimationToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.ViewState.ShowPortHoverAnimation = PortHoverAnimationToggle?.IsChecked == true;
        RenderGraph();
    }

    private void StraightIconCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas && canvas.Tag is TrackTemplate template)
        {
            _renderer.RenderToolboxIcon(canvas, template, new SolidColorBrush(Colors.CornflowerBlue));
        }
    }

    private void CurveIconCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas && canvas.Tag is TrackTemplate template)
        {
            _renderer.RenderToolboxIcon(canvas, template, new SolidColorBrush(Colors.CornflowerBlue));
        }
    }

    private void SwitchIconCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas && canvas.Tag is TrackTemplate template)
        {
            _renderer.RenderToolboxIcon(canvas, template, new SolidColorBrush(Colors.CornflowerBlue));
        }
    }
}
