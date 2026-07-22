// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Service.Recording;
using global::Moba.Common.Recording;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.ViewModel;

using Microsoft.Extensions.Logging.Abstractions;

using System.Text.Json;

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
            Assert.That(viewModel.IsReplayLoaded, Is.True);
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

    [Test]
    public async Task ReplayCommands_Should_ProjectPositionAndResetThroughIsolatedService()
    {
        await using var source = new RecordingSessionService(TimeProvider.System);
        source.Start(new RecordingSessionStartRequest("Replay", "1.0"));
        source.AddMarker("Checkpoint");
        var artifact = (await source.StopAsync()).Artifact!;
        await using var session = new RecordingSessionService(TimeProvider.System);
        var fileService = new StubRecordingFileService
        {
            ImportResult = new RecordingFileImportResult(true, false, "replay.json", artifact, null)
        };
        var replayService = new StubRecordingReplayService();
        using var viewModel = CreateViewModel(session, fileService, replayService);

        await viewModel.ImportCommand.ExecuteAsync(null);
        await viewModel.StepReplayCommand.ExecuteAsync(null);
        viewModel.ReplaySeekPosition = 2;
        await viewModel.SeekReplayCommand.ExecuteAsync(null);
        viewModel.PlayReplayCommand.Execute(null);
        viewModel.SelectedReplaySpeed = 4;
        viewModel.PauseReplayCommand.Execute(null);
        await viewModel.CancelReplayCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(replayService.LoadedArtifact, Is.SameAs(artifact));
            Assert.That(viewModel.ReplayState, Is.EqualTo(RecordingReplayState.Ready));
            Assert.That(viewModel.ReplayPosition, Is.Zero);
            Assert.That(viewModel.ReplayMaximum, Is.EqualTo(artifact.Entries.Length));
            Assert.That(viewModel.ReplayStatusText, Is.EqualTo("Isolated replay ready"));
            Assert.That(replayService.LastPlayedSpeed, Is.EqualTo(4));
            Assert.That(replayService.PlayCallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task EntityFilter_Should_MatchStableKindOrIdentifierWithoutReordering()
    {
        var journeyId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var artifact = CreateEntityFilterArtifact(journeyId);
        await using var session = new RecordingSessionService(TimeProvider.System);
        var fileService = new StubRecordingFileService
        {
            ImportResult = new RecordingFileImportResult(true, false, "entities.json", artifact, null)
        };
        using var viewModel = CreateViewModel(session, fileService);
        await viewModel.ImportCommand.ExecuteAsync(null);

        viewModel.EntityFilter = "journey";
        var kindSequences = viewModel.TimelineEntries.Select(entry => entry.Sequence).ToArray();
        viewModel.EntityFilter = journeyId.ToString();
        var idSequences = viewModel.TimelineEntries.Select(entry => entry.Sequence).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(kindSequences, Is.EqualTo(new long[] { 1 }));
            Assert.That(idSequences, Is.EqualTo(new long[] { 1 }));
        });
    }

    [Test]
    public async Task LargeImportedTimeline_Should_DrainAllBoundedBatchesWithoutReordering()
    {
        const int entryCount = 1_200;
        var artifact = CreateLargeTimelineArtifact(entryCount);
        await using var session = new RecordingSessionService(TimeProvider.System);
        var fileService = new StubRecordingFileService
        {
            ImportResult = new RecordingFileImportResult(true, false, "large.json", artifact, null)
        };
        using var viewModel = CreateViewModel(session, fileService);

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.TimelineEntries, Has.Count.EqualTo(entryCount));
            Assert.That(viewModel.TimelineEntries.Select(entry => entry.Sequence), Is.Ordered.And.Unique);
            Assert.That(viewModel.TimelineEntries[0].Sequence, Is.EqualTo(1));
            Assert.That(viewModel.TimelineEntries[^1].Sequence, Is.EqualTo(entryCount));
        });

        viewModel.SearchText = "Entry 1199";
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.TimelineEntries, Has.Count.EqualTo(1));
            Assert.That(viewModel.TimelineEntries[0].Sequence, Is.EqualTo(1_200));
        });
    }

    private static RecorderPageViewModel CreateViewModel(
        RecordingSessionService session,
        StubRecordingFileService? fileService = null,
        StubRecordingReplayService? replayService = null) =>
        new(
            session,
            replayService ?? new StubRecordingReplayService(),
            fileService ?? new StubRecordingFileService(),
            new StubRecordingContextProvider(),
            new ImmediateUiDispatcher(),
            NullLogger<RecorderPageViewModel>.Instance);

    private static RecordingArtifact CreateEntityFilterArtifact(Guid journeyId)
    {
        var started = new DateTimeOffset(2026, 7, 21, 10, 0, 0, TimeSpan.Zero);
        var entries = new[]
        {
            new RecordingEntry(
                1,
                started,
                TimeSpan.Zero,
                "journey",
                "unit-test",
                "journey.transition",
                "information",
                null,
                [new RecordingEntityReference("journey", journeyId)],
                JsonSerializer.SerializeToElement(new { value = 1 }),
                "Journey entry",
                RecordingReplayApplicability.DisplayOnly),
            new RecordingEntry(
                2,
                started.AddSeconds(1),
                TimeSpan.FromSeconds(1),
                "z21",
                "unit-test",
                "z21.feedback.activated",
                "information",
                null,
                [new RecordingEntityReference("feedback", Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"))],
                JsonSerializer.SerializeToElement(new { value = 2 }),
                "Feedback entry",
                RecordingReplayApplicability.DisplayOnly)
        };
        return new RecordingArtifact(
            new RecordingSessionMetadata(Guid.NewGuid(), "Entities", started, started.AddSeconds(1)),
            "1.0",
            null,
            new RecordingArtifactOptions(100, 1_000_000),
            entries);
    }

    private static RecordingArtifact CreateLargeTimelineArtifact(int entryCount)
    {
        var started = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
        var entries = Enumerable.Range(0, entryCount)
            .Select(index => new RecordingEntry(
                index + 1,
                started.AddMilliseconds(index),
                TimeSpan.FromMilliseconds(index),
                "runtime",
                "throughput-test",
                "runtime.test",
                "information",
                null,
                [],
                JsonSerializer.SerializeToElement(new { index }),
                $"Entry {index}",
                RecordingReplayApplicability.DisplayOnly))
            .ToArray();
        return new RecordingArtifact(
            new RecordingSessionMetadata(Guid.NewGuid(), "Large timeline", started, started.AddMilliseconds(entryCount - 1)),
            "1.0",
            null,
            new RecordingArtifactOptions(entryCount, 1_000_000),
            entries);
    }

    private sealed class StubRecordingReplayService : IRecordingReplayService
    {
        private RecordingReplaySnapshot _status =
            new(RecordingReplayState.Idle, null, 0, 0, 0, 0, TimeSpan.Zero, 1, null, RecordingReplayFailureCode.None, null);

        public RecordingArtifact? LoadedArtifact { get; private set; }

        public double LastPlayedSpeed { get; private set; }

        public int PlayCallCount { get; private set; }

        public RecordingReplaySnapshot CurrentStatus => _status;

        public event Action<RecordingReplaySnapshot>? StatusChanged;

        public RecordingReplayOperationResult Load(RecordingArtifact artifact)
        {
            LoadedArtifact = artifact;
            SetStatus(RecordingReplayState.Ready, 0, null);
            return RecordingReplayOperationResult.Success();
        }

        public RecordingReplayOperationResult Play(double speed)
        {
            LastPlayedSpeed = speed;
            PlayCallCount++;
            _status = _status with { Speed = speed };
            SetStatus(RecordingReplayState.Playing, _status.Position, _status.CurrentEntry);
            return RecordingReplayOperationResult.Success();
        }

        public RecordingReplayOperationResult Pause()
        {
            SetStatus(RecordingReplayState.Paused, _status.Position, _status.CurrentEntry);
            return RecordingReplayOperationResult.Success();
        }

        public Task<RecordingReplayOperationResult> StepAsync(CancellationToken cancellationToken = default)
        {
            var position = Math.Min(_status.Position + 1, _status.TotalEntryCount);
            SetStatus(
                position == _status.TotalEntryCount ? RecordingReplayState.Completed : RecordingReplayState.Paused,
                position,
                LoadedArtifact?.Entries.ElementAtOrDefault(position - 1));
            return Task.FromResult(RecordingReplayOperationResult.Success());
        }

        public Task<RecordingReplayOperationResult> SeekAsync(int position, CancellationToken cancellationToken = default)
        {
            SetStatus(
                position == _status.TotalEntryCount ? RecordingReplayState.Completed : RecordingReplayState.Paused,
                position,
                LoadedArtifact?.Entries.ElementAtOrDefault(position - 1));
            return Task.FromResult(RecordingReplayOperationResult.Success());
        }

        public Task<RecordingReplayOperationResult> CancelAsync(CancellationToken cancellationToken = default)
        {
            SetStatus(RecordingReplayState.Ready, 0, null);
            return Task.FromResult(RecordingReplayOperationResult.Success());
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private void SetStatus(RecordingReplayState state, int position, RecordingEntry? currentEntry)
        {
            _status = _status with
            {
                State = state,
                SessionId = LoadedArtifact?.Metadata.SessionId,
                Position = position,
                TotalEntryCount = LoadedArtifact?.Entries.Length ?? 0,
                AppliedEntryCount = position,
                Elapsed = currentEntry?.Elapsed ?? TimeSpan.Zero,
                CurrentEntry = currentEntry
            };
            StatusChanged?.Invoke(_status);
        }
    }

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