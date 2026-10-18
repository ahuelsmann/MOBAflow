// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Domain;

public interface IJourneyStopTransitionService
{
    JourneyStopTransitionResult Apply(Journey journey, JourneySessionState state, JourneyStopTransition transition);
}

public sealed record JourneyStopTransitionResult(Station? PreviousStation, Station? CurrentStation, bool Changed);

/// <summary>
/// Applies journey stop transitions to the runtime state, consistently for feedback steps and workflow actions.
/// The stop rules belong to <see cref="Journey"/>; this service only reads and updates <see cref="JourneySessionState"/>.
/// </summary>
public sealed class JourneyStopTransitionService : IJourneyStopTransitionService
{
    public JourneyStopTransitionResult Apply(Journey journey, JourneySessionState state, JourneyStopTransition transition)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(transition);

        var currentIndex = journey.IndexOfStop(state.CurrentStationId, state.CurrentPos);
        var previous = currentIndex >= 0 && currentIndex < journey.Stations.Count ? journey.Stations[currentIndex] : null;
        if (journey.ResolveStopTransition(currentIndex, transition) is not { } targetIndex)
        {
            return new(previous, previous, false);
        }

        var target = journey.Stations[targetIndex];
        state.CurrentStationId = target.Id;
        state.CurrentStationName = target.Name;
        state.CurrentPos = targetIndex;
        return new(previous, target, previous?.Id != target.Id);
    }
}
