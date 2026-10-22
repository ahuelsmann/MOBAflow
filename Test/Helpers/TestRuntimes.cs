// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;

using Moq;

/// <summary>Collaborators a test may replace when it creates a <see cref="MobaRuntimeService"/>.</summary>
internal sealed class TestRuntimeOptions
{
    public AppSettings? Settings { get; init; }

    public IWorkflowService? WorkflowService { get; init; }

    public IEventBus? EventBus { get; init; }

    public IInterlockingRuntime? InterlockingRuntime { get; init; }

    public IZ21EndpointSource? EndpointSource { get; init; }

    public ILogger<MobaRuntimeService>? Logger { get; init; }
}

/// <summary>
/// Creates runtimes and journey managers with explicit test collaborators: a local event bus, a mocked
/// interlocking runtime, production stop transitions and no runtime checkpoints. Counters the helper creates are
/// disposed with it, so a fixture owns one instance and disposes it after its tests.
/// </summary>
internal sealed partial class TestRuntimes : IDisposable
{
    private readonly List<IDisposable> _owned = [];

    /// <summary>A runtime over <paramref name="z21"/> whose InPort counters this helper owns.</summary>
    public MobaRuntimeService Create(IZ21 z21, TestRuntimeOptions? options = null)
    {
        var settings = options?.Settings ?? new AppSettings();
        return Create(z21, Own(new InPortCounterService(z21, settings)), options);
    }

    /// <summary>A runtime over <paramref name="z21"/> and the test's own <paramref name="counters"/>.</summary>
    public static MobaRuntimeService Create(IZ21 z21, InPortCounterService counters, TestRuntimeOptions? options = null)
    {
        options ??= new TestRuntimeOptions();
        var settings = options.Settings ?? new AppSettings();
        var eventBus = options.EventBus ?? new EventBus(NullLogger<EventBus>.Instance);
        return new MobaRuntimeService(
            new MobaRuntimeServiceDependencies
            {
                Z21 = z21,
                ExecutionContextFactory = new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = z21 }),
                Settings = settings,
                EventBus = eventBus,
                JourneyManagerFactory = JourneyManagers(options.WorkflowService ?? Mock.Of<IWorkflowService>(), eventBus),
                InterlockingRuntime = options.InterlockingRuntime ?? Mock.Of<IInterlockingRuntime>(),
                InPortCounterService = counters,
                EndpointSource = options.EndpointSource
            },
            options.Logger ?? NullLogger<MobaRuntimeService>.Instance);
    }

    /// <summary>A journey manager factory with production stop transitions and no runtime checkpoints.</summary>
    public static JourneyManagerFactory JourneyManagers(IWorkflowService workflowService, IEventBus? eventBus = null) =>
        new(workflowService, new JourneyStopTransitionService(), new NullJourneyRuntimeStateStore(),
            eventBus ?? new EventBus(NullLogger<EventBus>.Instance), TimeProvider.System, NullLoggerFactory.Instance);

    /// <summary>
    /// Journey manager collaborators around <paramref name="counters"/>; the manager disposes the coordinator,
    /// which defaults to a production coordinator over <paramref name="workflowService"/>.
    /// </summary>
    public static JourneyManagerDependencies JourneyDependencies(
        InPortCounterService counters,
        IWorkflowService? workflowService = null,
        IWorkflowExecutionCoordinator? coordinator = null,
        IJourneyRuntimeStateStore? runtimeStateStore = null,
        IEventBus? eventBus = null) => new()
        {
            StopTransitionService = new JourneyStopTransitionService(),
            RuntimeStateStore = runtimeStateStore ?? new NullJourneyRuntimeStateStore(),
            EventBus = eventBus ?? new EventBus(NullLogger<EventBus>.Instance),
            ExecutionCoordinator = coordinator
                ?? new WorkflowExecutionCoordinator(workflowService ?? Mock.Of<IWorkflowService>(), TimeProvider.System),
            InPortCounterService = counters
        };

    /// <summary>A journey manager for <paramref name="project"/> with a fresh execution context.</summary>
    public static JourneyManager JourneyManager(IZ21 z21, Moba.Domain.Project project, JourneyManagerDependencies dependencies) =>
        new(project, new ActionExecutionContext { Z21 = z21 }, dependencies, NullLogger<JourneyManager>.Instance);

    public void Dispose()
    {
        foreach (var owned in _owned)
            owned.Dispose();
        _owned.Clear();
    }

    private T Own<T>(T disposable) where T : IDisposable
    {
        _owned.Add(disposable);
        return disposable;
    }
}
