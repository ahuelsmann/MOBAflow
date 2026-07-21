// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Interface;

using Domain;

/// <summary>
/// Performs a complete, immutable and deterministic validation pass over a project timetable.
/// </summary>
public sealed class TimetableEvaluationService : ITimetableEvaluationService
{
    /// <inheritdoc />
    public TimetableEvaluationResult Evaluate(Project project, IReadOnlyCollection<TimetableServiceState>? states = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        var issues = new List<TimetableIssue>();
        var stateByService = states?.ToDictionary(state => state.ServiceId)
            ?? new Dictionary<Guid, TimetableServiceState>();
        ValidateDefinitions(project, stateByService, issues);
        ValidateResourceConflicts(project, stateByService, issues);

        return new TimetableEvaluationResult(issues
            .OrderBy(issue => issue.ServiceId)
            .ThenBy(issue => issue.ConflictingServiceId)
            .ThenBy(issue => issue.Kind)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray());
    }

    private static void ValidateDefinitions(Project project, IReadOnlyDictionary<Guid, TimetableServiceState> stateByService, List<TimetableIssue> issues)
    {
        var trainIds = project.Trains.Select(train => train.Id).ToHashSet();
        var stations = project.Stations.GroupBy(station => station.Id).ToDictionary(group => group.Key, group => group.First());

        foreach (var duplicate in project.TimetableServices.GroupBy(service => service.Id).Where(group => group.Count() > 1))
        {
            foreach (var service in duplicate)
            {
                Add(issues, TimetableIssueKind.DuplicateIdentifier, service, null, $"Timetable service id {service.Id} is duplicated.");
            }
        }


        foreach (var duplicate in project.TimetableServices
            .SelectMany(service => service.Calls.Select(call => (Service: service, Call: call)))
            .GroupBy(item => item.Call.Id)
            .Where(group => group.Count() > 1))
        {
            foreach (var service in duplicate.Select(item => item.Service).DistinctBy(service => service.Id))
            {
                Add(issues, TimetableIssueKind.DuplicateIdentifier, service, null, $"Timetable call id {duplicate.Key} is duplicated.");
            }
        }

        foreach (var service in project.TimetableServices)
        {
            var state = stateByService.GetValueOrDefault(service.Id);
            var journeyId = state?.AssignedJourneyId ?? service.JourneyId;
            var trainId = state?.AssignedTrainId ?? service.TrainId;
            var journey = project.Journeys.FirstOrDefault(candidate => candidate.Id == journeyId);
            if (journey is null)
            {
                Add(issues, TimetableIssueKind.InvalidReference, service, null, $"Journey {journeyId} does not exist.");
            }

            if (trainId is Guid assignedTrainId && !trainIds.Contains(assignedTrainId))
            {
                Add(issues, TimetableIssueKind.InvalidReference, service, null, $"Train {assignedTrainId} does not exist.");
            }

            if (string.IsNullOrWhiteSpace(service.ServiceNumber))
            {
                Add(issues, TimetableIssueKind.InvalidReference, service, null, "A service number is required.");
            }

            if (service.Calls.Count == 0)
            {
                Add(issues, TimetableIssueKind.InvalidReference, service, null, "At least one timetable call is required.");
            }

            var journeyStopOrder = journey?.Stations
                .Select((station, index) => (station.Id, index))
                .GroupBy(item => item.Id)
                .ToDictionary(group => group.Key, group => group.First().index)
                ?? [];
            DateTimeOffset? previousDeparture = null;
            var previousJourneyStopIndex = -1;
            foreach (var call in service.Calls)
            {
                if (!journeyStopOrder.TryGetValue(call.JourneyStopId, out var journeyStopIndex))
                {
                    Add(issues, TimetableIssueKind.InvalidReference, service, null, $"Journey stop {call.JourneyStopId} does not belong to journey {journeyId}.");
                }
                else if (journeyStopIndex <= previousJourneyStopIndex)
                {
                    Add(issues, TimetableIssueKind.InvalidTimeRange, service, null, $"Call {call.Id} contradicts the journey station order.");
                }
                previousJourneyStopIndex = Math.Max(previousJourneyStopIndex, journeyStopIndex);

                if (!stations.TryGetValue(call.StationId, out var station))
                {
                    Add(issues, TimetableIssueKind.InvalidReference, service, null, $"Station {call.StationId} does not exist.");
                }
                else
                {
                    var callState = state?.Calls.FirstOrDefault(candidate => candidate.CallId == call.Id);
                    var platformId = callState?.AssignedPlatformId ?? call.PlatformId;
                    if (station.Platforms.All(platform => platform.Id != platformId))
                    {
                        Add(issues, TimetableIssueKind.InvalidReference, service, null, $"Platform {platformId} does not belong to station {call.StationId}.");
                    }
                }

                if (call.ScheduledArrival > call.ScheduledDeparture)
                {
                    Add(issues, TimetableIssueKind.InvalidTimeRange, service, null, $"Call {call.Id} departs before it arrives.");
                }

                if (previousDeparture > call.ScheduledArrival)
                {
                    Add(issues, TimetableIssueKind.InvalidTimeRange, service, null, $"Call {call.Id} starts before the preceding call departs.");
                }

                previousDeparture = call.ScheduledDeparture;
            }
        }
    }

