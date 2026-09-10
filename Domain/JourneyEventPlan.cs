// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>A journey's independently evaluated feedback triggers and workflow assignments.</summary>
public sealed class JourneyEventPlan
{
    /// <summary>Editable display order; entries do not depend on preceding entries.</summary>
    public List<JourneyEvent> Events { get; set; } = [];
}

/// <summary>Starts a workflow when an InPort reaches a count relative to the journey start.</summary>
public sealed class JourneyEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public uint InPort { get; set; } = 1;

    /// <summary>One-based activation count since this journey run started.</summary>
    public ulong Count { get; set; } = 1;

    public Guid? WorkflowId { get; set; }

    public bool Enabled { get; set; } = true;
}
