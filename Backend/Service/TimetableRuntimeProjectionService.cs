// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Runtime;

using Domain;

using Interface;

/// <summary>
/// Projects unambiguous journey progress into timetable actual-arrival state.
/// Departures remain explicit operator decisions.
/// </summary>
public sealed class TimetableRuntimeProjectionService : ITimetableRuntimeProjectionService
{
    private readonly ITimetableOperationsService _operations;

    /// <summary>Initializes a new projection service.</summary>
    public TimetableRuntimeProjectionService(ITimetableOperationsService operations)
    {
        _operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }

    /// <inheritdoc />
    public async Task<TimetableProjectionResult> ProjectAsync(Project project, MobaRuntimeSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(snapshot);

        var states = await _operations.GetStatesAsync(project.Id, cancellationToken).ConfigureAwait(false);
        var stateByService = states.ToDictionary(state => state.ServiceId);
        var suppressed = new List<Guid>();
        var recorded = 0;

        var operatingDate = DateOnly.FromDateTime(snapshot.CreatedAt.LocalDateTime);
        foreach (var journeyGroup in project.TimetableServices
            .Where(service => service.ServiceDate == operatingDate)
            .GroupBy(service => stateByService.GetValueOrDefault(service.Id)?.AssignedJourneyId ?? service.JourneyId))
        {
            var liveOwners = journeyGroup
                .Where(service => !stateByService.TryGetValue(service.Id, out var state)
                    || state.Status is not (TimetableServiceStatus.Completed or TimetableServiceStatus.Cancelled))
                .ToArray();

            if (liveOwners.Length != 1)
            {
                if (liveOwners.Length > 1) suppressed.Add(journeyGroup.Key);
                continue;
            }

            if (!snapshot.JourneyStates.TryGetValue(journeyGroup.Key, out var journeyState)
                || journeyState.CurrentStationId is not Guid journeyStopId)
            {
                continue;
            }

            var service = liveOwners[0];
            var call = service.Calls.FirstOrDefault(candidate => candidate.JourneyStopId == journeyStopId);
            if (call is null) continue;

            var existingState = stateByService.GetValueOrDefault(service.Id);
            if (existingState?.Calls.FirstOrDefault(candidate => candidate.CallId == call.Id)?.ActualArrival is not null) continue;

            DateTimeOffset? occurredAt = journeyState.LastFeedbackTime is DateTime lastFeedbackTime
                ? new DateTimeOffset(lastFeedbackTime)
                : null;
            await _operations.RecordArrivalAsync(project.Id, service.Id, call.Id, occurredAt, cancellationToken).ConfigureAwait(false);
            recorded++;
        }

        return new TimetableProjectionResult(recorded, suppressed.Order().ToArray());
    }
}
