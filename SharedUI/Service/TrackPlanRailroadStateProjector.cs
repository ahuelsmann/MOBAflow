// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Common.Events;
using Domain;

/// <summary>Projects Z21 feedback into runtime-only track state without mutating the persisted layout.</summary>
public sealed class TrackPlanRailroadStateProjector : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ITrackFeedbackLookup _feedbackLookup;
    private readonly RailroadState _state;
    private readonly List<Guid> _subscriptionIds = [];
    private Timer? _feedbackExpiryTimer;

    /// <summary>Maximum time a feedback occupancy remains active without another feedback event.</summary>
    public TimeSpan FeedbackTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public TrackPlanRailroadStateProjector(IEventBus eventBus, ITrackFeedbackLookup feedbackLookup, RailroadState state)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _feedbackLookup = feedbackLookup ?? throw new ArgumentNullException(nameof(feedbackLookup));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void Activate()
    {
        if (_subscriptionIds.Count != 0)
            return;

        _subscriptionIds.Add(_eventBus.Subscribe<FeedbackReceivedEvent>(OnFeedbackReceived));
        _subscriptionIds.Add(_eventBus.Subscribe<SwitchPositionChangedEvent>(OnSwitchPositionChanged));
        _subscriptionIds.Add(_eventBus.Subscribe<SignalAspectChangedEvent>(OnSignalAspectChanged));
        _feedbackExpiryTimer = new Timer(_ => ExpireFeedbackNow(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void OnFeedbackReceived(FeedbackReceivedEvent feedback)
    {
        var timestamp = DateTimeOffset.UtcNow;
        foreach (var trackId in _feedbackLookup.GetTrackIdsByFeedbackInPort(feedback.InPort))
            _state.MarkFeedback(trackId, timestamp);
    }

    private void OnSwitchPositionChanged(SwitchPositionChangedEvent switchPosition) =>
        _state.SetSwitchPosition(switchPosition.SwitchId, switchPosition.IsLeft);

    private void OnSignalAspectChanged(SignalAspectChangedEvent signalAspect) =>
        _state.SetSignalAspect(signalAspect.SignalId, signalAspect.Aspect);

    /// <summary>Expires stale feedback explicitly; exposed for deterministic tests and controlled shutdown.</summary>
    public void ExpireFeedbackNow() => _state.ExpireFeedback(DateTimeOffset.UtcNow, FeedbackTimeout);

    public void Dispose()
    {
        foreach (var subscriptionId in _subscriptionIds)
            _eventBus.Unsubscribe(subscriptionId);
        _subscriptionIds.Clear();
        _feedbackExpiryTimer?.Dispose();
        _feedbackExpiryTimer = null;
    }
}
