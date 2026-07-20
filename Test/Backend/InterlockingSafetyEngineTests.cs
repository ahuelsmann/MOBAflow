// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service.Interlocking;
using global::Moba.Domain;

internal sealed class InterlockingSafetyEngineTests
{
    [Test]
    public void ConflictAnalyzer_DerivesSymmetricConflictsFromEverySafetyResource()
    {
        var ids = new FixtureIds();
        var first = CreateRoute(ids.Route1, ids.Signal1, ids.Block1, ids.Turnout1, TurnoutPosition.Straight);
        var second = CreateRoute(ids.Route2, ids.Signal1, ids.Block1, ids.Turnout1, TurnoutPosition.DivergingLeft);
        second.ConflictingRouteIds.Add(first.Id);
        var definition = CreateDefinition(ids, first, second);

        var matrix = InterlockingConflictAnalyzer.Analyze(definition);

        var conflict = matrix.GetConflict(first.Id, second.Id);
        Assert.Multiple(() =>
        {
            Assert.That(matrix.AreConflicting(second.Id, first.Id), Is.True);
            Assert.That(conflict, Is.Not.Null);
            Assert.That(conflict!.Reasons, Is.SupersetOf(new[]
            {
                RouteConflictReason.Explicit,
                RouteConflictReason.SharedBlock,
                RouteConflictReason.SharedSignal,
                RouteConflictReason.SharedPath,
                RouteConflictReason.IncompatibleTurnoutPosition
            }));
            Assert.That(conflict.RelatedResourceIds, Does.Contain(ids.Block1));
            Assert.That(conflict.RelatedResourceIds, Does.Contain(ids.Turnout1));
        });
    }

