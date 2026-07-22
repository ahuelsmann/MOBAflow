// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.IO;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.Sound;

using Moq;

/// <summary>
/// Direct unit tests for individual <see cref="IWorkflowActionHandler"/> implementations.
/// Keeps handler edge cases readable without going through the full <see cref="ActionExecutor"/> pipeline.
/// </summary>
[TestFixture]
internal sealed class WorkflowActionHandlersTests
{
    [Test]
    public async Task ChangeJourneyStopHandler_MovesToNextStop()
    {
        var first = new Station { Name = "Porta Westfalica" };
        var second = new Station { Name = "Minden" };
        var journey = new Journey { Stations = [first, second] };
        var state = new JourneySessionState
        {
            JourneyId = journey.Id,
            CurrentStationId = first.Id,
            CurrentStationName = first.Name,
            CurrentPos = 0
        };
        var action = new WorkflowAction
        {
            Type = ActionType.ChangeJourneyStop,
            ChangeJourneyStop = new ChangeJourneyStopActionPayload { MoveToNextStop = true }
        };
        var context = new ActionExecutionContext
        {
            Z21 = new Mock<IZ21>().Object,
            CurrentJourney = journey,
            CurrentJourneySessionState = state
        };

        await new ChangeJourneyStopWorkflowActionHandler().ExecuteAsync(action, context);

        Assert.Multiple(() =>
        {
            Assert.That(state.CurrentStationId, Is.EqualTo(second.Id));
            Assert.That(state.CurrentStationName, Is.EqualTo("Minden"));
            Assert.That(state.CurrentPos, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task CommandHandler_WithValidBytes_SendsThroughZ21()
    {
        var z21 = new Mock<IZ21>();
        var bytes = new byte[] { 0x04, 0x00, 0x85, 0x00 };
        var handler = new CommandWorkflowActionHandler();
        var action = new WorkflowAction
        {
            Name = "Stop",
            Type = ActionType.Command,
            Command = new CommandActionPayload { BytesBase64 = Convert.ToBase64String(bytes) }
        };
        var context = new ActionExecutionContext { Z21 = z21.Object };

        await handler.ExecuteAsync(action, context);

        z21.Verify(z => z.SendCommandAsync(bytes, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task CommandHandler_WithoutBytes_SkipsSend()
    {
        var z21 = new Mock<IZ21>();
        var handler = new CommandWorkflowActionHandler();
        var action = new WorkflowAction
        {
            Type = ActionType.Command,
            Command = new CommandActionPayload()
        };

        await handler.ExecuteAsync(action, new ActionExecutionContext { Z21 = z21.Object });

        z21.Verify(z => z.SendCommandAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AudioHandler_WithExistingFile_PlaysThroughSoundPlayer()
    {
        const string path = @"C:\audio\horn.wav";
        var fileSystem = new FakeFileSystem(path);
        var soundPlayer = new RecordingSoundPlayer();
        var handler = new AudioWorkflowActionHandler(fileSystem: fileSystem);
        var action = new WorkflowAction
        {
            Type = ActionType.Audio,
            Audio = new AudioActionPayload { FilePath = path }
        };
        var context = new ActionExecutionContext
        {
            Z21 = Mock.Of<IZ21>(),
            SoundPlayer = soundPlayer
        };

        await handler.ExecuteAsync(action, context);

        Assert.That(soundPlayer.LastPath, Is.EqualTo(path));
    }

    [Test]
    public void AudioHandler_MissingFile_ThrowsFileNotFound()
    {
        var handler = new AudioWorkflowActionHandler(fileSystem: new FakeFileSystem());
        var action = new WorkflowAction
        {
            Type = ActionType.Audio,
            Audio = new AudioActionPayload { FilePath = @"C:\missing.wav" }
        };
        var context = new ActionExecutionContext
        {
            Z21 = Mock.Of<IZ21>(),
            SoundPlayer = new NullSoundPlayer()
        };

        Assert.ThrowsAsync<FileNotFoundException>(() => handler.ExecuteAsync(action, context));
    }

    [Test]
    public async Task AnnouncementHandler_WithStationAndService_GeneratesSpeech()
    {
        var announcementService = new RecordingAnnouncementService();
        var handler = new AnnouncementWorkflowActionHandler(announcementService);
        var station = new Station { Name = "Berlin Hbf" };
        var action = new WorkflowAction
        {
            Name = "Arrival",
            Type = ActionType.Announcement,
            Announcement = new AnnouncementActionPayload { Message = "Naechster Halt {StationName}" }
        };
        var context = new ActionExecutionContext
        {
            Z21 = Mock.Of<IZ21>(),
            CurrentStation = station,
            CurrentStationIndex = 2
        };

        await handler.ExecuteAsync(action, context);

        Assert.Multiple(() =>
        {
            Assert.That(announcementService.SpeakCalls, Is.EqualTo(1));
            Assert.That(announcementService.LastStation, Is.SameAs(station));
            Assert.That(announcementService.LastStationIndex, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task AnnouncementHandler_WithoutStation_SkipsExecution()
    {
        var announcementService = new RecordingAnnouncementService();
        var handler = new AnnouncementWorkflowActionHandler(announcementService);
        var action = new WorkflowAction
        {
            Type = ActionType.Announcement,
            Announcement = new AnnouncementActionPayload { Message = "Hello" }
        };

        await handler.ExecuteAsync(action, new ActionExecutionContext { Z21 = Mock.Of<IZ21>() });

        Assert.That(announcementService.SpeakCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task SelectSignalAspectHandler_ResolvesMultiplexerAndSendsTurnout()
    {
        var z21 = new Mock<IZ21>();
        var handler = new SelectSignalAspectWorkflowActionHandler();
        var action = new WorkflowAction
        {
            Type = ActionType.SelectSignalAspect,
            SelectSignalAspect = new SelectSignalAspectActionPayload
            {
                BaseAddress = 201,
                SignalAspect = SignalAspect.Hp0,
                MultiplexerArticleNumber = "5229",
                SignalArticleNumber = "4046"
            }
        };

        await handler.ExecuteAsync(action, new ActionExecutionContext { Z21 = z21.Object });

        z21.Verify(z => z.SetTurnoutAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ExecuteScriptHandler_MissingScriptFile_ThrowsFileNotFound()
    {
        var handler = new ExecuteScriptWorkflowActionHandler(fileSystem: new FakeFileSystem());
        var action = new WorkflowAction
        {
            Type = ActionType.ExecuteScript,
            PowerShell = new PowerShellActionPayload { ScriptPath = @"C:\missing.ps1" }
        };

        Assert.ThrowsAsync<FileNotFoundException>(() =>
            handler.ExecuteAsync(action, new ActionExecutionContext { Z21 = Mock.Of<IZ21>() }));
    }

    [Test]
    public async Task CommandHandler_PropagatesCancellationTokenToZ21()
    {
        var z21 = new Mock<IZ21>();
        var action = new WorkflowAction
        {
            Type = ActionType.Command,
            Command = new CommandActionPayload { BytesBase64 = "AQID" }
        };
        using var cancellation = new CancellationTokenSource();

        await new CommandWorkflowActionHandler().ExecuteAsync(
            action,
            new ActionExecutionContext { Z21 = z21.Object },
            cancellation.Token);

        z21.Verify(z => z.SendCommandAsync(It.IsAny<byte[]>(), cancellation.Token), Times.Once);
    }

    [Test]
    public async Task AudioHandler_PropagatesCancellationTokenToSoundPlayer()
    {
        const string path = "gong.wav";
        var player = new RecordingSoundPlayer();
        var action = new WorkflowAction
        {
            Type = ActionType.Audio,
            Audio = new AudioActionPayload { FilePath = path }
        };
        using var cancellation = new CancellationTokenSource();

        await new AudioWorkflowActionHandler(fileSystem: new FakeFileSystem(path)).ExecuteAsync(
            action,
            new ActionExecutionContext { Z21 = Mock.Of<IZ21>(), SoundPlayer = player },
            cancellation.Token);

        Assert.That(player.LastCancellationToken, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public async Task AnnouncementHandler_PropagatesCancellationTokenToSpeechService()
    {
        var service = new RecordingAnnouncementService();
        var action = new WorkflowAction
        {
            Type = ActionType.Announcement,
            Announcement = new AnnouncementActionPayload { Message = "Next stop" }
        };
        using var cancellation = new CancellationTokenSource();

        await new AnnouncementWorkflowActionHandler(service).ExecuteAsync(
            action,
            new ActionExecutionContext
            {
                Z21 = Mock.Of<IZ21>(),
                CurrentStation = new Station { Name = "Minden" }
            },
            cancellation.Token);

        Assert.That(service.LastCancellationToken, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public void MutatingHandlers_PreCancelledToken_DoesNotStartEffect()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var state = new JourneySessionState { CurrentPos = 0 };
        var first = new Station { Name = "First" };
        var second = new Station { Name = "Second" };
        var journey = new Journey { Stations = [first, second] };
        state.CurrentStationId = first.Id;
        var context = new ActionExecutionContext
        {
            Z21 = Mock.Of<IZ21>(),
            CurrentJourney = journey,
            CurrentJourneySessionState = state
        };
        var action = new WorkflowAction
        {
            Type = ActionType.ChangeJourneyStop,
            ChangeJourneyStop = new ChangeJourneyStopActionPayload { MoveToNextStop = true }
        };

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            new ChangeJourneyStopWorkflowActionHandler().ExecuteAsync(action, context, cancellation.Token));
        Assert.That(state.CurrentStationId, Is.EqualTo(first.Id));
    }

    [Test]
    public void ExecuteScriptHandler_PreCancelledToken_DoesNotStartProcess()
    {
        const string path = "existing.ps1";
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var action = new WorkflowAction
        {
            Type = ActionType.ExecuteScript,
            PowerShell = new PowerShellActionPayload { ScriptPath = path }
        };

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            new ExecuteScriptWorkflowActionHandler(fileSystem: new FakeFileSystem(path)).ExecuteAsync(
                action,
                new ActionExecutionContext { Z21 = Mock.Of<IZ21>() },
                cancellation.Token));
    }

    private sealed class FakeFileSystem : IFileSystem
    {
        private readonly HashSet<string> _files;

        public FakeFileSystem(params string[] existingFiles)
        {
            _files = new HashSet<string>(existingFiles, StringComparer.OrdinalIgnoreCase);
        }

        public bool FileExists(string path) => _files.Contains(path);
    }

    private sealed class RecordingSoundPlayer : ISoundPlayer
    {
        public string? LastPath { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task PlayAsync(string waveFile, CancellationToken cancellationToken = default)
        {
            LastPath = waveFile;
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAnnouncementService : IAnnouncementService
    {
        public int SpeakCalls { get; private set; }

        public Station? LastStation { get; private set; }

        public int LastStationIndex { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public bool IsSpeakerEngineAvailable => true;

        public string GenerateAnnouncementText(Journey journey, Station station, int stationIndex)
            => GenerateAnnouncementText(journey.Text, station, stationIndex);

        public string GenerateAnnouncementText(string? templateText, Station station, int stationIndex, string? templateName = null)
            => templateText ?? string.Empty;

        public Task GenerateAndSpeakAnnouncementAsync(Journey journey, Station station, int stationIndex, CancellationToken cancellationToken = default)
            => GenerateAndSpeakAnnouncementAsync(journey.Text, station, stationIndex, cancellationToken);

        public Task GenerateAndSpeakAnnouncementAsync(
            string? templateText,
            Station station,
            int stationIndex,
            CancellationToken cancellationToken = default,
            string? templateName = null,
            bool suppressSpeechErrors = true)
        {
            _ = templateText;
            _ = templateName;
            _ = suppressSpeechErrors;
            SpeakCalls++;
            LastStation = station;
            LastStationIndex = stationIndex;
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
