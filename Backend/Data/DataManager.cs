// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Data;

using Domain;
using System.Text.Json;

public class DataManager
{
    public DataManager()
    {
        Cities = [];
    }

    public List<City> Cities { get; set; }

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
}
