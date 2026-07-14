// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using TrackLibrary.PikoA;

/// <summary>Adapts Piko A placements to the library-neutral renderer scene.</summary>
public static class TrackPlanRenderSceneBuilder
{
    public static TrackPlanRenderScene Build(
        IReadOnlyList<PlacedSegment> placements,
        IReadOnlySet<Guid>? selectedTrackIds = null,
        IReadOnlyDictionary<Guid, double>? feedbackIntensities = null,
        IReadOnlyList<TrackPlanValidationMarker>? validationMarkers = null,
        bool includePorts = false)
    {
        ArgumentNullException.ThrowIfNull(placements);

        var items = placements.Select(placement => new TrackPlanRenderItem(
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
                : 0)).ToList();

        var ports = includePorts
            ? placements
                .SelectMany(SegmentPortGeometry.GetAllPortWorldPositions)
                .Select(port => new TrackPlanPortMarker(port.X, port.Y, port.PortName))
                .ToList()
            : [];

        return new TrackPlanRenderScene(items, validationMarkers, ports);
    }
}

/// <summary>Compatibility adapter for rendering Piko A placements.</summary>
public static class PikoAPlacedTrackPlanSvgRendererExtensions
{
    public static string Render(
        this PlacedTrackPlanSvgRenderer renderer,
        IReadOnlyList<PlacedSegment> placements,
        double trackOpacity = 0.8,
        bool showGrid = false,
        bool showPorts = false)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(placements);
        return renderer.Render(
            TrackPlanRenderSceneBuilder.Build(placements, includePorts: showPorts),
            trackOpacity,
            showGrid);
    }
}
