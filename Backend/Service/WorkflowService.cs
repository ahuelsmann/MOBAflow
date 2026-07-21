// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Events;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>Validates, executes, dry-runs, and traces Workflow 2.0 graphs.</summary>
public partial class WorkflowService : IWorkflowService
{
    private readonly IActionExecutor _actionExecutor;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IWorkflowEffectPlanner _effectPlanner;
    private readonly IWorkflowConditionEvaluator _conditionEvaluator;
    private readonly IEventBus? _eventBus;
    private readonly IWorkflowTraceStore _traceStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WorkflowService>? _logger;

    /// <summary>Creates a workflow service with default Workflow 2.0 collaborators.</summary>
    public WorkflowService(IActionExecutor actionExecutor, ILogger<WorkflowService>? logger = null)
        : this(
            actionExecutor,
            new WorkflowValidator(),
            new WorkflowEffectPlanner(),
            new WorkflowConditionEvaluator(),
            null,
            new WorkflowTraceStore(),
            TimeProvider.System,
            logger)
    {
    }

    /// <summary>Creates a workflow service with an injectable time source.</summary>
    public WorkflowService(
        IActionExecutor actionExecutor,
        TimeProvider timeProvider,
        ILogger<WorkflowService>? logger = null)
        : this(
            actionExecutor,
            new WorkflowValidator(),
            new WorkflowEffectPlanner(),
            new WorkflowConditionEvaluator(),
            null,
            new WorkflowTraceStore(),
            timeProvider,
            logger)
    {
    }

    /// <summary>Creates the DI-composed Workflow 2.0 execution service.</summary>
    public WorkflowService(
        IActionExecutor actionExecutor,
        IWorkflowValidator workflowValidator,
        IWorkflowEffectPlanner effectPlanner,
        IWorkflowConditionEvaluator conditionEvaluator,
        IEventBus? eventBus,
        IWorkflowTraceStore traceStore,
        TimeProvider timeProvider,
        ILogger<WorkflowService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(actionExecutor);
        ArgumentNullException.ThrowIfNull(workflowValidator);
        ArgumentNullException.ThrowIfNull(effectPlanner);
        ArgumentNullException.ThrowIfNull(conditionEvaluator);
        ArgumentNullException.ThrowIfNull(traceStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _actionExecutor = actionExecutor;
        _workflowValidator = workflowValidator;
        _effectPlanner = effectPlanner;
        _conditionEvaluator = conditionEvaluator;
        _eventBus = eventBus;
        _traceStore = traceStore;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(
        Workflow workflow,
        ActionExecutionContext context,
        WorkflowExecutionOptions options = default) =>
        ExecuteAsync(workflow, context, options, CancellationToken.None);

    /// <inheritdoc />
    public async Task ExecuteAsync(
        Workflow workflow,
        ActionExecutionContext context,
        WorkflowExecutionOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);
        _ = options;

        var project = context.CurrentProject ?? new Project { Workflows = [workflow] };
        var result = await ExecuteAsync(
                new WorkflowExecutionRequest
                {
                    Project = project,
                    Workflow = workflow,
                    Context = context,
                    Mode = WorkflowRunMode.Live
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Status == WorkflowExecutionStatus.Cancelled)
            throw new OperationCanceledException("Workflow execution was cancelled.", cancellationToken);
        if (result.Status is WorkflowExecutionStatus.Failed or WorkflowExecutionStatus.NotStarted)
            throw new InvalidOperationException(result.FailureDetail ?? "Workflow validation or execution failed.");
    }
}
