namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Editierbares Trackplan-Modell mit platzierter Segmente und Verbindungen.
/// Unterstützt Drag &amp; Drop, Snapping und Gruppen-Bewegung.
/// </summary>
public sealed class EditableTrackPlan
{
    private readonly List<PlacedSegment> _segments = [];
    private readonly List<PortConnection> _connections = [];

    /// <summary>Alle platzierter Gleissegmente.</summary>
    public IReadOnlyList<PlacedSegment> Segments => _segments;

    /// <summary>Alle Port-Verbindungen zwischen Segmenten.</summary>
    public IReadOnlyList<PortConnection> Connections => _connections;

    /// <summary>Ereignis bei Änderung der Segmente oder Verbindungen.</summary>
    public event EventHandler? PlanChanged;

    /// <summary>Fügt ein neues platzierter Segment hinzu.</summary>
    public void AddSegment(PlacedSegment placed)
    {
        if (_segments.Any(s => s.Segment.No == placed.Segment.No))
            return;

        _segments.Add(placed);
        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Entfernt ein Segment und alle zugehörigen Verbindungen.</summary>
    public void RemoveSegment(Guid segmentNo)
    {
        _segments.RemoveAll(s => s.Segment.No == segmentNo);
        _connections.RemoveAll(c => c.SourceSegment == segmentNo || c.TargetSegment == segmentNo);
        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Aktualisiert die Position eines Segments.</summary>
    public void UpdateSegmentPosition(Guid segmentNo, double x, double y, double rotationDegrees)
    {
        var idx = _segments.FindIndex(s => s.Segment.No == segmentNo);
        if (idx < 0)
            return;

        var old = _segments[idx];
        _segments[idx] = old.WithPosition(x, y, rotationDegrees);
        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Verschiebt alle Segmente in der verbundenen Gruppe um den angegebenen Delta.</summary>
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

    /// <summary>Fügt eine Verbindung zwischen zwei Ports hinzu. Bestehende Verbindungen der betroffenen Ports werden entfernt.</summary>
    public void AddConnection(Guid sourceSegment, string sourcePort, Guid targetSegment, string targetPort)
    {
        RemoveConnectionsForPort(sourceSegment, sourcePort);
        RemoveConnectionsForPort(targetSegment, targetPort);

        _connections.Add(new PortConnection(sourceSegment, sourcePort, targetSegment, targetPort));

        // Gegenseitige Port-Referenzen im Segment setzen
        var src = _segments.FirstOrDefault(s => s.Segment.No == sourceSegment)?.Segment;
        var tgt = _segments.FirstOrDefault(s => s.Segment.No == targetSegment)?.Segment;
        if (src != null && tgt != null)
        {
            SetPortValue(src, sourcePort, tgt.No);
            SetPortValue(tgt, targetPort, src.No);
        }

        PlanChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Löst ein Segment aus der Gruppe – entfernt alle Verbindungen zu diesem Segment, ohne das Segment zu löschen.</summary>
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

    /// <summary>Entfernt eine Verbindung.</summary>
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
    /// Lädt einen TrackPlan aus Platzierungen und Verbindungen (z.B. von TrackPlanSvgRenderer.Render).
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