// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Sound;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moba.Common.Configuration;
using Moba.Sound;

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
        _factory = new SpeakerEngineFactory(
            _appSettings,
            optionsMonitor,
            NullLogger<PiperSpeechEngine>.Instance,
            NullLogger<SystemSpeechEngine>.Instance,
            CreateTestRegistrations());
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
    }
}