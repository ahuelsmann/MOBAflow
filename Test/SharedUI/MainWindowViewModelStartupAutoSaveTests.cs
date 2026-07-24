// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Regression tests for startup, autosave, and shutdown persistence behavior.
/// </summary>
internal partial class MainWindowViewModelShutdownTests
{
    [Test]
    public void Constructor_DoesNotSaveUnnamedSolution_WhenInitialRuntimeActivationCommitsUsageCheckpoint()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        var ioService = new Mock<IIoService>();

        runtime
            .Setup(candidate => candidate.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((project, _) =>
                eventBus.Publish(new VehicleUsageCheckpointCommittedEvent(
                    project.Id,
                    DateTimeOffset.UtcNow,
                    new Dictionary<Guid, VehicleUsageRuntimeSnapshot>())))
            .Returns(Task.CompletedTask);

        _ = CreateViewModel(runtime.Object, eventBus, ioService.Object);

        ioService.Verify(
            candidate => candidate.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()),
            Times.Never);
        ioService.Verify(
            candidate => candidate.SaveAsAsync(It.IsAny<Solution>()),
            Times.Never);
    }

    [Test]
    public async Task SaveSolutionInternalAsync_MarksSolutionDirtyWithoutSelectingPath()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        var ioService = new Mock<IIoService>();
        var viewModel = CreateViewModel(runtime.Object, eventBus, ioService.Object);

        await viewModel.SaveSolutionInternalAsync().ConfigureAwait(false);

        Assert.That(viewModel.HasUnsavedChanges, Is.True);
        ioService.Verify(
            candidate => candidate.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()),
            Times.Never);
        ioService.Verify(
            candidate => candidate.SaveAsAsync(It.IsAny<Solution>()),
            Times.Never);
    }

    [Test]
    public async Task SaveSolutionCommand_SelectsPathForUnnamedSolution()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        var ioService = new Mock<IIoService>();
        ioService
            .Setup(candidate => candidate.SaveAsAsync(It.IsAny<Solution>()))
            .ReturnsAsync((true, "selected.json", null));
        var viewModel = CreateViewModel(runtime.Object, eventBus, ioService.Object);
        await viewModel.SaveSolutionInternalAsync().ConfigureAwait(false);

        await viewModel.SaveSolutionCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.CurrentSolutionPath, Is.EqualTo("selected.json"));
            Assert.That(viewModel.HasUnsavedChanges, Is.False);
        }
        ioService.Verify(
            candidate => candidate.SaveAsAsync(It.IsAny<Solution>()),
            Times.Once);
    }

    [Test]
    public void UsageCheckpointWithChangedCounters_AutoSavesToKnownPath()
    {
        var locomotive = new Locomotive { Usage = new VehicleUsageData() };
        var project = new Project { Locomotives = [locomotive] };
        var solution = new Solution { Projects = [project] };
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        var ioService = new Mock<IIoService>();
        ioService
            .Setup(candidate => candidate.SaveAsync(solution, "existing.json"))
            .ReturnsAsync((true, "existing.json", null));
        var viewModel = CreateViewModel(runtime.Object, eventBus, ioService.Object, solution);
        viewModel.CurrentSolutionPath = "existing.json";

        eventBus.Publish(new VehicleUsageCheckpointCommittedEvent(
            project.Id,
            DateTimeOffset.UtcNow,
            new Dictionary<Guid, VehicleUsageRuntimeSnapshot>
            {
                [locomotive.Id] = new()
                {
                    VehicleId = locomotive.Id,
                    VehicleKind = TrainVehicleKind.Locomotive,
                    TrackedOperatingSeconds = 42,
                    TrackedCompletedTrips = 3
                }
            }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(locomotive.Usage!.TrackedOperatingSeconds, Is.EqualTo(42));
            Assert.That(locomotive.Usage.TrackedCompletedTrips, Is.EqualTo(3));
            Assert.That(viewModel.HasUnsavedChanges, Is.False);
        }
        ioService.Verify(
            candidate => candidate.SaveAsync(solution, "existing.json"),
            Times.Once);
        ioService.Verify(
            candidate => candidate.SaveAsAsync(It.IsAny<Solution>()),
            Times.Never);
    }

    [Test]
    public async Task PrepareForShutdownAsync_StopsShutdown_WhenUnnamedSolutionSaveIsCancelled()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        var ioService = new Mock<IIoService>();
        ioService
            .Setup(candidate => candidate.SaveAsAsync(It.IsAny<Solution>()))
            .ReturnsAsync((false, null, null));
        var viewModel = CreateViewModel(runtime.Object, eventBus, ioService.Object);
        await viewModel.SaveSolutionInternalAsync().ConfigureAwait(false);

        var result = await viewModel.PrepareForShutdownAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        runtime.Verify(
            candidate => candidate.DisconnectAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task PrepareForShutdownAsync_SavesChangedUsageExactlyOnce_ToKnownPath()
    {
        var locomotive = new Locomotive { Usage = new VehicleUsageData() };
        var project = new Project { Locomotives = [locomotive] };
        var solution = new Solution { Projects = [project] };
        var usage = new Dictionary<Guid, VehicleUsageRuntimeSnapshot>
        {
            [locomotive.Id] = new()
            {
                VehicleId = locomotive.Id,
                VehicleKind = TrainVehicleKind.Locomotive,
                TrackedOperatingSeconds = 42
            }
        };
        var runtimeSnapshot = new MobaRuntimeSnapshot { VehicleUsage = usage };
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        runtime.SetupGet(candidate => candidate.Current).Returns(runtimeSnapshot);
        runtime
            .Setup(candidate => candidate.CheckpointUsageAsync(It.IsAny<CancellationToken>()))
            .Callback(() => eventBus.Publish(new VehicleUsageCheckpointCommittedEvent(
                project.Id,
                DateTimeOffset.UtcNow,
                usage)))
            .Returns(Task.CompletedTask);
        var ioService = new Mock<IIoService>();
        ioService
            .Setup(candidate => candidate.SaveAsync(solution, "existing.json"))
            .ReturnsAsync((true, "existing.json", null));
        var viewModel = CreateViewModel(runtime.Object, eventBus, ioService.Object, solution);
        viewModel.CurrentSolutionPath = "existing.json";

        var result = await viewModel.PrepareForShutdownAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        ioService.Verify(
            candidate => candidate.SaveAsync(solution, "existing.json"),
            Times.Once);
    }

    private static MainWindowViewModel CreateViewModel(
        IMobaRuntime runtime,
        IEventBus eventBus,
        IIoService ioService,
        Solution? solution = null)
    {
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher
            .Setup(candidate => candidate.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtime,
            eventBus,
            dispatcher.Object,
            new AppSettings(),
            solution ?? new Solution(),
            new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            },
            new Mock<ILogger<MainWindowViewModel>>().Object,
            ioService);
    }

    private static Mock<IMobaRuntime> CreateRuntimeMock()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(candidate => candidate.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(candidate => candidate.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        runtime
            .Setup(candidate => candidate.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        runtime
            .Setup(candidate => candidate.CheckpointUsageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        runtime
            .Setup(candidate => candidate.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        runtime
            .Setup(candidate => candidate.SetActiveTrainAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return runtime;
    }
}
