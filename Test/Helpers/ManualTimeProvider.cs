// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

/// <summary>
/// A clock that only moves when a test calls <see cref="Advance"/>; one-shot timers (as used by
/// <c>Task.Delay</c>) fire when their due time is reached.
/// </summary>
internal sealed partial class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private readonly Lock _gate = new();
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _now;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    /// <summary>Moves the clock forward and fires every timer that became due.</summary>
    public void Advance(TimeSpan duration)
    {
        List<ManualTimer> due;
        lock (_gate)
        {
            _now += duration;
            due = _timers.Where(timer => timer.DueAt <= _now).ToList();
            foreach (var timer in due)
            {
                _timers.Remove(timer);
            }
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    private void Schedule(ManualTimer timer, DateTimeOffset? dueAt)
    {
        lock (_gate)
        {
            _timers.Remove(timer);
            timer.DueAt = dueAt;
            if (dueAt.HasValue)
            {
                _timers.Add(timer);
            }
        }
    }

    private sealed partial class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        public DateTimeOffset? DueAt { get; set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _ = period;
            owner.Schedule(this, dueTime == Timeout.InfiniteTimeSpan ? null : owner.GetUtcNow() + dueTime);
            return true;
        }

        public void Fire() => callback(state);

        public void Dispose() => owner.Schedule(this, null);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
