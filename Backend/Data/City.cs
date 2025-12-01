// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Data;

using Moba.Domain;

/// <summary>
/// This class represents a city. City also functions as a main station.
/// Additional stops in the city can be added via the list of Stations.
/// Cities serve as master data that can be reused across multiple journeys.
/// </summary>
public class City : Station
{
    public City()
    {
        Name = "New City";
    }

    /// <summary>
    /// List of train stations in this city.
    /// For cities with only one station (Main Station), this list contains one entry.
    /// For larger cities, multiple stations can be configured (e.g., "Hamburg Hbf", "Hamburg-Altona").
    /// </summary>
    public List<Station>? Stations { get; set; }
}