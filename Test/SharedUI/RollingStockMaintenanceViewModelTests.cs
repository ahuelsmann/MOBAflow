// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Common.Events;
using global::Moba.Common.Runtime;
using global::Moba.Domain;
using global::Moba.Domain.Enum;
using global::Moba.SharedUI.ViewModel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

internal sealed class RollingStockMaintenanceViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void SetContext_CombinesAuthoritativeRuntimeWithPersistedCorrections()
    {
        var locomotive = new Locomotive
        {
            Name = "Runtime locomotive",
            Usage = new VehicleUsageData
            {
                Corrections =
                [
                    new VehicleUsageCorrection
                    {
                        RecordedAt = Now.AddDays(-1),
                        OperatingSecondsDelta = 1800,
                        CompletedTripsDelta = 1,
                        Reason = "Imported workshop record"
                    }
                ]
            }
        };
        var snapshot = Snapshot(locomotive.Id, TrainVehicleKind.Locomotive, 7200, 3, isOperating: true);
        var viewModel = CreateViewModel(snapshot);
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });

        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OperatingTimeText, Is.EqualTo("2 h 30 min"));
            Assert.That(viewModel.CompletedTripsText, Is.EqualTo("4"));
            Assert.That(viewModel.IsOperating, Is.True);
            Assert.That(viewModel.OperatingStatusText, Is.EqualTo("Operating now"));
            Assert.That(viewModel.History.Single().Details, Does.Contain("Imported workshop record"));
        });
    }

    [Test]
    public async Task RecordCorrectionCommand_RequiresReasonAndAppendsAuditHistory()
    {
        var locomotive = new Locomotive
        {
            Usage = new VehicleUsageData { TrackedOperatingSeconds = 3600 }
        };
        var viewModel = CreateViewModel();
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });
        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());
        viewModel.CorrectionOperatingHours = 1.5;
        viewModel.CorrectionCompletedTrips = 2;

        await viewModel.RecordCorrectionCommand.ExecuteAsync(null);

        Assert.That(locomotive.Usage.Corrections, Is.Empty);
        Assert.That(viewModel.OperationStatus, Is.EqualTo("Enter a reason for the correction."));

        viewModel.CorrectionReason = "Corrected from service log";
        await viewModel.RecordCorrectionCommand.ExecuteAsync(null);

        var totals = new VehicleUsageService().CalculateTotals(locomotive.Usage);
        Assert.Multiple(() =>
        {
            Assert.That(locomotive.Usage.Corrections, Has.Count.EqualTo(1));
            Assert.That(locomotive.Usage.Corrections[0].OperatingSecondsDelta, Is.EqualTo(5400));
            Assert.That(locomotive.Usage.Corrections[0].CompletedTripsDelta, Is.EqualTo(2));
            Assert.That(totals.OperatingSeconds, Is.EqualTo(9000));
            Assert.That(totals.CompletedTrips, Is.EqualTo(2));
            Assert.That(viewModel.History.Single().Details, Does.Contain("Corrected from service log"));
        });
    }

    [Test]
    public async Task RecordCorrectionCommand_ValidatesAgainstLatestAuthoritativeRuntimeTotals()
    {
        var locomotive = new Locomotive { Usage = new VehicleUsageData() };
        var viewModel = CreateViewModel(Snapshot(
            locomotive.Id,
            TrainVehicleKind.Locomotive,
            operatingSeconds: 3600,
            completedTrips: 0,
            isOperating: false));
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });
        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());
        viewModel.CorrectionOperatingHours = -0.5;
        viewModel.CorrectionReason = "Remove duplicated import";

        await viewModel.RecordCorrectionCommand.ExecuteAsync(null);

        var totals = new VehicleUsageService().CalculateTotals(locomotive.Usage);
        Assert.Multiple(() =>
        {
            Assert.That(locomotive.Usage.TrackedOperatingSeconds, Is.EqualTo(3600));
            Assert.That(locomotive.Usage.Corrections.Single().OperatingSecondsDelta, Is.EqualTo(-1800));
            Assert.That(totals.OperatingSeconds, Is.EqualTo(1800));
        });
    }

    [Test]
    public async Task CompleteSelectedPlanCommand_UpdatesBaselineWithoutChangingLifetimeTotals()
    {
        var plan = new VehicleMaintenancePlan
        {
            Name = "Wheel cleaning",
            IntervalOperatingSeconds = 3600,
            OperatingSecondsAtLastCompletion = 0
        };
        var locomotive = new Locomotive
        {
            Usage = new VehicleUsageData { TrackedOperatingSeconds = 3600, TrackedCompletedTrips = 4 },
            Maintenance = new VehicleMaintenanceData { Plans = [plan] }
        };
        var viewModel = CreateViewModel();
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });
        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());
        viewModel.SelectedPlan = viewModel.MaintenancePlans.Single();

        await viewModel.CompleteSelectedPlanCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(plan.OperatingSecondsAtLastCompletion, Is.EqualTo(3600));
            Assert.That(locomotive.Usage.TrackedOperatingSeconds, Is.EqualTo(3600));
            Assert.That(locomotive.Maintenance.Entries, Has.Count.EqualTo(1));
            Assert.That(locomotive.Maintenance.Entries[0].OperatingSecondsAtService, Is.EqualTo(3600));
            Assert.That(viewModel.NextTaskStatusText, Does.Contain("3600").Or.Contain("1 h"));
        });
    }

    [Test]
    public void MaintenanceFilter_AppliesConsistentlyToAllRollingStockKinds()
    {
        var dueSoonLocomotive = VehicleWithPlan<Locomotive>("Due soon locomotive", 1800, 3600);
        var overdueLocomotive = VehicleWithPlan<Locomotive>("Overdue locomotive", 7200, 3600);
        var dueSoonPassenger = VehicleWithPlan<PassengerWagon>("Due soon passenger wagon", 1800, 3600);
        var overduePassenger = VehicleWithPlan<PassengerWagon>("Overdue passenger wagon", 7200, 3600);
        var dueSoonGoods = VehicleWithPlan<GoodsWagon>("Due soon goods wagon", 1800, 3600);
        var overdueGoods = VehicleWithPlan<GoodsWagon>("Overdue goods wagon", 7200, 3600);
        var project = new ProjectViewModel(new Project
        {
            Locomotives = [dueSoonLocomotive, overdueLocomotive],
            PassengerWagons = [dueSoonPassenger, overduePassenger],
            GoodsWagons = [dueSoonGoods, overdueGoods]
        });
        var viewModel = CreateViewModel();
        viewModel.SelectedFilter = viewModel.FilterOptions.Single(option => option.Value == MaintenanceFleetFilter.DueSoon);

        viewModel.SetContext(project, TrainVehicleKind.Locomotive, null);
        Assert.That(viewModel.VisibleLocomotives.Select(item => item.Name), Is.EqualTo(new[] { "Due soon locomotive" }));

        viewModel.SetContext(project, TrainVehicleKind.PassengerWagon, null);
        Assert.That(viewModel.VisiblePassengerWagons.Select(item => item.Name), Is.EqualTo(new[] { "Due soon passenger wagon" }));

        viewModel.SetContext(project, TrainVehicleKind.GoodsWagon, null);
        Assert.That(viewModel.VisibleGoodsWagons.Select(item => item.Name), Is.EqualTo(new[] { "Due soon goods wagon" }));

        viewModel.SelectedFilter = viewModel.FilterOptions.Single(option => option.Value == MaintenanceFleetFilter.Overdue);
        Assert.That(viewModel.VisibleGoodsWagons.Select(item => item.Name), Is.EqualTo(new[] { "Overdue goods wagon" }));
    }

    [Test]
    public void SetContext_DoesNotReenterFleetCollection_WhenSelectionChangesDuringRefresh()
    {
        var firstLocomotive = new Locomotive { Name = "First locomotive" };
        var secondLocomotive = new Locomotive { Name = "Second locomotive" };
        var viewModel = CreateViewModel();
        var project = new ProjectViewModel(new Project
        {
            Locomotives = [firstLocomotive, secondLocomotive]
        });
        viewModel.SetContext(
            project,
            TrainVehicleKind.Locomotive,
            project.Locomotives[0]);

        var selectionChangedDuringCollectionNotification = false;
        viewModel.VisibleLocomotives.CollectionChanged += (_, _) =>
        {
            if (selectionChangedDuringCollectionNotification)
                return;

            selectionChangedDuringCollectionNotification = true;
            viewModel.SetContext(
                project,
                TrainVehicleKind.Locomotive,
                project.Locomotives[1],
                "no match");
        };
        viewModel.VisibleLocomotives.CollectionChanged += (_, _) => { };

        Assert.DoesNotThrow(() => viewModel.SetContext(
            project,
            TrainVehicleKind.Locomotive,
            project.Locomotives[0],
            "no match"));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(selectionChangedDuringCollectionNotification, Is.True);
            Assert.That(viewModel.VisibleLocomotives, Is.Empty);
            Assert.That(viewModel.VehicleName, Is.EqualTo("Second locomotive"));
        }
    }

    [Test]
    public void UsageCheckpoint_PreservesSelectedVehicle_WhenVisibleFleetIsUnchanged()
    {
        var firstLocomotive = new Locomotive { Name = "First locomotive" };
        var secondLocomotive = new Locomotive { Name = "Second locomotive" };
        var staleUsage = Usage(firstLocomotive.Id, secondLocomotive.Id, 60);
        var checkpointUsage = Usage(firstLocomotive.Id, secondLocomotive.Id, 7200);
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(
            new MobaRuntimeSnapshot { VehicleUsage = staleUsage },
            eventBus);
        var project = new ProjectViewModel(new Project
        {
            Locomotives = [firstLocomotive, secondLocomotive]
        });
        viewModel.SetContext(
            project,
            TrainVehicleKind.Locomotive,
            project.Locomotives[0]);
        viewModel.Activate();

        var selectionClearedDuringCollectionNotification = false;
        viewModel.VisibleLocomotives.CollectionChanged += (_, _) =>
        {
            if (selectionClearedDuringCollectionNotification)
                return;

            selectionClearedDuringCollectionNotification = true;
            viewModel.SetContext(
                project,
                TrainVehicleKind.Locomotive,
                null);
        };

        Assert.DoesNotThrow(() => eventBus.Publish(new VehicleUsageCheckpointCommittedEvent(
            project.Model.Id,
            Now,
            checkpointUsage)));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(selectionClearedDuringCollectionNotification, Is.False);
            Assert.That(viewModel.VehicleName, Is.EqualTo("First locomotive"));
            Assert.That(viewModel.OperatingTimeText, Is.EqualTo("2 h 00 min"));
        }
    }

    [Test]
    public void RuntimeSnapshotSubscription_RefreshesWhileActiveAndStopsAfterDeactivation()
    {
        var locomotive = new Locomotive();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var viewModel = CreateViewModel(eventBus: eventBus);
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });
        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());
        viewModel.Activate();

        eventBus.Publish(new RuntimeSnapshotChangedEvent(
            Snapshot(locomotive.Id, TrainVehicleKind.Locomotive, 3600, 1, isOperating: true)));

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OperatingTimeText, Is.EqualTo("1 h 00 min"));
            Assert.That(viewModel.IsOperating, Is.True);
            Assert.That(eventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.EqualTo(1));
        });

        viewModel.Deactivate();
        eventBus.Publish(new RuntimeSnapshotChangedEvent(
            Snapshot(locomotive.Id, TrainVehicleKind.Locomotive, 7200, 2, isOperating: false)));

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.OperatingTimeText, Is.EqualTo("1 h 00 min"));
            Assert.That(viewModel.IsOperating, Is.True);
            Assert.That(eventBus.GetSubscriberCount<RuntimeSnapshotChangedEvent>(), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task AddMaintenancePlanCommand_CreatesCustomCombinedPlanFromCurrentUsageBaseline()
    {
        var locomotive = new Locomotive { Usage = new VehicleUsageData { TrackedOperatingSeconds = 7200, TrackedCompletedTrips = 8 } };
        var viewModel = CreateViewModel();
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });
        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());
        viewModel.NewPlanName = "Coupler inspection";
        viewModel.NewPlanIntervalDays = 30;
        viewModel.NewPlanOperatingHours = 25;
        viewModel.NewPlanCompletedTrips = 50;

        await viewModel.AddMaintenancePlanCommand.ExecuteAsync(null);

        var plan = locomotive.Maintenance!.Plans.Single();
        Assert.Multiple(() =>
        {
            Assert.That(plan.Name, Is.EqualTo("Coupler inspection"));
            Assert.That(plan.IntervalDays, Is.EqualTo(30));
            Assert.That(plan.IntervalOperatingSeconds, Is.EqualTo(25 * 60 * 60));
            Assert.That(plan.IntervalCompletedTrips, Is.EqualTo(50));
            Assert.That(plan.OperatingSecondsAtLastCompletion, Is.EqualTo(7200));
            Assert.That(plan.CompletedTripsAtLastCompletion, Is.EqualTo(8));
        });
    }

    private static RollingStockMaintenanceViewModel CreateViewModel(
        MobaRuntimeSnapshot? snapshot = null,
        IEventBus? eventBus = null)
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(candidate => candidate.Current).Returns(snapshot ?? MobaRuntimeSnapshot.Empty);
        return new RollingStockMaintenanceViewModel(
            new VehicleUsageService(),
            new VehicleMaintenanceService(),
            runtime.Object,
            eventBus ?? new EventBus(NullLogger<EventBus>.Instance),
            timeProvider: new FixedTimeProvider(Now));
    }

    private static MobaRuntimeSnapshot Snapshot(
        Guid vehicleId,
        TrainVehicleKind kind,
        long operatingSeconds,
        long completedTrips,
        bool isOperating)
        => new()
        {
            VehicleUsage = new Dictionary<Guid, VehicleUsageRuntimeSnapshot>
            {
                [vehicleId] = new()
                {
                    VehicleId = vehicleId,
                    VehicleKind = kind,
                    TrackedOperatingSeconds = operatingSeconds,
                    TrackedCompletedTrips = completedTrips,
                    IsOperating = isOperating
                }
            }
        };

    private static Dictionary<Guid, VehicleUsageRuntimeSnapshot> Usage(
        Guid firstVehicleId,
        Guid secondVehicleId,
        long operatingSeconds)
        => new()
        {
            [firstVehicleId] = new()
            {
                VehicleId = firstVehicleId,
                VehicleKind = TrainVehicleKind.Locomotive,
                TrackedOperatingSeconds = operatingSeconds,
                TrackedCompletedTrips = 0
            },
            [secondVehicleId] = new()
            {
                VehicleId = secondVehicleId,
                VehicleKind = TrainVehicleKind.Locomotive,
                TrackedOperatingSeconds = operatingSeconds,
                TrackedCompletedTrips = 0
            }
        };

    private static T VehicleWithPlan<T>(string name, long operatingSeconds, long intervalSeconds)
        where T : class, new()
    {
        object vehicle = new T();
        var usage = new VehicleUsageData { TrackedOperatingSeconds = operatingSeconds };
        var maintenance = new VehicleMaintenanceData
        {
            Plans =
            [
                new VehicleMaintenancePlan
                {
                    Name = "Inspection",
                    IntervalOperatingSeconds = intervalSeconds,
                    OperatingSecondsAtLastCompletion = 0
                }
            ]
        };

        switch (vehicle)
        {
            case Locomotive locomotive:
                locomotive.Name = name;
                locomotive.Usage = usage;
                locomotive.Maintenance = maintenance;
                break;
            case Wagon wagon:
                wagon.Name = name;
                wagon.Usage = usage;
                wagon.Maintenance = maintenance;
                break;
        }

        return (T)vehicle;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}