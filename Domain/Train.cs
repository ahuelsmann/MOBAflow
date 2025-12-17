// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Represents a train consisting of locomotives and wagons.
/// </summary>
public class Train
{
    public Train()
    {
        Id = Guid.NewGuid();
        Name = "New Train";
        Description = string.Empty;
        LocomotiveIds = [];
        WagonIds = [];
        TrainType = TrainType.None;
        ServiceType = ServiceType.None;
    }

    public Guid Id { get; set; }
    public bool IsDoubleTraction { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TrainType TrainType { get; set; }
    public ServiceType ServiceType { get; set; }
    
    /// <summary>
    /// Locomotive IDs (resolved at runtime via Project.Locomotives lookup)
    /// </summary>
    public List<Guid> LocomotiveIds { get; set; }
    
    /// <summary>
    /// Wagon IDs (resolved at runtime via Project wagons lookup)
    /// </summary>
    public List<Guid> WagonIds { get; set; }
}
