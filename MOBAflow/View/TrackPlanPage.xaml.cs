// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Common.Extension;
using SharedUI.ViewModel;

using SharedUI.Interface;

using TrackLibrary.PikoA;

using TrackPlan.Renderer;

using Converter;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

public sealed partial class TrackPlanPage
{
    private const string DragFormatTrackCatalog = "application/x-moba-track-catalog";
    private const double ScaleMmToPx = 1.0;
    private const double PortHighlightRadiusMm = 25.0;
    private const double RigidGroupSnapAngleToleranceDegrees = 1.0;
    /// <summary>Margin in mm, damit der gesamte Plan sichtbar ist (auch bei negativen Koordinaten).</summary>
    private const double ContentMarginMm = 50.0;

    public TrackPlanViewModel ViewModel { get; }
    public MainWindowViewModel MainViewModel { get; }
    private readonly EditableTrackPlan _plan;
    private readonly IIoService _ioService;
    private readonly ILogger<TrackPlanPage>? _logger;

    private Canvas? _ghostLayer;
    private Canvas? _rotationHandleLayer;
    private Shape? _ghostShape;
    private PlacedSegment? _draggedPlaced;
    private Guid? _draggedSegmentId;
    private double? _toolboxDragBaseRotationDegrees;
    private HashSet<Guid> _draggingGroup = [];
    private Point _dragStartCanvasPoint;
    private bool _dragHasMoved; // true when pointer actually moved during press
    private double _cachedDrawOffsetX;
    private double _cachedDrawOffsetY;
    private bool _drawOffsetInitialized;
    private readonly List<Ellipse> _portIndicators = [];
    private readonly List<Ellipse> _highlightedPorts = [];
    private bool _snapEnabled = true;
    private Guid? _selectedSegmentId;
    private bool _isSyncingSelectedTrackInPort;
    private double _rotationDragStartAngleRad;
    private double _rotationDragStartSegmentDegrees;

    private GridLength _toolboxExpandedWidth = new(180);
    private GridLength _propertiesExpandedWidth = new(240);

