// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Upcoming feature: Represents a passenger wagon.
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