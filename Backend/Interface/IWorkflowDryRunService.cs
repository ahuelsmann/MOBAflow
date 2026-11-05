// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;

/// <summary>
/// Plans one workflow of a project without invoking live action handlers. The editor uses it for its test run;
/// the service owns the runtime execution context so the editor never holds it.
/// </summary>
public interface IWorkflowDryRunService
{
    /// <summary>Plans <paramref name="workflow"/> for <paramref name="project"/> and returns its planned effects.</summary>
    Task<WorkflowExecutionResult> DryRunAsync(Project project, Workflow workflow, CancellationToken cancellationToken = default);
}
