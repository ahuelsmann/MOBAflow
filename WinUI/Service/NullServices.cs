// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Domain;
using SharedUI.Interface;

/// <summary>
/// Consolidated NullObject implementations for WinUI service fallbacks.
///
/// These follow the NullObject pattern: instead of checking for null services,
/// callers receive no-op implementations that safely do nothing.
/// Used when features are disabled, data is unavailable, or during testing.
/// </summary>

/// <summary>
/// No-op implementation of <see cref="ICityService"/>.
/// Returns empty collections and null for lookups.
/// </summary>
internal class NullCityService : ICityService
{
    public Task<List<City>> LoadCitiesAsync() => Task.FromResult(new List<City>());
    public List<City> FilterCities(string searchTerm) => [];
    public List<City> GetCachedCities() => [];
    public Station? FindStationById(Guid stationId) => null;
}

/// <summary>
/// No-op implementation of <see cref="ILocomotiveService"/>.
/// Returns empty collections and null for lookups.
/// </summary>
internal class NullLocomotiveService : ILocomotiveService
{
    public Task<List<LocomotiveCategory>> LoadCategoriesAsync() => Task.FromResult(new List<LocomotiveCategory>());
    public Task<List<LocomotiveSeries>> GetAllSeriesAsync() => Task.FromResult(new List<LocomotiveSeries>());
    public List<LocomotiveSeries> FilterSeries(string searchTerm) => [];
    public List<LocomotiveSeries> GetCachedSeries() => [];
    public LocomotiveSeries? FindByName(string name) => null;
    public LocomotiveSeries? FindByPartialName(string partialName) => null;
}

/// <summary>
/// No-op implementation of <see cref="ISettingsService"/>.
/// All async methods complete immediately; properties return defaults.
/// </summary>
internal class NullSettingsService : ISettingsService
{
    public AppSettings GetSettings() => new();
    public Task LoadSettingsAsync() => Task.CompletedTask;
    public Task SaveSettingsAsync(AppSettings settings) => Task.CompletedTask;
    public Task ResetToDefaultsAsync() => Task.CompletedTask;
    public string? LastSolutionPath { get; set; }
    public bool AutoLoadLastSolution { get; set; }
}
