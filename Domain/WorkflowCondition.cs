// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using System.Text.Json.Serialization;

/// <summary>
/// Base type for deterministic workflow conditions.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(FeedbackSourceWorkflowCondition), "feedbackSource")]
[JsonDerivedType(typeof(CurrentJourneyWorkflowCondition), "currentJourney")]
[JsonDerivedType(typeof(CurrentStationWorkflowCondition), "currentStation")]
public abstract class WorkflowCondition;

/// <summary>
/// Matches the hardware input that caused the workflow execution.
/// </summary>
public sealed class FeedbackSourceWorkflowCondition : WorkflowCondition
{
    /// <summary>Gets or sets the expected one-based feedback input port.</summary>
    public uint InPort { get; set; }
}

/// <summary>
/// Matches the journey captured in the workflow execution context.
/// </summary>
public sealed class CurrentJourneyWorkflowCondition : WorkflowCondition
{
    /// <summary>Gets or sets the expected journey identifier.</summary>
    public Guid JourneyId { get; set; }
}

/// <summary>
/// Matches the station captured in the workflow execution context.
/// </summary>
public sealed class CurrentStationWorkflowCondition : WorkflowCondition
{
    /// <summary>Gets or sets the expected station identifier.</summary>
    public Guid StationId { get; set; }
}
