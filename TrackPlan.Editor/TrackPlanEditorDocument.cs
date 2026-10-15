// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Editor;

using TrackLibrary.Base;

using Domain;

public sealed class TrackPlanEditorDocument
{
    public int Version { get; init; } = 1;

    /// <summary>Track catalogue of this document; empty means the editor's catalogue.</summary>
    public string LibraryId { get; init; } = string.Empty;

    public double? OffsetX { get; init; }

    public double? OffsetY { get; init; }

    public double? ZoomFactor { get; init; }

    public List<TrackPlanEditorSegment> Segments { get; init; } = [];

    public List<PortConnection> Connections { get; init; } = [];

    public static TrackPlanEditorDocument FromEditableTrackPlan(
        ITrackCatalog catalog,
        EditableTrackPlan plan,
        double? offsetX = null,
        double? offsetY = null,
        double? zoomFactor = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return FromData(catalog, plan.Segments, plan.Connections, offsetX, offsetY, zoomFactor);
    }

    public static TrackPlanEditorDocument FromData(
        ITrackCatalog catalog,
        IEnumerable<PlacedSegment> placements,
        IEnumerable<PortConnection> connections,
        double? offsetX = null,
        double? offsetY = null,
        double? zoomFactor = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(connections);

        return new TrackPlanEditorDocument
        {
            LibraryId = catalog.LibraryId,
            OffsetX = offsetX,
            OffsetY = offsetY,
            ZoomFactor = zoomFactor,
            Segments = placements.Select(placed => CreateSegmentSnapshot(catalog, placed)).ToList(),
            Connections = connections.ToList()
        };
    }

    public (List<PlacedSegment> Placements, List<PortConnection> Connections) ToEditableTrackPlanData(ITrackCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var placements = Segments.Select(segment => CreatePlacedSegment(catalog, segment, LibraryId)).ToList();
        return (placements, Connections.ToList());
    }

    /// <summary>
    /// Maps this editor document to the Domain <see cref="TrackPlanDocument"/>
    /// used by <c>Project.TrackPlan</c> when persisting to <c>solution.json</c>.
    /// </summary>
    public TrackPlanDocument ToDomainDocument()
    {
        return new TrackPlanDocument
        {
            Version = Version,
            LibraryId = LibraryId,
            OffsetX = OffsetX,
            OffsetY = OffsetY,
            ZoomFactor = ZoomFactor,
            Segments = Segments.Select(s => new TrackPlanSegment
            {
                Id = s.Id,
                Code = s.Code,
                LibraryId = s.LibraryId,
                X = s.X,
                Y = s.Y,
                RotationDegrees = s.RotationDegrees,
                InPort = s.InPort
            }).ToList(),
            Connections = Connections.Select(c => new TrackPlanConnection
            {
                SourceSegment = c.SourceSegment,
                SourcePort = c.SourcePort,
                TargetSegment = c.TargetSegment,
                TargetPort = c.TargetPort
            }).ToList()
        };
    }

    /// <summary>
    /// Creates an editor document from a Domain <see cref="TrackPlanDocument"/>
    /// as loaded from <c>solution.json</c>.
    /// </summary>
    public static TrackPlanEditorDocument FromDomainDocument(TrackPlanDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new TrackPlanEditorDocument
        {
            Version = document.Version,
            LibraryId = document.LibraryId,
            OffsetX = document.OffsetX,
            OffsetY = document.OffsetY,
            ZoomFactor = document.ZoomFactor,
            Segments = document.Segments.Select(s => new TrackPlanEditorSegment
            {
                Id = s.Id,
                Code = s.Code,
                LibraryId = string.IsNullOrWhiteSpace(s.LibraryId) ? document.LibraryId : s.LibraryId,
                X = s.X,
                Y = s.Y,
                RotationDegrees = s.RotationDegrees,
                InPort = s.InPort
            }).ToList(),
            Connections = document.Connections
                .Select(c => new PortConnection(c.SourceSegment, c.SourcePort, c.TargetSegment, c.TargetPort))
                .ToList()
        };
    }

    private static TrackPlanEditorSegment CreateSegmentSnapshot(ITrackCatalog catalog, PlacedSegment placed)
    {
        return new TrackPlanEditorSegment
        {
            Id = placed.Segment.No,
            Code = catalog.GetCode(placed.Segment),
            LibraryId = catalog.LibraryId,
            X = placed.X,
            Y = placed.Y,
            RotationDegrees = placed.RotationDegrees,
            InPort = placed.InPort
        };
    }

    private static PlacedSegment CreatePlacedSegment(ITrackCatalog catalog, TrackPlanEditorSegment segmentSnapshot, string documentLibraryId)
    {
        var libraryId = string.IsNullOrWhiteSpace(segmentSnapshot.LibraryId)
            ? (string.IsNullOrWhiteSpace(documentLibraryId) ? catalog.LibraryId : documentLibraryId)
            : segmentSnapshot.LibraryId;
        if (!string.Equals(libraryId, catalog.LibraryId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Track library '{libraryId}' is not available in the editor catalogue '{catalog.LibraryId}'.");

        if (!catalog.TryCreateSegment(segmentSnapshot.Code, out var segment))
            throw new InvalidOperationException($"Unknown track code '{segmentSnapshot.Code}'.");

        segment.No = segmentSnapshot.Id;
        return new PlacedSegment(segment, segmentSnapshot.X, segmentSnapshot.Y, segmentSnapshot.RotationDegrees, segmentSnapshot.InPort);
    }
}

public sealed class TrackPlanEditorSegment
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    /// <summary>Optional per-instance library override; empty values inherit the document library.</summary>
    public string? LibraryId { get; init; }

    public double X { get; init; }

    public double Y { get; init; }

    public double RotationDegrees { get; init; }

    /// <summary>Optional Z21 R-BUS feedback address assigned to this placed track.</summary>
    public int? InPort { get; init; }
}
