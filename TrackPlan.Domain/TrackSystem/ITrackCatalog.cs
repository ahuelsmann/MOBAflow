// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.TrackSystem;

/// <summary>
/// Interface for accessing the catalog of available track templates.
/// </summary>
public interface ITrackCatalog
{
    /// <summary>
    /// All available track templates.
    /// </summary>
    IReadOnlyList<TrackTemplate> Templates { get; }

    /// <summary>
    /// All straight track templates.
    /// </summary>
    IEnumerable<TrackTemplate> Straights { get; }

    /// <summary>
    /// All curved track templates.
    /// </summary>
    IEnumerable<TrackTemplate> Curves { get; }

    /// <summary>
    /// All switch track templates.
    /// </summary>
    IEnumerable<TrackTemplate> Switches { get; }

    /// <summary>
    /// Get a template by its ID.
    /// </summary>
    TrackTemplate? GetById(string id);
}
