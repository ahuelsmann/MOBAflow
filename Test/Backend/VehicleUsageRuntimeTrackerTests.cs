// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Service;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.Domain.Enum;

[TestFixture]
internal sealed class VehicleUsageRuntimeTrackerTests
{
    [Test]
    public void Checkpoint_AttributesPoweredLocomotivesAndWagonsOnce()
    {
        var time = new ManualTimeProvider();
        var store = new MemoryCheckpointStore();
        var (runtimeProject, editorProject, train, firstLocomotive, secondLocomotive, wagon) = CreateProjects();
        var tracker = new VehicleUsageRuntimeTracker(time, store);

        tracker.Activate(runtimeProject);
        Assert.That(tracker.SetActiveTrain(train.Id), Is.True);
        tracker.UpdateRuntimeState(
            isConnected: true,
            isTrackPowerOn: true,
            isEmergencyStopActive: false,
            isShortCircuitActive: false,
            isProgrammingModeActive: false,
            new Dictionary<int, LocomotiveRuntimeSnapshot>
            {
                [3] = new() { Address = 3, Speed = 20 },
                [4] = new() { Address = 4, Speed = 10 }
            });

        time.Advance(TimeSpan.FromSeconds(12));
        Assert.That(tracker.Checkpoint(), Is.True);

        var usage = tracker.GetSnapshot().Usage;
        Assert.Multiple(() =>
        {
            Assert.That(usage[firstLocomotive.Id].TrackedOperatingSeconds, Is.EqualTo(12));
            Assert.That(usage[secondLocomotive.Id].TrackedOperatingSeconds, Is.EqualTo(12));
            Assert.That(usage[wagon.Id].TrackedOperatingSeconds, Is.EqualTo(12),
                "A repeated consist entry must not double count the wagon.");
            Assert.That(runtimeProject.PassengerWagons.Single().Usage!.TrackedOperatingSeconds, Is.EqualTo(12));
        });
    }

    [Test]
    public void SafetyStopAndReconnect_SettleOnceWithoutCountingDowntime()
    {
        var time = new ManualTimeProvider();
        var (runtimeProject, editorProject, train, firstLocomotive, _, wagon) = CreateProjects();
        var tracker = new VehicleUsageRuntimeTracker(time, new MemoryCheckpointStore());
        tracker.Activate(runtimeProject);
        tracker.SetActiveTrain(train.Id);
        var moving = new Dictionary<int, LocomotiveRuntimeSnapshot>
        {
            [3] = new() { Address = 3, Speed = 15 }
        };

        tracker.UpdateRuntimeState(true, true, false, false, false, moving);
        time.Advance(TimeSpan.FromSeconds(5));
        tracker.UpdateRuntimeState(true, true, true, false, false, moving);
        tracker.UpdateRuntimeState(true, true, true, false, false, moving);
        time.Advance(TimeSpan.FromSeconds(10));
        tracker.UpdateRuntimeState(false, false, false, false, false, moving);
        time.Advance(TimeSpan.FromSeconds(10));
        tracker.UpdateRuntimeState(true, true, false, false, false, moving);
        time.Advance(TimeSpan.FromSeconds(7));
        tracker.Checkpoint();

        var usage = tracker.GetSnapshot().Usage;
        Assert.Multiple(() =>
        {
            Assert.That(usage[firstLocomotive.Id].TrackedOperatingSeconds, Is.EqualTo(12));
            Assert.That(usage[wagon.Id].TrackedOperatingSeconds, Is.EqualTo(12));
        });
    }

