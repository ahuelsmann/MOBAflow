// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Model;

using Converter;

using Newtonsoft.Json;

/// <summary>
/// Represents a platform (Bahnsteig) with its own workflow for platform-specific announcements.
/// Unlike station workflows (which are triggered by journey progression), platform workflows 
/// are triggered directly by feedback events independent of any specific train journey.
/// Example: "Attention on platform 3. The InterCity from Koblenz to Hamburg is now arriving."
/// </summary>
public class Platform
{
    public Platform()
    {
        Name = "New Platform";
    }

    /// <summary>
    /// Name or number of the platform (e.g., "Platform 1", "Gleis 3a").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Track number assigned to this platform.
    /// </summary>
    public uint Track { get; set; }

    /// <summary>
    /// R-BUS port assignment for this platform's feedback point.
    /// When this feedback is triggered, the platform workflow will be executed.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Workflow containing actions (announcements, commands, sounds) to be executed 
    /// when a train triggers the platform's feedback point.
    /// </summary>
    [JsonConverter(typeof(WorkflowConverter))]
    public Workflow? Flow { get; set; }

    /// <summary>
    /// Ignore repeated feedbacks to prevent multiple executions.
    /// </summary>
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }

    /// <summary>
    /// Ignore repeated feedbacks for x seconds.
    /// This prevents the same announcement from being triggered multiple times 
    /// when a train slowly passes over the feedback point.
    /// </summary>
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }
}