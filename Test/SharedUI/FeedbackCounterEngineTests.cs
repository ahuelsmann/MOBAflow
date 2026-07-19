// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

[TestFixture]
internal sealed class FeedbackCounterEngineTests
{
    [Test]
    public void ApplyFeedback_Should_RecordFirstFeedback()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        var engine = new FeedbackCounterEngine(timeProvider);
        var statistic = new InPortStatistic { InPort = 3 };

        // Act
        var result = engine.ApplyFeedback(statistic, useTimerFilter: true, timerIntervalSeconds: 10);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.ElapsedSincePrevious, Is.Null);
            Assert.That(statistic.Count, Is.EqualTo(1));
            Assert.That(statistic.LastLapTime, Is.Null);
            Assert.That(statistic.LastFeedbackTime, Is.EqualTo(new DateTime(2026, 7, 19, 12, 0, 0)));
        });
    }

    [Test]
    public void ApplyFeedback_Should_IgnoreFeedback_When_TimerIntervalHasNotElapsed()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        var engine = new FeedbackCounterEngine(timeProvider);
        var statistic = new InPortStatistic { InPort = 3 };
        engine.ApplyFeedback(statistic, useTimerFilter: true, timerIntervalSeconds: 10);
        timeProvider.Advance(TimeSpan.FromSeconds(4));

        // Act
        var result = engine.ApplyFeedback(statistic, useTimerFilter: true, timerIntervalSeconds: 10);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.ElapsedSincePrevious, Is.EqualTo(TimeSpan.FromSeconds(4)));
            Assert.That(statistic.Count, Is.EqualTo(1));
            Assert.That(statistic.LastLapTime, Is.Null);
            Assert.That(statistic.LastFeedbackTime, Is.EqualTo(new DateTime(2026, 7, 19, 12, 0, 0)));
        });
    }

    [Test]
    public void ApplyFeedback_Should_RecordLapTime_When_TimerIntervalHasElapsed()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        var engine = new FeedbackCounterEngine(timeProvider);
        var statistic = new InPortStatistic { InPort = 3 };
        engine.ApplyFeedback(statistic, useTimerFilter: true, timerIntervalSeconds: 10);
        timeProvider.Advance(TimeSpan.FromSeconds(12));

        // Act
        var result = engine.ApplyFeedback(statistic, useTimerFilter: true, timerIntervalSeconds: 10);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.ElapsedSincePrevious, Is.EqualTo(TimeSpan.FromSeconds(12)));
            Assert.That(statistic.Count, Is.EqualTo(2));
            Assert.That(statistic.LastLapTime, Is.EqualTo(TimeSpan.FromSeconds(12)));
            Assert.That(statistic.LastFeedbackTime, Is.EqualTo(new DateTime(2026, 7, 19, 12, 0, 12)));
        });
    }

    [Test]
    public void ApplyFeedback_Should_AcceptImmediateFeedback_When_TimerFilterIsDisabled()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        var engine = new FeedbackCounterEngine(timeProvider);
        var statistic = new InPortStatistic { InPort = 3 };
        engine.ApplyFeedback(statistic, useTimerFilter: false, timerIntervalSeconds: 10);
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        // Act
        var result = engine.ApplyFeedback(statistic, useTimerFilter: false, timerIntervalSeconds: 10);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(statistic.Count, Is.EqualTo(2));
            Assert.That(statistic.LastLapTime, Is.EqualTo(TimeSpan.FromSeconds(1)));
        });
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan elapsed)
        {
            _utcNow += elapsed;
        }
    }
}
