// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.Base.TrackSystem;

/// <summary>
/// Represents a track template defining the geometric and routing properties of a track piece.
/// This is the base model for all track systems (Piko A, Märklin, Fleischmann, etc.).
/// </summary>
public sealed record TrackTemplate(
    string Id,
    IReadOnlyList<TrackEnd> Ends,
    TrackGeometrySpec Geometry,
    SwitchRoutingModel? Routing = null
)
{
    /// <summary>
    /// Display code for UI labels (defaults to Id if not set).
    /// </summary>
    public string DisplayCode => Id;

    /// <summary>
    /// Gets a port by its ID.
    /// </summary>
    public TrackPort? GetPort(string portId) => null;
}
