// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Ghost;

using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.World;

public static class GhostFeedbackPlacement
{
    public static Point2D Preview(TrackEdge edge, Dictionary<Guid, Point2D> nodePositions)
        => Feedback.FeedbackPointPlacement.GetMidpoint(edge, nodePositions);
}