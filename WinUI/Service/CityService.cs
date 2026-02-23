// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;
using Domain;
using Microsoft.Extensions.Logging;
using SharedUI.Interface;

/// <summary>
/// Service für Städte/Bahnhöfe-Stammdaten aus der zentralen DataManager-Instanz.
/// Daten werden aus der gemeinsamen Stammdaten-Datei (z. B. data.json) geladen.
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
    /// Liefert alle Städte aus dem zentralen Stammdaten-Manager.
    /// </summary>
    public Task<List<City>> LoadCitiesAsync()
    {
        return Task.FromResult(_dataManager.Cities);
    }

    /// <summary>
    /// Filtert die Städte nach Suchbegriff (Name enthält).
    /// </summary>
    public List<City> FilterCities(string searchTerm)
    {
        var cities = _dataManager.Cities;
        return string.IsNullOrWhiteSpace(searchTerm)
            ? cities
            : [.. cities.Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Liefert die aktuell geladenen Städte ohne erneutes Laden.
    /// </summary>
    public List<City> GetCachedCities()
    {
        return _dataManager.Cities;
    }

    /// <summary>
    /// Sucht eine Station anhand der ID über alle Städte.
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