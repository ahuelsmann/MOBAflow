// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.TrackPlan.Renderer.Layout;

namespace Moba.TrackPlan.Editor.ViewModel;

/// <summary>
/// Represents a snap preview showing where a track would connect if released.
/// </summary>
public sealed record SnapPreview(
    Guid MovingEdgeId,
    string MovingPortId,
    Guid TargetEdgeId,
    string TargetPortId,
    Point2D MovingPortPosition,
    Point2D TargetPortPosition,
    Point2D PreviewPosition,
    double PreviewRotation);

public sealed record GhostTrackPlacement(
    string TemplateId,
    Point2D Position,
    double RotationDeg);

/// <summary>
/// Represents multiple ghost tracks from a selection drag operation.
/// Stores initial positions and current offsets for all dragged tracks.
/// </summary>
public sealed record MultiGhostPlacement(
    IReadOnlyList<Guid> TrackIds,
    IReadOnlyDictionary<Guid, Point2D> InitialPositions,
    Point2D CurrentOffset);

public sealed class TrackPlanEditorViewModel
{
    /// <summary>
    /// Maximum angle difference (in degrees) for ports to be considered connectable.
    /// Ports must point toward each other (180° apart) within this tolerance.
    /// </summary>
    public const double SnapAngleTolerance = 5.0;

    private readonly ILayoutEngine _layoutEngine;
    private readonly ValidationService _validationService;
    private readonly SerializationService _serializationService;
    private readonly ITrackCatalog _catalog;

    public TopologyGraph Graph { get; }

    public SelectionState Selection { get; } = new();
    public VisibilityState Visibility { get; } = new();
    public EditorViewState ViewState { get; } = new();

    public Dictionary<Guid, Point2D> Positions { get; } = new();
    public Dictionary<Guid, double> Rotations { get; } = new();

    /// <summary>
    /// All sections (logical groups of tracks) in the editor.
    /// </summary>
    public List<Section> Sections { get; } = [];

    /// <summary>
    /// All isolators (electrical breaks) in the topology.
    /// </summary>
    public List<Isolator> Isolators { get; } = [];

    /// <summary>
    /// All endcaps (track terminators) in the topology.
    /// </summary>
    public List<Endcap> Endcaps { get; } = [];

    public IReadOnlyList<ConstraintViolation> Violations { get; private set; } = [];

    private Guid? _selectedTrackId;
    private Guid? _hoveredTrackId;
    private Guid? _draggedTrackId;
    private Point2D _dragOffset;
    private bool _isDragging;
    private HashSet<Guid>? _dragGroup;
    private Dictionary<Guid, Point2D>? _dragGroupOffsets;

    // Multi-selection state
    private bool _isRectangleSelecting;
    private Point2D _selectionStart;
    private Point2D _selectionEnd;

    /// <summary>
    /// Tracks which branch is active for each switch edge.
    /// Key = EdgeId, Value = active branch port id (e.g., "B" or "C" for a 3-way switch).
    /// </summary>
    private readonly Dictionary<Guid, string> _switchBranchStates = new();

    /// <summary>
    /// Cached connection service for graph operations.
    /// </summary>
    private TrackConnectionService? _connectionService;

    /// <summary>
    /// Set of currently selected track IDs for multi-selection.
    /// </summary>
    public HashSet<Guid> SelectedTrackIds { get; } = [];

    /// <summary>
    /// Current ghost placement while dragging a template from the toolbox.
    /// </summary>
    public GhostTrackPlacement? GhostPlacement { get; private set; }

    /// <summary>
    /// Ghost placement for dragging multiple selected tracks.
    /// </summary>
    public MultiGhostPlacement? MultiGhostPlacement { get; private set; }

    /// <summary>
    /// Gets the current selection rectangle during rectangle selection.
    /// Returns null if no rectangle selection is in progress.
    /// </summary>
    public (Point2D Start, Point2D End)? SelectionRectangle =>
        _isRectangleSelecting ? (_selectionStart, _selectionEnd) : null;

    /// <summary>
    /// Current snap preview during drag operation. Null if no snap target is nearby.
    /// </summary>
    public SnapPreview? CurrentSnapPreview { get; private set; }

    public Guid? SelectedTrackId => _selectedTrackId;
    public Guid? HoveredTrackId => _hoveredTrackId;

    /// <summary>
    /// Gets the connection service for this graph.
    /// </summary>
    public TrackConnectionService ConnectionService =>
        _connectionService ??= new TrackConnectionService(Graph);

    public TrackPlanEditorViewModel(ITrackCatalog catalog, params ITopologyConstraint[] constraints)
    {
        _catalog = catalog;
        _validationService = new ValidationService(catalog);
        _serializationService = new SerializationService();

        Graph = new TopologyGraph();
        // Note: constraints are passed in but TopologyGraph.Validate() validates dynamically
        // Store them if needed for later validation calls
    }

    // ------------------------------------------------------------
    // Track Management
    // ------------------------------------------------------------

    public TrackEdge AddTrack(String templateId, Point2D position, double rotationDeg = 0)
    {
        var template = _catalog.GetById(templateId)
            ?? throw new InvalidOperationException($"Unknown template: {templateId}");

        var edge = new TrackEdge(Guid.NewGuid(), templateId);

        foreach (var end in template.Ends)
        {
            var node = new TrackNode(Guid.NewGuid());
            node.Ports.Add(end.Id);
            Graph.AddNode(node);

            edge.Connections[end.Id] = (node.Id, null, null);
        }

        Graph.AddEdge(edge);

        Positions[edge.Id] = position;
        Rotations[edge.Id] = rotationDeg;

        return edge;
    }

