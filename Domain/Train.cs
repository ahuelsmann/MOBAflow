// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Moba.Domain.Enum;

/// <summary>
/// Represents a train consisting of locomotives and wagons.
/// </summary>
public class Train
{
    public Train()
    {
        Name = "New Train";
        Description = string.Empty;
        Wagons = [];
        Locomotives = [];
        TrainType = TrainType.None;
        ServiceType = ServiceType.None;
    }

    public bool IsDoubleTraction { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TrainType TrainType { get; set; }
    public ServiceType ServiceType { get; set; }
    public List<Locomotive> Locomotives { get; set; }
    public List<Wagon> Wagons { get; set; }
}