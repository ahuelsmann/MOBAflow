// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using System.Text.Json;

/// <summary>
/// Solution contains project collection.
/// </summary>
public class Solution
{
    /// <summary>
    /// Current schema version for Solution JSON format.
    /// Increment this when breaking changes are made to the schema.
    /// </summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="Solution"/> class with default name and empty project list.
    /// </summary>
    public Solution()
    {
        Name = "New Solution";
        Projects = [];
        SchemaVersion = CurrentSchemaVersion;
    }

    /// <summary>
    /// Gets or sets the solution name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the list of projects contained in this solution.
    /// </summary>
    public List<Project> Projects { get; set; }

    /// <summary>
    /// Schema version of this solution file.
    /// Used to detect incompatible file formats.
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// Update the current solution instance from another solution instance.
    /// This keeps the same object reference and replaces the data.
    /// </summary>
    public void UpdateFrom(Solution other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Name = other.Name;
        SchemaVersion = other.SchemaVersion;

        // Replace projects while keeping the same List instance
        Projects.Clear();

        foreach (var p in other.Projects)
        {
            Projects.Add(p);
        }
    }

    /// <summary>
    /// Load solution data from a JSON file and apply it to this instance.
    /// Uses System.Text.Json for better polymorphism support (Workflows, Actions).
    /// </summary>
    public async Task LoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath is required", nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException("Solution file not found", filePath);

        var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var loaded = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default) ?? throw new InvalidOperationException("Failed to deserialize solution file");
        SolutionMigrator.MigrateToCurrent(loaded);
        UpdateFrom(loaded);
    }
}

/// <summary>Applies safe in-memory defaults when opening older solution schemas.</summary>
public static class SolutionMigrator
{
    public static void MigrateToCurrent(Solution solution)
    {
        ArgumentNullException.ThrowIfNull(solution);
        if (solution.SchemaVersion > Solution.CurrentSchemaVersion)
            throw new InvalidOperationException($"Solution schema {solution.SchemaVersion} is newer than supported schema {Solution.CurrentSchemaVersion}.");

        foreach (var project in solution.Projects)
        {
            if (project.Id == Guid.Empty) project.Id = Guid.NewGuid();
            foreach (var journey in project.Journeys)
            {
                journey.Stations ??= [];
                var firstStationIndex = journey.Stations
                    .Take(Math.Clamp((int)journey.FirstPos, 0, journey.Stations.Count))
                    .Count(station => !station.IsLegacyEventPlaceholder);
                journey.Stations.RemoveAll(station => station.IsLegacyEventPlaceholder);
                journey.FirstPos = journey.Stations.Count == 0
                    ? 0
                    : (uint)Math.Clamp(firstStationIndex, 0, journey.Stations.Count - 1);

                journey.FeedbackSequence ??= [];
                foreach (var step in journey.FeedbackSequence)
                {
                    if (step.RepeatCount == 0) step.RepeatCount = 1;
                    step.StopTransition ??= new JourneyStopTransition();
                    step.Conditions ??= [];
                }
            }
        }

        solution.SchemaVersion = Solution.CurrentSchemaVersion;
    }
}
