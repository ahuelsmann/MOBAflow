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
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Regression tests for startup, autosave, and shutdown persistence behavior.
/// </summary>
internal partial class MainWindowViewModelShutdownTests
{
    [Test]
    public void Constructor_DoesNotSaveUnnamedSolution_DuringInitialRuntimeActivation()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        var ioService = new Mock<IIoService>();

        runtime
            .Setup(candidate => candidate.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.HasUnsavedChanges, Is.True);
            Assert.That(viewModel.SolutionSaveState, Is.EqualTo(SolutionSaveState.NotSaved));
            Assert.That(viewModel.SolutionSaveStatusText, Is.EqualTo("Not saved - choose Save As"));
        }
        ioService.Verify(
            candidate => candidate.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()),
            Times.Never);
        ioService.Verify(
            candidate => candidate.SaveAsAsync(It.IsAny<Solution>()),
            Times.Never);
    }

    [Test]
    public async Task SaveSolutionInternalAsync_RapidRequests_AreSerializedAndLatestCompletionOwnsStatus()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        var ioService = new Mock<IIoService>();
        var firstSaveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondSaveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecondSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var saveCall = 0;
        var concurrentSaves = 0;
        var maximumConcurrentSaves = 0;
        ioService
            .Setup(candidate => candidate.SaveAsync(It.IsAny<Solution>(), "existing.json"))
            .Returns(async () =>
            {
                var currentConcurrent = Interlocked.Increment(ref concurrentSaves);
                maximumConcurrentSaves = Math.Max(maximumConcurrentSaves, currentConcurrent);
                try
                {
                    saveCall++;
                    if (saveCall == 1)
                    {
                        firstSaveStarted.SetResult();
                        await releaseFirstSave.Task.ConfigureAwait(false);
                    }
                    else
                    {
                        secondSaveStarted.SetResult();
                        await releaseSecondSave.Task.ConfigureAwait(false);
                    }

                    return (true, "existing.json", (string?)null);
                }
                finally
                {
                    Interlocked.Decrement(ref concurrentSaves);
                }
            });
        var viewModel = CreateViewModel(runtime.Object, eventBus, ioService.Object);
        viewModel.CurrentSolutionPath = "existing.json";

        // Act
        var firstSave = viewModel.SaveSolutionInternalAsync();
        await firstSaveStarted.Task.ConfigureAwait(false);
        var secondSave = viewModel.SaveSolutionInternalAsync();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.SolutionSaveState, Is.EqualTo(SolutionSaveState.Saving));
            Assert.That(viewModel.HasUnsavedChanges, Is.True);
        }

        releaseFirstSave.SetResult();
        await secondSaveStarted.Task.ConfigureAwait(false);
        Assert.That(viewModel.HasUnsavedChanges, Is.True);
        releaseSecondSave.SetResult();
        await Task.WhenAll(firstSave, secondSave).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(maximumConcurrentSaves, Is.EqualTo(1));
            Assert.That(viewModel.SolutionSaveState, Is.EqualTo(SolutionSaveState.Saved));
            Assert.That(viewModel.SolutionSaveStatusText, Is.EqualTo("Saved"));
            Assert.That(viewModel.HasUnsavedChanges, Is.False);
        }
    }

    [Test]
    public void SaveSolutionInternalAsync_WriteFailure_RetainsDirtyStateAndReportsNotSaved()
    {
        // Arrange
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var runtime = CreateRuntimeMock();
        var ioService = new Mock<IIoService>();
        ioService
            .Setup(candidate => candidate.SaveAsync(It.IsAny<Solution>(), "existing.json"))
            .ReturnsAsync((false, (string?)null, "disk full"));
        var viewModel = CreateViewModel(runtime.Object, eventBus, ioService.Object);
        viewModel.CurrentSolutionPath = "existing.json";

        // Act
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.SaveSolutionInternalAsync());

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("disk full"));
            Assert.That(viewModel.HasUnsavedChanges, Is.True);
            Assert.That(viewModel.SolutionSaveState, Is.EqualTo(SolutionSaveState.NotSaved));
            Assert.That(viewModel.SolutionSaveStatusText, Does.Contain("Not saved"));
        }
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
            .Setup(candidate => candidate.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return runtime;
    }
}
