// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.Common.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Service for loading city master data from germany-stations.json.
/// Cities are read-only reference data.
/// </summary>
public class CityService : ICityService
{
    private List<City>? _cachedCities;
    private readonly string _jsonFilePath;

    public CityService(AppSettings settings)
    {
        // Use configured path from appsettings.json
        var configuredPath = settings.CityLibrary.FilePath;
        
        // Support absolute or relative paths
        _jsonFilePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
            
        System.Diagnostics.Debug.WriteLine($"📚 CityService initialized with path: {_jsonFilePath}");
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
            System.Diagnostics.Debug.WriteLine($"⚠️ City library file not found: {_jsonFilePath}");
            _cachedCities = [];
            return _cachedCities;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_jsonFilePath);
            
            // Simple deserialization with Newtonsoft.Json - no complex options needed for POCOs
            var data = JsonConvert.DeserializeObject<CitiesData>(json);

            _cachedCities = data?.Cities ?? [];
            System.Diagnostics.Debug.WriteLine($"✅ Loaded {_cachedCities.Count} cities from library");
            return _cachedCities;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to load city library: {ex.Message}");
            _cachedCities = [];
            return _cachedCities;
        }
    }

    /// <summary>
    /// Filters cached cities by search term (name contains).
    /// </summary>
    public List<City> FilterCities(string searchTerm)
    {
        if (_cachedCities == null)
            return [];

        if (string.IsNullOrWhiteSpace(searchTerm))
            return _cachedCities;

        return _cachedCities
            .Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Gets all cached cities without reloading.
    /// </summary>
    public List<City> GetCachedCities()
    {
        return _cachedCities ?? [];
    }

    /// <summary>
    /// Helper class for JSON deserialization.
    /// </summary>
    private class CitiesData
    {
        public List<City> Cities { get; set; } = [];
    }
}
