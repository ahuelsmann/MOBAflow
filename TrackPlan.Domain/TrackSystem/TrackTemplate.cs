// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.TrackSystem;

public sealed record TrackTemplate(
    string Id,
    IReadOnlyList<TrackEnd> Ends,
    TrackGeometrySpec Geometry,
    SwitchRoutingModel? Routing = null
)
{
    /// <summary>
    /// Gets a port by its ID.
    /// </summary>
    public TrackPort? GetPort(string portId) => null;
}
