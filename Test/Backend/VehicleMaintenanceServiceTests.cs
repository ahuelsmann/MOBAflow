// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class VehicleMaintenanceServiceTests
{
    private readonly VehicleMaintenanceService _service = new();
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);

    [TestCase(-1, MaintenanceDueState.Overdue)]
    [TestCase(0, MaintenanceDueState.Due)]
    [TestCase(5, MaintenanceDueState.DueSoon)]
    [TestCase(6, MaintenanceDueState.Upcoming)]
    public void Evaluate_ClassifiesCalendarBoundary(int remainingDays, MaintenanceDueState expected)
    {
        var data = new VehicleMaintenanceData
        {
            Plans = [new VehicleMaintenancePlan
            {
                Name = "Inspection",
                LastCompletedAt = Now.AddDays(remainingDays - 30),
                IntervalDays = 30
            }]
        };

        var status = _service.Evaluate(data, Now, new MaintenanceDueSoonThresholds(TimeSpan.FromDays(5))).Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(status.State, Is.EqualTo(expected));
            Assert.That(status.DueAt, Is.EqualTo(Now.AddDays(remainingDays)));
        }
    }

    [Test]
    public void Evaluate_WithoutCompletionDate_IsNotScheduled()
    {
        var data = new VehicleMaintenanceData
        {
            Plans = [new VehicleMaintenancePlan { Name = "Inspection", IntervalDays = 30 }]
        };

        var status = _service.Evaluate(data, Now).Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(status.State, Is.EqualTo(MaintenanceDueState.NotScheduled));
            Assert.That(status.DueAt, Is.Null);
        }
    }

    [Test]
    public void Evaluate_RejectsNegativeDueSoonWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Evaluate(
            new VehicleMaintenanceData(), Now, new MaintenanceDueSoonThresholds(TimeSpan.FromDays(-1))));
    }

    [Test]
    public void CompletePlan_UpdatesSelectedDateAndHistory()
    {
        var plan = new VehicleMaintenancePlan
        {
            Name = "Wheel service", Category = MaintenanceCategory.WheelService, IntervalDays = 90
        };
        var other = new VehicleMaintenancePlan
        {
            Name = "Inspection", IntervalDays = 30, LastCompletedAt = Now.AddDays(-10)
        };
        var maintenance = new VehicleMaintenanceData { Plans = [plan, other] };

        var entry = _service.CompletePlan(maintenance, plan.Id, Now);
        var next = _service.Evaluate(maintenance, Now).Single(status => status.PlanId == plan.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plan.LastCompletedAt, Is.EqualTo(Now));
            Assert.That(other.LastCompletedAt, Is.EqualTo(Now.AddDays(-10)));
            Assert.That(entry.PerformedAt, Is.EqualTo(Now));
            Assert.That(entry.Description, Is.EqualTo("Wheel service"));
            Assert.That(entry.Category, Is.EqualTo(MaintenanceCategory.WheelService));
            Assert.That(maintenance.Entries, Is.EqualTo(new[] { entry }));
            Assert.That(next.DueAt, Is.EqualTo(Now.AddDays(90)));
        }
    }

    [Test]
    public void CompletePlan_RejectsUnknownPlanWithoutAddingHistory()
    {
        var maintenance = new VehicleMaintenanceData();

        Assert.Throws<ArgumentException>(() => _service.CompletePlan(maintenance, Guid.NewGuid(), Now));
        Assert.That(maintenance.Entries, Is.Empty);
    }

    [TestCase(null)]
    [TestCase(0)]
    [TestCase(-1)]
    public void Validate_RejectsMissingOrInvalidCalendarInterval(int? days)
    {
        var data = new VehicleMaintenanceData
        {
            Plans = [new VehicleMaintenancePlan { Name = "Inspection", IntervalDays = days }]
        };

        Assert.That(_service.Validate(data), Has.Some.Contains("positive calendar interval"));
        Assert.Throws<ArgumentException>(() => _service.Evaluate(data, Now));
    }

    [Test]
    public void Validate_RejectsInvalidCurrency()
    {
        var data = new VehicleMaintenanceData
        {
            Entries = [new VehicleMaintenanceEntry
            {
                Description = "Service", Cost = new MoneyAmount { Currency = "EURO" }
            }]
        };

        Assert.That(_service.Validate(data), Has.Some.Contains("currency"));
    }
}
