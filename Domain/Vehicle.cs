namespace Moba.Domain;

using Enum;

public class Vehicle
{
    public Guid VehicleId { get; set; }

    public TrainVehicleKind VehicleKind { get; set; }

    public bool IsReversed { get; set; }
}