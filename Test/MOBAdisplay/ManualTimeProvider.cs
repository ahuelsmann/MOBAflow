// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _syncRoot = new();
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;
    private long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public int ScheduledTimerCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _timers.Count(timer => timer.DueTimestamp is not null);
            }
        }
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_syncRoot)
        {
            return _utcNow;
        }
    }

    public override long GetTimestamp()
    {
        lock (_syncRoot)
        {
            return _timestamp;
        }
    }

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        lock (_syncRoot)
        {
            _utcNow += elapsed;
            _timestamp += elapsed.Ticks;
        }

        while (TryTakeDueTimer(out var timer))
        {
            timer.Invoke();
        }
    }

    private bool TryTakeDueTimer(out ManualTimer timer)
    {
        lock (_syncRoot)
        {
            timer = _timers
                .Where(candidate => candidate.DueTimestamp is not null && candidate.DueTimestamp <= _timestamp)
                .OrderBy(candidate => candidate.DueTimestamp)
                .FirstOrDefault()!;
            if (timer is null)
            {
                return false;
            }

            timer.ScheduleNext(_timestamp);
            return true;
        }
    }

    private void Change(ManualTimer timer, TimeSpan dueTime, TimeSpan period)
    {
        ValidateTimerValue(dueTime, nameof(dueTime));
        ValidateTimerValue(period, nameof(period));
        lock (_syncRoot)
        {
            if (!_timers.Contains(timer))
            {
                _timers.Add(timer);
            }

            timer.SetSchedule(
                dueTime == Timeout.InfiniteTimeSpan ? null : _timestamp + dueTime.Ticks,
                period);
        }
    }

    private void Remove(ManualTimer timer)
    {
        lock (_syncRoot)
        {
            _timers.Remove(timer);
        }
    }

    private static void ValidateTimerValue(TimeSpan value, string parameterName)
    {
        if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private sealed class ManualTimer : ITimer
    {
        private readonly ManualTimeProvider _owner;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private bool _disposed;
        private TimeSpan _period;

        public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state)
        {
            _owner = owner;
            _callback = callback;
            _state = state;
        }

        public long? DueTimestamp { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (_disposed)
            {
                return false;
            }

            _owner.Change(this, dueTime, period);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public void SetSchedule(long? dueTimestamp, TimeSpan period)
        {
            DueTimestamp = dueTimestamp;
            _period = period;
        }

        public void ScheduleNext(long currentTimestamp)
        {
            DueTimestamp = _period > TimeSpan.Zero && _period != Timeout.InfiniteTimeSpan
                ? currentTimestamp + _period.Ticks
                : null;
        }

        public void Invoke()
        {
            if (!_disposed)
            {
                _callback(_state);
            }
        }
    }
}