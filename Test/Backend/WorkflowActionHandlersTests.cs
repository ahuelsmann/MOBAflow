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

        z21.Verify(z => z.SendCommandAsync(bytes), Times.Once);
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

        z21.Verify(z => z.SendCommandAsync(It.IsAny<byte[]>()), Times.Never);
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

        public Task PlayAsync(string waveFile, CancellationToken cancellationToken = default)
        {
            LastPath = waveFile;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAnnouncementService : IAnnouncementService
    {
        public int SpeakCalls { get; private set; }

        public Station? LastStation { get; private set; }

        public int LastStationIndex { get; private set; }

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
            _ = cancellationToken;
            _ = templateName;
            _ = suppressSpeechErrors;
            SpeakCalls++;
            LastStation = station;
            LastStationIndex = stationIndex;
            return Task.CompletedTask;
        }
    }
}
