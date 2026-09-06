// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

/// <summary>
/// Central event bus for publishing and subscribing to application-wide events.
/// Decouples backend services (Z21, Feedback) from ViewModels.
/// 
/// Usage:
/// - Backend publishes: eventBus.Publish(new LocomotiveSpeedChangedEvent { ... })
/// - ViewModel subscribes: eventBus.Subscribe&lt;LocomotiveSpeedChangedEvent&gt;(OnSpeedChanged)
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="event">The event instance to publish</param>
    void Publish<TEvent>(TEvent @event) where TEvent : class, IEvent;

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to</typeparam>
    /// <param name="handler">Action called when event is published</param>
    /// <returns>Subscription ID for later unsubscription</returns>
    Guid Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent;

    /// <summary>
    /// Unsubscribes a previously registered handler.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID returned from Subscribe</param>
    void Unsubscribe(Guid subscriptionId);

    /// <summary>
    /// Gets the count of subscribers for a specific event type.
    /// Useful for diagnostics and testing.
    /// </summary>
    int GetSubscriberCount<TEvent>() where TEvent : class, IEvent;
}

/// <summary>
/// Optional diagnostics exposed by event-bus implementations and decorators.
/// </summary>
public interface IEventBusDiagnostics
{
    /// <summary>
    /// Gets the total number of subscriber invocations that failed.
    /// </summary>
    long HandlerFailureCount { get; }
}

/// <summary>
/// Default implementation of IEventBus using dictionary-based subscriptions.
/// Thread-safe for Publish and Subscribe operations.
/// Uses WeakReferences to prevent memory leaks - dead ViewModel references are automatically cleaned.
/// </summary>
public sealed class EventBus : IEventBus, IEventBusDiagnostics
{
    // Tuple: (subscription ID, WeakReference to target object, handler delegate)
    private readonly Dictionary<Type, List<(Guid Id, WeakReference<object>? TargetRef, Delegate Handler)>> _subscriptions = [];
    private readonly object _lock = new();
    private readonly ILogger<EventBus> _logger;
    private long _handlerFailureCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBus"/> class.
    /// </summary>
    /// <param name="logger">Logger for handler failures (handlers are isolated; failures do not propagate).</param>
    public EventBus(ILogger<EventBus> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public long HandlerFailureCount => Interlocked.Read(ref _handlerFailureCount);

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent);

        // Snapshot the live handlers under the lock, then invoke them outside the lock.
        // Invoking under the lock would (a) block any concurrent Subscribe/Unsubscribe for the
        // full handler duration and (b) deadlock on re-entrant publishes, because a handler that
        // publishes another event would re-enter the non-reentrant lock on the same thread.
        Delegate[] handlersSnapshot;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
                return;

            // Remove dead references (targets that have been garbage collected)
            handlers.RemoveAll(h => h.TargetRef != null && !h.TargetRef.TryGetTarget(out _));

            if (handlers.Count == 0)
                return;

            handlersSnapshot = new Delegate[handlers.Count];
            for (var i = 0; i < handlers.Count; i++)
                handlersSnapshot[i] = handlers[i].Handler;
        }

        foreach (var handler in handlersSnapshot)
        {
            try
            {
                ((Action<TEvent>)handler)(@event);
            }
            catch (Exception ex)
            {
                // Isolate failures so other handlers still run; surface severity for diagnostics.
                Interlocked.Increment(ref _handlerFailureCount);
                _logger.LogError(ex, "EventBus handler failed for {EventType}", eventType.Name);
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine($"EventBus handler failed for {eventType.Name}: {ex}");
                }
            }
        }
    }

    /// <inheritdoc />
    public Guid Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var subscriptionId = Guid.NewGuid();
        var eventType = typeof(TEvent);

        // Capture the target object (ViewModel) via WeakReference
        // If handler is a static method or lambda without closure, target is null
        var target = handler.Target;
        var targetRef = target != null ? new WeakReference<object>(target) : null;

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers = [];
                _subscriptions[eventType] = handlers;
            }

            handlers.Add((subscriptionId, targetRef, handler));
        }

        return subscriptionId;
    }

    /// <inheritdoc />
    public void Unsubscribe(Guid subscriptionId)
    {
        lock (_lock)
        {
            foreach (var handlers in _subscriptions.Values)
            {
                handlers.RemoveAll(h => h.Id == subscriptionId);
            }
        }
    }

    /// <inheritdoc />
    public int GetSubscriberCount<TEvent>() where TEvent : class, IEvent
    {
        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
                return 0;

            // Count only live references
            var liveCount = handlers.Count(h => h.TargetRef == null || h.TargetRef.TryGetTarget(out _));
            return liveCount;
        }
    }
}
