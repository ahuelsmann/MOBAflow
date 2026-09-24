// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using Common.Events;
using Domain;
using Events;
using Interface;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

/// <summary>
/// Projects ordered observations and sends direct turnout commands without resource ownership.
/// </summary>
public sealed class InterlockingRuntimeService : IInterlockingRuntime
{
    private static readonly TimeSpan TurnoutConfirmationTimeout = TimeSpan.FromSeconds(5);

    private readonly object _stateSync = new();
    private readonly SemaphoreSlim _commandGate = new(1, 1);
    private readonly IZ21 _z21;
    private readonly IEventBus _eventBus;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InterlockingRuntimeService> _logger;
    private readonly Channel<RuntimeWorkItem> _workItems;
    private readonly CancellationTokenSource _disposeCancellation = new();
    private readonly List<Guid> _subscriptionIds = [];
    private readonly Task _consumerTask;
    private readonly Dictionary<int, bool> _feedbackStates = [];
    private readonly HashSet<Guid> _processedObservations = [];

    private InterlockingDefinition? _definition;
    private SemanticTurnoutRuntimeCoordinator? _coordinator;
    private CancellationTokenSource _projectCancellation = new();
    private InterlockingRuntimeState _current = InterlockingRuntimeState.Empty;
    private bool _isSynchronized;
    private int _disposeStarted;

