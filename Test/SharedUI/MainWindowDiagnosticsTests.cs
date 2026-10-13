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
            new InterlockingDefinitionValidator());
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
            new InterlockingDefinitionValidator());
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
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()), new InterlockingDefinitionValidator());
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
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()), new InterlockingDefinitionValidator());
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
        IProjectDiagnosticsService diagnostics)
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
        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtime.Object,
            runtime.Object,
            runtime.Object,
            new LocalRuntimeCommandGateway(runtime.Object),
            new Mock<IEventBus>().Object,
            dispatcher.Object,
            TestSettingsPages.Create(new AppSettings(), dispatcher.Object),
            _rollingStockPages.GetLocomotivesPage(locomotiveSession1, null),
            _rollingStockPages.GetWagonsPage(locomotiveSession1, null),
            locomotiveSession1,
            new ActionExecutionContext { Z21 = new Mock<IZ21>().Object },
            new Mock<ILogger<MainWindowViewModel>>().Object,
            projectDiagnosticsService: diagnostics);
    }
}
