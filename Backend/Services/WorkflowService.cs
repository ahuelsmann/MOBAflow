// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Services;

using Moba.Backend.Interface;
using Moba.Domain;

using System.Diagnostics;

/// <summary>
/// Workflow Execution Service.
/// Handles business logic that was previously in Workflow.StartAsync().
/// This implements Clean Architecture by separating domain models from business logic.
/// </summary>
public class WorkflowService
{
    private readonly ActionExecutor _actionExecutor;
    private readonly IZ21 _z21;

    public WorkflowService(ActionExecutor actionExecutor, IZ21 z21)
    {
        _actionExecutor = actionExecutor;
        _z21 = z21;
    }

    /// <summary>
    /// Executes all actions in a workflow.
    /// This replaces the old Workflow.StartAsync() method.
    /// </summary>
    /// <param name="workflow">The workflow to execute (Domain POCO)</param>
    /// <param name="context">Execution context with dependencies</param>
    public async Task ExecuteAsync(Workflow workflow, ActionExecutionContext? context = null)
    {
        if (workflow.Actions.Count == 0)
        {
            throw new InvalidOperationException(
                $"Every workflow must have at least one action. Workflow: '{workflow.Name}' (ID: {workflow.Id})");
        }

        Debug.WriteLine($"▶ Starting workflow '{workflow.Name}' (ID: {workflow.Id}) with {workflow.Actions.Count} action(s)");

        // Create default context if none provided
        context ??= new ActionExecutionContext
        {
            Z21 = _z21,
            SpeakerEngine = null, // Set by caller if needed
            SoundPlayer = null    // Set by caller if needed
        };

        // Execute actions sequentially
        foreach (var action in workflow.Actions.OrderBy(a => a.Number))
        {
            try
            {
                await _actionExecutor.ExecuteAsync(action, context);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error executing action '{action.Name}' in workflow '{workflow.Name}': {ex.Message}");
                throw;
            }
        }

        Debug.WriteLine($"✅ Workflow '{workflow.Name}' completed successfully");
    }
}

/// <summary>
/// Execution context for actions.
/// Contains dependencies and state needed for action execution (Z21, SpeakerEngine, SoundPlayer, Journey context).
/// </summary>
public class ActionExecutionContext
{
    public required IZ21 Z21 { get; set; }
    public Sound.ISpeakerEngine? SpeakerEngine { get; set; }
    public Sound.ISoundPlayer? SoundPlayer { get; set; }
    
    /// <summary>
    /// Journey template text for announcements (e.g., "Train to {station}")
    /// </summary>
    public string? JourneyTemplateText { get; set; }
    
    /// <summary>
    /// Current station for journey-related workflows
    /// </summary>
    public Station? CurrentStation { get; set; }
}