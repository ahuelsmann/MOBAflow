// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Interlocking;

using Domain;

/// <summary>
/// Pure, deterministic interlocking state machine. It performs no I/O and dispatches no hardware commands.
/// </summary>
public sealed class InterlockingSafetyEngine
{
    private const string RouteMissingCode = "route.missing";
    private const string RouteMissingMessage = "The route does not exist.";

    private readonly IReadOnlyDictionary<Guid, RouteResources> _routes;
    private readonly IReadOnlyDictionary<Guid, SignalAspect> _safeSignalAspects;

    public InterlockingSafetyEngine(InterlockingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ConflictMatrix = InterlockingConflictAnalyzer.Analyze(definition);
        _routes = definition.Routes.ToDictionary(
            route => route.Id,
            route => new RouteResources(
                route.Id,
                route.ProtectedBlockIds.Distinct().ToArray(),
                route.SignalRequirements.ToDictionary(requirement => requirement.SignalId, requirement => requirement.ProceedAspect),
                route.TurnoutRequirements.ToDictionary(requirement => requirement.TurnoutId, requirement => requirement.Position)));
        _safeSignalAspects = definition.Signals.ToDictionary(signal => signal.Id, signal => signal.SafeAspect);

        InitialState = InterlockingRuntimeState.Create(
            0,
            definition.Turnouts.Select(turnout => new TurnoutRuntimeState(
                turnout.Id,
                TurnoutLifecycle.Unknown,
                null,
                null,
                null)),
            definition.Blocks.Select(block => new BlockRuntimeState(block.Id, BlockOccupancy.Unknown, null)),
            definition.Signals.Select(signal => new SignalRuntimeState(signal.Id, signal.SafeAspect, null)),
            definition.Routes.Select(route => new RouteRuntimeState(route.Id, RouteLifecycle.Available, null)),
            []);
    }

    public InterlockingConflictMatrix ConflictMatrix { get; }

    public InterlockingRuntimeState InitialState { get; }

    public InterlockingDecision ObserveBlock(
        InterlockingRuntimeState state,
        Guid blockId,
        BlockOccupancy occupancy,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!state.Blocks.TryGetValue(blockId, out var block))
            return Reject(state, "block.missing", "The observed block does not exist.", [blockId]);

        var blocks = state.Blocks.ToDictionary();
        var signals = state.Signals.ToDictionary();
        var routes = state.Routes.ToDictionary();
        blocks[blockId] = block with { Occupancy = occupancy };

        if (block.ReservationOwnerRouteId is { } ownerId && routes.TryGetValue(ownerId, out var owner))
        {
            if (occupancy == BlockOccupancy.Occupied && owner.Lifecycle == RouteLifecycle.Established)
            {
                routes[ownerId] = owner with { Lifecycle = RouteLifecycle.Occupied };
                SetSignalsSafe(signals, _routes[ownerId]);
            }
            else if ((occupancy is BlockOccupancy.Unknown or BlockOccupancy.Fault && HoldsResources(owner.Lifecycle))
                     || (occupancy == BlockOccupancy.Occupied
                         && owner.Lifecycle is RouteLifecycle.Selected or RouteLifecycle.Setting or RouteLifecycle.Releasing))
            {
                routes[ownerId] = owner with { Lifecycle = RouteLifecycle.Failed, FailureCode = "route.block.unsafe" };
                SetSignalsSafe(signals, _routes[ownerId]);
            }
        }

