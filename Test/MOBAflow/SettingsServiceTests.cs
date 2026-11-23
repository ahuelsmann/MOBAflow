#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.SharedUI.Configuration;
using Moba.WinUI.Service;
using System.Text.Json;

[TestFixture]
internal sealed class SettingsServiceTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task SaveAndReset_DoNotPersistGlobalZ21Endpoint(bool reset)
    {
        var directory = Path.Combine(Path.GetTempPath(), "mobaflow-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "appsettings.json");
            var settings = new UiSettings();
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
            var settings = new UiSettings();
            var service = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);
            settings.Speech.ProviderId = "piper";
            settings.Speech.Piper.ExecutablePath = "piper.exe";
            settings.Speech.Piper.ModelPath = "voice.onnx";
            settings.Speech.Piper.SentenceSilenceSeconds = 0.5;
            settings.Speech.SystemSpeech.DefaultVoiceName = "Windows voice";
            settings.Speech.Rate = 3;
            settings.Speech.Volume = 65;
            await service.SaveSettingsAsync(settings).ConfigureAwait(false);
            var loaded = new UiSettings();
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
    public async Task UiSections_SaveAndReloadThroughTheSettingsRoot()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mobaflow-ui-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "appsettings.json");
        try
        {
            var locomotiveId = Guid.NewGuid();
            var settings = new UiSettings();
            var service = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);
            FeatureToggleRegistry.TrySetPageAvailability(settings.FeatureToggles, FeatureTogglePages.Monitor, value: false);
            settings.FeatureToggles.Pages[FeatureTogglePages.Trains] = new FeatureTogglePageSettings { Label = string.Empty };
            settings.Layout.SolutionPage.IsProjectListExpanded = false;
            settings.WinUiTrainControlHost.SelectedLocomotiveFromProjectId = locomotiveId;
            settings.Display.Esp32IpAddress = "192.0.2.40";
            await service.SaveSettingsAsync(settings).ConfigureAwait(false);

            var loaded = new UiSettings();
            _ = new SettingsService(loaded, NullLogger<SettingsService>.Instance, path);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(loaded.FeatureToggles, FeatureTogglePages.Monitor), Is.False);
                Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(loaded.FeatureToggles, FeatureTogglePages.Overview), Is.True);
                Assert.That(FeatureToggleRegistry.GetBadgeLabel(loaded.FeatureToggles, FeatureTogglePages.Monitor), Is.EqualTo("Preview"));
                Assert.That(FeatureToggleRegistry.GetBadgeLabel(loaded.FeatureToggles, FeatureTogglePages.Trains), Is.Null);
                Assert.That(loaded.Layout.SolutionPage.IsProjectListExpanded, Is.False);
                Assert.That(loaded.WinUiTrainControlHost.SelectedLocomotiveFromProjectId, Is.EqualTo(locomotiveId));
                Assert.That(loaded.Display.Esp32IpAddress, Is.EqualTo("192.0.2.40"));
            }
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(directory);
        }
    }

    [Test]
    public async Task FeatureToggles_RemovedFlatFieldsLoadAsRegistryDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "mobaflow-old-toggles-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            await File.WriteAllTextAsync(path, """
                {"featureToggles":{"isMonitorPageAvailable":false,"trainControlPageLabel":""}}
                """).ConfigureAwait(false);
            var settings = new UiSettings();
            _ = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.FeatureToggles.Pages, Is.Empty);
                Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings.FeatureToggles, FeatureTogglePages.Monitor), Is.True);
                Assert.That(FeatureToggleRegistry.GetBadgeLabel(settings.FeatureToggles, FeatureTogglePages.TrainControl), Is.EqualTo("Preview"));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task FeatureToggles_LegacyDisabledOverviewIsNormalizedOnLoadAndSave()
    {
        var path = Path.Combine(Path.GetTempPath(), "mobaflow-overview-toggle-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            await File.WriteAllTextAsync(path, """
                {"featureToggles":{"pages":{"Overview":{"isAvailable":false,"label":""},"Monitor":{"isAvailable":false}}}}
                """).ConfigureAwait(false);

            var settings = new UiSettings();
            var service = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.FeatureToggles.Pages[FeatureTogglePages.Overview].IsAvailable, Is.True);
                Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings.FeatureToggles, FeatureTogglePages.Overview), Is.True);
                Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(settings.FeatureToggles, FeatureTogglePages.Monitor), Is.False);
            }

            await service.SaveSettingsAsync(settings).ConfigureAwait(false);
            using var saved = JsonDocument.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false));
            var pages = saved.RootElement.GetProperty("featureToggles").GetProperty("pages");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pages.GetProperty(FeatureTogglePages.Overview).GetProperty("isAvailable").GetBoolean(), Is.True);
                Assert.That(pages.GetProperty(FeatureTogglePages.Monitor).GetProperty("isAvailable").GetBoolean(), Is.False);
            }
        }
        finally
        {
            File.Delete(path);
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
            var settings = new UiSettings();
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
