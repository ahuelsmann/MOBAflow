// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

/// <summary>
/// Compatibility options retained for callers transitioning to <see cref="WorkflowExecutionRequest"/>.
/// </summary>
public readonly record struct WorkflowExecutionOptions
{
    /// <summary>
    /// Gets the retained caller option. Ordered workflows always stop at the first failed action.
    /// </summary>
    public bool StopOnFirstActionFailure { get; init; }
}
