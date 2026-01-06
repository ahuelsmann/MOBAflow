// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Microsoft.Extensions.Logging;
using Sound;

/// <summary>
/// Minimal test for Windows SAPI Text-to-Speech.
/// This is a temporary fallback solution while Microsoft.CognitiveServices.Speech is being repaired.
/// </summary>
public class SystemSpeechEngineTest
{
    private ISpeakerEngine _speakerEngine;

    [SetUp]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<SystemSpeechEngine>();

        _speakerEngine = new SystemSpeechEngine(logger);

        Assert.That(_speakerEngine, Is.Not.Null);
        Assert.That(_speakerEngine.Name, Is.EqualTo("System.Speech (Windows SAPI)"));
    }

    [Test]
    public Task OutputSpeech_MinimalTest()
    {
        // Minimal test to verify that Windows SAPI TTS works
        Assert.DoesNotThrowAsync(async () => 
            await _speakerEngine.AnnouncementAsync("Test Nachricht.", null));
        return Task.CompletedTask;
    }
}