    public void RemoveTrack(Guid edgeId)
    {
        var edge = Graph.GetEdge(edgeId);
        if (edge is null) return;

        foreach (var port in edge.Connections.Keys.ToList())
            Disconnect(edgeId, port);

        Graph.RemoveEdge(edgeId);

        Positions.Remove(edgeId);
        Rotations.Remove(edgeId);

        _selectedTrackId = null;
        _hoveredTrackId = null;
        _draggedTrackId = null;
        _isDragging = false;
        _dragGroup = null;
        _dragGroupOffsets = null;
    }

    /// <summary>
    /// Deletes all currently selected tracks and clears selection.
    /// </summary>
    public int DeleteSelectedEdges()
    {
        if (SelectedTrackIds.Count == 0)
            return 0;

        var toDelete = SelectedTrackIds.ToList();
        foreach (var edgeId in toDelete)
            RemoveTrack(edgeId);

        SelectedTrackIds.Clear();
        _selectedTrackId = null;
        _hoveredTrackId = null;
        _isDragging = false;
        _dragGroup = null;
        _dragGroupOffsets = null;

        return toDelete.Count;
    }

    /// <summary>
    /// Disconnects all ports of currently selected tracks from neighbors.
    /// Tracks remain in the graph but connections are severed.
    /// </summary>
    public int DisconnectSelectedEdges()
    {
        if (SelectedTrackIds.Count == 0)
            return 0;

        int disconnectCount = 0;
        var service = ConnectionService;

        foreach (var edgeId in SelectedTrackIds.ToList())
        {
            var edge = Graph.GetEdge(edgeId);
            if (edge is null)
                continue;

            foreach (var portId in edge.Connections.Keys.ToList())
            {
                if (service.IsPortConnected(edgeId, portId))
                {
                    Disconnect(edgeId, portId);
                    disconnectCount++;
                }
            }
        }

        return disconnectCount;
    }

    public void Clear()
    {
        Graph.Clear();
        Positions.Clear();
        Rotations.Clear();
        _selectedTrackId = null;
        _hoveredTrackId = null;
        _draggedTrackId = null;
        _isDragging = false;
        _dragGroup = null;
        _dragGroupOffsets = null;
    }

    // ------------------------------------------------------------
    // Connection / Snap
    // ------------------------------------------------------------

    public bool TryConnect(Guid edgeA, string portA, Guid edgeB, string portB)
        => ConnectionService.TryConnect(edgeA, portA, edgeB, portB);

    public void Disconnect(Guid edgeId, string portId)
        => ConnectionService.Disconnect(edgeId, portId);

    public bool IsPortConnected(Guid edgeId, string portId)
        => ConnectionService.IsPortConnected(edgeId, portId);

    public void SnapEdgeToPort(Guid movingEdgeId, string movingPortId, Guid targetEdgeId, string targetPortId)
    {
        var moving = Graph.Edges.First(e => e.Id == movingEdgeId);
        var target = Graph.Edges.First(e => e.Id == targetEdgeId);

        var movingTemplate = _catalog.GetById(moving.TemplateId)!;
        var targetTemplate = _catalog.GetById(target.TemplateId)!;

        var movingEnd = movingTemplate.Ends.First(e => e.Id == movingPortId);
        var targetEnd = targetTemplate.Ends.First(e => e.Id == targetPortId);

        // For curved tracks in a loop: ports should be COLLINEAR (same direction), not opposite
        // Target port global angle
        double targetGlobalAngle = Rotations.GetValueOrDefault(targetEdgeId, 0) + targetEnd.AngleDeg;

        // For Port A→Port B connection on curves: desired angle is the TARGET angle itself
        // (continuing the curve in the same direction)
        double desiredMovingGlobalAngle = targetGlobalAngle;

        // Adjust rotation so moving port points in desired direction
        double newRotation = NormalizeDeg(desiredMovingGlobalAngle - movingEnd.AngleDeg);
        Rotations[movingEdgeId] = newRotation;

        // Snap position: move so ports align
        var targetPos = GetPortWorldPosition(targetEdgeId, targetPortId);
        var movingOffset = GetPortOffset(movingTemplate, movingPortId, newRotation);

        Positions[movingEdgeId] = new Point2D(
            targetPos.X - movingOffset.X,
            targetPos.Y - movingOffset.Y
        );

        System.Diagnostics.Debug.WriteLine(
            $"SNAP: {moving.TemplateId}.{movingPortId} → {target.TemplateId}.{targetPortId} | " +
            $"TargetAngle: {targetGlobalAngle:F1}° | DesiredAngle: {desiredMovingGlobalAngle:F1}° | NewRot: {newRotation:F1}°");
    }

