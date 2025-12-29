// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Workflow Action - Pure Data Object.
/// Execution logic moved to ActionExecutor service in Backend.
/// </summary>
public class WorkflowAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Execution order number (1-based).
    /// Automatically updated when actions are reordered via drag & drop.
    /// Used for sorting actions before execution.
    /// </summary>
    public uint Number { get; set; }
    
    public ActionType Type { get; set; }

    /// <summary>
    /// Delay in milliseconds to wait AFTER this action completes.
    /// Only applies when Workflow.ExecutionMode is Sequential.
    /// Use for precise timing control (e.g., wait for audio to finish before next action).
    /// Default: 0 (no delay)
    /// </summary>
    public int DelayAfterMs { get; set; } = 0;

    /// <summary>
    /// Action-specific parameters (polymorphic via Type property).
    /// For Command: {"Address": 123, "Speed": 80, "Direction": "Forward"}
    /// For Audio: {"FilePath": "sound.wav"}
    /// For Announcement: {"Message": "Train departing", "VoiceName": "de-DE-KatjaNeural"}
    /// </summary>
    /// <summary>
    /// Action-specific parameters stored as plain objects after deserialization.
    /// Converters handle preservation of JSON structure (JToken) and map to object here.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}