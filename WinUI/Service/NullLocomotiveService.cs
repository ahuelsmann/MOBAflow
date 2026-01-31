// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Domain;
using SharedUI.Interface;

/// <summary>
/// No-op implementation of ILocomotiveService for scenarios where locomotive data is unavailable.
/// 
/// This follows the NullObject pattern: instead of checking for null services,
/// callers receive this no-op implementation that safely does nothing.
/// 
/// Usage:
/// - When locomotive library feature is disabled
/// - When locomotive data file is not available
/// - For testing without external dependencies
/// </summary>
public class NullLocomotiveService : ILocomotiveService
{
    /// <summary>
    /// Returns empty list instead of loading from file.
    /// </summary>
    public Task<List<LocomotiveCategory>> LoadCategoriesAsync()
    {
        return Task.FromResult(new List<LocomotiveCategory>());
    }

    /// <summary>
    /// Returns empty list since no categories are loaded.
    /// </summary>
    public Task<List<LocomotiveSeries>> GetAllSeriesAsync()
    {
        return Task.FromResult(new List<LocomotiveSeries>());
    }

    /// <summary>
    /// Returns empty list since no series are cached.
    /// </summary>
    public List<LocomotiveSeries> FilterSeries(string searchTerm)
    {
        return [];
    }

    /// <summary>
    /// Returns empty list (no series cached).
    /// </summary>
    public List<LocomotiveSeries> GetCachedSeries()
    {
        return [];
    }

    /// <summary>
    /// Returns null (no data available).
    /// </summary>
    public LocomotiveSeries? FindByName(string name)
    {
        return null;
    }

    /// <summary>
    /// Returns null (no data available).
    /// </summary>
    public LocomotiveSeries? FindByPartialName(string partialName)
    {
        return null;
    }
}
