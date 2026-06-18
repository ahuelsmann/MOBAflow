// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;

/// <summary>
/// Tests for <see cref="SpeechSpeakerEngineSelection"/> engine-name routing helpers.
/// </summary>
[TestFixture]
internal sealed class SpeechSpeakerEngineSelectionTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ShouldUseSystemSpeech_NullOrWhitespace_ReturnsTrue(string? engineName)
    {
        Assert.That(SpeechSpeakerEngineSelection.ShouldUseSystemSpeech(engineName), Is.True);
    }

    [TestCase("PiperTts")]
    [TestCase("Piper TTS")]
    public void ShouldUsePiperTts_PiperNames_ReturnsTrue(string engineName)
    {
        Assert.That(SpeechSpeakerEngineSelection.ShouldUsePiperTts(engineName), Is.True);
        Assert.That(SpeechSpeakerEngineSelection.ShouldUseSystemSpeech(engineName), Is.False);
    }

    [TestCase("SystemSpeech")]
    [TestCase("System Speech (Windows SAPI)")]
    public void ShouldUseSystemSpeech_KnownSystemNames_ReturnsTrue(string engineName)
    {
        Assert.That(SpeechSpeakerEngineSelection.ShouldUseSystemSpeech(engineName), Is.True);
        Assert.That(SpeechSpeakerEngineSelection.ShouldUsePiperTts(engineName), Is.False);
    }
}