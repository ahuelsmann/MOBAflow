// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Controls how the Journeys page station list is filtered for display.
/// </summary>
public enum StationListViewMode
{
    /// <summary>
    /// Show only real stops (non-virtual stations).
    /// </summary>
    StopsOnly = 0,

    /// <summary>
    /// Show the full journey timeline including virtual events.
    /// </summary>
    FullTimeline = 1
}
