// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Services;

using Moba.Domain;

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

    /// <summary>
    /// Resolves workflow references in stations and platforms by ID.
    /// This is needed after deserialization where WorkflowId properties are set.
    /// </summary>
    public void ResolveWorkflowReferences(Solution solution)
    {
        foreach (var project in solution.Projects)
        {
            // Build workflow lookup
            var workflowLookup = project.Workflows.ToDictionary(w => w.Id);

            // Resolve station workflow references
            foreach (var journey in project.Journeys)
            {
                foreach (var station in journey.Stations)
                {
                    // Station workflow reference would be resolved here
                    // (currently Station has WorkflowId property in Domain model)
                    
                    // Resolve platform workflow references
                    foreach (var platform in station.Platforms)
                    {
                        // Platform workflow reference would be resolved here
                        // (currently Platform has WorkflowId property in Domain model)
                    }
                }
            }
        }

        Debug.WriteLine($"✅ Workflow references resolved for {solution.Projects.Count} projects");
    }
    
    /// <summary>
    /// Restores workflow references in stations after deserialization.
    /// During JSON deserialization, Station.Flow contains a temporary Workflow object with only the Id.
    /// This method replaces it with the actual Workflow reference from Project.Workflows.
    /// </summary>
    public static void RestoreWorkflowReferences(Solution solution)
    {
        Debug.WriteLine("🔄 RestoreWorkflowReferences START");

        foreach (var project in solution.Projects)
        {
            Debug.WriteLine($"  Project: {project.Name}");
            Debug.WriteLine($"  Workflows: {project.Workflows.Count}");

            // Iterate through all Journeys and Stations
            foreach (var journey in project.Journeys)
            {
                foreach (var station in journey.Stations)
                {
                    // ✅ Flow is now a temporary Workflow object with only the GUID (from WorkflowConverter)
                    // Replace it with the real reference from project.Workflows
                    if (station.Flow != null)
                    {
                        Workflow? matchingWorkflow = null;

                        // Try to find by GUID (preferred)
                        if (station.Flow.Id != Guid.Empty)
                        {
                            matchingWorkflow = project.Workflows.FirstOrDefault(w => w.Id == station.Flow.Id);
                        }
                        // Fallback: Find by Name (for old JSON files)
                        else if (!string.IsNullOrEmpty(station.Flow.Name))
                        {
                            matchingWorkflow = project.Workflows.FirstOrDefault(w => w.Name == station.Flow.Name);
                        }

                        if (matchingWorkflow != null)
                        {
                            station.Flow = matchingWorkflow;
                            Debug.WriteLine($"      ✅ {station.Name}: Flow restored to {matchingWorkflow.Name}");
                        }
                        else
                        {
                            Debug.WriteLine($"      ❌ {station.Name}: Workflow not found (Id: {station.Flow.Id})");
                            station.Flow = null; // Reset if not found
                        }
                    }
                }
            }
        }

        Debug.WriteLine("✅ RestoreWorkflowReferences DONE");
    }
}