// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.TrackPlan;

using Common.Events;
using Domain;

/// <summary>Projects Z21 feedback into runtime-only track state without mutating the persisted layout.</summary>
public sealed class TrackPlanRailroadStateProjector : IDisposable
{
    private readonly object _lifecycleGate = new();
    private readonly IEventBus _eventBus;
    private readonly ITrackFeedbackLookup _feedbackLookup;
    private readonly RailroadState _state;
    private readonly List<Guid> _subscriptionIds = [];
    private Timer? _feedbackExpiryTimer;
    private TimeSpan _feedbackTimeout = TimeSpan.FromSeconds(5);
    private bool _disposed;

    /// <summary>Maximum time a feedback occupancy remains active without another feedback event.</summary>
    public TimeSpan FeedbackTimeout
    {
        get
        {
            lock (_lifecycleGate)
                return _feedbackTimeout;
        }
        set
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));
            lock (_lifecycleGate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _feedbackTimeout = value;
            }
        }
    }

    public TrackPlanRailroadStateProjector(IEventBus eventBus, ITrackFeedbackLookup feedbackLookup, RailroadState state)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _feedbackLookup = feedbackLookup ?? throw new ArgumentNullException(nameof(feedbackLookup));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void Activate()
    {
        lock (_lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_subscriptionIds.Count != 0)
                return;

            _subscriptionIds.Add(_eventBus.Subscribe<FeedbackReceivedEvent>(OnFeedbackReceived));
            _subscriptionIds.Add(_eventBus.Subscribe<SwitchPositionChangedEvent>(OnSwitchPositionChanged));
            _subscriptionIds.Add(_eventBus.Subscribe<SignalAspectChangedEvent>(OnSignalAspectChanged));
            _feedbackExpiryTimer = new Timer(_ => ExpireFeedbackNow(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    private void OnFeedbackReceived(FeedbackReceivedEvent feedback)
    {
        lock (_lifecycleGate)
        {
            if (_disposed)
                return;

            var timestamp = DateTimeOffset.UtcNow;
            foreach (var trackId in _feedbackLookup.GetTrackIdsByFeedbackInPort(feedback.InPort))
                _state.MarkFeedback(trackId, timestamp);
        }
    }

    private void OnSwitchPositionChanged(SwitchPositionChangedEvent switchPosition)
    {
        lock (_lifecycleGate)
        {
            if (!_disposed)
                _state.SetSwitchPosition(switchPosition.SwitchId, switchPosition.IsLeft);
        }
    }

    private void OnSignalAspectChanged(SignalAspectChangedEvent signalAspect)
    {
        lock (_lifecycleGate)
        {
            if (!_disposed)
                _state.SetSignalAspect(signalAspect.SignalId, signalAspect.Aspect);
        }
    }

    /// <summary>Expires stale feedback explicitly; exposed for deterministic tests and controlled shutdown.</summary>
    public void ExpireFeedbackNow()
    {
        lock (_lifecycleGate)
        {
            if (!_disposed)
                _state.ExpireFeedback(DateTimeOffset.UtcNow, _feedbackTimeout);
        }
    }

    public void Dispose()
    {
        Guid[] subscriptions;
        Timer? timer;

        lock (_lifecycleGate)
        {
            if (_disposed)
                return;

            _disposed = true;
            subscriptions = [.. _subscriptionIds];
            _subscriptionIds.Clear();
            timer = _feedbackExpiryTimer;
            _feedbackExpiryTimer = null;
        }

        foreach (var subscriptionId in subscriptions)
            _eventBus.Unsubscribe(subscriptionId);
        timer?.Dispose();
    }
}
