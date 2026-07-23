// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using System.Collections.Frozen;

using Domain;

public enum TurnoutRuntimeTransitionStatus
{
    Accepted,
    Rejected,
    IgnoredDuplicate,
    IgnoredOutOfOrder
}

/// <summary>
/// Immutable lifecycle state for one semantic turnout command and its physical confirmation.
/// </summary>
public sealed record TurnoutCommandRuntimeState(
    Guid TurnoutId,
    TurnoutLifecycle Lifecycle,
    TurnoutPosition? RequestedPosition,
    TurnoutPosition? ConfirmedPosition,
    Guid? CommandCorrelationId,
    DateTimeOffset? ConfirmationDeadlineUtc);

/// <summary>
/// Structured, correlated outcome of one semantic turnout runtime transition.
/// </summary>
public sealed record TurnoutRuntimeTransition(
    TurnoutRuntimeTransitionStatus Status,
    string Code,
    string Message,
    Guid CorrelationId,
    TurnoutCommandRuntimeState State);

/// <summary>
/// Serializes semantic turnout commands and reconciles raw feedback without depending on live hardware.
/// </summary>
public sealed class SemanticTurnoutRuntimeCoordinator
{
    private readonly object _stateSync = new();
    private readonly SemaphoreSlim _commandGate = new(1, 1);
    private readonly SemanticTurnoutCommandService _commandService;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _confirmationTimeout;
    private readonly IReadOnlyDictionary<Guid, ConfirmationDefinition[]> _confirmations;
    private readonly Dictionary<Guid, TurnoutCommandRuntimeState> _states;
    private readonly Dictionary<int, bool> _feedback = [];
    private readonly HashSet<Guid> _processedCorrelationIds = [];

    public SemanticTurnoutRuntimeCoordinator(
        InterlockingDefinition definition,
        SemanticTurnoutCommandService commandService,
        TimeProvider timeProvider,
        TimeSpan confirmationTimeout)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(commandService);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (confirmationTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(confirmationTimeout), "The confirmation timeout must be positive.");

