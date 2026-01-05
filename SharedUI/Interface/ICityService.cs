// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Domain;

/// <summary>
/// Service interface for loading city master data from JSON.
/// Cities are read-only reference data (e.g., germany-stations.json).
/// </summary>
public interface ICityService
{
    /// <summary>
    /// Loads all cities from the JSON file.
    /// </summary>
    Task<List<City>> LoadCitiesAsync();

    /// <summary>
    /// Filters cities by search term (case-insensitive).
    /// </summary>
    List<City> FilterCities(string searchTerm);

    /// <summary>
    /// Gets all cached cities (or empty list if not yet loaded).
    /// </summary>
    List<City> GetCachedCities();

    /// <summary>
    /// Finds a station by its ID across all cities.
    /// Returns null if station is not found.
    /// </summary>
    Station? FindStationById(Guid stationId);
}