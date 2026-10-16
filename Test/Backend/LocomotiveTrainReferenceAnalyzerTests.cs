// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service.Validation;
using Moba.Domain;
using Moba.Domain.Enum;

[TestFixture]
internal static class LocomotiveTrainReferenceAnalyzerTests
{
    private static readonly LocomotiveTrainReferenceAnalyzer _analyzer = new();

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    public static void Analyze_PreservesEveryPosition_AndCopiesStableLocations(int count)
    {
        var locomotive = new Locomotive { Name = "Harbor pilot" };
        var train = new Train { Name = "Morning service" };
        train.Vehicles.AddRange(Enumerable.Range(0, count).Select(_ =>
            new Vehicle { VehicleKind = TrainVehicleKind.Locomotive, VehicleId = locomotive.Id }));
        var project = new Project { Name = "Yard", Locomotives = [locomotive], Trains = [train] };

        var result = _analyzer.Analyze(project, LocomotiveTrainSource.EditedProjectData, locomotive.Id);
        var repeated = _analyzer.Analyze(project, LocomotiveTrainSource.EditedProjectData, locomotive.Id);
        train.Name = "Renamed later";
        train.Vehicles.Clear();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TargetState, Is.EqualTo(LocomotiveTrainTargetState.Exists));
            Assert.That(result.Coverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Complete));
            Assert.That(result.Findings, Has.Length.EqualTo(count));
            Assert.That(result.Findings.Select(finding => finding.Location.VehicleIndex), Is.EqualTo(Enumerable.Range(0, count)));
            Assert.That(result.Findings.Select(finding => finding.FindingId), Is.EqualTo(repeated.Findings.Select(finding => finding.FindingId)));
            Assert.That(result.Findings.All(finding => finding.Location.TrainName == "Morning service"), Is.True);
            Assert.That(result.Findings.All(finding => finding.RuleCode == "LTU001"), Is.True);
        }
    }

    [Test]
    public static void Analyze_MissingTargetBecomesNeutralUseAfterRepair_WithoutChangingOccurrenceIdentity()
    {
        var missingId = Guid.NewGuid();
        var project = new Project
        {
            Trains = [new Train { Name = "Broken consist", Vehicles = [new Vehicle { VehicleId = missingId }, new Vehicle { VehicleId = missingId }] }]
        };
        var missing = _analyzer.Analyze(project, LocomotiveTrainSource.EditedProjectData, missingId);
        project.Locomotives.Add(new Locomotive { Id = missingId });
        var repaired = _analyzer.Analyze(project, LocomotiveTrainSource.EditedProjectData, missingId);

        Assert.That(missing.Findings, Has.Length.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.Findings.All(finding => finding.RuleCode == "LTU002"), Is.True);
            Assert.That(missing.TargetState, Is.EqualTo(LocomotiveTrainTargetState.Missing));
            Assert.That(repaired.Findings.All(finding => finding.RuleCode == "LTU001"), Is.True);
            Assert.That(repaired.TargetState, Is.EqualTo(LocomotiveTrainTargetState.Exists));
            Assert.That(missing.Findings.Select(finding => finding.FindingId), Is.EqualTo(repaired.Findings.Select(finding => finding.FindingId)));
        }
    }

    [Test]
    public static void Analyze_EmptyLocomotiveEntry_IsOnePreciseBrokenReference()
    {
        var project = new Project { Trains = [new Train { Vehicles = [new Vehicle { VehicleId = Guid.Empty }] }] };

        var result = _analyzer.Analyze(project, LocomotiveTrainSource.LoadedRuntimeDefinition);

        Assert.That(result.Findings, Has.Length.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Findings[0].IsMissingReference, Is.True);
            Assert.That(result.Findings[0].Location.TargetId, Is.EqualTo(Guid.Empty));
            Assert.That(result.Findings[0].Location.VehiclePosition, Is.EqualTo(1));
        }
    }

    [Test]
    public static void Analyze_DistinguishesMissingTarget_AndIgnoresOtherVehicleKindsAndProjects()
    {
        var targetId = Guid.NewGuid();
        var foreign = new Project { Locomotives = [new Locomotive { Id = targetId }] };
        var project = new Project
        {
            Trains = [new Train { Vehicles = [new Vehicle { VehicleKind = TrainVehicleKind.PassengerWagon, VehicleId = targetId }] }]
        };

        var result = _analyzer.Analyze(project, LocomotiveTrainSource.EditedProjectData, targetId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(foreign.Locomotives.Single().Id, Is.EqualTo(targetId));
            Assert.That(result.TargetState, Is.EqualTo(LocomotiveTrainTargetState.Missing));
            Assert.That(result.Findings, Is.Empty);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public static void Analyze_AmbiguousTrainIdentity_ReturnsUnknownInsteadOfFabricatingLocations(bool duplicate)
    {
        var train = new Train { Id = duplicate ? Guid.NewGuid() : Guid.Empty };
        var project = new Project { Trains = duplicate ? [train, new Train { Id = train.Id }] : [train] };

        var result = _analyzer.Analyze(project, LocomotiveTrainSource.LoadedRuntimeDefinition);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Coverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
            Assert.That(result.Coverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.InvalidIdentity));
            Assert.That(result.Findings, Is.Empty);
        }
    }

    [Test]
    public static void Analyze_MultipleTrains_OrderByStableIdentity_AndSeparateSources()
    {
        var locomotive = new Locomotive();
        var first = new Train { Id = Guid.Parse("10000000-0000-0000-0000-000000000001") };
        var second = new Train { Id = Guid.Parse("20000000-0000-0000-0000-000000000001") };
        foreach (var train in new[] { first, second })
            train.Vehicles.Add(new Vehicle { VehicleKind = TrainVehicleKind.Locomotive, VehicleId = locomotive.Id });
        var project = new Project { Locomotives = [locomotive], Trains = [second, first] };

        var editor = _analyzer.Analyze(project, LocomotiveTrainSource.EditedProjectData, locomotive.Id);
        var runtime = _analyzer.Analyze(project, LocomotiveTrainSource.LoadedRuntimeDefinition, locomotive.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editor.Findings.Select(finding => finding.Location.TrainId), Is.EqualTo(new[] { first.Id, second.Id }));
            Assert.That(editor.Findings.Select(finding => finding.FindingId).Intersect(runtime.Findings.Select(finding => finding.FindingId)), Is.Empty);
            Assert.That(editor.Findings[0].Location.FieldPath, Is.EqualTo("vehicles[0].vehicleId"));
            Assert.That(editor.Findings[0].Location.VehiclePosition, Is.EqualTo(1));
        }
    }
}
