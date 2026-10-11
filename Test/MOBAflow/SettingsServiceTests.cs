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
            settings.Z21.CurrentIpAddress = "192.0.2.42";
            settings.Z21.DefaultPort = "21106";
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
}
#endif
