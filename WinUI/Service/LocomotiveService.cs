// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;
using Domain;
using Microsoft.Extensions.Logging;
using SharedUI.Interface;

/// <summary>
/// Service for locomotive master data from the central DataManager instance.
/// Data is loaded from the shared master data file (e.g. data.json).
/// </summary>
internal class LocomotiveService : ILocomotiveService
{
    private readonly DataManager _dataManager;
    private readonly ILogger<LocomotiveService> _logger;

    public LocomotiveService(DataManager dataManager, ILogger<LocomotiveService> logger)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _logger = logger;
        _logger.LogInformation("LocomotiveService initialized (data from DataManager)");
    }

    /// <summary>
    /// Returns all locomotive categories from the central master data manager.
    /// </summary>
    public Task<List<LocomotiveCategory>> LoadCategoriesAsync()
    {
        return Task.FromResult(_dataManager.Locomotives);
    }

    /// <summary>
    /// Returns a flat list of all locomotive series.
    /// </summary>
    public Task<List<LocomotiveSeries>> GetAllSeriesAsync()
    {
        var series = DataManager.FlattenLocomotiveSeries(_dataManager.Locomotives);
        return Task.FromResult(series);
    }

    /// <summary>
    /// Filters series by search term (name contains).
    /// </summary>
    public List<LocomotiveSeries> FilterSeries(string searchTerm)
    {
        var series = DataManager.FlattenLocomotiveSeries(_dataManager.Locomotives);
        return string.IsNullOrWhiteSpace(searchTerm)
            ? series
            : [.. series.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Returns the currently loaded series without reloading.
    /// </summary>
    public List<LocomotiveSeries> GetCachedSeries()
    {
        return DataManager.FlattenLocomotiveSeries(_dataManager.Locomotives);
    }

    /// <summary>
    /// Finds a series by exact name.
    /// </summary>
    public LocomotiveSeries? FindByName(string name)
    {
        return GetCachedSeries().FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a series by partial name.
    /// </summary>
    public LocomotiveSeries? FindByPartialName(string partialName)
    {
        return GetCachedSeries().FirstOrDefault(s =>
            s.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));
    }
}
