namespace Moba.Test.Unit;

using Sound;

public class SpeechTest
{
    private SpeakerEngine _speakerEngine;

    [SetUp]
    public void Setup()
    {
        _speakerEngine = new SpeakerEngine();

        Assert.That(_speakerEngine, Is.Not.Null);
    }

    [Test]
    public async Task OutputSpeech()
    {
        Assert.DoesNotThrow(() => _speakerEngine.AnnouncementAsync("Test message.", "ElkeNeural"));
    }
}