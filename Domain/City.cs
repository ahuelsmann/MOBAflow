// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// City - extends Station with list of additional stations.
/// Pure data object.
/// </summary>
public class City : Station
{
    public City()
    {
        Name = "New City";
        Stations = [];
    }

    public List<Station> Stations { get; set; }
}