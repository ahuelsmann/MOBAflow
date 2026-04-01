namespace Moba.TrackLibrary.PikoA;

public static class TrackPlanSnapHelper
{
    public sealed record SnapResult(PlacedSegment Placed, string SourcePort, Guid TargetSegmentId, string TargetPort, double DistanceMm);

    public static SnapResult? FindBestSnap(
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
        var movingPorts = SegmentPortGeometry.GetAllPortWorldPositions(movingSegment);

        foreach (var (sourcePort, sourceX, sourceY, _) in movingPorts)
        {
            if (IsPortConnected(connections, movingSegment.Segment.No, sourcePort))
                continue;

            foreach (var other in allSegments)
            {
                if (other.Segment.No == movingSegment.Segment.No || other.Segment.No == excludeSegmentId)
                    continue;
                if (movingGroup != null && movingGroup.Contains(other.Segment.No))
                    continue;

                foreach (var (targetPort, targetX, targetY, _) in SegmentPortGeometry.GetAllPortWorldPositions(other))
                {
                    if (IsPortConnected(connections, other.Segment.No, targetPort))
                        continue;

                    var deltaX = targetX - sourceX;
                    var deltaY = targetY - sourceY;
                    var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    if (distance >= distanceThresholdMm || distance >= bestDistance)
                        continue;

                    var desiredOutwardAngle = NormalizeAngle(SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(other, targetPort) + 180);
                    PlacedSegment candidate;

                    if (rigidGroup)
                    {
                        var currentOutwardAngle = SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(movingSegment, sourcePort);
                        if (GetAngleDeltaDegrees(currentOutwardAngle, desiredOutwardAngle) > rigidGroupAngleToleranceDegrees)
                            continue;

                        var (translatedX, translatedY, translatedRotation) = SegmentPortGeometry.TranslatePlacementForPort(
                            movingSegment,
                            sourcePort,
                            targetX,
                            targetY);
                        candidate = movingSegment.WithPosition(translatedX, translatedY, translatedRotation);
                    }
                    else
                    {
                        var (originX, originY, rotationDegrees) = SegmentPortGeometry.GetPlacementForPort(
                            movingSegment.Segment,
                            sourcePort,
                            targetX,
                            targetY,
                            desiredOutwardAngle);
                        candidate = movingSegment.WithPosition(originX, originY, rotationDegrees);
                    }

                    bestDistance = distance;
                    best = new SnapResult(candidate, sourcePort, other.Segment.No, targetPort, distance);
                }
            }
        }

        return best;
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
