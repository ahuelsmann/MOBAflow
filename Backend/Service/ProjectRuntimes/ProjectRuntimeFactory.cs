// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.ProjectRuntimes;

using Common.Configuration;

using Domain;

using Interface;

using Interlocking;

using Manager;

using Microsoft.Extensions.Logging;

/// <summary>
/// Builds the runtime of one project: runtime service, journey and workflow execution, feedback counters,
/// interlocking and whistle automation on the connection of the project's Z21. Stateless workflow handlers,
/// stores and audio are shared by all project runtimes.
/// </summary>
public sealed class ProjectRuntimeFactory(Z21ConnectionRegistry connections, ProjectRuntimeServices services)
{
    private readonly Z21ConnectionRegistry _connections = connections ?? throw new ArgumentNullException(nameof(connections));
    private readonly ProjectRuntimeServices _services = services ?? throw new ArgumentNullException(nameof(services));

    /// <summary>
    /// Creates the runtime of a project. The runtime is activated with the project but not started.
    /// </summary>
    public ProjectRuntime Create(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var shared = _services;
        var lease = _connections.Acquire(project);
        var bus = lease.EventBus;
        var z21 = lease.Z21;
        var loggers = shared.LoggerFactory;

        var counters = new InPortCounterService(
            z21,
            shared.Settings,
            shared.TimeProvider,
            loggers.CreateLogger<InPortCounterService>(),
            shared.CounterStoreFactory?.Invoke(project.Id));
        var interlocking = new InterlockingRuntimeService(
            z21,
            bus,
            shared.TimeProvider,
            loggers.CreateLogger<InterlockingRuntimeService>());
        var workflowService = new WorkflowService(
            shared.ActionExecutor,
            new WorkflowServiceDependencies
            {
                Validator = shared.WorkflowDependencies.Validator,
                EffectPlanner = shared.WorkflowDependencies.EffectPlanner,
                EventBus = bus,
                TraceStore = shared.WorkflowDependencies.TraceStore,
                TimeProvider = shared.WorkflowDependencies.TimeProvider,
                Logger = shared.WorkflowDependencies.Logger
            });
        var executionContext = new ActionExecutionContext
        {
            Z21 = z21,
            SpeakerEngine = shared.SharedExecutionContext.SpeakerEngine,
            SoundPlayer = shared.SharedExecutionContext.SoundPlayer
        };
        var unavailableReason = lease.ConflictProjectName is { } owner
            ? $"Z21 {Z21ConnectionRegistry.ToKey(project.Z21)} is already used by project '{owner}'"
            : null;
        var runtime = new MobaRuntimeService(
            z21,
            workflowService,
            new ActionExecutionContextFactory(executionContext),
            shared.Settings,
            loggers.CreateLogger<MobaRuntimeService>(),
            bus,
            journeyManagerFactory: new JourneyManagerFactory(
                z21,
                workflowService,
                new JourneyManagerDependencies
                {
                    StopTransitionService = shared.StopTransitionService,
                    RuntimeStateStore = shared.RuntimeStateStore,
                    TimeProvider = shared.TimeProvider,
                    EventBus = bus,
                    InPortCounterService = counters
                },
                loggers.CreateLogger<JourneyManager>()),
            timeProvider: shared.TimeProvider,
            interlockingRuntime: interlocking,
            inPortCounterService: counters,
            endpointSource: new ProjectZ21EndpointSource(project, unavailableReason));
        var whistle = new LocomotiveWhistleAutomationService(
            bus,
            new MobaRuntimeLocomotiveFunctionCommandGateway(runtime),
            loggers.CreateLogger<LocomotiveWhistleAutomationService>(),
            shared.TimeProvider);

        return new ProjectRuntime(project, runtime, lease, counters, interlocking, whistle);
    }

