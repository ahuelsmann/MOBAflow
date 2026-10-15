#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.WinUI.Service;
using System.Text.Json;

[TestFixture]
internal sealed class SettingsServiceTests
{
    [TestCase("system")]
    [TestCase("en-US")]
    [TestCase("de-DE")]
    public async Task UiLanguageSaveReloadPreservesSpeechAndOtherPreferences(string language)
    {
        var directory = Path.Combine(Path.GetTempPath(), "mobaflow-language-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "appsettings.json");
        try
        {
            var settings = new AppSettings();
            var service = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);
            settings.Application.UiLanguage = language;
            settings.Application.LastSolutionPath = "user-solution.json";
            settings.Speech.Language = "fr-FR";
            settings.Speech.TestMessage = "User announcement";
            await service.SaveSettingsAsync(settings);
            var loaded = new AppSettings();
            _ = new SettingsService(loaded, NullLogger<SettingsService>.Instance, path);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(loaded.Application.UiLanguage, Is.EqualTo(language));
                Assert.That(loaded.Application.LastSolutionPath, Is.EqualTo("user-solution.json"));
                Assert.That(loaded.Speech.Language, Is.EqualTo("fr-FR"));
                Assert.That(loaded.Speech.TestMessage, Is.EqualTo("User announcement"));
            }
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(directory);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SaveAndReset_DoNotPersistGlobalZ21Endpoint(bool reset)
    {
        var directory = Path.Combine(Path.GetTempPath(), "mobaflow-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "appsettings.json");
            var settings = new AppSettings();
            settings.Z21.AutoConnectRetryIntervalSeconds = 17;
            var service = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);

            if (reset)
                await service.ResetToDefaultsAsync().ConfigureAwait(false);
            else
                await service.SaveSettingsAsync(settings).ConfigureAwait(false);

            using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false));
            var z21 = saved.RootElement.GetProperty("z21");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(z21.TryGetProperty("currentIpAddress", out _), Is.False);
                Assert.That(z21.TryGetProperty("defaultPort", out _), Is.False);
                Assert.That(z21.GetProperty("autoConnectRetryIntervalSeconds").GetInt32(), Is.EqualTo(reset ? 10 : 17));
                Assert.That(saved.RootElement.TryGetProperty("application", out _), Is.True);
            }
        }
        finally
        {
            File.Delete(Path.Combine(directory, "appsettings.json"));
            Directory.Delete(directory);
        }
    }
    [Test]
    public async Task SpeechOptions_SaveAndReloadBothLocalProviderSections()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mobaflow-speech-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "appsettings.json");
        try
        {
            var settings = new AppSettings();
            var service = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);
            settings.Speech.ProviderId = "piper";
            settings.Speech.Piper.ExecutablePath = "piper.exe";
            settings.Speech.Piper.ModelPath = "voice.onnx";
            settings.Speech.Piper.SentenceSilenceSeconds = 0.5;
            settings.Speech.SystemSpeech.DefaultVoiceName = "Windows voice";
            settings.Speech.Rate = 3;
            settings.Speech.Volume = 65;
            await service.SaveSettingsAsync(settings).ConfigureAwait(false);
            var loaded = new AppSettings();
            _ = new SettingsService(loaded, NullLogger<SettingsService>.Instance, path);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(loaded.Speech.ProviderId, Is.EqualTo("piper"));
                Assert.That(loaded.Speech.Piper.ExecutablePath, Is.EqualTo("piper.exe"));
                Assert.That(loaded.Speech.Piper.ModelPath, Is.EqualTo("voice.onnx"));
                Assert.That(loaded.Speech.Piper.SentenceSilenceSeconds, Is.EqualTo(0.5));
                Assert.That(loaded.Speech.SystemSpeech.DefaultVoiceName, Is.EqualTo("Windows voice"));
                Assert.That(loaded.Speech.Rate, Is.EqualTo(3));
                Assert.That(loaded.Speech.Volume, Is.EqualTo(65));
            }
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(directory);
        }
    }

    [Test]
    public async Task SpeechSettings_RemovedFieldsAreIgnoredByProductionLoader()
    {
        var path = Path.Combine(Path.GetTempPath(), "mobaflow-old-speech-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            await File.WriteAllTextAsync(path, """
                {"speech":{"speakerEngineName":"PiperTts","piperExecutablePath":"old.exe","piperModelPath":"old.onnx"}}
                """).ConfigureAwait(false);
            var settings = new AppSettings();
            settings.Speech.ProviderId = "piper";
            _ = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Speech.ProviderId, Is.EqualTo("system-speech"));
                Assert.That(settings.Speech.Piper.ExecutablePath, Is.Empty);
                Assert.That(settings.Speech.Piper.ModelPath, Is.Empty);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}
#endif
