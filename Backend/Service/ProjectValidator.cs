// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;
using Microsoft.Extensions.Logging;

/// <summary>
/// Validates Project/Solution data structure against completeness requirements.
/// Ensures all Domain model types are represented in the loaded project.
/// </summary>
public interface IProjectValidator
{
    /// <summary>
    /// Validates that a project contains examples of all required domain types.
    /// </summary>
    /// <returns>Validation result with any warnings or errors</returns>
    ProjectValidationResult ValidateCompleteness(Solution solution);
}

public class ProjectValidator : IProjectValidator
{
    private readonly ILogger<ProjectValidator> _logger;

    public ProjectValidator(ILogger<ProjectValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Validates project completeness by checking that all critical domain types have at least one example.
    /// This is especially important for example-solution.json to ensure it's a good reference.
    /// </summary>
    public ProjectValidationResult ValidateCompleteness(Solution solution)
    {
        var result = new ProjectValidationResult();

        if (solution?.Projects.Count == 0)
        {
            result.AddError("No projects loaded in solution");
            return result;
        }

        // Validate each project in the solution
        foreach (var projectIndex in Enumerable.Range(0, solution.Projects.Count))
        {
            var project = solution.Projects[projectIndex];
            ValidateProjectContent(project, projectIndex, result);
        }

        return result;
    }

    /// <summary>
    /// Validates a single project's content for domain type completeness.
    /// </summary>
    private void ValidateProjectContent(Project project, int projectIndex, ProjectValidationResult result)
    {
        var projectName = string.IsNullOrEmpty(project.Name) ? $"Project[{projectIndex}]" : project.Name;

        // Check locomotives (required for any meaningful journey)
        if (project.Locomotives.Count == 0)
        {
            result.AddWarning($"[{projectName}] No locomotives defined. Journey execution will fail.");
        }
        else
        {
            result.AddInfo($"[{projectName}] ✓ Locomotives: {project.Locomotives.Count} defined");
        }

        // Check journeys (primary use case)
        if (project.Journeys.Count == 0)
        {
            result.AddWarning($"[{projectName}] No journeys defined. This project cannot execute any routes.");
        }
        else
        {
            result.AddInfo($"[{projectName}] ✓ Journeys: {project.Journeys.Count} defined");
            
            // Check journey stations
            var totalStations = project.Journeys.Sum(j => j.Stations.Count);
            if (totalStations == 0)
            {
                result.AddWarning($"[{projectName}] Journeys defined but no stations assigned.");
            }
            else
            {
                result.AddInfo($"[{projectName}] ✓ Stations: {totalStations} total across journeys");
            }
        }

        // Check speaker engines (for announcements)
        if (project.SpeakerEngines.Count == 0)
        {
            result.AddInfo($"[{projectName}] ℹ No speaker engines configured (announcements will be silent)");
        }
        else
        {
            result.AddInfo($"[{projectName}] ✓ Speaker Engines: {project.SpeakerEngines.Count} configured");
        }

        // Check voices
        if (project.Voices.Count == 0 && project.SpeakerEngines.Count > 0)
        {
            result.AddWarning($"[{projectName}] Speaker engines configured but no voices defined.");
        }
        else if (project.Voices.Count > 0)
        {
            result.AddInfo($"[{projectName}] ✓ Voices: {project.Voices.Count} available");
        }

        // Check trains (optional but recommended)
        if (project.Trains.Count > 0)
        {
            result.AddInfo($"[{projectName}] ✓ Trains: {project.Trains.Count} defined");
        }

        // Check workflows (optional)
        if (project.Workflows.Count > 0)
        {
            result.AddInfo($"[{projectName}] ✓ Workflows: {project.Workflows.Count} defined");
        }

        // Check passenger wagons (optional)
        if (project.PassengerWagons.Count > 0)
        {
            result.AddInfo($"[{projectName}] ✓ Passenger Wagons: {project.PassengerWagons.Count} defined");
        }

        // Check goods wagons (optional)
        if (project.GoodsWagons.Count > 0)
        {
            result.AddInfo($"[{projectName}] ✓ Goods Wagons: {project.GoodsWagons.Count} defined");
        }

        // Check signal box plan (optional)
        if (project.SignalBoxPlan != null)
        {
            result.AddInfo($"[{projectName}] ✓ Signal Box Plan defined");
        }
    }
}

/// <summary>
/// Result of project validation with info, warnings, and errors.
/// </summary>
public class ProjectValidationResult
{
    private readonly List<ValidationMessage> _messages = [];

    public IReadOnlyList<ValidationMessage> Messages => _messages.AsReadOnly();

    public bool IsValid => !HasErrors;
    public bool HasErrors => _messages.Any(m => m.Level == ValidationLevel.Error);
    public bool HasWarnings => _messages.Any(m => m.Level == ValidationLevel.Warning);

    public void AddInfo(string message)
    {
        _messages.Add(new ValidationMessage(ValidationLevel.Info, message));
    }

    public void AddWarning(string message)
    {
        _messages.Add(new ValidationMessage(ValidationLevel.Warning, message));
    }

    public void AddError(string message)
    {
        _messages.Add(new ValidationMessage(ValidationLevel.Error, message));
    }

    public string GetSummary()
    {
        var errors = _messages.Where(m => m.Level == ValidationLevel.Error).ToList();
        var warnings = _messages.Where(m => m.Level == ValidationLevel.Warning).ToList();
        var infos = _messages.Where(m => m.Level == ValidationLevel.Info).ToList();

        var summary = new System.Text.StringBuilder();
        if (errors.Count > 0)
            summary.AppendLine($"[ERRORS] {errors.Count}");
        if (warnings.Count > 0)
            summary.AppendLine($"[WARNINGS] {warnings.Count}");
        if (infos.Count > 0)
            summary.AppendLine($"[INFO] {infos.Count}");

        return summary.ToString().TrimEnd();
    }
}

public class ValidationMessage
{
    public ValidationMessage(ValidationLevel level, string text)
    {
        Level = level;
        Text = text;
        Timestamp = DateTime.UtcNow;
    }

    public ValidationLevel Level { get; }
    public string Text { get; }
    public DateTime Timestamp { get; }

    public override string ToString() => $"[{Level}] {Text}";
}

public enum ValidationLevel
{
    Info,
    Warning,
    Error
}
