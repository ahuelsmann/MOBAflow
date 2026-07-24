// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using Domain;
using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;

public enum SignalEffectStatus
{
    Succeeded,
    Offline,
    Failed
}

public sealed record SignalEffectCommand(
    Guid RouteId,
    Guid SignalId,
    SignalAspect Aspect,
    Guid CorrelationId);

public sealed record SignalEffectResult(SignalEffectStatus Status, string? Message = null);

public interface ISignalEffectGateway
{
    Task<SignalEffectResult> ExecuteAsync(
        SignalEffectCommand command,
        CancellationToken cancellationToken = default);
}

public enum RouteCoordinatorStatus
{
    Accepted,
    Pending,
    Rejected,
    Failed
}

public sealed record RouteCoordinatorResult(
    RouteCoordinatorStatus Status,
    string Code,
    string Message,
    Guid CorrelationId,
    InterlockingRuntimeState State);

public sealed record TurnoutCoordinatorResult(
    RouteCoordinatorStatus Status,
    string Code,
    string Message,
    Guid CorrelationId,
    InterlockingRuntimeState State);

public sealed record InterlockingLifecycleEvent(
    RouteCoordinatorStatus Status,
    string Code,
    string Message,
    Guid CorrelationId,
    InterlockingRuntimeState State);

public interface IInterlockingLifecycleEventSink
{
    void Publish(InterlockingLifecycleEvent lifecycleEvent);
}

/// <summary>
/// Serializes pure interlocking decisions and replaceable turnout and signal effects.
/// </summary>
public sealed class InterlockingRouteCoordinator : IAsyncDisposable
{
    private const string RouteMissingCode = "route.missing";
    private const string RouteMissingMessage = "The route does not exist.";
    private const string SafeStopFailedCode = "route.signal.safe-stop-failed";

    private readonly object _stateSync = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _shutdownCancellation = new();
    private readonly InterlockingSafetyEngine _engine;
    private readonly SemanticTurnoutRuntimeCoordinator _turnoutRuntime;
    private readonly ISignalEffectGateway _signalGateway;
    private readonly IInterlockingLifecycleEventSink _lifecycleSink;
    private readonly IReadOnlyDictionary<Guid, RouteDefinition> _routes;
    private readonly IReadOnlyDictionary<Guid, SignalAspect> _safeSignalAspects;
    private int _shutdownStarted;
    private int _disposeStarted;
    private InterlockingRuntimeState _state;

    public InterlockingRouteCoordinator(
        InterlockingDefinition definition,
        SemanticTurnoutRuntimeCoordinator turnoutRuntime,
        ISignalEffectGateway signalGateway,
        IInterlockingLifecycleEventSink? lifecycleSink = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(turnoutRuntime);
        ArgumentNullException.ThrowIfNull(signalGateway);

        _engine = new InterlockingSafetyEngine(definition);
        _turnoutRuntime = turnoutRuntime;
        _signalGateway = signalGateway;
        _lifecycleSink = lifecycleSink ?? NullInterlockingLifecycleEventSink.Instance;
        _routes = definition.Routes.ToFrozenDictionary(route => route.Id);
        _safeSignalAspects = definition.Signals.ToFrozenDictionary(signal => signal.Id, signal => signal.SafeAspect);
        _state = _engine.InitialState;
    }

    public InterlockingRuntimeState Snapshot
    {
        get
        {
            lock (_stateSync)
                return _state;
        }
    }

