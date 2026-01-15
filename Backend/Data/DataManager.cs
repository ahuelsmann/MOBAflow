// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Data;

using Domain;
using System.Text.Json;

/// <summary>
/// Manages master data loading for cities/stations and locomotive libraries.
/// Supports separate JSON files for each data type.
/// </summary>
public class DataManager
{
    public DataManager()
    {
        Cities = [];
        Locomotives = [];
    }

    /// <summary>
    /// List of cities with their stations (loaded from germany-stations.json).
    /// </summary>
    public List<City> Cities { get; set; }

    /// <summary>
    /// List of locomotive categories with their series (loaded from germany-locomotives.json).
    /// </summary>
    public List<LocomotiveCategory> Locomotives { get; set; }

    /// <summary>
    /// Load the data.
    /// </summary>
    /// <param name="path">Expects the full path including file name.</param>
    /// <returns>Returns an instance of the loaded data as DataManager, or null if loading fails.</returns>
    public static async Task<DataManager?> LoadAsync(string path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(json))
                {
                    var temp = JsonSerializer.Deserialize<DataManager>(json, JsonOptions.Default);
                    return temp;
                }
            }
        }
        catch
        {
            // Return null for any deserialization errors
        }
        return null;
    }

    /// <summary>
    /// Load locomotive library from a separate JSON file.
    /// </summary>
    /// <param name="path">Expects the full path including file name.</param>
    /// <returns>Returns a list of locomotive categories, or empty list if loading fails.</returns>
    public static async Task<List<LocomotiveCategory>> LoadLocomotivesAsync(string path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonSerializer.Deserialize<LocomotiveLibraryData>(json, JsonOptions.Default);
                    return data?.Locomotives ?? [];
                }
            }
        }
        catch
        {
            // Return empty list for any deserialization errors
        }
        return [];
    }

    /// <summary>
    /// Flattens locomotive categories into a single ordered list of series.
    /// </summary>
    public static List<LocomotiveSeries> FlattenLocomotiveSeries(List<LocomotiveCategory> categories)
    {
        return categories
            .SelectMany(cat => cat.Series)
            .OrderBy(s => s.Name)
            .ToList();
    }
}

/// <summary>
/// Helper class for deserializing locomotive library JSON files.
/// </summary>
internal class LocomotiveLibraryData
{
    public List<LocomotiveCategory> Locomotives { get; set; } = [];
}
