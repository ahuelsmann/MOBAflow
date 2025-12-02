// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Moba.Domain.Enum;

/// <summary>
/// Additional details about a locomotive or wagon.
/// </summary>
public class Details
{
    public uint Axles { get; set; } = 2; // Number of axles on a wagon
    public Epoch? Epoch { get; set; }
    public string? RailroadCompany { get; set; }
    public PowerSystem? Power { get; set; }
    public DigitalSystem? Digital { get; set; }
    public string Description { get; set; } = string.Empty;
}