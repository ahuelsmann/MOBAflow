// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;
using Domain;
using Microsoft.Extensions.Logging;
using SharedUI.Interface;

/// <summary>
/// Service für Lokomotiv-Stammdaten aus der zentralen DataManager-Instanz.
/// Daten werden aus der gemeinsamen Stammdaten-Datei (z. B. data.json) geladen.
/// </summary>
public class LocomotiveService : ILocomotiveService
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
    /// Liefert alle Lokomotiv-Kategorien aus dem zentralen Stammdaten-Manager.
    /// </summary>
    public Task<List<LocomotiveCategory>> LoadCategoriesAsync()
    {
        return Task.FromResult(_dataManager.Locomotives);
    }

    /// <summary>
    /// Liefert eine flache Liste aller Baureihen.
    /// </summary>
    public Task<List<LocomotiveSeries>> GetAllSeriesAsync()
    {
        var series = DataManager.FlattenLocomotiveSeries(_dataManager.Locomotives);
        return Task.FromResult(series);
    }

    /// <summary>
    /// Filtert die Baureihen nach Suchbegriff (Name enthält).
    /// </summary>
    public List<LocomotiveSeries> FilterSeries(string searchTerm)
    {
        var series = DataManager.FlattenLocomotiveSeries(_dataManager.Locomotives);
        return string.IsNullOrWhiteSpace(searchTerm)
            ? series
            : [.. series.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Liefert die aktuell geladenen Baureihen ohne erneutes Laden.
    /// </summary>
    public List<LocomotiveSeries> GetCachedSeries()
    {
        return DataManager.FlattenLocomotiveSeries(_dataManager.Locomotives);
    }

    /// <summary>
    /// Sucht eine Baureihe anhand des exakten Namens.
    /// </summary>
    public LocomotiveSeries? FindByName(string name)
    {
        return GetCachedSeries().FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sucht eine Baureihe anhand Teilnamens.
    /// </summary>
    public LocomotiveSeries? FindByPartialName(string partialName)
    {
        return GetCachedSeries().FirstOrDefault(s =>
            s.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));
    }
}
