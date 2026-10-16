// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Editor;

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
        SegmentPlacementGeometry geometry,
        IReadOnlyList<PlacedSegment> segments,
        IReadOnlyList<PortConnection> connections,
        double overlapThresholdMm = 1.0)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(connections);

        var openPorts = segments
            .SelectMany(segment => geometry.GetOpenPortWorldPositions(segment, connections)
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

        var connectedGroups = BuildConnectedGroups(geometry, segments, connections);
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
        SegmentPlacementGeometry geometry,
        IReadOnlyList<PlacedSegment> segments,
        IReadOnlyList<PortConnection> connections,
        double overlapThresholdMm = 1.0,
        double angleToleranceDegrees = 2.0)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(connections);

        var segmentsById = segments.ToDictionary(s => s.Segment.No);
        var openPorts = segments
            .SelectMany(segment => geometry.GetOpenPortWorldPositions(segment, connections)
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

                var leftAngle = geometry.GetPortOutwardWorldAngleDegrees(segmentsById[left.SegmentId], left.PortName);
                var rightAngle = geometry.GetPortOutwardWorldAngleDegrees(segmentsById[right.SegmentId], right.PortName);
                var expectedOpposite = SegmentPlacementGeometry.NormalizeAngle(rightAngle + 180);
                if (SegmentPlacementGeometry.AngleDeltaDegrees(leftAngle, expectedOpposite) > angleToleranceDegrees)
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

    private static IReadOnlyList<ConnectedGroup> BuildConnectedGroups(SegmentPlacementGeometry geometry, IReadOnlyList<PlacedSegment> segments, IReadOnlyList<PortConnection> connections)
    {
        var byId = segments.ToDictionary(segment => segment.Segment.No);
        var remaining = new HashSet<Guid>(byId.Keys);
        var groups = new List<ConnectedGroup>();

        while (remaining.Count > 0)
        {
            var ids = CollectConnectedGroup(remaining.First(), connections, byId);
            remaining.ExceptWith(ids);
            groups.Add(CreateGroupBounds(geometry, ids, byId));
        }

        return groups;
    }

    private static HashSet<Guid> CollectConnectedGroup(
        Guid seed,
        IReadOnlyList<PortConnection> connections,
        Dictionary<Guid, PlacedSegment> byId)
    {
        var ids = new HashSet<Guid> { seed };
        var queue = new Queue<Guid>();
        queue.Enqueue(seed);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in connections.Select(connection => GetNeighbor(connection, current)))
            {
                if (neighbor is Guid id && byId.ContainsKey(id) && ids.Add(id))
                    queue.Enqueue(id);
            }
        }

        return ids;
    }

    private static Guid? GetNeighbor(PortConnection connection, Guid segmentId)
    {
        if (connection.SourceSegment == segmentId)
            return connection.TargetSegment;

        return connection.TargetSegment == segmentId ? connection.SourceSegment : null;
    }

    private static ConnectedGroup CreateGroupBounds(SegmentPlacementGeometry geometry, IReadOnlyCollection<Guid> ids, Dictionary<Guid, PlacedSegment> byId)
    {
        var (minX, minY, maxX, maxY) = geometry.GetContentBounds(ids.Select(id => byId[id]))!.Value;
        return new ConnectedGroup(ids.ToList(), minX, minY, maxX, maxY);
    }
}