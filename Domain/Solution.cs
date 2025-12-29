// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Newtonsoft.Json;

/// <summary>
/// Solution contains project collection.
/// </summary>
public class Solution
{
    public Solution()
    {
        Name = "New Solution";
        Projects = [];
    }

    public string Name { get; set; }
    public List<Project> Projects { get; set; }

    /// <summary>
    /// Update the current solution instance from another solution instance.
    /// This keeps the same object reference and replaces the data.
    /// </summary>
    public void UpdateFrom(Solution other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));

        Name = other.Name;

        // Replace projects while keeping the same List instance
        Projects.Clear();

        foreach (var p in other.Projects)
        {
            Projects.Add(p);
        }
    }

    /// <summary>
    /// Load solution data from a JSON file and apply it to this instance.
    /// Uses Newtonsoft.Json for better polymorphism support (Workflows, Actions).
    /// </summary>
    public async Task LoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath is required", nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException("Solution file not found", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var loaded = JsonConvert.DeserializeObject<Solution>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore
        });

        if (loaded == null) throw new InvalidOperationException("Failed to deserialize solution file");

        UpdateFrom(loaded);
    }
}