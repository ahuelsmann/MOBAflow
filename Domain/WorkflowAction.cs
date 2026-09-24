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
    /// Display ordinal; the workflow action list determines execution order.
    /// </summary>
    public uint Number { get; set; }

    /// <summary>
    /// Gets or sets the action type that defines the concrete behavior.
    /// </summary>
    public ActionType Type { get; set; }

    /// <summary>Gets whether the payload required by the declared action type is present.</summary>
    [JsonIgnore]
    public bool HasPayload => WorkflowActionPayloadDescriptors.Find(Type)?.HasPayload(this) ?? false;

    /// <summary>
    /// Delay in milliseconds for timing control.
    ///
    /// Pause AFTER this action completes (before the next action starts).
    /// - Use for: Adding silence between actions (e.g., wait 1s after Gong before Announcement)
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

    /// <summary>Payload for <see cref="ActionType.ChangeJourneyStop"/> actions.</summary>
    public ChangeJourneyStopActionPayload? ChangeJourneyStop { get; set; }
}
