namespace Moba.TrackPlan.Renderer.Ghost;

using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.World;

public static class GhostFeedbackPlacement
{
    public static Point2D Preview(TrackEdge edge, Dictionary<Guid, Point2D> nodePositions)
        => Feedback.FeedbackPointPlacement.GetMidpoint(edge, nodePositions);
}