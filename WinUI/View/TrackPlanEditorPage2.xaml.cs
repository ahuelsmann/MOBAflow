// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Service;
using Moba.TrackPlan.TrackSystem;

using System.Collections.ObjectModel;
using System.Globalization;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

/// <summary>
/// TrackPlanEditorPage2 - Full-featured track plan editor with drag and drop,
/// snap-to-connect, selection, properties panel, and theme-aware rendering.
/// </summary>
public sealed partial class TrackPlanEditorPage2 : Page
{
    // Display scale: 1mm = 0.5px (so G231 = 115px length)
    private const double DisplayScale = 0.5;
    private const double SnapDistance = 30.0;
    private const double GridSize = 50.0;
    private const double TrackWidth = 6.0;
    private const double PortRadius = 8.0;
    private const double HitTestRadius = 40.0;

    private readonly TrackPlanEditorViewModel2 _viewModel;
    private readonly ITrackCatalog _trackCatalog = new PikoATrackCatalog();
    private readonly TrackConnectionService _connectionService;

    // Track positions (in canvas coordinates)
    private readonly Dictionary<Guid, Point> _trackPositions = [];
    private readonly Dictionary<Guid, double> _trackRotations = [];

    // Drag state for tracks
    private Guid? _draggedTrackId;
    private Point _dragOffset;
    private bool _isDragging;

    // Pan state (right-click drag)
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    // Hover state
    private Guid? _hoveredTrackId;

    // Selection state
    private Guid? _selectedTrackId;

    // Theme colors (resolved at runtime)
    private SolidColorBrush _trackBrush = null!;
    private SolidColorBrush _trackSelectedBrush = null!;
    private SolidColorBrush _portOpenBrush = null!;
    private SolidColorBrush _portConnectedBrush = null!;
    private SolidColorBrush _gridBrush = null!;
    private SolidColorBrush _feedbackBrush = null!;

    public ObservableCollection<ConstraintViolation> Violations { get; } = [];

    // Toolbox data sources (from track catalog)
    public IReadOnlyList<TrackTemplate> StraightTemplates => _trackCatalog.Straights.ToList();
    public IReadOnlyList<TrackTemplate> CurveTemplates => _trackCatalog.Curves.ToList();
    public IReadOnlyList<TrackTemplate> SwitchTemplates => _trackCatalog.Switches.ToList();


