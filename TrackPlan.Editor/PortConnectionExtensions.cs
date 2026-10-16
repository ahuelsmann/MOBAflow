// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Editor;

/// <summary>Queries over the port connections of a track plan.</summary>
public static class PortConnectionExtensions
{
    /// <summary>Whether the port of the segment takes part in any connection, as source or target.</summary>
    public static bool IsConnected(this IEnumerable<PortConnection> connections, Guid segmentId, string portName)
    {
        ArgumentNullException.ThrowIfNull(connections);
        return connections.Any(connection =>
            (connection.SourceSegment == segmentId && connection.SourcePort == portName)
            || (connection.TargetSegment == segmentId && connection.TargetPort == portName));
    }
}
