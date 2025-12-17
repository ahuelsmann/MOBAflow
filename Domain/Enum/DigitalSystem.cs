// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
// ReSharper disable InconsistentNaming
namespace Moba.Domain.Enum;

/// <summary>
/// Digital control system types for model railways.
/// </summary>
public enum DigitalSystem
{
    None,
    Analog,
    DCC,
    Motorola,
    Selectrix,
    MFX
}