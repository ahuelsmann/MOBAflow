// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Common.Navigation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SharedUI.ViewModel;
using TrackLibrary.Base;
using TrackLibrary.PikoA;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;

[NavigationItem(
    Tag = "trackplaneditor",
    Title = "Track Plan",
    Icon = "\uE7F9",
    Category = NavigationCategory.TrackManagement,
    Order = 10,
    FeatureToggleKey = "IsTrackPlanEditorPageAvailable",
    BadgeLabelKey = "TrackPlanEditorPageLabel")]
public sealed partial class TrackPlanPage : Page
{
    private const string DragFormatTrackCatalog = "application/x-moba-track-catalog";
    private const double ScaleMmToPx = 1.0;
    private const double SnapThresholdMm = 15.0;
    private const double PortHighlightRadiusMm = 25.0;

    public TrackPlanViewModel ViewModel { get; }
    private readonly EditableTrackPlan _plan;

    private Canvas? _ghostLayer;
    private Canvas? _rotationHandleLayer;
    private Shape? _ghostShape;
    private PlacedSegment? _draggedPlaced;
    private Guid? _draggedSegmentId;
    private HashSet<Guid> _draggingGroup = [];
    private Point _dragStartCanvasPoint;
    private bool _dragHasMoved; // true wenn Pointer während Press tatsächlich bewegt wurde
    private readonly Dictionary<Guid, UIElement> _segmentVisuals = new();
    private readonly List<Ellipse> _portIndicators = [];
    private readonly List<Ellipse> _highlightedPorts = [];
    private bool _snapEnabled = true;
    private Guid? _selectedSegmentId;
    private UIElement? _rotationHandle;
    private double _rotationDragStartAngleRad;
    private double _rotationDragStartSegmentDegrees;

    public TrackPlanPage(TrackPlanViewModel viewModel, EditableTrackPlan plan)
    {
        ViewModel = viewModel;
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        InitializeComponent();
        Loaded += OnLoaded;

        SnapToggle.Checked += (_, _) => { _snapEnabled = true; };
        SnapToggle.Unchecked += (_, _) => { _snapEnabled = false; };
        DisconnectButton.Click += (_, _) => DisconnectSelectedSegment();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SegmentPlanPathBuilder.ScaleMmToPx = ScaleMmToPx;
        PopulateToolbox();
        SetupCanvas();
        SetupZoom();
        _plan.PlanChanged += OnPlanChanged;
        RefreshCanvas();
    }

    private void OnPlanChanged(object? sender, EventArgs e) => RefreshCanvas();

    private void PopulateToolbox()
    {
        ToolboxStackPanel.Children.Clear();
        AddToolboxGroup("Geraden", PikoACatalog.Straights);
        AddToolboxGroup("Kurven", PikoACatalog.Curves);
        AddToolboxGroup("Weichen", PikoACatalog.Switches);
        AddToolboxGroup("Kreuzungen", PikoACatalog.Crossings);
    }

