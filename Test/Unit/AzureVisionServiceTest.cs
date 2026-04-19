// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moba.Vision;

[TestFixture]
internal class AzureVisionServiceTest
{
    private IVisionService _service = null!;

    [SetUp]
    public void Setup()
    {
        var opts = new VisionOptions
        {
            Key = "test-key",
            Endpoint = "https://example.cognitiveservices.azure.com/"
        };
        var monitor = new OptionsMonitorWrapper(opts);
        var logger = NullLogger<AzureVisionService>.Instance;

        _service = new AzureVisionService(monitor, logger);
        Assert.That(_service, Is.Not.Null);
        Assert.That(_service.Name, Is.EqualTo("Azure.AI.Vision.ImageAnalysis"));
        Assert.That(_service.IsConfigured, Is.True);
    }

    [Test]
    public async Task ReadTextAsync_WithTestKey_ReturnsEmptyResult()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var result = await _service.ReadTextAsync(ms);

        Assert.That(result, Is.SameAs(VisionReadResult.Empty));
        Assert.That(result.Lines, Is.Empty);
        Assert.That(result.WordCount, Is.Zero);
    }

    [Test]
    public void ReadTextAsync_WithMissingPath_ThrowsFileNotFound()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _service.ReadTextAsync(missingPath));
    }

    [Test]
    public void ReadTextAsync_WithoutCredentials_ThrowsInvalidOperation()
    {
        // Shield the test from developer-machine env vars that would otherwise make the
        // service consider itself configured.
        var originalKey = Environment.GetEnvironmentVariable("VISION_KEY");
        var originalEndpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("VISION_KEY", null);
            Environment.SetEnvironmentVariable("VISION_ENDPOINT", null);

            var monitor = new OptionsMonitorWrapper(new VisionOptions());
            var service = new AzureVisionService(monitor, NullLogger<AzureVisionService>.Instance);
            using var ms = new MemoryStream(new byte[] { 1, 2, 3 });

            Assert.That(service.IsConfigured, Is.False);
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.ReadTextAsync(ms));
        }
        finally
        {
            Environment.SetEnvironmentVariable("VISION_KEY", originalKey);
            Environment.SetEnvironmentVariable("VISION_ENDPOINT", originalEndpoint);
        }
    }

    private sealed class OptionsMonitorWrapper : IOptionsMonitor<VisionOptions>
    {
        private readonly VisionOptions _value;

        public OptionsMonitorWrapper(VisionOptions value) => _value = value;

        public VisionOptions CurrentValue => _value;

        public VisionOptions Get(string? name) => _value;

        public IDisposable? OnChange(Action<VisionOptions, string?> listener) => null;
    }
}
