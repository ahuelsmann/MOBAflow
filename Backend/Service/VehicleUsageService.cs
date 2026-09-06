// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

/// <summary>
/// Validates vehicle usage, calculates effective totals, and records auditable corrections.
/// </summary>
public interface IVehicleUsageService
{
    /// <summary>
    /// Validates persisted tracked counters and their correction history.
    /// </summary>
    /// <param name="usage">The usage data to validate.</param>
    /// <returns>Validation errors, or an empty list when the data is valid.</returns>
    IReadOnlyList<string> Validate(VehicleUsageData usage);

    /// <summary>
    /// Calculates effective lifetime totals without modifying tracked counters.
    /// </summary>
    /// <param name="usage">The usage data to evaluate.</param>
    /// <returns>Tracked values combined with all valid corrections.</returns>
    VehicleUsageTotals CalculateTotals(VehicleUsageData usage);

    /// <summary>
    /// Appends a valid correction while preserving the existing audit history.
    /// </summary>
    /// <param name="usage">The usage data that owns the history.</param>
    /// <param name="correction">The correction to append.</param>
    void RecordCorrection(VehicleUsageData usage, VehicleUsageCorrection correction);
}

/// <summary>
/// Default platform-neutral implementation of vehicle usage validation and correction handling.
/// </summary>
public sealed class VehicleUsageService : IVehicleUsageService
{
    public IReadOnlyList<string> Validate(VehicleUsageData usage)
    {
        ArgumentNullException.ThrowIfNull(usage);

        var errors = new List<string>();
        if (usage.TrackedOperatingSeconds < 0)
            errors.Add("Tracked operating time must not be negative.");
        if (usage.TrackedCompletedTrips < 0)
            errors.Add("Tracked completed trips must not be negative.");
        if (usage.TrackedDistanceKilometres < 0)
            errors.Add("Tracked distance must not be negative.");
        if (usage.Corrections is null)
        {
            errors.Add("Usage corrections must be a collection.");
            return errors;
        }

        ValidateCorrections(usage, errors);
        ValidateEffectiveTotals(usage, errors);
        return errors;
    }

    public VehicleUsageTotals CalculateTotals(VehicleUsageData usage)
    {
        ArgumentNullException.ThrowIfNull(usage);

        var errors = Validate(usage);
        if (errors.Count != 0)
            throw new ArgumentException(string.Join(" ", errors), nameof(usage));

        return CalculateTotalsCore(usage);
    }

    public void RecordCorrection(VehicleUsageData usage, VehicleUsageCorrection correction)
    {
        ArgumentNullException.ThrowIfNull(usage);
        ArgumentNullException.ThrowIfNull(correction);
        if (usage.Corrections is null)
            throw new ArgumentException("Usage corrections must be a collection.", nameof(usage));

        var candidate = new VehicleUsageData
        {
            TrackedOperatingSeconds = usage.TrackedOperatingSeconds,
            TrackedCompletedTrips = usage.TrackedCompletedTrips,
            TrackedDistanceKilometres = usage.TrackedDistanceKilometres,
            Corrections = [.. usage.Corrections, correction]
        };
        var errors = Validate(candidate);
        if (errors.Count != 0)
            throw new ArgumentException(string.Join(" ", errors), nameof(correction));

        usage.Corrections.Add(correction);
    }

    private static void ValidateCorrections(VehicleUsageData usage, List<string> errors)
    {
        foreach (var duplicateId in usage.Corrections
                     .GroupBy(correction => correction.Id)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            errors.Add($"Usage correction {duplicateId} occurs more than once.");
        }

        foreach (var correction in usage.Corrections)
        {
            if (correction.Id == Guid.Empty)
                errors.Add("Usage corrections require a stable identifier.");
            if (correction.RecordedAt == default)
                errors.Add($"Usage correction {correction.Id} requires a recorded time.");
            if (string.IsNullOrWhiteSpace(correction.Reason))
                errors.Add($"Usage correction {correction.Id} requires a reason.");
            if (correction.OperatingSecondsDelta == 0
                && correction.CompletedTripsDelta == 0
                && correction.DistanceKilometresDelta is null or 0)
            {
                errors.Add($"Usage correction {correction.Id} must adjust at least one counter.");
            }

            if (usage.TrackedDistanceKilometres is null && correction.DistanceKilometresDelta is not null)
                errors.Add($"Usage correction {correction.Id} cannot adjust unavailable distance.");
        }
    }

    private static void ValidateEffectiveTotals(VehicleUsageData usage, List<string> errors)
    {
        try
        {
            var totals = CalculateTotalsCore(usage);
            if (totals.OperatingSeconds < 0)
                errors.Add("Effective operating time must not be negative.");
            if (totals.CompletedTrips < 0)
                errors.Add("Effective completed trips must not be negative.");
            if (totals.DistanceKilometres < 0)
                errors.Add("Effective distance must not be negative.");
        }
        catch (OverflowException)
        {
            errors.Add("Usage counters exceed the supported range.");
        }
    }

    private static VehicleUsageTotals CalculateTotalsCore(VehicleUsageData usage)
    {
        var operatingSeconds = usage.TrackedOperatingSeconds;
        var completedTrips = usage.TrackedCompletedTrips;
        var distanceKilometres = usage.TrackedDistanceKilometres;

        checked
        {
            foreach (var correction in usage.Corrections)
            {
                operatingSeconds += correction.OperatingSecondsDelta;
                completedTrips += correction.CompletedTripsDelta;
                if (distanceKilometres is not null)
                    distanceKilometres += correction.DistanceKilometresDelta ?? 0;
            }
        }

        return new VehicleUsageTotals(operatingSeconds, completedTrips, distanceKilometres);
    }
}
