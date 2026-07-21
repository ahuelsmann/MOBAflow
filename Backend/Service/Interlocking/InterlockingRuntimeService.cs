// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using System.Threading.Channels;

using Common.Configuration;
using Common.Events;

using Domain;

using Events;
using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Projects ordered Z21 observations into the same safety engine used by route commands.
/// </summary>
public sealed class InterlockingRuntimeService : IInterlockingRuntime, IInterlockingLifecycleEventSink
{
    private static readonly TimeSpan TurnoutConfirmationTimeout = TimeSpan.FromSeconds(5);

    private readonly object _stateSync = new();
    private readonly SemaphoreSlim _activationGate = new(1, 1);
    private readonly IZ21 _z21;
    private readonly IEventBus _eventBus;
    private readonly AppSettings _appSettings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InterlockingRuntimeService> _logger;
    private readonly Channel<RuntimeWorkItem> _workItems;
    private readonly CancellationTokenSource _disposeCancellation = new();
    private readonly List<Guid> _subscriptionIds = [];
    private readonly Task _consumerTask;
    private readonly Dictionary<int, bool> _feedbackStates = [];

    private InterlockingDefinition? _definition;
    private InterlockingRouteCoordinator? _coordinator;
    private InterlockingRuntimeState _current = InterlockingRuntimeState.Empty;
    private bool _isSynchronized;
    private int _disposeStarted;

    public InterlockingRuntimeService(
        IZ21 z21,
        IEventBus eventBus,
        AppSettings appSettings,
        TimeProvider timeProvider,
        ILogger<InterlockingRuntimeService> logger)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(appSettings);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _z21 = z21;
        _eventBus = eventBus;
        _appSettings = appSettings;
        _timeProvider = timeProvider;
        _logger = logger;
        _workItems = Channel.CreateUnbounded<RuntimeWorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _consumerTask = ConsumeAsync();

