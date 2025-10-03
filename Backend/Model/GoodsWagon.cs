namespace Moba.Backend.Model;

using Moba.Backend.Model.Enum;

public class GoodsWagon : Wagon
{
    public GoodsWagon()
    {
        Cargo = CargoType.General;
    }

    /// <summary>
    /// Art der Ladung (z.B. Container, Kohle, Holz, Öl, etc.)
    /// </summary>
    public CargoType Cargo { get; set; }
}