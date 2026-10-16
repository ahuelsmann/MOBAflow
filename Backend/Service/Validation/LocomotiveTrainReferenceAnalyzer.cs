// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.Validation;

using Domain;
using Domain.Enum;
using Interface;
using System.Collections.Immutable;

/// <summary>Analyzes the project-local locomotive-to-train-entry relationship without changing the model.</summary>
public sealed class LocomotiveTrainReferenceAnalyzer : ILocomotiveTrainReferenceAnalyzer
{
    /// <inheritdoc />
    public LocomotiveTrainReferenceResult Analyze(Project project, LocomotiveTrainSource source, Guid? locomotiveId = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (locomotiveId == Guid.Empty)
            throw new ArgumentException("A requested locomotive ID must not be empty.", nameof(locomotiveId));
        if (!Enum.IsDefined(source))
            throw new ArgumentOutOfRangeException(nameof(source));

        var locomotiveIds = project.Locomotives.GroupBy(locomotive => locomotive.Id)
            .ToDictionary(group => group.Key, group => group.Count());
        if (HasInvalidIdentity(project, locomotiveIds))
        {
            return new(project.Id, project.Name, source, LocomotiveTrainTargetState.Ambiguous, [],
                new(LocomotiveTrainCoverageState.Unknown, LocomotiveTrainCoverageReason.InvalidIdentity));
        }

        return new(project.Id, project.Name, source, GetTargetState(locomotiveId, locomotiveIds),
            FindReferences(project, source, locomotiveId, locomotiveIds),
            new(LocomotiveTrainCoverageState.Complete, LocomotiveTrainCoverageReason.None));
    }

    private static bool HasInvalidIdentity(Project project, Dictionary<Guid, int> locomotiveIds)
    {
        var trainIds = project.Trains.Select(train => train.Id).ToArray();
        return project.Id == Guid.Empty || trainIds.Contains(Guid.Empty) || trainIds.Distinct().Count() != trainIds.Length
            || locomotiveIds.Any(pair => pair.Key == Guid.Empty || pair.Value != 1);
    }

    private static LocomotiveTrainTargetState GetTargetState(Guid? locomotiveId, Dictionary<Guid, int> locomotiveIds)
    {
        if (!locomotiveId.HasValue)
            return LocomotiveTrainTargetState.NotRequested;
        if (!locomotiveIds.TryGetValue(locomotiveId.Value, out var count))
            return LocomotiveTrainTargetState.Missing;
        return count == 1 ? LocomotiveTrainTargetState.Exists : LocomotiveTrainTargetState.Ambiguous;
    }

    private static ImmutableArray<LocomotiveTrainFinding> FindReferences(
        Project project, LocomotiveTrainSource source, Guid? locomotiveId, Dictionary<Guid, int> locomotiveIds)
    {
        var findings = ImmutableArray.CreateBuilder<LocomotiveTrainFinding>();
        foreach (var train in project.Trains.OrderBy(train => train.Id))
        {
            for (var index = 0; index < train.Vehicles.Count; index++)
            {
                var vehicle = train.Vehicles[index];
                if (vehicle.VehicleKind != TrainVehicleKind.Locomotive
                    || locomotiveId.HasValue && vehicle.VehicleId != locomotiveId.Value)
                    continue;
                var missing = !locomotiveIds.ContainsKey(vehicle.VehicleId);
                var location = new LocomotiveTrainLocation(source, project.Id, project.Name, train.Id, train.Name, index, vehicle.VehicleId);
                findings.Add(new(missing ? "LTU002" : "LTU001",
                    $"ltu:{source}:{project.Id:N}:{train.Id:N}:{index}:{vehicle.VehicleId:N}", location,
                    $"{(missing ? "Missing locomotive reference" : "Used in")}: Train {train.Name} - vehicle {index + 1}"));
            }
        }

        return findings.ToImmutable();
    }
}
