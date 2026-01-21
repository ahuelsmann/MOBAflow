// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Describes the livery or color scheme applied to rolling stock.
/// </summary>
public enum ColorScheme
{
    /// <summary>No specific livery selected.</summary>
    None,

    /// <summary>Private railway operator livery.</summary>
    Private,

    /// <summary>Special purpose or commemorative livery.</summary>
    Special,

    /// <summary>Classic chromoxidgrün livery.</summary>
    Chromoxidgrün,

    /// <summary>Orientrot livery.</summary>
    Orientrot,

    /// <summary>Ozeanblau (ocean blue) livery.</summary>
    Ozenblau,

    /// <summary>Verkehrsrot (traffic red) livery.</summary>
    Verkehrsrot,
}