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
    private Guid _subscriptionId;

    public TrackPlanRailroadStateProjector(IEventBus eventBus, ITrackFeedbackLookup feedbackLookup, RailroadState state)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _feedbackLookup = feedbackLookup ?? throw new ArgumentNullException(nameof(feedbackLookup));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void Activate()
    {
        if (_subscriptionId == Guid.Empty)
            _subscriptionId = _eventBus.Subscribe<FeedbackReceivedEvent>(OnFeedbackReceived);
    }

    private void OnFeedbackReceived(FeedbackReceivedEvent feedback)
    {
        var timestamp = DateTimeOffset.UtcNow;
        foreach (var trackId in _feedbackLookup.GetTrackIdsByFeedbackInPort(feedback.InPort))
            _state.MarkFeedback(trackId, timestamp);
    }

    public void Dispose()
    {
        if (_subscriptionId == Guid.Empty)
            return;
        _eventBus.Unsubscribe(_subscriptionId);
        _subscriptionId = Guid.Empty;
    }
}
