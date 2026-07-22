// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;
using Domain.Enum;

/// <summary>
/// Identifies the external-effect family of a workflow action.
/// </summary>
public enum WorkflowEffectCategory
{
    /// <summary>Raw command-station communication.</summary>
    CommandStation,

    /// <summary>Audio playback through the configured output.</summary>
    AudioOutput,

    /// <summary>Text-to-speech output.</summary>
    SpeechOutput,

    /// <summary>An external script process.</summary>
    ScriptProcess,

    /// <summary>A signal or accessory state change.</summary>
    Signal,

    /// <summary>A train destination display update.</summary>
    Display,

    /// <summary>A mutable journey-state transition.</summary>
    JourneyState
}

/// <summary>
/// Identifies how an effect accesses a controlled resource.
/// </summary>
public enum WorkflowResourceAccess
{
    /// <summary>The effect observes but does not change the resource.</summary>
    Read,

    /// <summary>The effect changes a resource that cannot be written concurrently.</summary>
    ExclusiveWrite
}

/// <summary>
/// Describes a controlled resource touched by a planned workflow effect.
/// </summary>
/// <param name="Key">Stable resource key used for conflict detection.</param>
/// <param name="Category">Effect family owning the resource.</param>
/// <param name="Access">Requested access mode.</param>
public sealed record WorkflowResourceDescriptor(
    string Key,
    WorkflowEffectCategory Category,
    WorkflowResourceAccess Access);

/// <summary>
/// Describes an external effect without invoking its live action handler.
/// </summary>
/// <param name="ActionType">Planned action type.</param>
/// <param name="Category">External-effect family.</param>
/// <param name="Description">Sanitized human-readable intent.</param>
/// <param name="Resources">Controlled resources touched by the effect.</param>
public sealed record WorkflowPlannedEffect(
    ActionType ActionType,
    WorkflowEffectCategory Category,
    string Description,
    IReadOnlyList<WorkflowResourceDescriptor> Resources);

/// <summary>
/// Describes one invalid action field discovered during effect planning.
/// </summary>
/// <param name="FieldPath">Path relative to the action payload.</param>
/// <param name="Message">English validation message.</param>
public sealed record WorkflowActionPlanningIssue(string FieldPath, string Message);

/// <summary>
/// Contains payload validation and the effect projected for a workflow action.
/// </summary>
/// <param name="Issues">Payload issues in deterministic order.</param>
/// <param name="Effect">Planned effect when the payload is valid.</param>
public sealed record WorkflowActionPlan(
    IReadOnlyList<WorkflowActionPlanningIssue> Issues,
    WorkflowPlannedEffect? Effect)
{
    /// <summary>Gets whether the action can be planned safely.</summary>
    public bool IsValid => Issues.Count == 0 && Effect != null;
}

/// <summary>
/// Validates action payloads and projects effects without reaching live handlers.
/// </summary>
public interface IWorkflowEffectPlanner
{
    /// <summary>Builds a side-effect-free plan for one typed workflow action.</summary>
    /// <param name="action">Action to validate and describe.</param>
    WorkflowActionPlan Plan(WorkflowAction action);
}
