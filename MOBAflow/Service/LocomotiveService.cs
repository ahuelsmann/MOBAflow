// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;

using Domain;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

/// <summary>
/// Service for locomotive master data from the central <see cref="MasterDataStore"/> instance.
/// Data is loaded from the shared master data file (e.g. data.json).
/// </summary>
internal class LocomotiveService : ILocomotiveService
{
    private readonly MasterDataStore _masterDataStore;

    public LocomotiveService(MasterDataStore masterDataStore, ILogger<LocomotiveService> logger)
    {
        _masterDataStore = masterDataStore ?? throw new ArgumentNullException(nameof(masterDataStore));
        logger.LogInformation("LocomotiveService initialized (data from MasterDataStore)");
    }

    /// <summary>
    /// Returns all locomotive categories from the central master data manager.
    /// </summary>
    public Task<List<LocomotiveCategory>> LoadCategoriesAsync()
    {
        return Task.FromResult(_masterDataStore.Locomotives);
    }

    /// <summary>
    /// Returns a flat list of all locomotive series.
    /// </summary>
    public Task<List<LocomotiveSeries>> GetAllSeriesAsync()
    {
        var series = MasterDataStore.FlattenLocomotiveSeries(_masterDataStore.Locomotives);
        return Task.FromResult(series);
    }

    /// <summary>
    /// Filters series by search term (name contains).
    /// </summary>
    public List<LocomotiveSeries> FilterSeries(string searchTerm)
    {
        var series = MasterDataStore.FlattenLocomotiveSeries(_masterDataStore.Locomotives);
        return string.IsNullOrWhiteSpace(searchTerm)
            ? series
            : [.. series.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Returns the currently loaded series without reloading.
    /// </summary>
    public List<LocomotiveSeries> GetCachedSeries()
    {
        return MasterDataStore.FlattenLocomotiveSeries(_masterDataStore.Locomotives);
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
