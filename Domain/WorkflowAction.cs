// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

using System.Text.Json.Serialization;

/// <summary>
/// Workflow Action - Pure Data Object.
/// Execution logic moved to ActionExecutor service in Backend.
/// </summary>
[JsonConverter(typeof(WorkflowActionJsonConverter))]
public class WorkflowAction
{
    /// <summary>
    /// Gets or sets the unique identifier of the workflow action.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name of the workflow action.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Execution order number (1-based).
    /// Automatically updated when actions are reordered via drag drop.
    /// Used for sorting actions before execution.
    /// </summary>
    public uint Number { get; set; }

    /// <summary>
    /// Gets or sets the action type that defines the concrete behavior.
    /// </summary>
    public ActionType Type { get; set; }

    /// <summary>
    /// Delay in milliseconds for timing control.
    ///
    /// Sequential Mode: Pause AFTER this action completes (before next action starts).
    /// - Use for: Adding silence between actions (e.g., wait 1s after Gong before Announcement)
    ///
    /// Parallel Mode: Start offset FROM previous action (cumulative).
    /// - Use for: Staggered overlapping effects (e.g., Gong at t=0, Announcement at t+500ms)
    ///
    /// Default: 0 (no delay)
    /// </summary>
    public int DelayAfterMs { get; set; }

    /// <summary>
    /// Payload for <see cref="ActionType.Command"/> actions.
    /// </summary>
    public CommandActionPayload? Command { get; set; }

    /// <summary>
    /// Payload for <see cref="ActionType.Audio"/> actions.
    /// </summary>
    public AudioActionPayload? Audio { get; set; }

    /// <summary>
    /// Payload for <see cref="ActionType.Announcement"/> actions.
    /// </summary>
    public AnnouncementActionPayload? Announcement { get; set; }

    /// <summary>
    /// Payload for <see cref="ActionType.ExecuteScript"/> actions.
    /// </summary>
    public PowerShellActionPayload? PowerShell { get; set; }

    /// <summary>
    /// Payload for <see cref="ActionType.SelectSignalAspect"/> actions.
    /// </summary>
    public SelectSignalAspectActionPayload? SelectSignalAspect { get; set; }

    /// <summary>
    /// Payload for <see cref="ActionType.TrainDestinationDisplay"/> actions.
    /// </summary>
    public TrainDestinationDisplayActionPayload? TrainDestinationDisplay { get; set; }
}