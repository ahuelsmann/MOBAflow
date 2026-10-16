// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service.Validation;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.Test.Helpers;
using Moq;

[TestFixture]
internal static class LocomotiveTrainRuntimeUsageReaderTests
{
    [Test]
    public static async Task CancellationDuringCapture_ReleasesLocks_WithoutExecutingEffects()
    {
        using var cancellation = new CancellationTokenSource();
        var analyzer = new Mock<ILocomotiveTrainReferenceAnalyzer>();
        analyzer.Setup(value => value.Analyze(It.IsAny<Project>(), It.IsAny<LocomotiveTrainSource>(), It.IsAny<Guid?>()))
            .Returns((Project project, LocomotiveTrainSource source, Guid? target) =>
            {
                cancellation.Cancel();
                return new LocomotiveTrainReferenceAnalyzer().Analyze(project, source, target);
            });
        var fixture = new TestProjectRuntimeHost(analyzer.Object);
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        var reader = fixture.Host;
        foreach (var z21 in fixture.Z21s) z21.Invocations.Clear();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await reader.ReadAsync(Request(project), cancellation.Token).ConfigureAwait(false)).ConfigureAwait(false);
        var next = await reader.ReadAsync(Request(project)).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        Assert.That(next.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Complete));
        foreach (var z21 in fixture.Z21s) z21.VerifyNoOtherCalls();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public static async Task InvalidRequestIdentity_IsRejectedBeforeAnyOwnerIsCreated(int invalidPart)
    {
        var fixture = new TestProjectRuntimeHost();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        var request = Request(project);
        request = invalidPart switch
        {
            0 => request with { RequestId = Guid.Empty },
            1 => request with { ProjectId = Guid.Empty },
            2 => request with { ProjectId = Guid.NewGuid() },
            _ => request with { LocomotiveId = Guid.Empty }
        };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await ((ILocomotiveTrainRuntimeUsageReader)fixture.Host).ReadAsync(request).ConfigureAwait(false)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Host.Runtimes, Is.Empty);
            Assert.That(fixture.Z21s, Is.Empty);
        }
    }

    [Test]
    public static async Task Read_UsesActualDisconnectedNonSelectedClone_NotTheOriginalEditor()
    {
        var fixture = new TestProjectRuntimeHost();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        var other = new Project();
        await fixture.Host.LoadAsync([project, other]).ConfigureAwait(false);
        fixture.Host.SelectProject(other.Id);
        var originalName = project.Trains[0].Name;
        project.Trains[0].Name = "Unsaved replacement";
        project.Trains[0].Vehicles.Clear();
        var reader = fixture.Host;
        var request = Request(project);

        var observed = await reader.ReadAsync(request).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Host.Selected!.ProjectId, Is.EqualTo(other.Id));
            Assert.That(fixture.Host.ConnectedProjectIds, Is.Empty);
            Assert.That(observed.Definition.Findings, Has.Length.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observed.Definition.Findings[0].Location.TrainName, Is.EqualTo(originalName));
            Assert.That(observed.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Complete));
            Assert.That(observed.RuntimeCoverage, Is.EqualTo(LocomotiveTrainRuntimeCoverage.Partial));
        }
        AssertUnknownCategories(observed);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observed.ObservedAtUtc.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(await reader.IsCurrentAsync(observed).ConfigureAwait(false), Is.EqualTo(LocomotiveTrainObservationValidity.Current));
        }
    }

    [Test]
    public static async Task Read_MissingRuntimeTarget_IsIndependentOfEditorExistence()
    {
        var fixture = new TestProjectRuntimeHost();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        var target = project.Locomotives[0];
        project.Locomotives.Clear();
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        project.Locomotives.Add(target);

        var observation = await ((ILocomotiveTrainRuntimeUsageReader)fixture.Host).ReadAsync(Request(project)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(observation.Definition.TargetState, Is.EqualTo(LocomotiveTrainTargetState.Missing));
            Assert.That(observation.Definition.Findings.Single().IsMissingReference, Is.True);
            Assert.That(new LocomotiveTrainReferenceAnalyzer().Analyze(project, LocomotiveTrainSource.EditedProjectData, target.Id).TargetState,
                Is.EqualTo(LocomotiveTrainTargetState.Exists));
        }
    }

    [Test]
    public static async Task Read_ZeroReferences_OnlyCompletesCategoryA()
    {
        var fixture = new TestProjectRuntimeHost();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        project.Trains.Clear();
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);

        var observed = await ((ILocomotiveTrainRuntimeUsageReader)fixture.Host).ReadAsync(Request(project)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(observed.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Complete));
            Assert.That(observed.Definition.Findings, Is.Empty);
            Assert.That(observed.RuntimeCoverage, Is.EqualTo(LocomotiveTrainRuntimeCoverage.Partial));
        }
        AssertUnknownCategories(observed);
    }

    [Test]
    public static async Task Read_ForeignLoadWithSameGuid_AndAbsentOwner_AreExplicitUnknown()
    {
        var fixture = new TestProjectRuntimeHost();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        var reader = fixture.Host;
        var absent = await reader.ReadAsync(Request(project)).ConfigureAwait(false);
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        var foreign = new Project { Id = project.Id };
        var mismatch = await reader.ReadAsync(new(Guid.NewGuid(), foreign, foreign.Id)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(absent.LoadedDefinitionCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.OwnerUnavailable));
            Assert.That(absent.Definition.ProjectName, Is.EqualTo(project.Id.ToString("D")),
                "Unavailable runtime evidence must not borrow a definition name from the editor identity guard.");
            Assert.That(mismatch.LoadedDefinitionCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.ContextMismatch));
            Assert.That(mismatch.RuntimeCoverage, Is.EqualTo(LocomotiveTrainRuntimeCoverage.Unknown));
            Assert.That(mismatch.Receipt, Is.Null);
        }
    }

    [Test]
    public static async Task SuccessfulRead_ThenSameIdReplacement_RejectsOldAndReusedReceipts()
    {
        var fixture = new TestProjectRuntimeHost();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        var reader = fixture.Host;
        var observation = await reader.ReadAsync(Request(project)).ConfigureAwait(false);
        Assert.That(await reader.IsCurrentAsync(observation with { RequestId = Guid.NewGuid() }).ConfigureAwait(false),
            Is.EqualTo(LocomotiveTrainObservationValidity.Stale));
        await fixture.Host.ReplaceAsync(project).ConfigureAwait(false);

        Assert.That(await reader.IsCurrentAsync(observation).ConfigureAwait(false), Is.EqualTo(LocomotiveTrainObservationValidity.Stale));
        var current = await reader.ReadAsync(Request(project)).ConfigureAwait(false);
        Assert.That(await reader.IsCurrentAsync(current).ConfigureAwait(false), Is.EqualTo(LocomotiveTrainObservationValidity.Current));
    }

    [Test]
    public static async Task SuccessfulRead_ThenDispose_RejectsReceiptAndFurtherReads()
    {
        var fixture = new TestProjectRuntimeHost();
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        var reader = fixture.Host;
        var observation = await reader.ReadAsync(Request(project)).ConfigureAwait(false);

        await fixture.Host.DisposeAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await reader.IsCurrentAsync(observation).ConfigureAwait(false), Is.EqualTo(LocomotiveTrainObservationValidity.Unavailable));
            Assert.That((await reader.ReadAsync(Request(project)).ConfigureAwait(false)).LoadedDefinitionCoverage.Reason,
                Is.EqualTo(LocomotiveTrainCoverageReason.OwnerUnavailable));
        }
    }

    [Test]
    public static async Task DefinitionUpdate_WaitsForConsistentCapture_AndInvalidatesItsReceipt()
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
        var owned = new TestProjectRuntimeHost(analyzer.Object);
        await using var ownedLifetime = owned.ConfigureAwait(false);
        fixture = owned;
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        var reader = fixture.Host;

        var observation = await reader.ReadAsync(Request(project)).ConfigureAwait(false);
        await update!.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(observation.Definition.Findings.Single().Location.TrainName, Is.EqualTo("Morning service"));
            Assert.That(await reader.IsCurrentAsync(observation).ConfigureAwait(false), Is.EqualTo(LocomotiveTrainObservationValidity.Stale));
        }
    }

    [Test]
    public static async Task DisposeDuringCapture_CannotReturnCompleteEvenThoughItBypassesTheHostGate()
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
        var owned = new TestProjectRuntimeHost(analyzer.Object);
        await using var ownedLifetime = owned.ConfigureAwait(false);
        fixture = owned;
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);

        var observation = await ((ILocomotiveTrainRuntimeUsageReader)fixture.Host).ReadAsync(Request(project)).ConfigureAwait(false);
        await disposal!.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(observation.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
            Assert.That(observation.Receipt, Is.Null);
        }
    }

    [Test]
    public static async Task FailedCapture_ReturnsQueryFailed_AndCancellationPropagates()
    {
        var analyzer = new Mock<ILocomotiveTrainReferenceAnalyzer>();
        analyzer.Setup(value => value.Analyze(It.IsAny<Project>(), It.IsAny<LocomotiveTrainSource>(), It.IsAny<Guid?>()))
            .Throws(new InvalidOperationException("Synthetic query failure"));
        var fixture = new TestProjectRuntimeHost(analyzer.Object);
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        var reader = fixture.Host;

        Assert.That((await reader.ReadAsync(Request(project)).ConfigureAwait(false)).LoadedDefinitionCoverage.Reason,
            Is.EqualTo(LocomotiveTrainCoverageReason.QueryFailed));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await reader.ReadAsync(Request(project), cancellation.Token).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Test]
    public static async Task Query_AndReceiptCheck_LeaveOwnersDefinitionsCommandsAndWorkflowsUntouched()
    {
        var analyzer = new Mock<ILocomotiveTrainReferenceAnalyzer>();
        analyzer.Setup(value => value.Analyze(It.IsAny<Project>(), It.IsAny<LocomotiveTrainSource>(), It.IsAny<Guid?>()))
            .Returns((Project project, LocomotiveTrainSource source, Guid? target) =>
                new LocomotiveTrainReferenceAnalyzer().Analyze(project, source, target));
        var fixture = new TestProjectRuntimeHost(analyzer.Object);
        await using var fixtureLifetime = fixture.ConfigureAwait(false);
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]).ConfigureAwait(false);
        var owner = fixture.Host.Get(project.Id);
        var before = fixture.Host.Snapshots.Single();
        foreach (var z21 in fixture.Z21s) z21.Invocations.Clear();
        var reader = fixture.Host;

        var first = await reader.ReadAsync(Request(project)).ConfigureAwait(false);
        var second = await reader.ReadAsync(Request(project)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await reader.IsCurrentAsync(first).ConfigureAwait(false), Is.EqualTo(LocomotiveTrainObservationValidity.Current));
            Assert.That(await reader.IsCurrentAsync(second).ConfigureAwait(false), Is.EqualTo(LocomotiveTrainObservationValidity.Current));

            Assert.That(fixture.Host.Get(project.Id), Is.SameAs(owner));
            Assert.That(fixture.Host.Snapshots.Single(), Is.SameAs(before));
            Assert.That(project.Trains[0].Vehicles, Has.Count.EqualTo(1));
        }
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observation.RetainedDefinitionsCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
            Assert.That(observation.OperationalUsageCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
            Assert.That(observation.RetainedDefinitionsCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.NotInspectedInSlice));
            Assert.That(observation.OperationalUsageCoverage.Reason, Is.EqualTo(LocomotiveTrainCoverageReason.NotInspectedInSlice));
        }
    }
}
