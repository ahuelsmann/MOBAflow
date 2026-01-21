// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Identifies the operational purpose of a train.
/// </summary>
public enum TrainType
{
    /// <summary>No classification applied.</summary>
    None,

    /// <summary>Passenger-carrying consist.</summary>
    Passenger,

    /// <summary>Freight service consist.</summary>
    Freight,

    /// <summary>Maintenance or works train.</summary>
    Maintenance,

    /// <summary>Special or charter service.</summary>
    Special,
}