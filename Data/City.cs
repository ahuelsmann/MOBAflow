namespace Moba.Data;

/// <summary>
/// This class represents a city with at least one train station.
/// </summary>
public class City
{
    public City()
    {
        Name = "New City";
    }

    /// <summary>
    /// Name of city.
    /// </summary>
    public string Name { get; set; }
}