    private void AddToolboxGroup(string title, IReadOnlyList<TrackCatalogEntry> entries)
    {
        var header = new TextBlock
        {
            Text = title,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
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
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
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

    private async void ToolboxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not TrackCatalogEntry entry)
            return;

        var ptr = e.GetCurrentPoint(border);
        if (ptr.Properties.IsLeftButtonPressed)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetData(DragFormatTrackCatalog, entry.Code);
            dataPackage.SetText(entry.DisplayName);

            _draggedPlaced = null;
            _draggedSegmentId = null;

            StartDragFromToolbox(entry, border, ptr, e.Pointer);
        }
    }

    private void StartDragFromToolbox(TrackCatalogEntry entry, Border sourceBorder, PointerPoint ptr, Pointer pointer)
    {
        var canvasPoint = sourceBorder.TransformToVisual(GraphCanvas).TransformPoint(ptr.Position);
        _draggedPlaced = new PlacedSegment(entry.CreateInstance(), canvasPoint.X / ScaleMmToPx, canvasPoint.Y / ScaleMmToPx, 0);
        _draggedSegmentId = null;
        CreateGhost(_draggedPlaced);
        UpdateGhostPosition(canvasPoint.X, canvasPoint.Y);
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
        GraphCanvas.Children.Add(_ghostLayer);

        _rotationHandleLayer = new Canvas
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        Canvas.SetZIndex(_rotationHandleLayer, 1100);
        GraphCanvas.Children.Add(_rotationHandleLayer);

        GraphCanvas.AllowDrop = true;
        GraphCanvas.DragOver += Canvas_DragOver;
        GraphCanvas.Drop += Canvas_Drop;
        GraphCanvas.PointerMoved += Canvas_PointerMoved_UpdateCoords;
        GraphCanvas.PointerPressed += Canvas_PointerPressed;
        GraphCanvas.PointerReleased += Canvas_PointerReleased;
        GraphCanvas.PointerExited += Canvas_PointerExited;
    }

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Move;
        if (e.DataView.Contains(DragFormatTrackCatalog))
            e.DragUIOverride.Caption = "Gleis ablegen";
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        var pos = e.GetPosition(GraphCanvas);
        var xMm = pos.X / ScaleMmToPx;
        var yMm = pos.Y / ScaleMmToPx;

        if (e.DataView.Contains(DragFormatTrackCatalog))
        {
            var code = e.DataView.GetDataAsync(DragFormatTrackCatalog).AsTask().Result?.ToString();
            var entry = PikoACatalog.All.FirstOrDefault(c => c.Code == code);
            if (entry != null)
            {
                var segment = entry.CreateInstance();
                var placed = new PlacedSegment(segment, xMm, yMm, 0);
                TrySnapAndPlace(placed, null);
            }
        }
    }

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var ptr = e.GetCurrentPoint(GraphCanvas);
        if (!ptr.Properties.IsLeftButtonPressed)
            return;

        var pos = ptr.Position;
        var xMm = pos.X / ScaleMmToPx;
        var yMm = pos.Y / ScaleMmToPx;

        var hit = HitTestSegment(xMm, yMm);
        if (hit == null)
        {
            _selectedSegmentId = null;
            UpdateSelectionInfo();
            RefreshCanvas();
        }
        else
        {
            _selectedSegmentId = hit.Segment.No;
            UpdateSelectionInfo();
            RefreshCanvas();
            _draggedSegmentId = hit.Segment.No;
            _draggedPlaced = hit;
            _draggingGroup = [.. _plan.GetConnectedGroup(hit.Segment.No)];
            _dragStartCanvasPoint = pos;
            _dragHasMoved = false;
            CreateGhost(hit);
            UpdateGhostPosition(pos.X, pos.Y);
            GraphCanvas.PointerMoved += Canvas_PointerMoved_CanvasDrag;
            GraphCanvas.PointerReleased += Canvas_PointerReleased_CanvasDrag;
            GraphCanvas.CapturePointer(e.Pointer);
        }
    }

    private void Canvas_PointerMoved_ToolboxDrag(object sender, PointerRoutedEventArgs e)
    {
        var ptr = e.GetCurrentPoint(MainGrid);
        var canvasPoint = MainGrid.TransformToVisual(GraphCanvas).TransformPoint(ptr.Position);
        UpdateGhostPosition(canvasPoint.X, canvasPoint.Y);
        if (_draggedPlaced != null)
        {
            _draggedPlaced = _draggedPlaced.WithPosition(canvasPoint.X / ScaleMmToPx, canvasPoint.Y / ScaleMmToPx, _draggedPlaced.RotationDegrees);
        }
        UpdatePortHighlights(canvasPoint.X / ScaleMmToPx, canvasPoint.Y / ScaleMmToPx);
    }

    private void Canvas_PointerMoved_CanvasDrag(object sender, PointerRoutedEventArgs e)
    {
        var ptr = e.GetCurrentPoint(GraphCanvas);
        var dx = ptr.Position.X - _dragStartCanvasPoint.X;
        var dy = ptr.Position.Y - _dragStartCanvasPoint.Y;
        _dragStartCanvasPoint = ptr.Position;
        if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
            _dragHasMoved = true;

        var deltaMmX = dx / ScaleMmToPx;
        var deltaMmY = dy / ScaleMmToPx;
        _plan.MoveGroup(_draggingGroup, deltaMmX, deltaMmY);

        if (_ghostShape != null && _draggedPlaced != null)
        {
            var updated = _draggedPlaced.WithPosition(_draggedPlaced.X + deltaMmX, _draggedPlaced.Y + deltaMmY, _draggedPlaced.RotationDegrees);
            _draggedPlaced = updated;
            UpdateGhostPosition(ptr.Position.X, ptr.Position.Y);
        }

        UpdatePortHighlights(ptr.Position.X / ScaleMmToPx, ptr.Position.Y / ScaleMmToPx);
    }

    private void Canvas_PointerReleased_ToolboxDrag(object sender, PointerRoutedEventArgs e)
    {
        MainGrid.PointerMoved -= Canvas_PointerMoved_ToolboxDrag;
        MainGrid.PointerReleased -= Canvas_PointerReleased_ToolboxDrag;
        MainGrid.ReleasePointerCapture(e.Pointer);

        if (_draggedPlaced != null)
        {
            var canvasPoint = MainGrid.TransformToVisual(GraphCanvas).TransformPoint(e.GetCurrentPoint(MainGrid).Position);
            var xMm = canvasPoint.X / ScaleMmToPx;
            var yMm = canvasPoint.Y / ScaleMmToPx;
            var placed = _draggedPlaced.WithPosition(xMm, yMm, _draggedPlaced.RotationDegrees);
            TrySnapAndPlace(placed, null);
        }

        ClearGhost();
        ClearPortHighlights();
    }

    private void Canvas_PointerReleased_CanvasDrag(object sender, PointerRoutedEventArgs e)
    {
        GraphCanvas.PointerMoved -= Canvas_PointerMoved_CanvasDrag;
        GraphCanvas.PointerReleased -= Canvas_PointerReleased_CanvasDrag;
        GraphCanvas.ReleasePointerCapture(e.Pointer);

        if (_draggedSegmentId.HasValue && _dragHasMoved)
        {
            TrySnapOnDrop(_draggedSegmentId.Value);
        }

        _draggedSegmentId = null;
        _draggedPlaced = null;
        ClearGhost();
        ClearPortHighlights();
    }

    private void Canvas_PointerMoved_UpdateCoords(object sender, PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(GraphCanvas).Position;
        CoordinatesText.Text = $"X: {(pos.X / ScaleMmToPx):F0} mm  Y: {(pos.Y / ScaleMmToPx):F0} mm";
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
        if (_snapEnabled)
        {
            var snap = FindBestSnap(placed, excludeSegmentId);
            if (snap != null)
            {
                var (newPlaced, sourcePort, targetSegmentId, targetPort) = snap.Value;
                _plan.AddSegment(newPlaced);
                _plan.AddConnection(newPlaced.Segment.No, sourcePort, targetSegmentId, targetPort);
                UpdateStats();
                return;
            }
        }

        _plan.AddSegment(placed);
        UpdateStats();
    }

    private void TrySnapOnDrop(Guid movedSegmentId)
    {
        var placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == movedSegmentId);
        if (placed == null || !_snapEnabled)
            return;

        var snap = FindBestSnap(placed, movedSegmentId);
        if (snap == null)
            return;

        var (newPlaced, sourcePort, targetSegmentId, targetPort) = snap.Value;
        var deltaX = newPlaced.X - placed.X;
        var deltaY = newPlaced.Y - placed.Y;

        // Ganze Gruppe um Snap-Delta verschieben, dann Rotation des gezogenen Segments setzen
        if (_draggingGroup.Count > 1)
            _plan.MoveGroup(_draggingGroup, deltaX, deltaY);

        _plan.UpdateSegmentPosition(movedSegmentId, newPlaced.X, newPlaced.Y, newPlaced.RotationDegrees);
        _plan.AddConnection(movedSegmentId, sourcePort, targetSegmentId, targetPort);
    }

    private (PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort)? FindBestSnap(PlacedSegment placed, Guid? excludeSegmentId)
    {
        const double bestDistThreshold = SnapThresholdMm * 1.5;
        (PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort)? best = null;
        var bestDist = double.MaxValue;

        var myEntryPort = GetEntryPortForSegment(placed.Segment.No);
        var myPorts = SegmentPortGeometry.GetAllPortWorldPositions(placed, myEntryPort);
        var myPortInfos = SegmentPortGeometry.GetPortsWithEntry(placed.Segment, myEntryPort)
            .ToDictionary(p => p.PortName, p => p.LocalAngleDegrees);

        foreach (var other in _plan.Segments)
        {
            if (other.Segment.No == placed.Segment.No || other.Segment.No == excludeSegmentId)
                continue;

            var otherEntryPort = GetEntryPortForSegment(other.Segment.No);
            var otherPorts = SegmentPortGeometry.GetAllPortWorldPositions(other, otherEntryPort);

            foreach (var (myPortName, mx, my, _) in myPorts)
            foreach (var (otherPortName, ox, oy, oAngle) in otherPorts)
            {
                var dx = ox - mx;
                var dy = oy - my;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < bestDist && dist < bestDistThreshold)
                {
                    bestDist = dist;
                    var myLocalAngle = myPortInfos.GetValueOrDefault(myPortName, 0);
                    // Zielport zeigt "heraus" mit oAngle; unser Port muss "hinein" zeigen = oAngle + 180.
                    var newRotation = NormalizeAngle(oAngle + 180 - myLocalAngle);
                    var newPlaced = placed.WithPosition(placed.X + dx, placed.Y + dy, newRotation);
                    best = (newPlaced, myPortName, other.Segment.No, otherPortName);
                }
            }
        }

        return best;
    }

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360) degrees -= 360;
        while (degrees < 0) degrees += 360;
        return degrees;
    }

    private void UpdatePortHighlights(double cursorXmm, double cursorYmm)
    {
        ClearPortHighlights();

        if (!_snapEnabled || _ghostShape == null || _draggedPlaced == null)
            return;

        var draggedEntryPort = _draggedSegmentId.HasValue ? GetEntryPortForSegment(_draggedSegmentId.Value) : 'A';
        var draggedPorts = SegmentPortGeometry.GetAllPortWorldPositions(_draggedPlaced, draggedEntryPort)
            .Select(p => (p.X, p.Y)).ToList();
        var threshold = PortHighlightRadiusMm;
        var portsToHighlight = new HashSet<(double X, double Y)>();

        foreach (var placed in _plan.Segments)
        {
            var entryPort = GetEntryPortForSegment(placed.Segment.No);
            var ports = SegmentPortGeometry.GetAllPortWorldPositions(placed, entryPort);
            foreach (var (_, px, py, _) in ports)
            {
                foreach (var (dx, dy) in draggedPorts)
                {
                    var dist = Math.Sqrt((px - dx) * (px - dx) + (py - dy) * (py - dy));
                    if (dist < threshold)
                    {
                        portsToHighlight.Add((px, py));
                        portsToHighlight.Add((dx, dy));
                        break;
                    }
                }
            }
        }

        foreach (var placed in _plan.Segments)
        {
            var entryPort = GetEntryPortForSegment(placed.Segment.No);
            var ports = SegmentPortGeometry.GetAllPortWorldPositions(placed, entryPort);
            foreach (var (_, px, py, _) in ports)
            {
                var highlight = portsToHighlight.Contains((px, py));
                var el = CreatePortIndicator(px * ScaleMmToPx, py * ScaleMmToPx, highlight);
                GraphCanvas.Children.Add(el);
                if (highlight)
                    _highlightedPorts.Add(el);
                else
                    _portIndicators.Add(el);
            }
        }

        foreach (var (px, py) in draggedPorts)
        {
            if (portsToHighlight.Contains((px, py)))
            {
                var el = CreatePortIndicator(px * ScaleMmToPx, py * ScaleMmToPx, true);
                GraphCanvas.Children.Add(el);
                _highlightedPorts.Add(el);
            }
        }
    }

    private void ClearPortHighlights()
    {
        foreach (var el in _highlightedPorts)
            GraphCanvas.Children.Remove(el);
        _highlightedPorts.Clear();
        foreach (var el in _portIndicators)
            GraphCanvas.Children.Remove(el);
        _portIndicators.Clear();
    }

    private PlacedSegment? HitTestSegment(double xMm, double yMm)
    {
        const double hitToleranceMm = 12;
        PlacedSegment? best = null;
        var bestDist = double.MaxValue;

        foreach (var placed in _plan.Segments)
        {
            var entryPort = GetEntryPortForSegment(placed.Segment.No);
            var ports = SegmentPortGeometry.GetAllPortWorldPositions(placed, entryPort).ToList();
            for (var i = 0; i < ports.Count; i++)
            {
                var (_, px, py, _) = ports[i];
                var d = Math.Sqrt((xMm - px) * (xMm - px) + (yMm - py) * (yMm - py));
                if (d < hitToleranceMm && d < bestDist)
                {
                    bestDist = d;
                    best = placed;
                }
            }

            for (var i = 0; i < ports.Count - 1; i++)
            {
                var (_, x1, y1, _) = ports[i];
                var (_, x2, y2, _) = ports[i + 1];
                var dist = DistanceToSegment(xMm, yMm, x1, y1, x2, y2);
                if (dist < hitToleranceMm && dist < bestDist)
                {
                    bestDist = dist;
                    best = placed;
                }
            }

            var connCount = ports.Count;
            if (connCount >= 2)
            {
                var (_, x1, y1, _) = ports[0];
                var (_, x2, y2, _) = ports[connCount - 1];
                var dist = DistanceToSegment(xMm, yMm, x1, y1, x2, y2);
                if (dist < hitToleranceMm && dist < bestDist)
                {
                    bestDist = dist;
                    best = placed;
                }
            }
        }

        return best;
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

    private char GetEntryPortForSegment(Guid segmentNo)
    {
        var incoming = _plan.Connections.FirstOrDefault(c => c.TargetSegment == segmentNo);
        return incoming != null && incoming.TargetPort.Length > 0
            ? incoming.TargetPort[^1]
            : 'A';
    }

    private void CreateGhost(PlacedSegment placed)
    {
        ClearGhost();
        var entryPort = GetEntryPortForSegment(placed.Segment.No);
        _ghostShape = SegmentPlanPathBuilder.CreatePath(placed, isGhost: true, isSelected: false, entryPort);
        if (_ghostLayer != null)
        {
            _ghostLayer.Children.Add(_ghostShape);
            Canvas.SetLeft(_ghostShape, 0);
            Canvas.SetTop(_ghostShape, 0);
        }
    }

    private void UpdateGhostPosition(double xPx, double yPx)
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
        foreach (var vis in _segmentVisuals.Values)
            GraphCanvas.Children.Remove(vis);
        _segmentVisuals.Clear();

        foreach (var placed in _plan.Segments)
        {
            var isSelected = placed.Segment.No == _selectedSegmentId;
            var entryPort = GetEntryPortForSegment(placed.Segment.No);
            var shape = SegmentPlanPathBuilder.CreatePath(placed, isGhost: false, isSelected, entryPort);
            // Geometrie ist bereits in Weltkoordinaten (mm → px), Position (0,0)
            Canvas.SetLeft(shape, 0);
            Canvas.SetTop(shape, 0);
            Canvas.SetZIndex(shape, 100);
            GraphCanvas.Children.Add(shape);
            _segmentVisuals[placed.Segment.No] = shape;
        }

        UpdateStats();
    }

    private bool _zoomSyncing;

    private void SetupZoom()
    {
        ZoomSlider.ValueChanged += (_, _) =>
        {
            ZoomPercentText.Text = $"{ZoomSlider.Value * 100:F0}%";
            UpdateStats();
            if (!_zoomSyncing)
            {
                _zoomSyncing = true;
                CanvasScrollViewer.ChangeView(null, null, (float)ZoomSlider.Value);
                _zoomSyncing = false;
            }
        };
        CanvasScrollViewer.ViewChanged += (s, _) =>
        {
            if (_zoomSyncing)
                return;
            var factor = CanvasScrollViewer.ZoomFactor;
            if (Math.Abs(ZoomSlider.Value - factor) > 0.001)
            {
                _zoomSyncing = true;
                ZoomSlider.Value = factor;
                ZoomPercentText.Text = $"{factor * 100:F0}%";
                _zoomSyncing = false;
            }
        };
        ZoomInButton.Click += (_, _) => ZoomSlider.Value = Math.Min(3, ZoomSlider.Value + 0.25);
        ZoomOutButton.Click += (_, _) => ZoomSlider.Value = Math.Max(0.1, ZoomSlider.Value - 0.25);
    }

    private void DisconnectSelectedSegment()
    {
        if (_selectedSegmentId == null)
            return;
        _plan.DisconnectSegmentFromGroup(_selectedSegmentId.Value);
    }

    private void UpdateSelectionInfo()
    {
        if (_selectedSegmentId == null)
        {
            SelectionInfoText.Text = "No selection";
            DisconnectButton.IsEnabled = false;
            UpdateRotationHandle(null);
            return;
        }

        var placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == _selectedSegmentId);
        if (placed == null)
        {
            SelectionInfoText.Text = "No selection";
            DisconnectButton.IsEnabled = false;
            UpdateRotationHandle(null);
            return;
        }

        var entry = PikoACatalog.All.FirstOrDefault(e => e.SegmentType == placed.Segment.GetType());
        var code = entry?.Code ?? placed.Segment.GetType().Name;
        var displayName = entry?.DisplayName ?? code;
        var connCount = _plan.Connections.Count(c => c.SourceSegment == placed.Segment.No || c.TargetSegment == placed.Segment.No);

        SelectionInfoText.Text = $"{code}\n{displayName}\n\nPosition: X={placed.X:F0} mm, Y={placed.Y:F0} mm\nRotation: {placed.RotationDegrees:F0}°\nVerbindungen: {connCount}";
        DisconnectButton.IsEnabled = connCount > 0;
        UpdateRotationHandle(connCount == 0 ? placed : null);
    }

    private const double RotationHandleOffsetMm = 35.0; // Abstand unterhalb des Drehpunkts (wie AnyRail)
    private const double RotationHandleRadiusPx = 12.0;

    private void UpdateRotationHandle(PlacedSegment? placed)
    {
        if (_rotationHandleLayer == null)
            return;

        _rotationHandleLayer.Children.Clear();
        _rotationHandle = null;

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
        ToolTipService.SetToolTip(handle, "Ziehen zum Drehen");

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

        _rotationHandle = handle;

        handle.PointerPressed += RotationHandle_PointerPressed;
    }

    private void RotationHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement handle || _selectedSegmentId == null || !e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
            return;

        var placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == _selectedSegmentId);
        if (placed == null)
            return;

        var connCount = _plan.Connections.Count(c => c.SourceSegment == placed.Segment.No || c.TargetSegment == placed.Segment.No);
        if (connCount > 0)
            return;

        var pivotX = placed.X * ScaleMmToPx;
        var pivotY = placed.Y * ScaleMmToPx;
        var ptr = e.GetCurrentPoint(GraphCanvas);
        var dx = ptr.Position.X - pivotX;
        var dy = ptr.Position.Y - pivotY;
        _rotationDragStartAngleRad = Math.Atan2(dy, dx);
        _rotationDragStartSegmentDegrees = placed.RotationDegrees;

        GraphCanvas.PointerMoved += RotationHandle_PointerMoved;
        GraphCanvas.PointerReleased += RotationHandle_PointerReleased;
        GraphCanvas.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void RotationHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_selectedSegmentId == null)
            return;

        var placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == _selectedSegmentId);
        if (placed == null)
            return;

        var pivotX = placed.X * ScaleMmToPx;
        var pivotY = placed.Y * ScaleMmToPx;
        var ptr = e.GetCurrentPoint(GraphCanvas);
        var dx = ptr.Position.X - pivotX;
        var dy = ptr.Position.Y - pivotY;
        var currentAngleRad = Math.Atan2(dy, dx);
        var deltaRad = currentAngleRad - _rotationDragStartAngleRad;
        var deltaDeg = deltaRad * 180.0 / Math.PI;
        // Im Bildschirm-KS: Uhrzeigersinn = negativer Winkel → Segment soll mitdrehen
        var newRotation = NormalizeAngle(_rotationDragStartSegmentDegrees - deltaDeg);
        _plan.UpdateSegmentPosition(placed.Segment.No, placed.X, placed.Y, newRotation);
    }

    private void RotationHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        GraphCanvas.PointerMoved -= RotationHandle_PointerMoved;
        GraphCanvas.PointerReleased -= RotationHandle_PointerReleased;
        GraphCanvas.ReleasePointerCapture(e.Pointer);
        UpdateSelectionInfo();
    }

    private void UpdateStats()
    {
        NodeCountText.Text = _plan.Segments.Count.ToString();
        EdgeCountText.Text = _plan.Connections.Count.ToString();
        var portCount = _plan.Segments.Sum(p => SegmentPortGeometry.GetPorts(p.Segment).Count);
        var openEnds = Math.Max(0, portCount - _plan.Connections.Count * 2);
        EndcapCountText.Text = openEnds.ToString();
        ZoomLevelText.Text = $"{ZoomSlider.Value * 100:F0}%";
    }
}