        _commandService = commandService;
        _timeProvider = timeProvider;
        _confirmationTimeout = confirmationTimeout;
        _confirmations = definition.Turnouts.ToFrozenDictionary(
            turnout => turnout.Id,
            turnout => turnout.Confirmations
                .Select(mapping => new ConfirmationDefinition(
                    mapping.Position,
                    mapping.Conditions
                        .Select(condition => new FeedbackCondition(
                            condition.FunctionAddress,
                            condition.OutputPosition))
                        .ToArray()))
                .ToArray());
        _states = definition.Turnouts.ToDictionary(
            turnout => turnout.Id,
            turnout => UnknownState(turnout.Id));
    }

    public IReadOnlyDictionary<Guid, TurnoutCommandRuntimeState> Snapshot
    {
        get
        {
            lock (_stateSync)
                return _states.ToFrozenDictionary();
        }
    }

    public async Task<TurnoutRuntimeTransition> RequestAsync(
        Guid turnoutId,
        TurnoutPosition position,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        // Enter the state machine even for an already-cancelled command so the failed
        // transition remains observable and can be reconciled by the caller.
        await _commandGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            lock (_stateSync)
            {
                if (!_states.TryGetValue(turnoutId, out var current))
                    return Transition(
                        TurnoutRuntimeTransitionStatus.Rejected,
                        "turnout.missing",
                        "The requested turnout does not exist.",
                        correlationId,
                        UnknownState(turnoutId));
                if (correlationId == Guid.Empty)
                    return Transition(
                        TurnoutRuntimeTransitionStatus.Rejected,
                        "turnout.correlation.empty",
                        "A semantic turnout command requires a non-empty correlation ID.",
                        correlationId,
                        current);
                if (_processedCorrelationIds.Contains(correlationId))
                    return Transition(
                        TurnoutRuntimeTransitionStatus.IgnoredDuplicate,
                        "turnout.command.duplicate",
                        "The semantic turnout command correlation was already processed.",
                        correlationId,
                        current);
                if (current.Lifecycle is TurnoutLifecycle.Requested or TurnoutLifecycle.Pending)
                    return Transition(
                        TurnoutRuntimeTransitionStatus.Rejected,
                        "turnout.command.busy",
                        "The turnout already has a command awaiting completion or confirmation.",
                        correlationId,
                        current);

                _processedCorrelationIds.Add(correlationId);
                ClearFeedback(turnoutId);
                _states[turnoutId] = current with
                {
                    Lifecycle = TurnoutLifecycle.Requested,
                    RequestedPosition = position,
                    ConfirmedPosition = null,
                    CommandCorrelationId = correlationId,
                    ConfirmationDeadlineUtc = null
                };
            }

            var execution = await _commandService.ExecuteAsync(
                turnoutId,
                position,
                correlationId,
                cancellationToken).ConfigureAwait(false);

            lock (_stateSync)
            {
                var requested = _states[turnoutId];
                if (requested.Lifecycle != TurnoutLifecycle.Requested
                    || requested.CommandCorrelationId != correlationId)
                {
                    return Transition(
                        TurnoutRuntimeTransitionStatus.IgnoredOutOfOrder,
                        "turnout.command.stale-completion",
                        "A later lifecycle transition superseded the completed effect command.",
                        correlationId,
                        requested);
                }

                var next = execution.Status switch
                {
                    TurnoutCommandExecutionStatus.Succeeded => requested with
                    {
                        Lifecycle = TurnoutLifecycle.Pending,
                        ConfirmationDeadlineUtc = _timeProvider.GetUtcNow() + _confirmationTimeout
                    },
                    TurnoutCommandExecutionStatus.Offline => requested with
                    {
                        Lifecycle = TurnoutLifecycle.Unknown,
                        RequestedPosition = null,
                        CommandCorrelationId = null,
                        ConfirmationDeadlineUtc = null
                    },
                    _ => requested with
                    {
                        Lifecycle = TurnoutLifecycle.Failed,
                        ConfirmationDeadlineUtc = null
                    }
                };
                _states[turnoutId] = next;
                ClearFeedback(turnoutId);

                var code = execution.Status == TurnoutCommandExecutionStatus.Succeeded
                    ? "turnout.command.pending"
                    : execution.Code;
                var message = execution.Status == TurnoutCommandExecutionStatus.Succeeded
                    ? "The semantic turnout command was dispatched and is awaiting confirmation."
                    : execution.Message;
                var status = execution.Status == TurnoutCommandExecutionStatus.Rejected
                    ? TurnoutRuntimeTransitionStatus.Rejected
                    : TurnoutRuntimeTransitionStatus.Accepted;
                return Transition(status, code, message, correlationId, next);
            }
        }
        finally
        {
            _commandGate.Release();
        }
    }

    public IReadOnlyList<TurnoutRuntimeTransition> ObserveFeedback(
        int functionAddress,
        bool outputPosition,
        Guid correlationId)
    {
        lock (_stateSync)
        {
            var relevantTurnoutIds = _confirmations
                .Where(item => item.Value.Any(mapping =>
                    mapping.Conditions.Any(condition => condition.FunctionAddress == functionAddress)))
                .Select(item => item.Key)
                .ToArray();
            if (relevantTurnoutIds.Length == 0)
                return [];

            if (correlationId == Guid.Empty)
                return relevantTurnoutIds.Select(turnoutId => Transition(
                    TurnoutRuntimeTransitionStatus.Rejected,
                    "turnout.correlation.empty",
                    "A turnout observation requires a non-empty correlation ID.",
                    correlationId,
                    _states[turnoutId])).ToArray();
            if (!_processedCorrelationIds.Add(correlationId))
                return relevantTurnoutIds.Select(turnoutId => Transition(
                    TurnoutRuntimeTransitionStatus.IgnoredDuplicate,
                    "turnout.observation.duplicate",
                    "The turnout observation correlation was already processed.",
                    correlationId,
                    _states[turnoutId])).ToArray();

            _feedback[functionAddress] = outputPosition;
            var transitions = new List<TurnoutRuntimeTransition>(relevantTurnoutIds.Length);
            foreach (var turnoutId in relevantTurnoutIds)
                transitions.Add(ReconcileFeedback(turnoutId, correlationId));
            return transitions;
        }
    }

    public IReadOnlyList<TurnoutRuntimeTransition> ExpirePending(Guid correlationId)
    {
        lock (_stateSync)
        {
            if (correlationId == Guid.Empty || !_processedCorrelationIds.Add(correlationId))
                return [];

            var now = _timeProvider.GetUtcNow();
            var transitions = new List<TurnoutRuntimeTransition>();
            foreach (var current in _states.Values.Where(state =>
                         state.Lifecycle == TurnoutLifecycle.Pending
                         && state.ConfirmationDeadlineUtc <= now).ToArray())
            {
                var failed = current with
                {
                    Lifecycle = TurnoutLifecycle.Failed,
                    ConfirmedPosition = null,
                    ConfirmationDeadlineUtc = null
                };
                _states[current.TurnoutId] = failed;
                ClearFeedback(current.TurnoutId);
                transitions.Add(Transition(
                    TurnoutRuntimeTransitionStatus.Accepted,
                    "turnout.confirmation.timeout",
                    "The turnout confirmation deadline expired; reconciliation is required.",
                    correlationId,
                    failed));
            }

            return transitions;
        }
    }

    public IReadOnlyList<TurnoutRuntimeTransition> MarkDisconnected(Guid correlationId)
    {
        lock (_stateSync)
        {
            if (correlationId == Guid.Empty || !_processedCorrelationIds.Add(correlationId))
                return [];

            var transitions = new List<TurnoutRuntimeTransition>(_states.Count);
            foreach (var turnoutId in _states.Values.Select(current => current.TurnoutId).ToArray())
            {
                var unknown = UnknownState(turnoutId);
                _states[turnoutId] = unknown;
                transitions.Add(Transition(
                    TurnoutRuntimeTransitionStatus.Accepted,
                    "turnout.disconnected",
                    "Turnout state became unknown because the effect connection was lost.",
                    correlationId,
                    unknown));
            }

            _feedback.Clear();
            return transitions;
        }
    }

    private TurnoutRuntimeTransition ReconcileFeedback(Guid turnoutId, Guid correlationId)
    {
        var current = _states[turnoutId];
        var matches = _confirmations[turnoutId]
            .Where(mapping => mapping.Conditions.Length > 0
                              && mapping.Conditions.All(condition =>
                                  _feedback.TryGetValue(condition.FunctionAddress, out var value)
                                  && value == condition.OutputPosition))
            .ToArray();
        if (matches.Length == 0)
            return Transition(
                TurnoutRuntimeTransitionStatus.Accepted,
                "turnout.confirmation.incomplete",
                "The turnout observation does not yet satisfy a complete confirmation mapping.",
                correlationId,
                current);
        if (matches.Length > 1)
        {
            var failed = current with
            {
                Lifecycle = TurnoutLifecycle.Failed,
                ConfirmedPosition = null,
                ConfirmationDeadlineUtc = null
            };
            _states[turnoutId] = failed;
            ClearFeedback(turnoutId);
            return Transition(
                TurnoutRuntimeTransitionStatus.Rejected,
                "turnout.confirmation.ambiguous",
                "The feedback matches multiple semantic turnout positions.",
                correlationId,
                failed);
        }

        var confirmedPosition = matches[0].Position;
        if (current.Lifecycle == TurnoutLifecycle.Requested
            || (current.Lifecycle == TurnoutLifecycle.Pending
                && current.RequestedPosition != confirmedPosition))
        {
            ClearFeedback(turnoutId);
            return Transition(
                TurnoutRuntimeTransitionStatus.IgnoredOutOfOrder,
                "turnout.confirmation.out-of-order",
                "The turnout confirmation arrived before dispatch completed or for another requested position.",
                correlationId,
                current);
        }
        if (current.Lifecycle == TurnoutLifecycle.Confirmed
            && current.ConfirmedPosition == confirmedPosition)
        {
            return Transition(
                TurnoutRuntimeTransitionStatus.IgnoredDuplicate,
                "turnout.confirmation.duplicate",
                "The turnout position was already confirmed.",
                correlationId,
                current);
        }

        var confirmed = current with
        {
            Lifecycle = TurnoutLifecycle.Confirmed,
            RequestedPosition = confirmedPosition,
            ConfirmedPosition = confirmedPosition,
            ConfirmationDeadlineUtc = null
        };
        _states[turnoutId] = confirmed;
        return Transition(
            TurnoutRuntimeTransitionStatus.Accepted,
            current.Lifecycle == TurnoutLifecycle.Pending ? "turnout.confirmed" : "turnout.reconciled",
            "The turnout feedback confirmed a semantic position.",
            correlationId,
            confirmed);
    }

    private void ClearFeedback(Guid turnoutId)
    {
        if (!_confirmations.TryGetValue(turnoutId, out var mappings))
            return;
        foreach (var functionAddress in mappings
                     .SelectMany(mapping => mapping.Conditions)
                     .Select(condition => condition.FunctionAddress)
                     .Distinct())
        {
            _feedback.Remove(functionAddress);
        }
    }

    private static TurnoutCommandRuntimeState UnknownState(Guid turnoutId) =>
        new(turnoutId, TurnoutLifecycle.Unknown, null, null, null, null);

    private static TurnoutRuntimeTransition Transition(
        TurnoutRuntimeTransitionStatus status,
        string code,
        string message,
        Guid correlationId,
        TurnoutCommandRuntimeState state) =>
        new(status, code, message, correlationId, state);

    private sealed record ConfirmationDefinition(
        TurnoutPosition Position,
        FeedbackCondition[] Conditions);

    private sealed record FeedbackCondition(int FunctionAddress, bool OutputPosition);
}
