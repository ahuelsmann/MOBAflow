// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.TrackSystem;

public interface ITrackCatalog
{
    string SystemId { get; }
    string SystemName { get; }
    string Manufacturer { get; }
    string Scale { get; }

    IReadOnlyList<TrackTemplate> Templates { get; }

    TrackTemplate? GetById(string id);
    IEnumerable<TrackTemplate> GetByCategory(TrackGeometryKind kind);
}