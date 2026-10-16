// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Moba.Test.Helpers;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.Validation;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Multiplex;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moq;

internal sealed partial class MainWindowDiagnosticsTests : IDisposable
{
    private readonly TestRollingStockPages _rollingStockPages = new();

    [TearDown]
    public void Dispose()
    {
        _rollingStockPages.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void InvalidUsageScope_RemainsVisibleWithoutAnExplicitQuery_AndRepairRestoresDiagnostics(int invalidIdentity)
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
        var service = new ProjectDiagnosticsService(new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()),
            new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());
        var viewModel = CreateViewModel(new Solution { Projects = [project] }, service);
        viewModel.ShowDiagnosticErrors = false;
        viewModel.ShowDiagnosticWarnings = false;
        viewModel.ShowDiagnosticInformation = false;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.LocomotivesPage.Usage.EditedResult, Is.Null, "This is ordinary project diagnostics, not Show usage.");
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
    public async Task UsageMessages_KeepSourcesSeparate_RepairWithoutDuplicates_AndShowCoverageDespiteFiltering()
    {
        var project = TestLocomotiveTrainUsage.CreateProject();
        var runtimeDefinition = TestLocomotiveTrainUsage.CreateProject();
        runtimeDefinition.Id = project.Id;
        runtimeDefinition.Trains[0].Vehicles[0].VehicleId = project.Locomotives[0].Id;
        runtimeDefinition.Locomotives.Clear();
        var reader = TestLocomotiveTrainUsage.CompleteReader(runtimeDefinition);
        var service = new ProjectDiagnosticsService(new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()),
            new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());
        var viewModel = CreateViewModel(new Solution { Projects = [project] }, service, reader.Object);

        await viewModel.LocomotivesPage.Usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);
        await viewModel.LocomotivesPage.Usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(2));
            Assert.That(viewModel.DiagnosticErrorCount, Is.EqualTo(1));
            Assert.That(viewModel.DiagnosticInformationCount, Is.EqualTo(1));
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
        await viewModel.LocomotivesPage.Usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.DiagnosticErrorCount, Is.Zero);
            Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(2));
            Assert.That(viewModel.VisibleProjectDiagnostics, Is.Empty);
        }
    }

    [Test]
    public async Task UsageMessages_FailedRuntimeRefresh_RemovesOldCompleteRows()
    {
        var project = TestLocomotiveTrainUsage.CreateProject();
        var reader = TestLocomotiveTrainUsage.CompleteReader(project);
        var service = new ProjectDiagnosticsService(new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()),
            new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());
        var viewModel = CreateViewModel(new Solution { Projects = [project] }, service, reader.Object);
        await viewModel.LocomotivesPage.Usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);
        Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(2));
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocomotiveTrainUsageRequest request, CancellationToken _) =>
                LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.QueryFailed));

        await viewModel.LocomotivesPage.Usage.QueryAsync(project, project.Locomotives[0].Id).ConfigureAwait(false);

        Assert.That(viewModel.ProjectDiagnostics, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.ProjectDiagnostics[0].Location!.Source, Is.EqualTo(LocomotiveTrainSource.EditedProjectData));
            Assert.That(viewModel.LocomotivesPage.Usage.CoverageSummary, Does.Contain("Query failed"));
        }
    }

    [Test]
    public void Diagnostics_RepairReplacesMissingReference_WithoutAccumulation()
    {
        var missingId = Guid.NewGuid();
        var project = new Project { Trains = [new Train { Vehicles = [new Vehicle { VehicleId = missingId }] }] };
        var diagnostics = new ProjectDiagnosticsService(
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()), new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());
        var viewModel = CreateViewModel(new Solution { Projects = [project] }, diagnostics);
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
    public void Diagnostics_CanBeFilteredBySeverity()
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
        var diagnostics = new ProjectDiagnosticsService(
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()),
            new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());
        var viewModel = CreateViewModel(new Solution { Projects = [project] }, diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.DiagnosticErrorCount, Is.EqualTo(1));
            Assert.That(viewModel.DiagnosticWarningCount, Is.EqualTo(1));
            Assert.That(viewModel.DiagnosticInformationCount, Is.Zero);
            Assert.That(viewModel.VisibleProjectDiagnostics, Has.Count.EqualTo(2));
        });

        viewModel.ShowDiagnosticWarnings = false;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.VisibleProjectDiagnostics, Has.Count.EqualTo(1));
            Assert.That(viewModel.VisibleProjectDiagnostics.Single().Severity, Is.EqualTo(ProjectDiagnosticSeverity.Error));
        });
    }

    [Test]
    public void Diagnostics_RefreshWhenPanelIsOpened()
    {
        var project = new Project { Name = "Test" };
        var diagnostics = new ProjectDiagnosticsService(
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()),
            new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());
        var viewModel = CreateViewModel(new Solution { Projects = [project] }, diagnostics);

        project.Interlocking.Blocks.Add(CreateBlockWithMissingClearObservation());

        Assert.That(viewModel.ProjectDiagnostics, Is.Empty);

        viewModel.IsDiagnosticsExpanded = true;

        Assert.That(viewModel.ProjectDiagnostics.Single().Severity, Is.EqualTo(ProjectDiagnosticSeverity.Error));
    }

    [TestCase("Ready")]
    [TestCase("Unavailable")]
    public void ProviderSwitchClearsThePreviousProvidersHealthResult(string previousStatus)
    {
        var diagnostics = new ProjectDiagnosticsService(
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()), new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());
        var viewModel = CreateViewModel(new Solution(), diagnostics);
        viewModel.UpdateHealthStatus(previousStatus);
        viewModel.SettingsPage.SelectedSpeechProviderId = "piper";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SpeechHealthStatus, Does.Contain("Initializing"));
            Assert.That(viewModel.SpeechHealthIcon, Is.EqualTo("\uE946"));
            Assert.That(viewModel.SpeechHealthColor, Is.EqualTo("SystemFillColorCautionBrush"));
        }
    }

    [Test]
    public void UnavailableSpeechIsShownAsACompletedWarningInsteadOfInitializing()
    {
        var diagnostics = new ProjectDiagnosticsService(
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()), new InterlockingDefinitionValidator(), new LocomotiveTrainReferenceAnalyzer());
        var viewModel = CreateViewModel(new Solution(), diagnostics);
        viewModel.UpdateHealthStatus("Unavailable");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SpeechHealthIcon, Is.EqualTo("\uE7BA"));
            Assert.That(viewModel.SpeechHealthStatus, Is.EqualTo("Speech: Unavailable"));
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

    private MainWindowViewModel CreateViewModel(
        Solution solution,
        IProjectDiagnosticsService diagnostics,
        ILocomotiveTrainRuntimeUsageReader? usageReader = null)
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(candidate => candidate.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(candidate => candidate.GetTrafficPackets()).Returns(Array.Empty<global::Moba.Backend.Model.Z21TrafficPacket>());
        runtime.Setup(candidate => candidate.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        var locomotiveSession1 = TestSolutionSessions.Create(solution, dispatcher.Object, runtime.Object);
        var stationEventBus1 = new Mock<IEventBus>().Object;
        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtime.Object,
            runtime.Object,
            runtime.Object,
            new LocalRuntimeCommandGateway(runtime.Object),
            stationEventBus1,
            dispatcher.Object,
            TestSettingsPages.Create(new AppSettings(), dispatcher.Object),
            _rollingStockPages.GetTrainsPage(locomotiveSession1, runtime.Object, null, usageReader),
            _rollingStockPages.GetStationsPage(locomotiveSession1, runtime.Object, new ActionExecutionContext { Z21 = new Mock<IZ21>().Object }, stationEventBus1),
            locomotiveSession1,
            new Mock<ILogger<MainWindowViewModel>>().Object,
            projectDiagnosticsService: diagnostics);
    }
}
