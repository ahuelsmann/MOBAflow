// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Events;

/// <summary>
/// Unit tests for EventBus (IEventBus implementation).
/// Covers Publish, Subscribe, Unsubscribe, and GetSubscriberCount.
/// </summary>
[TestFixture]
internal class EventBusTests
{
    [Test]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();

        Assert.DoesNotThrow(() => bus.Publish(new Z21ConnectionEstablishedEvent()));
    }

    [Test]
    public void Publish_InvokesSubscribedHandler()
    {
        var bus = new EventBus();
        var received = false;

        bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => received = true);
        bus.Publish(new Z21ConnectionEstablishedEvent());

        Assert.That(received, Is.True);
    }

    [Test]
    public void Publish_InvokesMultipleHandlersForSameEventType()
    {
        var bus = new EventBus();
        var count = 0;

        bus.Subscribe<FeedbackReceivedEvent>(_ => count++);
        bus.Subscribe<FeedbackReceivedEvent>(_ => count += 10);
        bus.Publish(new FeedbackReceivedEvent(1));

        Assert.That(count, Is.EqualTo(11));
    }

    [Test]
    public void Publish_DoesNotInvokeHandlersForOtherEventTypes()
    {
        var bus = new EventBus();
        var connectionReceived = false;
        var feedbackReceived = false;

        bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => connectionReceived = true);
        bus.Subscribe<FeedbackReceivedEvent>(_ => feedbackReceived = true);
        bus.Publish(new FeedbackReceivedEvent(1));

        Assert.That(connectionReceived, Is.False);
        Assert.That(feedbackReceived, Is.True);
    }

    [Test]
    public void Subscribe_ReturnsNonEmptyGuid()
    {
        var bus = new EventBus();

        var id = bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => { });

        Assert.That(id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Subscribe_ReturnsUniqueIds()
    {
        var bus = new EventBus();
        var id1 = bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => { });
        var id2 = bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => { });

        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void Unsubscribe_StopsReceivingEvents()
    {
        var bus = new EventBus();
        var count = 0;
        var id = bus.Subscribe<FeedbackReceivedEvent>(_ => count++);

        bus.Publish(new FeedbackReceivedEvent(1));
        Assert.That(count, Is.EqualTo(1));

        bus.Unsubscribe(id);
        bus.Publish(new FeedbackReceivedEvent(2));

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Unsubscribe_DoesNotAffectOtherSubscriptions()
    {
        var bus = new EventBus();
        var count = 0;
        var id1 = bus.Subscribe<FeedbackReceivedEvent>(_ => count++);
        bus.Subscribe<FeedbackReceivedEvent>(_ => count++);

        bus.Unsubscribe(id1);
        bus.Publish(new FeedbackReceivedEvent(1));

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void GetSubscriberCount_ReturnsZero_WhenNoSubscribers()
    {
        var bus = new EventBus();

        Assert.That(bus.GetSubscriberCount<Z21ConnectionEstablishedEvent>(), Is.Zero);
    }

    [Test]
    public void GetSubscriberCount_ReturnsCorrectCount_AfterSubscribe()
    {
        var bus = new EventBus();

        bus.Subscribe<FeedbackReceivedEvent>(_ => { });
        bus.Subscribe<FeedbackReceivedEvent>(_ => { });

        Assert.That(bus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.EqualTo(2));
    }

    [Test]
    public void GetSubscriberCount_ReturnsCorrectCount_AfterUnsubscribe()
    {
        var bus = new EventBus();
        var id = bus.Subscribe<FeedbackReceivedEvent>(_ => { });
        bus.Subscribe<FeedbackReceivedEvent>(_ => { });

        bus.Unsubscribe(id);

        Assert.That(bus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.EqualTo(1));
    }

    [Test]
    public void Publish_WithNullEvent_ThrowsArgumentNullException()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() => bus.Publish<Z21ConnectionEstablishedEvent>(null!));
    }

    [Test]
    public void Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() => bus.Subscribe<Z21ConnectionEstablishedEvent>(null!));
    }

    [Test]
    public void Publish_PassesEventInstanceToHandler()
    {
        var bus = new EventBus();
        FeedbackReceivedEvent? captured = null;

        bus.Subscribe<FeedbackReceivedEvent>(e => captured = e);
        var evt = new FeedbackReceivedEvent(42);
        bus.Publish(evt);

        Assert.That(captured, Is.SameAs(evt));
        Assert.That(captured!.InPort, Is.EqualTo(42));
    }
}
