// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Common.Events;
using Interface;
using Microsoft.Extensions.Logging;

/// <summary>
/// Captures allow-listed events before forwarding the original instance unchanged.
/// </summary>
public sealed class RecordingEventBusDecorator : IEventBus, IEventBusDiagnostics
{
    private readonly IEventBus _inner;
    private readonly IRecordingSessionService _recordingSession;
    private readonly RecordingEventMapperRegistry _mapperRegistry;
    private readonly ILogger<RecordingEventBusDecorator> _logger;

    /// <summary>Initializes the capture boundary around an existing event bus.</summary>
    public RecordingEventBusDecorator(
        IEventBus inner,
        IRecordingSessionService recordingSession,
        RecordingEventMapperRegistry mapperRegistry,
        ILogger<RecordingEventBusDecorator> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recordingSession = recordingSession ?? throw new ArgumentNullException(nameof(recordingSession));
        _mapperRegistry = mapperRegistry ?? throw new ArgumentNullException(nameof(mapperRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public long HandlerFailureCount => (_inner as IEventBusDiagnostics)?.HandlerFailureCount ?? 0;

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        try
        {
            if (_mapperRegistry.TryMap(@event, out var projection))
            {
                _recordingSession.TryRecord(projection!);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Recording mapper failed for {EventType}", typeof(TEvent).Name);
        }

        _inner.Publish(@event);
    }

    /// <inheritdoc />
    public Guid Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class, IEvent =>
        _inner.Subscribe(handler);

    /// <inheritdoc />
    public void Unsubscribe(Guid subscriptionId) => _inner.Unsubscribe(subscriptionId);

    /// <inheritdoc />
    public int GetSubscriberCount<TEvent>() where TEvent : class, IEvent =>
        _inner.GetSubscriberCount<TEvent>();
}