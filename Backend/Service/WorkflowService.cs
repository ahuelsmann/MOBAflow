// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Events;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>Groups the collaborators used by Workflow 2.0 graph execution.</summary>
public sealed class WorkflowServiceDependencies
{
    /// <summary>Gets the graph validator.</summary>
    public required IWorkflowValidator Validator { get; init; }

    /// <summary>Gets the dry-run effect planner.</summary>
    public required IWorkflowEffectPlanner EffectPlanner { get; init; }

    /// <summary>Gets the typed condition evaluator.</summary>
    public required IWorkflowConditionEvaluator ConditionEvaluator { get; init; }

    /// <summary>Gets the optional lifecycle event bus.</summary>
    public IEventBus? EventBus { get; init; }

    /// <summary>Gets the retained workflow trace store.</summary>
    public required IWorkflowTraceStore TraceStore { get; init; }

    /// <summary>Gets the execution time source.</summary>
    public required TimeProvider TimeProvider { get; init; }

    /// <summary>Gets the optional workflow logger.</summary>
    public ILogger<WorkflowService>? Logger { get; init; }
}

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
            CreateDefaultDependencies(TimeProvider.System, logger))
    {
    }

    /// <summary>Creates a workflow service with an injectable time source.</summary>
    public WorkflowService(
        IActionExecutor actionExecutor,
        TimeProvider timeProvider,
        ILogger<WorkflowService>? logger = null)
        : this(
            actionExecutor,
            CreateDefaultDependencies(timeProvider, logger))
    {
    }

    /// <summary>Creates the DI-composed Workflow 2.0 execution service.</summary>
    public WorkflowService(
        IActionExecutor actionExecutor,
        WorkflowServiceDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(actionExecutor);
        ArgumentNullException.ThrowIfNull(dependencies);
        ArgumentNullException.ThrowIfNull(dependencies.Validator);
        ArgumentNullException.ThrowIfNull(dependencies.EffectPlanner);
        ArgumentNullException.ThrowIfNull(dependencies.ConditionEvaluator);
        ArgumentNullException.ThrowIfNull(dependencies.TraceStore);
        ArgumentNullException.ThrowIfNull(dependencies.TimeProvider);
        _actionExecutor = actionExecutor;
        _workflowValidator = dependencies.Validator;
        _effectPlanner = dependencies.EffectPlanner;
        _conditionEvaluator = dependencies.ConditionEvaluator;
        _eventBus = dependencies.EventBus;
        _traceStore = dependencies.TraceStore;
        _timeProvider = dependencies.TimeProvider;
        _logger = dependencies.Logger;
    }

    private static WorkflowServiceDependencies CreateDefaultDependencies(
        TimeProvider timeProvider,
        ILogger<WorkflowService>? logger) => new()
        {
            Validator = new WorkflowValidator(),
            EffectPlanner = new WorkflowEffectPlanner(),
            ConditionEvaluator = new WorkflowConditionEvaluator(),
            TraceStore = new WorkflowTraceStore(),
            TimeProvider = timeProvider,
            Logger = logger
        };

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
