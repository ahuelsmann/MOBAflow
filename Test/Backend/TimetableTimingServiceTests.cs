// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using global::Moba.Backend.Service;
using global::Moba.Domain;

internal sealed class TimetableTimingServiceTests
{
    [Test]
    public void CalculateDelay_Should_UseActualDepartureWhenAvailable()
    {
        var scheduled = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var service = new TimetableTimingService(new FixedTimeProvider(scheduled.AddHours(1)));
        var call = new TimetableCall { ScheduledArrival = scheduled, ScheduledDeparture = scheduled.AddMinutes(2) };
        var state = new TimetableCallState { ActualDeparture = scheduled.AddMinutes(7) };

        var delay = service.CalculateDelay(call, state);

        Assert.That(delay, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public void CalculateDelay_Should_UseClockForOverdueCallWithoutActualTime()
    {
        var scheduled = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var service = new TimetableTimingService(new FixedTimeProvider(scheduled.AddMinutes(12)));
        var call = new TimetableCall { ScheduledArrival = scheduled, ScheduledDeparture = scheduled.AddMinutes(2) };

        var delay = service.CalculateDelay(call, null);

        Assert.That(delay, Is.EqualTo(TimeSpan.FromMinutes(10)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
