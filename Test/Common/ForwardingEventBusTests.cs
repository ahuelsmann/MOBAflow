// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Events;

/// <summary>
/// The bus of a project runtime forwards to the application bus only while its project is selected, judged when an
/// event is delivered.
/// </summary>
[TestFixture]
internal sealed class ForwardingEventBusTests
{
    [Test]
    public void EventQueuedBeforeDeselection_ReachesTheRuntimeButNotTheApplication()
    {
        var queued = new Queue<Action>();
        var local = new EventBus(NullLogger<EventBus>.Instance);
        var application = new EventBus(NullLogger<EventBus>.Instance);
        var bus = new ForwardingEventBus(local, application, queued.Enqueue) { IsForwarding = true };
        var runtimeReceived = 0;
        var applicationReceived = 0;
        bus.Subscribe<TestEvent>(_ => runtimeReceived++);
        application.Subscribe<TestEvent>(_ => applicationReceived++);

        bus.Publish(new TestEvent());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(runtimeReceived, Is.EqualTo(1), "Runtime handling must not wait for the UI queue.");
            Assert.That(applicationReceived, Is.Zero, "Application delivery still waits for the UI queue.");
        }
        bus.IsForwarding = false;
        queued.Dequeue().Invoke();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(runtimeReceived, Is.EqualTo(1));
            Assert.That(applicationReceived, Is.Zero);
        }
    }

    [Test]
    public void SelectedRuntime_HandlesFeedbackWhileApplicationDeliveryIsQueued()
    {
        var queued = new Queue<Action>();
        var application = new EventBus(NullLogger<EventBus>.Instance);
        var bus = new ForwardingEventBus(new EventBus(NullLogger<EventBus>.Instance), application, queued.Enqueue)
        {
            IsForwarding = true
        };
        var runtimeReceived = 0;
        var applicationReceived = 0;
        bus.Subscribe<TestEvent>(_ => runtimeReceived++);
        application.Subscribe<TestEvent>(_ => applicationReceived++);

        bus.Publish(new TestEvent());
        bus.Publish(new TestEvent());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(runtimeReceived, Is.EqualTo(2));
            Assert.That(applicationReceived, Is.Zero);
        }
        while (queued.TryDequeue(out var deliver))
        {
            deliver();
        }
        Assert.That(applicationReceived, Is.EqualTo(2));
    }

    [Test]
    public void WithoutDelivery_PublishesOnThePublishingThread()
    {
        var application = new EventBus(NullLogger<EventBus>.Instance);
        var bus = new ForwardingEventBus(new EventBus(NullLogger<EventBus>.Instance), application) { IsForwarding = true };
        var applicationReceived = 0;
        application.Subscribe<TestEvent>(_ => applicationReceived++);

        bus.Publish(new TestEvent());

        Assert.That(applicationReceived, Is.EqualTo(1));
    }

    private sealed record TestEvent : EventBase;
}
