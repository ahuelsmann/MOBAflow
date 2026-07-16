// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Backend.Service.Validation;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Multiplex;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;

internal sealed class MainWindowDiagnosticsTests
{
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
            SignalBoxPlan = new SignalBoxPlan
            {
                Elements = [new SbDetector { Name = "Missing", FeedbackAddress = 0 }]
            }
        };
        var diagnostics = new ProjectDiagnosticsService(
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()));
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
            new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()));
        var viewModel = CreateViewModel(new Solution { Projects = [project] }, diagnostics);

        project.SignalBoxPlan = new SignalBoxPlan
        {
            Elements = [new SbDetector { Name = "Missing", FeedbackAddress = 0 }]
        };

        Assert.That(viewModel.ProjectDiagnostics, Is.Empty);

        viewModel.IsDiagnosticsExpanded = true;

        Assert.That(viewModel.ProjectDiagnostics.Single().Severity, Is.EqualTo(ProjectDiagnosticSeverity.Error));
    }

    private static MainWindowViewModel CreateViewModel(
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

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtime.Object,
            new Mock<IEventBus>().Object,
            dispatcher.Object,
            new AppSettings(),
            solution,
            new ActionExecutionContext { Z21 = new Mock<IZ21>().Object },
            new Mock<ILogger<MainWindowViewModel>>().Object,
            projectDiagnosticsService: diagnostics);
    }
}
