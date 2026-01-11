namespace Moba.TrackPlan.TrackSystem;

public sealed record TrackTemplate(
    string Id,
    IReadOnlyList<TrackEnd> Ends,
    TrackGeometrySpec Geometry,
    SwitchRoutingModel? Routing = null
);