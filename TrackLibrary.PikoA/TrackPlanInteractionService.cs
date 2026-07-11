// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

/// <summary>
/// Platform-neutral editor interaction boundary for snapping and connection mutations.
/// WinUI translates pointer input into calls to this service; it does not decide topology.
/// </summary>
public sealed class TrackPlanInteractionService
{
    public sealed record SnapPreview(IReadOnlySet<(double X, double Y)> HighlightedPorts);
    private readonly EditableTrackPlan _plan;
    private readonly TrackPlanSpatialIndex _spatialIndex;

    public TrackPlanInteractionService(EditableTrackPlan plan)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _spatialIndex = new TrackPlanSpatialIndex(_plan);
    }

    public TrackPlanSnapHelper.SnapResult? FindBestSnap(
        PlacedSegment movingSegment,
        Guid? excludeSegmentId = null,
        IReadOnlySet<Guid>? movingGroup = null)
    {
        var candidates = SegmentPortGeometry.GetAllPortWorldPositions(movingSegment)
            .SelectMany(port => _spatialIndex.Query(port.X, port.Y, 25))
            .Distinct()
            .ToList();
        return TrackPlanSnapHelper.FindBestSnap(
            movingSegment,
            candidates,
            _plan.Connections,
            excludeSegmentId,
            movingGroup);
    }

    public void AddWithSnap(TrackPlanSnapHelper.SnapResult snap)
    {
        ArgumentNullException.ThrowIfNull(snap);
        _plan.AddSegment(snap.Placed);
        _plan.AddConnection(snap.Placed.Segment.No, snap.SourcePort, snap.TargetSegmentId, snap.TargetPort);
    }

    public void MoveWithSnap(Guid movedSegmentId, PlacedSegment originalPlacement, TrackPlanSnapHelper.SnapResult snap, IReadOnlySet<Guid> movingGroup)
    {
        ArgumentNullException.ThrowIfNull(originalPlacement);
        ArgumentNullException.ThrowIfNull(snap);
        ArgumentNullException.ThrowIfNull(movingGroup);

        var deltaX = snap.Placed.X - originalPlacement.X;
        var deltaY = snap.Placed.Y - originalPlacement.Y;
        if (movingGroup.Count > 1)
            _plan.MoveGroup(movingGroup, deltaX, deltaY);

        _plan.UpdateSegmentPosition(movedSegmentId, snap.Placed.X, snap.Placed.Y, snap.Placed.RotationDegrees);
        _plan.AddConnection(movedSegmentId, snap.SourcePort, snap.TargetSegmentId, snap.TargetPort);
    }

    /// <summary>
    /// Finds the nearest placed segment around a world-space pointer. The editor supplies
    /// coordinates in millimetres, so this method has no dependency on canvas scaling or WinUI.
    /// </summary>
    public PlacedSegment? HitTest(double worldX, double worldY, double toleranceMm = 12)
    {
        PlacedSegment? best = null;
        var bestDistance = double.MaxValue;

        foreach (var placed in _spatialIndex.Query(worldX, worldY, toleranceMm))
        {
            var ports = SegmentPortGeometry.GetAllPortWorldPositions(placed);
            for (var index = 0; index < ports.Count; index++)
            {
                var (_, x, y, _) = ports[index];
                Consider(placed, Distance(worldX, worldY, x, y), toleranceMm, ref best, ref bestDistance);

                if (index == 0)
                    continue;

                var (_, previousX, previousY, _) = ports[index - 1];
                Consider(placed, DistanceToLine(worldX, worldY, previousX, previousY, x, y), toleranceMm, ref best, ref bestDistance);
            }

            if (ports.Count > 2)
            {
                var (_, firstX, firstY, _) = ports[0];
                var (_, lastX, lastY, _) = ports[^1];
                Consider(placed, DistanceToLine(worldX, worldY, firstX, firstY, lastX, lastY), toleranceMm, ref best, ref bestDistance);
            }
        }

        return best;
    }

    /// <summary>Returns connector positions that are eligible for a visual snap preview.</summary>
    public SnapPreview GetSnapPreview(PlacedSegment movingSegment, IReadOnlySet<Guid>? movingGroup = null, double thresholdMm = 25)
    {
        ArgumentNullException.ThrowIfNull(movingSegment);
        var highlights = new HashSet<(double X, double Y)>();
        var movingPorts = SegmentPortGeometry.GetAllPortWorldPositions(movingSegment);
        foreach (var source in movingPorts)
        {
            if (IsConnected(movingSegment.Segment.No, source.PortName))
                continue;
            foreach (var candidate in _spatialIndex.Query(source.X, source.Y, thresholdMm))
            {
                if (candidate.Segment.No == movingSegment.Segment.No || (movingGroup?.Contains(candidate.Segment.No) ?? false))
                    continue;
                foreach (var target in SegmentPortGeometry.GetAllPortWorldPositions(candidate))
                {
                    if (IsConnected(candidate.Segment.No, target.PortName) || Distance(source.X, source.Y, target.X, target.Y) >= thresholdMm)
                        continue;
                    highlights.Add((source.X, source.Y));
                    highlights.Add((target.X, target.Y));
                }
            }
        }
        return new SnapPreview(highlights);
    }

    private bool IsConnected(Guid segmentId, string portName) => _plan.Connections.Any(connection =>
        (connection.SourceSegment == segmentId && connection.SourcePort == portName)
        || (connection.TargetSegment == segmentId && connection.TargetPort == portName));

    private static void Consider(PlacedSegment candidate, double distance, double tolerance, ref PlacedSegment? best, ref double bestDistance)
    {
        if (distance <= tolerance && distance < bestDistance)
        {
            best = candidate;
            bestDistance = distance;
        }
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        var deltaX = x1 - x2;
        var deltaY = y1 - y2;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private static double DistanceToLine(double pointX, double pointY, double startX, double startY, double endX, double endY)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        var lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);
        if (lengthSquared <= double.Epsilon)
            return Distance(pointX, pointY, startX, startY);

        var ratio = Math.Clamp(((pointX - startX) * deltaX + (pointY - startY) * deltaY) / lengthSquared, 0, 1);
        return Distance(pointX, pointY, startX + (ratio * deltaX), startY + (ratio * deltaY));
    }
}
