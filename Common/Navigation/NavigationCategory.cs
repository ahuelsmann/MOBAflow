// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Navigation;

/// <summary>
/// Categories for grouping navigation items with separators.
/// Shared across WinUI app and plugins.
/// </summary>
public enum NavigationCategory
{
    /// <summary>
    /// Core application functionality (overview, settings, solution).
    /// </summary>
    Core = 0,

    /// <summary>
    /// Train control related pages (throttle, locomotive control).
    /// </summary>
    TrainControl = 1,

    /// <summary>
    /// Journey planning and execution pages.
    /// </summary>
    Journey = 2,

    /// <summary>
    /// Solution and project management pages.
    /// </summary>
    Solution = 3,

    /// <summary>
    /// Track plan and signal box related pages.
    /// </summary>
    TrackManagement = 4,

    /// <summary>
    /// Monitoring and diagnostics pages.
    /// </summary>
    Monitoring = 5,

    /// <summary>
    /// Plugin-provided pages.
    /// </summary>
    Plugins = 6,

    /// <summary>
    /// Help and documentation pages.
    /// </summary>
    Help = 7
}
