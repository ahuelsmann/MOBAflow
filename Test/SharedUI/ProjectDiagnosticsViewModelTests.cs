// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Backend.Service.Validation;
using Moba.Common.Multiplex;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;
using System.ComponentModel;

/// <summary>
/// Messages pane state: project analysis, Z21 assignment and locomotive usage findings, the severity filters,
/// the refresh triggers (pane opened, project change, persisted model change, usage result) and detaching on dispose.
/// </summary>
[TestFixture]
internal sealed partial class ProjectDiagnosticsViewModelTests : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    [TearDown]
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        _disposables.Clear();
        GC.SuppressFinalize(this);
    }

    [Test]
    public void ConstructorRejectsMissingDependencies()
    {
        var session = CreateSession(new Solution { Projects = [new Project()] });
        var usage = CreateUsage(session);
        var service = CreateService();
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<ArgumentNullException>(() => new ProjectDiagnosticsViewModel(null!, service, usage));
            Assert.Throws<ArgumentNullException>(() => new ProjectDiagnosticsViewModel(session, null!, usage));
            Assert.Throws<ArgumentNullException>(() => new ProjectDiagnosticsViewModel(session, service, null!));
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void InvalidUsageScopeRemainsVisibleWithoutAnExplicitQueryAndRepairRestoresDiagnostics(int invalidIdentity)
    {
        var missingId = Guid.NewGuid();
        var train = new Train { Vehicles = [new Vehicle { VehicleId = missingId }] };
        var project = new Project { Trains = [train] };
        if (invalidIdentity == 0) train.Id = Guid.Empty;
        if (invalidIdentity == 1) project.Trains.Add(new Train { Id = train.Id });
        if (invalidIdentity == 2)
        {
            var duplicate = Guid.NewGuid();
            project.Locomotives.Add(new Locomotive { Id = duplicate, DigitalAddress = 4 });
            project.Locomotives.Add(new Locomotive { Id = duplicate, DigitalAddress = 5 });
        }
        var session = CreateSession(new Solution { Projects = [project] });
        var usage = CreateUsage(session);
        var viewModel = CreateViewModel(session, usage);
        viewModel.ShowDiagnosticErrors = false;
        viewModel.ShowDiagnosticWarnings = false;
        viewModel.ShowDiagnosticInformation = false;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(usage.EditedResult, Is.Null, "This is ordinary project diagnostics, not Show usage.");
            Assert.That(viewModel.DiagnosticErrorCount, Is.Zero, "Ambiguous scope cannot fabricate precise missing-reference errors.");
            Assert.That(viewModel.DiagnosticInformationCount, Is.EqualTo(1));
            Assert.That(viewModel.VisibleProjectDiagnostics, Is.EqualTo(viewModel.ProjectDiagnostics));
        }

        foreach (var item in project.Trains) item.Id = Guid.NewGuid();
        foreach (var locomotive in project.Locomotives) locomotive.Id = Guid.NewGuid();
        viewModel.RefreshProjectDiagnosticsCommand.Execute(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.DiagnosticInformationCount, Is.Zero);
            Assert.That(viewModel.DiagnosticErrorCount, Is.EqualTo(1));
            Assert.That(viewModel.VisibleProjectDiagnostics, Is.Empty, "An actual missing-reference error still follows the Error filter.");
        }
        viewModel.ShowDiagnosticErrors = true;
        Assert.That(viewModel.VisibleProjectDiagnostics.Single().RuleCode, Is.EqualTo("LTU002"));
        project.Locomotives.Add(new Locomotive { Id = missingId, DigitalAddress = 9 });
        viewModel.RefreshProjectDiagnosticsCommand.Execute(null);
        Assert.That(viewModel.VisibleProjectDiagnostics, Is.Empty);
    }

    [Test]
    public async Task UsageMessagesKeepSourcesSeparateRepairWithoutDuplicatesAndShowCoverageDespiteFiltering()
    {
        var project = TestLocomotiveTrainUsage.CreateProject();
        var runtimeDefinition = TestLocomotiveTrainUsage.CreateProject();
        runtimeDefinition.Id = project.Id;
        runtimeDefinition.Trains[0].Vehicles[0].VehicleId = project.Locomotives[0].Id;
        runtimeDefinition.Locomotives.Clear();
        var reader = TestLocomotiveTrainUsage.CompleteReader(runtimeDefinition);
        var session = CreateSession(new Solution { Projects = [project] });
        var usage = CreateUsage(session, reader.Object);
        var viewModel = CreateViewModel(session, usage);

        await usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);
        await usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(2));
            Assert.That(viewModel.DiagnosticErrorCount, Is.EqualTo(1));
            Assert.That(viewModel.DiagnosticInformationCount, Is.EqualTo(1));
            Assert.That(viewModel.IsDiagnosticsExpanded, Is.True, "Show usage opens the pane.");
        }
        Assert.That(viewModel.ProjectDiagnostics.Select(item => item.Location!.Source).Distinct().Count(), Is.EqualTo(2));
        viewModel.ShowDiagnosticInformation = false;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.VisibleProjectDiagnostics, Has.Count.EqualTo(1));
            Assert.That(viewModel.LocomotiveUsageCoverageSummary, Does.Contain("Retained definitions (B): Unknown"));
        }
        Assert.That(viewModel.LocomotiveUsageCoverageSummary, Does.Contain("Operational use (C): Unknown"));

        runtimeDefinition.Locomotives.Add(new Locomotive { Id = project.Locomotives[0].Id });
        await usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.DiagnosticErrorCount, Is.Zero);
            Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(2));
            Assert.That(viewModel.VisibleProjectDiagnostics, Is.Empty);
        }
    }

    [Test]
    public async Task UsageMessagesFailedRuntimeRefreshRemovesOldCompleteRows()
    {
        var project = TestLocomotiveTrainUsage.CreateProject();
        var reader = TestLocomotiveTrainUsage.CompleteReader(project);
        var session = CreateSession(new Solution { Projects = [project] });
        var usage = CreateUsage(session, reader.Object);
        var viewModel = CreateViewModel(session, usage);
        await usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);
        Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(2));
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocomotiveTrainUsageRequest request, CancellationToken _) =>
                LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.QueryFailed));

        await usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);

        Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ProjectDiagnostics[0].Location!.Source, Is.EqualTo(LocomotiveTrainSource.EditedProjectData));
            Assert.That(usage.CoverageSummary, Does.Contain("Query failed"));
        }
    }

    [Test]
    public void RepairReplacesMissingReferenceWithoutAccumulation()
    {
        var missingId = Guid.NewGuid();
        var project = new Project { Trains = [new Train { Vehicles = [new Vehicle { VehicleId = missingId }] }] };
        var session = CreateSession(new Solution { Projects = [project] });
        var viewModel = CreateViewModel(session, CreateUsage(session));
        viewModel.RefreshProjectDiagnosticsCommand.Execute(null);
        viewModel.RefreshProjectDiagnosticsCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.DiagnosticErrorCount, Is.EqualTo(1));
            Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(1));
        }
        project.Locomotives.Add(new Locomotive { Id = missingId });
        viewModel.RefreshProjectDiagnosticsCommand.Execute(null);
        Assert.That(viewModel.ProjectDiagnostics, Is.Empty);
    }

    [Test]
    public void DiagnosticsCanBeFilteredBySeverity()
    {
        var project = new Project
        {
            Name = "Test",
            Locomotives =
            [
                new Locomotive { Name = "One", DigitalAddress = 9 },
                new Locomotive { Name = "Two", DigitalAddress = 9 }
            ],
            Interlocking = new InterlockingDefinition
            {
                Blocks = [CreateBlockWithMissingClearObservation()]
            }
        };
        var session = CreateSession(new Solution { Projects = [project] });
        var viewModel = CreateViewModel(session, CreateUsage(session));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.DiagnosticErrorCount, Is.EqualTo(1));
            Assert.That(viewModel.DiagnosticWarningCount, Is.EqualTo(1));
            Assert.That(viewModel.DiagnosticInformationCount, Is.Zero);
            Assert.That(viewModel.DiagnosticsSummary, Is.EqualTo("1 errors, 1 warnings, 0 information"));
            Assert.That(viewModel.VisibleProjectDiagnostics, Has.Count.EqualTo(2));
        }

        viewModel.ShowDiagnosticWarnings = false;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.VisibleProjectDiagnostics, Has.Count.EqualTo(1));
            Assert.That(viewModel.VisibleProjectDiagnostics.Single().Severity, Is.EqualTo(ProjectDiagnosticSeverity.Error));
        }
    }

    [Test]
    public void DiagnosticsRefreshWhenThePaneIsOpened()
    {
        var project = new Project { Name = "Test" };
        var session = CreateSession(new Solution { Projects = [project] });
        var viewModel = CreateViewModel(session, CreateUsage(session));

        project.Interlocking.Blocks.Add(CreateBlockWithMissingClearObservation());

        Assert.That(viewModel.ProjectDiagnostics, Is.Empty);

        viewModel.IsDiagnosticsExpanded = true;

        Assert.That(viewModel.ProjectDiagnostics.Single().Severity, Is.EqualTo(ProjectDiagnosticSeverity.Error));
    }

    [Test]
    public void ProjectChangeRefreshesTheDiagnosticsForTheSelectedProject()
    {
        var conflicting = new Project
        {
            Name = "Conflicting",
            Locomotives = [new Locomotive { Name = "One", DigitalAddress = 9 }, new Locomotive { Name = "Two", DigitalAddress = 9 }]
        };
        var clean = new Project { Name = "Clean" };
        var session = CreateSession(new Solution { Projects = [conflicting, clean] });
        var viewModel = CreateViewModel(session, CreateUsage(session));
        Assert.That(viewModel.DiagnosticWarningCount, Is.EqualTo(1), "The duplicate address of the selected project is reported.");

        session.SelectedProject = session.SolutionViewModel!.Projects[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.DiagnosticWarningCount, Is.Zero);
            Assert.That(viewModel.ProjectDiagnostics, Is.Empty);
        }
    }

    [Test]
    public async Task ModelChangesAndUsageResultsRefreshUntilDisposed()
    {
        var project = TestLocomotiveTrainUsage.CreateProject();
        var session = new Mock<ISolutionSession>();
        session.SetupGet(value => value.Solution).Returns(new Solution { Projects = [project] });
        session.SetupGet(value => value.SelectedProject).Returns((ProjectViewModel?)null);
        var service = new Mock<IProjectDiagnosticsService>();
        service.Setup(value => value.Analyze(It.IsAny<Project?>())).Returns([]);
        var usage = CreateUsage(session.Object, TestLocomotiveTrainUsage.CompleteReader(project).Object);
        var viewModel = CreateViewModel(session.Object, usage, service.Object);
        service.Verify(value => value.Analyze(null), Times.Once, "The constructor analyzes once.");

        // The usage query invalidates on the same session events, so each trigger may refresh more than once.
        var analyses = service.Invocations.Count;
        session.Raise(value => value.ModelChanged += null, new PropertyChangedEventArgs("Name"));
        Assert.That(service.Invocations, Has.Count.GreaterThan(analyses), "A persisted model change refreshes.");
        analyses = service.Invocations.Count;
        session.Raise(value => value.PropertyChanged += null, new PropertyChangedEventArgs(nameof(ISolutionSession.SelectedProject)));
        Assert.That(service.Invocations, Has.Count.GreaterThan(analyses), "A project change refreshes.");
        analyses = service.Invocations.Count;
        session.Raise(value => value.PropertyChanged += null, new PropertyChangedEventArgs(nameof(ISolutionSession.SelectedJourney)));
        Assert.That(service.Invocations, Has.Count.EqualTo(analyses), "A journey change does not refresh.");

        await usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);
        var analysesBeforeDispose = service.Invocations.Count;
        Assert.That(viewModel.ProjectDiagnostics, Is.Not.Empty, "The usage result joins the pane.");

        viewModel.Dispose();
        viewModel.Dispose();
        session.Raise(value => value.ModelChanged += null, new PropertyChangedEventArgs("Name"));
        session.Raise(value => value.PropertyChanged += null, new PropertyChangedEventArgs(nameof(ISolutionSession.SelectedProject)));
        project.Locomotives.Clear();
        await usage.QueryAsync(project, project.Trains[0].Vehicles[0].VehicleId).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.Invocations, Has.Count.EqualTo(analysesBeforeDispose), "Disposal detaches the session and usage subscriptions.");
            Assert.That(viewModel.ProjectDiagnostics, Is.Not.Empty, "The pane keeps its last findings.");
        }
    }

    private static BlockDefinition CreateBlockWithMissingClearObservation() =>
        new()
        {
            Name = "Missing clear",
            FeedbackInputs =
            [
                new BlockFeedbackInput { InPort = 1, Role = BlockFeedbackRole.Occupied }
            ]
        };

    private static ProjectDiagnosticsService CreateService() =>
        new(new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()),
            new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());

    private static SolutionSession CreateSession(Solution solution)
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(candidate => candidate.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(candidate => candidate.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        return TestSolutionSessions.Create(solution, dispatcher.Object, runtime.Object);
    }

    private LocomotiveTrainUsageViewModel CreateUsage(ISolutionSession session, ILocomotiveTrainRuntimeUsageReader? usageReader = null)
    {
        var usage = TestRollingStockPages.CreateUsage(session, usageReader);
        _disposables.Add(usage);
        return usage;
    }

    private ProjectDiagnosticsViewModel CreateViewModel(
        ISolutionSession session, LocomotiveTrainUsageViewModel usage, IProjectDiagnosticsService? service = null)
    {
        var viewModel = new ProjectDiagnosticsViewModel(session, service ?? CreateService(), usage);
        _disposables.Add(viewModel);
        return viewModel;
    }
}
