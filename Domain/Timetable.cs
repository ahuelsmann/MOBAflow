// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// A dated train service whose calls reference the project's journey and infrastructure master data.
/// </summary>
public sealed class TimetableService
{
    /// <summary>Gets or sets the stable service identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the operator-facing service number.</summary>
    public string ServiceNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the descriptive service name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the referenced journey identifier.</summary>
    public Guid JourneyId { get; set; }

    /// <summary>Gets or sets the optional referenced train identifier.</summary>
    public Guid? TrainId { get; set; }

    /// <summary>Gets or sets the operating date.</summary>
    public DateOnly ServiceDate { get; set; }

    /// <summary>Gets or sets the ordered scheduled calls.</summary>
    public List<TimetableCall> Calls { get; set; } = [];
}

/// <summary>
/// One scheduled stop within a timetable service.
/// </summary>
public sealed class TimetableCall
{
    /// <summary>Gets or sets the stable call identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the referenced stop within the service journey.</summary>
    public Guid JourneyStopId { get; set; }

    /// <summary>Gets or sets the referenced project station.</summary>
    public Guid StationId { get; set; }

    /// <summary>Gets or sets the planned project platform.</summary>
    public Guid PlatformId { get; set; }

    /// <summary>Gets or sets the scheduled arrival including its UTC offset.</summary>
    public DateTimeOffset ScheduledArrival { get; set; }

    /// <summary>Gets or sets the scheduled departure including its UTC offset.</summary>
    public DateTimeOffset ScheduledDeparture { get; set; }
}

/// <summary>
/// Project-wide rules used by deterministic timetable validation.
/// </summary>
public sealed class TimetablePolicy
{
    /// <summary>Gets or sets the minimum time allowed between uses of the same train.</summary>
    public TimeSpan MinimumTurnaround { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>Describes the mutable operating status of a timetable service.</summary>
public enum TimetableServiceStatus
{
    /// <summary>The service has not started.</summary>
    Scheduled,
    /// <summary>The dispatcher has placed an advisory hold.</summary>
    Held,
    /// <summary>The service has recorded live progress.</summary>
    Running,
    /// <summary>The dispatcher marked the service complete.</summary>
    Completed,
    /// <summary>The dispatcher cancelled the service.</summary>
    Cancelled
}

/// <summary>
/// Mutable operating state kept outside the editable solution document.
/// </summary>
public sealed class TimetableServiceState
{
    /// <summary>Gets or sets the referenced timetable service identifier.</summary>
    public Guid ServiceId { get; set; }

    /// <summary>Gets or sets the current operating status.</summary>
    public TimetableServiceStatus Status { get; set; }

    /// <summary>Gets or sets the advisory hold deadline.</summary>
    public DateTimeOffset? HeldUntil { get; set; }

    /// <summary>Gets or sets the operator-provided hold reason.</summary>
    public string? HoldReason { get; set; }

    /// <summary>Gets or sets the session-specific train assignment.</summary>
    public Guid? AssignedTrainId { get; set; }

    /// <summary>Gets or sets the session-specific journey assignment.</summary>
    public Guid? AssignedJourneyId { get; set; }

    /// <summary>Gets or sets mutable state for individual calls.</summary>
    public List<TimetableCallState> Calls { get; set; } = [];
}

/// <summary>Contains mutable actual times and assignments for one timetable call.</summary>
public sealed class TimetableCallState
{
    /// <summary>Gets or sets the referenced timetable call identifier.</summary>
    public Guid CallId { get; set; }

    /// <summary>Gets or sets the session-specific platform assignment.</summary>
    public Guid? AssignedPlatformId { get; set; }

    /// <summary>Gets or sets the recorded actual arrival.</summary>
    public DateTimeOffset? ActualArrival { get; set; }

    /// <summary>Gets or sets the recorded actual departure.</summary>
    public DateTimeOffset? ActualDeparture { get; set; }
}
