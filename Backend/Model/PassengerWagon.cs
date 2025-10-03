namespace Moba.Backend.Model;

using Moba.Backend.Model.Enum;

public class PassengerWagon : Wagon
{
    public PassengerWagon()
    {
        WagonClass = PassengerClass.Second;
    }

    /// <summary>
    /// Wagenklasse (z.B. 1., 2. Klasse, Speisewagen, etc.)
    /// </summary>
    public PassengerClass WagonClass { get; set; }
}