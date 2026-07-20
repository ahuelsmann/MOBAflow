// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Service;
using Moba.Common.Events;

[TestFixture]
internal sealed class Z21EventPipelineTests
{
    [Test]
    public async Task StopAsync_PublishesAcceptedEventsInFifoOrder()
    {
        var eventBus = new RecordingEventBus();
        await using var pipeline = new Z21EventPipeline(eventBus, NullLogger.Instance, capacity: 512);

        for (var inPort = 1; inPort <= 500; inPort++)
        {
            Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(inPort)), Is.True);
        }

        await pipeline.StopAsync(TimeSpan.FromSeconds(2));

        Assert.That(
            eventBus.PublishedEvents.Cast<FeedbackReceivedEvent>().Select(@event => @event.InPort),
            Is.EqualTo(Enumerable.Range(1, 500)));
        Assert.Multiple(() =>
        {
            var snapshot = pipeline.GetSnapshot();
            Assert.That(snapshot.QueueDepth, Is.Zero);
            Assert.That(snapshot.EnqueuedEvents, Is.EqualTo(500));
            Assert.That(snapshot.PublishedEvents, Is.EqualTo(500));
            Assert.That(snapshot.RejectedEvents, Is.Zero);
        });
    }

    [Test]
    public async Task TryEnqueue_RejectsIncomingEvent_WhenCapacityIsExhausted()
    {
        using var releaseFirstPublish = new ManualResetEventSlim();
        var firstPublishEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var publishCount = 0;
        var eventBus = new RecordingEventBus
        {
            BeforePublish = _ =>
            {
                if (Interlocked.Increment(ref publishCount) == 1)
                {
                    firstPublishEntered.TrySetResult();
                    SpinWait.SpinUntil(() => releaseFirstPublish.IsSet, TimeSpan.FromSeconds(2));
                }
            }
        };
        await using var pipeline = new Z21EventPipeline(eventBus, NullLogger.Instance, capacity: 2);

        Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(1)), Is.True);
        await firstPublishEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(2)), Is.True);
            Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(3)), Is.True);
            Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(4)), Is.False);
        });

        releaseFirstPublish.Set();
        await pipeline.StopAsync(TimeSpan.FromSeconds(2));

        Assert.Multiple(() =>
        {
            Assert.That(
                eventBus.PublishedEvents.Cast<FeedbackReceivedEvent>().Select(@event => @event.InPort),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(pipeline.GetSnapshot().RejectedEvents, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Consumer_ContinuesAfterEventBusBoundaryThrows()
    {
        var attempt = 0;
        var eventBus = new RecordingEventBus
        {
            BeforePublish = _ =>
            {
                if (Interlocked.Increment(ref attempt) == 1)
                {
                    throw new InvalidOperationException("Expected test failure");
                }
            }
        };
        await using var pipeline = new Z21EventPipeline(eventBus, NullLogger.Instance, capacity: 4);

        Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(1)), Is.True);
        Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(2)), Is.True);

        await pipeline.StopAsync(TimeSpan.FromSeconds(2));

        Assert.Multiple(() =>
        {
            Assert.That(eventBus.PublishedEvents.Cast<FeedbackReceivedEvent>().Single().InPort, Is.EqualTo(2));
            Assert.That(pipeline.GetSnapshot().DispatchFailures, Is.EqualTo(1));
            Assert.That(pipeline.Completion.IsCompletedSuccessfully, Is.True);
        });
    }

    [Test]
    public async Task Snapshot_ReportsSubscriberFailuresFromEventBusDiagnostics()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        eventBus.Subscribe<FeedbackReceivedEvent>(_ => throw new InvalidOperationException("Expected test failure"));
        await using var pipeline = new Z21EventPipeline(eventBus, NullLogger.Instance, capacity: 2);

        Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(1)), Is.True);
        await pipeline.StopAsync(TimeSpan.FromSeconds(1));

        Assert.That(pipeline.GetSnapshot().SubscriberFailures, Is.EqualTo(1));
    }

    [Test]
    public async Task StopAsync_CancelsConsumerAndRecordsTimeout_WhenSubscriberBlocksDrain()
    {
        using var releasePublish = new ManualResetEventSlim();
        var publishEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventBus = new RecordingEventBus
        {
            BeforePublish = _ =>
            {
                publishEntered.TrySetResult();
                SpinWait.SpinUntil(() => releasePublish.IsSet, TimeSpan.FromSeconds(2));
            }
        };
        await using var pipeline = new Z21EventPipeline(eventBus, NullLogger.Instance, capacity: 2);

        Assert.That(pipeline.TryEnqueue(new FeedbackReceivedEvent(1)), Is.True);
        await publishEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await pipeline.StopAsync(TimeSpan.FromMilliseconds(20));

        Assert.That(pipeline.GetSnapshot().ShutdownTimeouts, Is.EqualTo(1));
        releasePublish.Set();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(1));
    }

    private sealed class RecordingEventBus : IEventBus
    {
        private readonly object _lock = new();
        private readonly List<IEvent> _publishedEvents = [];

        public Action<IEvent>? BeforePublish { get; init; }

        public IReadOnlyList<IEvent> PublishedEvents
        {
            get
            {
                lock (_lock)
                {
                    return _publishedEvents.ToArray();
                }
            }
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : class, IEvent
        {
            BeforePublish?.Invoke(@event);
            lock (_lock)
            {
                _publishedEvents.Add(@event);
            }
        }

        public Guid Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent => Guid.NewGuid();

        public void Unsubscribe(Guid subscriptionId)
        {
        }

        public int GetSubscriberCount<TEvent>() where TEvent : class, IEvent => 0;
    }
}