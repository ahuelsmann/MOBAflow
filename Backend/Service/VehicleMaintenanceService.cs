// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;
using System.Globalization;

public enum MaintenanceDueState
{
    NotScheduled,
    Upcoming,
    DueSoon,
    Due,
    Overdue
}

/// <summary>
/// Configures the deterministic window in which a future maintenance boundary is considered due soon.
/// </summary>
public sealed record MaintenanceDueSoonThresholds(
    TimeSpan CalendarWindow,
    long OperatingSeconds,
    long CompletedTrips,
    decimal DistanceKilometres)
{
    public static MaintenanceDueSoonThresholds Disabled { get; } = new(TimeSpan.Zero, 0, 0, 0);
}

public sealed record MaintenancePlanStatus(
    Guid PlanId,
    string Name,
    MaintenanceDueState State,
    DateTimeOffset? DueAt,
    long? RemainingOperatingSeconds,
    long? RemainingCompletedTrips,
    decimal? RemainingDistanceKilometres);

public interface IVehicleMaintenanceService
{
    IReadOnlyList<string> Validate(VehicleMaintenanceData maintenance);

    IReadOnlyList<MaintenancePlanStatus> Evaluate(
        VehicleMaintenanceData maintenance,
        VehicleUsageData? usage,
        DateTimeOffset now,
        MaintenanceDueSoonThresholds? dueSoonThresholds = null);

    VehicleMaintenanceEntry CompletePlan(
        VehicleMaintenanceData maintenance,
        Guid planId,
        VehicleUsageData? usage,
        DateTimeOffset completedAt);
}

/// <summary>
/// Evaluates and completes shared rolling-stock maintenance plans without modifying lifetime usage.
/// </summary>
public sealed class VehicleMaintenanceService : IVehicleMaintenanceService
{
    private readonly IVehicleUsageService _usageService;

    public VehicleMaintenanceService(IVehicleUsageService? usageService = null)
    {
        _usageService = usageService ?? new VehicleUsageService();
    }

    public IReadOnlyList<string> Validate(VehicleMaintenanceData maintenance)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        var errors = new List<string>();

        var entries = maintenance.Entries;
        var plans = maintenance.Plans;
        if (entries is null)
            errors.Add("Maintenance entries must be a collection.");
        if (plans is null)
            errors.Add("Maintenance plans must be a collection.");
        if (entries is null || plans is null)
            return errors;

