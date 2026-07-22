// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Sound;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moba.Sound;

/// <summary>
/// Tests for <see cref="SpeechHealthCheck"/> configuration validation and status messages.
/// Does not start Piper; only verifies path resolution and file-existence checks.
/// </summary>
[TestFixture]
internal sealed class SpeechHealthCheckTests
{
    private string _tempDir = null!;
    private string _executablePath = null!;
    private string _modelPath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"moba-speech-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _executablePath = Path.Combine(_tempDir, "piper.exe");
        _modelPath = Path.Combine(_tempDir, "model.onnx");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public void IsConfigured_WhenPathsMissing_ReturnsFalse()
    {
        var healthCheck = CreateHealthCheck(new SpeechOptions
        {
            PiperExecutablePath = Path.Combine(_tempDir, "missing.exe"),
            PiperModelPath = Path.Combine(_tempDir, "missing.onnx")
        });

        Assert.That(healthCheck.IsConfigured(), Is.False);
    }

    [Test]
    public void IsConfigured_WhenFilesExist_ReturnsTrue()
    {
        File.WriteAllText(_executablePath, "stub");
        File.WriteAllText(_modelPath, "stub");

        var healthCheck = CreateHealthCheck(new SpeechOptions
        {
            PiperExecutablePath = _executablePath,
            PiperModelPath = _modelPath
        });

        Assert.That(healthCheck.IsConfigured(), Is.True);
    }

    [Test]
    public void GetStatusMessage_WhenExecutableMissing_ReturnsExecutableNotFoundMessage()
    {
        var healthCheck = CreateHealthCheck(new SpeechOptions
        {
            PiperExecutablePath = _executablePath,
            PiperModelPath = _modelPath
        });

        var message = healthCheck.GetStatusMessage();

        Assert.That(message, Does.Contain("executable not found"));
    }

    [Test]
    public void GetStatusMessage_WhenModelMissing_ReturnsModelNotFoundMessage()
    {
        File.WriteAllText(_executablePath, "stub");
        var healthCheck = CreateHealthCheck(new SpeechOptions
        {
            PiperExecutablePath = _executablePath,
            PiperModelPath = _modelPath
        });

        var message = healthCheck.GetStatusMessage();

        Assert.That(message, Does.Contain("model not found"));
    }

    [Test]
    public void GetStatusMessage_WhenConfigured_ReturnsSuccessSummary()
    {
        File.WriteAllText(_executablePath, "stub");
        File.WriteAllText(_modelPath, "stub");

        var healthCheck = CreateHealthCheck(new SpeechOptions
        {
            PiperExecutablePath = _executablePath,
            PiperModelPath = _modelPath
        });

        var message = healthCheck.GetStatusMessage();

        Assert.Multiple(() =>
        {
            Assert.That(message, Does.Contain("Configured:"));
            Assert.That(message, Does.Contain("piper.exe"));
            Assert.That(message, Does.Contain("model.onnx"));
        });
    }

    [Test]
    public async Task TestConnectivityAsync_WhenNotConfigured_ReturnsFalse()
    {
        var healthCheck = CreateHealthCheck(new SpeechOptions());

        var result = await healthCheck.TestConnectivityAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task TestConnectivityAsync_WhenExecutableCannotStart_ReturnsFalse()
    {
        File.WriteAllText(_executablePath, "not a native executable");
        File.WriteAllText(_modelPath, "stub");
        var healthCheck = CreateHealthCheck(new SpeechOptions
        {
            PiperExecutablePath = _executablePath,
            PiperModelPath = _modelPath
        });

        var result = await healthCheck.TestConnectivityAsync();

        Assert.That(result, Is.False);
    }

    [TestCase("piper.exe", "model.onnx", true)]
    [TestCase("", "model.onnx", false)]
    [TestCase("piper.exe", " ", false)]
    public void SpeechOptions_IsConfigured_RequiresExecutableAndModel(
        string executablePath,
        string modelPath,
        bool expected)
    {
        var options = new SpeechOptions
        {
            PiperExecutablePath = executablePath,
            PiperModelPath = modelPath
        };

        Assert.That(options.IsConfigured, Is.EqualTo(expected));
    }

    private static SpeechHealthCheck CreateHealthCheck(SpeechOptions options)
    {
        return new SpeechHealthCheck(
            Options.Create(options),
            NullLogger<SpeechHealthCheck>.Instance);
    }
}
