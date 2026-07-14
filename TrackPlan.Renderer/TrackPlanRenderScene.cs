// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using TrackLibrary.Base;

/// <summary>Renderer-neutral projection of a track layout.</summary>
public sealed record TrackPlanRenderScene(
    IReadOnlyList<TrackPlanRenderItem> Items,
    IReadOnlyList<TrackPlanValidationMarker>? ValidationMarkers = null,
    IReadOnlyList<TrackPlanPortMarker>? PortMarkers = null)
{
    public IReadOnlyList<TrackPlanValidationMarker> Markers => ValidationMarkers ?? [];
    public IReadOnlyList<TrackPlanPortMarker> Ports => PortMarkers ?? [];
}

/// <summary>One drawable track with geometry expressed in local millimetres.</summary>
public sealed record TrackPlanRenderItem(
    Guid Id,
    string TemplateId,
    double X,
    double Y,
    double RotationDegrees,
    IReadOnlyList<ITrackPathCommand> Path,
    string? Label = null,
    bool IsSelected = false,
    double FeedbackIntensity = 0);

/// <summary>Renderer-neutral annotation for a layout validation finding.</summary>
public sealed record TrackPlanValidationMarker(Guid? TrackId, double X, double Y, string Message);

/// <summary>Renderer-neutral connector marker.</summary>
public sealed record TrackPlanPortMarker(double X, double Y, string Name);
