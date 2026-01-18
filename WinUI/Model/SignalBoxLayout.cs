// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Model;

/// <summary>
/// SignalBox page layout variants for skin selection.
/// Allows users to switch between different UI layouts while keeping theme independent.
/// </summary>
public enum SignalBoxLayout
{
    /// <summary>
    /// Original MOBAflow layout (light/dark compatible).
    /// Classic grid-based signal box editor from version 1.
    /// </summary>
    Original = 0,

    /// <summary>
    /// Modern responsive layout with VSM support.
    /// Current default design with Fluent Design System compliance.
    /// </summary>
    Modern = 1,

    /// <summary>
    /// ESU CabControl-inspired layout.
    /// Optimized for ESU electronic signal box operations.
    /// Works with any theme but recommended for ESU CabControl theme.
    /// </summary>
    ESU = 2,

    /// <summary>
    /// Roco Z21-inspired layout.
    /// Optimized for Roco Z21 electronic signal box operations.
    /// Works with any theme but recommended for RocoZ21 theme.
    /// </summary>
    Z21 = 3
}