        return Accept(state, new RuntimeStateChanges(Blocks: blocks, Signals: signals, Routes: routes), correlationId, "block.observed", "Block observation accepted.", [blockId]);
    }

    public InterlockingDecision ObserveTurnout(
        InterlockingRuntimeState state,
        Guid turnoutId,
        TurnoutPosition? confirmedPosition,
        bool isFaulted,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!state.Turnouts.TryGetValue(turnoutId, out var turnout))
            return Reject(state, "turnout.missing", "The observed turnout does not exist.", [turnoutId]);

        var turnouts = state.Turnouts.ToDictionary();
        var signals = state.Signals.ToDictionary();
        var routes = state.Routes.ToDictionary();
        var lifecycle = (isFaulted, confirmedPosition.HasValue) switch
        {
            (true, _) => TurnoutLifecycle.Failed,
            (false, true) => TurnoutLifecycle.Confirmed,
            _ => TurnoutLifecycle.Unknown
        };
        turnouts[turnoutId] = turnout with
        {
            Lifecycle = lifecycle,
            ConfirmedPosition = isFaulted ? null : confirmedPosition
        };

        if (turnout.LockOwnerRouteId is { } ownerId
            && routes.TryGetValue(ownerId, out var owner)
            && _routes.TryGetValue(ownerId, out var ownerResources)
            && ownerResources.TurnoutRequirements.TryGetValue(turnoutId, out var requiredPosition)
            && (isFaulted
                || (owner.Lifecycle != RouteLifecycle.Selected && confirmedPosition != requiredPosition)))
        {
            routes[ownerId] = owner with { Lifecycle = RouteLifecycle.Failed, FailureCode = "route.turnout.unconfirmed" };
            SetSignalsSafe(signals, ownerResources);
        }

        return Accept(state, new RuntimeStateChanges(Turnouts: turnouts, Signals: signals, Routes: routes), correlationId, "turnout.observed", "Turnout observation accepted.", [turnoutId]);
    }

    public InterlockingDecision ProjectTurnoutCommand(
        InterlockingRuntimeState state,
        Guid turnoutId,
        TurnoutLifecycle lifecycle,
        TurnoutPosition? requestedPosition,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!state.Turnouts.TryGetValue(turnoutId, out var turnout))
            return Reject(state, "turnout.missing", "The commanded turnout does not exist.", [turnoutId]);
        if (lifecycle is TurnoutLifecycle.Confirmed)
            return Reject(state, "turnout.command.lifecycle.invalid", "A command transition cannot confirm a turnout without feedback.", [turnoutId]);
        if (lifecycle is (TurnoutLifecycle.Requested or TurnoutLifecycle.Pending) && requestedPosition == null)
            return Reject(state, "turnout.command.position.missing", "A requested or pending turnout command requires a semantic position.", [turnoutId]);

        var turnouts = state.Turnouts.ToDictionary();
        turnouts[turnoutId] = turnout with
        {
            Lifecycle = lifecycle,
            RequestedPosition = lifecycle == TurnoutLifecycle.Unknown ? null : requestedPosition,
            ConfirmedPosition = null
        };
        return Accept(
            state,
            new RuntimeStateChanges(Turnouts: turnouts),
            correlationId,
            "turnout.command.projected",
            "Turnout command state projected.",
            [turnoutId]);
    }

    public InterlockingDecision ReserveRoute(
        InterlockingRuntimeState state,
        Guid routeId,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!_routes.TryGetValue(routeId, out var resources) || !state.Routes.TryGetValue(routeId, out var route))
            return Reject(state, RouteMissingCode, "The requested route does not exist.", [routeId]);
        if (route.Lifecycle != RouteLifecycle.Available)
            return Reject(state, "route.unavailable", "The requested route is not available.", [routeId]);

        var activeConflict = FindActiveConflict(state, routeId);
        if (activeConflict != null)
            return Reject(state, "route.conflict", "A conflicting route already holds safety resources.", [routeId, activeConflict.RouteId]);

        var unsafeBlock = resources.BlockIds
            .Select(id => state.Blocks.GetValueOrDefault(id))
            .FirstOrDefault(block => block == null || block.Occupancy != BlockOccupancy.Free || block.ReservationOwnerRouteId.HasValue);
        if (unsafeBlock != null)
            return Reject(state, "route.block.unsafe", "Every protected block must be explicitly free and unreserved.", [routeId, unsafeBlock.BlockId]);
        if (resources.BlockIds.Any(id => !state.Blocks.ContainsKey(id)))
            return Reject(state, "route.block.missing", "A protected block is missing from runtime state.", [routeId]);

        var lockedTurnout = resources.TurnoutRequirements.Keys
            .Select(id => state.Turnouts.GetValueOrDefault(id))
            .FirstOrDefault(turnout => turnout?.LockOwnerRouteId.HasValue == true);
        if (lockedTurnout != null)
            return Reject(state, "route.turnout.locked", "A required turnout is locked by another route.", [routeId, lockedTurnout.TurnoutId]);
        if (resources.TurnoutRequirements.Keys.Any(id => !state.Turnouts.ContainsKey(id)))
            return Reject(state, "route.turnout.missing", "A required turnout is missing from runtime state.", [routeId]);

        var lockedSignal = resources.SignalRequirements.Keys
            .Select(id => state.Signals.GetValueOrDefault(id))
            .FirstOrDefault(signal => signal?.LockOwnerRouteId.HasValue == true);
        if (lockedSignal != null)
            return Reject(state, "route.signal.locked", "A protected signal is locked by another route.", [routeId, lockedSignal.SignalId]);
        if (resources.SignalRequirements.Keys.Any(id => !state.Signals.ContainsKey(id)))
            return Reject(state, "route.signal.missing", "A protected signal is missing from runtime state.", [routeId]);

        var turnouts = state.Turnouts.ToDictionary();
        var blocks = state.Blocks.ToDictionary();
        var signals = state.Signals.ToDictionary();
        var routes = state.Routes.ToDictionary();

        foreach (var turnoutId in resources.TurnoutRequirements.Keys)
            turnouts[turnoutId] = turnouts[turnoutId] with { LockOwnerRouteId = routeId };
        foreach (var blockId in resources.BlockIds)
            blocks[blockId] = blocks[blockId] with { ReservationOwnerRouteId = routeId };
        foreach (var signalId in resources.SignalRequirements.Keys)
            signals[signalId] = signals[signalId] with { LockOwnerRouteId = routeId };
        routes[routeId] = route with { Lifecycle = RouteLifecycle.Selected, FailureCode = null };

        return Accept(state, new RuntimeStateChanges(turnouts, blocks, signals, routes), correlationId, "route.reserved", "Route resources reserved atomically.", [routeId]);
    }

    public InterlockingDecision BeginSetting(
        InterlockingRuntimeState state,
        Guid routeId,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!TryGetRouteInState(state, routeId, RouteLifecycle.Selected, out var route, out var resources, out var invalid))
            return invalid!;

        var turnouts = state.Turnouts.ToDictionary();
        var routes = state.Routes.ToDictionary();
        foreach (var requirement in resources!.TurnoutRequirements)
        {
            turnouts[requirement.Key] = turnouts[requirement.Key] with
            {
                Lifecycle = TurnoutLifecycle.Requested,
                RequestedPosition = requirement.Value
            };
        }

        routes[routeId] = route! with { Lifecycle = RouteLifecycle.Setting };
        return Accept(state, new RuntimeStateChanges(Turnouts: turnouts, Routes: routes), correlationId, "route.setting", "Route entered setting state.", [routeId]);
    }

    public InterlockingDecision EstablishRoute(
        InterlockingRuntimeState state,
        Guid routeId,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!TryGetRouteInState(state, routeId, RouteLifecycle.Setting, out var route, out var resources, out var invalid))
            return invalid!;

        var unsafeBlock = resources!.BlockIds.Select(id => state.Blocks[id]).FirstOrDefault(block => block.Occupancy != BlockOccupancy.Free);
        if (unsafeBlock != null)
            return Reject(state, "route.block.unsafe", "A route cannot be established unless every protected block is explicitly free.", [routeId, unsafeBlock.BlockId]);

        var unconfirmedTurnout = resources.TurnoutRequirements.FirstOrDefault(requirement =>
            state.Turnouts[requirement.Key].Lifecycle != TurnoutLifecycle.Confirmed
            || state.Turnouts[requirement.Key].ConfirmedPosition != requirement.Value);
        if (!unconfirmedTurnout.Equals(default(KeyValuePair<Guid, TurnoutPosition>)))
            return Reject(state, "route.turnout.unconfirmed", "A route cannot be established until every turnout confirms its required position.", [routeId, unconfirmedTurnout.Key]);

        var routes = state.Routes.ToDictionary();
        routes[routeId] = route! with { Lifecycle = RouteLifecycle.Established };
        return Accept(state, new RuntimeStateChanges(Routes: routes), correlationId, "route.established", "Route established after all safety prerequisites were confirmed.", [routeId]);
    }

    public InterlockingDecision ClearRouteSignals(
        InterlockingRuntimeState state,
        Guid routeId,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!_routes.TryGetValue(routeId, out var resources) || !state.Routes.TryGetValue(routeId, out var route))
            return Reject(state, RouteMissingCode, RouteMissingMessage, [routeId]);
        if (route.Lifecycle != RouteLifecycle.Established)
            return Reject(state, "route.signal.invalid-state", "Route signals can only show proceed for an established route.", [routeId]);

        var signals = state.Signals.ToDictionary();
        foreach (var requirement in resources.SignalRequirements)
        {
            var signal = signals[requirement.Key];
            if (signal.LockOwnerRouteId != routeId)
                return Reject(state, "route.signal.unlocked", "Every route signal must remain locked before it can show proceed.", [routeId, requirement.Key]);
            signals[requirement.Key] = signal with { Aspect = requirement.Value };
        }

        return Accept(state, new RuntimeStateChanges(Signals: signals), correlationId, "route.signal.proceed", "Configured route signal aspects accepted.", [routeId]);
    }

    public InterlockingDecision BeginRelease(
        InterlockingRuntimeState state,
        Guid routeId,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!_routes.TryGetValue(routeId, out var resources) || !state.Routes.TryGetValue(routeId, out var route))
            return Reject(state, RouteMissingCode, RouteMissingMessage, [routeId]);
        if (route.Lifecycle is not (RouteLifecycle.Established or RouteLifecycle.Occupied))
            return Reject(state, "route.release.invalid-state", "Only an established or occupied route can begin release.", [routeId]);

        var unsafeBlock = resources.BlockIds.Select(id => state.Blocks[id]).FirstOrDefault(block => block.Occupancy != BlockOccupancy.Free);
        if (unsafeBlock != null)
            return Reject(state, "route.release.block-unsafe", "Full-route release requires every protected block to be explicitly free.", [routeId, unsafeBlock.BlockId]);

        var routes = state.Routes.ToDictionary();
        var signals = state.Signals.ToDictionary();
        SetSignalsSafe(signals, resources);
        routes[routeId] = route with { Lifecycle = RouteLifecycle.Releasing };
        return Accept(state, new RuntimeStateChanges(Signals: signals, Routes: routes), correlationId, "route.releasing", "Route entered conservative full-route release.", [routeId]);
    }

    public InterlockingDecision CompleteRelease(
        InterlockingRuntimeState state,
        Guid routeId,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!TryGetRouteInState(state, routeId, RouteLifecycle.Releasing, out var route, out var resources, out var invalid))
            return invalid!;

        return ReleaseResources(state, route!, resources!, correlationId, "route.available", "Route resources released.");
    }

    public InterlockingDecision CancelRoute(
        InterlockingRuntimeState state,
        Guid routeId,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!_routes.TryGetValue(routeId, out var resources) || !state.Routes.TryGetValue(routeId, out var route))
            return Reject(state, RouteMissingCode, RouteMissingMessage, [routeId]);

        if (route.Lifecycle == RouteLifecycle.Selected)
            return ReleaseResources(state, route, resources, correlationId, "route.cancelled", "Selected route cancelled before hardware dispatch.");
        if (route.Lifecycle is RouteLifecycle.Setting or RouteLifecycle.Established or RouteLifecycle.Occupied or RouteLifecycle.Releasing)
        {
            var routes = state.Routes.ToDictionary();
            var signals = state.Signals.ToDictionary();
            SetSignalsSafe(signals, resources);
            routes[routeId] = route with { Lifecycle = RouteLifecycle.Failed, FailureCode = "route.cancel.reconciliation" };
            return Accept(state, new RuntimeStateChanges(Signals: signals, Routes: routes), correlationId, "route.cancel.reconciliation", "Cancellation after setting began requires explicit reconciliation; locks were retained.", [routeId]);
        }

        return Reject(state, "route.cancel.invalid-state", "The route cannot be cancelled from its current state.", [routeId]);
    }

    public InterlockingDecision ReconcileFailedRoute(
        InterlockingRuntimeState state,
        Guid routeId,
        Guid correlationId,
        long expectedRevision)
    {
        if (TryRejectInput(state, correlationId, expectedRevision) is { } rejected)
            return rejected;
        if (!_routes.TryGetValue(routeId, out var resources) || !state.Routes.TryGetValue(routeId, out var route))
            return Reject(state, RouteMissingCode, RouteMissingMessage, [routeId]);
        if (route.Lifecycle != RouteLifecycle.Failed)
            return Reject(state, "route.reconcile.invalid-state", "Only a failed route can be reconciled.", [routeId]);

        var unsafeBlock = resources.BlockIds.FirstOrDefault(blockId =>
            state.Blocks[blockId].Occupancy != BlockOccupancy.Free
            || state.Blocks[blockId].ReservationOwnerRouteId != routeId);
        if (unsafeBlock != Guid.Empty)
            return Reject(state, "route.reconcile.block-unsafe", "Every protected block must be explicitly free and remain reserved by the failed route.", [routeId, unsafeBlock]);

        var unsafeTurnout = resources.TurnoutRequirements.FirstOrDefault(requirement =>
            state.Turnouts[requirement.Key].Lifecycle != TurnoutLifecycle.Confirmed
            || state.Turnouts[requirement.Key].ConfirmedPosition != requirement.Value
            || state.Turnouts[requirement.Key].LockOwnerRouteId != routeId);
        if (!unsafeTurnout.Equals(default(KeyValuePair<Guid, TurnoutPosition>)))
            return Reject(state, "route.reconcile.turnout-unsafe", "Every required turnout must be confirmed and remain locked by the failed route.", [routeId, unsafeTurnout.Key]);

        var unsafeSignal = resources.SignalRequirements.Keys.FirstOrDefault(signalId =>
            state.Signals[signalId].Aspect != _safeSignalAspects[signalId]
            || state.Signals[signalId].LockOwnerRouteId != routeId);
        if (unsafeSignal != Guid.Empty)
            return Reject(state, "route.reconcile.signal-unsafe", "Every protected signal must show its safe aspect and remain locked by the failed route.", [routeId, unsafeSignal]);

        return ReleaseResources(
            state,
            route,
            resources,
            correlationId,
            "route.reconciled",
            "Verified safe route state reconciled and resources released.");
    }

    private InterlockingDecision ReleaseResources(
        InterlockingRuntimeState state,
        RouteRuntimeState route,
        RouteResources resources,
        Guid correlationId,
        string code,
        string message)
    {
        var turnouts = state.Turnouts.ToDictionary();
        var blocks = state.Blocks.ToDictionary();
        var signals = state.Signals.ToDictionary();
        var routes = state.Routes.ToDictionary();

        foreach (var turnoutId in resources.TurnoutRequirements.Keys)
            turnouts[turnoutId] = turnouts[turnoutId] with { LockOwnerRouteId = null, RequestedPosition = null };
        foreach (var blockId in resources.BlockIds)
            blocks[blockId] = blocks[blockId] with { ReservationOwnerRouteId = null };
        foreach (var signalId in resources.SignalRequirements.Keys)
            signals[signalId] = signals[signalId] with { LockOwnerRouteId = null, Aspect = _safeSignalAspects[signalId] };
        routes[route.RouteId] = route with { Lifecycle = RouteLifecycle.Available, FailureCode = null };

        return Accept(state, new RuntimeStateChanges(turnouts, blocks, signals, routes), correlationId, code, message, [route.RouteId]);
    }

    private bool TryGetRouteInState(
        InterlockingRuntimeState state,
        Guid routeId,
        RouteLifecycle expectedLifecycle,
        out RouteRuntimeState? route,
        out RouteResources? resources,
        out InterlockingDecision? invalid)
    {
        route = null;
        resources = null;
        if (!_routes.TryGetValue(routeId, out resources) || !state.Routes.TryGetValue(routeId, out route))
        {
            invalid = Reject(state, RouteMissingCode, RouteMissingMessage, [routeId]);
            return false;
        }

        if (route.Lifecycle != expectedLifecycle)
        {
            invalid = Reject(state, "route.invalid-state", $"Route must be {expectedLifecycle} for this transition.", [routeId]);
            return false;
        }

        invalid = null;
        return true;
    }

    private static bool HoldsResources(RouteLifecycle lifecycle) =>
        lifecycle is RouteLifecycle.Selected
            or RouteLifecycle.Setting
            or RouteLifecycle.Established
            or RouteLifecycle.Occupied
            or RouteLifecycle.Releasing
            or RouteLifecycle.Failed;

    private RouteRuntimeState? FindActiveConflict(InterlockingRuntimeState state, Guid routeId) =>
        state.Routes.Values.FirstOrDefault(candidate =>
            HoldsResources(candidate.Lifecycle)
            && ConflictMatrix.AreConflicting(routeId, candidate.RouteId));

    private void SetSignalsSafe(
        IDictionary<Guid, SignalRuntimeState> signals,
        RouteResources resources)
    {
        foreach (var signalId in resources.SignalRequirements.Keys)
            signals[signalId] = signals[signalId] with { Aspect = _safeSignalAspects[signalId] };
    }

    private static InterlockingDecision? TryRejectInput(
        InterlockingRuntimeState state,
        Guid correlationId,
        long expectedRevision)
    {
        if (correlationId == Guid.Empty)
            return Reject(state, "input.correlation.empty", "Every interlocking input requires a non-empty correlation ID.", []);
        if (state.ProcessedCorrelationIds.Contains(correlationId))
            return new InterlockingDecision(InterlockingDecisionStatus.IgnoredDuplicate, "input.duplicate", "The correlation ID was already processed.", [], state);
        if (expectedRevision < state.Revision)
            return new InterlockingDecision(InterlockingDecisionStatus.IgnoredStale, "input.revision.stale", "The input targets an older runtime revision.", [], state);
        if (expectedRevision > state.Revision)
            return Reject(state, "input.revision.future", "The input targets a future runtime revision.", []);

        return null;
    }

    private static InterlockingDecision Accept(
        InterlockingRuntimeState state,
        RuntimeStateChanges changes,
        Guid correlationId,
        string code,
        string message,
        IReadOnlyList<Guid> affectedIds)
    {
        var nextState = InterlockingRuntimeState.Create(
            state.Revision + 1,
            changes.Turnouts?.Values ?? state.Turnouts.Values,
            changes.Blocks?.Values ?? state.Blocks.Values,
            changes.Signals?.Values ?? state.Signals.Values,
            changes.Routes?.Values ?? state.Routes.Values,
            state.ProcessedCorrelationIds.Append(correlationId));
        return new InterlockingDecision(InterlockingDecisionStatus.Accepted, code, message, affectedIds, nextState);
    }

    private static InterlockingDecision Reject(
        InterlockingRuntimeState state,
        string code,
        string message,
        IReadOnlyList<Guid> affectedIds) =>
        new(InterlockingDecisionStatus.Rejected, code, message, affectedIds, state);

    private sealed record RouteResources(
        Guid RouteId,
        IReadOnlyList<Guid> BlockIds,
        IReadOnlyDictionary<Guid, SignalAspect> SignalRequirements,
        IReadOnlyDictionary<Guid, TurnoutPosition> TurnoutRequirements);

    private sealed record RuntimeStateChanges(
        IReadOnlyDictionary<Guid, TurnoutRuntimeState>? Turnouts = null,
        IReadOnlyDictionary<Guid, BlockRuntimeState>? Blocks = null,
        IReadOnlyDictionary<Guid, SignalRuntimeState>? Signals = null,
        IReadOnlyDictionary<Guid, RouteRuntimeState>? Routes = null);
}