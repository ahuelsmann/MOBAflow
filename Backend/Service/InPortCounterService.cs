// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Configuration;
using Common.Runtime;
using Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>One accepted activation and its immutable global counter value.</summary>
public sealed class InPortCountedEventArgs(InPortCounterSnapshot snapshot, Guid correlationId, long generation = 0, long inputRevision = 0) : EventArgs
{
    /// <summary>Counter value after accepting this activation.</summary>
    public InPortCounterSnapshot Snapshot { get; } = snapshot;

    /// <summary>Source activation identity propagated to workflow execution.</summary>
    public Guid CorrelationId { get; } = correlationId;

    /// <summary>Counter generation, changed by an explicit reset.</summary>
    public long Generation { get; } = generation;

    /// <summary>Input revision, changed by a correction or removal of this input.</summary>
    public long InputRevision { get; } = inputRevision;
}

/// <summary>
/// Owns application-lifetime input counts independently of projects, journeys, and UI pages.
/// Only an explicit reset clears counts.
/// </summary>
public sealed partial class InPortCounterService : IDisposable, IAsyncDisposable
{
    private readonly Lock _sync = new();
    private readonly IZ21 _z21;
    private readonly AppSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InPortCounterService> _logger;
    private readonly Dictionary<uint, InPortCounterSnapshot> _counters = [];
    private readonly Dictionary<uint, long> _inputRevisions = [];
    private readonly Queue<InPortCountedEventArgs> _pendingCounts = new();
    private EventHandler<InPortCountedEventArgs>? _journeyFeedbackHandler;
    private long _generation;
    private bool _publishingCounts;
    private bool _disposed;

    /// <summary>Subscribes once to source activations for this application lifetime.</summary>
    public InPortCounterService(
        IZ21 z21,
        AppSettings settings,
        TimeProvider? timeProvider = null,
        ILogger<InPortCounterService>? logger = null,
        IInPortCounterStore? store = null)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(settings);
        _z21 = z21;
        _settings = settings;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<InPortCounterService>.Instance;
        _store = store;
        _initialized = store is null;
        for (uint inPort = 1; inPort <= settings.Counter.CountOfFeedbackPoints; inPort++)
        {
            _counters[inPort] = new InPortCounterSnapshot(inPort, 0, null, null);
        }

