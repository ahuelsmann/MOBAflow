namespace Moba.Backend.Model;

using Enum;

/// <summary>
/// Represents a passenger wagon.
/// </summary>
public class PassengerWagon : Wagon
{
    public PassengerWagon()
    {
        WagonClass = PassengerClass.None;
    }

    /// <summary>
    /// Class (e.g. 1st, 2nd class.)
    /// </summary>
    public PassengerClass WagonClass { get; set; }
}