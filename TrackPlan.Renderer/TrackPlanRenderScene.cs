// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using TrackLibrary.PikoA;

/// <summary>Renderer-neutral projection of an editable layout.</summary>
public sealed record TrackPlanRenderScene(
    IReadOnlyList<TrackPlanRenderItem> Items,
    IReadOnlyList<TrackPlanValidationMarker>? ValidationMarkers = null)
{
    public IReadOnlyList<TrackPlanValidationMarker> Markers => ValidationMarkers ?? [];
}

/// <summary>One drawable track with geometry expressed in local millimetres.</summary>
public sealed record TrackPlanRenderItem(
    Guid Id,
    string TemplateId,
    double X,
    double Y,
    double RotationDegrees,
    IReadOnlyList<SegmentLocalPathBuilder.PathCommand> Path,
    string? Label = null,
    bool IsSelected = false,
    double FeedbackIntensity = 0);

/// <summary>Renderer-neutral annotation for a layout validation finding.</summary>
public sealed record TrackPlanValidationMarker(Guid? TrackId, double X, double Y, string Message);

/// <summary>Builds one shared scene for platform renderers and SVG export.</summary>
public static class TrackPlanRenderSceneBuilder
{
    public static TrackPlanRenderScene Build(
        IReadOnlyList<PlacedSegment> placements,
        IReadOnlySet<Guid>? selectedTrackIds = null,
        IReadOnlyDictionary<Guid, double>? feedbackIntensities = null,
        IReadOnlyList<TrackPlanValidationMarker>? validationMarkers = null)
    {
        ArgumentNullException.ThrowIfNull(placements);
        return new TrackPlanRenderScene(placements.Select(placement => new TrackPlanRenderItem(
            placement.Segment.No,
            placement.Segment.GetType().Name,
            placement.X,
            placement.Y,
            placement.RotationDegrees,
            SegmentLocalPathBuilder.GetPath(placement.Segment),
            placement.Segment.GetType().Name,
            selectedTrackIds?.Contains(placement.Segment.No) ?? false,
            feedbackIntensities?.TryGetValue(placement.Segment.No, out var intensity) == true
                ? Math.Clamp(intensity, 0, 1)
                : 0)).ToList(), validationMarkers);
    }
}
