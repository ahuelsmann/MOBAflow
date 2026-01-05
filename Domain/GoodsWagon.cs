// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Upcoming feature: Represents a freight wagon.
/// </summary>
public class GoodsWagon : Wagon
{
    public GoodsWagon()
    {
        Cargo = CargoType.None;
    }

    /// <summary>
    /// Type of cargo (e.g. Container, Coal, Wood, Oil, etc.)
    /// </summary>
    public CargoType Cargo { get; set; }
}