    /// <summary>
    /// Disconnects the Z21 connections that no project runtime uses anymore.
    /// </summary>
    public Task DisconnectUnusedAsync() => _connections.DisconnectUnusedAsync();
}

/// <summary>
/// Services shared by all project runtimes of the application.
/// </summary>
public sealed class ProjectRuntimeServices
{
    /// <summary>Gets the application settings.</summary>
    public required AppSettings Settings { get; init; }

    /// <summary>Gets the action executor with the stateless workflow action handlers.</summary>
    public required IActionExecutor ActionExecutor { get; init; }

    /// <summary>Gets the workflow dependencies; each runtime replaces the event bus with its own.</summary>
    public required WorkflowServiceDependencies WorkflowDependencies { get; init; }

    /// <summary>Gets the audio services shared by the workflow contexts.</summary>
    public required ActionExecutionContext SharedExecutionContext { get; init; }

    /// <summary>Gets the journey stop transition rules.</summary>
    public required IJourneyStopTransitionService StopTransitionService { get; init; }

    /// <summary>Gets the journey checkpoint store, keyed by project and journey.</summary>
    public required IJourneyRuntimeStateStore RuntimeStateStore { get; init; }

    /// <summary>Gets the clock.</summary>
    public required TimeProvider TimeProvider { get; init; }

    /// <summary>Gets the logger factory.</summary>
    public required ILoggerFactory LoggerFactory { get; init; }

    /// <summary>Gets the factory of the feedback counter store of a project; null keeps counters in memory.</summary>
    public Func<Guid, IInPortCounterStore>? CounterStoreFactory { get; init; }
}

/// <summary>
/// The runtime of one project and the services it owns.
/// </summary>
public sealed class ProjectRuntime : IAsyncDisposable
{
    private readonly Project _project;
    private readonly MobaRuntimeService _service;
    private readonly InPortCounterService _counters;
    private readonly LocomotiveWhistleAutomationService _whistle;
    private int _disposed;

    internal ProjectRuntime(
        Project project,
        MobaRuntimeService runtime,
        Z21ConnectionLease connection,
        InPortCounterService counters,
        InterlockingRuntimeService interlocking,
        LocomotiveWhistleAutomationService whistle)
    {
        _project = project;
        _service = runtime;
        Z21Key = Z21ConnectionRegistry.ToKey(project.Z21);
        Runtime = runtime;
        Connection = connection;
        _counters = counters;
        Interlocking = interlocking;
        _whistle = whistle;
    }

    /// <summary>Gets the project this runtime executes.</summary>
    public Guid ProjectId => _project.Id;

    /// <summary>Gets the Z21 the project was assigned when the runtime was created; null when none.</summary>
    public string? Z21Key { get; }

    internal Project Project => _project;

    /// <summary>Gets the runtime service.</summary>
    public IMobaRuntime Runtime { get; }

    /// <summary>Gets the interlocking of the project.</summary>
    public IInterlockingRuntime Interlocking { get; }

    /// <summary>Gets the Z21 connection the runtime borrows.</summary>
    public Z21ConnectionLease Connection { get; }

    /// <summary>
    /// Activates the project and starts the connection to its Z21 (or takes over an open one).
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await Runtime.ActivateProjectAsync(_project, cancellationToken).ConfigureAwait(false);
        _whistle.Activate(_project);
        await Runtime.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets a value indicating whether the runtime is connected to its Z21.</summary>
    public bool IsConnected => Runtime.Current.IsConnected;

    /// <summary>Sets every known locomotive on the project's Z21 to speed 0.</summary>
    public Task StopLocomotivesAsync(CancellationToken cancellationToken = default) =>
        _service.StopAllLocomotivesAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _whistle.Dispose();
        _service.Dispose();
        await Interlocking.DisposeAsync().ConfigureAwait(false);
        // Flushes a pending counter save before the runtime goes away.
        await _counters.DisposeAsync().ConfigureAwait(false);
        Connection.Release();
    }
}
