namespace Moba.Test.Unit;

using Sound;

public class CognitiveSpeechEngineTest
{
    private ISpeakerEngine _speakerEngine;

    [SetUp]
    public void Setup()
    {
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