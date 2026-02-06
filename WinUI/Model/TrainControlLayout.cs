// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Model;

/// <summary>
/// TrainControl page layout variants for skin selection.
/// Allows users to switch between different UI layouts while keeping theme independent.
/// </summary>
public enum TrainControlLayout
{
    /// <summary>
    /// Original MOBAflow layout (light/dark compatible).
    /// Classic controls and display elements from version 1.
    /// </summary>
    Original = 0,

    /// <summary>
    /// Modern responsive layout with VSM support.
    /// Current default design with Fluent Design System compliance.
    /// </summary>
    Modern = 1,

    /// <summary>
    /// ESU CabControl-inspired layout.
    /// Features round throttle control, function key grid, and locomotive preview.
    /// Optimized for ESU CabControl theme but works with any theme.
    /// </summary>
    Esu = 2,

    /// <summary>
    /// Märklin Central Station-inspired layout.
    /// Dual-cab design with two independent locomotive controls.
    /// Optimized for MärklinCS theme but works with any theme.
    /// </summary>
    Märklin = 3
}