        _settings.Counter.FeedbackPointsChanged += OnFeedbackPointsChanged;
        _z21.Received += OnFeedbackReceived;
    }

    /// <summary>Raised once for each activation accepted by the configured timer filter.</summary>
    public event EventHandler<InPortCountedEventArgs>? Counted;

    /// <summary>Raised when counts change.</summary>
    public event EventHandler? SnapshotChanged;

    /// <summary>Atomically replaces the sole journey evaluator for this application's active project.</summary>
    internal void SetJourneyFeedbackHandler(EventHandler<InPortCountedEventArgs> handler)
    {
        lock (_sync)
        {
            _journeyFeedbackHandler = handler;
        }
    }

    /// <summary>Releases an evaluator without removing a replacement installed by a newer project.</summary>
    internal void RemoveJourneyFeedbackHandler(EventHandler<InPortCountedEventArgs> handler)
    {
        lock (_sync)
        {
            if (_journeyFeedbackHandler == handler)
                _journeyFeedbackHandler = null;
        }
    }

    /// <summary>Changes with every explicit reset so queued counts from before the reset can be ignored.</summary>
    public long Generation
    {
        get
        {
            lock (_sync)
            {
                return _generation;
            }
        }
    }

    /// <summary>Returns detached, immutable statistics ordered by input number.</summary>
    public IReadOnlyList<InPortCounterSnapshot> GetSnapshot()
    {
        lock (_sync)
        {
            return Array.AsReadOnly(_counters.Values.OrderBy(counter => counter.InPort).ToArray());
        }
    }

    /// <summary>Queues work for a current activation atomically with respect to counter resets.</summary>
    internal void QueueIfCurrent(InPortCountedEventArgs activation, System.Action enqueue)
    {
        lock (_sync)
        {
            if (IsCurrent(activation))
            {
                enqueue();
            }
        }
    }

    /// <summary>Checks whether an activation still belongs to the current input value.</summary>
    internal bool IsCurrent(InPortCountedEventArgs activation)
    {
        lock (_sync)
        {
            return !_disposed && activation.Generation == _generation
                && _counters.ContainsKey(activation.Snapshot.InPort)
                && activation.InputRevision == _inputRevisions.GetValueOrDefault(activation.Snapshot.InPort);
        }
    }

    /// <summary>Corrects one configured count without publishing a feedback activation.</summary>
    public void Set(uint inPort, ulong value)
    {
        lock (_sync)
        {
            EnsureInitialized();
            if (!_counters.ContainsKey(inPort))
                throw new ArgumentOutOfRangeException(nameof(inPort), "The input is not configured.");
            _counters[inPort] = new InPortCounterSnapshot(inPort, value, null, null);
            _inputRevisions[inPort] = _inputRevisions.GetValueOrDefault(inPort) + 1;
            QueueSaveLocked();
        }

        PublishSnapshotChanged();
    }

    /// <summary>Resets all counts and timer history; also replaces saved counts that could not be loaded.</summary>
    public void ResetAll()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_initialized && _loadFailed)
            {
                // The explicit reset is the operator's recovery from an unreadable counter file.
                ReconcileInputsLocked();
                _initialized = true;
                _loadFailed = false;
                _persistenceError = null;
            }

            EnsureInitialized();
            foreach (var inPort in _counters.Keys.ToArray())
            {
                _counters[inPort] = new InPortCounterSnapshot(inPort, 0, null, null);
            }

            _generation++;
            _pendingCounts.Clear();
            QueueSaveLocked();
        }

        PublishSnapshotChanged();
    }

    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        bool startPublishing;
        lock (_sync)
        {
            if (_disposed || feedback.InPort <= 0)
            {
                return;
            }

            if (!_initialized)
            {
                // After a failed load, activations cannot be counted against unknown saved values.
                if (!_loadFailed) _feedbackBeforeLoad.Enqueue((feedback, _timeProvider.GetLocalNow()));
                return;
            }

            AcceptFeedbackLocked(feedback, _timeProvider.GetLocalNow());
            startPublishing = !_publishingCounts && _pendingCounts.Count > 0;
            if (startPublishing) _publishingCounts = true;
        }

        if (startPublishing)
        {
            PublishPendingCounts();
        }
    }

    private void AcceptFeedbackLocked(FeedbackResult feedback, DateTimeOffset now)
    {
        var inPort = checked((uint)feedback.InPort);
        if (!_counters.TryGetValue(inPort, out var previous)) return;
        var elapsed = previous.LastFeedbackTime is DateTimeOffset lastFeedbackTime
            ? now - lastFeedbackTime
            : (TimeSpan?)null;
        if (_settings.Counter.UseTimerFilter
            && elapsed.HasValue
            && elapsed.Value.TotalSeconds < _settings.Counter.TimerIntervalSeconds)
        {
            return;
        }

        // Do not wrap a saturated counter back to zero and accidentally match a new event.
        if (previous.Count == ulong.MaxValue)
        {
            return;
        }

        var snapshot = new InPortCounterSnapshot(inPort, previous.Count + 1, now, elapsed);
        _counters[inPort] = snapshot;
        QueueSaveLocked();
        _pendingCounts.Enqueue(new InPortCountedEventArgs(snapshot, feedback.CorrelationId, _generation,
            _inputRevisions.GetValueOrDefault(inPort)));
    }

    private void PublishPendingCounts()
    {
        while (true)
        {
            InPortCountedEventArgs next;
            EventHandler<InPortCountedEventArgs>? journeyHandler;
            lock (_sync)
            {
                if (!_pendingCounts.TryDequeue(out next!))
                {
                    _publishingCounts = false;
                    return;
                }

                journeyHandler = _journeyFeedbackHandler;
            }

            foreach (var subscriber in Delegate.EnumerateInvocationList(Counted))
            {
                InvokeCountedSubscriber(subscriber, next);
            }

            if (journeyHandler is not null)
                InvokeCountedSubscriber(journeyHandler, next);

            PublishSnapshotChanged();
        }
    }

    private void InvokeCountedSubscriber(EventHandler<InPortCountedEventArgs> subscriber, InPortCountedEventArgs next)
    {
        try
        {
            subscriber(this, next);
        }
        catch (Exception ex)
        {
            LogCountedSubscriberFailed(_logger, ex, next.Snapshot.InPort);
        }
    }

    private void PublishSnapshotChanged()
    {
        foreach (var subscriber in Delegate.EnumerateInvocationList(SnapshotChanged))
        {
            try
            {
                subscriber(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogSnapshotSubscriberFailed(_logger, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "InPort counter subscriber failed for input {InPort}")]
    private static partial void LogCountedSubscriberFailed(ILogger logger, Exception exception, uint inPort);

    [LoggerMessage(Level = LogLevel.Warning, Message = "InPort counter snapshot subscriber failed")]
    private static partial void LogSnapshotSubscriberFailed(ILogger logger, Exception exception);

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _pendingCounts.Clear();
            _feedbackBeforeLoad.Clear();
            _journeyFeedbackHandler = null;
        }

        _z21.Received -= OnFeedbackReceived;
        _settings.Counter.FeedbackPointsChanged -= OnFeedbackPointsChanged;
        GC.SuppressFinalize(this);
    }
}
