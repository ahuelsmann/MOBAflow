// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Domain;

/// <summary>
/// Service interface for loading locomotive master data from JSON.
/// Locomotives are read-only reference data (from data.json via DataManager).
/// </summary>
public interface ILocomotiveService
{
    /// <summary>
    /// Loads all locomotive categories from the JSON file.
    /// </summary>
    Task<List<LocomotiveCategory>> LoadCategoriesAsync();

    /// <summary>
    /// Gets a flattened list of all locomotive series (cached after first load).
    /// </summary>
    Task<List<LocomotiveSeries>> GetAllSeriesAsync();

    /// <summary>
    /// Filters locomotive series by search term (case-insensitive).
    /// </summary>
    List<LocomotiveSeries> FilterSeries(string searchTerm);

    /// <summary>
    /// Gets all cached locomotive series (or empty list if not yet loaded).
    /// </summary>
    List<LocomotiveSeries> GetCachedSeries();

    /// <summary>
    /// Finds a locomotive series by its exact name.
    /// Returns null if not found.
    /// </summary>
    LocomotiveSeries? FindByName(string name);

    /// <summary>
    /// Finds a locomotive series by partial name match.
    /// Returns the first match or null if not found.
    /// </summary>
    LocomotiveSeries? FindByPartialName(string partialName);
}