    [Test]
    public void ReserveRoute_UnknownBlock_RejectsWithoutPartialReservation()
    {
        var (engine, ids) = CreateSingleRouteEngine();

        var result = engine.ReserveRoute(engine.InitialState, ids.Route1, Guid.NewGuid(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InterlockingDecisionStatus.Rejected));
            Assert.That(result.Code, Is.EqualTo("route.block.unsafe"));
            Assert.That(result.State.Revision, Is.Zero);
            Assert.That(result.State.Blocks[ids.Block1].ReservationOwnerRouteId, Is.Null);
            Assert.That(result.State.Turnouts[ids.Turnout1].LockOwnerRouteId, Is.Null);
        });
    }

    [Test]
    public void ReserveRoute_FreeResources_ReservesBlocksTurnoutsAndSignalsAtomically()
    {
        var (engine, ids) = CreateSingleRouteEngine();
        var free = ObserveBlock(engine, engine.InitialState, ids.Block1, BlockOccupancy.Free);

        var result = engine.ReserveRoute(free, ids.Route1, Guid.NewGuid(), free.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.State.Routes[ids.Route1].Lifecycle, Is.EqualTo(RouteLifecycle.Selected));
            Assert.That(result.State.Blocks[ids.Block1].ReservationOwnerRouteId, Is.EqualTo(ids.Route1));
            Assert.That(result.State.Turnouts[ids.Turnout1].LockOwnerRouteId, Is.EqualTo(ids.Route1));
            Assert.That(result.State.Signals[ids.Signal1].LockOwnerRouteId, Is.EqualTo(ids.Route1));
        });
    }

    [Test]
    public void ReserveRoute_ConflictingRouteAlreadyReserved_RejectsSecondRoute()
    {
        var ids = new FixtureIds();
        var first = CreateRoute(ids.Route1, ids.Signal1, ids.Block1, ids.Turnout1, TurnoutPosition.Straight);
        var second = CreateRoute(ids.Route2, ids.Signal2, ids.Block1, ids.Turnout1, TurnoutPosition.DivergingLeft);
        var engine = new InterlockingSafetyEngine(CreateDefinition(ids, first, second));
        var state = ObserveBlock(engine, engine.InitialState, ids.Block1, BlockOccupancy.Free);
        state = engine.ReserveRoute(state, ids.Route1, Guid.NewGuid(), state.Revision).State;

        var rejected = engine.ReserveRoute(state, ids.Route2, Guid.NewGuid(), state.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Code, Is.EqualTo("route.conflict"));
            Assert.That(rejected.State, Is.SameAs(state));
            Assert.That(rejected.State.Routes[ids.Route2].Lifecycle, Is.EqualTo(RouteLifecycle.Available));
        });
    }

    [Test]
    public void EstablishRoute_RequiresConfirmedTurnoutPosition()
    {
        var (engine, ids) = CreateSingleRouteEngine();
        var state = PrepareSettingRoute(engine, ids);

        var rejected = engine.EstablishRoute(state, ids.Route1, Guid.NewGuid(), state.Revision);
        state = engine.ObserveTurnout(
            state,
            ids.Turnout1,
            TurnoutPosition.Straight,
            false,
            Guid.NewGuid(),
            state.Revision).State;
        var accepted = engine.EstablishRoute(state, ids.Route1, Guid.NewGuid(), state.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Code, Is.EqualTo("route.turnout.unconfirmed"));
            Assert.That(rejected.State.Revision, Is.EqualTo(state.Revision - 1));
            Assert.That(accepted.IsAccepted, Is.True);
            Assert.That(accepted.State.Routes[ids.Route1].Lifecycle, Is.EqualTo(RouteLifecycle.Established));
        });
    }

    [Test]
    public void ObserveBlock_OccupiedAfterEstablishment_TransitionsRouteToOccupied()
    {
        var (engine, ids) = CreateSingleRouteEngine();
        var state = EstablishRoute(engine, ids);

        var observed = engine.ObserveBlock(
            state,
            ids.Block1,
            BlockOccupancy.Occupied,
            Guid.NewGuid(),
            state.Revision);

        Assert.That(observed.State.Routes[ids.Route1].Lifecycle, Is.EqualTo(RouteLifecycle.Occupied));
    }

    [Test]
    public void ObserveBlock_UnsafeWhileSetting_FailsRouteAndRetainsLocks()
    {
        var (engine, ids) = CreateSingleRouteEngine();
        var state = PrepareSettingRoute(engine, ids);

        var observed = engine.ObserveBlock(
            state,
            ids.Block1,
            BlockOccupancy.Unknown,
            Guid.NewGuid(),
            state.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(observed.State.Routes[ids.Route1].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(observed.State.Blocks[ids.Block1].ReservationOwnerRouteId, Is.EqualTo(ids.Route1));
            Assert.That(observed.State.Turnouts[ids.Turnout1].LockOwnerRouteId, Is.EqualTo(ids.Route1));
        });
    }

    [Test]
    public void CancelRoute_AfterSettingStarted_FailsAndRetainsLocksForReconciliation()
    {
        var (engine, ids) = CreateSingleRouteEngine();
        var state = PrepareSettingRoute(engine, ids);

        var cancelled = engine.CancelRoute(state, ids.Route1, Guid.NewGuid(), state.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Code, Is.EqualTo("route.cancel.reconciliation"));
            Assert.That(cancelled.State.Routes[ids.Route1].Lifecycle, Is.EqualTo(RouteLifecycle.Failed));
            Assert.That(cancelled.State.Blocks[ids.Block1].ReservationOwnerRouteId, Is.EqualTo(ids.Route1));
            Assert.That(cancelled.State.Signals[ids.Signal1].LockOwnerRouteId, Is.EqualTo(ids.Route1));
        });
    }

    [Test]
    public void FullRouteRelease_OnlyUnlocksAfterExplicitFreeObservation()
    {
        var (engine, ids) = CreateSingleRouteEngine();
        var state = EstablishRoute(engine, ids);
        state = engine.ObserveBlock(state, ids.Block1, BlockOccupancy.Occupied, Guid.NewGuid(), state.Revision).State;

        var rejected = engine.BeginRelease(state, ids.Route1, Guid.NewGuid(), state.Revision);
        state = engine.ObserveBlock(state, ids.Block1, BlockOccupancy.Free, Guid.NewGuid(), state.Revision).State;
        state = engine.BeginRelease(state, ids.Route1, Guid.NewGuid(), state.Revision).State;
        var released = engine.CompleteRelease(state, ids.Route1, Guid.NewGuid(), state.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Code, Is.EqualTo("route.release.block-unsafe"));
            Assert.That(released.State.Routes[ids.Route1].Lifecycle, Is.EqualTo(RouteLifecycle.Available));
            Assert.That(released.State.Blocks[ids.Block1].ReservationOwnerRouteId, Is.Null);
            Assert.That(released.State.Turnouts[ids.Turnout1].LockOwnerRouteId, Is.Null);
            Assert.That(released.State.Signals[ids.Signal1].LockOwnerRouteId, Is.Null);
        });
    }

    [Test]
    public void RepeatedAndStaleInputs_DoNotAdvanceOrRegressRevision()
    {
        var (engine, ids) = CreateSingleRouteEngine();
        var correlationId = Guid.NewGuid();
        var first = engine.ObserveBlock(engine.InitialState, ids.Block1, BlockOccupancy.Free, correlationId, 0);

        var duplicate = engine.ObserveBlock(first.State, ids.Block1, BlockOccupancy.Occupied, correlationId, first.State.Revision);
        var stale = engine.ObserveBlock(first.State, ids.Block1, BlockOccupancy.Occupied, Guid.NewGuid(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Status, Is.EqualTo(InterlockingDecisionStatus.IgnoredDuplicate));
            Assert.That(stale.Status, Is.EqualTo(InterlockingDecisionStatus.IgnoredStale));
            Assert.That(duplicate.State, Is.SameAs(first.State));
            Assert.That(stale.State.Blocks[ids.Block1].Occupancy, Is.EqualTo(BlockOccupancy.Free));
            Assert.That(stale.State.Revision, Is.EqualTo(1));
        });
    }

    [Test]
    public void SameInputSequence_ProducesEquivalentState()
    {
        var firstFixture = CreateSingleRouteEngine();
        var secondFixture = CreateSingleRouteEngine();
        var observeCorrelation = Guid.Parse("00000000-0000-0000-0000-000000000091");
        var reserveCorrelation = Guid.Parse("00000000-0000-0000-0000-000000000092");

        var firstState = firstFixture.Engine.ObserveBlock(
            firstFixture.Engine.InitialState,
            firstFixture.Ids.Block1,
            BlockOccupancy.Free,
            observeCorrelation,
            0).State;
        firstState = firstFixture.Engine.ReserveRoute(
            firstState,
            firstFixture.Ids.Route1,
            reserveCorrelation,
            firstState.Revision).State;

        var secondState = secondFixture.Engine.ObserveBlock(
            secondFixture.Engine.InitialState,
            secondFixture.Ids.Block1,
            BlockOccupancy.Free,
            observeCorrelation,
            0).State;
        secondState = secondFixture.Engine.ReserveRoute(
            secondState,
            secondFixture.Ids.Route1,
            reserveCorrelation,
            secondState.Revision).State;

        Assert.Multiple(() =>
        {
            Assert.That(secondState.Revision, Is.EqualTo(firstState.Revision));
            Assert.That(secondState.Turnouts.OrderBy(item => item.Key), Is.EqualTo(firstState.Turnouts.OrderBy(item => item.Key)));
            Assert.That(secondState.Blocks.OrderBy(item => item.Key), Is.EqualTo(firstState.Blocks.OrderBy(item => item.Key)));
            Assert.That(secondState.Signals.OrderBy(item => item.Key), Is.EqualTo(firstState.Signals.OrderBy(item => item.Key)));
            Assert.That(secondState.Routes.OrderBy(item => item.Key), Is.EqualTo(firstState.Routes.OrderBy(item => item.Key)));
            Assert.That(secondState.ProcessedCorrelationIds.Order(), Is.EqualTo(firstState.ProcessedCorrelationIds.Order()));
        });
    }

    private static (InterlockingSafetyEngine Engine, FixtureIds Ids) CreateSingleRouteEngine()
    {
        var ids = new FixtureIds();
        var route = CreateRoute(ids.Route1, ids.Signal1, ids.Block1, ids.Turnout1, TurnoutPosition.Straight);
        return (new InterlockingSafetyEngine(CreateDefinition(ids, route)), ids);
    }

    private static InterlockingRuntimeState PrepareSettingRoute(InterlockingSafetyEngine engine, FixtureIds ids)
    {
        var state = ObserveBlock(engine, engine.InitialState, ids.Block1, BlockOccupancy.Free);
        state = engine.ReserveRoute(state, ids.Route1, Guid.NewGuid(), state.Revision).State;
        return engine.BeginSetting(state, ids.Route1, Guid.NewGuid(), state.Revision).State;
    }

    private static InterlockingRuntimeState EstablishRoute(InterlockingSafetyEngine engine, FixtureIds ids)
    {
        var state = PrepareSettingRoute(engine, ids);
        state = engine.ObserveTurnout(
            state,
            ids.Turnout1,
            TurnoutPosition.Straight,
            false,
            Guid.NewGuid(),
            state.Revision).State;
        return engine.EstablishRoute(state, ids.Route1, Guid.NewGuid(), state.Revision).State;
    }

    private static InterlockingRuntimeState ObserveBlock(
        InterlockingSafetyEngine engine,
        InterlockingRuntimeState state,
        Guid blockId,
        BlockOccupancy occupancy) =>
        engine.ObserveBlock(state, blockId, occupancy, Guid.NewGuid(), state.Revision).State;

    private static InterlockingDefinition CreateDefinition(FixtureIds ids, params RouteDefinition[] routes) =>
        new()
        {
            Turnouts = [new TurnoutDefinition { Id = ids.Turnout1, Name = "W1", DecoderAddress = 1 }],
            Signals =
            [
                new SignalDefinition { Id = ids.Signal1, Name = "N1", BaseAddress = 10 },
                new SignalDefinition { Id = ids.Signal2, Name = "N2", BaseAddress = 20 }
            ],
            Blocks =
            [
                new BlockDefinition { Id = ids.Block1, Name = "B1" },
                new BlockDefinition { Id = ids.Block2, Name = "B2" }
            ],
            Routes = routes.ToList()
        };

    private static RouteDefinition CreateRoute(
        Guid routeId,
        Guid signalId,
        Guid blockId,
        Guid turnoutId,
        TurnoutPosition position) =>
        new()
        {
            Id = routeId,
            Name = routeId.ToString("N"),
            EntryElementId = signalId,
            ExitElementId = blockId,
            PathElementIds = [turnoutId],
            ProtectedBlockIds = [blockId],
            ProtectedSignalIds = [signalId],
            TurnoutRequirements = [new RouteTurnoutRequirement { TurnoutId = turnoutId, Position = position }]
        };

    private sealed class FixtureIds
    {
        public Guid Turnout1 { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public Guid Signal1 { get; } = Guid.Parse("00000000-0000-0000-0000-000000000002");
        public Guid Signal2 { get; } = Guid.Parse("00000000-0000-0000-0000-000000000003");
        public Guid Block1 { get; } = Guid.Parse("00000000-0000-0000-0000-000000000004");
        public Guid Block2 { get; } = Guid.Parse("00000000-0000-0000-0000-000000000005");
        public Guid Route1 { get; } = Guid.Parse("00000000-0000-0000-0000-000000000006");
        public Guid Route2 { get; } = Guid.Parse("00000000-0000-0000-0000-000000000007");
    }
}
