// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;
using Domain;
using Microsoft.Extensions.Logging;
using SharedUI.Interface;

/// <summary>
/// Service for city/station master data from the central DataManager instance.
/// Data is loaded from the shared master data file (e.g. data.json).
/// </summary>
internal class CityService : ICityService
{
    private readonly DataManager _dataManager;
    private readonly ILogger<CityService> _logger;

    public CityService(DataManager dataManager, ILogger<CityService> logger)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _logger = logger;
        _logger.LogInformation("CityService initialized (data from DataManager)");
    }

    /// <summary>
    /// Returns all cities from the central master data manager.
    /// </summary>
    public Task<List<City>> LoadCitiesAsync()
    {
        return Task.FromResult(_dataManager.Cities);
    }

    /// <summary>
    /// Filters cities by search term (name contains).
    /// </summary>
    public List<City> FilterCities(string searchTerm)
    {
        var cities = _dataManager.Cities;
        return string.IsNullOrWhiteSpace(searchTerm)
            ? cities
            : [.. cities.Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Returns the currently loaded cities without reloading.
    /// </summary>
    public List<City> GetCachedCities()
    {
        return _dataManager.Cities;
    }

    /// <summary>
    /// Finds a station by ID across all cities.
    /// </summary>
    public Station? FindStationById(Guid stationId)
    {
        foreach (var city in _dataManager.Cities)
        {
            foreach (var station in city.Stations)
            {
                if (station.Id == stationId)
                    return station;
            }
        }
        return null;
    }
}