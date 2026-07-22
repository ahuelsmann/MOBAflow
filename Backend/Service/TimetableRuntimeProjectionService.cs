// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Common.Events;

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
    public async Task<TimetableProjectionResult> ProjectAsync(Project project, JourneyStationReachedEvent transition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (transition.ProjectId != project.Id)
        {
            return new TimetableProjectionResult(0, []);
        }

        var states = await _operations.GetStatesAsync(project.Id, cancellationToken).ConfigureAwait(false);
        var stateByService = states.ToDictionary(state => state.ServiceId);
        var operatingDate = DateOnly.FromDateTime(transition.OccurredAt.LocalDateTime);
        var candidates = project.TimetableServices
            .Where(service => service.ServiceDate == operatingDate)
            .Where(service => (stateByService.GetValueOrDefault(service.Id)?.AssignedJourneyId ?? service.JourneyId) == transition.JourneyId)
            .Where(service => stateByService.GetValueOrDefault(service.Id)?.Status is not (TimetableServiceStatus.Completed or TimetableServiceStatus.Cancelled))
            .ToArray();
        var owner = SelectOwner(candidates, stateByService, transition.OccurredAt);
        if (owner is null)
        {
            return new TimetableProjectionResult(0, candidates.Length > 1 ? [transition.JourneyId] : []);
        }

        var call = owner.Calls.FirstOrDefault(candidate => candidate.JourneyStopId == transition.StationId);
        if (call is null)
        {
            return new TimetableProjectionResult(0, []);
        }

        var existingState = stateByService.GetValueOrDefault(owner.Id);
        if (existingState?.Calls.FirstOrDefault(candidate => candidate.CallId == call.Id)?.ActualArrival is not null)
        {
            return new TimetableProjectionResult(0, []);
        }

        await _operations.RecordArrivalAsync(project.Id, owner.Id, call.Id, transition.OccurredAt, cancellationToken).ConfigureAwait(false);
        return new TimetableProjectionResult(1, []);
    }

    private static TimetableService? SelectOwner(
        IReadOnlyCollection<TimetableService> candidates,
        IReadOnlyDictionary<Guid, TimetableServiceState> stateByService,
        DateTimeOffset occurredAt)
    {
        var operationalOwners = candidates
            .Where(service => stateByService.GetValueOrDefault(service.Id)?.Status is TimetableServiceStatus.Running or TimetableServiceStatus.Held)
            .ToArray();
        if (operationalOwners.Length == 1) return operationalOwners[0];
        if (operationalOwners.Length > 1) return null;

        var ranked = candidates
            .Where(service => service.Calls.Count > 0)
            .Select(service => (Service: service, Distance: DistanceFromSchedule(service, occurredAt)))
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Service.Id)
            .ToArray();
        if (ranked.Length == 0) return null;
        if (ranked.Length > 1 && ranked[0].Distance == ranked[1].Distance) return null;
        return ranked[0].Service;
    }

    private static TimeSpan DistanceFromSchedule(TimetableService service, DateTimeOffset occurredAt)
    {
        var start = service.Calls.Min(call => call.ScheduledArrival);
        var end = service.Calls.Max(call => call.ScheduledDeparture);
        if (occurredAt < start) return start - occurredAt;
        if (occurredAt > end) return occurredAt - end;
        return TimeSpan.Zero;
    }
}
