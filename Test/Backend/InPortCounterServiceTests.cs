// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moq;
using System.Collections.Concurrent;

[TestFixture]
public sealed class InPortCounterServiceTests
{
    [Test]
    public void AcceptedActivations_CountEachInputIndependentlyAndPreservePreviousSnapshots()
    {
        var z21 = new Mock<IZ21>();
        using var counters = CreateCounters(z21);
        Raise(z21, 2);
        var first = counters.GetSnapshot();
        Raise(z21, 3);
        Raise(z21, 2);

        Assert.Multiple(() =>
        {
            Assert.That(counters.GetSnapshot().Select(value => (value.InPort, value.Count)),
                Is.EqualTo(new[] { (2u, 2UL), (3u, 1UL) }));
            Assert.That(first.Single().Count, Is.EqualTo(1UL));
        });
    }

    [Test]
    public void TimerFilter_UsesLastAcceptedActivationAndResetsOnlyExplicitly()
    {
        var z21 = new Mock<IZ21>();
        var clock = new CounterTimeProvider();
        var settings = new AppSettings { Counter = { UseTimerFilter = true, TimerIntervalSeconds = 10 } };
        using var counters = new InPortCounterService(z21.Object, settings, clock);
        var accepted = 0;
        counters.Counted += (_, _) => accepted++;
        Raise(z21, 1);
        clock.Advance(TimeSpan.FromSeconds(9));
        Raise(z21, 1);
        clock.Advance(TimeSpan.FromSeconds(1));
        Raise(z21, 1);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.EqualTo(2));
            Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(2UL));
            Assert.That(counters.GetSnapshot().Single().LastLapTime, Is.EqualTo(TimeSpan.FromSeconds(10)));
        });

        Assert.That(counters.TryResetAll(), Is.True);
        Assert.That(counters.GetSnapshot().Single().LastFeedbackTime, Is.Null);
        Raise(z21, 1);
        Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(1UL));
    }

    [Test]
    public async Task Reset_IsRejectedWhileAnyNewOrLegacyJourneyIsActive()
    {
        var z21 = new Mock<IZ21>();
        using var counters = CreateCounters(z21);
        var newJourney = new Journey { EventPlan = new JourneyEventPlan() };
        var legacyJourney = new Journey { FeedbackSequence = [new JourneyFeedbackStep { InPort = 1 }] };
        var project = new Project { Journeys = [newJourney, legacyJourney] };
        using var manager = new JourneyManager(z21.Object, project, Mock.Of<IWorkflowService>(),
            dependencies: new JourneyManagerDependencies { InPortCounterService = counters });
        Raise(z21, 1);
        Assert.That(counters.TryResetAll(), Is.False, "Legacy journeys can execute immediately.");
        await manager.StartJourneyAsync(newJourney);
        await manager.StopJourneyAsync(legacyJourney);
        Assert.That(counters.TryResetAll(), Is.False, "The new journey still owns its captured count bases.");
        await manager.StopJourneyAsync(newJourney);
        Assert.Multiple(() =>
        {
            Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(1UL));
            Assert.That(counters.TryResetAll(), Is.True);
            Assert.That(counters.GetSnapshot().Single().Count, Is.Zero);
        });
    }

    [Test]
    public void ReplacingJourneyManagers_DoesNotResetCountsOrDuplicateSourceSubscriptions()
    {
        var z21 = new Mock<IZ21>();
        using var counters = CreateCounters(z21);
        var project = new Project { Journeys = [new Journey { EventPlan = new JourneyEventPlan() }] };
        using (var first = new JourneyManager(z21.Object, project, Mock.Of<IWorkflowService>(),
                   dependencies: new JourneyManagerDependencies { InPortCounterService = counters }))
        {
            Raise(z21, 1);
        }

        using (var second = new JourneyManager(z21.Object, project, Mock.Of<IWorkflowService>(),
                   dependencies: new JourneyManagerDependencies { InPortCounterService = counters }))
        {
            Raise(z21, 1);
            Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(2UL));
        }

        Raise(z21, 1);
        Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(3UL));
        counters.Dispose();
        Raise(z21, 1);
        Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(3UL));
    }

    [Test]
    public async Task ConcurrentActivations_DoNotLoseCountUpdates()
    {
        var z21 = new Mock<IZ21>();
        using var counters = CreateCounters(z21);
        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => Raise(z21, 1))));
        Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(100UL));
    }

    [Test]
    public async Task ConcurrentActivations_PublishCountsInOrderWhenFirstSubscriberIsBusy()
    {
        var z21 = new Mock<IZ21>();
        using var counters = CreateCounters(z21);
        using var releaseFirst = new ManualResetEventSlim();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delivered = new ConcurrentQueue<ulong>();
        counters.Counted += (_, args) =>
        {
            if (args.Snapshot.Count == 1)
            {
                firstEntered.TrySetResult();
                if (!releaseFirst.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("The first counter notification was not released.");
                }
            }

            delivered.Enqueue(args.Snapshot.Count);
        };

        var firstActivation = Task.Run(() => Raise(z21, 1));
        try
        {
            await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Run(() => Raise(z21, 1)).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(delivered, Is.Empty, "The second count must wait behind the first notification.");
        }
        finally
        {
            releaseFirst.Set();
            await firstActivation.WaitAsync(TimeSpan.FromSeconds(5));
        }

        Assert.That(delivered, Is.EqualTo(new[] { 1UL, 2UL }));
    }

    [Test]
    public async Task ResetThenStart_RejectsAnOldActivationStillBeingDelivered()
    {
        var z21 = new Mock<IZ21>();
        using var counters = CreateCounters(z21);
        using var releaseFirst = new ManualResetEventSlim();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        counters.Counted += (_, args) =>
        {
            if (args.Generation == 0)
            {
                firstEntered.TrySetResult();
                if (!releaseFirst.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("The old counter notification was not released.");
                }
            }
        };
        var journey = new Journey
        {
            EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { InPort = 1, Count = 1 }] }
        };
        using var manager = new JourneyManager(z21.Object, new Project { Journeys = [journey] }, Mock.Of<IWorkflowService>(),
            dependencies: new JourneyManagerDependencies { InPortCounterService = counters });
        var oldActivation = Task.Run(() => Raise(z21, 1));
        try
        {
            await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(counters.TryResetAll(), Is.True);
            await manager.StartJourneyAsync(journey);
        }
        finally
        {
            releaseFirst.Set();
            await oldActivation.WaitAsync(TimeSpan.FromSeconds(5));
        }

        Assert.That(manager.GetState(journey.Id)!.CompletedEventIds, Is.Empty);
        Raise(z21, 1);
        Assert.That(manager.GetState(journey.Id)!.CompletedEventIds, Has.Count.EqualTo(1));
    }

    [Test]
    public void FailedSubscriber_DoesNotPreventOtherSubscribersOrLaterActivations()
    {
        var z21 = new Mock<IZ21>();
        using var counters = CreateCounters(z21);
        var delivered = new List<ulong>();
        counters.Counted += (_, _) => throw new InvalidOperationException("Subscriber failed.");
        counters.Counted += (_, args) => delivered.Add(args.Snapshot.Count);
        Raise(z21, 1);
        Raise(z21, 1);
        Assert.That(delivered, Is.EqualTo(new[] { 1UL, 2UL }));
    }

    private static InPortCounterService CreateCounters(Mock<IZ21> z21) =>
        new(z21.Object, new AppSettings { Counter = { UseTimerFilter = false } });

    internal static void Raise(Mock<IZ21> z21, int inPort) =>
        z21.Raise(source => source.Received += null, CreateFeedback(inPort));

    internal static FeedbackResult CreateFeedback(int inPort)
    {
        var offset = inPort - 1;
        var packet = new byte[15];
        packet[0] = 0x0F;
        packet[2] = 0x80;
        packet[4] = (byte)(offset / 64);
        packet[5 + offset % 64 / 8] = (byte)(1 << (offset % 8));
        return new FeedbackResult(packet);
    }

    private sealed class CounterTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }
}
