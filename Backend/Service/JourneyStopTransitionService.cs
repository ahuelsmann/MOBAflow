// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;

public interface IJourneyStopTransitionService
{
    JourneyStopTransitionResult Apply(Journey journey, JourneySessionState state, JourneyStopTransition transition);
}

public sealed record JourneyStopTransitionResult(Station? PreviousStation, Station? CurrentStation, bool Changed, bool CompletionRequested);

/// <summary>Applies journey stop transitions consistently for feedback steps and workflow actions.</summary>
public sealed class JourneyStopTransitionService : IJourneyStopTransitionService
{
    public JourneyStopTransitionResult Apply(Journey journey, JourneySessionState state, JourneyStopTransition transition)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(transition);

        var previous = ResolveCurrentStation(journey, state);
        if (transition.Mode == JourneyStopTransitionMode.None)
        {
            return new(previous, previous, false, false);
        }

        var targetIndex = transition.Mode == JourneyStopTransitionMode.Next
            ? ResolveCurrentIndex(journey, state) + 1
            : journey.Stations.FindIndex(station => station.Id == transition.StationId);

        if (targetIndex >= journey.Stations.Count && transition.Mode == JourneyStopTransitionMode.Next)
        {
            state.IsJourneyCompletionRequested = true;
            return new(previous, previous, false, true);
        }

        if (targetIndex < 0 || targetIndex >= journey.Stations.Count)
        {
            throw new InvalidOperationException("The configured target stop does not exist in the current journey");
        }

        var target = journey.Stations[targetIndex];
        state.CurrentStationId = target.Id;
        state.CurrentStationName = target.Name;
        state.CurrentPos = targetIndex;
        return new(previous, target, previous?.Id != target.Id, false);
    }

    private static int ResolveCurrentIndex(Journey journey, JourneySessionState state) =>
        state.CurrentStationId.HasValue
            ? journey.Stations.FindIndex(station => station.Id == state.CurrentStationId.Value)
            : state.CurrentPos;

    private static Station? ResolveCurrentStation(Journey journey, JourneySessionState state)
    {
        var index = ResolveCurrentIndex(journey, state);
        return index >= 0 && index < journey.Stations.Count ? journey.Stations[index] : null;
    }
}
