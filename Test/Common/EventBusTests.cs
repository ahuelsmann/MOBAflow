// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Events;

/// <summary>
/// Unit tests for EventBus (IEventBus implementation).
/// Covers Publish, Subscribe, Unsubscribe, and GetSubscriberCount.
/// </summary>
[TestFixture]
[Category("Unit")]
internal class EventBusTests
{
    [Test]
    public void Constructor_RejectsNullLoggerWithParameterName()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new EventBus(null!));

        Assert.That(exception!.ParamName, Is.EqualTo("logger"));
    }

    [Test]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);

        Assert.DoesNotThrow(() => bus.Publish(new Z21ConnectionEstablishedEvent()));
    }

    [Test]
    public void Publish_InvokesSubscribedHandler()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var received = false;

        bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => received = true);
        bus.Publish(new Z21ConnectionEstablishedEvent());

        Assert.That(received, Is.True);
    }

    [Test]
    public void Publish_InvokesMultipleHandlersForSameEventType()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var count = 0;

        bus.Subscribe<FeedbackReceivedEvent>(_ => count++);
        bus.Subscribe<FeedbackReceivedEvent>(_ => count += 10);
        bus.Publish(new FeedbackReceivedEvent(1));

        Assert.That(count, Is.EqualTo(11));
    }

    [Test]
    public void Publish_RecordsFailingSubscriberAndContinuesWithRemainingHandlers()
    {
        var logger = new RecordingLogger<EventBus>();
        var bus = new EventBus(logger);
        var laterHandlerInvoked = false;
        var failure = new InvalidOperationException("Expected test failure");
        bus.Subscribe<FeedbackReceivedEvent>(_ => throw failure);
        bus.Subscribe<FeedbackReceivedEvent>(_ => laterHandlerInvoked = true);

        bus.Publish(new FeedbackReceivedEvent(1));

        Assert.Multiple(() =>
        {
            Assert.That(laterHandlerInvoked, Is.True);
            Assert.That(logger.Levels, Does.Contain(LogLevel.Error));
            Assert.That(logger.Messages, Has.One.EqualTo("EventBus handler failed for FeedbackReceivedEvent"));
            Assert.That(logger.Exceptions, Has.One.SameAs(failure));
            Assert.That(bus.HandlerFailureCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Publish_StaticHandlerRemainsSubscribedUntilRemovedAndFailuresAreIsolated()
    {
        var logger = new RecordingLogger<EventBus>();
        var bus = new EventBus(logger);
        // A named static method has a null delegate target, unlike a compiler-generated lambda target.
        Action<FeedbackReceivedEvent> staticHandler = ThrowFromStaticHandler;
        var staticId = bus.Subscribe(staticHandler);
        var received = new List<FeedbackReceivedEvent>();
        bus.Subscribe<FeedbackReceivedEvent>(received.Add);

        Assert.That(bus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.EqualTo(2));

        var firstEvent = new FeedbackReceivedEvent(1);
        var secondEvent = new FeedbackReceivedEvent(2);
        bus.Publish(firstEvent);
        bus.Publish(secondEvent);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(received, Has.Count.EqualTo(2));
            Assert.That(received[0], Is.SameAs(firstEvent));
            Assert.That(received[1], Is.SameAs(secondEvent));
            Assert.That(bus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.EqualTo(2));
            Assert.That(bus.HandlerFailureCount, Is.EqualTo(2));
            Assert.That(logger.Levels, Has.Count.EqualTo(2).And.All.EqualTo(LogLevel.Error));
        }

        bus.Unsubscribe(staticId);
        var thirdEvent = new FeedbackReceivedEvent(3);
        bus.Publish(thirdEvent);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.EqualTo(1));
            Assert.That(received, Has.Count.EqualTo(3));
            Assert.That(received[2], Is.SameAs(thirdEvent));
            Assert.That(bus.HandlerFailureCount, Is.EqualTo(2));
            Assert.That(logger.Levels, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void Publish_DoesNotInvokeHandlersForOtherEventTypes()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
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
        var bus = new EventBus(NullLogger<EventBus>.Instance);

        var id = bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => { });

        Assert.That(id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Subscribe_ReturnsUniqueIds()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var id1 = bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => { });
        var id2 = bus.Subscribe<Z21ConnectionEstablishedEvent>(_ => { });

        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void Unsubscribe_StopsReceivingEvents()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
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
        var bus = new EventBus(NullLogger<EventBus>.Instance);
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
        var bus = new EventBus(NullLogger<EventBus>.Instance);

        Assert.That(bus.GetSubscriberCount<Z21ConnectionEstablishedEvent>(), Is.Zero);
    }

    [Test]
    public void GetSubscriberCount_ReturnsCorrectCount_AfterSubscribe()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);

        bus.Subscribe<FeedbackReceivedEvent>(_ => { });
        bus.Subscribe<FeedbackReceivedEvent>(_ => { });

        Assert.That(bus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.EqualTo(2));
    }

    [Test]
    public void GetSubscriberCount_ReturnsCorrectCount_AfterUnsubscribe()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var id = bus.Subscribe<FeedbackReceivedEvent>(_ => { });
        bus.Subscribe<FeedbackReceivedEvent>(_ => { });

        bus.Unsubscribe(id);

        Assert.That(bus.GetSubscriberCount<FeedbackReceivedEvent>(), Is.EqualTo(1));
    }

    [Test]
    public void Publish_WithNullEvent_ThrowsArgumentNullException()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);

        Assert.Throws<ArgumentNullException>(() => bus.Publish<Z21ConnectionEstablishedEvent>(null!));
    }

    [Test]
    public void Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);

        Assert.Throws<ArgumentNullException>(() => bus.Subscribe<Z21ConnectionEstablishedEvent>(null!));
    }

    [Test]
    public void Publish_PassesEventInstanceToHandler()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        FeedbackReceivedEvent? captured = null;

        bus.Subscribe<FeedbackReceivedEvent>(e => captured = e);
        var evt = new FeedbackReceivedEvent(42);
        bus.Publish(evt);

        Assert.That(captured, Is.SameAs(evt));
        Assert.That(captured!.InPort, Is.EqualTo(42));
    }

    private static void ThrowFromStaticHandler(FeedbackReceivedEvent _) =>
        throw new InvalidOperationException("Expected static-handler test failure");

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogLevel> Levels { get; } = [];
        public List<string> Messages { get; } = [];
        public List<Exception?> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Levels.Add(logLevel);
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }
}