namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

public sealed class TrackGeometryRenderer
{
    /// <summary>
    /// Renders track geometry primitives for visualization.
    /// </summary>
    /// <param name="edge">The track edge being rendered.</param>
    /// <param name="template">Track template with geometry specification.</param>
    /// <param name="a">Start point.</param>
    /// <param name="b">End point (for straight/curve) or straight-end point (for switches).</param>
    /// <param name="isLeftSwitch">For switches: true = left-hand, false = right-hand.</param>
    public IEnumerable<IGeometryPrimitive> Render(
        TrackEdge edge,
        TrackTemplate template,
        Point2D a,
        Point2D b,
        bool isLeftSwitch = true)
    {
        return template.Geometry.GeometryKind switch
        {
            TrackGeometryKind.Straight => StraightGeometry.Render(a, b),
            TrackGeometryKind.Curve => CurveGeometry.Render(a, b, template.Geometry),
            TrackGeometryKind.Switch => SwitchGeometry.RenderSimple(a, b, isLeftSwitch),
            _ => []
        };
    }
}