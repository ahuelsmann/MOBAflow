// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

using Interface;

/// <summary>
/// Plans a workflow through <see cref="IWorkflowService"/> in dry-run mode with a context created from the
/// shared execution services and the project under edit.
/// </summary>
public sealed class WorkflowDryRunService : IWorkflowDryRunService
{
    private readonly IWorkflowService _workflowService;
    private readonly ActionExecutionContextFactory _contextFactory;

    /// <summary>Initializes a new instance of the <see cref="WorkflowDryRunService"/> class.</summary>
    /// <param name="workflowService">Executes the workflow in dry-run mode.</param>
    /// <param name="contextFactory">Creates the execution context over the shared runtime services.</param>
    public WorkflowDryRunService(IWorkflowService workflowService, ActionExecutionContextFactory contextFactory)
    {
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(contextFactory);
        _workflowService = workflowService;
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public Task<WorkflowExecutionResult> DryRunAsync(Project project, Workflow workflow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(workflow);
        var context = _contextFactory.Create(new ActionExecutionContextState { CurrentProject = project });
        return _workflowService.ExecuteAsync(new WorkflowExecutionRequest
        {
            Project = project,
            Workflow = workflow,
            Context = context,
            Mode = WorkflowRunMode.DryRun,
            SourceCorrelationId = Guid.NewGuid()
        }, cancellationToken);
    }
}
