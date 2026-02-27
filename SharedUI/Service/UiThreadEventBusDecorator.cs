// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Common.Events;
using Interface;

/// <summary>
/// Decorator for IEventBus: executes all handler calls on the UI thread.
/// ViewModels for EventBus subscriptions no longer need a dispatcher –
/// the thread boundary is handled centrally here.
/// </summary>
public sealed class UiThreadEventBusDecorator : IEventBus
{
    private readonly IEventBus _inner;
    private readonly IUiDispatcher _dispatcher;

    public UiThreadEventBusDecorator(IEventBus inner, IUiDispatcher dispatcher)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        _dispatcher.InvokeOnUi(() => _inner.Publish(@event));
    }

    /// <inheritdoc />
    public Guid Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent
        => _inner.Subscribe(handler);

    /// <inheritdoc />
    public void Unsubscribe(Guid subscriptionId)
        => _inner.Unsubscribe(subscriptionId);

    /// <inheritdoc />
    public int GetSubscriberCount<TEvent>() where TEvent : class, IEvent
        => _inner.GetSubscriberCount<TEvent>();
}
