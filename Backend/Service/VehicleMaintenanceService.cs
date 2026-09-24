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
    TimeSpan CalendarWindow)
{
    public static MaintenanceDueSoonThresholds Disabled { get; } = new(TimeSpan.Zero);
}

public sealed record MaintenancePlanStatus(
    Guid PlanId,
    string Name,
    MaintenanceDueState State,
    DateTimeOffset? DueAt);

public interface IVehicleMaintenanceService
{
    IReadOnlyList<string> Validate(VehicleMaintenanceData maintenance);

    IReadOnlyList<MaintenancePlanStatus> Evaluate(
        VehicleMaintenanceData maintenance,
        DateTimeOffset now,
        MaintenanceDueSoonThresholds? dueSoonThresholds = null);

    VehicleMaintenanceEntry CompletePlan(
        VehicleMaintenanceData maintenance,
        Guid planId,
        DateTimeOffset completedAt);
}

/// <summary>
/// Evaluates and completes shared rolling-stock maintenance plans by calendar date.
/// </summary>
public sealed class VehicleMaintenanceService : IVehicleMaintenanceService
{
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
        DateTimeOffset now,
        MaintenanceDueSoonThresholds? dueSoonThresholds = null)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        var validation = Validate(maintenance);
        if (validation.Count != 0)
            throw new ArgumentException(string.Join(" ", validation), nameof(maintenance));

        var thresholds = dueSoonThresholds ?? MaintenanceDueSoonThresholds.Disabled;
        ValidateThresholds(thresholds);

        return maintenance.Plans
            .Select(plan => EvaluatePlan(plan, now, thresholds))
            .OrderByDescending(status => status.State)
            .ThenBy(status => status.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(status => status.PlanId)
            .ToArray();
    }

    public VehicleMaintenanceEntry CompletePlan(
        VehicleMaintenanceData maintenance,
        Guid planId,
        DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        var validation = Validate(maintenance);
        if (validation.Count != 0)
            throw new ArgumentException(string.Join(" ", validation), nameof(maintenance));

        var plan = maintenance.Plans.SingleOrDefault(candidate => candidate.Id == planId)
            ?? throw new ArgumentException($"Maintenance plan {planId} does not exist.", nameof(planId));
        var entry = new VehicleMaintenanceEntry
        {
            PerformedAt = completedAt,
            Category = plan.Category,
            Description = plan.Name
        };

        plan.LastCompletedAt = completedAt;
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
            if (plan.IntervalDays is null or <= 0)
                errors.Add($"Maintenance plan {plan.Id} requires a positive calendar interval.");
        }
    }

    private static void AddDuplicateIdErrors(IEnumerable<Guid> ids, string subject, List<string> errors)
    {
        foreach (var duplicateId in ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key))
            errors.Add($"{subject} {duplicateId} occurs more than once.");
    }

    private static MaintenancePlanStatus EvaluatePlan(
        VehicleMaintenancePlan plan,
        DateTimeOffset now,
        MaintenanceDueSoonThresholds thresholds)
    {
        DateTimeOffset? dueAt = plan.IntervalDays is { } intervalDays && plan.LastCompletedAt is { } completedAt
            ? completedAt.AddDays(intervalDays)
            : null;
        var state = MaintenanceDueState.NotScheduled;
        if (dueAt is { } date)
        {
            if (date < now)
                state = MaintenanceDueState.Overdue;
            else if (date == now)
                state = MaintenanceDueState.Due;
            else if (thresholds.CalendarWindow > TimeSpan.Zero && date - now <= thresholds.CalendarWindow)
                state = MaintenanceDueState.DueSoon;
            else
                state = MaintenanceDueState.Upcoming;
        }

        return new MaintenancePlanStatus(plan.Id, plan.Name, state, dueAt);
    }

    private static void ValidateThresholds(MaintenanceDueSoonThresholds thresholds)
    {
        if (thresholds.CalendarWindow < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(thresholds), "The due-soon window must not be negative.");
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
