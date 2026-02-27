namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Editable track plan model with placed segments and connections.
/// Supports drag and drop, snapping, and group movement.
/// </summary>
public sealed class EditableTrackPlan
{
    private readonly List<PlacedSegment> _segments = [];
    private readonly List<PortConnection> _connections = [];

    /// <summary>All placed track segments.</summary>
    public IReadOnlyList<PlacedSegment> Segments => _segments;

    /// <summary>All port connections between segments.</summary>
    public IReadOnlyList<PortConnection> Connections => _connections;

    /// <summary>Event raised when segments or connections change.</summary>
    public event EventHandler? PlanChanged;

    /// <summary>Adds a new placed segment.</summary>
    public void AddSegment(PlacedSegment placed)
    {
        if (_segments.Any(s => s.Segment.No == placed.Segment.No))
            return;

        _segments.Add(placed);
        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes a segment and all associated connections.</summary>
    public void RemoveSegment(Guid segmentNo)
    {
        _segments.RemoveAll(s => s.Segment.No == segmentNo);
        _connections.RemoveAll(c => c.SourceSegment == segmentNo || c.TargetSegment == segmentNo);
        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Updates the position of a segment.</summary>
    public void UpdateSegmentPosition(Guid segmentNo, double x, double y, double rotationDegrees)
    {
        var idx = _segments.FindIndex(s => s.Segment.No == segmentNo);
        if (idx < 0)
            return;

        var old = _segments[idx];
        _segments[idx] = old.WithPosition(x, y, rotationDegrees);
        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Moves all segments in the connected group by the specified delta.</summary>
    public void MoveGroup(IReadOnlySet<Guid> segmentNos, double deltaX, double deltaY)
    {
        for (var i = 0; i < _segments.Count; i++)
        {
            if (!segmentNos.Contains(_segments[i].Segment.No))
                continue;

            var p = _segments[i];
            _segments[i] = p.WithPosition(p.X + deltaX, p.Y + deltaY, p.RotationDegrees);
        }

        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Adds a connection between two ports. Existing connections for the affected ports are removed.</summary>
    public void AddConnection(Guid sourceSegment, string sourcePort, Guid targetSegment, string targetPort)
    {
        RemoveConnectionsForPort(sourceSegment, sourcePort);
        RemoveConnectionsForPort(targetSegment, targetPort);

        _connections.Add(new PortConnection(sourceSegment, sourcePort, targetSegment, targetPort));

        // Set mutual port references in the segment
        var src = _segments.FirstOrDefault(s => s.Segment.No == sourceSegment)?.Segment;
        var tgt = _segments.FirstOrDefault(s => s.Segment.No == targetSegment)?.Segment;
        if (src != null && tgt != null)
        {
            SetPortValue(src, sourcePort, tgt.No);
            SetPortValue(tgt, targetPort, src.No);
        }

        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Disconnects a segment from the group – removes all connections to this segment without deleting the segment.</summary>
    public void DisconnectSegmentFromGroup(Guid segmentNo)
    {
        var toRemove = _connections
            .Where(c => c.SourceSegment == segmentNo || c.TargetSegment == segmentNo)
            .ToList();
        foreach (var c in toRemove)
        {
            RemoveConnection(c.SourceSegment, c.SourcePort, c.TargetSegment, c.TargetPort);
        }
    }

    /// <summary>Removes a connection.</summary>
    public void RemoveConnection(Guid sourceSegment, string sourcePort, Guid targetSegment, string targetPort)
    {
        _connections.RemoveAll(c =>
            c.SourceSegment == sourceSegment && c.SourcePort == sourcePort &&
            c.TargetSegment == targetSegment && c.TargetPort == targetPort);

        var src = _segments.FirstOrDefault(s => s.Segment.No == sourceSegment)?.Segment;
        var tgt = _segments.FirstOrDefault(s => s.Segment.No == targetSegment)?.Segment;
        if (src != null)
            SetPortValue(src, sourcePort, null);
        if (tgt != null)
            SetPortValue(tgt, targetPort, null);

        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Loads a track plan from placements and connections (e.g. from TrackPlanSvgRenderer.Render).
    /// </summary>
    public void LoadFromPlacements(IReadOnlyList<PlacedSegment> placements, IReadOnlyList<PortConnection> connections)
    {
        _segments.Clear();
        _connections.Clear();

        foreach (var placed in placements)
            _segments.Add(placed);

        foreach (var conn in connections)
        {
            _connections.Add(conn);
            var src = _segments.FirstOrDefault(s => s.Segment.No == conn.SourceSegment)?.Segment;
            var tgt = _segments.FirstOrDefault(s => s.Segment.No == conn.TargetSegment)?.Segment;
            if (src != null && tgt != null)
            {
                SetPortValue(src, conn.SourcePort, tgt.No);
                SetPortValue(tgt, conn.TargetPort, src.No);
            }
        }

        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ermittelt alle Segment-IDs, die mit dem angegebenen verbunden sind (transitiv).</summary>
    public IReadOnlySet<Guid> GetConnectedGroup(Guid segmentNo)
    {
        var result = new HashSet<Guid> { segmentNo };
        var queue = new Queue<Guid>(result);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var c in _connections)
            {
                Guid? neighbor = null;
                if (c.SourceSegment == current)
                    neighbor = c.TargetSegment;
                else if (c.TargetSegment == current)
                    neighbor = c.SourceSegment;

                if (neighbor.HasValue && result.Add(neighbor.Value))
                    queue.Enqueue(neighbor.Value);
            }
        }

        return result;
    }

    private void RemoveConnectionsForPort(Guid segmentNo, string portName)
    {
        _connections.RemoveAll(c =>
            (c.SourceSegment == segmentNo && c.SourcePort == portName) ||
            (c.TargetSegment == segmentNo && c.TargetPort == portName));

        var seg = _segments.FirstOrDefault(s => s.Segment.No == segmentNo)?.Segment;
        if (seg != null)
            SetPortValue(seg, portName, null);
    }

    private static void SetPortValue(Segment segment, string portName, Guid? value)
    {
        var prop = segment.GetType().GetProperty(portName);
        prop?.SetValue(segment, value);
    }
}