        _subscriptionIds.Add(_eventBus.Subscribe<FeedbackStateChangedEvent>(OnFeedbackStateChanged));
        _subscriptionIds.Add(_eventBus.Subscribe<TurnoutInfoChangedEvent>(OnTurnoutInfoChanged));
        _subscriptionIds.Add(_eventBus.Subscribe<Z21ConnectionEstablishedEvent>(_ => Enqueue(SynchronizeCoreAsync)));
        _subscriptionIds.Add(_eventBus.Subscribe<Z21ConnectionLostEvent>(_ => Enqueue(MarkDisconnectedCoreAsync)));
    }

    public InterlockingRuntimeState Current
    {
        get
        {
            lock (_stateSync)
                return _current;
        }
    }

    public bool IsSynchronized
    {
        get
        {
            lock (_stateSync)
                return _isSynchronized;
        }
    }

    public async Task ActivateAsync(
        InterlockingDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        await WhenIdleAsync(cancellationToken).ConfigureAwait(false);
        await _activationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_coordinator != null)
                await _coordinator.ShutdownAsync(Guid.NewGuid(), cancellationToken).ConfigureAwait(false);

            var turnoutGateway = new Z21TurnoutEffectGateway(_z21);
            var commandService = new SemanticTurnoutCommandService(definition, turnoutGateway);
            var turnoutRuntime = new SemanticTurnoutRuntimeCoordinator(
                definition,
                commandService,
                _timeProvider,
                TurnoutConfirmationTimeout);
            _definition = definition;
            _feedbackStates.Clear();
            _coordinator = new InterlockingRouteCoordinator(
                definition,
                turnoutRuntime,
                new Z21SignalEffectGateway(definition, _z21, _appSettings),
                this);
            SetState(_coordinator.Snapshot, false);
            PublishSnapshot(Guid.NewGuid(), "interlocking.activated");
        }
        finally
        {
            _activationGate.Release();
        }
    }

    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _workItems.Writer.WriteAsync(
            new RuntimeWorkItem(SynchronizeCoreAsync, completion),
            cancellationToken).ConfigureAwait(false);
        await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<RouteCoordinatorResult> PreviewRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteRouteCommandAsync(
            (coordinator, token) => coordinator.PreviewRouteAsync(routeId, correlationId, token),
            correlationId,
            cancellationToken);

    public Task<RouteCoordinatorResult> SelectRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteRouteCommandAsync(
            (coordinator, token) => coordinator.SelectRouteAsync(routeId, correlationId, token),
            correlationId,
            cancellationToken);

    public Task<RouteCoordinatorResult> SetRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteRouteCommandAsync(
            (coordinator, token) => coordinator.SetRouteAsync(routeId, correlationId, token),
            correlationId,
            cancellationToken);

    public Task<RouteCoordinatorResult> CancelRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteRouteCommandAsync(
            (coordinator, token) => coordinator.CancelRouteAsync(routeId, correlationId, token),
            correlationId,
            cancellationToken,
            requireSynchronization: false);

    public Task<RouteCoordinatorResult> SafeStopRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteRouteCommandAsync(
            (coordinator, token) => coordinator.SafeStopRouteAsync(routeId, correlationId, token),
            correlationId,
            cancellationToken,
            requireSynchronization: false);

    public Task<RouteCoordinatorResult> ReconcileRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteRouteCommandAsync(
            (coordinator, token) => coordinator.ReconcileRouteAsync(routeId, correlationId, token),
            correlationId,
            cancellationToken);

    public Task<RouteCoordinatorResult> ReleaseRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        ExecuteRouteCommandAsync(
            (coordinator, token) => coordinator.ReleaseRouteAsync(routeId, correlationId, token),
            correlationId,
            cancellationToken);

    public async Task WhenIdleAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _workItems.Writer.WriteAsync(
            new RuntimeWorkItem(_ => Task.CompletedTask, completion),
            cancellationToken).ConfigureAwait(false);
        await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;

        foreach (var subscriptionId in _subscriptionIds)
            _eventBus.Unsubscribe(subscriptionId);
        _subscriptionIds.Clear();
        _workItems.Writer.TryComplete();
        await _consumerTask.ConfigureAwait(false);

        if (_coordinator != null)
            await _coordinator.ShutdownAsync(Guid.NewGuid()).ConfigureAwait(false);
        _disposeCancellation.Cancel();
        _disposeCancellation.Dispose();
        _activationGate.Dispose();
    }

    void IInterlockingLifecycleEventSink.Publish(InterlockingLifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        lock (_stateSync)
        {
            if (_current.Revision == lifecycleEvent.State.Revision)
                return;
            _current = lifecycleEvent.State;
        }

        _eventBus.Publish(new InterlockingRuntimeSnapshotChangedEvent(
            lifecycleEvent.State,
            IsSynchronized,
            lifecycleEvent.CorrelationId,
            lifecycleEvent.Code));
    }

    private void OnFeedbackStateChanged(FeedbackStateChangedEvent observation) =>
        Enqueue(token => ObserveFeedbackAsync(observation, token));

    private void OnTurnoutInfoChanged(TurnoutInfoChangedEvent observation) =>
        Enqueue(token => ObserveTurnoutAsync(observation, token));

    private void Enqueue(Func<CancellationToken, Task> callback)
    {
        if (Volatile.Read(ref _disposeStarted) != 0)
            return;
        if (!_workItems.Writer.TryWrite(new RuntimeWorkItem(callback, null)))
            _logger.LogWarning("Interlocking runtime rejected an observation because its queue is closed.");
    }

    private async Task ConsumeAsync()
    {
        await foreach (var item in _workItems.Reader.ReadAllAsync())
        {
            try
            {
                await item.Callback(_disposeCancellation.Token).ConfigureAwait(false);
                item.Completion?.TrySetResult();
            }
            catch (OperationCanceledException) when (_disposeCancellation.IsCancellationRequested)
            {
                item.Completion?.TrySetCanceled(_disposeCancellation.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Interlocking runtime work item failed.");
                item.Completion?.TrySetException(ex);
            }
        }
    }

    private async Task ObserveFeedbackAsync(
        FeedbackStateChangedEvent observation,
        CancellationToken cancellationToken)
    {
        var definition = _definition;
        var coordinator = _coordinator;
        if (definition == null || coordinator == null || observation.CorrelationId == Guid.Empty)
            return;

        _feedbackStates[observation.InPort] = observation.IsActive;
        foreach (var block in definition.Blocks.Where(block =>
                     block.FeedbackInputs.Any(input => input.InPort == observation.InPort)))
        {
            await coordinator.ObserveBlockAsync(
                block.Id,
                ProjectOccupancy(block),
                observation.CorrelationId,
                cancellationToken).ConfigureAwait(false);
        }

        UpdateSynchronization(observation.CorrelationId);
    }

    private async Task ObserveTurnoutAsync(
        TurnoutInfoChangedEvent observation,
        CancellationToken cancellationToken)
    {
        var coordinator = _coordinator;
        if (coordinator == null || observation.CorrelationId == Guid.Empty)
            return;

        if (!observation.IsSwitched)
        {
            SetSynchronization(false);
            await coordinator.MarkDisconnectedAsync(
                observation.CorrelationId,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await coordinator.ObserveTurnoutFeedbackAsync(
            observation.FunctionAddress,
            observation.OutputPosition,
            observation.CorrelationId,
            cancellationToken).ConfigureAwait(false);
        UpdateSynchronization(observation.CorrelationId);
    }

    private BlockOccupancy ProjectOccupancy(BlockDefinition block)
    {
        var occupied = block.FeedbackInputs.Any(input =>
            input.Role == BlockFeedbackRole.Occupied
            && _feedbackStates.TryGetValue(input.InPort, out var active)
            && active == input.ActiveState);
        var clear = block.FeedbackInputs.Any(input =>
            input.Role == BlockFeedbackRole.Clear
            && _feedbackStates.TryGetValue(input.InPort, out var active)
            && active == input.ActiveState);
        return (occupied, clear) switch
        {
            (true, true) => BlockOccupancy.Fault,
            (true, false) => BlockOccupancy.Occupied,
            (false, true) => BlockOccupancy.Free,
            _ => BlockOccupancy.Unknown
        };
    }

    private async Task SynchronizeCoreAsync(CancellationToken cancellationToken)
    {
        var definition = _definition;
        if (definition == null)
            return;

        SetSynchronization(false);
        try
        {
            await _z21.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            foreach (var decoderAddress in definition.Turnouts
                         .SelectMany(turnout => turnout.Confirmations
                             .SelectMany(mapping => mapping.Conditions)
                             .Select(condition => condition.FunctionAddress)
                             .Append(turnout.DecoderAddress))
                         .Distinct()
                         .Order())
            {
                await _z21.GetTurnoutInfoAsync(decoderAddress, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Interlocking synchronization query failed.");
        }
    }

    private async Task MarkDisconnectedCoreAsync(CancellationToken cancellationToken)
    {
        var definition = _definition;
        var coordinator = _coordinator;
        if (definition == null || coordinator == null)
            return;

        SetSynchronization(false);
        _feedbackStates.Clear();
        var correlationId = Guid.NewGuid();
        foreach (var block in definition.Blocks)
        {
            await coordinator.ObserveBlockAsync(
                block.Id,
                BlockOccupancy.Unknown,
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }

        await coordinator.MarkDisconnectedAsync(correlationId, cancellationToken).ConfigureAwait(false);
    }

    private void UpdateSynchronization(Guid correlationId)
    {
        var state = Current;
        var synchronized = state.Turnouts.Values.All(turnout =>
                               turnout.Lifecycle == TurnoutLifecycle.Confirmed)
                           && state.Blocks.Values.All(block =>
                               block.Occupancy is BlockOccupancy.Free or BlockOccupancy.Occupied);
        bool changed;
        lock (_stateSync)
        {
            changed = _isSynchronized != synchronized;
            _isSynchronized = synchronized;
        }

        if (changed)
            PublishSnapshot(correlationId, synchronized ? "interlocking.synchronized" : "interlocking.unsynchronized");
    }

    private void SetState(InterlockingRuntimeState state, bool synchronized)
    {
        lock (_stateSync)
        {
            _current = state;
            _isSynchronized = synchronized;
        }
    }

    private void SetSynchronization(bool synchronized)
    {
        lock (_stateSync)
            _isSynchronized = synchronized;
    }

    private void PublishSnapshot(Guid correlationId, string code) =>
        _eventBus.Publish(new InterlockingRuntimeSnapshotChangedEvent(
            Current,
            IsSynchronized,
            correlationId,
            code));

    private async Task<RouteCoordinatorResult> ExecuteRouteCommandAsync(
        Func<InterlockingRouteCoordinator, CancellationToken, Task<RouteCoordinatorResult>> command,
        Guid correlationId,
        CancellationToken cancellationToken,
        bool requireSynchronization = true)
    {
        await WhenIdleAsync(cancellationToken).ConfigureAwait(false);
        var coordinator = _coordinator;
        if (coordinator == null)
            return Rejected("interlocking.inactive", "No interlocking definition is active.", correlationId);
        if (requireSynchronization && !IsSynchronized)
            return Rejected("interlocking.unsynchronized", "Route commands require a complete live interlocking snapshot.", correlationId);
        return await command(coordinator, cancellationToken).ConfigureAwait(false);
    }

    private RouteCoordinatorResult Rejected(string code, string message, Guid correlationId) =>
        new(RouteCoordinatorStatus.Rejected, code, message, correlationId, Current);

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);
    }

    private sealed record RuntimeWorkItem(
        Func<CancellationToken, Task> Callback,
        TaskCompletionSource? Completion);

}
