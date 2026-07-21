// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Domain;
using Common.Events;

/// <summary>Validates timetable definitions and effective session assignments.</summary>
public interface ITimetableEvaluationService
{
    /// <summary>Evaluates the entire project timetable without mutating it.</summary>
    TimetableEvaluationResult Evaluate(Project project, IReadOnlyCollection<TimetableServiceState>? states = null);
}

/// <summary>Persists project-scoped timetable operating state.</summary>
public interface ITimetableStateStore
{
    /// <summary>Loads all operating states for a project.</summary>
    Task<IReadOnlyList<TimetableServiceState>> LoadAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Atomically saves all operating states for a project.</summary>
    Task SaveAsync(Guid projectId, IReadOnlyCollection<TimetableServiceState> states, CancellationToken cancellationToken = default);
}

/// <summary>Applies durable manual dispatcher decisions.</summary>
public interface ITimetableOperationsService
{
    /// <summary>Gets the current project-scoped operating states.</summary>
    Task<IReadOnlyList<TimetableServiceState>> GetStatesAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Places an advisory hold on a service.</summary>
    Task<TimetableServiceState> HoldAsync(Guid projectId, Guid serviceId, DateTimeOffset heldUntil, string reason, CancellationToken cancellationToken = default);

    /// <summary>Releases a previously held service.</summary>
    Task<TimetableServiceState> ReleaseAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default);

    /// <summary>Cancels a non-completed service.</summary>
    Task<TimetableServiceState> CancelAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default);

    /// <summary>Marks a non-cancelled service complete.</summary>
    Task<TimetableServiceState> CompleteAsync(Guid projectId, Guid serviceId, CancellationToken cancellationToken = default);

    /// <summary>Assigns a different train for the operating session.</summary>
    Task<TimetableServiceState> ReassignTrainAsync(Guid projectId, Guid serviceId, Guid trainId, CancellationToken cancellationToken = default);

    /// <summary>Assigns a different journey for the operating session.</summary>
    Task<TimetableServiceState> ReassignJourneyAsync(Guid projectId, Guid serviceId, Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>Assigns a different platform to a call for the operating session.</summary>
    Task<TimetableServiceState> ReassignPlatformAsync(Guid projectId, Guid serviceId, Guid callId, Guid platformId, CancellationToken cancellationToken = default);

    /// <summary>Records an actual arrival idempotently.</summary>
    Task<TimetableServiceState> RecordArrivalAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default);

    /// <summary>Records an actual departure idempotently.</summary>
    Task<TimetableServiceState> RecordDepartureAsync(Guid projectId, Guid serviceId, Guid callId, DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default);
}

/// <summary>Calculates planned-versus-actual timetable delay.</summary>
public interface ITimetableTimingService
{
    /// <summary>Calculates the signed delay for a call using actual state or the current clock.</summary>
    TimeSpan CalculateDelay(TimetableCall call, TimetableCallState? state);
}

/// <summary>Projects unambiguous runtime journey progress into timetable state.</summary>
public interface ITimetableRuntimeProjectionService
{
    /// <summary>Projects one authoritative station transition into the operating session.</summary>
    Task<TimetableProjectionResult> ProjectAsync(Project project, JourneyStationReachedEvent transition, CancellationToken cancellationToken = default);
}

/// <summary>Classifies timetable validation and conflict findings.</summary>
public enum TimetableIssueKind
{
    /// <summary>A referenced entity is missing or incompatible.</summary>
    InvalidReference,
    /// <summary>Times or journey stop order are contradictory.</summary>
    InvalidTimeRange,
    /// <summary>A stable identifier appears more than once.</summary>
    DuplicateIdentifier,
    /// <summary>Two calls occupy the same platform at overlapping times.</summary>
    PlatformConflict,
    /// <summary>One journey is assigned to overlapping services.</summary>
    JourneyConflict,
    /// <summary>One train is assigned to overlapping services.</summary>
    TrainConflict,
    /// <summary>A train has less than the required turnaround.</summary>
    TurnaroundConflict
}

/// <summary>Describes one deterministic timetable finding and its affected services.</summary>
public sealed record TimetableIssue(
    TimetableIssueKind Kind,
    Guid ServiceId,
    Guid? ConflictingServiceId,
    string Message);

/// <summary>Contains the complete result of a timetable evaluation pass.</summary>
public sealed record TimetableEvaluationResult(IReadOnlyList<TimetableIssue> Issues)
{
    /// <summary>Gets a value indicating whether the timetable has no findings.</summary>
    public bool IsValid => Issues.Count == 0;
}

/// <summary>Summarizes arrivals recorded and journey mappings suppressed during projection.</summary>
public sealed record TimetableProjectionResult(int RecordedArrivals, IReadOnlyList<Guid> SuppressedJourneyIds);
