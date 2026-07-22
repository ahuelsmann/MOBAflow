// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

/// <summary>
/// Compatibility options retained for callers transitioning to <see cref="WorkflowExecutionRequest"/>.
/// </summary>
public readonly record struct WorkflowExecutionOptions
{
    /// <summary>
    /// Gets the superseded Workflow 1.x stop flag. Workflow 2.0 error policies are authoritative.
    /// </summary>
    public bool StopOnFirstActionFailure { get; init; }
}
