#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Configuration;
using Moba.Domain;
using Moba.WinUI.Service;

[TestFixture]
internal sealed class WinUiSettingsPersistenceTests
{
    [Test]
    public async Task SavingWinUiSettings_OmitsGlobalZ21EndpointWithoutChangingMobileSerialization()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mobaflow-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "appsettings.json");
            var settings = new AppSettings
            {
                Z21 = new Z21Settings
                {
                    CurrentIpAddress = "192.168.0.111", DefaultPort = "21105",
                    AutoConnectRetryIntervalSeconds = 17, SystemStatePollingIntervalSeconds = 4
                }
            };
            var service = new SettingsService(settings, NullLogger<SettingsService>.Instance, path);

            await service.SaveSettingsAsync(settings).ConfigureAwait(false);

            using var persisted = JsonDocument.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false));
            var z21 = persisted.RootElement.GetProperty("z21");
            using var mobile = JsonDocument.Parse(JsonSerializer.Serialize(settings, JsonOptions.Default));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(z21.TryGetProperty("currentIpAddress", out _), Is.False);
                Assert.That(z21.TryGetProperty("defaultPort", out _), Is.False);
                Assert.That(z21.GetProperty("autoConnectRetryIntervalSeconds").GetInt32(), Is.EqualTo(17));
                Assert.That(z21.GetProperty("systemStatePollingIntervalSeconds").GetInt32(), Is.EqualTo(4));
                Assert.That(mobile.RootElement.GetProperty("z21").GetProperty("currentIpAddress").GetString(), Is.EqualTo("192.168.0.111"));
                Assert.That(settings.Z21.DefaultPort, Is.EqualTo("21105"));
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
#endif
