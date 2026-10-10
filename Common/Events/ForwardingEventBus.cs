// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// The event bus of one project runtime. Runtime handlers subscribe to it; while <see cref="IsForwarding"/> is set,
/// every published event is also published to the application bus so the UI shows this runtime.
/// </summary>
/// <param name="local">The bus that runtime handlers subscribe to.</param>
/// <param name="target">The application bus that receives events while forwarding.</param>
/// <param name="deliver">
/// Runs the delivery of an event, for example on the UI thread; null delivers on the publishing thread. Whether the
/// event is forwarded is decided when it is delivered, so an event queued before the project was deselected never
/// reaches the application bus.
/// </param>
public sealed class ForwardingEventBus(IEventBus local, IEventBus target, Action<Action>? deliver = null) : IEventBus
{
    private readonly IEventBus _local = local ?? throw new ArgumentNullException(nameof(local));
    private readonly IEventBus _target = target ?? throw new ArgumentNullException(nameof(target));
    private readonly Action<Action> _deliver = deliver ?? (action => action());
    private volatile bool _isForwarding;

    /// <summary>
    /// Gets or sets a value indicating whether events are also published to the application bus.
    /// </summary>
    public bool IsForwarding
    {
        get => _isForwarding;
        set => _isForwarding = value;
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        _deliver(() =>
        {
            _local.Publish(@event);
            if (_isForwarding)
            {
                _target.Publish(@event);
            }
        });
    }

    /// <inheritdoc />
    public Guid Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent => _local.Subscribe(handler);

    /// <inheritdoc />
    public void Unsubscribe(Guid subscriptionId) => _local.Unsubscribe(subscriptionId);

    /// <inheritdoc />
    public int GetSubscriberCount<TEvent>() where TEvent : class, IEvent => _local.GetSubscriberCount<TEvent>();
}
