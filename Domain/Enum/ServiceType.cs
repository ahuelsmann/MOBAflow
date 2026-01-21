// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Specifies the service category a train provides.
/// </summary>
public enum ServiceType
{
    /// <summary>No service classification assigned.</summary>
    None,

    /// <summary>Regional express service (fast regional stops).</summary>
    RegionalExpress,

    /// <summary>Intercity long-distance service.</summary>
    InterCity,

    /// <summary>High-speed intercity express service.</summary>
    InterCityExpress,

    /// <summary>Interregional service.</summary>
    InterRegio,

    /// <summary>Freight service.</summary>
    Freight,

    /// <summary>Special or chartered service.</summary>
    Special,
}