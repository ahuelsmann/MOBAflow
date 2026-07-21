// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Service.Recording;
using global::Moba.Common.Recording;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.ViewModel;

using Microsoft.Extensions.Logging.Abstractions;

internal sealed class RecorderPageViewModelTests
{
    [Test]
    public async Task LifecycleCommands_Should_UpdateStateAnnotationsAndTimelineFilters()
    {
        await using var session = new RecordingSessionService(TimeProvider.System);
        using var viewModel = CreateViewModel(session);
        viewModel.SessionName = "Yard run";

        viewModel.StartCommand.Execute(null);
        viewModel.AnnotationText = "Reached the yard";
        viewModel.AddMarkerCommand.Execute(null);
        viewModel.PauseCommand.Execute(null);
        viewModel.AnnotationText = "Waiting for clearance";
        viewModel.AddNoteCommand.Execute(null);
        viewModel.ResumeCommand.Execute(null);
        await viewModel.StopCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RecordingSessionState.Completed));
            Assert.That(viewModel.CanExportArtifact, Is.True);
            Assert.That(viewModel.TimelineEntries.Select(entry => entry.TypeKey), Does.Contain("recorder.marker"));
            Assert.That(viewModel.TimelineEntries.Select(entry => entry.TypeKey), Does.Contain("recorder.note"));
            Assert.That(viewModel.TimelineEntries.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
            Assert.That(viewModel.HasError, Is.False);
        });

        viewModel.SearchText = "clearance";
        Assert.That(viewModel.TimelineEntries, Has.Count.EqualTo(1));
        Assert.That(viewModel.TimelineEntries[0].TypeKey, Is.EqualTo("recorder.note"));

        viewModel.ClearFiltersCommand.Execute(null);
        Assert.That(viewModel.TimelineEntries, Has.Count.GreaterThan(1));
    }

    [Test]
    public async Task ImportAndExportCommands_Should_RoundTripCompletedArtifactBoundary()
    {
        await using var source = new RecordingSessionService(TimeProvider.System);
        source.Start(new RecordingSessionStartRequest("Imported", "1.0"));
        source.AddNote("Portable context");
        var artifact = (await source.StopAsync()).Artifact!;

        await using var target = new RecordingSessionService(TimeProvider.System);
        var fileService = new StubRecordingFileService
        {
            ImportResult = new RecordingFileImportResult(true, false, "recording.json", artifact, null)
        };
        using var viewModel = CreateViewModel(target, fileService);

        await viewModel.ImportCommand.ExecuteAsync(null);
        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RecordingSessionState.Completed));
            Assert.That(viewModel.TimelineEntries.Select(entry => entry.DisplayText), Does.Contain("Note: Portable context"));
            Assert.That(fileService.ExportedArtifact, Is.SameAs(artifact));
            Assert.That(viewModel.StatusText, Does.StartWith("Exported to "));
            Assert.That(viewModel.HasError, Is.False);
        });
    }

    [Test]
    public async Task ImportCommand_Should_SurfaceValidationFailureWithoutMutatingSession()
    {
        await using var session = new RecordingSessionService(TimeProvider.System);
        var fileService = new StubRecordingFileService
        {
            ImportResult = new RecordingFileImportResult(
                false,
                false,
                "invalid.json",
                null,
                "Import failed at $.format: Unsupported version.")
        };
        using var viewModel = CreateViewModel(session, fileService);

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.State, Is.EqualTo(RecordingSessionState.Idle));
            Assert.That(viewModel.ErrorMessage, Does.Contain("Unsupported version"));
            Assert.That(viewModel.TimelineEntries, Is.Empty);
        });
    }

    private static RecorderPageViewModel CreateViewModel(
        RecordingSessionService session,
        StubRecordingFileService? fileService = null) =>
        new(
            session,
            fileService ?? new StubRecordingFileService(),
            new StubRecordingContextProvider(),
            new ImmediateUiDispatcher(),
            NullLogger<RecorderPageViewModel>.Instance);

    private sealed class StubRecordingFileService : IRecordingFileService
    {
        public RecordingFileImportResult ImportResult { get; set; } =
            new(false, true, null, null, null);

        public RecordingArtifact? ExportedArtifact { get; private set; }

        public Task<RecordingFileExportResult> ExportAsync(
            RecordingArtifact artifact,
            CancellationToken cancellationToken = default)
        {
            ExportedArtifact = artifact;
            return Task.FromResult(new RecordingFileExportResult(true, false, "recording.json", null));
        }

        public Task<RecordingFileImportResult> ImportAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ImportResult);
    }

    private sealed class StubRecordingContextProvider : IRecordingContextProvider
    {
        public string SourceApplicationVersion => "1.0-test";

        public RecordingProjectIdentity? GetProjectIdentity() =>
            new(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), "Test project");
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public void InvokeOnUi(Action action) => action();

        public Task InvokeOnUiAsync(Func<Task> asyncAction) => asyncAction();

        public Task<T> InvokeOnUiAsync<T>(Func<Task<T>> asyncFunc) => asyncFunc();

        public void InvokeOnUiHighPriority(Action action) => action();

        public void InvokeOnUiLowPriority(Action action) => action();

        public Task InvokeOnUiAsync(Func<Task> asyncAction, UiPriority priority) => asyncAction();
    }
}