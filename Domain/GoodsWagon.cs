// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Upcoming feature: Represents a freight wagon.
/// </summary>
public class GoodsWagon : Wagon
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoodsWagon"/> class with default cargo.
    /// </summary>
    public GoodsWagon()
    {
        Cargo = CargoType.None;
    }

    /// <summary>
    /// Type of cargo (e.g. Container, Coal, Wood, Oil, etc.)
    /// </summary>
    public CargoType Cargo { get; set; }
}