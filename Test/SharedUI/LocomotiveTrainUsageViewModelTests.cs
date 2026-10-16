// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Service.Validation;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;
using System.ComponentModel;

[TestFixture]
internal sealed class LocomotiveTrainUsageViewModelTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task RealOwnerChangedAfterRead_IsRevalidatedBeforeApplying(bool dispose)
    {
        await using var fixture = new TestProjectRuntimeHost();
        var project = CreateProject();
        await fixture.Host.LoadAsync([project]);
        var actual = (ILocomotiveTrainRuntimeUsageReader)fixture.Host;
        var reader = new Mock<ILocomotiveTrainRuntimeUsageReader>();
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (LocomotiveTrainUsageRequest request, CancellationToken token) =>
            {
                var result = await actual.ReadAsync(request, token);
                if (dispose) await fixture.Host.DisposeAsync();
                else await fixture.Host.ReplaceAsync(project, token);
                return result;
            });
        reader.Setup(value => value.IsCurrentAsync(It.IsAny<LocomotiveTrainRuntimeObservation>(), It.IsAny<CancellationToken>()))
            .Returns((LocomotiveTrainRuntimeObservation observation, CancellationToken token) => actual.IsCurrentAsync(observation, token));
        using var viewModel = CreateViewModel(CreateSession(project).Object, reader.Object);

        await viewModel.QueryAsync(project, project.Locomotives[0].Id);

        Assert.That(viewModel.RuntimeObservation!.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
        Assert.That(viewModel.RuntimeObservation.LoadedDefinitionCoverage.Reason, Is.EqualTo(dispose
            ? LocomotiveTrainCoverageReason.OwnerUnavailable : LocomotiveTrainCoverageReason.StaleObservation));
        Assert.That(viewModel.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(viewModel.Diagnostics[0].Location.Source, Is.EqualTo(LocomotiveTrainSource.EditedProjectData));
    }

    [Test]
    public async Task SnapshotBurst_CoalescesPendingChecks_UsesUiDispatch_AndDisposalUnsubscribes()
    {
        var project = CreateProject();
        var reader = CompleteReader(project);
        var pending = new TaskCompletionSource<LocomotiveTrainObservationValidity>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checks = 0;
        reader.Setup(value => value.IsCurrentAsync(It.IsAny<LocomotiveTrainRuntimeObservation>(), It.IsAny<CancellationToken>()))
            .Returns(() => Interlocked.Increment(ref checks) == 1 ? Task.FromResult(LocomotiveTrainObservationValidity.Current) : pending.Task);
        var snapshots = new Mock<IProjectRuntimeSnapshots>();
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        dispatcher.Setup(value => value.InvokeOnUiAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(action => action());
        using var viewModel = CreateViewModel(CreateSession(project).Object, reader.Object, snapshots.Object, dispatcher.Object);
        await viewModel.QueryAsync(project, project.Locomotives[0].Id);
        var invalidated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.RuntimeObservation)
                && viewModel.RuntimeObservation?.LoadedDefinitionCoverage.State == LocomotiveTrainCoverageState.Unknown)
                invalidated.TrySetResult();
        };

        await Task.Run(() =>
        {
            for (var index = 0; index < 20; index++)
                snapshots.Raise(value => value.RuntimeSnapshotChanged += null,
                    new ProjectRuntimeSnapshotEventArgs(new MobaRuntimeSnapshot { ProjectId = project.Id }));
        });
        Assert.That(checks, Is.EqualTo(2), "A burst must not start concurrent checks while one is pending.");
        pending.SetResult(LocomotiveTrainObservationValidity.Stale);
        await invalidated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        dispatcher.Verify(value => value.InvokeOnUiAsync(It.IsAny<Func<Task>>()), Times.AtLeast(2));
        reader.Verify(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        viewModel.Dispose();
        snapshots.VerifyRemove(value => value.RuntimeSnapshotChanged -= It.IsAny<EventHandler<ProjectRuntimeSnapshotEventArgs>>(), Times.Once);
        snapshots.Raise(value => value.RuntimeSnapshotChanged += null,
            new ProjectRuntimeSnapshotEventArgs(new MobaRuntimeSnapshot { ProjectId = project.Id }));
        Assert.That(checks, Is.EqualTo(2));
    }

    [Test]
    public async Task Query_SeparatesEditorAndRuntimeOccurrences_AndKeepsUnknownBC()
    {
        var project = CreateProject();
        var clone = CreateProject();
        clone.Id = project.Id;
        clone.Locomotives[0].Id = project.Locomotives[0].Id;
        clone.Trains[0].Vehicles[0].VehicleId = project.Locomotives[0].Id;
        var session = CreateSession(project);
        var reader = CompleteReader(clone);
        using var viewModel = CreateViewModel(session.Object, reader.Object);

        await viewModel.QueryAsync(project, project.Locomotives[0].Id);

        Assert.That(viewModel.Diagnostics, Has.Length.EqualTo(2));
        Assert.That(viewModel.Diagnostics.Select(finding => finding.Location.Source).Distinct().Count(), Is.EqualTo(2));
        Assert.That(viewModel.RuntimeObservation!.RuntimeCoverage, Is.EqualTo(LocomotiveTrainRuntimeCoverage.Partial));
        Assert.That(viewModel.CoverageSummary, Does.Contain("Retained definitions (B): Unknown"));
        Assert.That(viewModel.CoverageSummary, Does.Contain("Operational use (C): Unknown"));
    }

    [TestCase(LocomotiveTrainCoverageReason.UnsupportedRemote)]
    [TestCase(LocomotiveTrainCoverageReason.QueryFailed)]
    [TestCase(LocomotiveTrainCoverageReason.OwnerUnavailable)]
    public async Task FailedRefresh_ReplacesOldComplete_WithExplicitUnknown(LocomotiveTrainCoverageReason reason)
    {
        var project = CreateProject();
        var session = CreateSession(project);
        var reader = CompleteReader(project);
        using var viewModel = CreateViewModel(session.Object, reader.Object);
        await viewModel.QueryAsync(project, project.Locomotives[0].Id);
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocomotiveTrainUsageRequest request, CancellationToken _) => LocomotiveTrainRuntimeObservation.Unknown(request, reason));

        await viewModel.QueryAsync(project, project.Locomotives[0].Id);

        Assert.That(viewModel.RuntimeObservation!.LoadedDefinitionCoverage.Reason, Is.EqualTo(reason));
        Assert.That(viewModel.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(viewModel.Diagnostics.Single().Location.Source, Is.EqualTo(LocomotiveTrainSource.EditedProjectData));
        Assert.That(viewModel.CoverageSummary, Does.Contain("Unknown"));
    }

    [TestCase(LocomotiveTrainObservationValidity.Current)]
    [TestCase(LocomotiveTrainObservationValidity.Stale)]
    public async Task LateReceiptAnswer_CannotReplaceANewerTargetQuery(LocomotiveTrainObservationValidity oldValidity)
    {
        var project = CreateProject();
        var second = new Locomotive { Name = "Second target" };
        project.Locomotives.Add(second);
        var reader = CompleteReader(project);
        var pending = new TaskCompletionSource<LocomotiveTrainObservationValidity>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checks = 0;
        reader.Setup(value => value.IsCurrentAsync(It.IsAny<LocomotiveTrainRuntimeObservation>(), It.IsAny<CancellationToken>()))
            .Returns(() => ++checks == 1 ? pending.Task : Task.FromResult(LocomotiveTrainObservationValidity.Current));
        using var viewModel = CreateViewModel(CreateSession(project).Object, reader.Object);
        var oldQuery = viewModel.QueryAsync(project, project.Locomotives[0].Id);
        viewModel.SetTarget(project, second.Id);
        await viewModel.QueryAsync(project, second.Id);
        var current = viewModel.RuntimeObservation;

        pending.SetResult(oldValidity);
        await oldQuery;

        Assert.That(viewModel.RuntimeObservation, Is.SameAs(current));
        Assert.That(viewModel.Diagnostics, Is.Empty);
    }

    [Test]
    public async Task SnapshotHint_RevalidatesExistingReceipt_WithoutAnotherRead_AndClearsStaleComplete()
    {
        var project = CreateProject();
        var reader = CompleteReader(project);
        reader.SetupSequence(value => value.IsCurrentAsync(It.IsAny<LocomotiveTrainRuntimeObservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LocomotiveTrainObservationValidity.Current)
            .ReturnsAsync(LocomotiveTrainObservationValidity.Stale);
        var snapshots = new Mock<IProjectRuntimeSnapshots>();
        using var viewModel = CreateViewModel(CreateSession(project).Object, reader.Object, snapshots.Object);
        await viewModel.QueryAsync(project, project.Locomotives[0].Id);
        var invalidated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.RuntimeObservation)
                && viewModel.RuntimeObservation?.LoadedDefinitionCoverage.Reason == LocomotiveTrainCoverageReason.StaleObservation)
                invalidated.TrySetResult();
        };

        await Task.Run(() => snapshots.Raise(value => value.RuntimeSnapshotChanged += null,
            new ProjectRuntimeSnapshotEventArgs(new MobaRuntimeSnapshot { ProjectId = project.Id })));
        await invalidated.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(viewModel.EditedResult, Is.Not.Null);
        Assert.That(viewModel.Diagnostics, Has.Length.EqualTo(1));
        reader.Verify(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Query_ReadsUnsavedEdits_WithoutSavingOrChangingTheModel()
    {
        var project = CreateProject();
        var session = CreateSession(project);
        var reader = UnknownReader();
        using var viewModel = CreateViewModel(session.Object, reader.Object);
        var train = project.Trains.Single();
        train.Vehicles.Add(new Vehicle { VehicleId = project.Locomotives.Single().Id });

        await viewModel.QueryAsync(project, project.Locomotives.Single().Id);

        Assert.That(viewModel.EditedResult!.Findings, Has.Length.EqualTo(2));
        Assert.That(train.Vehicles, Has.Count.EqualTo(2));
        Assert.That(viewModel.RuntimeObservation!.LoadedDefinitionCoverage.State, Is.EqualTo(LocomotiveTrainCoverageState.Unknown));
        session.Verify(value => value.SaveAsync(It.IsAny<bool>()), Times.Never);
        session.Verify(value => value.SaveSolutionInternalAsync(), Times.Never);
        session.Verify(value => value.StartRuntimesAsync(), Times.Never);
        session.Verify(value => value.RemoveProject(It.IsAny<ProjectViewModel>()), Times.Never);
        reader.Verify(value => value.ReadAsync(It.Is<LocomotiveTrainUsageRequest>(request =>
            ReferenceEquals(request.ExpectedProject, project) && request.ProjectId == project.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InPlaceSolutionReload_DiscardsTheOldAnswerEvenWithSameObjectsAndIds()
    {
        var project = CreateProject();
        var session = CreateSession(project);
        var pending = new TaskCompletionSource<LocomotiveTrainRuntimeObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        LocomotiveTrainUsageRequest? captured = null;
        var reader = UnknownReader();
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<LocomotiveTrainUsageRequest, CancellationToken>((request, _) => captured = request).Returns(pending.Task);
        using var viewModel = CreateViewModel(session.Object, reader.Object);

        var query = viewModel.QueryAsync(project, project.Locomotives.Single().Id);
        session.Raise(value => value.SolutionLoaded += null, EventArgs.Empty);
        pending.SetResult(LocomotiveTrainRuntimeObservation.Unknown(captured!, LocomotiveTrainCoverageReason.OwnerUnavailable));
        await query;

        Assert.That(viewModel.EditedResult, Is.Null);
        Assert.That(viewModel.RuntimeObservation, Is.Null);
        Assert.That(viewModel.Diagnostics, Is.Empty);
    }

    [Test]
    public async Task ModelChange_ClearsObservedResults_WithoutStartingAnotherReadOrSaving()
    {
        var project = CreateProject();
        var session = CreateSession(project);
        var reader = UnknownReader();
        using var viewModel = CreateViewModel(session.Object, reader.Object);
        await viewModel.QueryAsync(project, project.Locomotives.Single().Id);

        session.Raise(value => value.ModelChanged += null, project, new PropertyChangedEventArgs("Vehicles"));

        Assert.That(viewModel.EditedResult, Is.Null);
        Assert.That(viewModel.RuntimeObservation, Is.Null);
        reader.Verify(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(value => value.SaveSolutionInternalAsync(), Times.Never);
    }

    [Test]
    public async Task Query_ForeignProjectWithSameGuid_IsRejected()
    {
        var project = CreateProject();
        var session = CreateSession(project);
        var reader = UnknownReader();
        using var viewModel = CreateViewModel(session.Object, reader.Object);
        var foreign = new Project { Id = project.Id };

        await viewModel.QueryAsync(foreign, project.Locomotives.Single().Id);

        Assert.That(viewModel.EditedResult, Is.Null);
        reader.Verify(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Cancellation_DiscardsPartialResults()
    {
        var project = CreateProject();
        var session = CreateSession(project);
        var reader = UnknownReader();
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .Returns<LocomotiveTrainUsageRequest, CancellationToken>((_, token) =>
                Task.FromCanceled<LocomotiveTrainRuntimeObservation>(token));
        using var viewModel = CreateViewModel(session.Object, reader.Object);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await viewModel.QueryAsync(project, project.Locomotives.Single().Id, cancelled.Token);

        Assert.That(viewModel.EditedResult, Is.Null);
        Assert.That(viewModel.Diagnostics, Is.Empty);
    }

    internal static Project CreateProject()
    {
        var locomotive = new Locomotive { Name = "Harbor pilot" };
        return new Project
        {
            Name = "Yard", Locomotives = [locomotive],
            Trains = [new Train { Name = "Morning service", Vehicles = [new Vehicle { VehicleKind = TrainVehicleKind.Locomotive, VehicleId = locomotive.Id }] }]
        };
    }

    internal static Mock<ISolutionSession> CreateSession(Project project)
    {
        var session = new Mock<ISolutionSession>();
        session.SetupGet(value => value.Solution).Returns(new Solution { Projects = [project] });
        session.SetupGet(value => value.SelectedProject).Returns(new ProjectViewModel(project));
        return session;
    }

    internal static Mock<ILocomotiveTrainRuntimeUsageReader> UnknownReader()
    {
        var reader = new Mock<ILocomotiveTrainRuntimeUsageReader>();
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocomotiveTrainUsageRequest request, CancellationToken _) =>
                LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.OwnerUnavailable));
        return reader;
    }

    internal static Mock<ILocomotiveTrainRuntimeUsageReader> CompleteReader(Project definition)
    {
        var reader = new Mock<ILocomotiveTrainRuntimeUsageReader>();
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocomotiveTrainUsageRequest request, CancellationToken _) =>
                new LocomotiveTrainRuntimeObservation(request.RequestId, request.ProjectId, DateTimeOffset.UtcNow,
                    new LocomotiveTrainReferenceAnalyzer().Analyze(definition, LocomotiveTrainSource.LoadedRuntimeDefinition, request.LocomotiveId)));
        reader.Setup(value => value.IsCurrentAsync(It.IsAny<LocomotiveTrainRuntimeObservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LocomotiveTrainObservationValidity.Current);
        return reader;
    }

    internal static LocomotiveTrainUsageViewModel CreateViewModel(
        ISolutionSession session, ILocomotiveTrainRuntimeUsageReader reader, IProjectRuntimeSnapshots? snapshots = null,
        IUiDispatcher? dispatcherOverride = null)
    {
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        dispatcher.Setup(value => value.InvokeOnUiAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(action => action());
        return new(session, new LocomotiveTrainReferenceAnalyzer(), reader,
            snapshots ?? new Mock<IProjectRuntimeSnapshots>().Object, dispatcherOverride ?? dispatcher.Object,
            NullLogger<LocomotiveTrainUsageViewModel>.Instance);
    }
}
