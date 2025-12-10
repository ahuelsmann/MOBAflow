// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Data;
using Domain;

using Newtonsoft.Json;

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
    /// <returns>Returns an instance of the loaded data as DataManager.</returns>
    public static async Task<DataManager?> LoadAsync(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(json))
            {
                var temp = JsonConvert.DeserializeObject<DataManager>(json);
                return temp;
            }
        }
        return null;
    }
}
