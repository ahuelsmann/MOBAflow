// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

/// <summary>
/// Defines the terminal behavior after a workflow step fails.
/// </summary>
public enum WorkflowFailureBehavior
{
    /// <summary>Stop the workflow with a failed result.</summary>
    Stop,

    /// <summary>Continue with the step's normal successor.</summary>
    Continue,

    /// <summary>Continue at the configured failure branch.</summary>
    FailureBranch
}

/// <summary>
/// Defines a bounded retry modifier for a workflow error policy.
/// </summary>
public sealed class WorkflowRetryPolicy
{
    /// <summary>Gets or sets the number of attempts after the initial attempt.</summary>
    public int AdditionalAttempts { get; set; }

    /// <summary>Gets or sets the fixed delay before each additional attempt.</summary>
    public int DelayMs { get; set; }
}

/// <summary>
/// Defines retry and terminal behavior for a workflow or individual step.
/// </summary>
public sealed class WorkflowErrorPolicy
{
    /// <summary>Gets or sets the terminal behavior after retries are exhausted.</summary>
    public WorkflowFailureBehavior Behavior { get; set; } = WorkflowFailureBehavior.Stop;

    /// <summary>Gets or sets the failure-branch entry step when required by <see cref="Behavior"/>.</summary>
    public Guid? FailureStepId { get; set; }

    /// <summary>Gets or sets the optional bounded retry modifier.</summary>
    public WorkflowRetryPolicy? Retry { get; set; }
}
