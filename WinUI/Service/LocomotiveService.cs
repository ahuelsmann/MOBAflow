// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;

using Common.Configuration;

using Domain;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

/// <summary>
/// Service for loading locomotive master data from germany-locomotives.json.
/// Locomotives are read-only reference data with caching.
/// </summary>
public class LocomotiveService : ILocomotiveService
{
    private List<LocomotiveCategory>? _cachedCategories;
    private List<LocomotiveSeries>? _cachedSeries;
    private readonly string _jsonFilePath;
    private readonly ILogger<LocomotiveService> _logger;

    public LocomotiveService(AppSettings settings, ILogger<LocomotiveService> logger)
    {
        _logger = logger;

        // Use configured path from appsettings.json
        var configuredPath = settings.LocomotiveLibrary.FilePath;

        // Support absolute or relative paths
        _jsonFilePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

        _logger.LogInformation("LocomotiveService initialized with path: {JsonFilePath}", _jsonFilePath);
    }

    /// <summary>
    /// Loads all locomotive categories from JSON file (cached after first load).
    /// </summary>
    public async Task<List<LocomotiveCategory>> LoadCategoriesAsync()
    {
        if (_cachedCategories != null)
            return _cachedCategories;

        _cachedCategories = await DataManager.LoadLocomotivesAsync(_jsonFilePath).ConfigureAwait(false);
        _cachedSeries = DataManager.FlattenLocomotiveSeries(_cachedCategories);

        _logger.LogInformation("Loaded {CategoryCount} categories with {SeriesCount} locomotive series from library",
            _cachedCategories.Count, _cachedSeries.Count);

        return _cachedCategories;
    }

    /// <summary>
    /// Gets a flattened list of all locomotive series.
    /// </summary>
    public async Task<List<LocomotiveSeries>> GetAllSeriesAsync()
    {
        if (_cachedSeries != null)
            return _cachedSeries;

        await LoadCategoriesAsync().ConfigureAwait(false);
        return _cachedSeries ?? [];
    }

    /// <summary>
    /// Filters cached series by search term (name contains).
    /// </summary>
    public List<LocomotiveSeries> FilterSeries(string searchTerm)
    {
        return _cachedSeries == null
            ? []
            : string.IsNullOrWhiteSpace(searchTerm)
            ? _cachedSeries
            : [.. _cachedSeries.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Gets all cached series without reloading.
    /// </summary>
    public List<LocomotiveSeries> GetCachedSeries()
    {
        return _cachedSeries ?? [];
    }

    /// <summary>
    /// Finds a locomotive series by its exact name.
    /// </summary>
    public LocomotiveSeries? FindByName(string name)
    {
        return _cachedSeries?.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a locomotive series by partial name match.
    /// </summary>
    public LocomotiveSeries? FindByPartialName(string partialName)
    {
        return _cachedSeries?.FirstOrDefault(s =>
            s.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));
    }
}
