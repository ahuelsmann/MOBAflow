// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Factory interface for creating track edges from a track type specification.
/// Enables fluent syntax like: topology.Add(PikoA.R9).Port("A")
/// </summary>
public interface ITrackTypeFactory
{
    /// <summary>
    /// The template ID for this track type (e.g., "R9", "WR", "G231").
    /// </summary>
    string TemplateId { get; }

    /// <summary>
    /// Create a new track edge instance for this type.
    /// </summary>
    TrackEdge CreateEdge();
}
