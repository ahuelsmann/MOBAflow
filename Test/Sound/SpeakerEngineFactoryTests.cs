// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Sound;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Sound;
using Moq;

/// <summary>
/// Tests for <see cref="SpeakerEngineFactory"/> engine selection without starting real TTS engines.
/// Uses lightweight registrations to keep behavior focused on configuration routing.
/// </summary>
[TestFixture]
internal sealed class SpeakerEngineFactoryTests
{
    private AppSettings _appSettings = null!;
    private SpeakerEngineFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _appSettings = new AppSettings
        {
            Speech = new SpeechSettings
            {
                SpeakerEngineName = SpeechSpeakerEngineSelection.SystemSpeech,
                PiperExecutablePath = "piper.exe",
                PiperModelPath = "model.onnx"
            }
        };

        var optionsMonitor = new TestOptionsMonitor<SpeechOptions>(new SpeechOptions());
        _factory = CreateFactory(optionsMonitor, CreateTestRegistrations());
    }

    [Test]
    public void CreateEngine_SystemSpeech_ReturnsRegisteredSystemEngine()
    {
        var engine = _factory.CreateEngine(SpeechSpeakerEngineSelection.SystemSpeech);

        Assert.That(engine.Name, Is.EqualTo("TestSystemEngine"));
    }

    [Test]
    public void CreateEngine_PiperTts_ReturnsRegisteredPiperEngine()
    {
        var engine = _factory.CreateEngine(SpeechSpeakerEngineSelection.PiperTts);

        Assert.That(engine.Name, Is.EqualTo("TestPiperEngine"));
    }

    [Test]
    public void CreateEngine_UnknownName_FallsBackToSystemEngine()
    {
        var engine = _factory.CreateEngine("UnknownEngine");

        Assert.That(engine.Name, Is.EqualTo("TestSystemEngine"));
    }

    [Test]
    public void CreateEngineFromOptions_UsesAppSettingsSpeakerEngineName()
    {
        _appSettings.Speech.SpeakerEngineName = SpeechSpeakerEngineSelection.PiperTts;

        var engine = _factory.CreateEngineFromOptions();

        Assert.That(engine.Name, Is.EqualTo("TestPiperEngine"));
    }

    [Test]
    public async Task AnnouncementService_ResolvesCurrentSelectionAndOptionsForEachCall()
    {
        var systemSpeaker = new Mock<ISpeakerEngine>();
        var piperSpeaker = new Mock<ISpeakerEngine>();
        systemSpeaker.Setup(engine => engine.AnnouncementAsync(
                It.IsAny<string>(), null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        piperSpeaker.Setup(engine => engine.AnnouncementAsync(
                It.IsAny<string>(), null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var systemRegistration = new Mock<ISpeakerEngineRegistration>();
        var piperRegistration = new Mock<ISpeakerEngineRegistration>();
        systemRegistration.Setup(value => value.CanCreate(SpeechSpeakerEngineSelection.SystemSpeech)).Returns(true);
        piperRegistration.Setup(value => value.CanCreate(SpeechSpeakerEngineSelection.PiperTts)).Returns(true);
        SpeechOptions? firstOptions = null;
        SpeechOptions? secondOptions = null;
        systemRegistration.Setup(value => value.Create(It.IsAny<SpeechOptions>()))
            .Callback<SpeechOptions>(value => firstOptions = value).Returns(systemSpeaker.Object);
        piperRegistration.Setup(value => value.Create(It.IsAny<SpeechOptions>()))
            .Callback<SpeechOptions>(value => secondOptions = value).Returns(piperSpeaker.Object);
        var factory = CreateFactory(
            new TestOptionsMonitor<SpeechOptions>(new SpeechOptions { PiperTimeoutSeconds = 17 }),
            [systemRegistration.Object, piperRegistration.Object]);
        var service = new AnnouncementService(factory);
        _appSettings.Speech.VoiceName = "First voice";
        _appSettings.Speech.Rate = -3;
        _appSettings.Speech.Volume = 45;

        await service.GenerateAndSpeakAnnouncementAsync("First", new Station(), 1).ConfigureAwait(false);
        _appSettings.Speech.SpeakerEngineName = SpeechSpeakerEngineSelection.PiperTts;
        _appSettings.Speech.VoiceName = "Second voice";
        _appSettings.Speech.Rate = 6;
        _appSettings.Speech.Volume = 80;
        _appSettings.Speech.PiperModelPath = "second-model.onnx";
        await service.GenerateAndSpeakAnnouncementAsync("Second", new Station(), 1).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstOptions, Is.Not.Null);
            Assert.That(secondOptions, Is.Not.Null.And.Not.SameAs(firstOptions));
            Assert.That(firstOptions!.VoiceName, Is.EqualTo("First voice"));
            Assert.That(firstOptions.Rate, Is.EqualTo(-3));
            Assert.That(firstOptions.Volume, Is.EqualTo(45));
            Assert.That(secondOptions!.VoiceName, Is.EqualTo("Second voice"));
            Assert.That(secondOptions.Rate, Is.EqualTo(6));
            Assert.That(secondOptions.Volume, Is.EqualTo(80));
            Assert.That(secondOptions.PiperModelPath, Is.EqualTo("second-model.onnx"));
            Assert.That(secondOptions.PiperTimeoutSeconds, Is.EqualTo(17));
        }
        systemSpeaker.Verify(engine => engine.AnnouncementAsync("First", null, CancellationToken.None), Times.Once);
        piperSpeaker.Verify(engine => engine.AnnouncementAsync("Second", null, CancellationToken.None), Times.Once);
        systemRegistration.Verify(value => value.Create(It.IsAny<SpeechOptions>()), Times.Once);
        piperRegistration.Verify(value => value.Create(It.IsAny<SpeechOptions>()), Times.Once);
    }

    private SpeakerEngineFactory CreateFactory(
        IOptionsMonitor<SpeechOptions> optionsMonitor,
        IEnumerable<ISpeakerEngineRegistration> registrations) => new(
            _appSettings,
            optionsMonitor,
            NullLogger<PiperSpeechEngine>.Instance,
            NullLogger<SystemSpeechEngine>.Instance,
            registrations);

    private static IReadOnlyList<ISpeakerEngineRegistration> CreateTestRegistrations() =>
    [
        new TestSpeakerEngineRegistration(
            SpeechSpeakerEngineSelection.PiperDisplayName,
            isFallback: false,
            canCreate: SpeechSpeakerEngineSelection.ShouldUsePiperTts,
            engineName: "TestPiperEngine"),
        new TestSpeakerEngineRegistration(
            SpeechSpeakerEngineSelection.SystemSpeech,
            isFallback: true,
            canCreate: SpeechSpeakerEngineSelection.ShouldUseSystemSpeech,
            engineName: "TestSystemEngine")
    ];

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class TestSpeakerEngineRegistration(
        string displayName,
        bool isFallback,
        Func<string, bool> canCreate,
        string engineName) : ISpeakerEngineRegistration
    {
        public string EngineName => displayName;

        public bool IsFallback => isFallback;

        public bool CanCreate(string engineName) => canCreate(engineName);

        public ISpeakerEngine Create(SpeechOptions options)
        {
            _ = options;
            return new NamedTestEngine(engineName);
        }
    }

    private sealed class NamedTestEngine(string name) : ISpeakerEngine
    {
        public string Name { get; } = name;

        public Task AnnouncementAsync(string message, string? voiceName = null)
            => Task.CompletedTask;

        public Task AnnouncementAsync(
            string message,
            string? voiceName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return AnnouncementAsync(message, voiceName);
        }
    }
}
