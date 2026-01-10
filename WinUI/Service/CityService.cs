// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;

using Domain;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Text.Json;

/// <summary>
/// Service for loading city master data from germany-stations.json.
/// Cities are read-only reference data.
/// </summary>
public class CityService : ICityService
{
    private List<City>? _cachedCities;
    private readonly string _jsonFilePath;
    private readonly ILogger<CityService> _logger;

    public CityService(AppSettings settings, ILogger<CityService> logger)
    {
        _logger = logger;
        // Use configured path from appsettings.json
        var configuredPath = settings.CityLibrary.FilePath;

        // Support absolute or relative paths
        _jsonFilePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

        _logger.LogInformation("CityService initialized with path: {JsonFilePath}", _jsonFilePath);
    }

    /// <summary>
    /// Loads all cities from JSON file (cached after first load).
    /// </summary>
    public async Task<List<City>> LoadCitiesAsync()
    {
        if (_cachedCities != null)
            return _cachedCities;

        if (!File.Exists(_jsonFilePath))
        {
            _logger.LogWarning("City library file not found: {JsonFilePath}", _jsonFilePath);
            _cachedCities = [];
            return _cachedCities;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_jsonFilePath);

            // Simple deserialization with System.Text.Json - no complex options needed for POCOs
            var data = JsonSerializer.Deserialize<CitiesData>(json, JsonOptions.Default);

            _cachedCities = data?.Cities ?? [];
            _logger.LogInformation("Loaded {Count} cities from library", _cachedCities.Count);
            return _cachedCities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load city library from {JsonFilePath}", _jsonFilePath);
            _cachedCities = [];
            return _cachedCities;
        }
    }

    /// <summary>
    /// Filters cached cities by search term (name contains).
    /// </summary>
    public List<City> FilterCities(string searchTerm)
    {
        return _cachedCities == null
            ? []
            : string.IsNullOrWhiteSpace(searchTerm)
            ? _cachedCities
            : [.. _cachedCities.Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Gets all cached cities without reloading.
    /// </summary>
    public List<City> GetCachedCities()
    {
        return _cachedCities ?? [];
    }

    /// <summary>
    /// Finds a station by its ID across all cities.
    /// Returns null if station is not found.
    /// </summary>
    public Station? FindStationById(Guid stationId)
    {
        if (_cachedCities == null)
            return null;

        foreach (var city in _cachedCities)
        {
            foreach (var station in city.Stations)
            {
                if (station.Id == stationId)
                    return station;
            }
        }

        return null;
    }

    /// <summary>
    /// Helper class for JSON deserialization.
    /// </summary>
    private class CitiesData
    {
        public List<City> Cities { get; set; } = [];
    }
}