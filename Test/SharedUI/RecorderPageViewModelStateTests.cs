// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Interface;
using global::Moba.Common.Recording;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.Desktop.ViewModel;
using global::Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System.Text.Json;

/// <summary>
/// Verifies which recorder and replay commands the page offers in each session state, the texts it shows for each
/// state and how failed or throwing file and replay operations surface to the operator.
/// </summary>
[TestFixture]
internal sealed partial class RecorderPageViewModelStateTests
{
    private static readonly Guid SessionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly string[] LifecycleCommandNames = ["Start", "Pause", "Resume", "Stop", "AddMarker", "AddNote", "Export", "Import"];
    private static readonly string[] ReplayCommandNames = ["Play", "Pause", "Step", "Seek", "Cancel"];

    // State, Start, Pause, Resume, Stop, AddMarker, Import
    [TestCase(RecordingSessionState.Idle, true, false, false, false, false, true)]
    [TestCase(RecordingSessionState.Recording, false, true, false, true, true, false)]
    [TestCase(RecordingSessionState.Paused, false, false, true, true, true, false)]
    [TestCase(RecordingSessionState.Stopping, false, false, false, false, false, false)]
    [TestCase(RecordingSessionState.Completed, true, false, false, false, false, true)]
    [TestCase(RecordingSessionState.Faulted, true, false, false, false, false, true)]
    public void LifecycleCommands_FollowTheSessionState(
        RecordingSessionState state,
        bool canStart,
        bool canPause,
        bool canResume,
        bool canStop,
        bool canAnnotate,
        bool canImport)
    {
        using var fixture = new Fixture();
        fixture.ViewModel.AnnotationText = "Note";

        fixture.RaiseSession(SessionSnapshot(state));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.StartCommand.CanExecute(null), Is.EqualTo(canStart), "Start");
            Assert.That(fixture.ViewModel.PauseCommand.CanExecute(null), Is.EqualTo(canPause), "Pause");
            Assert.That(fixture.ViewModel.ResumeCommand.CanExecute(null), Is.EqualTo(canResume), "Resume");
            Assert.That(fixture.ViewModel.StopCommand.CanExecute(null), Is.EqualTo(canStop), "Stop");
            Assert.That(fixture.ViewModel.AddMarkerCommand.CanExecute(null), Is.EqualTo(canAnnotate), "AddMarker");
            Assert.That(fixture.ViewModel.AddNoteCommand.CanExecute(null), Is.EqualTo(canAnnotate), "AddNote");
            Assert.That(fixture.ViewModel.ImportCommand.CanExecute(null), Is.EqualTo(canImport), "Import");
            Assert.That(fixture.ViewModel.IsRecording, Is.EqualTo(state == RecordingSessionState.Recording));
            Assert.That(fixture.ViewModel.IsPaused, Is.EqualTo(state == RecordingSessionState.Paused));
        }
    }

    [Test]
    public void StartAndAnnotations_NeedNonBlankText()
    {
        using var fixture = new Fixture();
        fixture.ViewModel.SessionName = "  ";
        var startWithoutName = fixture.ViewModel.StartCommand.CanExecute(null);
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording));
        fixture.ViewModel.AnnotationText = " ";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(startWithoutName, Is.False);
            Assert.That(fixture.ViewModel.AddMarkerCommand.CanExecute(null), Is.False);
            Assert.That(fixture.ViewModel.AddNoteCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public void SessionStateChange_NotifiesTheCommandsSoTheButtonsUpdate()
    {
        using var fixture = new Fixture();
        var changed = new HashSet<string>(StringComparer.Ordinal);
        fixture.ViewModel.StartCommand.CanExecuteChanged += (_, _) => changed.Add("Start");
        fixture.ViewModel.PauseCommand.CanExecuteChanged += (_, _) => changed.Add("Pause");
        fixture.ViewModel.ResumeCommand.CanExecuteChanged += (_, _) => changed.Add("Resume");
        fixture.ViewModel.StopCommand.CanExecuteChanged += (_, _) => changed.Add("Stop");
        fixture.ViewModel.AddMarkerCommand.CanExecuteChanged += (_, _) => changed.Add("AddMarker");
        fixture.ViewModel.AddNoteCommand.CanExecuteChanged += (_, _) => changed.Add("AddNote");
        fixture.ViewModel.ExportCommand.CanExecuteChanged += (_, _) => changed.Add("Export");
        fixture.ViewModel.ImportCommand.CanExecuteChanged += (_, _) => changed.Add("Import");

        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording));

        Assert.That(changed, Is.EquivalentTo(LifecycleCommandNames));
    }

    [Test]
    public void ReplayStateChange_NotifiesTheReplayCommandsSoTheButtonsUpdate()
    {
        using var fixture = new Fixture();
        var changed = new HashSet<string>(StringComparer.Ordinal);
        fixture.ViewModel.PlayReplayCommand.CanExecuteChanged += (_, _) => changed.Add("Play");
        fixture.ViewModel.PauseReplayCommand.CanExecuteChanged += (_, _) => changed.Add("Pause");
        fixture.ViewModel.StepReplayCommand.CanExecuteChanged += (_, _) => changed.Add("Step");
        fixture.ViewModel.SeekReplayCommand.CanExecuteChanged += (_, _) => changed.Add("Seek");
        fixture.ViewModel.CancelReplayCommand.CanExecuteChanged += (_, _) => changed.Add("Cancel");

        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Ready, position: 0, total: 3));

        Assert.That(changed, Is.EquivalentTo(ReplayCommandNames));
    }

    [Test]
    public async Task StopInProgress_DisablesEveryLifecycleCommandUntilItCompletes()
    {
        using var fixture = new Fixture();
        fixture.ViewModel.AnnotationText = "Note";
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording));
        var stop = new TaskCompletionSource<RecordingStopResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.Setup(session => session.StopAsync(It.IsAny<CancellationToken>())).Returns(stop.Task);

        var stopping = fixture.ViewModel.StopCommand.ExecuteAsync(null);
        var busy = fixture.ViewModel.IsBusy;
        var commandsWhileBusy = new[]
        {
            fixture.ViewModel.PauseCommand.CanExecute(null),
            fixture.ViewModel.AddMarkerCommand.CanExecute(null),
            fixture.ViewModel.ImportCommand.CanExecute(null)
        };
        stop.SetResult(new RecordingStopResult(RecordingOperationResult.Success(), null));
        await stopping.ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(busy, Is.True);
            Assert.That(commandsWhileBusy, Is.All.False);
            Assert.That(fixture.ViewModel.IsBusy, Is.False);
            Assert.That(fixture.ViewModel.PauseCommand.CanExecute(null), Is.True);
        }
    }

    [TestCase(RecordingSessionState.Idle, false, "Ready to record")]
    [TestCase(RecordingSessionState.Recording, false, "Recording")]
    [TestCase(RecordingSessionState.Recording, true, "Recording limit reached")]
    [TestCase(RecordingSessionState.Paused, false, "Recording paused")]
    [TestCase(RecordingSessionState.Stopping, false, "Stopping and draining accepted entries")]
    [TestCase(RecordingSessionState.Completed, false, "Recording completed")]
    [TestCase(RecordingSessionState.Faulted, false, "Recording completed with a fault")]
    public void StatusText_DescribesTheSessionState(RecordingSessionState state, bool isLimitReached, string expected)
    {
        using var fixture = new Fixture();

        fixture.RaiseSession(SessionSnapshot(state) with { IsLimitReached = isLimitReached });

        Assert.That(fixture.ViewModel.StatusText, Is.EqualTo(expected));
    }

    [TestCase(0, "0 entries")]
    [TestCase(1, "1 entry")]
    [TestCase(2, "2 entries")]
    public void EntryCountText_UsesTheSingularOnlyForOneEntry(int count, string expected)
    {
        using var fixture = new Fixture();

        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording) with { EntryCount = count });

        Assert.That(fixture.ViewModel.EntryCountText, Is.EqualTo(expected));
    }

    [Test]
    public void SessionFailure_IsShownOnlyWithACodeAndAMessage()
    {
        using var fixture = new Fixture();

        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording) with { LastFailureMessage = "Ignored without code" });
        var withoutCode = fixture.ViewModel.ErrorMessage;
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording) with { LastFailureCode = RecordingFailureCode.LimitReached });
        var withoutMessage = fixture.ViewModel.ErrorMessage;
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Faulted) with
        {
            LastFailureCode = RecordingFailureCode.DrainTimeout,
            LastFailureMessage = "Entries were dropped."
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutCode, Is.Null);
            Assert.That(withoutMessage, Is.Null);
            Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Entries were dropped."));
            Assert.That(fixture.ViewModel.HasError, Is.True);
        }
    }

    [Test]
    public void AddMarker_KeepsTheTextWhenTheSessionRejectsIt()
    {
        using var fixture = new Fixture();
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording));
        fixture.Session.Setup(session => session.AddMarker(It.IsAny<string>()))
            .Returns(RecordingOperationResult.Failure(RecordingFailureCode.LimitReached, "Recording limit reached."));
        fixture.ViewModel.AnnotationText = "Platform 2";

        fixture.ViewModel.AddMarkerCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Recording limit reached."));
            Assert.That(fixture.ViewModel.AnnotationText, Is.EqualTo("Platform 2"));
        }
    }

    [Test]
    public void AddNote_ClearsTheTextAndAnEarlierErrorOnSuccess()
    {
        using var fixture = new Fixture();
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Paused));
        fixture.Session.Setup(session => session.AddNote(It.IsAny<string>()))
            .Returns(RecordingOperationResult.Failure(RecordingFailureCode.InvalidRequest, "Too long."));
        fixture.ViewModel.AnnotationText = "First";
        fixture.ViewModel.AddNoteCommand.Execute(null);
        fixture.Session.Setup(session => session.AddNote("Second")).Returns(RecordingOperationResult.Success());
        fixture.ViewModel.AnnotationText = "Second";

        fixture.ViewModel.AddNoteCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.Null);
            Assert.That(fixture.ViewModel.AnnotationText, Is.Empty);
        }
    }

    [Test]
    public void Start_PassesTheSessionNameAndProjectContext()
    {
        using var fixture = new Fixture();
        fixture.ViewModel.SessionName = "Yard run";

        fixture.ViewModel.StartCommand.Execute(null);

        fixture.Session.Verify(session => session.Start(It.Is<RecordingSessionStartRequest>(request =>
            request.Name == "Yard run" && request.SourceApplicationVersion == "1.0-test")), Times.Once);
    }

    [Test]
    public void Export_IsOfferedOnlyForACompletedArtifact()
    {
        using var fixture = new Fixture();
        var withoutArtifact = fixture.ViewModel.ExportCommand.CanExecute(null);

        fixture.CompleteWithArtifact();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutArtifact, Is.False);
            Assert.That(fixture.ViewModel.ExportCommand.CanExecute(null), Is.True);
            Assert.That(fixture.ViewModel.CanExportArtifact, Is.True);
        }
    }

    [TestCase("Disk full.", "Disk full.")]
    [TestCase(null, "The recording could not be exported.")]
    public async Task Export_Failure_ShowsTheReason(string? reason, string expected)
    {
        using var fixture = new Fixture();
        fixture.CompleteWithArtifact();
        fixture.Files.Setup(files => files.ExportAsync(It.IsAny<RecordingArtifact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordingFileExportResult(false, false, null, reason));

        await fixture.ViewModel.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo(expected));
            Assert.That(fixture.ViewModel.StatusText, Is.EqualTo("Recording completed"));
            Assert.That(fixture.ViewModel.IsBusy, Is.False);
        }
    }

    [Test]
    public async Task Export_CancelledByTheOperator_ShowsNoErrorAndKeepsTheStatus()
    {
        using var fixture = new Fixture();
        fixture.CompleteWithArtifact();
        fixture.Files.Setup(files => files.ExportAsync(It.IsAny<RecordingArtifact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordingFileExportResult(false, true, null, "Cancelled"));

        await fixture.ViewModel.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.Null);
            Assert.That(fixture.ViewModel.StatusText, Is.EqualTo("Recording completed"));
        }
    }

    [Test]
    public async Task Export_Throwing_ReportsTheOperationAndReleasesBusy()
    {
        using var fixture = new Fixture();
        fixture.CompleteWithArtifact();
        fixture.Files.Setup(files => files.ExportAsync(It.IsAny<RecordingArtifact>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("locked"));

        await fixture.ViewModel.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Exporting the recording failed."));
            Assert.That(fixture.ViewModel.IsBusy, Is.False);
        }
    }

    [Test]
    public async Task Import_FailureWithoutReason_ShowsTheDefaultMessage()
    {
        using var fixture = new Fixture();
        fixture.Files.Setup(files => files.ImportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordingFileImportResult(false, false, "bad.json", null, null));

        await fixture.ViewModel.ImportCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("The recording could not be imported."));
    }

    [Test]
    public async Task Import_CancelledByTheOperator_ImportsNothing()
    {
        using var fixture = new Fixture();
        fixture.Files.Setup(files => files.ImportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordingFileImportResult(false, true, null, null, null));

        await fixture.ViewModel.ImportCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.Null);
            fixture.Session.Verify(session => session.Import(It.IsAny<RecordingArtifact>()), Times.Never);
        }
    }

    [Test]
    public async Task Import_Throwing_ReportsTheOperationAndReleasesBusy()
    {
        using var fixture = new Fixture();
        fixture.Files.Setup(files => files.ImportAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("picker"));

        await fixture.ViewModel.ImportCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Importing the recording failed."));
            Assert.That(fixture.ViewModel.IsBusy, Is.False);
        }
    }

    [Test]
    public async Task Stop_Throwing_ReportsTheOperationAndReleasesBusy()
    {
        using var fixture = new Fixture();
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording));
        fixture.Session.Setup(session => session.StopAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("drain"));

        await fixture.ViewModel.StopCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Stopping the recording failed."));
            Assert.That(fixture.ViewModel.IsBusy, Is.False);
        }
    }

    [Test]
    public async Task Stop_RejectedBySession_ShowsItsMessage()
    {
        using var fixture = new Fixture();
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording));
        fixture.Session.Setup(session => session.StopAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordingStopResult(
                RecordingOperationResult.Failure(RecordingFailureCode.DrainTimeout, "Draining timed out."), null));

        await fixture.ViewModel.StopCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Draining timed out."));
    }

    [Test]
    public void CompletedArtifact_IsLoadedIntoReplayOncePerSession()
    {
        using var fixture = new Fixture();

        fixture.CompleteWithArtifact();
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Completed) with { EntryCount = 1 });

        fixture.Replay.Verify(replay => replay.Load(It.IsAny<RecordingArtifact>()), Times.Once);
    }

    [Test]
    public void CompletedArtifact_RejectedByReplay_ShowsTheReasonAndRetriesLater()
    {
        using var fixture = new Fixture();
        fixture.Replay.Setup(replay => replay.Load(It.IsAny<RecordingArtifact>()))
            .Returns(RecordingReplayOperationResult.Failure(RecordingReplayFailureCode.LiveHardwareConnected, "Disconnect the Z21 first."));

        fixture.CompleteWithArtifact();
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Completed) with { EntryCount = 1 });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Disconnect the Z21 first."));
            fixture.Replay.Verify(replay => replay.Load(It.IsAny<RecordingArtifact>()), Times.Exactly(2));
        }
    }

    [TestCase(RecordingReplayState.Idle, null, "Load or complete a recording to start isolated replay")]
    [TestCase(RecordingReplayState.Ready, null, "Isolated replay ready")]
    [TestCase(RecordingReplayState.Paused, null, "Isolated replay paused")]
    [TestCase(RecordingReplayState.Completed, null, "Isolated replay completed")]
    [TestCase(RecordingReplayState.Blocked, null, "Isolated replay blocked")]
    [TestCase(RecordingReplayState.Blocked, "Z21 connected", "Z21 connected")]
    [TestCase(RecordingReplayState.Faulted, null, "Isolated replay failed")]
    [TestCase(RecordingReplayState.Faulted, "Apply failed", "Apply failed")]
    public void ReplayStatusText_DescribesTheReplayState(RecordingReplayState state, string? failure, string expected)
    {
        using var fixture = new Fixture();

        fixture.RaiseReplay(ReplaySnapshot(state) with { LastFailureMessage = failure });

        Assert.That(fixture.ViewModel.ReplayStatusText, Is.EqualTo(expected));
    }

    [Test]
    public void ReplayStatus_ProjectsPositionSpeedAndElapsedTime()
    {
        using var fixture = new Fixture();

        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Playing, position: 2, total: 5) with
        {
            Speed = 2,
            Elapsed = new TimeSpan(0, 1, 2, 3, 45)
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ReplayPosition, Is.EqualTo(2));
            Assert.That(fixture.ViewModel.ReplayMaximum, Is.EqualTo(5));
            Assert.That(fixture.ViewModel.ReplaySeekPosition, Is.EqualTo(2));
            Assert.That(fixture.ViewModel.SelectedReplaySpeed, Is.EqualTo(2));
            Assert.That(fixture.ViewModel.ReplayElapsedText, Is.EqualTo("01:02:03.045"));
            Assert.That(fixture.ViewModel.ReplayStatusText, Is.EqualTo("Replaying at 2x"));
            Assert.That(fixture.ViewModel.IsReplayLoaded, Is.True);
        }
    }

    [TestCase(RecordingReplayFailureCode.LiveHardwareConnected, true)]
    [TestCase(RecordingReplayFailureCode.ApplyFailed, true)]
    [TestCase(RecordingReplayFailureCode.InternalError, true)]
    [TestCase(RecordingReplayFailureCode.InvalidPosition, false)]
    [TestCase(RecordingReplayFailureCode.Cancelled, false)]
    public void ReplayFailure_IsShownOnlyForOperatorRelevantCodes(RecordingReplayFailureCode code, bool shown)
    {
        using var fixture = new Fixture();

        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Blocked) with { LastFailureCode = code, LastFailureMessage = "Replay problem" });

        Assert.That(fixture.ViewModel.ErrorMessage, shown ? Is.EqualTo("Replay problem") : Is.Null);
    }

    // State, position, total, Play, Pause, Step, Seek, Cancel
    [TestCase(RecordingReplayState.Ready, 0, 3, true, false, true, true, false)]
    [TestCase(RecordingReplayState.Paused, 1, 3, true, false, true, true, true)]
    [TestCase(RecordingReplayState.Completed, 3, 3, true, false, false, true, true)]
    [TestCase(RecordingReplayState.Playing, 1, 3, false, true, false, false, true)]
    [TestCase(RecordingReplayState.Faulted, 1, 3, false, false, false, false, true)]
    public void ReplayCommands_FollowTheReplayState(
        RecordingReplayState state,
        int position,
        int total,
        bool canPlay,
        bool canPause,
        bool canStep,
        bool canSeek,
        bool canCancel)
    {
        using var fixture = new Fixture();

        fixture.RaiseReplay(ReplaySnapshot(state, position, total));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.PlayReplayCommand.CanExecute(null), Is.EqualTo(canPlay), "Play");
            Assert.That(fixture.ViewModel.PauseReplayCommand.CanExecute(null), Is.EqualTo(canPause), "Pause");
            Assert.That(fixture.ViewModel.StepReplayCommand.CanExecute(null), Is.EqualTo(canStep), "Step");
            Assert.That(fixture.ViewModel.SeekReplayCommand.CanExecute(null), Is.EqualTo(canSeek), "Seek");
            Assert.That(fixture.ViewModel.CancelReplayCommand.CanExecute(null), Is.EqualTo(canCancel), "Cancel");
        }
    }

    [Test]
    public void ReplayCommands_WithoutALoadedArtifact_AreAllUnavailable()
    {
        using var fixture = new Fixture();

        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Paused, position: 1, total: 3) with { SessionId = null });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.IsReplayLoaded, Is.False);
            Assert.That(fixture.ViewModel.PlayReplayCommand.CanExecute(null), Is.False);
            Assert.That(fixture.ViewModel.StepReplayCommand.CanExecute(null), Is.False);
            Assert.That(fixture.ViewModel.SeekReplayCommand.CanExecute(null), Is.False);
            Assert.That(fixture.ViewModel.CancelReplayCommand.CanExecute(null), Is.False);
        }
    }

    [Test]
    public async Task SeekReplay_ClampsTheTargetToTheRecording()
    {
        using var fixture = new Fixture();
        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Paused, position: 1, total: 3));
        fixture.ViewModel.ReplaySeekPosition = 7.6;

        await fixture.ViewModel.SeekReplayCommand.ExecuteAsync(null).ConfigureAwait(false);

        fixture.Replay.Verify(replay => replay.SeekAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StepReplay_Throwing_ReportsTheOperationAndReleasesBusy()
    {
        using var fixture = new Fixture();
        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Paused, position: 1, total: 3));
        fixture.Replay.Setup(replay => replay.StepAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("apply"));

        await fixture.ViewModel.StepReplayCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Stepping isolated replay failed."));
            Assert.That(fixture.ViewModel.IsReplayBusy, Is.False);
        }
    }

    [Test]
    public async Task CancelReplay_RejectedByReplay_ShowsItsMessage()
    {
        using var fixture = new Fixture();
        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Paused, position: 1, total: 3));
        fixture.Replay.Setup(replay => replay.CancelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RecordingReplayOperationResult.Failure(RecordingReplayFailureCode.InvalidState, "Nothing to cancel."));

        await fixture.ViewModel.CancelReplayCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Nothing to cancel."));
    }

    [Test]
    public void PauseReplay_RejectedByReplay_ShowsItsMessage()
    {
        using var fixture = new Fixture();
        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Playing, position: 1, total: 3));
        fixture.Replay.Setup(replay => replay.Pause())
            .Returns(RecordingReplayOperationResult.Failure(RecordingReplayFailureCode.InvalidState, "Not playing."));

        fixture.ViewModel.PauseReplayCommand.Execute(null);

        Assert.That(fixture.ViewModel.ErrorMessage, Is.EqualTo("Not playing."));
    }

    [Test]
    public void Dispose_StopsFollowingSessionAndReplayStatus()
    {
        var fixture = new Fixture();
        fixture.Dispose();
        fixture.Dispose();

        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording));
        fixture.RaiseReplay(ReplaySnapshot(RecordingReplayState.Playing, position: 1, total: 3));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.State, Is.EqualTo(RecordingSessionState.Idle));
            Assert.That(fixture.ViewModel.ReplayState, Is.EqualTo(RecordingReplayState.Idle));
        }
    }

    [Test]
    public void SelectedEntry_SurvivesAFilterThatKeepsIt()
    {
        using var fixture = new Fixture();
        var entries = new[] { Entry(1, "runtime"), Entry(2, "journey") };
        fixture.Session.Setup(session => session.ReadEntries(0, It.IsAny<int>())).Returns(entries);
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording) with { EntryCount = 2 });
        fixture.ViewModel.SelectedEntry = fixture.ViewModel.TimelineEntries[1];

        fixture.ViewModel.CategoryFilter = " journey ";
        var keptSelection = fixture.ViewModel.SelectedEntry;
        fixture.ViewModel.CategoryFilter = "runtime";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(keptSelection?.Sequence, Is.EqualTo(2));
            Assert.That(fixture.ViewModel.SelectedEntry, Is.Null);
            Assert.That(fixture.ViewModel.TimelineEntries.Select(entry => entry.Sequence), Is.EqualTo(new long[] { 1 }));
            Assert.That(fixture.ViewModel.HasEntries, Is.True);
        }
    }

    [Test]
    public void NewSession_ClearsTheTimelineOfThePreviousSession()
    {
        using var fixture = new Fixture();
        fixture.Session.Setup(session => session.ReadEntries(0, It.IsAny<int>())).Returns((RecordingEntry[])[Entry(1, "runtime")]);
        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording) with { EntryCount = 1 });
        fixture.Session.Setup(session => session.ReadEntries(It.IsAny<long>(), It.IsAny<int>())).Returns(Array.Empty<RecordingEntry>());

        fixture.RaiseSession(SessionSnapshot(RecordingSessionState.Recording) with { SessionId = Guid.NewGuid() });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.ViewModel.TimelineEntries, Is.Empty);
            Assert.That(fixture.ViewModel.HasEntries, Is.False);
        }
    }

    private static RecordingSessionSnapshot SessionSnapshot(RecordingSessionState state) =>
        new(state, SessionId, "Run", 0, 0, 0, 0, false, RecordingFailureCode.None, null);

    private static RecordingReplaySnapshot ReplaySnapshot(RecordingReplayState state, int position = 0, int total = 0) =>
        new(state, SessionId, position, total, position, 0, TimeSpan.Zero, 1, null, RecordingReplayFailureCode.None, null);

    private static RecordingEntry Entry(long sequence, string category) => new(
        sequence,
        new DateTimeOffset(2026, 7, 21, 10, 0, 0, TimeSpan.Zero).AddSeconds(sequence),
        TimeSpan.FromSeconds(sequence),
        category,
        "unit-test",
        $"{category}.test",
        "information",
        null,
        [],
        JsonSerializer.SerializeToElement(new { sequence }),
        $"Entry {sequence}",
        RecordingReplayApplicability.DisplayOnly);

    private sealed partial class Fixture : IDisposable
    {
        public Fixture()
        {
            Session.SetupGet(session => session.CurrentStatus).Returns(SessionSnapshot(RecordingSessionState.Idle) with { SessionId = null });
            Session.Setup(session => session.ReadEntries(It.IsAny<long>(), It.IsAny<int>())).Returns(Array.Empty<RecordingEntry>());
            Session.Setup(session => session.Start(It.IsAny<RecordingSessionStartRequest>())).Returns(RecordingOperationResult.Success());
            Replay.SetupGet(replay => replay.CurrentStatus).Returns(ReplaySnapshot(RecordingReplayState.Idle) with { SessionId = null });
            Replay.Setup(replay => replay.Load(It.IsAny<RecordingArtifact>())).Returns(RecordingReplayOperationResult.Success());
            Replay.Setup(replay => replay.Play(It.IsAny<double>())).Returns(RecordingReplayOperationResult.Success());
            Replay.Setup(replay => replay.SeekAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RecordingReplayOperationResult.Success());
            var context = new Mock<IRecordingContextProvider>();
            context.SetupGet(provider => provider.SourceApplicationVersion).Returns("1.0-test");
            ViewModel = new RecorderPageViewModel(
                Session.Object,
                Replay.Object,
                Files.Object,
                context.Object,
                ImmediateUiDispatcher.Instance,
                NullLogger<RecorderPageViewModel>.Instance);
        }

        public Mock<IRecordingSessionService> Session { get; } = new();

        public Mock<IRecordingReplayService> Replay { get; } = new();

        public Mock<IRecordingFileService> Files { get; } = new();

        public RecorderPageViewModel ViewModel { get; }

        public void RaiseSession(RecordingSessionSnapshot snapshot) =>
            Session.Raise(session => session.StatusChanged += null, snapshot);

        public void RaiseReplay(RecordingReplaySnapshot snapshot) =>
            Replay.Raise(replay => replay.StatusChanged += null, snapshot);

        public void CompleteWithArtifact()
        {
            var started = new DateTimeOffset(2026, 7, 21, 10, 0, 0, TimeSpan.Zero);
            var artifact = new RecordingArtifact(
                new RecordingSessionMetadata(SessionId, "Run", started, started.AddSeconds(1)),
                "1.0",
                null,
                new RecordingArtifactOptions(100, 1_000_000),
                (RecordingEntry[])[Entry(1, "runtime")]);
            Session.SetupGet(session => session.CurrentArtifact).Returns(artifact);
            RaiseSession(SessionSnapshot(RecordingSessionState.Completed) with { EntryCount = 1 });
        }

        public void Dispose() => ViewModel.Dispose();
    }
}