    private bool TrySnapAndConnect(Guid newEdgeId, double snapDistance, out string? message)
    {
        var service = ConnectionService;
        var newEdge = Graph.Edges.FirstOrDefault(e => e.Id == newEdgeId);
        if (newEdge is null)
        {
            message = null;
            return false;
        }

        var newTemplate = _catalog.GetById(newEdge.TemplateId);
        if (newTemplate is null)
        {
            message = null;
            return false;
        }

        // Find best snap across ALL port combinations
        (Guid newEdgeId, string newPortId, Guid existingEdgeId, string existingPortId, double distance)? bestSnap = null;

        foreach (var newEnd in newTemplate.Ends)
        {
            if (service.IsPortConnected(newEdgeId, newEnd.Id))
                continue;

            var newPortWorldPos = GetPortWorldPosition(newEdgeId, newEnd.Id);

            foreach (var existingEdge in Graph.Edges.Where(e => e.Id != newEdgeId))
            {
                var existingTemplate = _catalog.GetById(existingEdge.TemplateId);
                if (existingTemplate is null) continue;

                foreach (var existingEnd in existingTemplate.Ends)
                {
                    if (service.IsPortConnected(existingEdge.Id, existingEnd.Id))
                        continue;

                    // Port compatibility: A connects to B/C, B connects to A, C connects to A
                    if (!IsPortCompatible(newEnd.Id, existingEnd.Id))
                        continue;

                    var existingPortWorldPos = GetPortWorldPosition(existingEdge.Id, existingEnd.Id);
                    var dx = newPortWorldPos.X - existingPortWorldPos.X;
                    var dy = newPortWorldPos.Y - existingPortWorldPos.Y;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    // Keep best snap across all combinations
                    if (distance < snapDistance && (bestSnap is null || distance < bestSnap.Value.distance))
                    {
                        bestSnap = (newEdgeId, newEnd.Id, existingEdge.Id, existingEnd.Id, distance);
                    }
                }
            }
        }

        if (bestSnap.HasValue)
        {
            var snap = bestSnap.Value;
            SnapEdgeToPort(snap.newEdgeId, snap.newPortId, snap.existingEdgeId, snap.existingPortId);

            if (service.TryConnect(snap.newEdgeId, snap.newPortId, snap.existingEdgeId, snap.existingPortId))
            {
                var targetEdge = Graph.Edges.FirstOrDefault(e => e.Id == snap.existingEdgeId);
                var newE = Graph.Edges.FirstOrDefault(e => e.Id == snap.newEdgeId);
                message = $"Snapped {newE?.TemplateId}.{snap.newPortId} → {targetEdge?.TemplateId}.{snap.existingPortId} ({snap.distance:F1}mm)";
                return true;
            }
        }

        message = null;
        return false;
    }

    private static bool IsPortCompatible(string portA, string portB)
    {
        // A/B/C ports: A connects to B/C, B/C connect to A
        bool isAPort = portA == "A";
        bool isBPort = portB == "B";
        bool isCPort = portB == "C";

        if (isAPort)
            return isBPort || isCPort;

        return portA == "B" || portA == "C";
    }

    // ------------------------------------------------------------
    // Section / Isolator Management
    // ------------------------------------------------------------

    /// <summary>
    /// Creates a new section from the currently selected tracks.
    /// </summary>
    public Section? CreateSectionFromSelection(string name, string color)
    {
        if (SelectedTrackIds.Count == 0)
            return null;

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = name,
            Color = color,
            TrackIds = [.. SelectedTrackIds]
        };

