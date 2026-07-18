// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class LocomotiveMaintenanceServiceTests
{
    private readonly LocomotiveMaintenanceService _service = new();

    [Test]
    public void Evaluate_ClassifiesDateAndCounterBoundariesDeterministically()
    {
        var now = new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);
        var data = new LocomotiveMaintenanceData
        {
            OperatingHours = 100,
            Plans =
            [
                new LocomotiveMaintenancePlan { Name = "Due today", LastCompletedAt = now.AddDays(-30), IntervalDays = 30 },
                new LocomotiveMaintenancePlan { Name = "Overdue", LastCompletedAt = now.AddDays(-31), IntervalDays = 30 },
                new LocomotiveMaintenancePlan { Name = "Soon", OperatingHoursAtLastCompletion = 10, IntervalOperatingHours = 100 }
            ]
        };

        var results = _service.Evaluate(data, now).ToDictionary(result => result.Name);

        Assert.Multiple(() =>
        {
            Assert.That(results["Due today"].State, Is.EqualTo(MaintenanceDueState.Due));
            Assert.That(results["Overdue"].State, Is.EqualTo(MaintenanceDueState.Overdue));
            Assert.That(results["Soon"].State, Is.EqualTo(MaintenanceDueState.Upcoming));
        });
    }

    [Test]
    public void Validate_RejectsNegativeCountersInvalidIntervalsAndCurrency()
    {
        var data = new LocomotiveMaintenanceData
        {
            OperatingHours = -1,
            Entries = [new LocomotiveMaintenanceEntry { Description = "Service", Cost = new MoneyAmount { Currency = "EURO" } }],
            Plans = [new LocomotiveMaintenancePlan { Name = "Invalid", IntervalDays = 0 }]
        };

        var errors = _service.Validate(data);

        Assert.Multiple(() =>
        {
            Assert.That(errors, Has.Some.Contains("negative"));
            Assert.That(errors, Has.Some.Contains("currency"));
            Assert.That(errors, Has.Some.Contains("positive"));
        });
    }
}
