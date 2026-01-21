// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
// ReSharper disable InconsistentNaming
namespace Moba.Domain.Enum;

/// <summary>
/// Power system types for locomotives.
/// </summary>
public enum PowerSystem
{
    /// <summary>No power system specified.</summary>
    None,

    /// <summary>Alternating current (AC).</summary>
    AC,

    /// <summary>Direct current (DC).</summary>
    DC,
}