        Sections.Add(section);
        return section;
    }

    /// <summary>
    /// Removes a section. Does not remove the tracks.
    /// </summary>
    public void RemoveSection(Guid sectionId)
    {
        var section = Sections.FirstOrDefault(s => s.Id == sectionId);
        if (section is not null)
            Sections.Remove(section);
    }

    /// <summary>
    /// Gets the section a track belongs to, if any.
    /// </summary>
    public Section? GetSectionForTrack(Guid edgeId)
        => Sections.FirstOrDefault(s => s.TrackIds.Contains(edgeId));

    /// <summary>
    /// Toggles an isolator at a specific port.
    /// </summary>
    public bool ToggleIsolator(Guid edgeId, string portId)
    {
        var existing = Isolators.FirstOrDefault(
            i => i.EdgeId == edgeId && i.PortId == portId);

        if (existing is not null)
        {
            Isolators.Remove(existing);
            return false;
        }

        Isolators.Add(new Isolator
        {
            Id = Guid.NewGuid(),
            EdgeId = edgeId,
            PortId = portId
        });
        return true;
    }

    /// <summary>
    /// Checks if a port has an isolator.
    /// </summary>
    public bool HasIsolator(Guid edgeId, string portId)
        => Isolators.Any(i => i.EdgeId == edgeId && i.PortId == portId);

    /// <summary>
    /// Finds the nearest snap target for a dragged edge within snap distance.
    /// Does not modify any state - only calculates the potential snap.
    /// </summary>
    public SnapPreview? FindNearestSnapTarget(Guid movingEdgeId, double snapDistance)
    {
        var service = ConnectionService;  // Verwende Cache statt new
        var movingEdge = Graph.Edges.FirstOrDefault(e => e.Id == movingEdgeId);
        if (movingEdge is null)
            return null;

        var movingTemplate = _catalog.GetById(movingEdge.TemplateId);
        if (movingTemplate is null)
            return null;

        return FindNearestSnapTarget(
            movingTemplate,
            Positions[movingEdgeId],
            Rotations.GetValueOrDefault(movingEdgeId, 0),
            movingEdgeId,
            snapDistance,
            _dragGroup);
    }

    /// <summary>
    /// Finds the nearest snap target for a ghost placement (using TemplateId and position/rotation).
    /// Called during drag of a new track from toolbox.
    /// </summary>
    private SnapPreview? FindNearestSnapTarget(string templateId, Point2D position, double rotationDeg, double snapDistance)
    {
        var template = _catalog.GetById(templateId);
        if (template is null)
            return null;

        return FindNearestSnapTarget(
            template,
            position,
            rotationDeg,
            movingEdgeId: null,
            snapDistance,
            excludedEdges: null);
    }

    private SnapPreview? FindNearestSnapTarget(
        TrackTemplate movingTemplate,
        Point2D movingPosition,
        double movingRotationDeg,
        Guid? movingEdgeId,
        double snapDistance,
        HashSet<Guid>? excludedEdges)
    {
        var service = ConnectionService;

        (Guid targetEdgeId, string targetPortId, string movingPortId, double distance)? best = null;

        bool allowFreeRotation = !movingEdgeId.HasValue;

        foreach (var movingEnd in movingTemplate.Ends)
        {
            if (movingEdgeId.HasValue && service.IsPortConnected(movingEdgeId.Value, movingEnd.Id))
                continue;

            var movingPortPos = GetPortWorldPosition(movingTemplate, movingPosition, movingRotationDeg, movingEnd.Id);

            foreach (var targetEdge in Graph.Edges.Where(e => !movingEdgeId.HasValue || e.Id != movingEdgeId.Value))
            {
                if (excludedEdges?.Contains(targetEdge.Id) == true)
                    continue;

                var targetTemplate = _catalog.GetById(targetEdge.TemplateId);
                if (targetTemplate is null) continue;

                foreach (var targetEnd in targetTemplate.Ends)
                {
                    if (service.IsPortConnected(targetEdge.Id, targetEnd.Id))
                        continue;

                    var targetPortPos = GetPortWorldPosition(targetEdge.Id, targetEnd.Id);
                    var dx = movingPortPos.X - targetPortPos.X;
                    var dy = movingPortPos.Y - targetPortPos.Y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);

                    if (dist >= snapDistance)
                        continue;

                    if (!allowFreeRotation)
                    {
                        // Check angle compatibility only when rotation is fixed (existing track)
                        double movingAngle = movingRotationDeg + movingEnd.AngleDeg;
                        double targetAngle = Rotations.GetValueOrDefault(targetEdge.Id, 0) + targetEnd.AngleDeg;
                        double angleDiff = Math.Abs(NormalizeDeg(movingAngle - targetAngle - 180.0));
                        if (angleDiff > 180.0)
                            angleDiff = 360.0 - angleDiff;

                        if (angleDiff > SnapAngleTolerance)
                            continue;
                    }

                    if (best is null || dist < best.Value.distance)
                    {
                        best = (targetEdge.Id, targetEnd.Id, movingEnd.Id, dist);
                    }
                }
            }
        }

        if (!best.HasValue)
            return null;

        var targetPos = GetPortWorldPosition(best.Value.targetEdgeId, best.Value.targetPortId);
        var targetEdgeObj = Graph.Edges.First(e => e.Id == best.Value.targetEdgeId);
        var targetTemplateObj = _catalog.GetById(targetEdgeObj.TemplateId)!;
        var targetEndObj = targetTemplateObj.Ends.First(e => e.Id == best.Value.targetPortId);

        var movingEndObj = movingTemplate.Ends.First(e => e.Id == best.Value.movingPortId);

        double targetGlobalAngle = Rotations.GetValueOrDefault(best.Value.targetEdgeId, 0) + targetEndObj.AngleDeg;
        double desiredMovingGlobalAngle = targetGlobalAngle + 180.0;
        double previewRotation = NormalizeDeg(desiredMovingGlobalAngle - movingEndObj.AngleDeg);

        var movingOffset = GetPortOffset(movingTemplate, best.Value.movingPortId, previewRotation);
        var previewPosition = new Point2D(
            targetPos.X - movingOffset.X,
            targetPos.Y - movingOffset.Y);

        var currentMovingPortPos = GetPortWorldPosition(movingTemplate, movingPosition, movingRotationDeg, best.Value.movingPortId);

        return new SnapPreview(
            MovingEdgeId: movingEdgeId ?? Guid.Empty,
            MovingPortId: best.Value.movingPortId,
            TargetEdgeId: best.Value.targetEdgeId,
            TargetPortId: best.Value.targetPortId,
            MovingPortPosition: currentMovingPortPos,
            TargetPortPosition: targetPos,
            PreviewPosition: previewPosition,
            PreviewRotation: previewRotation);
    }

    // ------------------------------------------------------------
    // Auto Layout
    // ------------------------------------------------------------

    public void ApplyAutoLayout()
    {
        // LayoutEngine liefert nur Positionen
        var layoutPositions = _layoutEngine.Layout(Graph);

        foreach (var kvp in layoutPositions)
        {
            Positions[kvp.Key] = kvp.Value;

            // Rotation bleibt, wie sie ist – wenn nicht vorhanden, 0°
            if (!Rotations.ContainsKey(kvp.Key))
                Rotations[kvp.Key] = 0.0;
        }
    }

    // ------------------------------------------------------------
    // Interaction API
    // ------------------------------------------------------------

    public Guid? HitTest(Point2D position, double hitRadius)
    {
        double bestDistance = hitRadius;
        Guid? bestEdge = null;

        foreach (var edge in Graph.Edges)
        {
            if (!Positions.TryGetValue(edge.Id, out var pos))
                continue;

            var template = _catalog.GetById(edge.TemplateId);
            if (template is null)
                continue;

            var rot = Rotations.GetValueOrDefault(edge.Id, 0.0);
            var dist = GetDistanceToTrack(template, pos, rot, position);

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestEdge = edge.Id;
            }
        }

        return bestEdge;
    }

    private static double GetDistanceToTrack(TrackTemplate template, Point2D position, double rotationDeg, Point2D point)
    {
        IEnumerable<IGeometryPrimitive> primitives = template.Geometry.GeometryKind switch
        {
            TrackGeometryKind.Straight => StraightGeometry.Render(template, position, rotationDeg),
            TrackGeometryKind.Curve => CurveGeometry.Render(template, position, rotationDeg),
            TrackGeometryKind.Switch => SwitchGeometry.Render(template, position, rotationDeg),
            _ => Array.Empty<IGeometryPrimitive>()
        };

        double best = double.MaxValue;
        foreach (var primitive in primitives)
        {
            var dist = DistanceToPrimitive(primitive, point);
            if (dist < best)
                best = dist;
        }
        return best;
    }

    private static double DistanceToPrimitive(IGeometryPrimitive primitive, Point2D point)
    {
        if (primitive is LinePrimitive line)
        {
            return DistancePointToSegment(point, line.From, line.To);
        }

        if (primitive is ArcPrimitive arc)
        {
            // sample arc into small segments for distance calculation
            int segments = Math.Max(12, (int)Math.Ceiling(Math.Abs(arc.SweepAngleRad * 180 / Math.PI / 5)));
            Point2D? prev = null;
            double best = double.MaxValue;
            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = arc.StartAngleRad + t * arc.SweepAngleRad;
                var p = new Point2D(
                    arc.Center.X + arc.Radius * Math.Cos(angle),
                    arc.Center.Y + arc.Radius * Math.Sin(angle));
                if (prev is not null)
                    best = Math.Min(best, DistancePointToSegment(point, prev.Value, p));
                prev = p;
            }
            return best;
        }

        return double.MaxValue;
    }

    private static double DistancePointToSegment(Point2D p, Point2D a, Point2D b)
    {
        var ab = new Point2D(b.X - a.X, b.Y - a.Y);
        var ap = new Point2D(p.X - a.X, p.Y - a.Y);
        double abLenSq = ab.X * ab.X + ab.Y * ab.Y;
        if (abLenSq == 0)
            return Math.Sqrt(ap.X * ap.X + ap.Y * ap.Y);
        double t = (ap.X * ab.X + ap.Y * ab.Y) / abLenSq;
        t = Math.Clamp(t, 0, 1);
        var closest = new Point2D(a.X + ab.X * t, a.Y + ab.Y * t);
        double dx = p.X - closest.X;
        double dy = p.Y - closest.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public void PointerDown(Point2D position, bool isLeftButton, bool isCtrlPressed = false)
    {
        if (!isLeftButton)
            return;

        var hit = HitTest(position, hitRadius: 60.0);
        _hoveredTrackId = hit;

        if (hit.HasValue)
        {
            if (isCtrlPressed)
            {
                // Ctrl+Click: toggle selection
                if (SelectedTrackIds.Contains(hit.Value))
                    SelectedTrackIds.Remove(hit.Value);
                else
                    SelectedTrackIds.Add(hit.Value);
            }
            else
            {
                // Normal click: single selection and start drag
                if (!SelectedTrackIds.Contains(hit.Value))
                {
                    SelectedTrackIds.Clear();
                    SelectedTrackIds.Add(hit.Value);
                }
            }

            _selectedTrackId = hit;
            _draggedTrackId = hit.Value;
            var pos = Positions[hit.Value];
            _dragOffset = new Point2D(position.X - pos.X, position.Y - pos.Y);
            _isDragging = true;

            // Find all connected edges and calculate their offsets relative to drag start
            var service = ConnectionService;
            _dragGroup = service.GetConnectedGroup(hit.Value);
            _dragGroupOffsets = new Dictionary<Guid, Point2D>();

            foreach (var edgeId in _dragGroup)
            {
                if (Positions.TryGetValue(edgeId, out var edgePos))
                {
                    _dragGroupOffsets[edgeId] = new Point2D(
                        edgePos.X - pos.X,
                        edgePos.Y - pos.Y);
                }
            }
        }
        else
        {
            // Click on empty space: start rectangle selection
            if (!isCtrlPressed)
                SelectedTrackIds.Clear();

            _selectedTrackId = null;
            _draggedTrackId = null;
            _isDragging = false;
            _dragGroup = null;
            _dragGroupOffsets = null;

            // Start rectangle selection
            _isRectangleSelecting = true;
            _selectionStart = position;
            _selectionEnd = position;
        }
    }

    public void PointerMove(Point2D position, bool gridSnap, double gridSize, double snapDistance = 30.0)
    {
        // Rectangle selection update
        if (_isRectangleSelecting)
        {
            _selectionEnd = position;
            return;
        }

        if (_isDragging && _draggedTrackId.HasValue && _dragGroupOffsets is not null)
        {
            var newPos = new Point2D(position.X - _dragOffset.X, position.Y - _dragOffset.Y);

            if (gridSnap)
            {
                newPos = new Point2D(
                    Math.Round(newPos.X / gridSize) * gridSize,
                    Math.Round(newPos.Y / gridSize) * gridSize);
            }

            // Move all connected edges together
            foreach (var (edgeId, offset) in _dragGroupOffsets)
            {
                Positions[edgeId] = new Point2D(
                    newPos.X + offset.X,
                    newPos.Y + offset.Y);
            }

            // Update snap preview for visual feedback
            CurrentSnapPreview = FindNearestSnapTarget(_draggedTrackId.Value, snapDistance);
            return;
        }

        if (GhostPlacement is not null)
            return;

        CurrentSnapPreview = null;
        var hit = HitTest(position, hitRadius: 60.0);
        _hoveredTrackId = hit;
    }

    public string DropTrack(string templateId, Point2D position, bool gridSnap, double gridSize, bool snapEnabled, double snapDistance, out TrackEdge createdEdge, double? rotationDegOverride = null, SnapPreview? snapPreview = null)
    {
        if (gridSnap)
        {
            position = new Point2D(
                Math.Round(position.X / gridSize) * gridSize,
                Math.Round(position.Y / gridSize) * gridSize);
        }

        var rotation = rotationDegOverride ?? 0.0;
        createdEdge = AddTrack(templateId, position, rotationDeg: rotation);
        _selectedTrackId = createdEdge.Id;
        _hoveredTrackId = createdEdge.Id;

        string status = $"Placed {templateId} at ({position.X:F0}, {position.Y:F0})";

        if (snapEnabled)
        {
            // Prefer preview calculated during drag
            var preview = snapPreview ?? CurrentSnapPreview;
            if (preview is not null)
            {
                Positions[createdEdge.Id] = preview.PreviewPosition;
                Rotations[createdEdge.Id] = preview.PreviewRotation;

                if (ConnectionService.TryConnect(
                    createdEdge.Id, preview.MovingPortId,
                    preview.TargetEdgeId, preview.TargetPortId))
                {
                    var targetEdge = Graph.Edges.FirstOrDefault(e => e.Id == preview.TargetEdgeId);
                    status = $"Connected {templateId}.{preview.MovingPortId} → {targetEdge?.TemplateId}.{preview.TargetPortId}";
                }
            }
            else if (TrySnapAndConnect(createdEdge.Id, snapDistance, out var msg) && msg is not null)
            {
                status = msg;
            }
        }

        CurrentSnapPreview = null;
        GhostPlacement = null;
        return status;
    }

    public void BeginGhostPlacement(string templateId)
    {
        GhostPlacement = new GhostTrackPlacement(templateId, new Point2D(0, 0), 0);
        CurrentSnapPreview = null;
    }

    public void UpdateGhostPlacement(Point2D position, bool gridSnap, double gridSize, double snapDistance, bool snapEnabled)
    {
        if (GhostPlacement is null)
            return;

        if (gridSnap)
        {
            position = new Point2D(
                Math.Round(position.X / gridSize) * gridSize,
                Math.Round(position.Y / gridSize) * gridSize);
        }

        GhostPlacement = GhostPlacement with { Position = position };

        CurrentSnapPreview = snapEnabled
            ? FindNearestSnapTarget(GhostPlacement.TemplateId, position, GhostPlacement.RotationDeg, snapDistance)
            : null;
    }

    public (Point2D Position, double RotationDeg, SnapPreview? Preview)? CommitGhostPlacement(bool snapEnabled, double snapDistance)
    {
        if (GhostPlacement is null)
            return null;

        var placement = GhostPlacement;
        SnapPreview? preview = null;

        if (snapEnabled)
        {
            preview = CurrentSnapPreview ?? FindNearestSnapTarget(placement.TemplateId, placement.Position, placement.RotationDeg, snapDistance);
            if (preview is not null)
            {
                placement = placement with
                {
                    Position = preview.PreviewPosition,
                    RotationDeg = preview.PreviewRotation
                };
            }
        }

        GhostPlacement = null;
        CurrentSnapPreview = null;
        return (placement.Position, placement.RotationDeg, preview);
    }

    public void CancelGhostPlacement()
    {
        GhostPlacement = null;
        CurrentSnapPreview = null;
    }

    public void BeginMultiGhostPlacement(IReadOnlyList<Guid> trackIds)
    {
        if (trackIds.Count == 0)
            return;

        var initialPositions = new Dictionary<Guid, Point2D>();
        foreach (var trackId in trackIds)
        {
            if (Positions.TryGetValue(trackId, out var pos))
                initialPositions[trackId] = pos;
        }

        MultiGhostPlacement = new MultiGhostPlacement(trackIds, initialPositions, new Point2D(0, 0));
        CurrentSnapPreview = null;
    }

    public void UpdateMultiGhostPlacement(Point2D newOffset, bool gridSnap, double gridSize)
    {
        if (MultiGhostPlacement is null)
            return;

        var offset = newOffset;

        if (gridSnap)
        {
            offset = new Point2D(
                Math.Round(offset.X / gridSize) * gridSize,
                Math.Round(offset.Y / gridSize) * gridSize);
        }

        MultiGhostPlacement = MultiGhostPlacement with { CurrentOffset = offset };
    }

    public void CommitMultiGhostPlacement()
    {
        if (MultiGhostPlacement is null)
            return;

        // Offset wurde bereits in PointerMove auf Positionen angewendet
        MultiGhostPlacement = null;
        CurrentSnapPreview = null;
    }

    public void CancelMultiGhostPlacement()
    {
        if (MultiGhostPlacement is null)
            return;

        // Restore original positions
        foreach (var trackId in MultiGhostPlacement.TrackIds)
        {
            if (MultiGhostPlacement.InitialPositions.TryGetValue(trackId, out var originalPos))
                Positions[trackId] = originalPos;
        }

        MultiGhostPlacement = null;
        CurrentSnapPreview = null;
    }

    /// <summary>
    /// Finds nearest snap target for the first track in the multi-ghost selection.
    /// (First iteration: snap primary track; can be extended to snap all connected ports)
    /// </summary>
    public SnapPreview? FindNearestSnapTargetForMulti(IReadOnlyList<Guid> trackIds, double snapDistance)
    {
        if (trackIds.Count == 0)
            return null;

        // For now, snap the first track in selection
        // TODO: Extended version could consider all ports of all tracks
        return FindNearestSnapTarget(trackIds[0], snapDistance);
    }

    /// <summary>
    /// Updates CurrentSnapPreview for multi-selection drag (called during PointerMove).
    /// Sets CurrentSnapPreview if a valid snap target is found.
    /// </summary>
    public void FindAndSetSnapPreviewForMulti(IReadOnlyList<Guid> trackIds, double snapDistance)
    {
        CurrentSnapPreview = FindNearestSnapTargetForMulti(trackIds, snapDistance);
    }

    // ------------------------------------------------------------
    // Validation & Serialization
    // ------------------------------------------------------------

    public void Validate()
    {
        Violations = _validationService.Validate(Graph);
    }

    public string Serialize()
        => _serializationService.Serialize(Graph);

    public void LoadFromJson(string json)
    {
        var loaded = _serializationService.Deserialize(json);

        Graph.Clear();

        foreach (var n in loaded.Nodes) Graph.AddNode(n);
        foreach (var e in loaded.Edges) Graph.AddEdge(e);

        // Endcaps are managed separately in ViewModel
        Endcaps.Clear();
        // Note: If Endcaps need to be serialized, extend SerializationService

        Positions.Clear();
        Rotations.Clear();
        ClearSelection();
    }

    // ------------------------------------------------------------
    // Geometry Helpers
    // ------------------------------------------------------------

    private Point2D GetPortWorldPosition(Guid edgeId, string portId)
    {
        var edge = Graph.Edges.First(e => e.Id == edgeId);
        var template = _catalog.GetById(edge.TemplateId)!;

        var pos = Positions[edgeId];
        var rot = Rotations[edgeId];

        var offset = GetPortOffset(template, portId, rot);
        return new Point2D(pos.X + offset.X, pos.Y + offset.Y);
    }

    private static Point2D GetPortWorldPosition(TrackTemplate template, Point2D position, double rotationDeg, string portId)
    {
        var offset = GetPortOffset(template, portId, rotationDeg);
        return new Point2D(position.X + offset.X, position.Y + offset.Y);
    }

    private static Point2D GetPortOffset(TrackTemplate template, string portId, double rotationDeg)
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
            double sweep = spec.AngleDeg!.Value * Math.PI / 180.0;

            if (portId == "A") return Rot(new Point2D(0, 0), rotRad);

            var end = new Point2D(
                radius * Math.Sin(sweep),
                radius - radius * Math.Cos(sweep)
            );
            return Rot(end, rotRad);
        }

        if (spec.GeometryKind == TrackGeometryKind.Switch)
        {
            double length = spec.LengthMm!.Value;
            double radius = spec.RadiusMm!.Value;
            double sweep = spec.AngleDeg!.Value * Math.PI / 180.0;
            double junction = spec.JunctionOffsetMm ?? (length / 2.0);

            if (portId == "A") return Rot(new Point2D(0, 0), rotRad);
            if (portId == "B") return Rot(new Point2D(length, 0), rotRad);

            var j = new Point2D(junction, 0);
            var center = new Point2D(j.X, j.Y + radius);
            double startAngle = Math.Atan2(j.Y - center.Y, j.X - center.X);

            var end = new Point2D(
                center.X + radius * Math.Cos(startAngle + sweep),
                center.Y + radius * Math.Sin(startAngle + sweep)
            );

            return Rot(end, rotRad);
        }

        return new Point2D(0, 0);
    }

    private static double NormalizeDeg(double angle)
    {
        angle %= 360.0;
        if (angle < 0) angle += 360.0;
        return angle;
    }

    public string PointerUp(Point2D position, bool snapEnabled, double snapDistance, bool gridSnap, double gridSize, bool isCtrlPressed = false)
    {
        string status = "Ready";

        // Handle rectangle selection
        if (_isRectangleSelecting)
        {
            var selectedInRect = GetEdgesInRectangle(_selectionStart, _selectionEnd);

            if (isCtrlPressed)
            {
                foreach (var id in selectedInRect)
                    SelectedTrackIds.Add(id);
            }
            else
            {
                SelectedTrackIds.Clear();
                foreach (var id in selectedInRect)
                    SelectedTrackIds.Add(id);
            }

            _isRectangleSelecting = false;
            status = selectedInRect.Count > 0
                ? $"Selected {selectedInRect.Count} track(s)"
                : "Ready";
            return status;
        }

        if (_isDragging && _draggedTrackId.HasValue)
        {
            // Use the snap preview calculated during drag (most accurate nearest-port selection)
            if (snapEnabled && CurrentSnapPreview is not null)
            {
                var snap = CurrentSnapPreview;
                SnapEdgeToPort(snap.MovingEdgeId, snap.MovingPortId, snap.TargetEdgeId, snap.TargetPortId);

                if (ConnectionService.TryConnect(
                    snap.MovingEdgeId, snap.MovingPortId,
                    snap.TargetEdgeId, snap.TargetPortId))
                {
                    var movingEdge = Graph.Edges.FirstOrDefault(e => e.Id == snap.MovingEdgeId);
                    var targetEdge = Graph.Edges.FirstOrDefault(e => e.Id == snap.TargetEdgeId);
                    status = $"Connected {movingEdge?.TemplateId}.{snap.MovingPortId} → {targetEdge?.TemplateId}.{snap.TargetPortId}";
                }
            }

            var pos = Positions[_draggedTrackId.Value];
            var edge = Graph.Edges.First(e => e.Id == _draggedTrackId.Value);
            if (status == "Ready")
                status = $"Placed {edge.TemplateId} at ({pos.X:F0}, {pos.Y:F0})";
        }

        _isDragging = false;
        _draggedTrackId = null;
        _dragGroup = null;
        _dragGroupOffsets = null;
        CurrentSnapPreview = null;
        return status;
    }

    private List<Guid> GetEdgesInRectangle(Point2D start, Point2D end)
    {
        var minX = Math.Min(start.X, end.X);
        var maxX = Math.Max(start.X, end.X);
        var minY = Math.Min(start.Y, end.Y);
        var maxY = Math.Max(start.Y, end.Y);

        var result = new List<Guid>();
        foreach (var (edgeId, pos) in Positions)
        {
            if (pos.X >= minX && pos.X <= maxX && pos.Y >= minY && pos.Y <= maxY)
                result.Add(edgeId);
        }
        return result;
    }

    public (TrackTemplate Template, Point2D Position, double RotationDeg)? GetCurrentDragPreviewPose()
    {
        if (!_draggedTrackId.HasValue)
            return null;

        if (CurrentSnapPreview is null)
            return null;

        var edge = Graph.Edges.FirstOrDefault(e => e.Id == _draggedTrackId.Value);
        if (edge is null)
            return null;

        var template = _catalog.GetById(edge.TemplateId);
        if (template is null)
            return null;

        return (template, CurrentSnapPreview.PreviewPosition, CurrentSnapPreview.PreviewRotation);
    }

    public void FindAndSetSnapPreview(Guid trackId, double snapDistance)
    {
        CurrentSnapPreview = FindNearestSnapTarget(trackId, snapDistance);
    }

    public void ClearSnapPreview()
    {
        CurrentSnapPreview = null;
    }

    public void ClearSelection()
    {
        _selectedTrackId = null;
        _hoveredTrackId = null;
        _draggedTrackId = null;
        _isDragging = false;
        _dragGroup = null;
        _dragGroupOffsets = null;
        _isRectangleSelecting = false;
        GhostPlacement = null;
        MultiGhostPlacement = null;
        CurrentSnapPreview = null;
        SelectedTrackIds.Clear();
    }

    /// <summary>
    /// Selects all tracks connected to the specified track (for triple-click).
    /// </summary>
    public void SelectConnectedGroup(Guid edgeId)
    {
        var group = ConnectionService.GetConnectedGroup(edgeId);

        SelectedTrackIds.Clear();
        foreach (var id in group)
            SelectedTrackIds.Add(id);

        _selectedTrackId = edgeId;
    }

    /// <summary>
    /// Selects all tracks along the shortest path between two tracks (for Shift+Click).
    /// </summary>
    public void SelectPathBetween(Guid fromEdgeId, Guid toEdgeId)
    {
        var path = ConnectionService.FindShortestPath(fromEdgeId, toEdgeId);

        if (path.Count == 0)
            return;

        foreach (var id in path)
            SelectedTrackIds.Add(id);

        _selectedTrackId = toEdgeId;
    }

    /// <summary>
    /// Toggles the active branch for a switch track (for double-click).
    /// </summary>
    public void ToggleSwitchBranch(Guid edgeId)
    {
        var edge = Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null) return;

        var template = _catalog.GetById(edge.TemplateId);
        if (template?.Geometry.GeometryKind != TrackGeometryKind.Switch) return;

        // Get all branch ports (typically B, C, etc. - excluding the common A port)
        var branchPorts = template.Ends
            .Where(e => e.Id != "A")
            .Select(e => e.Id)
            .ToList();

        if (branchPorts.Count < 2) return;

        // Get current branch or default to first
        if (!_switchBranchStates.TryGetValue(edgeId, out var currentBranch))
        {
            currentBranch = branchPorts[0];
        }

        // Cycle to next branch
        var currentIndex = branchPorts.IndexOf(currentBranch);
        var nextIndex = (currentIndex + 1) % branchPorts.Count;
        _switchBranchStates[edgeId] = branchPorts[nextIndex];
    }

    /// <summary>
    /// Gets the currently active branch for a switch track.
    /// </summary>
    public string? GetActiveSwitchBranch(Guid edgeId) =>
        _switchBranchStates.TryGetValue(edgeId, out var branch) ? branch : null;
}