// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.Base;

/// <summary>
/// Base for all track segments.
/// Records enable value semantics and immutable geometry with extensible connection logic.
/// </summary>
public abstract record Segment
{
    /// <summary>
    /// Unique identifier of the segment in the track plan.
    /// </summary>
    public Guid No { get; set; } = Guid.NewGuid();
}
