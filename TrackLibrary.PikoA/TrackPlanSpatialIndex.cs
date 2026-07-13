// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.PikoA;

/// <summary>Uniform-grid spatial index for editor candidate queries in world millimetres.</summary>
public sealed class TrackPlanSpatialIndex
{
    private readonly EditableTrackPlan _plan;
    private readonly Dictionary<(int X, int Y), Dictionary<Guid, PlacedSegment>> _cells = [];
    private readonly Dictionary<Guid, HashSet<(int X, int Y)>> _cellsBySegmentId = [];
    private bool _handledDetailedMutation;

    public TrackPlanSpatialIndex(EditableTrackPlan plan, double cellSizeMm = 100)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        CellSizeMm = cellSizeMm > 0 ? cellSizeMm : throw new ArgumentOutOfRangeException(nameof(cellSizeMm));
        _plan.PlanMutated += OnPlanMutated;
        _plan.PlanChanged += OnPlanChanged;
        Rebuild();
    }

    public double CellSizeMm { get; }

    public IReadOnlyCollection<PlacedSegment> Query(double x, double y, double radiusMm)
    {
        var result = new Dictionary<Guid, PlacedSegment>();
        var minX = ToCell(x - radiusMm);
        var maxX = ToCell(x + radiusMm);
        var minY = ToCell(y - radiusMm);
        var maxY = ToCell(y + radiusMm);
        for (var cellX = minX; cellX <= maxX; cellX++)
        for (var cellY = minY; cellY <= maxY; cellY++)
            if (_cells.TryGetValue((cellX, cellY), out var contents))
                foreach (var placed in contents.Values)
                    result[placed.Segment.No] = placed;
        return result.Values.ToList();
    }

    private void Rebuild()
    {
        _cells.Clear();
        _cellsBySegmentId.Clear();
        foreach (var placed in _plan.Segments)
            Add(placed);
    }

    private void OnPlanMutated(object? sender, EditableTrackPlan.PlanMutation mutation)
    {
        _ = sender;
        _handledDetailedMutation = true;
        if (mutation.RequiresFullRebuild)
        {
            Rebuild();
            return;
        }
        if (mutation.Previous != null)
            Remove(mutation.Previous);
        if (mutation.Current != null)
            Add(mutation.Current);
    }

    private void OnPlanChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (_handledDetailedMutation)
        {
            _handledDetailedMutation = false;
            return;
        }
        Rebuild();
    }

    private void Add(PlacedSegment placed)
    {
        var ports = SegmentPortGeometry.GetAllPortWorldPositions(placed);
        var segmentCells = new HashSet<(int X, int Y)>();
        for (var cellX = ToCell(ports.Min(port => port.X)); cellX <= ToCell(ports.Max(port => port.X)); cellX++)
        for (var cellY = ToCell(ports.Min(port => port.Y)); cellY <= ToCell(ports.Max(port => port.Y)); cellY++)
        {
            var cell = (cellX, cellY);
            if (!_cells.TryGetValue(cell, out var contents))
                _cells[cell] = contents = [];
            contents[placed.Segment.No] = placed;
            segmentCells.Add(cell);
        }
        _cellsBySegmentId[placed.Segment.No] = segmentCells;
    }

    private void Remove(PlacedSegment placed)
    {
        if (!_cellsBySegmentId.Remove(placed.Segment.No, out var segmentCells))
            return;

        foreach (var cell in segmentCells)
        {
            if (!_cells.TryGetValue(cell, out var contents))
                continue;
            contents.Remove(placed.Segment.No);
            if (contents.Count == 0)
                _cells.Remove(cell);
        }
    }

    private int ToCell(double coordinate) => (int)Math.Floor(coordinate / CellSizeMm);
}
