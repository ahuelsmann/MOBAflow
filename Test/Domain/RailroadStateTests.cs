// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

[TestFixture]
internal sealed class RailroadStateTests
{
    [Test]
    public void RailroadState_Should_StoreAndClearOccupancyAndFeedback()
    {
        // Arrange
        var state = new RailroadState();
        var trackId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act and assert
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.False);
            Assert.That(state.GetLastFeedback(trackId), Is.Null);
        });

        state.SetOccupied(trackId, true);
        Assert.That(state.IsOccupied(trackId), Is.True);

        state.MarkFeedback(trackId, timestamp);
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.True);
            Assert.That(state.GetLastFeedback(trackId), Is.EqualTo(timestamp));
        });

        state.ClearFeedback(trackId);
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.False);
            Assert.That(state.GetLastFeedback(trackId), Is.Null);
        });
    }

    [Test]
    public void ExpireFeedback_Should_ExpireAtTimeoutBoundaryOnly()
    {
        // Arrange
        var state = new RailroadState();
        var trackId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        state.MarkFeedback(trackId, timestamp);

        // Act
        state.ExpireFeedback(timestamp.AddMilliseconds(999), TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(state.IsOccupied(trackId), Is.True);

        state.ExpireFeedback(timestamp.AddSeconds(1), TimeSpan.FromSeconds(1));
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.False);
            Assert.That(state.GetLastFeedback(trackId), Is.Null);
            Assert.That(
                () => state.ExpireFeedback(timestamp, TimeSpan.FromMilliseconds(-1)),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ExpireFeedback_Should_AcceptZeroTimeoutAndExpireImmediately()
    {
        // Arrange
        var state = new RailroadState();
        var trackId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        state.MarkFeedback(trackId, timestamp);

        // Act
        state.ExpireFeedback(timestamp, TimeSpan.Zero);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.False);
            Assert.That(state.GetLastFeedback(trackId), Is.Null);
        });
    }

    [Test]
    public void RailroadState_Should_StoreSwitchAndValidatedSignalState()
    {
        // Arrange
        var state = new RailroadState();

        // Act and assert
        Assert.Multiple(() =>
        {
            Assert.That(state.GetSwitchPosition(4), Is.Null);
            Assert.That(state.GetSignalAspect(5), Is.Null);
        });

        state.SetSwitchPosition(4, isLeft: false);
        state.SetSignalAspect(5, "Stop");

        Assert.Multiple(() =>
        {
            Assert.That(state.GetSwitchPosition(4), Is.False);
            Assert.That(state.GetSignalAspect(5), Is.EqualTo("Stop"));
            Assert.That(() => state.SetSignalAspect(5, ""), Throws.ArgumentException);
            Assert.That(() => state.SetSignalAspect(5, " "), Throws.ArgumentException);
            Assert.That(() => state.SetSignalAspect(5, null!), Throws.ArgumentNullException);
        });
    }
}