    private static void ValidateResourceConflicts(Project project, IReadOnlyDictionary<Guid, TimetableServiceState> stateByService, List<TimetableIssue> issues)
    {
        var services = project.TimetableServices
            .Where(service => service.Calls.Count > 0)
            .Where(service => stateByService.GetValueOrDefault(service.Id)?.Status is not (TimetableServiceStatus.Completed or TimetableServiceStatus.Cancelled))
            .OrderBy(service => service.Calls.Min(call => call.ScheduledArrival))
            .ThenBy(service => service.Id)
            .ToArray();

        for (var leftIndex = 0; leftIndex < services.Length; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < services.Length; rightIndex++)
            {
                var left = services[leftIndex];
                var right = services[rightIndex];
                ValidatePlatformConflicts(left, right, stateByService, issues);

                var leftStart = left.Calls.Min(call => call.ScheduledArrival);
                var leftEnd = left.Calls.Max(call => call.ScheduledDeparture);
                var rightStart = right.Calls.Min(call => call.ScheduledArrival);
                var rightEnd = right.Calls.Max(call => call.ScheduledDeparture);

                var leftState = stateByService.GetValueOrDefault(left.Id);
                var rightState = stateByService.GetValueOrDefault(right.Id);
                var leftJourneyId = leftState?.AssignedJourneyId ?? left.JourneyId;
                var rightJourneyId = rightState?.AssignedJourneyId ?? right.JourneyId;
                if (leftJourneyId == rightJourneyId && Overlaps(leftStart, leftEnd, rightStart, rightEnd))
                {
                    Add(issues, TimetableIssueKind.JourneyConflict, left, right, "The same journey is assigned to overlapping services.");
                }

                var leftTrainId = leftState?.AssignedTrainId ?? left.TrainId;
                var rightTrainId = rightState?.AssignedTrainId ?? right.TrainId;
                if (leftTrainId is Guid trainId && rightTrainId == trainId)
                {
                    if (Overlaps(leftStart, leftEnd, rightStart, rightEnd))
                    {
                        Add(issues, TimetableIssueKind.TrainConflict, left, right, "The same train is assigned to overlapping services.");
                    }
                    else
                    {
                        var gap = rightStart >= leftEnd ? rightStart - leftEnd : leftStart - rightEnd;
                        if (gap < project.TimetablePolicy.MinimumTurnaround)
                        {
                            Add(issues, TimetableIssueKind.TurnaroundConflict, left, right, $"Train turnaround is shorter than {project.TimetablePolicy.MinimumTurnaround}.");
                        }
                    }
                }
            }
        }
    }

    private static void ValidatePlatformConflicts(
        TimetableService left,
        TimetableService right,
        IReadOnlyDictionary<Guid, TimetableServiceState> stateByService,
        List<TimetableIssue> issues)
    {
        var leftState = stateByService.GetValueOrDefault(left.Id);
        var rightState = stateByService.GetValueOrDefault(right.Id);
        foreach (var leftCall in left.Calls)
        {
            var leftPlatformId = leftState?.Calls.FirstOrDefault(state => state.CallId == leftCall.Id)?.AssignedPlatformId ?? leftCall.PlatformId;
            foreach (var rightCall in right.Calls)
            {
                var rightPlatformId = rightState?.Calls.FirstOrDefault(state => state.CallId == rightCall.Id)?.AssignedPlatformId ?? rightCall.PlatformId;
                if (rightPlatformId != leftPlatformId) continue;
                if (Overlaps(leftCall.ScheduledArrival, leftCall.ScheduledDeparture, rightCall.ScheduledArrival, rightCall.ScheduledDeparture))
                {
                    Add(issues, TimetableIssueKind.PlatformConflict, left, right, $"Platform {leftPlatformId} is occupied by both services.");
                }
            }
        }
    }

    private static bool Overlaps(DateTimeOffset leftStart, DateTimeOffset leftEnd, DateTimeOffset rightStart, DateTimeOffset rightEnd)
        => leftStart < rightEnd && rightStart < leftEnd;

    private static void Add(List<TimetableIssue> issues, TimetableIssueKind kind, TimetableService service, TimetableService? conflictingService, string message)
        => issues.Add(new TimetableIssue(kind, service.Id, conflictingService?.Id, message));
}
