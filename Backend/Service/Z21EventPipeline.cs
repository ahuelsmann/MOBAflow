// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Events;

using Microsoft.Extensions.Logging;

using System.Threading.Channels;

/// <summary>
/// Immutable diagnostics for the ordered Z21 event pipeline.
/// </summary>
/// <param name="Capacity">Maximum number of queued events.</param>
/// <param name="QueueDepth">Current number of queued events.</param>
/// <param name="PeakQueueDepth">Highest observed queue depth.</param>
/// <param name="EnqueuedEvents">Number of events accepted by the pipeline.</param>
/// <param name="PublishedEvents">Number of events published successfully.</param>
/// <param name="RejectedEvents">Number of incoming events rejected while full or stopped.</param>
/// <param name="DispatchFailures">Number of failures raised by the event-bus boundary.</param>
/// <param name="SubscriberFailures">Number of isolated subscriber failures reported by the event bus.</param>
/// <param name="ShutdownTimeouts">Number of drain operations that exceeded their timeout.</param>
public sealed record Z21EventPipelineSnapshot(
    int Capacity,
    int QueueDepth,
    int PeakQueueDepth,
    long EnqueuedEvents,
    long PublishedEvents,
    long RejectedEvents,
    long DispatchFailures,
    long SubscriberFailures,
    long ShutdownTimeouts);

/// <summary>
/// Publishes accepted Z21 events through one bounded FIFO consumer.
/// </summary>
public sealed class Z21EventPipeline : IAsyncDisposable
{
    /// <summary>
    /// Default number of events that can wait for publication.
    /// </summary>
    public const int DefaultCapacity = 1024;

    /// <summary>
    /// Default time allowed for an orderly drain during asynchronous disposal.
    /// </summary>
    public static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly Channel<QueuedEvent> _channel;
    private readonly CancellationTokenSource _consumerCancellation = new();
    private readonly IEventBus _eventBus;
    private readonly ILogger? _logger;
    private readonly object _queueLock = new();
    private readonly Task _consumerTask;
    private bool _acceptingEvents = true;
    private int _queueDepth;
    private int _peakQueueDepth;
    private long _enqueuedEvents;
    private long _publishedEvents;
    private long _rejectedEvents;
    private long _dispatchFailures;
    private long _shutdownTimeouts;

    /// <summary>
    /// Initializes a new ordered event pipeline.
    /// </summary>
    /// <param name="eventBus">Destination event bus.</param>
    /// <param name="logger">Optional structured logger.</param>
    /// <param name="capacity">Maximum number of waiting events.</param>
    public Z21EventPipeline(IEventBus eventBus, ILogger? logger = null, int capacity = DefaultCapacity)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _eventBus = eventBus;
        _logger = logger;
        Capacity = capacity;
        _channel = Channel.CreateBounded<QueuedEvent>(new BoundedChannelOptions(capacity)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
        _consumerTask = ConsumeAsync();
    }

    /// <summary>
    /// Gets the configured queue capacity.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets a task that completes when the single consumer has stopped.
    /// </summary>
    public Task Completion => _consumerTask;

    /// <summary>
    /// Attempts to enqueue an event without blocking the caller.
    /// </summary>
    /// <typeparam name="TEvent">Concrete event type used for EventBus routing.</typeparam>
    /// <param name="event">Event to publish.</param>
    /// <returns><see langword="true"/> when accepted; otherwise <see langword="false"/>.</returns>
    public bool TryEnqueue<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent).Name;
        lock (_queueLock)
        {
            if (!_acceptingEvents || !_channel.Writer.TryWrite(new QueuedEvent(eventType, () => _eventBus.Publish(@event))))
            {
                Interlocked.Increment(ref _rejectedEvents);
                _logger?.LogWarning(
                    "Z21 event rejected. EventType={EventType}, QueueDepth={QueueDepth}, Capacity={Capacity}",
                    eventType,
                    _queueDepth,
                    Capacity);
                return false;
            }

            _queueDepth++;
            _peakQueueDepth = Math.Max(_peakQueueDepth, _queueDepth);
            Interlocked.Increment(ref _enqueuedEvents);
            return true;
        }
    }

    /// <summary>
    /// Gets a consistent snapshot of current pipeline diagnostics.
    /// </summary>
    public Z21EventPipelineSnapshot GetSnapshot()
    {
        lock (_queueLock)
        {
            return new Z21EventPipelineSnapshot(
                Capacity,
                _queueDepth,
                _peakQueueDepth,
                Interlocked.Read(ref _enqueuedEvents),
                Interlocked.Read(ref _publishedEvents),
                Interlocked.Read(ref _rejectedEvents),
                Interlocked.Read(ref _dispatchFailures),
                (_eventBus as IEventBusDiagnostics)?.HandlerFailureCount ?? 0,
                Interlocked.Read(ref _shutdownTimeouts));
        }
    }

    /// <summary>
    /// Stops accepting events and drains accepted work within the supplied timeout.
    /// </summary>
    /// <param name="timeout">Maximum drain duration.</param>
    /// <param name="cancellationToken">Cancels the caller's wait and the consumer.</param>
    public async Task StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        CompleteWriter();
        try
        {
            await _consumerTask.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Interlocked.Increment(ref _shutdownTimeouts);
            _logger?.LogWarning(
                "Z21 event pipeline drain timed out. QueueDepth={QueueDepth}, TimeoutMilliseconds={TimeoutMilliseconds}",
                GetSnapshot().QueueDepth,
                timeout.TotalMilliseconds);
            CancelConsumer();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CancelConsumer();
            throw;
        }
    }

    /// <summary>
    /// Stops accepting events and cancels the consumer without waiting.
    /// </summary>
    public void Cancel()
    {
        CompleteWriter();
        CancelConsumer();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync(DefaultShutdownTimeout).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private void CompleteWriter()
    {
        lock (_queueLock)
        {
            if (!_acceptingEvents)
            {
                return;
            }

            _acceptingEvents = false;
            _channel.Writer.TryComplete();
        }
    }

    private void CancelConsumer()
    {
        try
        {
            _consumerCancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The consumer completed between the shutdown decision and cancellation.
        }
    }

    private async Task ConsumeAsync()
    {
        try
        {
            await foreach (var queuedEvent in _channel.Reader.ReadAllAsync(_consumerCancellation.Token).ConfigureAwait(false))
            {
                lock (_queueLock)
                {
                    _queueDepth--;
                }

                try
                {
                    queuedEvent.Publish();
                    Interlocked.Increment(ref _publishedEvents);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _dispatchFailures);
                    _logger?.LogError(ex, "Z21 event dispatch failed. EventType={EventType}", queuedEvent.EventType);
                }
            }
        }
        catch (OperationCanceledException) when (_consumerCancellation.IsCancellationRequested)
        {
            // Cancellation is the documented fallback when draining cannot complete.
        }
        finally
        {
            _consumerCancellation.Dispose();
        }
    }

    private sealed record QueuedEvent(string EventType, Action Publish);
}