    [Test]
    public void ChangingActiveTrain_SettlesOldConsistBeforeSwitch()
    {
        var time = new ManualTimeProvider();
        var (runtimeProject, editorProject, firstTrain, firstLocomotive, _, firstWagon) = CreateProjects();
        var secondWagon = new GoodsWagon { Id = Guid.NewGuid(), Usage = new VehicleUsageData() };
        runtimeProject.GoodsWagons.Add(secondWagon);
        editorProject.GoodsWagons.Add(new GoodsWagon { Id = secondWagon.Id, Usage = new VehicleUsageData() });
        var secondTrain = new Train
        {
            Vehicles =
            [
                new Vehicle { VehicleId = firstLocomotive.Id, VehicleKind = TrainVehicleKind.Locomotive },
                new Vehicle { VehicleId = secondWagon.Id, VehicleKind = TrainVehicleKind.GoodsWagon }
            ]
        };
        runtimeProject.Trains.Add(secondTrain);
        editorProject.Trains.Add(new Train { Id = secondTrain.Id });
        var tracker = new VehicleUsageRuntimeTracker(time, new MemoryCheckpointStore());
        tracker.Activate(runtimeProject);
        tracker.SetActiveTrain(firstTrain.Id);
        tracker.UpdateRuntimeState(
            true,
            true,
            false,
            false,
            false,
            new Dictionary<int, LocomotiveRuntimeSnapshot> { [3] = new() { Address = 3, Speed = 10 } });

        time.Advance(TimeSpan.FromSeconds(4));
        tracker.SetActiveTrain(secondTrain.Id);
        time.Advance(TimeSpan.FromSeconds(6));
        tracker.Checkpoint();

        var usage = tracker.GetSnapshot().Usage;
        Assert.Multiple(() =>
        {
            Assert.That(usage[firstLocomotive.Id].TrackedOperatingSeconds, Is.EqualTo(10));
            Assert.That(usage[firstWagon.Id].TrackedOperatingSeconds, Is.EqualTo(4));
            Assert.That(usage[secondWagon.Id].TrackedOperatingSeconds, Is.EqualTo(6));
        });
    }

