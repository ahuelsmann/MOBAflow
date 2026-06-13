// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moba.Common.Configuration;
using Moba.Sound;

internal class PiperSpeechEngineTest
{
    [Test]
    public void Name_Should_BePiperTts()
    {
        var engine = CreateEngine(new SpeechOptions(), new FakePiperProcessRunner(), new FakePiperAudioPlayer());

        Assert.That(engine.Name, Is.EqualTo("Piper TTS"));
    }

    [Test]
    public void AnnouncementAsync_Should_Throw_WhenExecutableMissing()
    {
        var options = new SpeechOptions
        {
            PiperExecutablePath = @"C:\missing\piper.exe",
            PiperModelPath = CreateTempFile()
        };

        var engine = CreateEngine(options, new FakePiperProcessRunner(), new FakePiperAudioPlayer());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await engine.AnnouncementAsync("Naechster Halt Bielefeld Hauptbahnhof.", null));

        Assert.That(ex!.Message, Does.Contain("Piper executable not found"));
    }

    [Test]
    public void AnnouncementAsync_Should_Throw_WhenModelMissing()
    {
        var options = new SpeechOptions
        {
            PiperExecutablePath = CreateTempFile(),
            PiperModelPath = @"C:\missing\voice.onnx"
        };

        var engine = CreateEngine(options, new FakePiperProcessRunner(), new FakePiperAudioPlayer());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await engine.AnnouncementAsync("Naechster Halt Bielefeld Hauptbahnhof.", null));

        Assert.That(ex!.Message, Does.Contain("Piper model not found"));
    }

    [Test]
    public async Task AnnouncementAsync_Should_RunPiperAndPlayGeneratedAudio()
    {
        var executablePath = CreateTempFile();
        var modelPath = CreateTempFile();
        var runner = new FakePiperProcessRunner();
        var audioPlayer = new FakePiperAudioPlayer();
        var options = new SpeechOptions
        {
            PiperExecutablePath = executablePath,
            PiperModelPath = modelPath,
            Rate = -2
        };

        var engine = CreateEngine(options, runner, audioPlayer);

        await engine.AnnouncementAsync("Naechster Halt Bielefeld Hauptbahnhof.", null);

        Assert.Multiple(() =>
        {
            Assert.That(runner.LastRequest, Is.Not.Null);
            Assert.That(runner.LastRequest!.ExecutablePath, Is.EqualTo(executablePath));
            Assert.That(runner.LastRequest.ModelPath, Is.EqualTo(modelPath));
            Assert.That(runner.LastRequest.Text, Is.EqualTo("Naechster Halt Bielefeld Hauptbahnhof."));
            Assert.That(runner.LastRequest.LengthScale, Is.EqualTo(1.1).Within(0.001));
            Assert.That(audioPlayer.PlayedPath, Is.Not.Empty);
        });
    }

    [Test]
    public async Task AnnouncementAsync_Should_NormalizeTextBeforePiper()
    {
        var executablePath = CreateTempFile();
        var modelPath = CreateTempFile();
        var runner = new FakePiperProcessRunner();
        var options = new SpeechOptions
        {
            PiperExecutablePath = executablePath,
            PiperModelPath = modelPath,
            EnablePronunciationNormalization = true
        };

        var engine = CreateEngine(options, runner, new FakePiperAudioPlayer());

        await engine.AnnouncementAsync("Nächster Halt Berlin Hbf, Gleis 3.", null);

        Assert.That(
            runner.LastRequest!.Text,
            Is.EqualTo("Nächster Halt Berlin Hauptbahnhof, Gleis drei."));
    }

    [Test]
    public async Task AnnouncementAsync_Should_PassSentenceSilenceToPiper()
    {
        var executablePath = CreateTempFile();
        var modelPath = CreateTempFile();
        var runner = new FakePiperProcessRunner();
        var options = new SpeechOptions
        {
            PiperExecutablePath = executablePath,
            PiperModelPath = modelPath,
            PiperSentenceSilenceSeconds = 0.4
        };

        var engine = CreateEngine(options, runner, new FakePiperAudioPlayer());

        await engine.AnnouncementAsync("Test.", null);

        Assert.That(runner.LastRequest!.SentenceSilenceSeconds, Is.EqualTo(0.4).Within(0.001));
    }

    [Test]
    public async Task AnnouncementAsync_Should_MapSpeechRateToPiperLengthScale()
    {
        var executablePath = CreateTempFile();
        var modelPath = CreateTempFile();
        var slowRunner = new FakePiperProcessRunner();
        var normalRunner = new FakePiperProcessRunner();
        var fastRunner = new FakePiperProcessRunner();

        await CreateEngine(
            new SpeechOptions { PiperExecutablePath = executablePath, PiperModelPath = modelPath, Rate = -10 },
            slowRunner,
            new FakePiperAudioPlayer()).AnnouncementAsync("Langsam.", null);
        await CreateEngine(
            new SpeechOptions { PiperExecutablePath = executablePath, PiperModelPath = modelPath, Rate = 0 },
            normalRunner,
            new FakePiperAudioPlayer()).AnnouncementAsync("Normal.", null);
        await CreateEngine(
            new SpeechOptions { PiperExecutablePath = executablePath, PiperModelPath = modelPath, Rate = 10 },
            fastRunner,
            new FakePiperAudioPlayer()).AnnouncementAsync("Schnell.", null);

        Assert.Multiple(() =>
        {
            Assert.That(slowRunner.LastRequest!.LengthScale, Is.EqualTo(1.5).Within(0.001));
            Assert.That(normalRunner.LastRequest!.LengthScale, Is.EqualTo(1.0).Within(0.001));
            Assert.That(fastRunner.LastRequest!.LengthScale, Is.EqualTo(0.5).Within(0.001));
        });
    }

    [Test]
    public void SpeakerEngineFactory_Should_CreatePiper_WhenPiperSelectedAndConfigured()
    {
        var executablePath = CreateTempFile();
        var modelPath = CreateTempFile();
        var appSettings = new AppSettings
        {
            Speech =
            {
                SpeakerEngineName = SpeechSpeakerEngineSelection.PiperDisplayName,
                PiperExecutablePath = executablePath,
                PiperModelPath = modelPath,
                Rate = 7
            }
        };
        var factory = new SpeakerEngineFactory(
            appSettings,
            new OptionsMonitorWrapper(new SpeechOptions()),
            NullLogger<PiperSpeechEngine>.Instance,
            NullLogger<SystemSpeechEngine>.Instance);

        var engine = factory.CreateEngineFromOptions();

        Assert.That(engine, Is.TypeOf<PiperSpeechEngine>());
    }

    [Test]
    public void SpeakerEngineFactory_Should_CreatePiper_WhenPiperSelectedEvenWithMissingPaths()
    {
        var appSettings = new AppSettings
        {
            Speech =
            {
                SpeakerEngineName = SpeechSpeakerEngineSelection.PiperDisplayName
            }
        };
        var options = new SpeechOptions();
        var factory = new SpeakerEngineFactory(
            appSettings,
            new OptionsMonitorWrapper(options),
            NullLogger<PiperSpeechEngine>.Instance,
            NullLogger<SystemSpeechEngine>.Instance);

        var engine = factory.CreateEngineFromOptions();

        Assert.That(engine, Is.TypeOf<PiperSpeechEngine>());
    }

    [Test]
    public void SpeakerEngineFactory_Should_CreateRegisteredEngine_WhenRegistrationMatches()
    {
        var appSettings = new AppSettings
        {
            Speech =
            {
                SpeakerEngineName = "CustomEngine"
            }
        };
        var factory = new SpeakerEngineFactory(
            appSettings,
            new OptionsMonitorWrapper(new SpeechOptions()),
            NullLogger<PiperSpeechEngine>.Instance,
            NullLogger<SystemSpeechEngine>.Instance,
            [new CustomSpeakerEngineRegistration()]);

        var engine = factory.CreateEngineFromOptions();

        Assert.That(engine, Is.TypeOf<CustomSpeakerEngine>());
    }

    private static PiperSpeechEngine CreateEngine(
        SpeechOptions options,
        IPiperProcessRunner runner,
        IPiperAudioPlayer audioPlayer)
    {
        return new PiperSpeechEngine(
            new OptionsMonitorWrapper(options),
            NullLogger<PiperSpeechEngine>.Instance,
            runner,
            audioPlayer);
    }

    private static string CreateTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mobaflow-test-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private sealed class FakePiperProcessRunner : IPiperProcessRunner
    {
        public PiperSynthesisRequest? LastRequest { get; private set; }

        public Task<PiperProcessResult> SynthesizeAsync(PiperSynthesisRequest request)
        {
            LastRequest = request;
            File.WriteAllText(request.OutputPath, "fake wav");
            return Task.FromResult(new PiperProcessResult(0, string.Empty, string.Empty));
        }
    }

    private sealed class FakePiperAudioPlayer : IPiperAudioPlayer
    {
        public string PlayedPath { get; private set; } = string.Empty;

        public Task PlayAsync(string wavPath)
        {
            PlayedPath = wavPath;
            return Task.CompletedTask;
        }
    }

    private sealed class OptionsMonitorWrapper(SpeechOptions value) : IOptionsMonitor<SpeechOptions>
    {
        public SpeechOptions CurrentValue => value;

        public SpeechOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<SpeechOptions, string?> listener) => null;
    }

    private sealed class CustomSpeakerEngineRegistration : ISpeakerEngineRegistration
    {
        public string EngineName => "CustomEngine";

        public bool IsFallback => false;

        public bool CanCreate(string engineName) => engineName == EngineName;

        public ISpeakerEngine Create(SpeechOptions options)
        {
            _ = options;
            return new CustomSpeakerEngine();
        }
    }

    private sealed class CustomSpeakerEngine : ISpeakerEngine
    {
        public string Name { get; } = "CustomEngine";

        public Task AnnouncementAsync(string message, string? voiceName)
        {
            _ = message;
            _ = voiceName;
            return Task.CompletedTask;
        }
    }
}
