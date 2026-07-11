// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using TrackLibrary.PikoA;

/// <summary>Renderer-neutral projection of an editable layout.</summary>
public sealed record TrackPlanRenderScene(IReadOnlyList<TrackPlanRenderItem> Items);

/// <summary>One drawable track with geometry expressed in local millimetres.</summary>
public sealed record TrackPlanRenderItem(
    Guid Id,
    string TemplateId,
    double X,
    double Y,
    double RotationDegrees,
    IReadOnlyList<SegmentLocalPathBuilder.PathCommand> Path);

/// <summary>Builds one shared scene for platform renderers and SVG export.</summary>
public static class TrackPlanRenderSceneBuilder
{
    public static TrackPlanRenderScene Build(IReadOnlyList<PlacedSegment> placements)
    {
        ArgumentNullException.ThrowIfNull(placements);
        return new TrackPlanRenderScene(placements.Select(placement => new TrackPlanRenderItem(
            placement.Segment.No,
            placement.Segment.GetType().Name,
            placement.X,
            placement.Y,
            placement.RotationDegrees,
            SegmentLocalPathBuilder.GetPath(placement.Segment))).ToList());
    }
}
