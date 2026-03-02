// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Microsoft.Extensions.Logging;
using Moba.Sound;

/// <summary>
/// Minimal test for Windows SAPI Text-to-Speech.
/// This is a temporary fallback solution while Microsoft.CognitiveServices.Speech is being repaired.
/// </summary>
internal class SystemSpeechEngineTest
{
    private ISpeakerEngine _speakerEngine = null!;

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
        // Some CI agents have no audio device which causes System.Speech to throw an AudioException.
        // In that case we mark the test as ignored instead of failing the pipeline.
        try
        {
            Assert.DoesNotThrowAsync(async () =>
                await _speakerEngine.AnnouncementAsync("Test Nachricht.", null));
        }
        catch (System.Speech.Internal.Synthesis.AudioException)
        {
            Assert.Ignore("Skipping SystemSpeechEngine test because no audio device is available on this environment.");
        }

        return Task.CompletedTask;
    }
}
