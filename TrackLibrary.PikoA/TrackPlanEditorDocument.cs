namespace Moba.TrackLibrary.PikoA;

using Base;

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
            RotationDegrees = placed.RotationDegrees
        };
    }

    private static PlacedSegment CreatePlacedSegment(TrackPlanEditorSegment segmentSnapshot)
    {
        var entry = PikoACatalog.All.FirstOrDefault(c => string.Equals(c.Code, segmentSnapshot.Code, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            throw new InvalidOperationException($"Unknown track code '{segmentSnapshot.Code}'.");

        var segment = (Segment)Activator.CreateInstance(entry.SegmentType)!;
        segment.No = segmentSnapshot.Id;
        return new PlacedSegment(segment, segmentSnapshot.X, segmentSnapshot.Y, segmentSnapshot.RotationDegrees);
    }
}

public sealed class TrackPlanEditorSegment
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public double X { get; init; }

    public double Y { get; init; }

    public double RotationDegrees { get; init; }
}
