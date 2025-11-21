// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Sound;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

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

        // Create a simple logger factory for testing
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<CognitiveSpeechEngine>();

        _speakerEngine = new CognitiveSpeechEngine(options, logger);

        Assert.That(_speakerEngine, Is.Not.Null);
        Assert.That(_speakerEngine.Name, Is.EqualTo("Microsoft.CognitiveServices.Speech"));
    }

    [Test]
    public async Task OutputSpeech()
    {
        Assert.DoesNotThrowAsync(async () => 
            await _speakerEngine.AnnouncementAsync("Nächster Halt Bielefeld Hauptbahnhof. Ausstieg in Fahrtrichtung rechts.", "ElkeNeural"));
    }
}