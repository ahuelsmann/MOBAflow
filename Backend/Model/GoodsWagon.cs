namespace Moba.Backend.Model;

using Enum;

/// <summary>
/// Represents a freight wagon.
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