namespace Moba.TrackPlan.Renderer.Layout;

using Moba.TrackPlan.Graph;

public interface ILayoutEngine
{
    Dictionary<Guid, Point2D> Layout(TopologyGraph graph);
}