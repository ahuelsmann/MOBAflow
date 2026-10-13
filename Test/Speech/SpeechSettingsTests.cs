// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Speech;

using Moba.Common.Configuration;
using System.Text.Json;

[TestFixture]
internal sealed class SpeechSettingsTests
{
    [TestCase("{}")]
    [TestCase("{\"Speech\":{\"SpeakerEngineName\":\"PiperTts\",\"PiperExecutablePath\":\"old.exe\",\"PiperModelPath\":\"old.onnx\",\"PiperConfigPath\":\"old.json\",\"EnablePiperAudioDiagnostics\":true}}")]
    public void FreshOrRemovedFields_SelectSystemSpeechWithoutCopyingLegacyOptions(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json)!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Speech.ProviderId, Is.EqualTo("system-speech"));
            Assert.That(settings.Speech.Piper.ExecutablePath, Is.Empty);
            Assert.That(settings.Speech.Piper.ModelPath, Is.Empty);
            Assert.That(settings.Speech.Piper.ConfigPath, Is.Empty);
            Assert.That(settings.Speech.Piper.EnableAudioDiagnostics, Is.False);
        }
    }

    [Test]
    public void NewOptions_RoundTripWithoutRemovedFields()
    {
        var settings = new AppSettings();
        settings.Speech.ProviderId = "piper";
        settings.Speech.Rate = 2;
        settings.Speech.Volume = 55;
        settings.Speech.Piper.ModelPath = "test.onnx";
        settings.Speech.Piper.SentenceSilenceSeconds = 0.4;
        settings.Speech.SystemSpeech.DefaultVoiceName = "Windows voice";
        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<AppSettings>(json)!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored.Speech.ProviderId, Is.EqualTo("piper"));
            Assert.That(restored.Speech.Rate, Is.EqualTo(2));
            Assert.That(restored.Speech.Volume, Is.EqualTo(55));
            Assert.That(restored.Speech.Piper.ModelPath, Is.EqualTo("test.onnx"));
            Assert.That(restored.Speech.Piper.SentenceSilenceSeconds, Is.EqualTo(0.4));
            Assert.That(restored.Speech.SystemSpeech.DefaultVoiceName, Is.EqualTo("Windows voice"));
            Assert.That(json, Does.Not.Contain("SpeakerEngineName"));
            Assert.That(json, Does.Not.Contain("PiperModelPath"));
        }
    }
}
