// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>A reusable, ordered sequence of actions, independent of its triggering events.</summary>
public class Workflow
{
    /// <summary>Gets or sets the stable identifier shared by all event assignments.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = "New Flow";

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets actions in execution order. List position is authoritative.</summary>
    public List<WorkflowAction> Actions { get; set; } = [];
}