    public TrackPlanEditorPage2(TrackPlanEditorViewModel2 viewModel)
    {
        _connectionService = new TrackConnectionService(viewModel.Graph);
        _viewModel = viewModel;
        InitializeComponent();

        Loaded += OnLoaded;
        ActualThemeChanged += OnThemeChanged;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateThemeColors();
        UpdateStatistics();
        RenderGraph();
        
        // Ensure page can receive keyboard input
        Focus(FocusState.Programmatic);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.R:
                // R = Remove connections (disconnect)
                DisconnectSelectedTrack();
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.D:
            case Windows.System.VirtualKey.Delete:
                // D or Delete = Delete selected track
                if (_selectedTrackId.HasValue)
                {
                    DeleteSelected_Click(sender, e);
                    e.Handled = true;
                }
                break;

            case Windows.System.VirtualKey.Escape:
                // Escape = Clear selection
                ClearSelection();
                e.Handled = true;
                break;
        }
    }

    private void OnThemeChanged(FrameworkElement sender, object args)
    {
        UpdateThemeColors();
        RenderGraph();
    }

    private void UpdateThemeColors()
    {
        var isDark = ActualTheme == ElementTheme.Dark ||
                     (ActualTheme == ElementTheme.Default && App.Current.RequestedTheme == ApplicationTheme.Dark);

        if (isDark)
        {
            _trackBrush = new SolidColorBrush(Colors.Silver);
            _trackSelectedBrush = new SolidColorBrush(Colors.DodgerBlue);
            _portOpenBrush = new SolidColorBrush(Colors.Orange);
            _portConnectedBrush = new SolidColorBrush(Colors.LimeGreen);
            _gridBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            _feedbackBrush = new SolidColorBrush(Colors.Red);
        }
        else
        {
            _trackBrush = new SolidColorBrush(Colors.DimGray);
            _trackSelectedBrush = new SolidColorBrush(Colors.Blue);
            _portOpenBrush = new SolidColorBrush(Colors.DarkOrange);
            _portConnectedBrush = new SolidColorBrush(Colors.Green);
            _gridBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
            _feedbackBrush = new SolidColorBrush(Colors.DarkRed);
        }
    }

    private void UpdateStatistics()
    {
        NodeCountText.Text = _viewModel.Graph.Nodes.Count.ToString();
        EdgeCountText.Text = _viewModel.Graph.Edges.Count.ToString();
        EndcapCountText.Text = _viewModel.Graph.Endcaps.Count.ToString();
        ZoomLevelText.Text = string.Format(CultureInfo.InvariantCulture, "{0:P0}", CanvasScrollViewer.ZoomFactor);
    }

    private void UpdatePropertiesPanel()
    {
        if (_selectedTrackId is null)
        {
            SelectionInfoText.Text = "No selection";
            PropertiesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == _selectedTrackId);
        if (edge is null)
        {
            SelectionInfoText.Text = "Track not found";
            PropertiesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        SelectionInfoText.Text = $"Selected: {edge.TemplateId}";
        PropertiesPanel.Visibility = Visibility.Visible;

        TrackIdTextBox.Text = edge.Id.ToString()[..8];
        TemplateIdTextBox.Text = edge.TemplateId ?? "Unknown";

        if (_trackPositions.TryGetValue(edge.Id, out var pos))
        {
            PositionXBox.Value = pos.X;
            PositionYBox.Value = pos.Y;
        }

        if (_trackRotations.TryGetValue(edge.Id, out var rot))
        {
            RotationBox.Value = rot;
        }

        FeedbackPointBox.Value = edge.FeedbackPointNumber ?? double.NaN;
    }

    // Toolbox Drag Start (legacy - kept for ListView compatibility)
    private void Toolbox_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is TrackTemplate template)
        {
            e.Data.SetText(template.Id);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            StatusText.Text = $"Dragging {template.Id}...";
        }
    }

    /// <summary>
    /// WinUI 3 Drag & Drop Best Practice:
    /// 1. Set CanDrag="True" in XAML (before any pointer events)
    /// 2. Handle DragStarting event to set DataPackage
    /// 3. PointerPressed is only for visual feedback, not for initiating drag
    /// </summary>
    private void ToolboxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Visual feedback only - drag is handled by WinUI via CanDrag + DragStarting
        if (sender is FrameworkElement { DataContext: TrackTemplate template })
        {
            StatusText.Text = $"Click: {template.Id}";
        }
    }

    /// <summary>
    /// DragStarting is called by WinUI when CanDrag="True" and user starts dragging.
    /// This is the ONLY place to set the DataPackage content.
    /// </summary>
    private void Element_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is FrameworkElement { DataContext: TrackTemplate template })
        {
            // Set data for the drag operation
            args.Data.SetText(template.Id);
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            
            // Optional: Allow drag to continue even if source window loses focus
            args.AllowedOperations = DataPackageOperation.Copy;
            
            StatusText.Text = $"Dragging {template.Id}...";
        }
        else
        {
            // Cancel drag if no valid template
            args.Cancel = true;
        }
    }

    /// <summary>
    /// Called when drag operation completes (success or cancel).
    /// </summary>
    private void Element_DropCompleted(UIElement sender, DropCompletedEventArgs args)
    {
        StatusText.Text = args.DropResult == DataPackageOperation.Copy
            ? "Drop completed"
            : "Drop cancelled";
    }

    // Toolbox Item Hover Enter
    private void ToolboxItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (SolidColorBrush)Application.Current.Resources["SubtleFillColorTertiaryBrush"];
        }
    }



    // Toolbox Item Hover Exit
    private void ToolboxItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (SolidColorBrush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        }
    }

    // Canvas Drag Over
    private void GraphCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop to place track";
        e.DragUIOverride.IsCaptionVisible = true;
    }

    // Canvas Drop
    private async void GraphCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text))
            return;


        var templateId = await e.DataView.GetTextAsync();
        var template = _trackCatalog.GetById(templateId);
        if (template is null)
            return;

        var position = e.GetPosition(GraphCanvas);

        // Snap to grid if enabled
        if (GridToggle.IsChecked == true)
        {
            position = new Point(
                Math.Round(position.X / GridSize) * GridSize,
                Math.Round(position.Y / GridSize) * GridSize);
        }

        // Create edge
        var edge = new TrackEdge
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Connections = []
        };

        // Create nodes for each end
        foreach (var end in template.Ends)
        {
            var node = new TrackNode
            {
                Id = Guid.NewGuid(),
                Ports = [new TrackPort { Id = end.Id }]
            };
            _viewModel.AddNode(node);
            edge.Connections[end.Id] = new Endpoint(node.Id, end.Id);
        }


        _viewModel.AddEdge(edge);
        _trackPositions[edge.Id] = position;
        _trackRotations[edge.Id] = 0;

        // Try snap to nearby ports and connect
        if (SnapToggle.IsChecked == true)
        {
            TrySnapAndConnect(edge.Id);
        }

        StatusText.Text = $"Placed {templateId} at ({position.X:F0}, {position.Y:F0})";

        UpdateStatistics();
        RenderGraph();
        SelectTrack(edge.Id);
    }

    /// <summary>
    /// Attempts to snap and connect a new edge to nearby open ports.
    /// </summary>
    private void TrySnapAndConnect(Guid newEdgeId)
    {
        var newEdge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == newEdgeId);
        if (newEdge is null) return;



        var newTemplate = _trackCatalog.GetById(newEdge.TemplateId);
        if (newTemplate is null) return;

        // Check each port of the new edge
        foreach (var newEnd in newTemplate.Ends)
        {
            // Skip if already connected
            if (_connectionService.IsPortConnected(newEdgeId, newEnd.Id))
                continue;

            var newPortWorldPos = GetPortWorldPosition(newEdgeId, newEnd.Id);

            // Find nearest open port on other edges
            (Guid edgeId, string portId, double distance)? nearestPort = null;

            foreach (var existingEdge in _viewModel.Graph.Edges.Where(e => e.Id != newEdgeId))
            {
                var existingTemplate = _trackCatalog.GetById(existingEdge.TemplateId);
                if (existingTemplate is null) continue;

                foreach (var existingEnd in existingTemplate.Ends)
                {
                    // Skip if already connected
                    if (_connectionService.IsPortConnected(existingEdge.Id, existingEnd.Id))
                        continue;

                    var existingPortWorldPos = GetPortWorldPosition(existingEdge.Id, existingEnd.Id);
                    var distance = Math.Sqrt(
                        Math.Pow(newPortWorldPos.X - existingPortWorldPos.X, 2) +
                        Math.Pow(newPortWorldPos.Y - existingPortWorldPos.Y, 2));

                    if (distance < SnapDistance && (nearestPort is null || distance < nearestPort.Value.distance))
                    {
                        nearestPort = (existingEdge.Id, existingEnd.Id, distance);
                    }
                }
            }

            // Connect to nearest port if found
            if (nearestPort.HasValue)
            {
                // Snap position
                SnapEdgeToPort(newEdgeId, newEnd.Id, nearestPort.Value.edgeId, nearestPort.Value.portId);

                // Create logical connection
                if (_connectionService.TryConnect(newEdgeId, newEnd.Id, nearestPort.Value.edgeId, nearestPort.Value.portId))
                {
                    var targetEdge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == nearestPort.Value.edgeId);
                    StatusText.Text = $"Connected {newEdge.TemplateId}.{newEnd.Id} to {targetEdge?.TemplateId}.{nearestPort.Value.portId}";
                    return; // Only connect one port per drop
                }
            }
        }
    }

    /// <summary>
    /// Gets the world position of a port (canvas coordinates).
    /// </summary>
    private Point GetPortWorldPosition(Guid edgeId, string portId)
    {
        if (!_trackPositions.TryGetValue(edgeId, out var pos))
            return new Point(0, 0);

        var rotation = _trackRotations.GetValueOrDefault(edgeId, 0);
        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        var template = _trackCatalog.GetById(edge?.TemplateId);
        var offset = GetPortOffset(portId, template, rotation);

        return new Point(pos.X + offset.X, pos.Y + offset.Y);
    }

    /// <summary>
    /// Gets the outward angle of a port in degrees (direction the port "points").
    /// </summary>
    private double GetPortAngle(Guid edgeId, string portId)
    {
        var rotation = _trackRotations.GetValueOrDefault(edgeId, 0);
        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        var template = _trackCatalog.GetById(edge?.TemplateId);

        if (template is null) return rotation;

        // Port A points "backward" (180° from track direction)
        // Port B points "forward" (0° from track direction)
        // Port C points at diverge angle
        return portId switch
        {
            "A" => rotation + 180,
            "B" => rotation,
            "C" when template.Geometry.GeometryKind == TrackGeometryKind.Switch =>
                rotation + (template.Id.Contains('L') ? (template.Geometry.AngleDeg ?? 15) : -(template.Geometry.AngleDeg ?? 15)),
            _ => rotation
        };
    }

    /// <summary>
    /// Snaps an edge so that its port aligns with a target port, including rotation.
    /// </summary>
    private void SnapEdgeToPort(Guid edgeId, string portId, Guid targetEdgeId, string targetPortId)
    {
        // Calculate required rotation: ports must face opposite directions
        var targetPortAngle = GetPortAngle(targetEdgeId, targetPortId);
        var currentPortAngle = GetPortAngle(edgeId, portId);

        // The new edge's port should face opposite to the target port
        // So: newPortAngle = targetPortAngle + 180
        // And: newPortAngle = rotation + portLocalAngle
        // For port A: portLocalAngle = 180, so rotation = targetPortAngle + 180 - 180 = targetPortAngle
        // For port B: portLocalAngle = 0, so rotation = targetPortAngle + 180
        var requiredRotation = portId switch
        {
            "A" => targetPortAngle,  // Port A is at 180° from center, needs to face opposite of target
            "B" => targetPortAngle + 180,  // Port B is at 0° from center
            _ => targetPortAngle + 180
        };

        // Normalize to 0-360
        while (requiredRotation < 0) requiredRotation += 360;
        while (requiredRotation >= 360) requiredRotation -= 360;

        _trackRotations[edgeId] = requiredRotation;

        // Now calculate position with new rotation
        var targetPortPos = GetPortWorldPosition(targetEdgeId, targetPortId);
        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        var template = _trackCatalog.GetById(edge?.TemplateId);
        var portOffset = GetPortOffset(portId, template, requiredRotation);

        // Calculate new center position so port aligns with target
        var newCenter = new Point(
            targetPortPos.X - portOffset.X,
            targetPortPos.Y - portOffset.Y);

        _trackPositions[edgeId] = newCenter;
    }

    /// <summary>
    /// Gets all edges connected to a given edge (recursively - the whole connected group).
    /// </summary>
    private HashSet<Guid> GetConnectedGroup(Guid startEdgeId)
    {
        var group = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(startEdgeId);

        while (queue.Count > 0)
        {
            var edgeId = queue.Dequeue();
            if (group.Contains(edgeId)) continue;
            group.Add(edgeId);

            var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
            if (edge is null) continue;

            // Find all edges connected to this edge's ports
            foreach (var portId in edge.Connections.Keys)
            {
                var connectedPort = _connectionService.GetConnectedPort(edgeId, portId);
                if (connectedPort.HasValue && !group.Contains(connectedPort.Value.EdgeId))
                {
                    queue.Enqueue(connectedPort.Value.EdgeId);
                }
            }
        }

        return group;
    }

    /// <summary>
    /// Moves a connected group of edges by a delta.
    /// </summary>
    private void MoveConnectedGroup(Guid edgeId, Point delta)
    {
        var group = GetConnectedGroup(edgeId);
        foreach (var id in group)
        {
            if (_trackPositions.TryGetValue(id, out var pos))
            {
                _trackPositions[id] = new Point(pos.X + delta.X, pos.Y + delta.Y);
            }
        }
    }

    // Canvas Pointer Events
    private void GraphCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(GraphCanvas);
        var position = point.Position;

        // Right-click: Start panning
        if (point.Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _panStartPoint = position;
            _panStartHorizontalOffset = CanvasScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = CanvasScrollViewer.VerticalOffset;
            GraphCanvas.CapturePointer(e.Pointer);
            StatusText.Text = "Panning...";
            return;
        }


        // Left-click: Check if clicking on a track
        var clickedTrackId = FindTrackAtPosition(position);

        if (clickedTrackId.HasValue)
        {
            SelectTrack(clickedTrackId.Value);

            if (point.Properties.IsLeftButtonPressed)
            {
                // Start dragging track
                _isDragging = true;
                _draggedTrackId = clickedTrackId.Value;
                if (_trackPositions.TryGetValue(clickedTrackId.Value, out var trackPos))
                {
                    _dragOffset = new Point(position.X - trackPos.X, position.Y - trackPos.Y);
                }
                GraphCanvas.CapturePointer(e.Pointer);
                StatusText.Text = $"Dragging {_viewModel.Graph.Edges.FirstOrDefault(ed => ed.Id == clickedTrackId.Value)?.TemplateId}...";
            }
        }
        else
        {
            // Clicked on empty space - deselect
            ClearSelection();
        }
    }

    private void GraphCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(GraphCanvas);
        var position = point.Position;

        // Update coordinates display
        CoordinatesText.Text = string.Format(CultureInfo.InvariantCulture, "X: {0:F0}  Y: {1:F0}", position.X, position.Y);

        // Panning with right mouse button
        if (_isPanning)
        {
            var deltaX = _panStartPoint.X - position.X;
            var deltaY = _panStartPoint.Y - position.Y;
            CanvasScrollViewer.ChangeView(
                _panStartHorizontalOffset + deltaX,
                _panStartVerticalOffset + deltaY,
                null,
                disableAnimation: true);
            return;
        }

        // Track dragging - moves the whole connected group
        if (_isDragging && _draggedTrackId.HasValue)
        {
            var newPos = new Point(position.X - _dragOffset.X, position.Y - _dragOffset.Y);

            // Snap to grid if enabled
            if (GridToggle.IsChecked == true)
            {
                newPos = new Point(
                    Math.Round(newPos.X / GridSize) * GridSize,
                    Math.Round(newPos.Y / GridSize) * GridSize);
            }

            // Calculate delta and move the whole connected group
            if (_trackPositions.TryGetValue(_draggedTrackId.Value, out var oldPos))
            {
                var delta = new Point(newPos.X - oldPos.X, newPos.Y - oldPos.Y);
                MoveConnectedGroup(_draggedTrackId.Value, delta);
            }
            else
            {
                _trackPositions[_draggedTrackId.Value] = newPos;
            }

            RenderGraph();
            UpdatePropertiesPanel();
        }

        // Hover detection (when not dragging or panning)
        if (!_isDragging && !_isPanning)
        {
            var hoveredId = FindTrackAtPosition(position);
            if (hoveredId != _hoveredTrackId)
            {
                _hoveredTrackId = hoveredId;
                RenderGraph();
            }
        }
    }

    private void GraphCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // End panning
        if (_isPanning)
        {
            _isPanning = false;
            GraphCanvas.ReleasePointerCaptures();
            StatusText.Text = "Ready";
            return;
        }

        // End track dragging
        if (_isDragging && _draggedTrackId.HasValue)
        {
            if (SnapToggle.IsChecked == true)
            {
                TrySnapAndConnect(_draggedTrackId.Value);
                RenderGraph();
            }
            
            if (_trackPositions.TryGetValue(_draggedTrackId.Value, out var pos))
            {
                var edge = _viewModel.Graph.Edges.FirstOrDefault(ed => ed.Id == _draggedTrackId.Value);
                StatusText.Text = $"Placed {edge?.TemplateId} at ({pos.X:F0}, {pos.Y:F0})";
            }
            else
            {
                StatusText.Text = "Track placed";
            }
        }


        _isDragging = false;
        _draggedTrackId = null;
        GraphCanvas.ReleasePointerCaptures();
    }



    private void GraphCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(GraphCanvas);
        var delta = point.Properties.MouseWheelDelta;

        // Ctrl+Wheel = Zoom (handled by ScrollViewer, but we update UI)
        if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            // Let ScrollViewer handle zoom
            e.Handled = false;
        }
        else
        {
            // Without Ctrl: scroll vertically
            e.Handled = false;
        }

        UpdateStatistics();
    }

    private Guid? FindTrackAtPosition(Point position)
    {
        foreach (var (trackId, trackPos) in _trackPositions)
        {
            var distance = Math.Sqrt(
                Math.Pow(position.X - trackPos.X, 2) +
                Math.Pow(position.Y - trackPos.Y, 2));

            if (distance < HitTestRadius)
            {
                return trackId;
            }
        }

        return null;
    }

    private void SelectTrack(Guid trackId)
    {
        _selectedTrackId = trackId;
        UpdatePropertiesPanel();
        RenderGraph();
    }

    private void ClearSelection()
    {
        _selectedTrackId = null;
        UpdatePropertiesPanel();
        RenderGraph();
    }

    // Properties Panel Events
    private void PositionBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_selectedTrackId is null || double.IsNaN(args.NewValue)) return;

        if (_trackPositions.TryGetValue(_selectedTrackId.Value, out var pos))
        {
            if (sender == PositionXBox)
                _trackPositions[_selectedTrackId.Value] = new Point(args.NewValue, pos.Y);
            else if (sender == PositionYBox)
                _trackPositions[_selectedTrackId.Value] = new Point(pos.X, args.NewValue);

            RenderGraph();
        }
    }

    private void RotationBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_selectedTrackId is null || double.IsNaN(args.NewValue)) return;

        _trackRotations[_selectedTrackId.Value] = args.NewValue;
        RenderGraph();
    }

    private void FeedbackPointBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_selectedTrackId is null) return;

        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == _selectedTrackId);
        if (edge is null) return;

        edge.FeedbackPointNumber = double.IsNaN(args.NewValue) ? null : (int)args.NewValue;
        RenderGraph();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTrackId is null) return;

        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == _selectedTrackId);
        if (edge is not null)
        {
            // Disconnect all ports first
            foreach (var portId in edge.Connections.Keys.ToList())
            {
                _connectionService.Disconnect(edge.Id, portId);
            }

            _viewModel.RemoveEdge(edge.Id);
            _trackPositions.Remove(edge.Id);
            _trackRotations.Remove(edge.Id);
        }

        ClearSelection();
        UpdateStatistics();
        StatusText.Text = "Track deleted";
    }

    /// <summary>
    /// Disconnects all ports of the selected track from other tracks.
    /// </summary>
    private void DisconnectSelected_Click(object sender, RoutedEventArgs e)
    {
        DisconnectSelectedTrack();
    }

    private void DisconnectSelectedTrack()
    {
        if (_selectedTrackId is null) return;

        var edge = _viewModel.Graph.Edges.FirstOrDefault(e => e.Id == _selectedTrackId);
        if (edge is null) return;

        var disconnectedCount = 0;
        foreach (var portId in edge.Connections.Keys.ToList())
        {
            if (_connectionService.IsPortConnected(edge.Id, portId))
            {
                _connectionService.Disconnect(edge.Id, portId);
                disconnectedCount++;
            }
        }

        if (disconnectedCount > 0)
        {
            StatusText.Text = $"Disconnected {disconnectedCount} port(s) from {edge.TemplateId}";
            RenderGraph();
        }
        else
        {
            StatusText.Text = $"{edge.TemplateId} has no connections";
        }
    }

    // Toolbar Events
    private void NewPlan_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Clear();
        _trackPositions.Clear();
        _trackRotations.Clear();
        ClearSelection();
        UpdateStatistics();
        RenderGraph();
        StatusText.Text = "New plan created";
    }

    private void LoadPlan_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Load not yet implemented";
    }

    private void SavePlan_Click(object sender, RoutedEventArgs e)
    {
        var json = _viewModel.Serialize();
        StatusText.Text = $"Serialized {_viewModel.Graph.Edges.Count} tracks";
    }

    private void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        Violations.Clear();
        foreach (var violation in _viewModel.Validate())
        {
            Violations.Add(violation);
        }

        StatusText.Text = Violations.Count == 0
            ? "Validation passed"
            : $"Found {Violations.Count} issue(s)";
    }


    private void ZoomFit_Click(object sender, RoutedEventArgs e)
    {
        // Calculate bounds and fit
        if (_trackPositions.Count == 0)
        {
            CanvasScrollViewer.ChangeView(0, 0, 1.0f);
            return;
        }

        var minX = _trackPositions.Values.Min(p => p.X) - 100;
        var maxX = _trackPositions.Values.Max(p => p.X) + 100;
        var minY = _trackPositions.Values.Min(p => p.Y) - 100;
        var maxY = _trackPositions.Values.Max(p => p.Y) + 100;

        var contentWidth = maxX - minX;
        var contentHeight = maxY - minY;

        var zoomX = CanvasScrollViewer.ViewportWidth / contentWidth;
        var zoomY = CanvasScrollViewer.ViewportHeight / contentHeight;
        var zoom = (float)Math.Min(zoomX, zoomY);
        zoom = Math.Clamp(zoom, 0.1f, 2.0f);

        CanvasScrollViewer.ChangeView(minX, minY, zoom);
        UpdateStatistics();
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        CanvasScrollViewer.ChangeView(null, null, 1.0f);
        UpdateStatistics();
    }

    // Rendering
    private void RenderGraph()
    {
        GraphCanvas.Children.Clear();

        // Draw grid if enabled
        if (GridToggle.IsChecked == true)
        {
            RenderGrid();
        }

        // Render all tracks
        foreach (var edge in _viewModel.Graph.Edges)
        {
            if (!_trackPositions.TryGetValue(edge.Id, out var position))
                continue;

            var rotation = _trackRotations.GetValueOrDefault(edge.Id, 0);
            var isSelected = edge.Id == _selectedTrackId;

            RenderTrack(edge, position, rotation, isSelected);
        }
    }

    private void RenderGrid()
    {
        var canvasWidth = GraphCanvas.Width;
        var canvasHeight = GraphCanvas.Height;

        // Vertical lines
        for (double x = 0; x <= canvasWidth; x += GridSize)
        {
            var line = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = canvasHeight,
                Stroke = _gridBrush,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(line);
        }

        // Horizontal lines
        for (double y = 0; y <= canvasHeight; y += GridSize)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = canvasWidth,
                Y2 = y,
                Stroke = _gridBrush,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(line);
        }
    }

    private void RenderTrack(TrackEdge edge, Point position, double rotation, bool isSelected)
    {
        var template = _trackCatalog.GetById(edge.TemplateId ?? "G231");
        var isHovered = edge.Id == _hoveredTrackId;
        var brush = isSelected ? _trackSelectedBrush : (isHovered ? _trackSelectedBrush : _trackBrush);
        var strokeWidth = isSelected ? TrackWidth + 3 : (isHovered ? TrackWidth + 1 : TrackWidth);

        switch (template?.Geometry.GeometryKind)
        {
            case TrackGeometryKind.Straight:
                RenderStraightTrack(position, rotation, brush, strokeWidth, template);
                break;

            case TrackGeometryKind.Curve:
                RenderCurveTrack(position, rotation, brush, strokeWidth, template);
                break;

            case TrackGeometryKind.Switch:
                RenderSwitchTrack(position, rotation, brush, strokeWidth, template);
                break;

            default:
                RenderStraightTrack(position, rotation, brush, strokeWidth, null);
                break;
        }

        // Render ports with connection status
        foreach (var end in template?.Ends ?? [])
        {
            var portOffset = GetPortOffset(end.Id, template, rotation);
            var isConnected = _connectionService.IsPortConnected(edge.Id, end.Id);
            RenderPort(position.X + portOffset.X, position.Y + portOffset.Y, isConnected);
        }

        // Render feedback point
        if (edge.FeedbackPointNumber.HasValue)
        {
            RenderFeedbackPoint(position, edge.FeedbackPointNumber.Value);
        }

        // Selection highlight
        if (isSelected)
        {
            var highlight = new Ellipse
            {
                Width = 50,
                Height = 50,
                Stroke = _trackSelectedBrush,
                StrokeThickness = 2,
                StrokeDashArray = [2, 2],
                Fill = new SolidColorBrush(Color.FromArgb(30, 0, 120, 215))
            };
            Canvas.SetLeft(highlight, position.X - 40);
            Canvas.SetTop(highlight, position.Y - 40);
            highlight.Width = 80;
            highlight.Height = 80;
            GraphCanvas.Children.Add(highlight);
        }
    }

    private void RenderStraightTrack(Point position, double rotation, SolidColorBrush brush, double strokeWidth, TrackTemplate? template)
    {
        // Use actual track length from template, scaled for display
        var lengthMm = template?.Geometry.LengthMm ?? 231.0;
        var length = lengthMm * DisplayScale;
        var radians = rotation * Math.PI / 180;

        var x1 = position.X - (length / 2) * Math.Cos(radians);
        var y1 = position.Y - (length / 2) * Math.Sin(radians);
        var x2 = position.X + (length / 2) * Math.Cos(radians);
        var y2 = position.Y + (length / 2) * Math.Sin(radians);

        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = brush,
            StrokeThickness = strokeWidth,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        GraphCanvas.Children.Add(line);

        // Draw track label
        if (template != null)
        {
            var label = new TextBlock
            {
                Text = template.Id,
                FontSize = 10,
                Foreground = brush,
                Opacity = 0.7
            };
            Canvas.SetLeft(label, position.X - 12);
            Canvas.SetTop(label, position.Y - 20);
            GraphCanvas.Children.Add(label);
        }
    }

    private void RenderCurveTrack(Point position, double rotation, SolidColorBrush brush, double strokeWidth, TrackTemplate template)
    {
        // Use GetPortOffset to get consistent port positions
        var portAOffset = GetPortOffset("A", template, rotation);
        var portBOffset = GetPortOffset("B", template, rotation);

        var portAX = position.X + portAOffset.X;
        var portAY = position.Y + portAOffset.Y;
        var portBX = position.X + portBOffset.X;
        var portBY = position.Y + portBOffset.Y;

        var radius = (template.Geometry.RadiusMm ?? 360) * DisplayScale;
        var angleDeg = template.Geometry.AngleDeg ?? 30;

        // Draw the arc from Port A to Port B
        var figure = new PathFigure { StartPoint = new Point(portAX, portAY) };
        var arc = new ArcSegment
        {
            Point = new Point(portBX, portBY),
            Size = new Windows.Foundation.Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = angleDeg > 180
        };
        figure.Segments.Add(arc);

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        var path = new Path
        {
            Data = geometry,
            Stroke = brush,
            StrokeThickness = strokeWidth,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        GraphCanvas.Children.Add(path);

        // Draw track label at position
        var label = new TextBlock
        {
            Text = template.Id,
            FontSize = 10,
            Foreground = brush,
            Opacity = 0.7
        };
        Canvas.SetLeft(label, position.X - 8);
        Canvas.SetTop(label, position.Y - 20);
        GraphCanvas.Children.Add(label);
    }



    private void RenderSwitchTrack(Point position, double rotation, SolidColorBrush brush, double strokeWidth, TrackTemplate template)
    {
        // G231 length as base for switches
        var length = 231.0 * DisplayScale;
        var radians = rotation * Math.PI / 180;
        var divergeAngle = (template.Geometry.AngleDeg ?? 15) * Math.PI / 180;

        // Main straight
        var x1 = position.X - (length / 2) * Math.Cos(radians);
        var y1 = position.Y - (length / 2) * Math.Sin(radians);
        var x2 = position.X + (length / 2) * Math.Cos(radians);
        var y2 = position.Y + (length / 2) * Math.Sin(radians);

        var mainLine = new Line
        {
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            Stroke = brush,
            StrokeThickness = strokeWidth,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        GraphCanvas.Children.Add(mainLine);

        // Diverging branch
        var branchLength = length * 0.7;
        var isLeft = template.Id.Contains('L');
        var branchRadians = radians + (isLeft ? divergeAngle : -divergeAngle);

        var bx2 = position.X + branchLength * Math.Cos(branchRadians);
        var by2 = position.Y + branchLength * Math.Sin(branchRadians);

        var branchLine = new Line
        {
            X1 = position.X, Y1 = position.Y, X2 = bx2, Y2 = by2,
            Stroke = brush,
            StrokeThickness = strokeWidth - 1,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        GraphCanvas.Children.Add(branchLine);

        // Draw track label
        var label = new TextBlock
        {
            Text = template.Id,
            FontSize = 10,
            Foreground = brush,
            Opacity = 0.7
        };
        Canvas.SetLeft(label, position.X - 12);
        Canvas.SetTop(label, position.Y - 25);
        GraphCanvas.Children.Add(label);
    }

    private void RenderPort(double x, double y, bool isConnected)
    {
        var port = new Ellipse
        {
            Width = PortRadius * 2,
            Height = PortRadius * 2,
            Fill = isConnected ? _portConnectedBrush : _portOpenBrush,
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };
        Canvas.SetLeft(port, x - PortRadius);
        Canvas.SetTop(port, y - PortRadius);
        GraphCanvas.Children.Add(port);
    }

    private void RenderFeedbackPoint(Point position, int number)
    {
        var indicator = new Ellipse
        {
            Width = 16,
            Height = 16,
            Fill = _feedbackBrush
        };
        Canvas.SetLeft(indicator, position.X - 8);
        Canvas.SetTop(indicator, position.Y + 20);
        GraphCanvas.Children.Add(indicator);

        var label = new TextBlock
        {
            Text = number.ToString(),
            FontSize = 10,
            Foreground = new SolidColorBrush(Colors.White),
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        Canvas.SetLeft(label, position.X - 4);
        Canvas.SetTop(label, position.Y + 22);
        GraphCanvas.Children.Add(label);
    }

    private Point GetPortOffset(string portId, TrackTemplate? template, double rotation)
    {
        if (template is null)
        {
            // Default straight track offset
            var defaultLength = 231.0 / 2 * DisplayScale;
            var defaultRadians = rotation * Math.PI / 180;
            return portId switch
            {
                "A" => new Point(-defaultLength * Math.Cos(defaultRadians), -defaultLength * Math.Sin(defaultRadians)),
                "B" => new Point(defaultLength * Math.Cos(defaultRadians), defaultLength * Math.Sin(defaultRadians)),
                _ => new Point(0, 0)
            };
        }

        var radians = rotation * Math.PI / 180;

        switch (template.Geometry.GeometryKind)
        {
            case TrackGeometryKind.Straight:
                var lengthMm = template.Geometry.LengthMm ?? 231.0;
                var length = (lengthMm / 2) * DisplayScale;
                return portId switch
                {
                    "A" => new Point(-length * Math.Cos(radians), -length * Math.Sin(radians)),
                    "B" => new Point(length * Math.Cos(radians), length * Math.Sin(radians)),
                    _ => new Point(0, 0)
                };

            case TrackGeometryKind.Curve:
                // Curve geometry - must match RenderCurveTrack exactly
                // Port A is at entry (tangent = rotation direction)
                // Port B is at exit (tangent = rotation + angle direction)
                var curveRadius = (template.Geometry.RadiusMm ?? 360) * DisplayScale;
                var curveAngleDeg = template.Geometry.AngleDeg ?? 30;
                var curveAngleRad = curveAngleDeg * Math.PI / 180;

                // Arc center is perpendicular to entry direction
                var arcCenterOffsetX = curveRadius * Math.Sin(radians);
                var arcCenterOffsetY = -curveRadius * Math.Cos(radians);

                // Port A angle from arc center
                var portAAngleFromCenter = radians - Math.PI / 2 + curveAngleRad / 2;
                // Port B angle from arc center  
                var portBAngleFromCenter = radians - Math.PI / 2 - curveAngleRad / 2;

                // Port positions relative to track center (position)
                // First find where ports are in world coords, then subtract position
                var portAOffsetX = arcCenterOffsetX + curveRadius * Math.Cos(portAAngleFromCenter);
                var portAOffsetY = arcCenterOffsetY + curveRadius * Math.Sin(portAAngleFromCenter);
                var portBOffsetX = arcCenterOffsetX + curveRadius * Math.Cos(portBAngleFromCenter);
                var portBOffsetY = arcCenterOffsetY + curveRadius * Math.Sin(portBAngleFromCenter);

                // Adjust so midpoint of arc is at position (0,0)
                var midOffsetX = (portAOffsetX + portBOffsetX) / 2;
                var midOffsetY = (portAOffsetY + portBOffsetY) / 2;

                return portId switch
                {
                    "A" => new Point(portAOffsetX - midOffsetX, portAOffsetY - midOffsetY),
                    "B" => new Point(portBOffsetX - midOffsetX, portBOffsetY - midOffsetY),
                    _ => new Point(0, 0)
                };

            case TrackGeometryKind.Switch:
                var switchLength = 231.0 / 2 * DisplayScale;
                var divergeAngle = (template.Geometry.AngleDeg ?? 15) * Math.PI / 180;
                var isLeft = template.Id.Contains('L');
                var branchRadians = radians + (isLeft ? divergeAngle : -divergeAngle);
                return portId switch
                {
                    "A" => new Point(-switchLength * Math.Cos(radians), -switchLength * Math.Sin(radians)),
                    "B" => new Point(switchLength * Math.Cos(radians), switchLength * Math.Sin(radians)),
                    "C" => new Point(switchLength * 0.7 * Math.Cos(branchRadians), switchLength * 0.7 * Math.Sin(branchRadians)),
                    _ => new Point(0, 0)
                };

            default:
                return new Point(0, 0);
        }
    }
}



