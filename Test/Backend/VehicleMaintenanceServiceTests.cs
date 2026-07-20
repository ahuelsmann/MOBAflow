// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class VehicleMaintenanceServiceTests
{
    private readonly VehicleMaintenanceService _service = new();

    [Test]
    public void Evaluate_ClassifiesDateAndUsageBoundariesDeterministically()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);
        var usage = new VehicleUsageData
        {
            TrackedOperatingSeconds = 3_600,
            TrackedCompletedTrips = 10,
            TrackedDistanceKilometres = 25m
        };
        var data = new VehicleMaintenanceData
        {
            Plans =
            [
                new VehicleMaintenancePlan { Name = "Due today", LastCompletedAt = now.AddDays(-30), IntervalDays = 30 },
                new VehicleMaintenancePlan { Name = "Overdue", CompletedTripsAtLastCompletion = 3, IntervalCompletedTrips = 6 },
                new VehicleMaintenancePlan { Name = "Upcoming", OperatingSecondsAtLastCompletion = 3_000, IntervalOperatingSeconds = 1_000 }
            ]
        };

        // Act
        var results = _service.Evaluate(data, usage, now).ToDictionary(result => result.Name);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(results["Due today"].State, Is.EqualTo(MaintenanceDueState.Due));
            Assert.That(results["Overdue"].State, Is.EqualTo(MaintenanceDueState.Overdue));
            Assert.That(results["Upcoming"].State, Is.EqualTo(MaintenanceDueState.Upcoming));
            Assert.That(results["Upcoming"].RemainingOperatingSeconds, Is.EqualTo(400));
        });
    }

    [Test]
    public void Evaluate_UsesConfiguredDueSoonThresholdForFirstApproachingBoundary()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);
        var usage = new VehicleUsageData
        {
            TrackedOperatingSeconds = 9_500,
            TrackedCompletedTrips = 97,
            TrackedDistanceKilometres = 49m
        };
        var data = new VehicleMaintenanceData
        {
            Plans =
            [
                new VehicleMaintenancePlan
                {
                    Name = "Combined inspection",
                    LastCompletedAt = now.AddDays(-20),
                    IntervalDays = 30,
                    IntervalOperatingSeconds = 10_000,
                    IntervalCompletedTrips = 100,
                    IntervalDistanceKilometres = 50m
                }
            ]
        };
        var thresholds = new MaintenanceDueSoonThresholds(
            CalendarWindow: TimeSpan.FromDays(5),
            OperatingSeconds: 600,
            CompletedTrips: 2,
            DistanceKilometres: 0.5m);

        // Act
        var status = _service.Evaluate(data, usage, now, thresholds).Single();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(status.State, Is.EqualTo(MaintenanceDueState.DueSoon));
            Assert.That(status.RemainingOperatingSeconds, Is.EqualTo(500));
            Assert.That(status.RemainingCompletedTrips, Is.EqualTo(3));
            Assert.That(status.RemainingDistanceKilometres, Is.EqualTo(1m));
        });
    }

    [Test]
    public void Evaluate_CombinedPlanBecomesDue_WhenAnyConfiguredBoundaryIsReached()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);
        var usage = new VehicleUsageData
        {
            TrackedOperatingSeconds = 10_000,
            TrackedCompletedTrips = 20
        };
        var data = new VehicleMaintenanceData
        {
            Plans =
            [
                new VehicleMaintenancePlan
                {
                    Name = "Combined inspection",
                    LastCompletedAt = now.AddDays(-1),
                    IntervalDays = 30,
                    IntervalOperatingSeconds = 10_000,
                    IntervalCompletedTrips = 100
                }
            ]
        };

        // Act
        var status = _service.Evaluate(data, usage, now).Single();

        // Assert
        Assert.That(status.State, Is.EqualTo(MaintenanceDueState.Due));
    }

    [Test]
    public void CompletePlan_UpdatesConfiguredBaselinesAndHistory_WithoutChangingLifetimeUsage()
    {
        // Arrange
        var completedAt = new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);
        var plan = new VehicleMaintenancePlan
        {
            Name = "Wheel service",
            Category = MaintenanceCategory.WheelService,
            IntervalDays = 90,
            IntervalOperatingSeconds = 18_000,
            IntervalCompletedTrips = 25
        };
        var maintenance = new VehicleMaintenanceData { Plans = [plan] };
        var usage = new VehicleUsageData
        {
            TrackedOperatingSeconds = 7_200,
            TrackedCompletedTrips = 12,
            TrackedDistanceKilometres = 42m
        };

        // Act
        var entry = _service.CompletePlan(maintenance, plan.Id, usage, completedAt);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(plan.LastCompletedAt, Is.EqualTo(completedAt));
            Assert.That(plan.OperatingSecondsAtLastCompletion, Is.EqualTo(7_200));
            Assert.That(plan.CompletedTripsAtLastCompletion, Is.EqualTo(12));
            Assert.That(plan.DistanceKilometresAtLastCompletion, Is.Null);
            Assert.That(entry.Description, Is.EqualTo("Wheel service"));
            Assert.That(entry.DistanceKilometresAtService, Is.EqualTo(42m));
            Assert.That(maintenance.Entries, Is.EqualTo(new[] { entry }));
            Assert.That(usage.TrackedOperatingSeconds, Is.EqualTo(7_200));
            Assert.That(usage.TrackedCompletedTrips, Is.EqualTo(12));
        });
    }

    [Test]
    public void CompletePlan_RejectsDistancePlan_WhenDistanceIsUnavailable()
    {
        // Arrange
        var plan = new VehicleMaintenancePlan { Name = "Distance service", IntervalDistanceKilometres = 10m };
        var maintenance = new VehicleMaintenanceData { Plans = [plan] };

        // Act
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.CompletePlan(maintenance, plan.Id, new VehicleUsageData(), DateTimeOffset.UtcNow));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("requires tracked distance"));
            Assert.That(maintenance.Entries, Is.Empty);
            Assert.That(plan.DistanceKilometresAtLastCompletion, Is.Null);
        });
    }

    [Test]
    public void Validate_RejectsNegativeCountersInvalidIntervalsAndCurrency()
    {
        // Arrange
        var data = new VehicleMaintenanceData
        {
            Entries =
            [
                new VehicleMaintenanceEntry
                {
                    Description = "Service",
                    CompletedTripsAtService = -1,
                    Cost = new MoneyAmount { Currency = "EURO" }
                }
            ],
            Plans = [new VehicleMaintenancePlan { Name = "Invalid", IntervalDays = 0 }]
        };

        // Act
        var errors = _service.Validate(data);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(errors, Has.Some.Contains("negative"));
            Assert.That(errors, Has.Some.Contains("currency"));
            Assert.That(errors, Has.Some.Contains("positive"));
        });
    }
}