    public async Task<RouteCoordinatorResult> ObserveBlockAsync(
        Guid blockId,
        BlockOccupancy occupancy,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var decision = _engine.ObserveBlock(
                Snapshot,
                blockId,
                occupancy,
                StepCorrelation(correlationId, $"block:{blockId:N}"),
                Snapshot.Revision);
            ApplyDecision(decision, correlationId);
            if (!decision.IsAccepted)
                return FromDecision(decision, correlationId);

            foreach (var route in _routes.Values.Where(route =>
                         Snapshot.Routes[route.Id].Lifecycle is RouteLifecycle.Occupied or RouteLifecycle.Failed))
            {
                var safe = await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
                if (!safe && Snapshot.Routes[route.Id].Lifecycle != RouteLifecycle.Failed)
                {
                    var failed = _engine.CancelRoute(
                        Snapshot,
                        route.Id,
                        StepCorrelation(correlationId, $"block-safe-stop:{route.Id:N}"),
                        Snapshot.Revision);
                    ApplyDecision(failed, correlationId);
                    return Result(
                        RouteCoordinatorStatus.Failed,
                        SafeStopFailedCode,
                        "A route signal could not be restored to its safe aspect.",
                        correlationId);
                }
            }

            return FromDecision(decision with { State = Snapshot }, correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<TurnoutCoordinatorResult> SetTurnoutAsync(
        Guid turnoutId,
        TurnoutPosition position,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return TurnoutResult(RouteCoordinatorStatus.Rejected, "input.correlation.empty", "Every interlocking coordinator operation requires a non-empty correlation ID.", correlationId);
        if (IsShutdown)
            return TurnoutResult(RouteCoordinatorStatus.Rejected, "interlocking.shutdown", "The interlocking coordinator is shutting down.", correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!Snapshot.Turnouts.TryGetValue(turnoutId, out var turnout))
                return TurnoutResult(RouteCoordinatorStatus.Rejected, "turnout.missing", "The requested turnout does not exist.", correlationId);
            if (turnout.LockOwnerRouteId != null)
                return TurnoutResult(RouteCoordinatorStatus.Rejected, "turnout.locked", "The turnout is locked by an active route.", correlationId);
            if (turnout.Lifecycle is TurnoutLifecycle.Requested or TurnoutLifecycle.Pending)
                return TurnoutResult(RouteCoordinatorStatus.Rejected, "turnout.command.busy", "The turnout already has a command awaiting completion or confirmation.", correlationId);

            var requested = _engine.ProjectTurnoutCommand(
                Snapshot,
                turnoutId,
                TurnoutLifecycle.Requested,
                position,
                StepCorrelation(correlationId, "standalone-requested"),
                Snapshot.Revision);
            ApplyDecision(requested, correlationId);
            if (!requested.IsAccepted)
                return TurnoutResult(RouteCoordinatorStatus.Rejected, requested.Code, requested.Message, correlationId);

            var transition = await _turnoutRuntime.RequestAsync(
                turnoutId,
                position,
                StepCorrelation(correlationId, "standalone-effect"),
                cancellationToken).ConfigureAwait(false);
            var projected = _engine.ProjectTurnoutCommand(
                Snapshot,
                turnoutId,
                transition.State.Lifecycle,
                transition.State.RequestedPosition,
                StepCorrelation(correlationId, "standalone-result"),
                Snapshot.Revision);
            ApplyDecision(projected, correlationId);

            var status = transition.Status == TurnoutRuntimeTransitionStatus.Rejected
                ? RouteCoordinatorStatus.Rejected
                : transition.State.Lifecycle switch
                {
                    TurnoutLifecycle.Pending => RouteCoordinatorStatus.Pending,
                    TurnoutLifecycle.Failed => RouteCoordinatorStatus.Failed,
                    TurnoutLifecycle.Unknown => RouteCoordinatorStatus.Rejected,
                    _ => RouteCoordinatorStatus.Accepted
                };
            return TurnoutResult(status, transition.Code, transition.Message, correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> SetRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_routes.TryGetValue(routeId, out var route))
                return Result(RouteCoordinatorStatus.Rejected, RouteMissingCode, RouteMissingMessage, correlationId);

            if (Snapshot.Routes[routeId].Lifecycle == RouteLifecycle.Available)
            {
                var reserved = _engine.ReserveRoute(
                    Snapshot,
                    routeId,
                    StepCorrelation(correlationId, "reserve"),
                    Snapshot.Revision);
                ApplyDecision(reserved, correlationId);
                if (!reserved.IsAccepted)
                    return FromDecision(reserved, correlationId);
            }

            var setting = _engine.BeginSetting(
                Snapshot,
                routeId,
                StepCorrelation(correlationId, "setting"),
                Snapshot.Revision);
            ApplyDecision(setting, correlationId);
            if (!setting.IsAccepted)
                return FromDecision(setting, correlationId);

            foreach (var requirement in route.TurnoutRequirements)
            {
                var requested = _engine.ProjectTurnoutCommand(
                    Snapshot,
                    requirement.TurnoutId,
                    TurnoutLifecycle.Requested,
                    requirement.Position,
                    StepCorrelation(correlationId, $"turnout-requested:{requirement.TurnoutId:N}"),
                    Snapshot.Revision);
                ApplyDecision(requested, correlationId);
                if (!requested.IsAccepted)
                    return FromDecision(requested, correlationId);

                var transition = await _turnoutRuntime.RequestAsync(
                    requirement.TurnoutId,
                    requirement.Position,
                    StepCorrelation(correlationId, $"turnout:{requirement.TurnoutId:N}"),
                    cancellationToken).ConfigureAwait(false);
                var projected = _engine.ProjectTurnoutCommand(
                    Snapshot,
                    requirement.TurnoutId,
                    transition.State.Lifecycle,
                    transition.State.RequestedPosition,
                    StepCorrelation(correlationId, $"turnout-result:{requirement.TurnoutId:N}"),
                    Snapshot.Revision);
                ApplyDecision(projected, correlationId);
                if (transition.State.Lifecycle == TurnoutLifecycle.Pending)
                    continue;

                var failed = _engine.ObserveTurnout(
                    Snapshot,
                    requirement.TurnoutId,
                    null,
                    true,
                    StepCorrelation(correlationId, $"turnout-failed:{requirement.TurnoutId:N}"),
                    Snapshot.Revision);
                ApplyDecision(failed, correlationId);
                await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
                return Result(RouteCoordinatorStatus.Failed, transition.Code, transition.Message, correlationId);
            }

            if (route.TurnoutRequirements.Count == 0)
                return await EstablishAndClearSignalsAsync(route, correlationId, cancellationToken).ConfigureAwait(false);

            return Result(
                RouteCoordinatorStatus.Pending,
                "route.turnout.pending",
                "The route is reserved and awaits turnout confirmation.",
                correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> PreviewRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var preview = _engine.ReserveRoute(
                Snapshot,
                routeId,
                StepCorrelation(correlationId, "preview"),
                Snapshot.Revision);
            return preview.IsAccepted
                ? Result(
                    RouteCoordinatorStatus.Accepted,
                    "route.preview.available",
                    "The route is currently available and passes all reservation checks.",
                    correlationId)
                : FromDecision(preview, correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> SelectRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var selected = _engine.ReserveRoute(
                Snapshot,
                routeId,
                StepCorrelation(correlationId, "select"),
                Snapshot.Revision);
            ApplyDecision(selected, correlationId);
            return FromDecision(selected, correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> ObserveTurnoutFeedbackAsync(
        int functionAddress,
        bool outputPosition,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var transitions = _turnoutRuntime.ObserveFeedback(functionAddress, outputPosition, correlationId);
            foreach (var state in transitions.Where(transition =>
                         transition.Status == TurnoutRuntimeTransitionStatus.Accepted
                         && transition.State.Lifecycle == TurnoutLifecycle.Confirmed)
                     .Select(transition => transition.State))
            {
                var observed = _engine.ObserveTurnout(
                    Snapshot,
                    state.TurnoutId,
                    state.ConfirmedPosition,
                    false,
                    StepCorrelation(correlationId, $"confirm:{state.TurnoutId:N}"),
                    Snapshot.Revision);
                ApplyDecision(observed, correlationId);
            }

            var readyRoute = _routes.Values.FirstOrDefault(route =>
                Snapshot.Routes[route.Id].Lifecycle == RouteLifecycle.Setting
                && route.TurnoutRequirements.All(requirement =>
                    Snapshot.Turnouts[requirement.TurnoutId].Lifecycle == TurnoutLifecycle.Confirmed
                    && Snapshot.Turnouts[requirement.TurnoutId].ConfirmedPosition == requirement.Position));
            if (readyRoute != null)
                return await EstablishAndClearSignalsAsync(readyRoute, correlationId, cancellationToken).ConfigureAwait(false);

            var failedTransition = transitions.FirstOrDefault(transition =>
                transition.State.Lifecycle == TurnoutLifecycle.Failed);
            if (failedTransition != null)
                return Result(RouteCoordinatorStatus.Failed, failedTransition.Code, failedTransition.Message, correlationId);

            return Result(
                RouteCoordinatorStatus.Pending,
                transitions.FirstOrDefault()?.Code ?? "turnout.feedback.unmapped",
                transitions.FirstOrDefault()?.Message ?? "The turnout feedback did not complete a route transition.",
                correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> ExpirePendingAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var transitions = _turnoutRuntime.ExpirePending(correlationId);
            if (transitions.Count == 0)
            {
                return Result(
                    RouteCoordinatorStatus.Pending,
                    "turnout.confirmation.pending",
                    "No pending turnout confirmation has expired.",
                    correlationId);
            }

            foreach (var turnoutId in transitions.Select(transition => transition.State.TurnoutId))
            {
                var observed = _engine.ObserveTurnout(
                    Snapshot,
                    turnoutId,
                    null,
                    true,
                    StepCorrelation(correlationId, $"timeout:{turnoutId:N}"),
                    Snapshot.Revision);
                ApplyDecision(observed, correlationId);
            }

            var affectedRoutes = _routes.Values.Where(route =>
                Snapshot.Routes[route.Id].Lifecycle == RouteLifecycle.Failed
                && route.TurnoutRequirements.Any(requirement =>
                    transitions.Any(transition => transition.State.TurnoutId == requirement.TurnoutId)));
            var safe = true;
            foreach (var route in affectedRoutes)
                safe &= await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);

            var first = transitions[0];
            return Result(
                RouteCoordinatorStatus.Failed,
                safe ? first.Code : SafeStopFailedCode,
                safe ? first.Message : "A timed-out route signal could not be restored to its safe aspect.",
                correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> MarkDisconnectedAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var transitions = _turnoutRuntime.MarkDisconnected(correlationId);
            foreach (var turnoutId in transitions.Select(transition => transition.State.TurnoutId))
            {
                var observed = _engine.ObserveTurnout(
                    Snapshot,
                    turnoutId,
                    null,
                    false,
                    StepCorrelation(correlationId, $"disconnect:{turnoutId:N}"),
                    Snapshot.Revision);
                ApplyDecision(observed, correlationId);
            }

            var affectedRoutes = _routes.Values.Where(route =>
                Snapshot.Routes[route.Id].Lifecycle is RouteLifecycle.Setting
                    or RouteLifecycle.Established
                    or RouteLifecycle.Occupied
                    or RouteLifecycle.Releasing
                    or RouteLifecycle.Failed).ToArray();
            foreach (var routeId in affectedRoutes.Where(route =>
                         Snapshot.Routes[route.Id].Lifecycle != RouteLifecycle.Failed)
                     .Select(route => route.Id))
            {
                var failed = _engine.CancelRoute(
                    Snapshot,
                    routeId,
                    StepCorrelation(correlationId, $"disconnect-route:{routeId:N}"),
                    Snapshot.Revision);
                ApplyDecision(failed, correlationId);
            }

            foreach (var route in affectedRoutes)
                await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);

            return Result(
                RouteCoordinatorStatus.Failed,
                "route.effect.disconnected",
                "The effect connection was lost; active routes require explicit reconciliation and retain their locks.",
                correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> ShutdownAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
        {
            return Result(
                RouteCoordinatorStatus.Accepted,
                "coordinator.shutdown",
                "The interlocking coordinator is already shut down.",
                correlationId);
        }

        await _shutdownCancellation.CancelAsync().ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var transitions = _turnoutRuntime.MarkDisconnected(
                StepCorrelation(correlationId, "shutdown-turnouts"));
            foreach (var turnoutId in transitions.Select(transition => transition.State.TurnoutId))
            {
                var observed = _engine.ObserveTurnout(
                    Snapshot,
                    turnoutId,
                    null,
                    false,
                    StepCorrelation(correlationId, $"shutdown-turnout:{turnoutId:N}"),
                    Snapshot.Revision);
                ApplyDecision(observed, correlationId);
            }

            var affectedRoutes = _routes.Values.Where(route =>
                Snapshot.Routes[route.Id].Lifecycle is RouteLifecycle.Selected
                    or RouteLifecycle.Setting
                    or RouteLifecycle.Established
                    or RouteLifecycle.Occupied
                    or RouteLifecycle.Releasing
                    or RouteLifecycle.Failed).ToArray();
            foreach (var routeId in affectedRoutes.Where(route =>
                         Snapshot.Routes[route.Id].Lifecycle != RouteLifecycle.Failed)
                     .Select(route => route.Id))
            {
                var stopped = _engine.CancelRoute(
                    Snapshot,
                    routeId,
                    StepCorrelation(correlationId, $"shutdown-route:{routeId:N}"),
                    Snapshot.Revision);
                ApplyDecision(stopped, correlationId);
            }

            var safe = true;
            foreach (var route in affectedRoutes.Where(route =>
                         Snapshot.Routes[route.Id].Lifecycle == RouteLifecycle.Failed))
            {
                safe &= await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
            }

            return Result(
                safe ? RouteCoordinatorStatus.Accepted : RouteCoordinatorStatus.Failed,
                safe ? "coordinator.shutdown" : "coordinator.shutdown.safe-stop-failed",
                safe
                    ? "The interlocking coordinator shut down without releasing uncertain route locks."
                    : "The interlocking coordinator retained route locks, but a safe signal effect failed during shutdown.",
                correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;

        await ShutdownAsync(Guid.NewGuid()).ConfigureAwait(false);
        _shutdownCancellation.Dispose();
        _gate.Dispose();
    }

    public async Task<RouteCoordinatorResult> CancelRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_routes.TryGetValue(routeId, out var route))
                return Result(RouteCoordinatorStatus.Rejected, RouteMissingCode, RouteMissingMessage, correlationId);

            var cancelled = _engine.CancelRoute(
                Snapshot,
                routeId,
                StepCorrelation(correlationId, "cancel"),
                Snapshot.Revision);
            ApplyDecision(cancelled, correlationId);
            if (!cancelled.IsAccepted)
                return FromDecision(cancelled, correlationId);

            var safe = await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
            if (!safe)
                return Result(RouteCoordinatorStatus.Failed, SafeStopFailedCode, "Cancellation could not confirm every safe signal effect.", correlationId);

            var status = Snapshot.Routes[routeId].Lifecycle == RouteLifecycle.Failed
                ? RouteCoordinatorStatus.Failed
                : RouteCoordinatorStatus.Accepted;
            return Result(status, cancelled.Code, cancelled.Message, correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> SafeStopRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_routes.TryGetValue(routeId, out var route))
                return Result(RouteCoordinatorStatus.Rejected, RouteMissingCode, RouteMissingMessage, correlationId);
            if (Snapshot.Routes[routeId].Lifecycle == RouteLifecycle.Available)
            {
                return Result(
                    RouteCoordinatorStatus.Rejected,
                    "route.safe-stop.invalid-state",
                    "An available route does not hold resources that require a safe stop.",
                    correlationId);
            }

            if (Snapshot.Routes[routeId].Lifecycle != RouteLifecycle.Failed)
            {
                var stopped = _engine.CancelRoute(
                    Snapshot,
                    routeId,
                    StepCorrelation(correlationId, "safe-stop"),
                    Snapshot.Revision);
                ApplyDecision(stopped, correlationId);
                if (!stopped.IsAccepted)
                    return FromDecision(stopped, correlationId);
            }

            var safe = await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
            return Result(
                safe ? RouteCoordinatorStatus.Accepted : RouteCoordinatorStatus.Failed,
                safe ? "route.safe-stopped" : SafeStopFailedCode,
                safe
                    ? "The route was placed in its fail-safe state and uncertain locks were retained."
                    : "The route retained its locks, but a protected signal could not be confirmed safe.",
                correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> ReconcileRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_routes.TryGetValue(routeId, out var route))
                return Result(RouteCoordinatorStatus.Rejected, RouteMissingCode, RouteMissingMessage, correlationId);

            var safe = await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
            if (!safe)
            {
                return Result(
                    RouteCoordinatorStatus.Failed,
                    "route.reconcile.signal-safe-failed",
                    "The route remains locked because its protected signals could not be confirmed safe.",
                    correlationId);
            }

            var reconciled = _engine.ReconcileFailedRoute(
                Snapshot,
                routeId,
                StepCorrelation(correlationId, "reconcile"),
                Snapshot.Revision);
            ApplyDecision(reconciled, correlationId);
            return FromDecision(reconciled, correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RouteCoordinatorResult> ReleaseRouteAsync(
        Guid routeId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            return EmptyCorrelation(correlationId);
        if (IsShutdown)
            return ShutdownRejected(correlationId);

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCancellation.Token);
        cancellationToken = operationCancellation.Token;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_routes.TryGetValue(routeId, out var route))
                return Result(RouteCoordinatorStatus.Rejected, RouteMissingCode, RouteMissingMessage, correlationId);

            var releasing = _engine.BeginRelease(
                Snapshot,
                routeId,
                StepCorrelation(correlationId, "release-begin"),
                Snapshot.Revision);
            ApplyDecision(releasing, correlationId);
            if (!releasing.IsAccepted)
                return FromDecision(releasing, correlationId);

            var safe = await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
            if (!safe)
                return Result(RouteCoordinatorStatus.Failed, SafeStopFailedCode, "Route locks were retained because a safe signal effect failed.", correlationId);

            var released = _engine.CompleteRelease(
                Snapshot,
                routeId,
                StepCorrelation(correlationId, "release-complete"),
                Snapshot.Revision);
            ApplyDecision(released, correlationId);
            return FromDecision(released, correlationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<RouteCoordinatorResult> EstablishAndClearSignalsAsync(
        RouteDefinition route,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var established = _engine.EstablishRoute(
            Snapshot,
            route.Id,
            StepCorrelation(correlationId, "establish"),
            Snapshot.Revision);
        ApplyDecision(established, correlationId);
        if (!established.IsAccepted)
            return FromDecision(established, correlationId);

        foreach (var requirement in route.SignalRequirements)
        {
            var effect = await ExecuteSignalEffectAsync(
                new SignalEffectCommand(
                    route.Id,
                    requirement.SignalId,
                    requirement.ProceedAspect,
                    StepCorrelation(correlationId, $"signal-proceed:{requirement.SignalId:N}")),
                cancellationToken).ConfigureAwait(false);
            if (effect.Status == SignalEffectStatus.Succeeded)
                continue;

            await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
            var failed = _engine.CancelRoute(
                Snapshot,
                route.Id,
                StepCorrelation(correlationId, "signal-failed"),
                Snapshot.Revision);
            ApplyDecision(failed, correlationId);
            return Result(
                RouteCoordinatorStatus.Failed,
                effect.Status == SignalEffectStatus.Offline ? "route.signal.offline" : "route.signal.failed",
                effect.Message ?? "A configured route signal effect failed.",
                correlationId);
        }

        var cleared = _engine.ClearRouteSignals(
            Snapshot,
            route.Id,
            StepCorrelation(correlationId, "signal-state"),
            Snapshot.Revision);
        ApplyDecision(cleared, correlationId);
        if (cleared.IsAccepted)
            return FromDecision(cleared, correlationId);

        await SetSignalsSafeAsync(route, correlationId, cancellationToken).ConfigureAwait(false);
        var cancelled = _engine.CancelRoute(
            Snapshot,
            route.Id,
            StepCorrelation(correlationId, "signal-state-failed"),
            Snapshot.Revision);
        ApplyDecision(cancelled, correlationId);
        return Result(RouteCoordinatorStatus.Failed, cleared.Code, cleared.Message, correlationId);
    }

    private async Task<bool> SetSignalsSafeAsync(
        RouteDefinition route,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var succeeded = true;
        foreach (var signalId in route.SignalRequirements.Select(requirement => requirement.SignalId))
        {
            var effect = await ExecuteSignalEffectAsync(
                new SignalEffectCommand(
                    route.Id,
                    signalId,
                    _safeSignalAspects[signalId],
                    StepCorrelation(correlationId, $"signal-safe:{signalId:N}")),
                cancellationToken).ConfigureAwait(false);
            succeeded &= effect.Status == SignalEffectStatus.Succeeded;
        }

        return succeeded;
    }

    private async Task<SignalEffectResult> ExecuteSignalEffectAsync(
        SignalEffectCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _signalGateway.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new SignalEffectResult(SignalEffectStatus.Failed, ex.Message);
        }
    }

    private void ApplyDecision(InterlockingDecision decision, Guid correlationId)
    {
        var changed = false;
        lock (_stateSync)
        {
            changed = _state.Revision != decision.State.Revision;
            _state = decision.State;
        }

        if (!changed)
            return;

        PublishLifecycleEvent(
            decision.Status == InterlockingDecisionStatus.Accepted
                ? RouteCoordinatorStatus.Accepted
                : RouteCoordinatorStatus.Rejected,
            decision.Code,
            decision.Message,
            correlationId,
            decision.State);
    }

    private RouteCoordinatorResult FromDecision(InterlockingDecision decision, Guid correlationId)
    {
        var status = decision.Status switch
        {
            InterlockingDecisionStatus.Accepted => RouteCoordinatorStatus.Accepted,
            InterlockingDecisionStatus.Rejected => RouteCoordinatorStatus.Rejected,
            _ => RouteCoordinatorStatus.Pending
        };
        return Result(status, decision.Code, decision.Message, correlationId);
    }

    private RouteCoordinatorResult Result(
        RouteCoordinatorStatus status,
        string code,
        string message,
        Guid correlationId)
    {
        var result = new RouteCoordinatorResult(status, code, message, correlationId, Snapshot);
        PublishLifecycleEvent(
            result.Status,
            result.Code,
            result.Message,
            result.CorrelationId,
            result.State);

        return result;
    }

    private TurnoutCoordinatorResult TurnoutResult(
        RouteCoordinatorStatus status,
        string code,
        string message,
        Guid correlationId)
    {
        var result = new TurnoutCoordinatorResult(status, code, message, correlationId, Snapshot);
        PublishLifecycleEvent(status, code, message, correlationId, result.State);
        return result;
    }

    private void PublishLifecycleEvent(
        RouteCoordinatorStatus status,
        string code,
        string message,
        Guid correlationId,
        InterlockingRuntimeState state)
    {
        try
        {
            _lifecycleSink.Publish(new InterlockingLifecycleEvent(
                status,
                code,
                message,
                correlationId,
                state));
        }
        catch (Exception)
        {
            // Lifecycle projection is observational and must never weaken a completed safety decision.
        }
    }

    private RouteCoordinatorResult EmptyCorrelation(Guid correlationId) =>
        Result(
            RouteCoordinatorStatus.Rejected,
            "input.correlation.empty",
            "Every interlocking coordinator operation requires a non-empty correlation ID.",
            correlationId);

    private bool IsShutdown => Volatile.Read(ref _shutdownStarted) != 0;

    private RouteCoordinatorResult ShutdownRejected(Guid correlationId) =>
        Result(
            RouteCoordinatorStatus.Rejected,
            "coordinator.shutdown",
            "The interlocking coordinator is shut down and rejects new operations.",
            correlationId);

    private static Guid StepCorrelation(Guid correlationId, string step)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{correlationId:N}:{step}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private sealed class NullInterlockingLifecycleEventSink : IInterlockingLifecycleEventSink
    {
        public static NullInterlockingLifecycleEventSink Instance { get; } = new();

        public void Publish(InterlockingLifecycleEvent lifecycleEvent)
        {
        }
    }
}

/// <summary>
/// Simulation adapter that records signal effects and injects deterministic outcomes.
/// </summary>
public sealed class RecordingSignalEffectGateway : ISignalEffectGateway
{
    private readonly object _sync = new();
    private readonly Func<SignalEffectCommand, SignalEffectResult> _resultFactory;
    private readonly List<SignalEffectCommand> _commands = [];

    public RecordingSignalEffectGateway(Func<SignalEffectCommand, SignalEffectResult>? resultFactory = null)
    {
        _resultFactory = resultFactory ?? (_ => new SignalEffectResult(SignalEffectStatus.Succeeded));
    }

    public IReadOnlyList<SignalEffectCommand> Commands
    {
        get
        {
            lock (_sync)
                return _commands.ToArray();
        }
    }

    public Task<SignalEffectResult> ExecuteAsync(
        SignalEffectCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
            _commands.Add(command);
        return Task.FromResult(_resultFactory(command));
    }
}

/// <summary>
/// Simulation adapter that records immutable interlocking lifecycle events.
/// </summary>
public sealed class RecordingInterlockingLifecycleEventSink : IInterlockingLifecycleEventSink
{
    private readonly object _sync = new();
    private readonly List<InterlockingLifecycleEvent> _events = [];

    public IReadOnlyList<InterlockingLifecycleEvent> Events
    {
        get
        {
            lock (_sync)
                return _events.ToArray();
        }
    }

    public void Publish(InterlockingLifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        lock (_sync)
            _events.Add(lifecycleEvent);
    }
}