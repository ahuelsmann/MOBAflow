// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Domain;
using SharedUI.Interface;

/// <summary>
/// No-op implementation of ICityService for scenarios where city data is unavailable.
/// 
/// This follows the NullObject pattern: instead of checking for null services,
/// callers receive this no-op implementation that safely does nothing.
/// 
/// Usage:
/// - When city library feature is disabled
/// - When city data file is not available
/// - For testing without external dependencies
/// </summary>
public class NullCityService : ICityService
{
    /// <summary>
    /// Returns empty list instead of loading from file.
    /// </summary>
    public Task<List<City>> LoadCitiesAsync()
    {
        return Task.FromResult(new List<City>());
    }

    /// <summary>
    /// Returns empty list since no cities are cached.
    /// </summary>
    public List<City> FilterCities(string searchTerm)
    {
        return new List<City>();
    }

    /// <summary>
    /// Returns empty list (no cities cached).
    /// </summary>
    public List<City> GetCachedCities()
    {
        return new List<City>();
    }

    /// <summary>
    /// Always returns null since no cities are available.
    /// </summary>
    public Station? FindStationById(Guid stationId)
    {
        return null;
    }
}
