namespace Moba.TrackLibrary.PikoA;

using Base;

using Moba.Domain;

public sealed class TrackPlanEditorDocument
{
    public int Version { get; init; } = 1;

    public double? OffsetX { get; init; }

    public double? OffsetY { get; init; }

    public double? ZoomFactor { get; init; }

    public List<TrackPlanEditorSegment> Segments { get; init; } = [];

    public List<PortConnection> Connections { get; init; } = [];

    public static TrackPlanEditorDocument FromEditableTrackPlan(
        EditableTrackPlan plan,
        double? offsetX = null,
        double? offsetY = null,
        double? zoomFactor = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return FromData(plan.Segments, plan.Connections, offsetX, offsetY, zoomFactor);
    }

    public static TrackPlanEditorDocument FromData(
        IEnumerable<PlacedSegment> placements,
        IEnumerable<PortConnection> connections,
        double? offsetX = null,
        double? offsetY = null,
        double? zoomFactor = null)
    {
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(connections);

        return new TrackPlanEditorDocument
        {
            OffsetX = offsetX,
            OffsetY = offsetY,
            ZoomFactor = zoomFactor,
            Segments = placements.Select(CreateSegmentSnapshot).ToList(),
            Connections = connections.ToList()
        };
    }

    public (List<PlacedSegment> Placements, List<PortConnection> Connections) ToEditableTrackPlanData()
    {
        var placements = Segments.Select(CreatePlacedSegment).ToList();
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
            OffsetX = OffsetX,
            OffsetY = OffsetY,
            ZoomFactor = ZoomFactor,
            Segments = Segments.Select(s => new TrackPlanSegment
            {
                Id = s.Id,
                Code = s.Code,
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
            OffsetX = document.OffsetX,
            OffsetY = document.OffsetY,
            ZoomFactor = document.ZoomFactor,
            Segments = document.Segments.Select(s => new TrackPlanEditorSegment
            {
                Id = s.Id,
                Code = s.Code,
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

    private static TrackPlanEditorSegment CreateSegmentSnapshot(PlacedSegment placed)
    {
        var entry = PikoACatalog.All.FirstOrDefault(c => c.SegmentType == placed.Segment.GetType());
        if (entry == null)
            throw new InvalidOperationException($"No catalog entry found for segment type {placed.Segment.GetType().Name}.");

        return new TrackPlanEditorSegment
        {
            Id = placed.Segment.No,
            Code = entry.Code,
            X = placed.X,
            Y = placed.Y,
            RotationDegrees = placed.RotationDegrees,
            InPort = placed.InPort
        };
    }

    private static PlacedSegment CreatePlacedSegment(TrackPlanEditorSegment segmentSnapshot)
    {
        var entry = PikoACatalog.All.FirstOrDefault(c => string.Equals(c.Code, segmentSnapshot.Code, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            throw new InvalidOperationException($"Unknown track code '{segmentSnapshot.Code}'.");

        var segment = (Segment)Activator.CreateInstance(entry.SegmentType)!;
        segment.No = segmentSnapshot.Id;
        return new PlacedSegment(segment, segmentSnapshot.X, segmentSnapshot.Y, segmentSnapshot.RotationDegrees, segmentSnapshot.InPort);
    }
}

public sealed class TrackPlanEditorSegment
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public double X { get; init; }

    public double Y { get; init; }

    public double RotationDegrees { get; init; }

    /// <summary>Optional Z21 R-BUS feedback address assigned to this placed track.</summary>
    public int? InPort { get; init; }
}
