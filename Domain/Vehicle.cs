// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

public class Vehicle
{
    public Guid VehicleId { get; set; }

    public TrainVehicleKind VehicleKind { get; set; }

    public bool IsReversed { get; set; }
}