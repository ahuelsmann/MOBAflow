// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Common.Navigation;

using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using SharedUI.ViewModel;

using System.Diagnostics;

using TrackLibrary.PikoA;

using TrackPlan.Renderer;

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
    /// <summary>Margin in mm, damit der gesamte Plan sichtbar ist (auch bei negativen Koordinaten).</summary>
    private const double ContentMarginMm = 50.0;

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
    private bool _isCanvasDragging;
    private bool _useCachedOffsetForNextRefresh;
    private double _cachedDrawOffsetX;
    private double _cachedDrawOffsetY;
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
        LoadTestPlanButton.Click += (_, _) => LoadTestPlan();
        OpenSvgInBrowserButton.Click += (_, _) => OpenSvgInBrowser();
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
    /// Lädt das Test-TrackPlanResult (identisch zum SVG-Test) für visuellen Vergleich Win2D vs. SVG.
    /// </summary>
    private void LoadTestPlan()
    {
        var plan = CreateTestPlan();
        var renderResult = new TrackPlanSvgRenderer().Render(plan);
        _plan.LoadFromPlacements(renderResult.Placements, plan.Connections);
        StatusText.Text = "Test Plan geladen. Klicke „SVG im Browser“ für direkten Vergleich.";
    }

    /// <summary>
    /// Exportiert dasselbe Test-Plan als SVG und öffnet im Browser.
    /// </summary>
    private void OpenSvgInBrowser()
    {
        var plan = CreateTestPlan();
        var renderResult = new TrackPlanSvgRenderer().Render(plan);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "trackplan-win2d-compare.html");
        new SvgExporter().Export(renderResult.Svg, path);

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }

        StatusText.Text = $"SVG geöffnet: {path}";
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SegmentPlanPathBuilder.ScaleMmToPx = ScaleMmToPx;
        PopulateToolbox();
        SetupCanvas();
        SetupZoom();
        _plan.PlanChanged += OnPlanChanged;
        this.KeyDown += Page_KeyDown;
        RefreshCanvas();
    }

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Delete && e.Key != VirtualKey.Back)
            return;

        if (_selectedSegmentId == null)
            return;

        _plan.RemoveSegment(_selectedSegmentId.Value);
        _selectedSegmentId = null;
        UpdateSelectionInfo();
        RefreshCanvas();
        e.Handled = true;
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
        var canvasPoint = sourceBorder.TransformToVisual(OverlayCanvas).TransformPoint(ptr.Position);
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
            e.DragUIOverride.Caption = "Gleis ablegen";
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        var pos = e.GetPosition(OverlayCanvas);
        var (offsetX, offsetY) = GetDrawOffset();
        var xMm = pos.X / ScaleMmToPx - offsetX;
        var yMm = pos.Y / ScaleMmToPx - offsetY;

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
        Focus(FocusState.Pointer);

        var ptr = e.GetCurrentPoint(OverlayCanvas);
        if (!ptr.Properties.IsLeftButtonPressed)
            return;

        var pos = ptr.Position;
        var (offsetX, offsetY) = GetDrawOffset();
        var xMm = pos.X / ScaleMmToPx - offsetX;
        var yMm = pos.Y / ScaleMmToPx - offsetY;

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
            _isCanvasDragging = true;
            _draggedSegmentId = hit.Segment.No;
            _draggedPlaced = hit;
            _draggingGroup = [.. _plan.GetConnectedGroup(hit.Segment.No)];
            _dragStartCanvasPoint = pos;
            _dragHasMoved = false;
            CreateGhost(hit);
            UpdateGhostPosition(pos.X, pos.Y);
            OverlayCanvas.PointerMoved += Canvas_PointerMoved_CanvasDrag;
            OverlayCanvas.PointerReleased += Canvas_PointerReleased_CanvasDrag;
            OverlayCanvas.CapturePointer(e.Pointer);
        }
    }

    private void Canvas_PointerMoved_ToolboxDrag(object sender, PointerRoutedEventArgs e)
    {
        var ptr = e.GetCurrentPoint(MainGrid);
        var canvasPoint = MainGrid.TransformToVisual(OverlayCanvas).TransformPoint(ptr.Position);
        var (offsetX, offsetY) = GetDrawOffset();
        var worldX = canvasPoint.X / ScaleMmToPx - offsetX;
        var worldY = canvasPoint.Y / ScaleMmToPx - offsetY;
        if (_draggedPlaced != null)
        {
            _draggedPlaced = _draggedPlaced.WithPosition(worldX, worldY, _draggedPlaced.RotationDegrees);
            UpdateGhostPosition(canvasPoint.X, canvasPoint.Y);
        }
        UpdatePortHighlights(worldX, worldY);
    }

    private void Canvas_PointerMoved_CanvasDrag(object sender, PointerRoutedEventArgs e)
    {
        var ptr = e.GetCurrentPoint(OverlayCanvas);
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

        var (offsetX, offsetY) = GetDrawOffset();
        UpdatePortHighlights(ptr.Position.X / ScaleMmToPx - offsetX, ptr.Position.Y / ScaleMmToPx - offsetY);
    }

    private void Canvas_PointerReleased_ToolboxDrag(object sender, PointerRoutedEventArgs e)
    {
        MainGrid.PointerMoved -= Canvas_PointerMoved_ToolboxDrag;
        MainGrid.PointerReleased -= Canvas_PointerReleased_ToolboxDrag;
        MainGrid.ReleasePointerCapture(e.Pointer);

        if (_draggedPlaced != null)
        {
            var canvasPoint = MainGrid.TransformToVisual(OverlayCanvas).TransformPoint(e.GetCurrentPoint(MainGrid).Position);
            var (offsetX, offsetY) = GetDrawOffset();
            var xMm = canvasPoint.X / ScaleMmToPx - offsetX;
            var yMm = canvasPoint.Y / ScaleMmToPx - offsetY;
            var placed = _draggedPlaced.WithPosition(xMm, yMm, _draggedPlaced.RotationDegrees);
            TrySnapAndPlace(placed, null);
        }

        ClearGhost();
        ClearPortHighlights();
    }

    private void Canvas_PointerReleased_CanvasDrag(object sender, PointerRoutedEventArgs e)
    {
        OverlayCanvas.PointerMoved -= Canvas_PointerMoved_CanvasDrag;
        OverlayCanvas.PointerReleased -= Canvas_PointerReleased_CanvasDrag;
        OverlayCanvas.ReleasePointerCapture(e.Pointer);

        _isCanvasDragging = false;

        if (_draggedSegmentId.HasValue && _dragHasMoved)
        {
            TrySnapOnDrop(_draggedSegmentId.Value);
        }

        _draggedSegmentId = null;
        _draggedPlaced = null;
        ClearGhost();
        ClearPortHighlights();
        RefreshCanvas();
    }

    private void Canvas_PointerMoved_UpdateCoords(object sender, PointerRoutedEventArgs e)
    {
        var pos = e.GetCurrentPoint(OverlayCanvas).Position;
        var (offsetX, offsetY) = GetDrawOffset();
        var worldX = pos.X / ScaleMmToPx - offsetX;
        var worldY = pos.Y / ScaleMmToPx - offsetY;
        CoordinatesText.Text = $"X: {worldX:F0} mm  Y: {worldY:F0} mm";
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
                _useCachedOffsetForNextRefresh = targetPort != "PortA" && !_plan.Connections.Any(c => c.TargetSegment == targetSegmentId);
                _plan.AddSegment(newPlaced);
                AdjustTargetSegmentForNewEntryPort(targetSegmentId, targetPort);
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

        _useCachedOffsetForNextRefresh = targetPort != "PortA" && !_plan.Connections.Any(c => c.TargetSegment == targetSegmentId);

        // Ganze Gruppe um Snap-Delta verschieben, dann Rotation des gezogenen Segments setzen
        if (_draggingGroup.Count > 1)
            _plan.MoveGroup(_draggingGroup, deltaX, deltaY);

        _plan.UpdateSegmentPosition(movedSegmentId, newPlaced.X, newPlaced.Y, newPlaced.RotationDegrees);
        AdjustTargetSegmentForNewEntryPort(targetSegmentId, targetPort);
        _plan.AddConnection(movedSegmentId, sourcePort, targetSegmentId, targetPort);
    }

    /// <summary>
    /// Passt das Zielsegment an, wenn die erste Verbindung an Port B (oder C/D) erfolgt.
    /// Ohne diese Anpassung würde die Kurve gespiegelt erscheinen, da (X,Y,R) Port A als Ursprung erwartet.
    /// </summary>
    private void AdjustTargetSegmentForNewEntryPort(Guid targetSegmentId, string targetPort)
    {
        if (targetPort == "PortA")
            return;
        var targetHadIncoming = _plan.Connections.Any(c => c.TargetSegment == targetSegmentId);
        if (targetHadIncoming)
            return;
        var target = _plan.Segments.FirstOrDefault(s => s.Segment.No == targetSegmentId);
        if (target == null)
            return;
        var (newX, newY, portAngle) = SegmentPortGeometry.GetPortWorldPosition(target, targetPort);
        // Bei Entry B zeigt die Pfad-Tangente am Ursprung in die entgegengesetzte Richtung (+180°).
        var newR = NormalizeAngle(portAngle - 180);
        _plan.UpdateSegmentPosition(targetSegmentId, newX, newY, newR);
    }

    private (PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort)? FindBestSnap(PlacedSegment placed, Guid? excludeSegmentId)
    {
        const double bestDistThreshold = SnapThresholdMm * 1.5;
        (PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort)? best = null;
        var bestDist = double.MaxValue;

        var myEntryPort = GetEntryPortForSegment(placed.Segment.No);
        var myPorts = SegmentPortGeometry.GetAllPortWorldPositions(placed, myEntryPort);
        var myPortsWithEntry = SegmentPortGeometry.GetPortsWithEntry(placed.Segment, myEntryPort)
            .ToDictionary(p => p.PortName, p => (p.LocalX, p.LocalY, p.LocalAngleDegrees));

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
                        if (!myPortsWithEntry.TryGetValue(myPortName, out var portInfo))
                            continue;
                        var (localX, localY, myLocalAngle) = portInfo;
                        // Tangenten müssen übereinstimmen: our_rotation + myLocalAngle = oAngle.
                        var newRotation = NormalizeAngle(oAngle - myLocalAngle);
                        // Ursprung so setzen, dass unser Port (localX, localY) bei neuer Rotation genau (ox, oy) trifft.
                        var r = newRotation * Math.PI / 180;
                        var cos = Math.Cos(r);
                        var sin = Math.Sin(r);
                        var newOriginX = ox - (localX * cos - localY * sin);
                        var newOriginY = oy - (localX * sin + localY * cos);
                        var newPlaced = placed.WithPosition(newOriginX, newOriginY, newRotation);
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

    private void UpdatePortHighlights(double cursorWorldXmm, double cursorWorldYmm)
    {
        ClearPortHighlights();

        if (!_snapEnabled || _ghostShape == null || _draggedPlaced == null)
            return;

        var (offsetX, offsetY) = GetDrawOffset();
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
                var el = CreatePortIndicator((px + offsetX) * ScaleMmToPx, (py + offsetY) * ScaleMmToPx, highlight);
                OverlayCanvas.Children.Add(el);
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
                var el = CreatePortIndicator((px + offsetX) * ScaleMmToPx, (py + offsetY) * ScaleMmToPx, true);
                OverlayCanvas.Children.Add(el);
                _highlightedPorts.Add(el);
            }
        }
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

    /// <summary>
    /// Berechnet die Bounding-Box aller Segmente in Weltkoordinaten (mm).
    /// </summary>
    private (double MinX, double MinY, double MaxX, double MaxY)? ComputeContentBoundsMm()
    {
        if (_plan.Segments.Count == 0)
            return null;

        var first = _plan.Segments[0];
        var entryPort = GetEntryPortForSegment(first.Segment.No);
        var path = SegmentLocalPathBuilder.GetPath(first.Segment, entryPort);
        var (localMinX, localMinY, localMaxX, localMaxY) = SegmentLocalPathBuilder.GetBounds(path);

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var placed in _plan.Segments)
        {
            entryPort = GetEntryPortForSegment(placed.Segment.No);
            path = SegmentLocalPathBuilder.GetPath(placed.Segment, entryPort);
            (localMinX, localMinY, localMaxX, localMaxY) = SegmentLocalPathBuilder.GetBounds(path);

            var angleRad = placed.RotationDegrees * Math.PI / 180;
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            static double Tx(double ox, double oy, double lx, double ly, double cos, double sin) =>
                ox + lx * cos - ly * sin;
            static double Ty(double ox, double oy, double lx, double ly, double cos, double sin) =>
                oy + lx * sin + ly * cos;

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
    /// Während eines Canvas-Drags bleibt der Offset fix, damit die Verschiebung sichtbar bleibt.
    /// PlanChanged ruft sonst bei jedem MoveGroup RefreshCanvas auf; ein neu berechneter Offset
    /// würde die Bewegung aufheben (neuer Offset = alter Offset − Delta).
    /// </remarks>
    private (double OffsetX, double OffsetY) GetDrawOffset()
    {
        if (_isCanvasDragging)
            return (_cachedDrawOffsetX, _cachedDrawOffsetY);
        if (_useCachedOffsetForNextRefresh)
        {
            _useCachedOffsetForNextRefresh = false;
            return (_cachedDrawOffsetX, _cachedDrawOffsetY);
        }

        var bounds = ComputeContentBoundsMm();
        if (bounds == null)
        {
            _cachedDrawOffsetX = 0;
            _cachedDrawOffsetY = 0;
            return (0, 0);
        }

        var (minX, minY, _, _) = bounds.Value;
        _cachedDrawOffsetX = ContentMarginMm - minX;
        _cachedDrawOffsetY = ContentMarginMm - minY;
        return (_cachedDrawOffsetX, _cachedDrawOffsetY);
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

    private void GraphCanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        var resourceCreator = ds;
        var (offsetX, offsetY) = GetDrawOffset();

        // Stroke-Style wie SegmentPlanPathBuilder: Round Join & Caps
        using var strokeStyle = new CanvasStrokeStyle
        {
            LineJoin = CanvasLineJoin.Round,
            StartCap = CanvasCapStyle.Round,
            EndCap = CanvasCapStyle.Round
        };

        foreach (var placed in _plan.Segments)
        {
            var isSelected = placed.Segment.No == _selectedSegmentId;
            var entryPort = GetEntryPortForSegment(placed.Segment.No);
            var pathCommands = SegmentLocalPathBuilder.GetPath(placed.Segment, entryPort);
            var worldGeometry = PathToCanvasGeometryConverter.ToCanvasGeometryInWorldCoords(
                resourceCreator, pathCommands, placed.X + offsetX, placed.Y + offsetY, placed.RotationDegrees, ScaleMmToPx);

            var strokeWidth = (float)(isSelected ? 10 : 4);
            var color = isSelected
                ? Windows.UI.Color.FromArgb(255, 0, 120, 215)
                : Windows.UI.Color.FromArgb(255, 26, 26, 26);
            ds.DrawGeometry(worldGeometry, color, strokeWidth, strokeStyle);
        }
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

        var (offsetX, offsetY) = GetDrawOffset();
        var pivotDisplayX = (placed.X + offsetX) * ScaleMmToPx;
        var pivotDisplayY = (placed.Y + offsetY) * ScaleMmToPx;
        var ptr = e.GetCurrentPoint(OverlayCanvas);
        var dx = ptr.Position.X - pivotDisplayX;
        var dy = ptr.Position.Y - pivotDisplayY;
        _rotationDragStartAngleRad = Math.Atan2(dy, dx);
        _rotationDragStartSegmentDegrees = placed.RotationDegrees;

        OverlayCanvas.PointerMoved += RotationHandle_PointerMoved;
        OverlayCanvas.PointerReleased += RotationHandle_PointerReleased;
        OverlayCanvas.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void RotationHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_selectedSegmentId == null)
            return;

        var placed = _plan.Segments.FirstOrDefault(s => s.Segment.No == _selectedSegmentId);
        if (placed == null)
            return;

        var (offsetX, offsetY) = GetDrawOffset();
        var pivotDisplayX = (placed.X + offsetX) * ScaleMmToPx;
        var pivotDisplayY = (placed.Y + offsetY) * ScaleMmToPx;
        var ptr = e.GetCurrentPoint(OverlayCanvas);
        var dx = ptr.Position.X - pivotDisplayX;
        var dy = ptr.Position.Y - pivotDisplayY;
        var currentAngleRad = Math.Atan2(dy, dx);
        var deltaRad = currentAngleRad - _rotationDragStartAngleRad;
        var deltaDeg = deltaRad * 180.0 / Math.PI;
        // Im Bildschirm-KS: Uhrzeigersinn = negativer Winkel → Segment soll mitdrehen
        var newRotation = NormalizeAngle(_rotationDragStartSegmentDegrees - deltaDeg);
        _plan.UpdateSegmentPosition(placed.Segment.No, placed.X, placed.Y, newRotation);
    }

    private void RotationHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        OverlayCanvas.PointerMoved -= RotationHandle_PointerMoved;
        OverlayCanvas.PointerReleased -= RotationHandle_PointerReleased;
        OverlayCanvas.ReleasePointerCapture(e.Pointer);
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