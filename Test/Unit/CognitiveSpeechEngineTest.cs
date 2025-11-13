namespace Moba.Test.Unit;

using System;
using Sound;

public class CognitiveSpeechEngineTest
{
    private ISpeakerEngine _speakerEngine;

    [SetUp]
    public void Setup()
    {
        // Ensure required environment variables are set for tests so the engine does not throw
        Environment.SetEnvironmentVariable("SPEECH_KEY", "test-key");
        Environment.SetEnvironmentVariable("SPEECH_REGION", "germanywestcentral");

        _speakerEngine = new CognitiveSpeechEngine();

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