    public InterlockingRuntimeService(
        IZ21 z21,
        IEventBus eventBus,
        TimeProvider timeProvider,
        ILogger<InterlockingRuntimeService> logger)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _z21 = z21;
        _eventBus = eventBus;
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
        get { lock (_stateSync) return _current; }
    }

    public bool IsSynchronized
    {
        get { lock (_stateSync) return _isSynchronized; }
    }

    public async Task ActivateAsync(InterlockingDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var coordinator = new SemanticTurnoutRuntimeCoordinator(
            definition,
            new SemanticTurnoutCommandService(definition, new Z21TurnoutEffectGateway(_z21)),
            _timeProvider,
            TurnoutConfirmationTimeout);
        CancellationTokenSource? previousCancellation = null;
        await QueueAndWaitAsync(_ =>
        {
            lock (_stateSync)
            {
                previousCancellation = _projectCancellation;
                _projectCancellation = new CancellationTokenSource();
                _definition = definition;
                _coordinator = coordinator;
                _feedbackStates.Clear();
                _processedObservations.Clear();
                _isSynchronized = false;
                _current = InterlockingRuntimeState.Create(
                    _current.Revision + 1,
                    ProjectTurnouts(coordinator),
                    definition.Blocks.Select(block => new BlockRuntimeState(block.Id, BlockOccupancy.Unknown)),
                    definition.Signals.Select(signal => new SignalRuntimeState(signal.Id, null)),
                    []);
            }
            PublishSnapshot(Guid.NewGuid(), "interlocking.activated");
            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
        if (previousCancellation != null)
        {
            await previousCancellation.CancelAsync().ConfigureAwait(false);
            previousCancellation.Dispose();
        }
    }

    public Task SynchronizeAsync(CancellationToken cancellationToken = default) =>
        QueueAndWaitAsync(SynchronizeCoreAsync, cancellationToken);

    public async Task<TurnoutCoordinatorResult> SetTurnoutAsync(
        Guid turnoutId,
        TurnoutPosition position,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await WhenIdleAsync(cancellationToken).ConfigureAwait(false);
        SemanticTurnoutRuntimeCoordinator? coordinator;
        CancellationTokenSource operationCancellation;
        lock (_stateSync)
        {
            ThrowIfDisposed();
            coordinator = _coordinator;
            operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _disposeCancellation.Token, _projectCancellation.Token);
        }
        using (operationCancellation)
        {
            await _commandGate.WaitAsync(operationCancellation.Token).ConfigureAwait(false);
            try
            {
                if (coordinator == null)
                    return Rejected("interlocking.inactive", "No operational definition is active.", correlationId);
                Task<TurnoutRuntimeTransition>? execution = null;
                await QueueAndWaitAsync(_ =>
                {
                    lock (_stateSync)
                    {
                        if (!ReferenceEquals(coordinator, _coordinator))
                            return Task.CompletedTask;
                    }
                    // Start dispatch in order, but let observations continue while hardware is awaited.
                    execution = coordinator.RequestAsync(turnoutId, position, correlationId, operationCancellation.Token);
                    if (coordinator.Snapshot.TryGetValue(turnoutId, out var state) &&
                        state.Lifecycle == TurnoutLifecycle.Requested)
                    {
                        bool stillRequested;
                        lock (_stateSync)
                        {
                            RefreshState();
                            stillRequested = _current.Turnouts[turnoutId].Lifecycle == TurnoutLifecycle.Requested;
                        }
                        if (stillRequested)
                            PublishSnapshot(correlationId, "turnout.command.requested");
                    }
                    return Task.CompletedTask;
                }, operationCancellation.Token).ConfigureAwait(false);
                if (execution == null)
                    return ProjectChanged(correlationId);
                var transition = await execution.ConfigureAwait(false);
                if (Volatile.Read(ref _disposeStarted) != 0)
                    return Rejected("turnout.shutdown", "The operational runtime is shutting down.", correlationId);
                TurnoutCoordinatorResult? result = null;
                await QueueAndWaitAsync(_ =>
                {
                    lock (_stateSync)
                    {
                        if (!ReferenceEquals(coordinator, _coordinator))
                        {
                            result = ProjectChanged(correlationId);
                            return Task.CompletedTask;
                        }
                        RefreshState();
                        result = new TurnoutCoordinatorResult(
                            CommandStatus(transition), transition.Code, transition.Message, correlationId, _current);
                    }
                    PublishSnapshot(correlationId, transition.Code);
                    return Task.CompletedTask;
                }, _disposeCancellation.Token).ConfigureAwait(false);
                return result!;
            }
            finally
            {
                _commandGate.Release();
            }
        }
    }

    private static TurnoutCoordinatorStatus CommandStatus(TurnoutRuntimeTransition transition) =>
        transition.Status switch
        {
            TurnoutRuntimeTransitionStatus.Rejected => TurnoutCoordinatorStatus.Rejected,
            _ when transition.State.Lifecycle == TurnoutLifecycle.Pending => TurnoutCoordinatorStatus.Pending,
            _ when transition.State.Lifecycle == TurnoutLifecycle.Failed => TurnoutCoordinatorStatus.Failed,
            _ when transition.State.Lifecycle == TurnoutLifecycle.Unknown => TurnoutCoordinatorStatus.Rejected,
            _ => TurnoutCoordinatorStatus.Accepted
        };

    private TurnoutCoordinatorResult ProjectChanged(Guid correlationId) =>
        Rejected("turnout.project.changed", "The active project changed during the command.", correlationId);

    public Task WhenIdleAsync(CancellationToken cancellationToken = default) =>
        QueueAndWaitAsync(_ => Task.CompletedTask, cancellationToken);

    private async Task QueueAndWaitAsync(Func<CancellationToken, Task> callback, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        using var workCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _disposeCancellation.Token);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _workItems.Writer.WriteAsync(new RuntimeWorkItem(_ =>
        {
            workCancellation.Token.ThrowIfCancellationRequested();
            return callback(workCancellation.Token);
        }, completion), workCancellation.Token).ConfigureAwait(false);
        // Once accepted, observe completion before disposing tokens or cleaning up replaced state.
        await completion.Task.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;
        foreach (var subscriptionId in _subscriptionIds)
            _eventBus.Unsubscribe(subscriptionId);
        _subscriptionIds.Clear();
        await _disposeCancellation.CancelAsync().ConfigureAwait(false);
        _workItems.Writer.TryComplete();
        await _consumerTask.ConfigureAwait(false);
        await _commandGate.WaitAsync().ConfigureAwait(false);
        _commandGate.Release();
        _commandGate.Dispose();
        _projectCancellation.Dispose();
        _disposeCancellation.Dispose();
    }

    private void OnFeedbackStateChanged(FeedbackStateChangedEvent observation) =>
        Enqueue(token => ObserveFeedbackAsync(observation, token));

    private void OnTurnoutInfoChanged(TurnoutInfoChangedEvent observation) =>
        Enqueue(token => ObserveTurnoutAsync(observation, token));

    private void Enqueue(Func<CancellationToken, Task> callback)
    {
        if (Volatile.Read(ref _disposeStarted) == 0 &&
            !_workItems.Writer.TryWrite(new RuntimeWorkItem(callback, null)))
            _logger.LogWarning("Operational runtime rejected an observation because its queue is closed.");
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
            catch (OperationCanceledException ex)
            {
                item.Completion?.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operational runtime work item failed.");
                item.Completion?.TrySetException(ex);
            }
        }
    }

    private Task ObserveFeedbackAsync(FeedbackStateChangedEvent observation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_stateSync)
        {
            if (_definition == null || observation.CorrelationId == Guid.Empty ||
                !_processedObservations.Add(observation.CorrelationId))
                return Task.CompletedTask;
            _feedbackStates[observation.InPort] = observation.IsActive;
            RefreshState();
        }
        PublishSnapshot(observation.CorrelationId, "block.observed");
        return Task.CompletedTask;
    }

    private Task ObserveTurnoutAsync(TurnoutInfoChangedEvent observation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_stateSync)
        {
            if (_coordinator == null || observation.CorrelationId == Guid.Empty ||
                !_processedObservations.Add(observation.CorrelationId))
                return Task.CompletedTask;
            if (observation.IsSwitched)
                _coordinator.ObserveFeedback(observation.FunctionAddress, observation.OutputPosition, observation.CorrelationId);
            else
                _coordinator.MarkDisconnected(observation.CorrelationId);
            RefreshState();
        }
        PublishSnapshot(observation.CorrelationId, "turnout.observed");
        return Task.CompletedTask;
    }

    private BlockOccupancy ProjectOccupancy(BlockDefinition block)
    {
        var occupied = block.FeedbackInputs.Any(input =>
            input.Role == BlockFeedbackRole.Occupied &&
            _feedbackStates.TryGetValue(input.InPort, out var active) && active == input.ActiveState);
        var clear = block.FeedbackInputs.Any(input =>
            input.Role == BlockFeedbackRole.Clear &&
            _feedbackStates.TryGetValue(input.InPort, out var active) && active == input.ActiveState);
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
        InterlockingDefinition? definition;
        lock (_stateSync)
        {
            definition = _definition;
            _isSynchronized = false;
        }
        if (definition == null)
            return;
        try
        {
            await _z21.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            foreach (var address in definition.Turnouts.SelectMany(turnout =>
                         turnout.Confirmations.SelectMany(mapping => mapping.Conditions)
                             .Select(condition => condition.FunctionAddress).Append(turnout.DecoderAddress))
                         .Distinct().Order())
                await _z21.GetTurnoutInfoAsync(address, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Operational synchronization query failed.");
        }
    }

    private Task MarkDisconnectedCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var correlationId = Guid.NewGuid();
        lock (_stateSync)
        {
            if (_coordinator == null)
                return Task.CompletedTask;
            _feedbackStates.Clear();
            _coordinator.MarkDisconnected(correlationId);
            RefreshState();
            _isSynchronized = false;
        }
        PublishSnapshot(correlationId, "interlocking.disconnected");
        return Task.CompletedTask;
    }

    // Caller holds _stateSync; snapshots are copied before publication.
    private void RefreshState()
    {
        if (_definition == null || _coordinator == null)
            return;
        _current = InterlockingRuntimeState.Create(
            _current.Revision + 1,
            ProjectTurnouts(_coordinator),
            _definition.Blocks.Select(block => new BlockRuntimeState(block.Id, ProjectOccupancy(block))),
            _current.Signals.Values,
            _processedObservations);
        _isSynchronized = _z21.IsConnected &&
            _current.Turnouts.Values.All(turnout => turnout.Lifecycle == TurnoutLifecycle.Confirmed) &&
            _current.Blocks.Values.All(block => block.Occupancy is BlockOccupancy.Free or BlockOccupancy.Occupied);
    }

    private static IEnumerable<TurnoutRuntimeState> ProjectTurnouts(SemanticTurnoutRuntimeCoordinator coordinator) =>
        coordinator.Snapshot.Values.Select(state => new TurnoutRuntimeState(
            state.TurnoutId, state.Lifecycle, state.RequestedPosition, state.ConfirmedPosition));

    private void PublishSnapshot(Guid correlationId, string code)
    {
        InterlockingRuntimeSnapshotChangedEvent snapshotEvent;
        lock (_stateSync)
            snapshotEvent = new InterlockingRuntimeSnapshotChangedEvent(_current, _isSynchronized, correlationId, code);
        _eventBus.Publish(snapshotEvent);
    }

    private TurnoutCoordinatorResult Rejected(string code, string message, Guid correlationId) =>
        new(TurnoutCoordinatorStatus.Rejected, code, message, correlationId, Current);

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);

    private sealed record RuntimeWorkItem(Func<CancellationToken, Task> Callback, TaskCompletionSource? Completion);
}
