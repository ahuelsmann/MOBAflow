// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Moba.Domain;
using System.Linq;

/// <summary>
/// Service for validating delete operations to prevent breaking references.
/// Checks if entities are referenced elsewhere before allowing deletion.
/// </summary>
public class ValidationService
{
    private readonly Project _project;

    public ValidationService(Project project)
    {
        _project = project;
    }

    /// <summary>
    /// Validates if a Journey can be deleted.
    /// A Journey cannot be deleted if it's referenced by another Journey's NextJourney property.
    /// </summary>
    public ValidationResult CanDeleteJourney(Journey journey)
    {
        if (journey == null)
            return ValidationResult.Failure("Journey is null");

        // Check if any Journey references this Journey as NextJourney
        var referencingJourney = _project.Journeys
            .FirstOrDefault(j => j.NextJourney == journey.Name);

        if (referencingJourney != null)
        {
            return ValidationResult.Failure(
                $"Journey '{journey.Name}' cannot be deleted because it is referenced by Journey '{referencingJourney.Name}' as NextJourney.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates if a Workflow can be deleted.
    /// A Workflow cannot be deleted if it's assigned to any Station or Platform.
    /// </summary>
    public ValidationResult CanDeleteWorkflow(Workflow workflow)
    {
        if (workflow == null)
            return ValidationResult.Failure("Workflow is null");

        // Check if any Station uses this Workflow
        foreach (var journey in _project.Journeys)
        {
            var referencingStation = journey.Stations
                .FirstOrDefault(s => s.Flow?.Name == workflow.Name);

            if (referencingStation != null)
            {
                return ValidationResult.Failure(
                    $"Workflow '{workflow.Name}' cannot be deleted because it is used by Station '{referencingStation.Name}' in Journey '{journey.Name}'.");
            }

            // Check if any Platform uses this Workflow
            foreach (var station in journey.Stations)
            {
                var referencingPlatform = station.Platforms
                    .FirstOrDefault(p => p.Flow?.Name == workflow.Name);

                if (referencingPlatform != null)
                {
                    return ValidationResult.Failure(
                        $"Workflow '{workflow.Name}' cannot be deleted because it is used by Platform '{referencingPlatform.Name}' at Station '{station.Name}' in Journey '{journey.Name}'.");
                }
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates if a Train can be deleted.
    /// A Train cannot be deleted if it's assigned to any Journey.
    /// </summary>
    public ValidationResult CanDeleteTrain(Train train)
    {
        if (train == null)
            return ValidationResult.Failure("Train is null");

        // Check if any Journey uses this Train
        var referencingJourney = _project.Journeys
            .FirstOrDefault(j => j.Train?.Name == train.Name);

        if (referencingJourney != null)
        {
            return ValidationResult.Failure(
                $"Train '{train.Name}' cannot be deleted because it is assigned to Journey '{referencingJourney.Name}'.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates if a Locomotive can be deleted.
    /// A Locomotive cannot be deleted if it's part of any Train's composition.
    /// </summary>
    public ValidationResult CanDeleteLocomotive(Locomotive locomotive)
    {
        if (locomotive == null)
            return ValidationResult.Failure("Locomotive is null");

        // Check if any Train uses this Locomotive
        var referencingTrain = _project.Trains
            .FirstOrDefault(t => t.Locomotives.Any(l => l.Name == locomotive.Name));

        if (referencingTrain != null)
        {
            return ValidationResult.Failure(
                $"Locomotive '{locomotive.Name}' cannot be deleted because it is used in Train '{referencingTrain.Name}'.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates if a Wagon can be deleted.
    /// A Wagon cannot be deleted if it's part of any Train's composition.
    /// </summary>
    public ValidationResult CanDeleteWagon(Wagon wagon)
    {
        if (wagon == null)
            return ValidationResult.Failure("Wagon is null");

        // Check if any Train uses this Wagon
        var referencingTrain = _project.Trains
            .FirstOrDefault(t => t.Wagons.Any(w => w.Name == wagon.Name));

        if (referencingTrain != null)
        {
            return ValidationResult.Failure(
                $"Wagon '{wagon.Name}' cannot be deleted because it is used in Train '{referencingTrain.Name}'.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates if a Station can be deleted.
    /// A Station can always be deleted (no external references).
    /// </summary>
    public ValidationResult CanDeleteStation(Station station)
    {
        if (station == null)
            return ValidationResult.Failure("Station is null");

        return ValidationResult.Success();
    }
}