        ValidateEntries(entries, errors);
        ValidatePlans(plans, errors);
        return errors;
    }

    public IReadOnlyList<MaintenancePlanStatus> Evaluate(
        VehicleMaintenanceData maintenance,
        VehicleUsageData? usage,
        DateTimeOffset now,
        MaintenanceDueSoonThresholds? dueSoonThresholds = null)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        var validation = Validate(maintenance);
        if (validation.Count != 0)
            throw new ArgumentException(string.Join(" ", validation), nameof(maintenance));

        var thresholds = dueSoonThresholds ?? MaintenanceDueSoonThresholds.Disabled;
        ValidateThresholds(thresholds);
        var totals = CalculateUsageTotals(usage);

        return maintenance.Plans
            .Select(plan => EvaluatePlan(plan, totals, now, thresholds))
            .OrderByDescending(status => status.State)
            .ThenBy(status => status.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(status => status.PlanId)
            .ToArray();
    }

    public VehicleMaintenanceEntry CompletePlan(
        VehicleMaintenanceData maintenance,
        Guid planId,
        VehicleUsageData? usage,
        DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        var validation = Validate(maintenance);
        if (validation.Count != 0)
            throw new ArgumentException(string.Join(" ", validation), nameof(maintenance));

        var plan = maintenance.Plans.SingleOrDefault(candidate => candidate.Id == planId)
            ?? throw new ArgumentException($"Maintenance plan {planId} does not exist.", nameof(planId));
        var totals = CalculateUsageTotals(usage);
        if (plan.IntervalDistanceKilometres is not null && totals.DistanceKilometres is null)
            throw new ArgumentException("Distance-based maintenance requires tracked distance.", nameof(usage));

        var entry = new VehicleMaintenanceEntry
        {
            PerformedAt = completedAt,
            Category = plan.Category,
            Description = plan.Name,
            OperatingSecondsAtService = totals.OperatingSeconds,
            CompletedTripsAtService = totals.CompletedTrips,
            DistanceKilometresAtService = totals.DistanceKilometres
        };

        if (plan.IntervalDays is not null)
            plan.LastCompletedAt = completedAt;
        if (plan.IntervalOperatingSeconds is not null)
            plan.OperatingSecondsAtLastCompletion = totals.OperatingSeconds;
        if (plan.IntervalCompletedTrips is not null)
            plan.CompletedTripsAtLastCompletion = totals.CompletedTrips;
        if (plan.IntervalDistanceKilometres is not null)
            plan.DistanceKilometresAtLastCompletion = totals.DistanceKilometres;
        maintenance.Entries.Add(entry);

        return entry;
    }

    private static void ValidateEntries(IReadOnlyCollection<VehicleMaintenanceEntry> entries, List<string> errors)
    {
        AddDuplicateIdErrors(entries.Select(entry => entry.Id), "Maintenance entry", errors);
        foreach (var entry in entries)
        {
            if (entry.Id == Guid.Empty)
                errors.Add("Maintenance entries require a stable identifier.");
            if (string.IsNullOrWhiteSpace(entry.Description))
                errors.Add($"Maintenance entry {entry.Id} requires a description.");
            if (entry.OperatingSecondsAtService < 0
                || entry.CompletedTripsAtService < 0
                || entry.DistanceKilometresAtService < 0)
            {
                errors.Add($"Maintenance entry {entry.Id} contains a negative counter.");
            }
            if (entry.Cost is { } cost && !IsIsoCurrency(cost.Currency))
                errors.Add($"Maintenance entry {entry.Id} uses an invalid ISO 4217 currency code.");
        }
    }

    private static void ValidatePlans(IReadOnlyCollection<VehicleMaintenancePlan> plans, List<string> errors)
    {
        AddDuplicateIdErrors(plans.Select(plan => plan.Id), "Maintenance plan", errors);
        foreach (var plan in plans)
        {
            if (plan.Id == Guid.Empty)
                errors.Add("Maintenance plans require a stable identifier.");
            if (string.IsNullOrWhiteSpace(plan.Name))
                errors.Add($"Maintenance plan {plan.Id} requires a name.");
            if (plan.IntervalDays is <= 0
                || plan.IntervalOperatingSeconds is <= 0
                || plan.IntervalCompletedTrips is <= 0
                || plan.IntervalDistanceKilometres is <= 0)
            {
                errors.Add($"Maintenance plan {plan.Id} intervals must be positive.");
            }
            if (plan.OperatingSecondsAtLastCompletion < 0
                || plan.CompletedTripsAtLastCompletion < 0
                || plan.DistanceKilometresAtLastCompletion < 0)
            {
                errors.Add($"Maintenance plan {plan.Id} contains a negative completion counter.");
            }
            if (plan.IntervalDays is null
                && plan.IntervalOperatingSeconds is null
                && plan.IntervalCompletedTrips is null
                && plan.IntervalDistanceKilometres is null)
            {
                errors.Add($"Maintenance plan {plan.Id} requires at least one interval.");
            }
        }
    }

    private static void AddDuplicateIdErrors(IEnumerable<Guid> ids, string subject, List<string> errors)
    {
        foreach (var duplicateId in ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key))
            errors.Add($"{subject} {duplicateId} occurs more than once.");
    }

    private static MaintenancePlanStatus EvaluatePlan(
        VehicleMaintenancePlan plan,
        VehicleUsageTotals totals,
        DateTimeOffset now,
        MaintenanceDueSoonThresholds thresholds)
    {
        DateTimeOffset? dueAt = plan.IntervalDays is { } intervalDays && plan.LastCompletedAt is { } completedAt
            ? completedAt.AddDays(intervalDays)
            : null;
        var remainingOperatingSeconds = Remaining(
            totals.OperatingSeconds,
            plan.OperatingSecondsAtLastCompletion,
            plan.IntervalOperatingSeconds);
        var remainingCompletedTrips = Remaining(
            totals.CompletedTrips,
            plan.CompletedTripsAtLastCompletion,
            plan.IntervalCompletedTrips);
        var remainingDistance = Remaining(
            totals.DistanceKilometres,
            plan.DistanceKilometresAtLastCompletion,
            plan.IntervalDistanceKilometres);

        var scheduled = dueAt is not null
            || remainingOperatingSeconds is not null
            || remainingCompletedTrips is not null
            || remainingDistance is not null;
        var overdue = dueAt < now
            || remainingOperatingSeconds < 0
            || remainingCompletedTrips < 0
            || remainingDistance < 0;
        var due = dueAt == now
            || remainingOperatingSeconds == 0
            || remainingCompletedTrips == 0
            || remainingDistance == 0;
        var dueSoon = IsDueSoon(
            dueAt,
            remainingOperatingSeconds,
            remainingCompletedTrips,
            remainingDistance,
            now,
            thresholds);
        var state = !scheduled
            ? MaintenanceDueState.NotScheduled
            : overdue
                ? MaintenanceDueState.Overdue
                : due
                    ? MaintenanceDueState.Due
                    : dueSoon
                        ? MaintenanceDueState.DueSoon
                        : MaintenanceDueState.Upcoming;

        return new MaintenancePlanStatus(
            plan.Id,
            plan.Name,
            state,
            dueAt,
            remainingOperatingSeconds,
            remainingCompletedTrips,
            remainingDistance);
    }

    private VehicleUsageTotals CalculateUsageTotals(VehicleUsageData? usage)
        => usage is null
            ? new VehicleUsageTotals(0, 0, null)
            : _usageService.CalculateTotals(usage);

    private static long? Remaining(long current, long? completedAt, long? interval)
        => interval is { } intervalValue
            ? checked((completedAt ?? 0) + intervalValue - current)
            : null;

    private static decimal? Remaining(decimal? current, decimal? completedAt, decimal? interval)
        => current is { } currentValue && interval is { } intervalValue
            ? (completedAt ?? 0) + intervalValue - currentValue
            : null;

    private static bool IsDueSoon(
        DateTimeOffset? dueAt,
        long? remainingOperatingSeconds,
        long? remainingCompletedTrips,
        decimal? remainingDistance,
        DateTimeOffset now,
        MaintenanceDueSoonThresholds thresholds)
        => dueAt is { } calendarDueAt
               && thresholds.CalendarWindow > TimeSpan.Zero
               && calendarDueAt <= now.Add(thresholds.CalendarWindow)
           || remainingOperatingSeconds is > 0
               && thresholds.OperatingSeconds > 0
               && remainingOperatingSeconds <= thresholds.OperatingSeconds
           || remainingCompletedTrips is > 0
               && thresholds.CompletedTrips > 0
               && remainingCompletedTrips <= thresholds.CompletedTrips
           || remainingDistance is > 0
               && thresholds.DistanceKilometres > 0
               && remainingDistance <= thresholds.DistanceKilometres;

    private static void ValidateThresholds(MaintenanceDueSoonThresholds thresholds)
    {
        if (thresholds.CalendarWindow < TimeSpan.Zero
            || thresholds.OperatingSeconds < 0
            || thresholds.CompletedTrips < 0
            || thresholds.DistanceKilometres < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholds), "Due-soon thresholds must not be negative.");
        }
    }

    private static bool IsIsoCurrency(string? currency)
    {
        if (currency is null || currency.Length != 3 || currency.Any(character => !char.IsAsciiLetterUpper(character)))
            return false;

        try
        {
            return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(culture => new RegionInfo(culture.Name).ISOCurrencySymbol)
                .Contains(currency, StringComparer.Ordinal);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
