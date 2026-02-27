// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Data;

using Domain;

using System.Text.Json;

/// <summary>
/// Central master data class for cities/stations and locomotive library.
/// Loads and saves from a shared JSON file (e.g. data.json), analogous to the Solution class.
/// </summary>
public class DataManager
{
    /// <summary>
    /// Current schema version for the master data JSON format.
    /// Increment when making breaking schema changes.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    public DataManager()
    {
        Cities = [];
        Locomotives = [];
        ViessmannMultiplexSignals = [];
        SchemaVersion = CurrentSchemaVersion;
    }

    /// <summary>
    /// Schema version of this master data file (for detecting incompatible formats).
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// List of cities with their stations (from the shared master data file).
    /// </summary>
    public List<City> Cities { get; set; }

    /// <summary>
    /// List of locomotive categories with their series (from the shared master data file).
    /// </summary>
    public List<LocomotiveCategory> Locomotives { get; set; }

    /// <summary>
    /// Viessmann Multiplex signals (Ks main signal, Ks distant signal) for the ComboBox in the signal box.
    /// Source: https://viessmann-modell.com/sortiment/spur-h0/signale/
    /// </summary>
    public List<ViessmannMultiplexSignalEntry> ViessmannMultiplexSignals { get; set; }

    /// <summary>
    /// Updates this instance from another DataManager instance.
    /// Keeps the same object reference and replaces the data.
    /// </summary>
    public void UpdateFrom(DataManager other)
    {
        ArgumentNullException.ThrowIfNull(other);
        SchemaVersion = other.SchemaVersion;
        Cities.Clear();
        foreach (var c in other.Cities)
            Cities.Add(c);
        Locomotives.Clear();
        foreach (var l in other.Locomotives)
            Locomotives.Add(l);
        ViessmannMultiplexSignals.Clear();
        foreach (var s in other.ViessmannMultiplexSignals)
            ViessmannMultiplexSignals.Add(s);
    }

    /// <summary>
    /// Loads master data from a JSON file and applies it to this instance.
    /// Analogous to Solution.LoadAsync. If file is missing, lists are cleared (no exception thrown).
    /// </summary>
    public async Task LoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required", nameof(filePath));

        if (!File.Exists(filePath))
        {
            Cities.Clear();
            Locomotives.Clear();
            ViessmannMultiplexSignals.Clear();
            return;
        }

        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            Cities.Clear();
            Locomotives.Clear();
            ViessmannMultiplexSignals.Clear();
            return;
        }
        var loaded = JsonSerializer.Deserialize<DataManager>(json, JsonOptions.Default)
            ?? throw new InvalidOperationException("Failed to deserialize master data file");
        UpdateFrom(loaded);
    }

    /// <summary>
    /// Saves the current master data to a JSON file.
    /// Analogous to Solution save.
    /// </summary>
    public async Task SaveAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required", nameof(filePath));

        var json = JsonSerializer.Serialize(this, JsonOptions.Default);
        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads master data from a file (static, for tests or one-time loading).
    /// </summary>
    /// <param name="path">Full path including file name.</param>
    /// <returns>Loaded instance or null on error.</returns>
    public static async Task<DataManager?> LoadFromFileAsync(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var temp = JsonSerializer.Deserialize<DataManager>(json, JsonOptions.Default);
                    return temp;
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Loads only the locomotive library from a separate JSON file (legacy; default is data.json with Cities + Locomotives).
    /// </summary>
    /// <param name="path">Full path including file name.</param>
    /// <returns>List of locomotive categories or empty list on error.</returns>
    public static async Task<List<LocomotiveCategory>> LoadLocomotivesAsync(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<LocomotiveLibraryData>(json, JsonOptions.Default);
                    return data?.Locomotives ?? [];
                }
                catch (JsonException)
                {
                    return [];
                }
            }
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
/// Entry for a Viessmann Multiplex signal (main or distant signal).
/// </summary>
public abstract class ViessmannMultiplexSignalEntry
{
    /// <summary>Viessmann article number (e.g. "4040", "4046").</summary>
    public string ArticleNumber { get; set; } = string.Empty;

    /// <summary>Display name for the ComboBox (e.g. "Ks-Vorsignal", "Ks-Mehrabschnittssignal").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Role: "main" = main signal, "distant" = distant signal.</summary>
    public string Role { get; set; } = "main";
}

/// <summary>
/// Helper class for deserializing locomotive library JSON files.
/// </summary>
internal class LocomotiveLibraryData
{
    public List<LocomotiveCategory> Locomotives { get; set; } = [];
}
