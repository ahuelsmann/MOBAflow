// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moba.Sound;

public class CognitiveSpeechEngineTest
{
    private ISpeakerEngine _speakerEngine;

    [SetUp]
    public void Setup()
    {
        // Configure SpeechOptions with test credentials
        var options = Options.Create(new SpeechOptions
        {
            Key = "test-key",
            Region = "germanywestcentral"
        });

        // Wrap IOptions in IOptionsMonitor for testing
        var optionsMonitor = new OptionsMonitorWrapper(options.Value);

        // Create a simple logger factory for testing
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<CognitiveSpeechEngine>();

        _speakerEngine = new CognitiveSpeechEngine(optionsMonitor, logger);

        Assert.That(_speakerEngine, Is.Not.Null);
        Assert.That(_speakerEngine.Name, Is.EqualTo("Microsoft.CognitiveServices.Speech"));
    }

    [Test]
    public Task OutputSpeech()
    {
        Assert.DoesNotThrowAsync(async () => 
            await _speakerEngine.AnnouncementAsync("Naechster Halt Bielefeld Hauptbahnhof. Ausstieg in Fahrtrichtung rechts.", "ElkeNeural"));
        return Task.CompletedTask;
    }

    // Simple wrapper to convert IOptions to IOptionsMonitor for testing
    private class OptionsMonitorWrapper : IOptionsMonitor<SpeechOptions>
    {
        private readonly SpeechOptions _value;

        public OptionsMonitorWrapper(SpeechOptions value)
        {
            _value = value;
        }

        public SpeechOptions CurrentValue => _value;

        public SpeechOptions Get(string? name) => _value;

        public IDisposable? OnChange(Action<SpeechOptions, string?> listener) => null;
    }
}
