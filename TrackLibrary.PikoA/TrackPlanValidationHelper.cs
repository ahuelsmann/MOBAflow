namespace Moba.TrackLibrary.PikoA;

public static class TrackPlanValidationHelper
{
    public sealed record OpenPort(Guid SegmentId, string PortName, double X, double Y);

    public sealed record OverlappingPort(
        Guid LeftSegmentId,
        string LeftPortName,
        Guid RightSegmentId,
        string RightPortName,
        double CenterX,
        double CenterY,
        double DistanceMm);

    public sealed record ConnectedGroup(IReadOnlyList<Guid> SegmentIds, double MinX, double MinY, double MaxX, double MaxY);

    public sealed record ValidationAnalysis(
        IReadOnlyList<OpenPort> OpenPorts,
        IReadOnlyList<OverlappingPort> OverlappingPorts,
        IReadOnlyList<ConnectedGroup> ConnectedGroups);

    public static ValidationAnalysis Analyze(
        IReadOnlyList<PlacedSegment> segments,
        IReadOnlyList<PortConnection> connections,
        double overlapThresholdMm = 1.0)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(connections);

        var openPorts = segments
            .SelectMany(segment => SegmentPortGeometry.GetAllPortWorldPositions(segment)
                .Where(port => !IsPortConnected(connections, segment.Segment.No, port.PortName))
                .Select(port => new OpenPort(segment.Segment.No, port.PortName, port.X, port.Y)))
            .ToList();

