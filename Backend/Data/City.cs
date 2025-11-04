namespace Moba.Backend.Data;

using Model;

/// <summary>
/// This class represents a city with at least one train station.
/// Cities serve as master data that can be reused across multiple journeys.
/// </summary>
public class City
{
    public City()
    {
        Name = "New City";
        Stations = [];
    }

    /// <summary>
    /// Name of city.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// List of train stations in this city.
    /// For cities with only one station (Main Station), this list contains one entry.
    /// For larger cities, multiple stations can be configured (e.g., "Hamburg Hbf", "Hamburg-Altona").
    /// </summary>
    public List<Station> Stations { get; set; }
}