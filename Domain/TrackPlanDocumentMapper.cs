// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Maps the persisted compatibility document to the renderer-independent layout aggregate.
/// It accepts version 1 documents, whose library identity was implicitly PIKO A.
/// </summary>
public static class TrackPlanDocumentMapper
{
    public const string LegacyLibraryId = "piko-a";

    public static Layout ToLayout(TrackPlanDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var libraryId = string.IsNullOrWhiteSpace(document.LibraryId) ? LegacyLibraryId : document.LibraryId;
        var layout = new Layout();

        foreach (var segment in document.Segments)
        {
            layout.AddTrack(new TrackInstance(
                segment.Id,
                string.IsNullOrWhiteSpace(segment.LibraryId) ? libraryId : segment.LibraryId,
                segment.Code,
                segment.X,
                segment.Y,
                segment.RotationDegrees,
                segment.InPort));
        }

        foreach (var connection in document.Connections)
            layout.Connect(new Connection(connection.SourceSegment, connection.SourcePort, connection.TargetSegment, connection.TargetPort));

        return layout;
    }

    public static TrackPlanDocument ToDocument(Layout layout, double? offsetX = null, double? offsetY = null, double? zoomFactor = null)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var tracks = layout.Tracks.OrderBy(track => track.Id).ToList();
        var libraryId = tracks.Select(track => track.LibraryId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1
            ? tracks[0].LibraryId
            : string.Empty;

        return new TrackPlanDocument
        {
            Version = 2,
            LibraryId = libraryId,
            OffsetX = offsetX,
            OffsetY = offsetY,
            ZoomFactor = zoomFactor,
            Segments = tracks.Select(track => new TrackPlanSegment
            {
                Id = track.Id,
                LibraryId = string.Equals(track.LibraryId, libraryId, StringComparison.OrdinalIgnoreCase) ? null : track.LibraryId,
                Code = track.TemplateId,
                X = track.X,
                Y = track.Y,
                RotationDegrees = track.RotationDegrees,
                InPort = track.FeedbackInPort
            }).ToList(),
            Connections = layout.Connections.Select(connection => new TrackPlanConnection
            {
                SourceSegment = connection.SourceTrackId,
                SourcePort = connection.SourceConnectorId,
                TargetSegment = connection.TargetTrackId,
                TargetPort = connection.TargetConnectorId
            }).ToList()
        };
    }
}
