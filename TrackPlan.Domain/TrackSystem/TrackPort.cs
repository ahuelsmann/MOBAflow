// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.TrackSystem;

/// <summary>
/// Represents a single connection port on a track segment.
/// </summary>
public sealed record TrackPort(
    string Id,
    string Label,
    double OffsetMm,
    double AngleDeg)
{
    /// <summary>
    /// Unique identifier for this port (e.g., "A", "B", "C").
    /// </summary>
    public string Id { get; } = Id;

    /// <summary>
    /// Display label for this port.
    /// </summary>
    public string Label { get; } = Label;

    /// <summary>
    /// Distance along track from origin in mm.
    /// </summary>
    public double OffsetMm { get; } = OffsetMm;

    /// <summary>
    /// Exit angle in degrees (0-359).
    /// </summary>
    public double AngleDeg { get; } = AngleDeg;
}
