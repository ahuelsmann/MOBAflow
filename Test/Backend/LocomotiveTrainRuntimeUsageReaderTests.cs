// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service.Validation;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.Test.Helpers;
using Moq;

[TestFixture]
internal sealed class LocomotiveTrainRuntimeUsageReaderTests
{
    [Test]
    public async Task Read_UsesActualDisconnectedNonSelectedClone_NotTheOriginalEditor()
    {
        await using var fixture = new TestProjectRuntimeHost();
        var project = CreateProject();
        var other = new Project();
        await fixture.Host.LoadAsync([project, other]);
        fixture.Host.SelectProject(other.Id);
        var originalName = project.Trains[0].Name;
        project.Trains[0].Name = "Unsaved replacement";
        project.Trains[0].Vehicles.Clear();
        var reader = (ILocomotiveTrainRuntimeUsageReader)fixture.Host;
        var request = Request(project);

        var observed = await reader.ReadAsync(request);

        Assert.That(fixture.Host.Selected!.ProjectId, Is.EqualTo(other.Id));
        Assert.That(fixture.Host.ConnectedProjectIds, Is.Empty);
        Assert.That(observed.Definition.Findings, Has.Length.EqualTo(1));
        Assert.That(observed.Definition.Findings[0].Location.TrainName, Is.EqualTo(originalName));
        Assert.That(observed.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Complete));
        Assert.That(observed.RuntimeCoverage, Is.EqualTo(LocomotiveTrainRuntimeCoverage.Partial));
        AssertUnknownCategories(observed);
        Assert.That(observed.ObservedAtUtc.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(await reader.IsCurrentAsync(observed), Is.EqualTo(LocomotiveTrainObservationValidity.Current));
    }

    [Test]
    public async Task Read_MissingRuntimeTarget_IsIndependentOfEditorExistence()
    {
        await using var fixture = new TestProjectRuntimeHost();
        var project = CreateProject();
        var target = project.Locomotives[0];
        project.Locomotives.Clear();
        await fixture.Host.LoadAsync([project]);
        project.Locomotives.Add(target);

        var observation = await ((ILocomotiveTrainRuntimeUsageReader)fixture.Host).ReadAsync(Request(project));

        Assert.That(observation.Definition.TargetState, Is.EqualTo(LocomotiveTrainTargetState.Missing));
        Assert.That(observation.Definition.Findings.Single().IsMissingReference, Is.True);
        Assert.That(new LocomotiveTrainReferenceAnalyzer().Analyze(project, LocomotiveTrainSource.EditedProjectData, target.Id).TargetState,
            Is.EqualTo(LocomotiveTrainTargetState.Exists));
    }

