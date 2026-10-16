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
    public async Task ExternalHandlers_WithRecordingAdapter_DoNotReachProductionDependencies()
    {
        var adapter = new RecordingEffectAdapter();
        var files = new Mock<IFileSystem>(MockBehavior.Strict);
        var z21 = new Mock<IZ21>(MockBehavior.Strict);
        var sound = new Mock<ISoundPlayer>(MockBehavior.Strict);
        var speech = new Mock<IAnnouncementService>(MockBehavior.Strict);
        speech.Setup(service => service.GenerateAnnouncementText("Next {StationName}", It.IsAny<Station>(), 2, "Arrival"))
            .Returns("Next Minden");
        var context = new ActionExecutionContext
        {
            Z21 = z21.Object, SoundPlayer = sound.Object,
            CurrentStation = new Station { Name = "Minden" }, CurrentStationIndex = 2
        };
        var actions = new WorkflowAction[]
        {
            new() { Type = ActionType.Command, Command = new() { BytesBase64 = "AQID" } },
            new() { Type = ActionType.Audio, Audio = new() { FilePath = "missing.wav" } },
            new() { Name = "Arrival", Type = ActionType.Announcement, Announcement = new() { Message = "Next {StationName}" } },
            new() { Type = ActionType.ExecuteScript, PowerShell = new() { ScriptPath = "missing.ps1", Arguments = "-Mode test" } },
            new() { Type = ActionType.SelectSignalAspect, SelectSignalAspect = new() { BaseAddress = 201 } },
            new() { Type = ActionType.TrainDestinationDisplay, TrainDestinationDisplay = new() { DisplayDeviceId = Guid.NewGuid() } }
        };
        IWorkflowActionHandler[] handlers =
        [
            new CommandWorkflowActionHandler(effectAdapter: adapter),
            new AudioWorkflowActionHandler(fileSystem: files.Object, effectAdapter: adapter),
            new AnnouncementWorkflowActionHandler(speech.Object, effectAdapter: adapter),
            new ExecuteScriptWorkflowActionHandler(fileSystem: files.Object, effectAdapter: adapter),
            new SelectSignalAspectWorkflowActionHandler(effectAdapter: adapter),
            new TrainDestinationDisplayWorkflowActionHandler(effectAdapter: adapter)
        ];
        using var cancellation = new CancellationTokenSource();

        for (var index = 0; index < actions.Length; index++)
            await handlers[index].ExecuteAsync(actions[index], context, cancellation.Token).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adapter.Requests.Select(request => request.ActionId), Is.EqualTo(actions.Select(action => action.Id)));
            Assert.That(adapter.Tokens, Is.All.EqualTo(cancellation.Token));
            Assert.That(((WorkflowCommandEffectRequest)adapter.Requests[0]).Bytes.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(((WorkflowAudioEffectRequest)adapter.Requests[1]).FilePath, Is.EqualTo("missing.wav"));
            Assert.That(((WorkflowAnnouncementEffectRequest)adapter.Requests[2]).RenderedText, Is.EqualTo("Next Minden"));
            Assert.That(((WorkflowScriptEffectRequest)adapter.Requests[3]).ScriptPath, Is.EqualTo("missing.ps1"));
            Assert.That(((WorkflowSignalEffectRequest)adapter.Requests[4]).Command.DccAddress, Is.EqualTo(201));
            Assert.That(((WorkflowDisplayEffectRequest)adapter.Requests[5]).DisplayDeviceId,
                Is.EqualTo(actions[5].TrainDestinationDisplay!.DisplayDeviceId));
        }
        files.VerifyNoOtherCalls();
        z21.VerifyNoOtherCalls();
        sound.VerifyNoOtherCalls();
        speech.Verify(service => service.GenerateAndSpeakAnnouncementAsync(It.IsAny<string>(), It.IsAny<Station>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task RecordingAdapter_DoesNotBypassMalformedPayloadOrCancellation()
    {
        var adapter = new RecordingEffectAdapter();
        var context = new ActionExecutionContext { Z21 = new Mock<IZ21>(MockBehavior.Strict).Object };
        var handler = new AudioWorkflowActionHandler(effectAdapter: adapter);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.ExecuteAsync(
            new WorkflowAction { Type = ActionType.Audio, Audio = new() { FilePath = " " } }, context)).ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await Assert.ThrowsAsync<OperationCanceledException>(() => handler.ExecuteAsync(
            new WorkflowAction { Type = ActionType.Audio, Audio = new() { FilePath = "missing.wav" } }, context, cancellation.Token)).ConfigureAwait(false);

        Assert.That(adapter.Requests, Is.Empty);
    }

    private sealed class RecordingEffectAdapter : IWorkflowEffectAdapter
    {
        public List<WorkflowEffectRequest> Requests { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];

        public Task ExecuteAsync(WorkflowEffectRequest request, ActionExecutionContext context, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Tokens.Add(cancellationToken);
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task ProductionDisplay_RemainsUnconfiguredAndMakesNoDeviceCall()
    {
        var z21 = new Mock<IZ21>(MockBehavior.Strict);
        await new TrainDestinationDisplayWorkflowActionHandler().ExecuteAsync(new WorkflowAction
        {
            Type = ActionType.TrainDestinationDisplay,
            TrainDestinationDisplay = new() { DisplayDeviceId = Guid.NewGuid() }
        }, new ActionExecutionContext { Z21 = z21.Object }).ConfigureAwait(false);
        z21.VerifyNoOtherCalls();
    }

    [Test]
    public async Task EffectAdapterFailure_PropagatesToExecutorCaller()
    {
        var adapter = new Mock<IWorkflowEffectAdapter>();
        adapter.Setup(value => value.ExecuteAsync(It.IsAny<WorkflowEffectRequest>(),
            It.IsAny<ActionExecutionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Effect failed"));
        var handler = new CommandWorkflowActionHandler(effectAdapter: adapter.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(new WorkflowAction
        {
            Type = ActionType.Command, Command = new() { BytesBase64 = "AQID" }
        }, new ActionExecutionContext { Z21 = new Mock<IZ21>(MockBehavior.Strict).Object })).ConfigureAwait(false);
    }

    [Test]
    public async Task AllHandlers_PreCancelledToken_DoNotInvokeAdapterOrMutateJourney()
    {
        var adapter = new Mock<IWorkflowEffectAdapter>(MockBehavior.Strict);
        IWorkflowActionHandler[] handlers =
        [
            new CommandWorkflowActionHandler(effectAdapter: adapter.Object),
            new AudioWorkflowActionHandler(effectAdapter: adapter.Object),
            new AnnouncementWorkflowActionHandler(effectAdapter: adapter.Object),
            new ExecuteScriptWorkflowActionHandler(effectAdapter: adapter.Object),
            new SelectSignalAspectWorkflowActionHandler(effectAdapter: adapter.Object),
            new TrainDestinationDisplayWorkflowActionHandler(effectAdapter: adapter.Object),
            new ChangeJourneyStopWorkflowActionHandler()
        ];
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        foreach (var handler in handlers)
            await Assert.ThrowsAsync<OperationCanceledException>(() => handler.ExecuteAsync(
                new WorkflowAction { Type = handler.ActionType },
                new ActionExecutionContext { Z21 = new Mock<IZ21>(MockBehavior.Strict).Object }, cancellation.Token)).ConfigureAwait(false);
        adapter.VerifyNoOtherCalls();
    }

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
    public async Task AudioHandler_MissingFile_ThrowsFileNotFound()
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

        await Assert.ThrowsAsync<FileNotFoundException>(() => handler.ExecuteAsync(action, context)).ConfigureAwait(false);
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
    public async Task ExecuteScriptHandler_MissingScriptFile_ThrowsFileNotFound()
    {
        var handler = new ExecuteScriptWorkflowActionHandler(fileSystem: new FakeFileSystem());
        var action = new WorkflowAction
        {
            Type = ActionType.ExecuteScript,
            PowerShell = new PowerShellActionPayload { ScriptPath = @"C:\missing.ps1" }
        };

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            handler.ExecuteAsync(action, new ActionExecutionContext { Z21 = Mock.Of<IZ21>() })).ConfigureAwait(false);
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
    public async Task MutatingHandlers_PreCancelledToken_DoesNotStartEffect()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
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

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new ChangeJourneyStopWorkflowActionHandler().ExecuteAsync(action, context, cancellation.Token)).ConfigureAwait(false);
        Assert.That(state.CurrentStationId, Is.EqualTo(first.Id));
    }

    [Test]
    public async Task ExecuteScriptHandler_PreCancelledToken_DoesNotStartProcess()
    {
        const string path = "existing.ps1";
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        var action = new WorkflowAction
        {
            Type = ActionType.ExecuteScript,
            PowerShell = new PowerShellActionPayload { ScriptPath = path }
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new ExecuteScriptWorkflowActionHandler(fileSystem: new FakeFileSystem(path)).ExecuteAsync(
                action,
                new ActionExecutionContext { Z21 = Mock.Of<IZ21>() },
                cancellation.Token)).ConfigureAwait(false);
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

        public bool IsSpeechOutputAvailable => true;

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