    [Test]
    public void JourneyCompletion_IsIdempotentByRunIdentity()
    {
        var time = new ManualTimeProvider();
        var (runtimeProject, editorProject, train, firstLocomotive, secondLocomotive, wagon) = CreateProjects();
        var tracker = new VehicleUsageRuntimeTracker(time, new MemoryCheckpointStore());
        var runId = Guid.NewGuid();
        tracker.Activate(runtimeProject);
        tracker.SetActiveTrain(train.Id);

        Assert.That(tracker.RecordJourneyCompleted(runId), Is.True);
        Assert.That(tracker.RecordJourneyCompleted(runId), Is.False);

        var snapshot = tracker.GetSnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Usage[firstLocomotive.Id].TrackedCompletedTrips, Is.EqualTo(1));
            Assert.That(snapshot.Usage[secondLocomotive.Id].TrackedCompletedTrips, Is.EqualTo(1));
            Assert.That(snapshot.Usage[wagon.Id].TrackedCompletedTrips, Is.EqualTo(1));
            Assert.That(snapshot.Diagnostics.DuplicateJourneyCompletions, Is.EqualTo(1));
        });
    }

    [Test]
    public void Activate_RecoversAbsoluteCheckpointWithoutDoubleCounting()
    {
        var time = new ManualTimeProvider();
        var store = new MemoryCheckpointStore();
        var (firstRuntime, firstEditor, train, firstLocomotive, _, wagon) = CreateProjects();
        var firstTracker = new VehicleUsageRuntimeTracker(time, store);
        var completedRunId = Guid.NewGuid();
        firstTracker.Activate(firstRuntime);
        firstTracker.SetActiveTrain(train.Id);
        firstTracker.RecordJourneyCompleted(completedRunId);
        firstTracker.UpdateRuntimeState(
            true,
            true,
            false,
            false,
            false,
            new Dictionary<int, LocomotiveRuntimeSnapshot> { [3] = new() { Address = 3, Speed = 10 } });
        time.Advance(TimeSpan.FromSeconds(9));
        firstTracker.Checkpoint();

        var (recoveredRuntime, recoveredEditor, recoveredTrain, _, _, _) = CreateProjects(
            firstRuntime.Id,
            train.Id,
            firstLocomotive.Id,
            wagon.Id);
        var recoveredTracker = new VehicleUsageRuntimeTracker(time, store);
        recoveredTracker.Activate(recoveredRuntime);
        recoveredTracker.SetActiveTrain(recoveredTrain.Id);
        Assert.That(recoveredTracker.RecordJourneyCompleted(completedRunId), Is.False);
        recoveredTracker.Checkpoint();

        var usage = recoveredTracker.GetSnapshot().Usage;
        Assert.Multiple(() =>
        {
            Assert.That(usage[firstLocomotive.Id].TrackedOperatingSeconds, Is.EqualTo(9));
            Assert.That(usage[wagon.Id].TrackedOperatingSeconds, Is.EqualTo(9));
            Assert.That(usage[firstLocomotive.Id].TrackedCompletedTrips, Is.EqualTo(1));
            Assert.That(usage[wagon.Id].TrackedCompletedTrips, Is.EqualTo(1));
            Assert.That(recoveredTracker.GetSnapshot().Diagnostics.RecoveredVehicles, Is.EqualTo(2));
            Assert.That(recoveredTracker.GetSnapshot().Diagnostics.RejectedUpdates, Is.EqualTo(1),
                "A checkpoint for a deleted vehicle must be ignored without resurrecting it.");
            Assert.That(recoveredTracker.GetSnapshot().Diagnostics.DuplicateJourneyCompletions, Is.EqualTo(1));
        });
    }

    private static (Project RuntimeProject,
        Project EditorProject,
        Train Train,
        Locomotive FirstLocomotive,
        Locomotive SecondLocomotive,
        PassengerWagon Wagon) CreateProjects(
        Guid? projectId = null,
        Guid? trainId = null,
        Guid? firstLocomotiveId = null,
        Guid? wagonId = null)
    {
        var resolvedProjectId = projectId ?? Guid.NewGuid();
        var firstLocomotive = new Locomotive
        {
            Id = firstLocomotiveId ?? Guid.NewGuid(),
            DigitalAddress = 3,
            Usage = new VehicleUsageData()
        };
        var secondLocomotive = new Locomotive
        {
            Id = Guid.NewGuid(),
            DigitalAddress = 4,
            Usage = new VehicleUsageData()
        };
        var wagon = new PassengerWagon
        {
            Id = wagonId ?? Guid.NewGuid(),
            Usage = new VehicleUsageData()
        };
        var train = new Train
        {
            Id = trainId ?? Guid.NewGuid(),
            Vehicles =
            [
                new Vehicle { VehicleId = firstLocomotive.Id, VehicleKind = TrainVehicleKind.Locomotive },
                new Vehicle { VehicleId = secondLocomotive.Id, VehicleKind = TrainVehicleKind.Locomotive },
                new Vehicle { VehicleId = wagon.Id, VehicleKind = TrainVehicleKind.PassengerWagon },
                new Vehicle { VehicleId = wagon.Id, VehicleKind = TrainVehicleKind.PassengerWagon }
            ]
        };
        var runtimeProject = new Project
        {
            Id = resolvedProjectId,
            Locomotives = [firstLocomotive, secondLocomotive],
            PassengerWagons = [wagon],
            Trains = [train]
        };
        var editorProject = new Project
        {
            Id = resolvedProjectId,
            Locomotives =
            [
                new Locomotive { Id = firstLocomotive.Id, DigitalAddress = 3, Usage = new VehicleUsageData() },
                new Locomotive { Id = secondLocomotive.Id, DigitalAddress = 4, Usage = new VehicleUsageData() }
            ],
            PassengerWagons = [new PassengerWagon { Id = wagon.Id, Usage = new VehicleUsageData() }],
            Trains = [new Train { Id = train.Id }]
        };
        return (runtimeProject, editorProject, train, firstLocomotive, secondLocomotive, wagon);
    }

    private sealed class MemoryCheckpointStore : IVehicleUsageCheckpointStore
    {
        private readonly Dictionary<Guid, VehicleUsageCheckpointState> _states = [];

        public VehicleUsageCheckpointState? Load(Guid projectId) => _states.GetValueOrDefault(projectId);

        public void Save(VehicleUsageCheckpointState state) => _states[state.ProjectId] = state;
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;
        private DateTimeOffset _utcNow = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan elapsed)
        {
            _timestamp += elapsed.Ticks;
            _utcNow += elapsed;
        }
    }
}
