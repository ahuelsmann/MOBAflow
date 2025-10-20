namespace Moba.Backend.Model;

using Enum;

public class GoodsWagon : Wagon
{
    public GoodsWagon()
    {
        Cargo = CargoType.General;
    }

    /// <summary>
    /// Type of cargo (e.g. Container, Coal, Wood, Oil, etc.)
    /// </summary>
    public CargoType Cargo { get; set; }
}