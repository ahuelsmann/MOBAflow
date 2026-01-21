// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
// ReSharper disable InconsistentNaming
namespace Moba.Domain.Enum;

/// <summary>
/// Digital control system types for model railways.
/// </summary>
public enum DigitalSystem
{
    /// <summary>No digital control system specified.</summary>
    None,

    /// <summary>Analog control.</summary>
    Analog,

    /// <summary>Digital Command Control (DCC).</summary>
    DCC,

    /// <summary>Motorola digital system.</summary>
    Motorola,

    /// <summary>Selectrix digital system.</summary>
    Selectrix,

    /// <summary>MFX digital system.</summary>
    MFX
}