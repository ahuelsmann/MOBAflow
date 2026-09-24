// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Service;
using global::Moba.Domain;
using global::Moba.Domain.Enum;
using global::Moba.SharedUI.ViewModel;

internal sealed class RollingStockMaintenanceViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task CompleteSelectedPlanCommand_UpdatesDateAndHistory()
    {
        var plan = new VehicleMaintenancePlan
        {
            Name = "Wheel cleaning",
            IntervalDays = 30,
            LastCompletedAt = Now.AddDays(-30)
        };
        var locomotive = new Locomotive
        {
            Maintenance = new VehicleMaintenanceData { Plans = [plan] }
        };
        var viewModel = CreateViewModel();
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });
        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());
        viewModel.SelectedPlan = viewModel.MaintenancePlans.Single();

        await viewModel.CompleteSelectedPlanCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(plan.LastCompletedAt, Is.EqualTo(Now));
            Assert.That(locomotive.Maintenance!.Entries.Single().PerformedAt, Is.EqualTo(Now));
            Assert.That(viewModel.MaintenancePlans.Single().DueAt, Is.EqualTo(Now.AddDays(30)));
            Assert.That(viewModel.History.Single().Title, Is.EqualTo("Wheel cleaning"));
        });
    }

    [Test]
    public void MaintenanceFilter_AppliesConsistentlyToAllRollingStockKinds()
    {
        var dueSoonLocomotive = VehicleWithPlan<Locomotive>("Due soon locomotive", 10, 30);
        var overdueLocomotive = VehicleWithPlan<Locomotive>("Overdue locomotive", 31, 30);
        var dueSoonPassenger = VehicleWithPlan<PassengerWagon>("Due soon passenger wagon", 10, 30);
        var overduePassenger = VehicleWithPlan<PassengerWagon>("Overdue passenger wagon", 31, 30);
        var dueSoonGoods = VehicleWithPlan<GoodsWagon>("Due soon goods wagon", 10, 30);
        var overdueGoods = VehicleWithPlan<GoodsWagon>("Overdue goods wagon", 31, 30);
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
    public async Task AddMaintenancePlanCommand_CreatesCalendarPlan()
    {
        var locomotive = new Locomotive();
        var viewModel = CreateViewModel();
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });
        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());
        viewModel.NewPlanName = "Coupler inspection";
        viewModel.NewPlanIntervalDays = 30;

        await viewModel.AddMaintenancePlanCommand.ExecuteAsync(null);

        var plan = locomotive.Maintenance!.Plans.Single();
        Assert.Multiple(() =>
        {
            Assert.That(plan.Name, Is.EqualTo("Coupler inspection"));
            Assert.That(plan.IntervalDays, Is.EqualTo(30));
            Assert.That(plan.LastCompletedAt, Is.EqualTo(Now));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(1.5)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(2147483648d)]
    public async Task AddMaintenancePlanCommand_RejectsInvalidCalendarInterval(double intervalDays)
    {
        var locomotive = new Locomotive();
        var viewModel = CreateViewModel();
        var project = new ProjectViewModel(new Project { Locomotives = [locomotive] });
        viewModel.SetContext(project, TrainVehicleKind.Locomotive, project.Locomotives.Single());
        viewModel.NewPlanIntervalDays = intervalDays;

        await viewModel.AddMaintenancePlanCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(locomotive.Maintenance, Is.Null);
            Assert.That(viewModel.OperationStatus, Is.EqualTo("Enter a positive whole number of days."));
        }
    }

    private static RollingStockMaintenanceViewModel CreateViewModel()
        => new(new VehicleMaintenanceService(), timeProvider: new FixedTimeProvider(Now));

    [TestCase(TrainVehicleKind.Locomotive)]
    [TestCase(TrainVehicleKind.PassengerWagon)]
    [TestCase(TrainVehicleKind.GoodsWagon)]
    public void SetContext_ReflectsVehicleRemovalAndAdditionInSameProject(TrainVehicleKind kind)
    {
        var project = new ProjectViewModel(new Project
        {
            Locomotives = [new Locomotive(), new Locomotive()],
            PassengerWagons = [new PassengerWagon(), new PassengerWagon()],
            GoodsWagons = [new GoodsWagon(), new GoodsWagon()]
        });
        var viewModel = CreateViewModel();
        System.Collections.IList source = kind switch
        {
            TrainVehicleKind.Locomotive => project.Locomotives,
            TrainVehicleKind.PassengerWagon => project.PassengerWagons,
            _ => project.GoodsWagons
        };
        System.Collections.IList visible = kind switch
        {
            TrainVehicleKind.Locomotive => viewModel.VisibleLocomotives,
            TrainVehicleKind.PassengerWagon => viewModel.VisiblePassengerWagons,
            _ => viewModel.VisibleGoodsWagons
        };
        viewModel.SetContext(project, kind, source[0]);
        var removed = source[1];

        source.RemoveAt(1);
        viewModel.SetContext(project, kind, source[0]);

        Assert.That(visible, Is.EqualTo(source));

        source.Add(removed);
        viewModel.SetContext(project, kind, removed);

        Assert.That(visible, Is.EqualTo(source));
    }

    private static T VehicleWithPlan<T>(string name, int elapsedDays, int intervalDays)
        where T : class, new()
    {
        object vehicle = new T();
        var maintenance = new VehicleMaintenanceData
        {
            Plans =
            [
                new VehicleMaintenancePlan
                {
                    Name = "Inspection",
                    IntervalDays = intervalDays,
                    LastCompletedAt = Now.AddDays(-elapsedDays)
                }
            ]
        };

        switch (vehicle)
        {
            case Locomotive locomotive:
                locomotive.Name = name;
                locomotive.Maintenance = maintenance;
                break;
            case Wagon wagon:
                wagon.Name = name;
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
