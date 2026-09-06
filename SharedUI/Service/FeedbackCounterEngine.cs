// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using ViewModel;

internal sealed class FeedbackCounterEngine
{
    private readonly TimeProvider _timeProvider;

    public FeedbackCounterEngine(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public FeedbackCounterUpdate ApplyFeedback(
        InPortStatistic statistic,
        bool useTimerFilter,
        double timerIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(statistic);

        var receivedAt = _timeProvider.GetLocalNow().DateTime;
        TimeSpan? elapsedSincePrevious = statistic.LastFeedbackTime.HasValue
            ? receivedAt - statistic.LastFeedbackTime.Value
            : null;

        if (useTimerFilter
            && elapsedSincePrevious.HasValue
            && elapsedSincePrevious.Value.TotalSeconds < timerIntervalSeconds)
        {
            return new FeedbackCounterUpdate(false, elapsedSincePrevious);
        }

        statistic.LastLapTime = elapsedSincePrevious;
        statistic.Count++;
        statistic.LastFeedbackTime = receivedAt;

        return new FeedbackCounterUpdate(true, elapsedSincePrevious);
    }
}

internal readonly record struct FeedbackCounterUpdate(
    bool IsAccepted,
    TimeSpan? ElapsedSincePrevious);
