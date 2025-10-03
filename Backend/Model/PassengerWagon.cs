namespace Moba.Backend.Model;

using Moba.Backend.Model.Enum;

public class PassengerWagon : Wagon
{
    public PassengerWagon()
    {
        WagonClass = PassengerClass.Second;
    }

    /// <summary>
    /// Car class (e.g. 1st, 2nd class, dining car, etc.)
    /// </summary>
    public PassengerClass WagonClass { get; set; }
}