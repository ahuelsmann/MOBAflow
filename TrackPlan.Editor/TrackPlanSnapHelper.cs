// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Editor;

public sealed class TrackPlanSnapHelper(SegmentPlacementGeometry geometry)
{
    private readonly SegmentPlacementGeometry _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));

    public sealed record SnapResult(PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort, double DistanceMm);

    public SnapResult? FindBestSnap(
        PlacedSegment movingSegment,
        IReadOnlyList<PlacedSegment> allSegments,
        IReadOnlyList<PortConnection> connections,
        Guid? excludeSegmentId = null,
        IReadOnlySet<Guid>? movingGroup = null,
        double distanceThresholdMm = 22.5,
        double rigidGroupAngleToleranceDegrees = 1.0)
    {
        ArgumentNullException.ThrowIfNull(movingSegment);
        ArgumentNullException.ThrowIfNull(allSegments);
        ArgumentNullException.ThrowIfNull(connections);

        SnapResult? best = null;
        var bestDistance = double.MaxValue;
        var rigidGroup = movingGroup != null && movingGroup.Count > 1 && movingGroup.Contains(movingSegment.Segment.No);

        foreach (var (sourcePort, sourceX, sourceY, _) in _geometry.GetAllPortWorldPositions(movingSegment))
        {
            if (IsPortConnected(connections, movingSegment.Segment.No, sourcePort))
                continue;

            foreach (var target in GetOpenTargetPorts(movingSegment, allSegments, connections, excludeSegmentId, movingGroup))
            {
                var distance = Distance(sourceX, sourceY, target.X, target.Y);
                if (distance >= distanceThresholdMm || distance >= bestDistance)
                    continue;

                var candidate = TryAlign(movingSegment, sourcePort, target, rigidGroup, rigidGroupAngleToleranceDegrees);
                if (candidate == null)
                    continue;

                bestDistance = distance;
                best = new SnapResult(candidate, sourcePort, target.Segment.Segment.No, target.Port, distance);
            }
        }

        return best;
    }

    private sealed record SnapTarget(PlacedSegment Segment, string Port, double X, double Y);

    private IEnumerable<SnapTarget> GetOpenTargetPorts(
        PlacedSegment movingSegment,
        IReadOnlyList<PlacedSegment> allSegments,
        IReadOnlyList<PortConnection> connections,
        Guid? excludeSegmentId,
        IReadOnlySet<Guid>? movingGroup)
    {
        foreach (var other in allSegments)
        {
            if (other.Segment.No == movingSegment.Segment.No
                || other.Segment.No == excludeSegmentId
                || (movingGroup != null && movingGroup.Contains(other.Segment.No)))
            {
                continue;
            }

            foreach (var (targetPort, targetX, targetY, _) in _geometry.GetAllPortWorldPositions(other))
            {
                if (!IsPortConnected(connections, other.Segment.No, targetPort))
                    yield return new SnapTarget(other, targetPort, targetX, targetY);
            }
        }
    }

    /// <summary>
    /// Placement of the moving segment that joins <paramref name="sourcePort"/> to the target port, or <c>null</c>
    /// when a rigid group cannot be translated onto the target without rotating.
    /// </summary>
    private PlacedSegment? TryAlign(
        PlacedSegment movingSegment,
        string sourcePort,
        SnapTarget target,
        bool rigidGroup,
        double rigidGroupAngleToleranceDegrees)
    {
        var desiredOutwardAngle = NormalizeAngle(_geometry.GetPortOutwardWorldAngleDegrees(target.Segment, target.Port) + 180);

        if (rigidGroup)
        {
            var currentOutwardAngle = _geometry.GetPortOutwardWorldAngleDegrees(movingSegment, sourcePort);
            if (GetAngleDeltaDegrees(currentOutwardAngle, desiredOutwardAngle) > rigidGroupAngleToleranceDegrees)
                return null;

            var (translatedX, translatedY, translatedRotation) = _geometry.TranslatePlacementForPort(
                movingSegment,
                sourcePort,
                target.X,
                target.Y);
            return movingSegment.WithPosition(translatedX, translatedY, translatedRotation);
        }

        var (originX, originY, rotationDegrees) = _geometry.GetPlacementForPort(
            movingSegment.Segment,
            sourcePort,
            target.X,
            target.Y,
            desiredOutwardAngle);
        return movingSegment.WithPosition(originX, originY, rotationDegrees);
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        var deltaX = x2 - x1;
        var deltaY = y2 - y1;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private static bool IsPortConnected(IReadOnlyList<PortConnection> connections, Guid segmentId, string portName)
    {
        return connections.Any(connection =>
            (connection.SourceSegment == segmentId && connection.SourcePort == portName)
            || (connection.TargetSegment == segmentId && connection.TargetPort == portName));
    }

    private static double GetAngleDeltaDegrees(double leftDegrees, double rightDegrees)
    {
        var delta = NormalizeAngle(leftDegrees - rightDegrees);
        if (delta > 180)
            delta = 360 - delta;
        return Math.Abs(delta);
    }

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360)
            degrees -= 360;
        while (degrees < 0)
            degrees += 360;
        return degrees;
    }
}