    public TrackPlanPage(
        TrackPlanViewModel viewModel,
        MainWindowViewModel mainViewModel,
        EditableTrackPlan plan,
        IIoService ioService,
        ILogger<TrackPlanPage>? logger = null)
    {
        ViewModel = viewModel;
        MainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _ioService = ioService ?? throw new ArgumentNullException(nameof(ioService));
        _logger = logger;
        InitializeComponent();
        InitializeEditorFeatures();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        SnapToggle.Checked += (_, _) => { _snapEnabled = true; };
        SnapToggle.Unchecked += (_, _) => { _snapEnabled = false; };
        DisconnectButton.Click += (_, _) => DisconnectSelectedSegment();
        LoadTestPlanButton.Click += (_, _) => LoadTestPlan();
        OpenSvgInBrowserButton.Click += (_, _) => OpenSvgInBrowser();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsToolboxExpanded))
        {
            if (!ViewModel.IsToolboxExpanded)
            {
                if (!ColToolbox.Width.IsAuto)
                {
                    _toolboxExpandedWidth = ColToolbox.Width;
                }
                ColToolbox.Width = GridLength.Auto;
            }
            else
            {
                ColToolbox.Width = _toolboxExpandedWidth;
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsPropertiesExpanded))
        {
            if (!ViewModel.IsPropertiesExpanded)
            {
                if (!ColProperties.Width.IsAuto)
                {
                    _propertiesExpandedWidth = ColProperties.Width;
                }
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColProperties.Width = _propertiesExpandedWidth;
            }
        }
    }

    private static TrackPlanResult CreateTestPlan() =>
        new TrackPlanBuilder()
            .Start(0)
            .Add<WR>().Connections(
                wr => wr.FromA.ToB<R9>().FromA.ToA<G62>(),
                wr => wr.FromB.ToA<G239>().FromB.ToA<G62>(),
                wr => wr.FromC.ToA<R9>().FromB.ToA<R9>().FromB.ToA<G62>())
            .Create();

    /// <summary>
    /// Loads the test TrackPlanResult (identical to SVG test) for visual comparison Win2D vs. SVG.
    /// </summary>
    private void LoadTestPlan()
    {
        var plan = CreateTestPlan();
        var renderResult = new TrackPlanSvgRenderer().Render(plan);
        LoadCurrentPlacementsAsDocument(renderResult.Placements, plan.Connections, clearHistory: true);
        StatusText.Text = "Test plan loaded. Click \"SVG in Browser\" for direct comparison.";
    }

    /// <summary>
    /// Exports the same test plan as SVG and opens in browser.
    /// </summary>
    private void OpenSvgInBrowser()
    {
        ExportCurrentTrackPlanToBrowser();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SegmentPlanPathBuilder.ScaleMmToPx = ScaleMmToPx;
        PopulateToolbox();
        SetupCanvas();
        SetupZoom();
        _plan.PlanChanged += OnPlanChanged;
        KeyDown += Page_KeyDown;

        // Solution <-> EditableTrackPlan sync is handled centrally by TrackPlanSolutionBinder (singleton).
        // The page only reflects the current plan state that the binder maintains.
        RecalculateDrawOffset();
        RefreshCanvas();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _plan.PlanChanged -= OnPlanChanged;
        KeyDown -= Page_KeyDown;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
    }

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        _ = sender;
        HandlePageKeyDownAsync(e).Observe(ex => _logger?.LogWarning(ex, "Keyboard handler failed"));
    }

    private void OnDeleteAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        // Don't eat Delete/Backspace while the user is editing a text field.
        if (FocusManager.GetFocusedElement(XamlRoot) is TextBox or NumberBox)
            return;

        if (_selectedSegmentId == null)
            return;

        DeleteSelectedSegment();
        args.Handled = true;
    }

    private void OnUndoAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        Undo();
        args.Handled = true;
    }

    private void OnRedoAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        Redo();
        args.Handled = true;
    }

    private void OnSelectedTrackInPortChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (_selectedSegmentId == null || _isSyncingSelectedTrackInPort)
            return;

        int? newInPort = double.IsNaN(args.NewValue) ? null : (int)args.NewValue;
        var placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == _selectedSegmentId.Value);
        if (placed == null || placed.InPort == newInPort)
            return;

        var before = CaptureDocumentState();
        _plan.UpdateSegmentInPort(_selectedSegmentId.Value, newInPort);
        CommitHistorySnapshot(before);
    }

    private async Task HandlePageKeyDownAsync(KeyRoutedEventArgs e)
    {
        try
        {
            if (IsCtrlPressed())
            {
                if (e.Key == VirtualKey.Z)
                {
                    Undo();
                    e.Handled = true;
                    return;
                }

                if (e.Key == VirtualKey.Y)
                {
                    Redo();
                    e.Handled = true;
                    return;
                }

                if (e.Key == VirtualKey.S)
                {
                    await SaveTrackPlanAsync(IsShiftPressed());
                    e.Handled = true;
                    return;
                }

                if (e.Key == VirtualKey.O)
                {
                    await LoadTrackPlanAsync();
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key != VirtualKey.Delete && e.Key != VirtualKey.Back)
                return;

            if (_selectedSegmentId == null)
                return;

            DeleteSelectedSegment();
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Keyboard handler failed");
        }
    }

    private void OnPlanChanged(object? sender, EventArgs e)
    {
        if (_plan.Segments.Count == 0)
        {
            _cachedDrawOffsetX = 0;
            _cachedDrawOffsetY = 0;
            _drawOffsetInitialized = true;
        }

        if (_selectedSegmentId.HasValue && _plan.Segments.All(s => s.Segment.No != _selectedSegmentId.Value))
        {
            _selectedSegmentId = null;
        }

        if (!_isApplyingDocumentState)
            RefreshDirtyState();

        UpdateSelectionInfo();
        UpdateCommandStates();
        RefreshCanvas();
    }

    private void PopulateToolbox()
    {
        ToolboxStackPanel.Children.Clear();
        AddToolboxGroup("Straights", PikoACatalog.Straights);
        AddToolboxGroup("Curves", PikoACatalog.Curves);
        AddToolboxGroup("Switches", PikoACatalog.Switches);
        AddToolboxGroup("Crossings", PikoACatalog.Crossings);
    }

    private void AddToolboxGroup(string title, IReadOnlyList<TrackCatalogEntry> entries)
    {
        var header = new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 4)
        };
        ToolboxStackPanel.Children.Add(header);
        foreach (var entry in entries)
        {
            var border = new Border
            {
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 2, 0, 2),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"]!,
                Tag = entry,
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"]!
            };
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };
            var symbol = TrackPreviewSymbol.CreateSymbol(entry);
            panel.Children.Add(symbol);
            var codeText = new TextBlock
            {
                Text = entry.Code,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            };
            ToolTipService.SetToolTip(codeText, entry.DisplayName);
            panel.Children.Add(codeText);
            border.Child = panel;
            border.PointerPressed += ToolboxItem_PointerPressed;
            border.PointerEntered += (s, _) => { if (s is Border b) b.Opacity = 0.8; };
            border.PointerExited += (s, _) => { if (s is Border b) b.Opacity = 1.0; };
            ToolboxStackPanel.Children.Add(border);
        }
    }

    private void ToolboxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        HandleToolboxItemPointerPressedAsync(sender, e).Observe(ex => _logger?.LogWarning(ex, "Toolbox drag start failed"));
    }

    private Task HandleToolboxItemPointerPressedAsync(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (sender is not Border border || border.Tag is not TrackCatalogEntry entry)
                return Task.CompletedTask;

            var ptr = e.GetCurrentPoint(border);
            if (ptr.Properties.IsLeftButtonPressed)
            {
                var dataPackage = new DataPackage();
                dataPackage.SetData(DragFormatTrackCatalog, entry.Code);
                dataPackage.SetText(entry.DisplayName);

                _draggedPlaced = null;
                _draggedSegmentId = null;
                _draggingGroup = [];

                StartDragFromToolbox(entry, border, ptr, e.Pointer);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Toolbox drag start failed");
        }

        return Task.CompletedTask;
    }

    private void StartDragFromToolbox(TrackCatalogEntry entry, Border sourceBorder, PointerPoint ptr, Pointer pointer)
    {
        var canvasPoint = sourceBorder.TransformToVisual(OverlayCanvas).TransformPoint(ptr.Position);
        _draggingGroup = [];
        _toolboxDragBaseRotationDegrees = 0;
        _draggedPlaced = new PlacedSegment(entry.CreateInstance(), canvasPoint.X / ScaleMmToPx, canvasPoint.Y / ScaleMmToPx, 0);
        _draggedSegmentId = null;
        CreateGhost(_draggedPlaced);
        UpdateGhostPosition();
        MainGrid.PointerMoved += Canvas_PointerMoved_ToolboxDrag;
        MainGrid.PointerReleased += Canvas_PointerReleased_ToolboxDrag;
        MainGrid.CapturePointer(pointer);
    }

    private void SetupCanvas()
    {
        _ghostLayer = new Canvas
        {
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        Canvas.SetZIndex(_ghostLayer, 1000);
        OverlayCanvas.Children.Add(_ghostLayer);

        _rotationHandleLayer = new Canvas
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        Canvas.SetZIndex(_rotationHandleLayer, 1100);
        OverlayCanvas.Children.Add(_rotationHandleLayer);

        OverlayCanvas.AllowDrop = true;
        OverlayCanvas.DragOver += Canvas_DragOver;
        OverlayCanvas.Drop += Canvas_Drop;
        OverlayCanvas.PointerMoved += Canvas_PointerMoved_UpdateCoords;
        OverlayCanvas.PointerPressed += Canvas_PointerPressed;
        OverlayCanvas.PointerReleased += Canvas_PointerReleased;
        OverlayCanvas.PointerExited += Canvas_PointerExited;
    }

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Move;
        if (e.DataView.Contains(DragFormatTrackCatalog))
            e.DragUIOverride.Caption = "Drop track";
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        _ = sender;
        HandleCanvasDropAsync(e).Observe(ex => _logger?.LogWarning(ex, "Drop handling failed"));
    }

    private async Task HandleCanvasDropAsync(DragEventArgs e)
    {
        try
        {
            var (xMm, yMm) = ToWorldCoordinates(e.GetPosition(OverlayCanvas));

            if (e.DataView.Contains(DragFormatTrackCatalog))
            {
                var data = await e.DataView.GetDataAsync(DragFormatTrackCatalog);
                var code = data?.ToString();
                var entry = PikoACatalog.All.FirstOrDefault(c => c.Code == code);
                if (entry != null)
                {
                    var segment = entry.CreateInstance();
                    var placed = new PlacedSegment(segment, xMm, yMm, 0);
                    TrySnapAndPlace(placed, null);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Drop handling failed");
        }
    }

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // OverlayCanvas has IsTabStop=true, so Focus() sticks here and KeyboardAccelerators on the page fire.
        OverlayCanvas.Focus(FocusState.Pointer);

        var ptr = e.GetCurrentPoint(OverlayCanvas);
        if (!ptr.Properties.IsLeftButtonPressed)
            return;

        var pos = ptr.Position;
        var (xMm, yMm) = ToWorldCoordinates(pos);

        var hit = HitTestSegment(xMm, yMm);
        if (hit == null)
        {
            _draggingGroup = [];
            _selectedSegmentId = null;
            UpdateSelectionInfo();
            RefreshCanvas();
        }
        else
        {
            BeginCanvasDrag(hit, pos, e.Pointer);
        }
    }

    private void BeginCanvasDrag(PlacedSegment hit, Point pointerPosition, Pointer pointer)
    {
        _selectedSegmentId = hit.Segment.No;
        UpdateSelectionInfo();
        RefreshCanvas();

        _toolboxDragBaseRotationDegrees = null;
        _draggedSegmentId = hit.Segment.No;
        _draggedPlaced = hit;
        _draggingGroup = [.. _plan.GetConnectedGroup(hit.Segment.No)];
        _pendingDragSnapshot = CaptureDocumentState();
        _dragStartCanvasPoint = pointerPosition;
        _dragHasMoved = false;
        CreateGhost(hit);
        UpdateGhostPosition();
        AttachCanvasDragHandlers(pointer);
    }

    private void AttachCanvasDragHandlers(Pointer pointer)
    {
        OverlayCanvas.PointerMoved += Canvas_PointerMoved_CanvasDrag;
        OverlayCanvas.PointerReleased += Canvas_PointerReleased_CanvasDrag;
        OverlayCanvas.CapturePointer(pointer);
    }

    private void Canvas_PointerMoved_ToolboxDrag(object sender, PointerRoutedEventArgs e)
    {
        var ptr = e.GetCurrentPoint(MainGrid);
        var canvasPoint = MainGrid.TransformToVisual(OverlayCanvas).TransformPoint(ptr.Position);
        var (worldX, worldY) = ToWorldCoordinates(canvasPoint);
        UpdateToolboxDragPlacement(worldX, worldY);
    }

    private void Canvas_PointerMoved_CanvasDrag(object sender, PointerRoutedEventArgs e)
    {
        var ptr = e.GetCurrentPoint(OverlayCanvas);
        var dx = ptr.Position.X - _dragStartCanvasPoint.X;
        var dy = ptr.Position.Y - _dragStartCanvasPoint.Y;
        _dragStartCanvasPoint = ptr.Position;
        if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
            _dragHasMoved = true;

        UpdateDraggedGroupPosition(dx / ScaleMmToPx, dy / ScaleMmToPx);
    }

    private void UpdateDraggedGroupPosition(double deltaMmX, double deltaMmY)
    {
        _plan.MoveGroup(_draggingGroup, deltaMmX, deltaMmY);

        if (_ghostShape != null && _draggedPlaced != null)
        {
            _draggedPlaced = _draggedPlaced.WithPosition(
                _draggedPlaced.X + deltaMmX,
                _draggedPlaced.Y + deltaMmY,
                _draggedPlaced.RotationDegrees);
            UpdateGhostPosition();
        }

        UpdatePortHighlights();
    }

    private void Canvas_PointerReleased_ToolboxDrag(object sender, PointerRoutedEventArgs e)
    {
        DetachToolboxDragHandlers(e.Pointer);

        CompleteToolboxDrop(e);
        ResetDragState();
    }

    private void DetachToolboxDragHandlers(Pointer pointer)
    {
        MainGrid.PointerMoved -= Canvas_PointerMoved_ToolboxDrag;
        MainGrid.PointerReleased -= Canvas_PointerReleased_ToolboxDrag;
        MainGrid.ReleasePointerCapture(pointer);
    }

    private void CompleteToolboxDrop(PointerRoutedEventArgs e)
    {
        if (_draggedPlaced == null)
        {
            return;
        }

        var canvasPoint = MainGrid.TransformToVisual(OverlayCanvas).TransformPoint(e.GetCurrentPoint(MainGrid).Position);
        var (xMm, yMm) = ToWorldCoordinates(canvasPoint);
        UpdateToolboxDragPlacement(xMm, yMm);
        TrySnapAndPlace(_draggedPlaced, null);
    }

    private void Canvas_PointerReleased_CanvasDrag(object sender, PointerRoutedEventArgs e)
    {
        DetachCanvasDragHandlers(e.Pointer);

        CompleteCanvasDrop();
        ResetDragState();
        RefreshCanvas();
    }

    private void DetachCanvasDragHandlers(Pointer pointer)
    {
        OverlayCanvas.PointerMoved -= Canvas_PointerMoved_CanvasDrag;
        OverlayCanvas.PointerReleased -= Canvas_PointerReleased_CanvasDrag;
        OverlayCanvas.ReleasePointerCapture(pointer);
    }

    private void CompleteCanvasDrop()
    {
        if (ShouldSnapDraggedSegmentOnDrop())
        {
            TrySnapOnDrop(_draggedSegmentId!.Value);
        }

        CommitPendingDragSnapshot();
    }

    private bool ShouldSnapDraggedSegmentOnDrop()
    {
        return _draggedSegmentId.HasValue && _dragHasMoved;
    }

    private void CommitPendingDragSnapshot()
    {
        if (_pendingDragSnapshot == null)
        {
            return;
        }

        CommitHistorySnapshot(_pendingDragSnapshot);
        _pendingDragSnapshot = null;
    }

    private void ResetDragState()
    {
        _draggedSegmentId = null;
        _draggedPlaced = null;
        _toolboxDragBaseRotationDegrees = null;
        _draggingGroup = [];
        ClearGhost();
        ClearPortHighlights();
    }

    private void UpdateToolboxDragPlacement(double worldX, double worldY)
    {
        if (_draggedPlaced == null)
            return;

        var rotationDegrees = _toolboxDragBaseRotationDegrees ?? _draggedPlaced.RotationDegrees;
        var rawPlaced = _draggedPlaced.WithPosition(worldX, worldY, rotationDegrees);
        var snap = _snapEnabled ? FindBestSnap(rawPlaced, null) : null;
        _draggedPlaced = snap?.Placed ?? rawPlaced;
        UpdateGhostPosition();
        UpdatePortHighlights();
    }

    private void Canvas_PointerMoved_UpdateCoords(object sender, PointerRoutedEventArgs e)
    {
        var (worldX, worldY) = ToWorldCoordinates(e.GetCurrentPoint(OverlayCanvas).Position);
        CoordinatesText.Text = $"X: {worldX:F0} mm  Y: {worldY:F0} mm";
    }

    private (double X, double Y) ToWorldCoordinates(Point canvasPoint)
    {
        var (offsetX, offsetY) = GetDrawOffset();
        return (canvasPoint.X / ScaleMmToPx - offsetX, canvasPoint.Y / ScaleMmToPx - offsetY);
    }

    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Nur wenn kein Drag aktiv war
    }

    private void Canvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        CoordinatesText.Text = "X: 0  Y: 0";
    }

    private void TrySnapAndPlace(PlacedSegment placed, Guid? excludeSegmentId)
    {
        var before = CaptureDocumentState();
        var snap = FindSnapWhenEnabled(placed, excludeSegmentId);
        if (snap != null)
        {
            ApplySnapAdd(snap.Value);
            UpdateStats();
            CommitHistorySnapshot(before);
            return;
        }

        AddSegmentWithoutSnap(placed);
        UpdateStats();
        CommitHistorySnapshot(before);
    }

    private void TrySnapOnDrop(Guid movedSegmentId)
    {
        var placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == movedSegmentId);
        if (placed == null)
            return;

        var snap = FindSnapWhenEnabled(placed, movedSegmentId);
        if (snap == null)
            return;

        ApplySnapMove(movedSegmentId, placed, snap.Value);
    }

    private (PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort)? FindBestSnap(PlacedSegment placed, Guid? excludeSegmentId)
    {
        var movingGroup = _draggedSegmentId == placed.Segment.No ? _draggingGroup : null;
        var snap = TrackPlanSnapHelper.FindBestSnap(
            placed,
            _plan.Segments,
            _plan.Connections,
            excludeSegmentId,
            movingGroup);

        return snap == null
            ? null
            : (snap.Placed, snap.SourcePort, snap.TargetSegmentId, snap.TargetPort);
    }

    private (PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort)? FindSnapWhenEnabled(PlacedSegment placed, Guid? excludeSegmentId)
    {
        if (!_snapEnabled)
        {
            return null;
        }

        return FindBestSnap(placed, excludeSegmentId);
    }

    private void ApplySnapAdd((PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort) snap)
    {
        _plan.AddSegment(snap.Placed);
        _plan.AddConnection(snap.Placed.Segment.No, snap.SourcePort, snap.TargetSegmentId, snap.TargetPort);
    }

    private void AddSegmentWithoutSnap(PlacedSegment placed)
    {
        _plan.AddSegment(placed);
    }

    private void ApplySnapMove(
        Guid movedSegmentId,
        PlacedSegment originalPlaced,
        (PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort) snap)
    {
        var deltaX = snap.Placed.X - originalPlaced.X;
        var deltaY = snap.Placed.Y - originalPlaced.Y;

        // Keep connected group movement rigid before final position/rotation update.
        if (_draggingGroup.Count > 1)
        {
            _plan.MoveGroup(_draggingGroup, deltaX, deltaY);
        }

        _plan.UpdateSegmentPosition(movedSegmentId, snap.Placed.X, snap.Placed.Y, snap.Placed.RotationDegrees);
        _plan.AddConnection(movedSegmentId, snap.SourcePort, snap.TargetSegmentId, snap.TargetPort);
    }

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360) degrees -= 360;
        while (degrees < 0) degrees += 360;
        return degrees;
    }

    private bool IsPortConnected(Guid segmentId, string portName)
    {
        return _plan.Connections.Any(connection =>
            (connection.SourceSegment == segmentId && connection.SourcePort == portName)
            || (connection.TargetSegment == segmentId && connection.TargetPort == portName));
    }

    private bool IsRigidDraggingGroup(PlacedSegment placed)
    {
        return _draggedSegmentId == placed.Segment.No && _draggingGroup.Count > 1 && _draggingGroup.Contains(placed.Segment.No);
    }

    private bool CanRigidlySnapToTargetPort(PlacedSegment movingSegment, string sourcePort, PlacedSegment targetSegment, string targetPort)
    {
        if (!IsRigidDraggingGroup(movingSegment))
            return true;

        var desiredOutwardAngle = NormalizeAngle(SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(targetSegment, targetPort) + 180);
        var currentOutwardAngle = SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(movingSegment, sourcePort);
        return GetAngleDeltaDegrees(currentOutwardAngle, desiredOutwardAngle) <= RigidGroupSnapAngleToleranceDegrees;
    }

    private static double GetAngleDeltaDegrees(double leftDegrees, double rightDegrees)
    {
        var delta = NormalizeAngle(leftDegrees - rightDegrees);
        if (delta > 180)
            delta = 360 - delta;
        return Math.Abs(delta);
    }

    private void UpdatePortHighlights()
    {
        ClearPortHighlights();

        if (!CanShowPortHighlights())
            return;

        var (offsetX, offsetY) = GetDrawOffset();
        var draggedPorts = GetUnconnectedDraggedPorts();
        var portsToHighlight = CalculatePortsToHighlight(draggedPorts);
        RenderTargetPortIndicators(offsetX, offsetY, portsToHighlight);
        RenderDraggedPortIndicators(offsetX, offsetY, draggedPorts, portsToHighlight);
    }

    private bool CanShowPortHighlights()
    {
        return _snapEnabled && _showPortHover && _ghostShape != null && _draggedPlaced != null;
    }

    private List<(string PortName, double X, double Y)> GetUnconnectedDraggedPorts()
    {
        if (_draggedPlaced == null)
        {
            return [];
        }

        return SegmentPortGeometry.GetAllPortWorldPositions(_draggedPlaced)
            .Where(p => !IsPortConnected(_draggedPlaced.Segment.No, p.PortName))
            .Select(p => (p.PortName, p.X, p.Y))
            .ToList();
    }

    private HashSet<(double X, double Y)> CalculatePortsToHighlight(List<(string PortName, double X, double Y)> draggedPorts)
    {
        var portsToHighlight = new HashSet<(double X, double Y)>();
        if (_draggedPlaced == null)
        {
            return portsToHighlight;
        }

        foreach (var placed in EnumerateSnapTargetSegments())
        {
            foreach (var (targetPortName, px, py, _) in SegmentPortGeometry.GetAllPortWorldPositions(placed))
            {
                if (IsPortConnected(placed.Segment.No, targetPortName))
                {
                    continue;
                }

                foreach (var (sourcePortName, dx, dy) in draggedPorts)
                {
                    var dist = Math.Sqrt((px - dx) * (px - dx) + (py - dy) * (py - dy));
                    if (dist < PortHighlightRadiusMm && CanRigidlySnapToTargetPort(_draggedPlaced, sourcePortName, placed, targetPortName))
                    {
                        portsToHighlight.Add((px, py));
                        portsToHighlight.Add((dx, dy));
                        break;
                    }
                }
            }
        }

        return portsToHighlight;
    }

    private void RenderTargetPortIndicators(double offsetX, double offsetY, HashSet<(double X, double Y)> portsToHighlight)
    {
        foreach (var placed in EnumerateSnapTargetSegments())
        {
            foreach (var (targetPortName, px, py, _) in SegmentPortGeometry.GetAllPortWorldPositions(placed))
            {
                if (IsPortConnected(placed.Segment.No, targetPortName))
                {
                    continue;
                }

                var highlight = portsToHighlight.Contains((px, py));
                AddPortIndicator((px + offsetX) * ScaleMmToPx, (py + offsetY) * ScaleMmToPx, highlight);
            }
        }
    }

    private void RenderDraggedPortIndicators(
        double offsetX,
        double offsetY,
        List<(string PortName, double X, double Y)> draggedPorts,
        HashSet<(double X, double Y)> portsToHighlight)
    {
        foreach (var (_, px, py) in draggedPorts)
        {
            if (!portsToHighlight.Contains((px, py)))
            {
                continue;
            }

            AddPortIndicator((px + offsetX) * ScaleMmToPx, (py + offsetY) * ScaleMmToPx, true);
        }
    }

    private void AddPortIndicator(double canvasX, double canvasY, bool highlight)
    {
        var indicator = CreatePortIndicator(canvasX, canvasY, highlight);
        OverlayCanvas.Children.Add(indicator);
        if (highlight)
        {
            _highlightedPorts.Add(indicator);
            return;
        }

        _portIndicators.Add(indicator);
    }

    private IEnumerable<PlacedSegment> EnumerateSnapTargetSegments()
    {
        return _plan.Segments.Where(placed => !_draggingGroup.Contains(placed.Segment.No));
    }

    private void ClearPortHighlights()
    {
        foreach (var el in _highlightedPorts)
            OverlayCanvas.Children.Remove(el);
        _highlightedPorts.Clear();
        foreach (var el in _portIndicators)
            OverlayCanvas.Children.Remove(el);
        _portIndicators.Clear();
    }

    private PlacedSegment? HitTestSegment(double xMm, double yMm)
    {
        const double hitToleranceMm = 12;
        PlacedSegment? best = null;
        var bestDist = double.MaxValue;

        foreach (var placed in _plan.Segments)
        {
            var ports = SegmentPortGeometry.GetAllPortWorldPositions(placed).ToList();
            TryHitTestPorts(placed, ports, xMm, yMm, hitToleranceMm, ref best, ref bestDist);
            TryHitTestNeighborPortSegments(placed, ports, xMm, yMm, hitToleranceMm, ref best, ref bestDist);
            TryHitTestFirstToLastPortSegment(placed, ports, xMm, yMm, hitToleranceMm, ref best, ref bestDist);
        }

        return best;
    }

    private static void TryHitTestPorts(
        PlacedSegment placed,
        IReadOnlyList<(string PortName, double X, double Y, double AngleDegrees)> ports,
        double xMm,
        double yMm,
        double hitToleranceMm,
        ref PlacedSegment? best,
        ref double bestDist)
    {
        foreach (var (_, px, py, _) in ports)
        {
            var dist = Math.Sqrt((xMm - px) * (xMm - px) + (yMm - py) * (yMm - py));
            UpdateBestHitCandidate(placed, dist, hitToleranceMm, ref best, ref bestDist);
        }
    }

    private static void TryHitTestNeighborPortSegments(
        PlacedSegment placed,
        IReadOnlyList<(string PortName, double X, double Y, double AngleDegrees)> ports,
        double xMm,
        double yMm,
        double hitToleranceMm,
        ref PlacedSegment? best,
        ref double bestDist)
    {
        for (var i = 0; i < ports.Count - 1; i++)
        {
            var (_, x1, y1, _) = ports[i];
            var (_, x2, y2, _) = ports[i + 1];
            var dist = DistanceToSegment(xMm, yMm, x1, y1, x2, y2);
            UpdateBestHitCandidate(placed, dist, hitToleranceMm, ref best, ref bestDist);
        }
    }

    private static void TryHitTestFirstToLastPortSegment(
        PlacedSegment placed,
        IReadOnlyList<(string PortName, double X, double Y, double AngleDegrees)> ports,
        double xMm,
        double yMm,
        double hitToleranceMm,
        ref PlacedSegment? best,
        ref double bestDist)
    {
        if (ports.Count < 2)
        {
            return;
        }

        var (_, x1, y1, _) = ports[0];
        var (_, x2, y2, _) = ports[ports.Count - 1];
        var dist = DistanceToSegment(xMm, yMm, x1, y1, x2, y2);
        UpdateBestHitCandidate(placed, dist, hitToleranceMm, ref best, ref bestDist);
    }

    private static void UpdateBestHitCandidate(
        PlacedSegment placed,
        double dist,
        double hitToleranceMm,
        ref PlacedSegment? best,
        ref double bestDist)
    {
        if (dist < hitToleranceMm && dist < bestDist)
        {
            bestDist = dist;
            best = placed;
        }
    }

    private static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var lenSq = dx * dx + dy * dy;
        if (lenSq < 1e-10)
            return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        var t = Math.Clamp(((px - x1) * dx + (py - y1) * dy) / lenSq, 0.0, 1.0);
        var projX = x1 + t * dx;
        var projY = y1 + t * dy;
        return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }

    /// <summary>
    /// Berechnet die Bounding-Box aller Segmente in Weltkoordinaten (mm).
    /// </summary>
    private (double MinX, double MinY, double MaxX, double MaxY)? ComputeContentBoundsMm()
    {
        if (_plan.Segments.Count == 0)
            return null;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var placed in _plan.Segments)
        {
            var path = SegmentLocalPathBuilder.GetPath(placed.Segment);
            var (localMinX, localMinY, localMaxX, localMaxY) = SegmentLocalPathBuilder.GetBounds(path);

            var angleRad = placed.RotationDegrees * Math.PI / 180;
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            static double Tx(double ox, double oy, double lx, double ly, double cos, double sin)
            {
                _ = oy;
                return ox + lx * cos - ly * sin;
            }

            static double Ty(double ox, double oy, double lx, double ly, double cos, double sin)
            {
                _ = ox;
                return oy + lx * sin + ly * cos;
            }

            var corners = new[]
            {
                (Tx(placed.X, placed.Y, localMinX, localMinY, cos, sin), Ty(placed.X, placed.Y, localMinX, localMinY, cos, sin)),
                (Tx(placed.X, placed.Y, localMaxX, localMinY, cos, sin), Ty(placed.X, placed.Y, localMaxX, localMinY, cos, sin)),
                (Tx(placed.X, placed.Y, localMinX, localMaxY, cos, sin), Ty(placed.X, placed.Y, localMinX, localMaxY, cos, sin)),
                (Tx(placed.X, placed.Y, localMaxX, localMaxY, cos, sin), Ty(placed.X, placed.Y, localMaxX, localMaxY, cos, sin))
            };

            foreach (var (x, y) in corners)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return (minX, minY, maxX, maxY);
    }

    /// <summary>Offset in mm, damit der gesamte Inhalt im sichtbaren Bereich liegt (analog SVG viewBox).</summary>
    /// <remarks>
    /// During interactive editing the offset must stay stable, otherwise placed or moved tracks
    /// appear to jump because the viewport is auto-fitted after every model change.
    /// </remarks>
    private (double OffsetX, double OffsetY) GetDrawOffset()
    {
        if (!_drawOffsetInitialized)
            RecalculateDrawOffset();

        return (_cachedDrawOffsetX, _cachedDrawOffsetY);
    }

    private void RecalculateDrawOffset()
    {
        var bounds = ComputeContentBoundsMm();
        if (bounds == null)
        {
            _cachedDrawOffsetX = 0;
            _cachedDrawOffsetY = 0;
            _drawOffsetInitialized = true;
            return;
        }

        var (minX, minY, _, _) = bounds.Value;
        _cachedDrawOffsetX = ContentMarginMm - minX;
        _cachedDrawOffsetY = ContentMarginMm - minY;
        _drawOffsetInitialized = true;
    }

    private void CreateGhost(PlacedSegment placed)
    {
        ClearGhost();
        _ghostShape = SegmentPlanPathBuilder.CreatePath(placed, isGhost: true, isSelected: false);
        if (_ghostLayer != null)
        {
            _ghostLayer.Children.Add(_ghostShape);
            Canvas.SetLeft(_ghostShape, 0);
            Canvas.SetTop(_ghostShape, 0);
        }
    }

    private void UpdateGhostPosition()
    {
        if (_ghostShape == null || _draggedPlaced == null)
            return;
        // Ghost wird bei Bewegungen neu erzeugt (CreatePath mit aktualisiertem _draggedPlaced), Position (0,0)
        CreateGhost(_draggedPlaced);
    }

    private void ClearGhost()
    {
        if (_ghostShape != null && _ghostLayer != null)
        {
            _ghostLayer.Children.Remove(_ghostShape);
            _ghostShape = null;
        }
    }

    private Ellipse CreatePortIndicator(double xPx, double yPx, bool highlight)
    {
        var r = highlight ? 10 : 5;
        var el = new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Fill = highlight
                ? (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]!
                : (Brush)Application.Current.Resources["SubtleFillColorTertiaryBrush"]!,
            Stroke = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]!,
            StrokeThickness = highlight ? 2 : 1
        };
        Canvas.SetLeft(el, xPx - r);
        Canvas.SetTop(el, yPx - r);
        Canvas.SetZIndex(el, 500);
        return el;
    }

    private void RefreshCanvas()
    {
        var (offsetX, offsetY) = GetDrawOffset();
        if (_ghostLayer != null)
        {
            Canvas.SetLeft(_ghostLayer, offsetX * ScaleMmToPx);
            Canvas.SetTop(_ghostLayer, offsetY * ScaleMmToPx);
        }
        if (_rotationHandleLayer != null)
        {
            Canvas.SetLeft(_rotationHandleLayer, offsetX * ScaleMmToPx);
            Canvas.SetTop(_rotationHandleLayer, offsetY * ScaleMmToPx);
        }
        GraphCanvasControl?.Invalidate();
        UpdateStats();
    }

    private void GraphCanvasControl_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // Keine asynchronen Ressourcen; Geometrie wird pro Draw erzeugt
    }

    /// <summary>Reads the theme-dependent track stroke color for Win2D (Dark: light, Light: dark).</summary>
    private static Color ResolveTrackPlanStrokeBrush()
    {
        if (Application.Current.Resources.TryGetValue("TrackPlanStrokeBrush", out var obj) && obj is SolidColorBrush brush)
            return brush.Color;
        return Color.FromArgb(255, 26, 26, 26);
    }

    /// <summary>Reads the theme-dependent selection stroke color for Win2D.</summary>
    private static Color ResolveTrackPlanStrokeSelectedBrush()
    {
        if (Application.Current.Resources.TryGetValue("TrackPlanStrokeSelectedBrush", out var obj) && obj is SolidColorBrush brush)
            return brush.Color;
        return Color.FromArgb(255, 0, 120, 215);
    }

    private void GraphCanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        var resourceCreator = ds;
        var (offsetX, offsetY) = GetDrawOffset();

        if (_showGrid)
            DrawGrid(ds, offsetX, offsetY);

        // Stroke-Style wie SegmentPlanPathBuilder: Round Join & Caps
        using var strokeStyle = new CanvasStrokeStyle();
        strokeStyle.LineJoin = CanvasLineJoin.Round;
        strokeStyle.StartCap = CanvasCapStyle.Round;
        strokeStyle.EndCap = CanvasCapStyle.Round;

        // Theme-aware track colors (Fluent Design: from resources for Dark/Light)
        var strokeBrush = ApplyOpacity(ResolveTrackPlanStrokeBrush(), _trackOpacity);
        var selectedBrush = ApplyOpacity(ResolveTrackPlanStrokeSelectedBrush(), _trackOpacity);

        foreach (var placed in _plan.Segments)
        {
            var isSelected = placed.Segment.No == _selectedSegmentId;
            var pathCommands = SegmentLocalPathBuilder.GetPath(placed.Segment);
            var worldGeometry = PathToCanvasGeometryConverter.ToCanvasGeometryInWorldCoords(
                resourceCreator, pathCommands, placed.X + offsetX, placed.Y + offsetY, placed.RotationDegrees, ScaleMmToPx);

            var strokeWidth = (float)(isSelected ? 10 : 4);
            var color = isSelected ? selectedBrush : strokeBrush;
            ds.DrawGeometry(worldGeometry, color, strokeWidth, strokeStyle);
        }

        if (_showValidationOverlay)
            DrawValidationOverlay(ds, offsetX, offsetY);
    }

    private void DrawValidationOverlay(CanvasDrawingSession drawingSession, double offsetX, double offsetY)
    {
        if (_plan.Segments.Count == 0)
            return;

        var analysis = TrackPlanValidationHelper.Analyze(_plan.Segments, _plan.Connections);
        var openPortFill = Color.FromArgb(160, 255, 185, 0);
        var openPortStroke = Color.FromArgb(255, 255, 140, 0);
        var overlapFill = Color.FromArgb(180, 232, 17, 35);
        var overlapStroke = Color.FromArgb(255, 180, 0, 20);
        var groupStroke = Color.FromArgb(220, 0, 120, 215);

        if (analysis.ConnectedGroups.Count > 1)
        {
            using var groupStrokeStyle = new CanvasStrokeStyle();
            groupStrokeStyle.DashStyle = CanvasDashStyle.Dash;
            foreach (var group in analysis.ConnectedGroups)
            {
                var x = (float)((group.MinX + offsetX) * ScaleMmToPx - 18);
                var y = (float)((group.MinY + offsetY) * ScaleMmToPx - 18);
                var width = (float)Math.Max(24, (group.MaxX - group.MinX) * ScaleMmToPx + 36);
                var height = (float)Math.Max(24, (group.MaxY - group.MinY) * ScaleMmToPx + 36);
                drawingSession.DrawRectangle(x, y, width, height, groupStroke, 2, groupStrokeStyle);
            }
        }

        foreach (var openPort in analysis.OpenPorts)
        {
            var centerX = (float)((openPort.X + offsetX) * ScaleMmToPx);
            var centerY = (float)((openPort.Y + offsetY) * ScaleMmToPx);
            drawingSession.FillCircle(centerX, centerY, 5, openPortFill);
            drawingSession.DrawCircle(centerX, centerY, 5, openPortStroke, 2);
        }

        foreach (var overlappingPort in analysis.OverlappingPorts)
        {
            var centerX = (float)((overlappingPort.CenterX + offsetX) * ScaleMmToPx);
            var centerY = (float)((overlappingPort.CenterY + offsetY) * ScaleMmToPx);
            drawingSession.FillCircle(centerX, centerY, 9, overlapFill);
            drawingSession.DrawCircle(centerX, centerY, 9, overlapStroke, 2.5f);
        }
    }

    private bool _zoomSyncing;

    private void SetupZoom()
    {
        ZoomSlider.ValueChanged += OnZoomSliderValueChanged;
        CanvasScrollViewer.ViewChanged += OnCanvasScrollViewerViewChanged;
        ZoomInButton.Click += OnZoomInButtonClick;
        ZoomOutButton.Click += OnZoomOutButtonClick;
    }

    private void OnZoomSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        ZoomPercentText.Text = $"{ZoomSlider.Value * 100:F0}%";
        UpdateStats();
        if (_zoomSyncing)
        {
            return;
        }

        _zoomSyncing = true;
        CanvasScrollViewer.ChangeView(null, null, (float)ZoomSlider.Value);
        _zoomSyncing = false;
    }

    private void OnCanvasScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (_zoomSyncing)
        {
            return;
        }

        var factor = CanvasScrollViewer.ZoomFactor;
        if (Math.Abs(ZoomSlider.Value - factor) <= 0.001)
        {
            return;
        }

        _zoomSyncing = true;
        ZoomSlider.Value = factor;
        ZoomPercentText.Text = $"{factor * 100:F0}%";
        _zoomSyncing = false;
    }

    private void OnZoomInButtonClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        ZoomSlider.Value = Math.Min(3, ZoomSlider.Value + 0.25);
    }

    private void OnZoomOutButtonClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        ZoomSlider.Value = Math.Max(0.1, ZoomSlider.Value - 0.25);
    }

    private void DisconnectSelectedSegment()
    {
        if (_selectedSegmentId == null)
            return;
        var before = CaptureDocumentState();
        _plan.DisconnectSegmentFromGroup(_selectedSegmentId.Value);
        CommitHistorySnapshot(before);
    }

    private void UpdateSelectionInfo()
    {
        if (_selectedSegmentId == null)
        {
            SelectionInfoText.Text = "No selection";
            DisconnectButton.IsEnabled = false;
            HideSelectedTrackPropertiesPanel();
            UpdateRotationHandle(null);
            UpdateCommandStates();
            return;
        }

        var placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == _selectedSegmentId);
        if (placed == null)
        {
            SelectionInfoText.Text = "No selection";
            DisconnectButton.IsEnabled = false;
            HideSelectedTrackPropertiesPanel();
            UpdateRotationHandle(null);
            UpdateCommandStates();
            return;
        }

        var entry = PikoACatalog.All.FirstOrDefault(e => e.SegmentType == placed.Segment.GetType());
        var code = entry?.Code ?? placed.Segment.GetType().Name;
        var displayName = entry?.DisplayName ?? code;
        var connCount = _plan.Connections.Count(c => c.SourceSegment == placed.Segment.No || c.TargetSegment == placed.Segment.No);

        SelectionInfoText.Text = $"{code}\n{displayName}\n\nPosition: X={placed.X:F0} mm, Y={placed.Y:F0} mm\nRotation: {placed.RotationDegrees:F0}°\nConnections: {connCount}";
        DisconnectButton.IsEnabled = connCount > 0;
        ShowSelectedTrackPropertiesPanel(placed);
        UpdateRotationHandle(connCount == 0 ? placed : null);
        UpdateCommandStates();
    }

    private void ShowSelectedTrackPropertiesPanel(PlacedSegment placed)
    {
        _isSyncingSelectedTrackInPort = true;
        try
        {
            SelectedTrackInPortBox.Value = placed.InPort ?? double.NaN;
        }
        finally
        {
            _isSyncingSelectedTrackInPort = false;
        }
        SelectedTrackPropertiesPanel.Visibility = Visibility.Visible;
    }

    private void HideSelectedTrackPropertiesPanel()
    {
        _isSyncingSelectedTrackInPort = true;
        try
        {
            SelectedTrackInPortBox.Value = double.NaN;
        }
        finally
        {
            _isSyncingSelectedTrackInPort = false;
        }
        SelectedTrackPropertiesPanel.Visibility = Visibility.Collapsed;
    }

    private const double RotationHandleOffsetMm = 35.0; // Abstand unterhalb des Drehpunkts (wie AnyRail)
    private const double RotationHandleRadiusPx = 12.0;

    private void UpdateRotationHandle(PlacedSegment? placed)
    {
        if (_rotationHandleLayer == null)
            return;

        _rotationHandleLayer.Children.Clear();

        if (placed == null)
            return;

        var pivotX = placed.X * ScaleMmToPx;
        var pivotY = placed.Y * ScaleMmToPx;
        var handleCenterY = pivotY + RotationHandleOffsetMm;

        var handle = new Border
        {
            Width = RotationHandleRadiusPx * 2,
            Height = RotationHandleRadiusPx * 2,
            CornerRadius = new CornerRadius(RotationHandleRadiusPx),
            Background = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]!,
            BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]!,
            BorderThickness = new Thickness(1.5)
        };
        ToolTipService.SetToolTip(handle, "Drag to rotate");

        // Linie vom Drehpunkt zum Handle (wie AnyRail)
        var line = new Line
        {
            X1 = pivotX,
            Y1 = pivotY,
            X2 = pivotX,
            Y2 = handleCenterY,
            Stroke = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"]!,
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 4 }
        };
        Canvas.SetLeft(line, 0);
        Canvas.SetTop(line, 0);
        _rotationHandleLayer.Children.Add(line);

        Canvas.SetLeft(handle, pivotX - RotationHandleRadiusPx);
        Canvas.SetTop(handle, handleCenterY - RotationHandleRadiusPx);
        Canvas.SetZIndex(handle, 1);
        _rotationHandleLayer.Children.Add(handle);

        handle.PointerPressed += RotationHandle_PointerPressed;
    }

    private void RotationHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!TryStartRotationDrag(sender, e, out var placed))
            return;

        _rotationDragStartAngleRad = CalculatePointerAngleRad(placed, e);
        _rotationDragStartSegmentDegrees = placed.RotationDegrees;
        _pendingRotationSnapshot = CaptureDocumentState();

        AttachRotationDragHandlers(e.Pointer);
        e.Handled = true;
    }

    private void RotationHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var placed = GetSelectedSegment();
        if (placed == null)
            return;

        var currentAngleRad = CalculatePointerAngleRad(placed, e);
        var deltaRad = currentAngleRad - _rotationDragStartAngleRad;
        var deltaDeg = deltaRad * 180.0 / Math.PI;
        // Im Bildschirm-KS: Uhrzeigersinn = negativer Winkel → Segment soll mitdrehen
        var newRotation = NormalizeAngle(_rotationDragStartSegmentDegrees - deltaDeg);
        _plan.UpdateSegmentPosition(placed.Segment.No, placed.X, placed.Y, newRotation);
    }

    private void RotationHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        DetachRotationDragHandlers(e.Pointer);
        CommitHistorySnapshot(_pendingRotationSnapshot);
        _pendingRotationSnapshot = null;
        UpdateSelectionInfo();
    }

    private void AttachRotationDragHandlers(Pointer pointer)
    {
        OverlayCanvas.PointerMoved += RotationHandle_PointerMoved;
        OverlayCanvas.PointerReleased += RotationHandle_PointerReleased;
        OverlayCanvas.CapturePointer(pointer);
    }

    private void DetachRotationDragHandlers(Pointer pointer)
    {
        OverlayCanvas.PointerMoved -= RotationHandle_PointerMoved;
        OverlayCanvas.PointerReleased -= RotationHandle_PointerReleased;
        OverlayCanvas.ReleasePointerCapture(pointer);
    }

    private bool TryStartRotationDrag(object sender, PointerRoutedEventArgs e, out PlacedSegment placed)
    {
        placed = null!;
        if (sender is not UIElement handle || !e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
        {
            return false;
        }

        var selectedPlaced = GetSelectedSegment();
        if (selectedPlaced == null || HasConnections(selectedPlaced.Segment.No))
        {
            return false;
        }

        placed = selectedPlaced;
        return true;
    }

    private PlacedSegment? GetSelectedSegment()
    {
        if (_selectedSegmentId == null)
        {
            return null;
        }

        return _plan.Segments.FirstOrDefault(s => s.Segment.No == _selectedSegmentId);
    }

    private bool HasConnections(Guid segmentId)
    {
        return _plan.Connections.Any(c => c.SourceSegment == segmentId || c.TargetSegment == segmentId);
    }

    private double CalculatePointerAngleRad(PlacedSegment placed, PointerRoutedEventArgs e)
    {
        var (pivotDisplayX, pivotDisplayY) = GetPlacedSegmentPivotDisplayPosition(placed);
        var ptr = e.GetCurrentPoint(OverlayCanvas);
        var dx = ptr.Position.X - pivotDisplayX;
        var dy = ptr.Position.Y - pivotDisplayY;
        return Math.Atan2(dy, dx);
    }

    private (double X, double Y) GetPlacedSegmentPivotDisplayPosition(PlacedSegment placed)
    {
        var (offsetX, offsetY) = GetDrawOffset();
        return ((placed.X + offsetX) * ScaleMmToPx, (placed.Y + offsetY) * ScaleMmToPx);
    }

    private void UpdateStats()
    {
        var analysis = TrackPlanValidationHelper.Analyze(_plan.Segments, _plan.Connections);
        NodeCountText.Text = _plan.Segments.Count.ToString();
        EdgeCountText.Text = _plan.Connections.Count.ToString();
        EndcapCountText.Text = analysis.OpenPorts.Count.ToString();
        ZoomLevelText.Text = $"{ZoomSlider.Value * 100:F0}%";
    }
}