// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Services;

using Domain;

using System.Diagnostics;

/// <summary>
/// Solution Service.
/// Handles business logic for Solution management (merge, update, validation).
/// This implements Clean Architecture by separating domain models from business logic.
/// </summary>
public class SolutionService
{
    /// <summary>
    /// Updates a target Solution instance with data from a source Solution.
    /// This is used to update DI singleton without replacing the reference.
    /// Replaces the old Solution.UpdateFrom() method.
    /// </summary>
    /// <param name="target">The solution to update (typically DI singleton)</param>
    /// <param name="source">The source solution to copy data from</param>
    public void MergeSolution(Solution target, Solution source)
    {
        Debug.WriteLine($"🔄 SolutionService.MergeSolution START - Source has {source.Projects.Count} projects");

        // Clear existing data
        target.Projects.Clear();

        // Copy name (settings are global and stored in app configuration)
        target.Name = source.Name;

        // Deep copy projects (to avoid reference sharing)
        foreach (var sourceProject in source.Projects)
        {
            target.Projects.Add(sourceProject); // Shallow copy for now - models are POCOs
        }

        Debug.WriteLine($"✅ SolutionService.MergeSolution COMPLETE - Target now has {target.Projects.Count} projects");
    }

    /// <summary>
    /// Validates a Solution for completeness and consistency.
    /// </summary>
    /// <returns>Validation result with errors/warnings</returns>
    public ValidationResult ValidateSolution(Solution solution)
    {
        var result = new ValidationResult { IsValid = true };

        if (solution.Projects.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("Solution must contain at least one project");
        }

        foreach (var project in solution.Projects)
        {
            if (string.IsNullOrWhiteSpace(project.Name))
            {
                result.IsValid = false;
                result.Errors.Add($"Project at index {solution.Projects.IndexOf(project)} has no name");
            }

            // Validate workflows have unique IDs
            var workflowIds = project.Workflows.Select(w => w.Id).ToList();
            var duplicates = workflowIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
            
            if (duplicates.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Project '{project.Name}' has duplicate workflow IDs: {string.Join(", ", duplicates)}");
            }
        }

        return result;
    }
}
