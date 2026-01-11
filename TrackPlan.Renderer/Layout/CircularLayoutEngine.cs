namespace Moba.TrackPlan.Renderer.Layout;

using Moba.TrackPlan.Graph;

public sealed class CircularLayoutEngine : ILayoutEngine
{
    public Dictionary<Guid, Point2D> Layout(TopologyGraph graph)
    {
        var result = new Dictionary<Guid, Point2D>();
        var count = graph.Nodes.Count;
        var radius = 200.0;

        for (int i = 0; i < count; i++)
        {
            var angle = 2 * Math.PI * i / count;
            var node = graph.Nodes[i];

            result[node.Id] = new Point2D(
                radius * Math.Cos(angle),
                radius * Math.Sin(angle)
            );
        }

        return result;
    }
}