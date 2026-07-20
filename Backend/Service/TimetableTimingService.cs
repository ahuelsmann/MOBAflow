// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;

using Interface;

/// <summary>
/// Calculates planned-versus-actual delay consistently using the injected clock.
/// </summary>
public sealed class TimetableTimingService : ITimetableTimingService
{
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new timing service with an injectable clock.</summary>
    public TimetableTimingService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public TimeSpan CalculateDelay(TimetableCall call, TimetableCallState? state)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (state?.ActualDeparture is DateTimeOffset actualDeparture) return actualDeparture - call.ScheduledDeparture;
        if (state?.ActualArrival is DateTimeOffset actualArrival) return actualArrival - call.ScheduledArrival;

        var now = _timeProvider.GetUtcNow();
        return now > call.ScheduledDeparture ? now - call.ScheduledDeparture : TimeSpan.Zero;
    }
}
