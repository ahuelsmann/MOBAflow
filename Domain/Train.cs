// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Upcoming feature: Represents a train consisting of locomotives and wagons.
/// </summary>
public class Train
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Train"/> class with default values.
    /// </summary>
    public Train()
    {
        Id = Guid.NewGuid();
        Name = "New Train";
        Description = string.Empty;
        Vehicles = new List<Vehicle>();
        TrainType = TrainType.None;
        ServiceType = ServiceType.None;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the train.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the train uses double traction.
    /// </summary>
    public bool IsDoubleTraction { get; set; }

    /// <summary>
    /// Gets or sets the display name of the train.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a description of the train.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the train type (e.g., passenger or freight).
    /// </summary>
    public TrainType TrainType { get; set; }

    /// <summary>
    /// Gets or sets the service type (e.g., IC, RE, S-Bahn).
    /// </summary>
    public ServiceType ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the ordered, freely mixed list of vehicles that form this train.
    /// </summary>
    public List<Vehicle> Vehicles { get; set; }
}