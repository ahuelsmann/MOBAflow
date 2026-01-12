// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Geometry;

using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

public sealed class TrackGeometryRenderer
{
    public IEnumerable<IGeometryPrimitive> Render(
        TrackTemplate template,
        Point2D start,
        double startAngleDeg)
    {
        var spec = template.Geometry;

        return spec.GeometryKind switch
        {
            TrackGeometryKind.Straight
                => StraightGeometry.Render(start, startAngleDeg, spec.LengthMm!.Value),

            TrackGeometryKind.Curve
                => CurveGeometry.Render(start, startAngleDeg, spec),

            TrackGeometryKind.Switch
                => RenderSwitch(template, spec, start, startAngleDeg),

            _ => Array.Empty<IGeometryPrimitive>()
        };
    }

    private IEnumerable<IGeometryPrimitive> RenderSwitch(
        TrackTemplate template,
        TrackGeometrySpec spec,
        Point2D start,
        double startAngleDeg)
    {
        bool isLeft = template.Id.Contains('L');
        return SwitchGeometry.Render(start, startAngleDeg, spec, template.Routing!, isLeft);
    }
}