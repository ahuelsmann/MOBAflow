// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>
/// Describes the passenger service class or coach type.
/// </summary>
public enum PassengerClass
{
    /// <summary>No class assigned.</summary>
    None,

    /// <summary>First-class coach.</summary>
    First,

    /// <summary>Second-class coach.</summary>
    Second,

    /// <summary>Third-class coach.</summary>
    Third,

    /// <summary>Dedicated dining coach.</summary>
    Dining,

    /// <summary>Bistro or snack coach.</summary>
    Bistro,

    /// <summary>Sleeping coach.</summary>
    Sleeping,

    /// <summary>Baggage coach.</summary>
    Baggage,

    /// <summary>Coach with bike storage.</summary>
    Bike,

    /// <summary>Mail or postal coach.</summary>
    Mail,

    /// <summary>Coach providing mixed accommodations.</summary>
    Mixed,
}