        var overlappingPorts = new List<OverlappingPort>();
        for (var i = 0; i < openPorts.Count; i++)
        {
            for (var j = i + 1; j < openPorts.Count; j++)
            {
                var left = openPorts[i];
                var right = openPorts[j];
                if (left.SegmentId == right.SegmentId)
                    continue;

                var dx = left.X - right.X;
                var dy = left.Y - right.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance >= overlapThresholdMm)
                    continue;

                overlappingPorts.Add(new OverlappingPort(
                    left.SegmentId,
                    left.PortName,
                    right.SegmentId,
                    right.PortName,
                    (left.X + right.X) / 2,
                    (left.Y + right.Y) / 2,
                    distance));
            }
        }

        var connectedGroups = BuildConnectedGroups(segments, connections);
        return new ValidationAnalysis(openPorts, overlappingPorts, connectedGroups);
    }

    /// <summary>
    /// Finds open-port pairs that geometrically coincide within <paramref name="overlapThresholdMm"/> AND whose
    /// outward world angles are opposite within <paramref name="angleToleranceDegrees"/>, i.e. a missing connection
    /// that can safely be inferred from the placement. Used to auto-heal plans after load, import, or edit.
    /// Each port appears in at most one returned pair (the geometrically closest match wins), so repeated application
    /// is idempotent.
    /// </summary>
    public static IReadOnlyList<PortConnection> FindImplicitConnections(
        IReadOnlyList<PlacedSegment> segments,
        IReadOnlyList<PortConnection> connections,
        double overlapThresholdMm = 1.0,
        double angleToleranceDegrees = 2.0)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(connections);

        var segmentsById = segments.ToDictionary(s => s.Segment.No);
        var openPorts = segments
            .SelectMany(segment => SegmentPortGeometry.GetAllPortWorldPositions(segment)
                .Where(port => !IsPortConnected(connections, segment.Segment.No, port.PortName))
                .Select(port => (SegmentId: segment.Segment.No, port.PortName, port.X, port.Y)))
            .ToList();

        var consumed = new HashSet<(Guid, string)>();
        var result = new List<PortConnection>();

        for (var i = 0; i < openPorts.Count; i++)
        {
            var left = openPorts[i];
            if (consumed.Contains((left.SegmentId, left.PortName)))
                continue;

            var bestDistance = overlapThresholdMm;
            (Guid Seg, string Port)? bestMatch = null;
            for (var j = i + 1; j < openPorts.Count; j++)
            {
                var right = openPorts[j];
                if (left.SegmentId == right.SegmentId)
                    continue;
                if (consumed.Contains((right.SegmentId, right.PortName)))
                    continue;

                var dx = left.X - right.X;
                var dy = left.Y - right.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance >= bestDistance)
                    continue;

                var leftAngle = SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(segmentsById[left.SegmentId], left.PortName);
                var rightAngle = SegmentPortGeometry.GetPortOutwardWorldAngleDegrees(segmentsById[right.SegmentId], right.PortName);
                var expectedOpposite = NormalizeAngle(rightAngle + 180);
                if (AngleDelta(leftAngle, expectedOpposite) > angleToleranceDegrees)
                    continue;

                bestDistance = distance;
                bestMatch = (right.SegmentId, right.PortName);
            }

            if (bestMatch is { } match)
            {
                consumed.Add((left.SegmentId, left.PortName));
                consumed.Add(match);
                result.Add(new PortConnection(left.SegmentId, left.PortName, match.Seg, match.Port));
            }
        }

        return result;
    }

    private static double NormalizeAngle(double degrees)
    {
        while (degrees >= 360) degrees -= 360;
        while (degrees < 0) degrees += 360;
        return degrees;
    }

    private static double AngleDelta(double leftDegrees, double rightDegrees)
    {
        var delta = NormalizeAngle(leftDegrees - rightDegrees);
        if (delta > 180)
            delta = 360 - delta;
        return Math.Abs(delta);
    }

    private static IReadOnlyList<ConnectedGroup> BuildConnectedGroups(IReadOnlyList<PlacedSegment> segments, IReadOnlyList<PortConnection> connections)
    {
        var byId = segments.ToDictionary(segment => segment.Segment.No);
        var remaining = new HashSet<Guid>(byId.Keys);
        var groups = new List<ConnectedGroup>();

        while (remaining.Count > 0)
        {
            var seed = remaining.First();
            var ids = new HashSet<Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(seed);
            ids.Add(seed);
            remaining.Remove(seed);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var connection in connections)
                {
                    Guid? neighbor = null;
                    if (connection.SourceSegment == current)
                        neighbor = connection.TargetSegment;
                    else if (connection.TargetSegment == current)
                        neighbor = connection.SourceSegment;

                    if (neighbor.HasValue && byId.ContainsKey(neighbor.Value) && ids.Add(neighbor.Value))
                    {
                        queue.Enqueue(neighbor.Value);
                        remaining.Remove(neighbor.Value);
                    }
                }
            }

            groups.Add(CreateGroupBounds(ids, byId));
        }

        return groups;
    }

    private static ConnectedGroup CreateGroupBounds(IReadOnlyCollection<Guid> ids, IReadOnlyDictionary<Guid, PlacedSegment> byId)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var id in ids)
        {
            var placed = byId[id];
            var path = SegmentLocalPathBuilder.GetPath(placed.Segment);
            var (localMinX, localMinY, localMaxX, localMaxY) = SegmentLocalPathBuilder.GetBounds(path);
            var angleRad = placed.RotationDegrees * Math.PI / 180;
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            static double Tx(double ox, double lx, double ly, double cos, double sin) => ox + lx * cos - ly * sin;
            static double Ty(double oy, double lx, double ly, double cos, double sin) => oy + lx * sin + ly * cos;

            var corners = new[]
            {
                (Tx(placed.X, localMinX, localMinY, cos, sin), Ty(placed.Y, localMinX, localMinY, cos, sin)),
                (Tx(placed.X, localMaxX, localMinY, cos, sin), Ty(placed.Y, localMaxX, localMinY, cos, sin)),
                (Tx(placed.X, localMinX, localMaxY, cos, sin), Ty(placed.Y, localMinX, localMaxY, cos, sin)),
                (Tx(placed.X, localMaxX, localMaxY, cos, sin), Ty(placed.Y, localMaxX, localMaxY, cos, sin))
            };

            foreach (var (x, y) in corners)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return new ConnectedGroup(ids.ToList(), minX, minY, maxX, maxY);
    }

    private static bool IsPortConnected(IReadOnlyList<PortConnection> connections, Guid segmentId, string portName)
    {
        return connections.Any(connection =>
            (connection.SourceSegment == segmentId && connection.SourcePort == portName)
            || (connection.TargetSegment == segmentId && connection.TargetPort == portName));
    }
}
