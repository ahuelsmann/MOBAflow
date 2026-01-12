// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Feedback;

using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.World;

public static class FeedbackPointPlacement
{
    public static Point2D GetMidpoint(TrackEdge edge, Dictionary<Guid, Point2D> nodePositions)
    {
        var points = edge.Connections.Values
            .Select(e => nodePositions[e.NodeId])
            .ToList();

        return new Point2D(
            points.Average(p => p.X),
            points.Average(p => p.Y)
        );
    }
}