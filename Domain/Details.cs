// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Upcoming feature: Additional details about a locomotive or wagon.
/// </summary>
public class Details
{
    /// <summary>
    /// Gets or sets the number of axles on the vehicle.
    /// </summary>
    public uint Axles { get; set; } = 2;

    /// <summary>
    /// Gets or sets the railway epoch (e.g., III, IV, V).
    /// </summary>
    public Epoch? Epoch { get; set; }

    /// <summary>
    /// Gets or sets the railroad company (e.g., DB, SBB, ÖBB).
    /// </summary>
    public string? RailroadCompany { get; set; }

    /// <summary>
    /// Gets or sets the power system (e.g., AC, DC, digital).
    /// </summary>
    public PowerSystem? Power { get; set; }

    /// <summary>
    /// Gets or sets the digital system (e.g., DCC, Motorola).
    /// </summary>
    public DigitalSystem? Digital { get; set; }

    /// <summary>
    /// Gets or sets a free-text description of the vehicle details.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}