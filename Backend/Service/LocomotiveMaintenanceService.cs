// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;
using System.Globalization;

public enum MaintenanceDueState
{
    NotScheduled,
    Upcoming,
    Due,
    Overdue
}

public sealed record MaintenancePlanStatus(
    Guid PlanId,
    string Name,
    MaintenanceDueState State,
    DateTimeOffset? DueAt,
    decimal? RemainingOperatingHours,
    decimal? RemainingDistanceKilometres);

public interface ILocomotiveMaintenanceService
{
    IReadOnlyList<string> Validate(LocomotiveMaintenanceData maintenance);

    IReadOnlyList<MaintenancePlanStatus> Evaluate(LocomotiveMaintenanceData maintenance, DateTimeOffset now);
}

public sealed class LocomotiveMaintenanceService : ILocomotiveMaintenanceService
{
    public IReadOnlyList<string> Validate(LocomotiveMaintenanceData maintenance)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        var errors = new List<string>();

        if (maintenance.OperatingHours < 0)
            errors.Add("Operating hours must not be negative.");
        if (maintenance.DistanceKilometres < 0)
            errors.Add("Distance must not be negative.");

        foreach (var entry in maintenance.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Description))
                errors.Add($"Maintenance entry {entry.Id} requires a description.");
            if (entry.OperatingHoursAtService < 0 || entry.DistanceKilometresAtService < 0)
                errors.Add($"Maintenance entry {entry.Id} contains a negative counter.");
            if (entry.Cost is { } cost && !IsIsoCurrency(cost.Currency))
                errors.Add($"Maintenance entry {entry.Id} uses an invalid ISO 4217 currency code.");
        }

        foreach (var plan in maintenance.Plans)
        {
            if (string.IsNullOrWhiteSpace(plan.Name))
                errors.Add($"Maintenance plan {plan.Id} requires a name.");
            if (plan.IntervalDays is <= 0 || plan.IntervalOperatingHours is <= 0 || plan.IntervalDistanceKilometres is <= 0)
                errors.Add($"Maintenance plan {plan.Id} intervals must be positive.");
            if (plan.OperatingHoursAtLastCompletion < 0 || plan.DistanceKilometresAtLastCompletion < 0)
                errors.Add($"Maintenance plan {plan.Id} contains a negative completion counter.");
            if (plan.IntervalDays is null && plan.IntervalOperatingHours is null && plan.IntervalDistanceKilometres is null)
                errors.Add($"Maintenance plan {plan.Id} requires at least one interval.");
        }

        return errors;
    }

    public IReadOnlyList<MaintenancePlanStatus> Evaluate(LocomotiveMaintenanceData maintenance, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(maintenance);
        var validation = Validate(maintenance);
        if (validation.Count != 0)
            throw new ArgumentException(string.Join(" ", validation), nameof(maintenance));

        return maintenance.Plans
            .Select(plan => EvaluatePlan(plan, maintenance, now))
            .OrderByDescending(status => status.State)
            .ThenBy(status => status.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(status => status.PlanId)
            .ToArray();
    }

    private static MaintenancePlanStatus EvaluatePlan(
        LocomotiveMaintenancePlan plan,
        LocomotiveMaintenanceData maintenance,
        DateTimeOffset now)
    {
        DateTimeOffset? dueAt = plan.IntervalDays is { } intervalDays && plan.LastCompletedAt is { } completedAt
            ? completedAt.AddDays(intervalDays)
            : null;
        var remainingHours = Remaining(maintenance.OperatingHours, plan.OperatingHoursAtLastCompletion, plan.IntervalOperatingHours);
        var remainingDistance = Remaining(maintenance.DistanceKilometres, plan.DistanceKilometresAtLastCompletion, plan.IntervalDistanceKilometres);

        var scheduled = dueAt is not null || remainingHours is not null || remainingDistance is not null;
        var overdue = dueAt < now || remainingHours < 0 || remainingDistance < 0;
        var due = dueAt == now || remainingHours == 0 || remainingDistance == 0;
        var state = !scheduled
            ? MaintenanceDueState.NotScheduled
            : overdue
                ? MaintenanceDueState.Overdue
                : due
                    ? MaintenanceDueState.Due
                    : MaintenanceDueState.Upcoming;

        return new MaintenancePlanStatus(plan.Id, plan.Name, state, dueAt, remainingHours, remainingDistance);
    }

    private static decimal? Remaining(decimal? current, decimal? completedAt, decimal? interval)
        => current is { } currentValue && interval is { } intervalValue
            ? (completedAt ?? 0) + intervalValue - currentValue
            : null;

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