    [Test]
    public async Task Read_ZeroReferences_OnlyCompletesCategoryA()
    {
        await using var fixture = new TestProjectRuntimeHost();
        var project = CreateProject();
        project.Trains.Clear();
        await fixture.Host.LoadAsync([project]);

        var observed = await ((ILocomotiveTrainRuntimeUsageReader)fixture.Host).ReadAsync(Request(project));

        Assert.That(observed.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Complete));
        Assert.That(observed.Definition.Findings, Is.Empty);
        Assert.That(observed.RuntimeCoverage, Is.EqualTo(LocomotiveTrainRuntimeCoverage.Partial));
        AssertUnknownCategories(observed);
    }

    [Test]
    public async Task Read_ForeignLoadWithSameGuid_AndAbsentOwner_AreExplicitUnknown()
    {
        await using var fixture = new TestProjectRuntimeHost();
        var project = CreateProject();
        var reader = (ILocomotiveTrainRuntimeUsageReader)fixture.Host;
        var absent = await reader.ReadAsync(Request(project));
        await fixture.Host.LoadAsync([project]);
        var foreign = new Project { Id = project.Id };
        var mismatch = await reader.ReadAsync(new(Guid.NewGuid(), foreign, foreign.Id));

        Assert.That(absent.LoadedDefinitionCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.OwnerUnavailable));
        Assert.That(mismatch.LoadedDefinitionCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.ContextMismatch));
        Assert.That(mismatch.RuntimeCoverage, Is.EqualTo(LocomotiveTrainRuntimeCoverage.Unknown));
        Assert.That(mismatch.Receipt, Is.Null);
    }

    [Test]
    public async Task SuccessfulRead_ThenSameIdReplacement_RejectsOldAndReusedReceipts()
    {
        await using var fixture = new TestProjectRuntimeHost();
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]);
        var reader = (ILocomotiveTrainRuntimeUsageReader)fixture.Host;
        var observation = await reader.ReadAsync(Request(project));
        Assert.That(await reader.IsCurrentAsync(observation with { RequestId = Guid.NewGuid() }),
            Is.EqualTo(LocomotiveTrainObservationValidity.Stale));
        await fixture.Host.ReplaceAsync(project);

        Assert.That(await reader.IsCurrentAsync(observation), Is.EqualTo(LocomotiveTrainObservationValidity.Stale));
        var current = await reader.ReadAsync(Request(project));
        Assert.That(await reader.IsCurrentAsync(current), Is.EqualTo(LocomotiveTrainObservationValidity.Current));
    }

    [Test]
    public async Task SuccessfulRead_ThenDispose_RejectsReceiptAndFurtherReads()
    {
        await using var fixture = new TestProjectRuntimeHost();
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]);
        var reader = (ILocomotiveTrainRuntimeUsageReader)fixture.Host;
        var observation = await reader.ReadAsync(Request(project));

        await fixture.Host.DisposeAsync();

        Assert.That(await reader.IsCurrentAsync(observation), Is.EqualTo(LocomotiveTrainObservationValidity.Unavailable));
        Assert.That((await reader.ReadAsync(Request(project))).LoadedDefinitionCoverage.Reason,
            Is.EqualTo(LocomotiveTrainCoverageReason.OwnerUnavailable));
    }

    [Test]
    public async Task DefinitionUpdate_WaitsForConsistentCapture_AndInvalidatesItsReceipt()
    {
        Task? update = null;
        var project = CreateProject();
        TestProjectRuntimeHost? fixture = null;
        var analyzer = new Mock<ILocomotiveTrainReferenceAnalyzer>();
        analyzer.Setup(value => value.Analyze(It.IsAny<Project>(), It.IsAny<LocomotiveTrainSource>(), It.IsAny<Guid?>()))
            .Returns((Project clone, LocomotiveTrainSource source, Guid? target) =>
            {
                project.Trains[0].Name = "Updated definition";
                update = fixture!.Host.UpdateAsync(project);
                Assert.That(update.IsCompleted, Is.False, "Definition refresh must wait for the active capture lock.");
                return new LocomotiveTrainReferenceAnalyzer().Analyze(clone, source, target);
            });
        await using var owned = new TestProjectRuntimeHost(analyzer.Object);
        fixture = owned;
        await fixture.Host.LoadAsync([project]);
        var reader = (ILocomotiveTrainRuntimeUsageReader)fixture.Host;

        var observation = await reader.ReadAsync(Request(project));
        await update!.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(observation.Definition.Findings.Single().Location.TrainName, Is.EqualTo("Morning service"));
        Assert.That(await reader.IsCurrentAsync(observation), Is.EqualTo(LocomotiveTrainObservationValidity.Stale));
    }

    [Test]
    public async Task DisposeDuringCapture_CannotReturnCompleteEvenThoughItBypassesTheHostGate()
    {
        TestProjectRuntimeHost? fixture = null;
        Task? disposal = null;
        var analyzer = new Mock<ILocomotiveTrainReferenceAnalyzer>();
        analyzer.Setup(value => value.Analyze(It.IsAny<Project>(), It.IsAny<LocomotiveTrainSource>(), It.IsAny<Guid?>()))
            .Returns((Project clone, LocomotiveTrainSource source, Guid? target) =>
            {
                disposal = fixture!.Host.DisposeAsync().AsTask();
                return new LocomotiveTrainReferenceAnalyzer().Analyze(clone, source, target);
            });
        await using var owned = new TestProjectRuntimeHost(analyzer.Object);
        fixture = owned;
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]);

        var observation = await ((ILocomotiveTrainRuntimeUsageReader)fixture.Host).ReadAsync(Request(project));
        await disposal!.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(observation.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
        Assert.That(observation.Receipt, Is.Null);
    }

    [Test]
    public async Task FailedCapture_ReturnsQueryFailed_AndCancellationPropagates()
    {
        var analyzer = new Mock<ILocomotiveTrainReferenceAnalyzer>();
        analyzer.Setup(value => value.Analyze(It.IsAny<Project>(), It.IsAny<LocomotiveTrainSource>(), It.IsAny<Guid?>()))
            .Throws(new InvalidOperationException("Synthetic query failure"));
        await using var fixture = new TestProjectRuntimeHost(analyzer.Object);
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]);
        var reader = (ILocomotiveTrainRuntimeUsageReader)fixture.Host;

        Assert.That((await reader.ReadAsync(Request(project))).LoadedDefinitionCoverage.Reason,
            Is.EqualTo(LocomotiveTrainCoverageReason.QueryFailed));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await reader.ReadAsync(Request(project), cancellation.Token));
    }

    [Test]
    public async Task Query_AndReceiptCheck_LeaveOwnersDefinitionsCommandsAndWorkflowsUntouched()
    {
        var analyzer = new Mock<ILocomotiveTrainReferenceAnalyzer>();
        analyzer.Setup(value => value.Analyze(It.IsAny<Project>(), It.IsAny<LocomotiveTrainSource>(), It.IsAny<Guid?>()))
            .Returns((Project project, LocomotiveTrainSource source, Guid? target) =>
                new LocomotiveTrainReferenceAnalyzer().Analyze(project, source, target));
        await using var fixture = new TestProjectRuntimeHost(analyzer.Object);
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]);
        var owner = fixture.Host.Get(project.Id);
        var before = fixture.Host.Snapshots.Single();
        foreach (var z21 in fixture.Z21s) z21.Invocations.Clear();
        var reader = (ILocomotiveTrainRuntimeUsageReader)fixture.Host;

        var first = await reader.ReadAsync(Request(project));
        var second = await reader.ReadAsync(Request(project));
        Assert.That(await reader.IsCurrentAsync(first), Is.EqualTo(LocomotiveTrainObservationValidity.Current));
        Assert.That(await reader.IsCurrentAsync(second), Is.EqualTo(LocomotiveTrainObservationValidity.Current));

        Assert.That(fixture.Host.Get(project.Id), Is.SameAs(owner));
        Assert.That(fixture.Host.Snapshots.Single(), Is.SameAs(before));
        Assert.That(project.Trains[0].Vehicles, Has.Count.EqualTo(1));
        foreach (var z21 in fixture.Z21s) z21.VerifyNoOtherCalls();
        Mock.Get(fixture.Services.ActionExecutor).VerifyNoOtherCalls();
        analyzer.Verify(value => value.Analyze(It.IsAny<Project>(), It.IsAny<LocomotiveTrainSource>(), It.IsAny<Guid?>()),
            Times.Exactly(2), "Receipt checks compare identities without scanning again.");
        Assert.That(first.Definition.Findings.Select(finding => finding.FindingId),
            Is.EqualTo(second.Definition.Findings.Select(finding => finding.FindingId)));
    }

    private static Project CreateProject()
    {
        var locomotive = new Locomotive { Name = "Harbor pilot" };
        return new Project
        {
            Name = "Yard", Locomotives = [locomotive],
            Trains = [new Train { Name = "Morning service", Vehicles = [new Vehicle { VehicleKind = TrainVehicleKind.Locomotive, VehicleId = locomotive.Id }] }]
        };
    }

    private static LocomotiveTrainUsageRequest Request(Project project) => new(Guid.NewGuid(), project, project.Id,
        project.Locomotives.FirstOrDefault()?.Id ?? project.Trains.FirstOrDefault()?.Vehicles.FirstOrDefault()?.VehicleId);

    private static void AssertUnknownCategories(LocomotiveTrainRuntimeObservation observation)
    {
        Assert.That(observation.RetainedDefinitionsCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
        Assert.That(observation.OperationalUsageCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
        Assert.That(observation.RetainedDefinitionsCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.NotInspectedInSlice));
        Assert.That(observation.OperationalUsageCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.NotInspectedInSlice));
    }
}
