// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Configuration;
using Common.Runtime;
using Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Frozen;

/// <summary>One accepted activation and its immutable global counter value.</summary>
public sealed class InPortCountedEventArgs(InPortCounterSnapshot snapshot, Guid correlationId, long generation = 0) : EventArgs
{
    /// <summary>Counter value after accepting this activation.</summary>
    public InPortCounterSnapshot Snapshot { get; } = snapshot;

    /// <summary>Source activation identity propagated to workflow execution.</summary>
    public Guid CorrelationId { get; } = correlationId;

    /// <summary>Counter generation, changed by an explicit reset.</summary>
    public long Generation { get; } = generation;
}

/// <summary>
/// Owns application-lifetime input counts independently of projects, journeys, and UI pages.
/// Only an explicit reset clears counts; registering a journey captures its bases atomically.
/// </summary>
public sealed class InPortCounterService : IDisposable
{
    private readonly object _sync = new();
    private readonly IZ21 _z21;
    private readonly AppSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InPortCounterService> _logger;
    private readonly Dictionary<uint, InPortCounterSnapshot> _counters = [];
    private readonly HashSet<Guid> _activeRegistrations = [];
    private readonly Queue<InPortCountedEventArgs> _pendingCounts = new();
    private long _generation;
    private bool _publishingCounts;
    private bool _disposed;

    /// <summary>Subscribes once to source activations for this application lifetime.</summary>
    public InPortCounterService(
        IZ21 z21,
        AppSettings settings,
        TimeProvider? timeProvider = null,
        ILogger<InPortCounterService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(settings);
        _z21 = z21;
        _settings = settings;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<InPortCounterService>.Instance;
        for (uint inPort = 1; inPort <= settings.Counter.CountOfFeedbackPoints; inPort++)
        {
            _counters[inPort] = new InPortCounterSnapshot(inPort, 0, null, null);
        }

        _z21.Received += OnFeedbackReceived;
    }

    /// <summary>Raised once for each activation accepted by the configured timer filter.</summary>
    public event EventHandler<InPortCountedEventArgs>? Counted;

    /// <summary>Raised when counts or permission to reset them change.</summary>
    public event EventHandler? SnapshotChanged;

    /// <summary>Whether a journey currently prevents an explicit counter reset.</summary>
    public bool HasActiveJourneys
    {
        get
        {
            lock (_sync)
            {
                return _activeRegistrations.Count != 0;
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

    /// <summary>Resets counts and timer history only when no journey is active.</summary>
    public bool TryResetAll()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_activeRegistrations.Count != 0)
            {
                return false;
            }

            foreach (var inPort in _counters.Keys.ToArray())
            {
                _counters[inPort] = new InPortCounterSnapshot(inPort, 0, null, null);
            }

            _generation++;
            _pendingCounts.Clear();
        }

        PublishSnapshotChanged();
        return true;
    }

    internal InPortCounterRun BeginJourneyRun()
    {
        InPortCounterRun run;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var registrationId = Guid.NewGuid();
            _activeRegistrations.Add(registrationId);
            run = new InPortCounterRun(
                registrationId,
                _counters.ToFrozenDictionary(pair => pair.Key, pair => pair.Value.Count),
                _generation);
        }

        PublishSnapshotChanged();
        return run;
    }

    internal void EndJourneyRun(Guid registrationId)
    {
        bool changed;
        lock (_sync)
        {
            changed = _activeRegistrations.Remove(registrationId);
        }

        if (changed)
        {
            PublishSnapshotChanged();
        }
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

            var inPort = checked((uint)feedback.InPort);
            _counters.TryGetValue(inPort, out var previous);
            var now = _timeProvider.GetLocalNow();
            var elapsed = previous?.LastFeedbackTime is DateTimeOffset lastFeedbackTime
                ? now - lastFeedbackTime
                : (TimeSpan?)null;
            if (_settings.Counter.UseTimerFilter
                && elapsed.HasValue
                && elapsed.Value.TotalSeconds < _settings.Counter.TimerIntervalSeconds)
            {
                return;
            }

            // Do not wrap a saturated counter back to zero and accidentally match a new event.
            if (previous?.Count == ulong.MaxValue)
            {
                return;
            }

            var snapshot = new InPortCounterSnapshot(inPort, (previous?.Count ?? 0) + 1, now, elapsed);
            _counters[inPort] = snapshot;
            _pendingCounts.Enqueue(new InPortCountedEventArgs(snapshot, feedback.CorrelationId, _generation));
            startPublishing = !_publishingCounts;
            _publishingCounts = true;
        }

        if (startPublishing)
        {
            PublishPendingCounts();
        }
    }

    private void PublishPendingCounts()
    {
        while (true)
        {
            InPortCountedEventArgs next;
            lock (_sync)
            {
                if (!_pendingCounts.TryDequeue(out next!))
                {
                    _publishingCounts = false;
                    return;
                }
            }

            foreach (var subscriber in Counted?.GetInvocationList() ?? [])
            {
                try
                {
                    ((EventHandler<InPortCountedEventArgs>)subscriber)(this, next);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "InPort counter subscriber failed for input {InPort}", next.Snapshot.InPort);
                }
            }

            PublishSnapshotChanged();
        }
    }

    private void PublishSnapshotChanged()
    {
        foreach (var subscriber in SnapshotChanged?.GetInvocationList() ?? [])
        {
            try
            {
                ((EventHandler)subscriber)(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "InPort counter snapshot subscriber failed");
            }
        }
    }

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
        }

        _z21.Received -= OnFeedbackReceived;
        GC.SuppressFinalize(this);
    }
}

internal sealed record InPortCounterRun(Guid RegistrationId, IReadOnlyDictionary<uint, ulong> Bases, long Generation);
